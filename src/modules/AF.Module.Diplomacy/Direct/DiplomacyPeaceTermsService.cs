using System;
using System.Linq;
using AnimusForge.Refactor.Contracts;
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
			return DiplomacyConversationBridge.TryBuildTributePowerContext(payer, receiver, out AfTributePowerContext context)
				? context.CalculatedTribute
				: 0;
		}
		// An explicitly negotiated amount is authoritative. The prosperity-based
		// calculation is only for "auto" terms and must not silently rewrite a
		// concrete amount that the player and NPC already accepted.
		return int.TryParse(token, out int parsed) && parsed >= 0 ? parsed : -1;
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

	internal static DiplomacyPeaceEffectReceipt ApplyPeace(
        Kingdom payer, Kingdom receiver, int requestedDailyTribute, int requestedDurationDays, string source)
    {
        if (payer == null || receiver == null || payer == receiver || payer.IsEliminated || receiver.IsEliminated)
            return new(false, false, false, 0, 0, false, "王国目标无效");
        if (!FactionManager.IsAtWarAgainstFaction(payer, receiver))
            return new(true, false, false, 0, 0, false, "双方已不处于战争状态");
        if (requestedDailyTribute < 0)
            return new(true, false, false, 0, 0, false, "每日贡金不能为负数");
        int days = ResolveDurationDays(requestedDurationDays.ToString(), requestedDailyTribute > 0);
        string diagnostic = "";
        try
        {
            MeetingBattleRuntime.RunWithDiplomaticSideEffectsUnlocked(source ?? "diplomacy_make_peace", () =>
                MakePeaceAction.ApplyByKingdomDecision(payer, receiver, requestedDailyTribute, days));
        }
        catch (Exception ex) { diagnostic = ex.Message; }
        return ConfirmPeace(payer, receiver, requestedDailyTribute, days, source, diagnostic);
    }

    internal static DiplomacyPeaceEffectReceipt ConfirmPeace(Kingdom payer, Kingdom receiver,
        int requestedDailyTribute, int requestedDurationDays, string source, string diagnostic = "")
    {
        bool peace;
        try { peace = payer != null && receiver != null && !FactionManager.IsAtWarAgainstFaction(payer, receiver); }
        catch (Exception ex) { return new(false, false, false, 0, 0, false, diagnostic + " | peace readback: " + ex.Message); }
        if (!peace) return new(true, false, false, 0, 0, false, diagnostic + " 和平动作未生效");
        // Registration is an ancillary guard; its failure cannot erase confirmed peace.
        try { DiplomacyRecentPeaceBridge.RegisterPeace(payer, receiver, source ?? "diplomacy_make_peace"); }
        catch (Exception ex) { diagnostic += " | peace guard: " + ex.Message; }
        try
        {
            StanceLink stance = payer.GetStanceWith(receiver);
            int tribute = stance.GetDailyTributeToPay(payer);
            int days = stance.DailyTributeInstallments;
            int requestedDays = ResolveDurationDays(requestedDurationDays.ToString(), requestedDailyTribute > 0);
            return new(true, true, true, tribute, days,
                tribute == requestedDailyTribute && days == requestedDays, diagnostic);
        }
        catch (Exception ex) { return new(true, true, false, 0, 0, false, diagnostic + " | tribute readback: " + ex.Message); }
    }
}
