using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;

namespace AnimusForge;

internal static class DiplomacyPeaceTermsService
{
	public static int ResolveTributeAmount(string amountToken, Kingdom payer, Kingdom receiver)
	{
		string token = (amountToken ?? "").Trim();
		if (string.IsNullOrWhiteSpace(token) || token == "0")
		{
			return 0;
		}
		if (token.Equals("auto", StringComparison.OrdinalIgnoreCase))
		{
			return DiplomacyBehavior.TryBuildTributePowerContext(payer, receiver, out AfTributePowerContext context)
				? context.CalculatedTribute
				: 0;
		}
		return int.TryParse(token, out int parsed) ? ClampTributeAmount(payer, parsed) : -1;
	}

	public static int ClampTributeAmount(Kingdom payer, int requestedAmount)
	{
		if (payer == null)
		{
			return 0;
		}
		int maximum = (int)(payer.Fiefs.Sum(x => x?.Prosperity ?? 0f) * 0.15f * 0.35f);
		return (MBMath.ClampInt(requestedAmount, 0, Math.Max(0, maximum)) / 10) * 10;
	}

	public static int ResolveDurationDays(string durationToken, bool hasTribute)
	{
		string token = (durationToken ?? "").Trim();
		if (string.IsNullOrWhiteSpace(token) || token == "0" || token.Equals("default", StringComparison.OrdinalIgnoreCase))
		{
			return hasTribute ? 100 : 0;
		}
		return int.TryParse(token, out int parsed)
			? MBMath.ClampInt(parsed, 1, 252)
			: (hasTribute ? 100 : 0);
	}

	public static bool TryApplyPeace(
		Kingdom payer,
		Kingdom receiver,
		int requestedDailyTribute,
		int requestedDurationDays,
		string source,
		out int appliedDailyTribute,
		out int appliedDurationDays,
		out string failureReason)
	{
		appliedDailyTribute = 0;
		appliedDurationDays = 0;
		failureReason = "";
		if (payer == null || receiver == null || payer == receiver || payer.IsEliminated || receiver.IsEliminated)
		{
			failureReason = "王国目标无效";
			return false;
		}
		if (!FactionManager.IsAtWarAgainstFaction(payer, receiver))
		{
			failureReason = "双方已不处于战争状态";
			return false;
		}
		appliedDailyTribute = ClampTributeAmount(payer, requestedDailyTribute);
		appliedDurationDays = ResolveDurationDays(requestedDurationDays.ToString(), appliedDailyTribute > 0);
		int tributeForAction = appliedDailyTribute;
		int durationForAction = appliedDurationDays;
		MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked(source ?? "diplomacy_make_peace", () =>
			MakePeaceAction.ApplyByKingdomDecision(payer, receiver, tributeForAction, durationForAction));
		DiplomacyRecentPeaceGuard.RegisterPeace(payer, receiver, source ?? "diplomacy_make_peace");
		return true;
	}
}
