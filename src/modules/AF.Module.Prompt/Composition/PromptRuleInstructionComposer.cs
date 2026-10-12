using System;
using System.Collections.Generic;
using System.Text;

namespace AnimusForge;

/// <summary>
/// Host-captured bodies for the triggered rule instruction block. Each field is the already
/// resolved runtime text for that topic (null/blank = topic not applicable); no game objects.
/// </summary>
internal sealed class PromptRuleInstructionSections
{
	internal bool UseDuelContext;
	internal bool IsQualified;
	internal int PlayerTier;
	internal string DuelInstruction;
	internal string PlayerDisplayName;
	internal bool UseRewardContext;
	internal string RewardInstruction;
	internal bool IncludeDuelStake;
	internal string DuelStakeInstruction;
	internal bool IsLoanContext;
	internal string LoanInstruction;
	internal bool IsSurroundingsContext;
	internal string SurroundingsInstruction;
	/// <summary>Matched extra rules text (semantic retrieval or preselected ids), already post-processed by the host.</summary>
	internal string ExtraRuleInstructions;
	internal bool WorldMapPartyCommandContext;
	internal string WorldMapInstruction;
	internal bool PartyTransferEligible;
	internal bool AllowMeetingTaunt;
	internal string MeetingTauntInstruction;
	internal string MeetingTauntMarker;
	internal HashSet<string> ExcludedRuleIds;
}

/// <summary>
/// Pure assembly of the "【附加规则:*】" block sequence in legacy order: duel → reward(+duel_stake)
/// → loan → surroundings → worldmap (if not already in extras) → party-transfer-promoted reward/loan
/// → matched extra rules → meeting_taunt. Reward/loan resolution chains are owned here once.
/// </summary>
internal static class PromptRuleInstructionComposer
{
	internal static string BuildUnqualifiedDuelBody(string playerDisplayName, int playerTier)
	{
		return PromptComposerTexts.Format("rule_unqualified_duel", ("NAME", PromptExtrasComposer.ResolvePlayerDisplayName(playerDisplayName)), ("TIER", playerTier));
	}

	/// <summary>Legacy reward fallback chain: merchant(+runtime) → hero runtime / non-hero default → global default.</summary>
	internal static string ResolveRewardInstruction(bool hasAnyHero, string merchantInstruction, string runtimeInstructionForMerchant, string runtimeInstruction, string nonHeroInstruction, string defaultInstruction)
	{
		string text = "";
		if (!hasAnyHero && !string.IsNullOrWhiteSpace(merchantInstruction))
		{
			text = string.IsNullOrWhiteSpace(runtimeInstructionForMerchant) ? merchantInstruction : (runtimeInstructionForMerchant.Trim() + "\n" + merchantInstruction.Trim());
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = hasAnyHero ? runtimeInstruction : nonHeroInstruction;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = defaultInstruction;
		}
		return text;
	}

	/// <summary>Legacy loan fallback chain: hero or merchant-kind → runtime, else non-hero default; then global default.</summary>
	internal static string ResolveLoanInstruction(bool hasAnyHero, bool isSettlementMerchant, string runtimeInstruction, string nonHeroInstruction, string defaultInstruction)
	{
		string text = (hasAnyHero || isSettlementMerchant) ? runtimeInstruction : nonHeroInstruction;
		return string.IsNullOrWhiteSpace(text) ? defaultInstruction : text;
	}

	internal static string Compose(PromptRuleInstructionSections s)
	{
		if (s == null)
		{
			return "";
		}
		HashSet<string> excluded = s.ExcludedRuleIds;
		StringBuilder sb = new StringBuilder();
		if (s.UseDuelContext && !PromptRuleIdPolicy.IsExcluded(excluded, "duel"))
		{
			if (s.IsQualified)
			{
				if (!string.IsNullOrWhiteSpace(s.DuelInstruction))
				{
					PromptRuleBlockText.Append(sb, "duel", s.DuelInstruction);
				}
			}
			else
			{
				PromptRuleBlockText.Append(sb, "duel", BuildUnqualifiedDuelBody(s.PlayerDisplayName, s.PlayerTier));
			}
		}
		if (s.UseRewardContext && !PromptRuleIdPolicy.IsExcluded(excluded, "reward"))
		{
			PromptRuleBlockText.Append(sb, "reward", s.RewardInstruction);
			if (s.IncludeDuelStake)
			{
				PromptRuleBlockText.Append(sb, "duel_stake", s.DuelStakeInstruction);
			}
		}
		if (s.IsLoanContext && !PromptRuleIdPolicy.IsExcluded(excluded, "loan"))
		{
			PromptRuleBlockText.Append(sb, "loan", s.LoanInstruction);
		}
		if (s.IsSurroundingsContext && !PromptRuleIdPolicy.IsExcluded(excluded, "surroundings"))
		{
			PromptRuleBlockText.Append(sb, "surroundings", s.SurroundingsInstruction);
		}
		string extra = s.ExtraRuleInstructions;
		if (s.WorldMapPartyCommandContext && !PromptRuleIdPolicy.IsExcluded(excluded, "worldmap_party_command")
			&& !PromptRuleBlockText.Has(extra, "worldmap_party_command") && !PromptRuleBlockText.Has(sb.ToString(), "worldmap_party_command"))
		{
			PromptRuleBlockText.Append(sb, "worldmap_party_command", s.WorldMapInstruction);
		}
		if (s.PartyTransferEligible && PromptRuleBlockText.Has(extra, "party_transfer"))
		{
			string injected = sb.ToString();
			if (!PromptRuleBlockText.Has(injected, "reward") && !PromptRuleIdPolicy.IsExcluded(excluded, "reward") && !string.IsNullOrWhiteSpace(s.RewardInstruction))
			{
				PromptRuleBlockText.Append(sb, "reward", s.RewardInstruction);
			}
			if (!PromptRuleBlockText.Has(injected, "loan") && !PromptRuleIdPolicy.IsExcluded(excluded, "loan") && !string.IsNullOrWhiteSpace(s.LoanInstruction))
			{
				PromptRuleBlockText.Append(sb, "loan", s.LoanInstruction);
			}
		}
		if (!string.IsNullOrWhiteSpace(extra))
		{
			sb.AppendLine(extra.Trim());
		}
		if (s.AllowMeetingTaunt && !string.IsNullOrWhiteSpace(s.MeetingTauntInstruction)
			&& (string.IsNullOrEmpty(s.MeetingTauntMarker) || sb.ToString().IndexOf(s.MeetingTauntMarker, StringComparison.OrdinalIgnoreCase) < 0))
		{
			PromptRuleBlockText.Append(sb, "meeting_taunt", s.MeetingTauntInstruction);
		}
		return sb.ToString().Trim();
	}
}
