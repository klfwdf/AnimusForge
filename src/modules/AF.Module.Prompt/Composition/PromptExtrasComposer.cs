using AnimusForge;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnimusForge;

/// <summary>
/// Host-captured section texts for the shared main-chain Extras block, in canonical order.
/// Every field is a finished string (or null/empty to skip); no game objects live here so
/// the composition step can run off the game thread once the host has captured the sections.
/// </summary>
internal sealed class PromptExtrasSections
{
	internal string LoanDueDateReference;
	internal string LoanDebtHint;
	internal string TrustPrompt;
	internal string SettlementMerchantDebtHint;
	internal string DuelResultLine;
	internal string VanillaBattleDefeatLine;
	internal string ReleasedPrisonerLine;
	internal string ActivePrisonerStatusLine;
	internal string FeastAttendanceContext;
	internal string ClarificationHint;
	internal string HeroArmyRuntimeFact;
	internal string PlayerArmyRuntimeFact;
	internal string ResidentRecentActions;
	internal string CurrentFamilyStatus;
	internal string NearbySettlementsDetail;
	internal string WeeklyShortReports;
	internal string ActivePolicyContext;
	internal string TriggeredRuleInstructions;
	internal string WeeklyFullReports;
	internal string LoreContext;
	internal string EntityMainPromptBlock;
	internal string AgendaMainPromptBlock;
}

/// <summary>Injected-block markers detected in the final Extras text (diagnostics only).</summary>
internal struct PromptExtrasMarkers
{
	internal bool Duel, Reward, Loan, WorldMap, NpcMajor, ResidentRecentActions, VanillaIssue, VanillaIssueRuntimeBlock;
}

/// <summary>
/// Composes the shared Extras block: fixed relationship templates, canonical section order,
/// entity retrieval rule-id set and marker detection. Pure; one call per prompt build.
/// </summary>
internal static class PromptExtrasComposer
{
	internal static string DefaultPlayerDisplayName => PromptComposerTexts.Get("extras_player");
	internal static string ResidentRecentActionsHeader => PromptComposerTexts.Raw("ResidentRecentActionsHeader");
	internal static string VanillaIssueRuntimeBlockPrefix => PromptComposerTexts.Raw("VanillaIssueRuntimeBlockPrefix");

	internal static string ResolvePlayerDisplayName(string displayName)
		=> string.IsNullOrWhiteSpace(displayName) ? DefaultPlayerDisplayName : displayName;

	internal static string BuildDuelResultLine(bool playerWon, string playerDisplayName)
	{
		string name = ResolvePlayerDisplayName(playerDisplayName);
		return PromptComposerTexts.Format(playerWon ? "extras_duel_lost" : "extras_duel_won", ("NAME", name));
	}

	internal static string BuildVanillaBattleDefeatLine(string playerDisplayName)
	{
		string name = ResolvePlayerDisplayName(playerDisplayName);
		return PromptComposerTexts.Format("extras_battle_defeat", ("NAME", name));
	}

	internal static string BuildReleasedPrisonerLine(string playerDisplayName)
	{
		string name = ResolvePlayerDisplayName(playerDisplayName);
		return PromptComposerTexts.Format("extras_prisoner_released", ("NAME", name));
	}

	/// <summary>Canonical order; blank sections are skipped, each non-blank section ends with a newline.</summary>
	internal static string Compose(PromptExtrasSections sections)
	{
		StringBuilder sb = new StringBuilder();
		if (sections == null)
		{
			return string.Empty;
		}
		Append(sb, sections.LoanDueDateReference, allowWhitespace: true);
		Append(sb, sections.LoanDebtHint, allowWhitespace: true);
		Append(sb, sections.TrustPrompt, allowWhitespace: true);
		Append(sb, sections.SettlementMerchantDebtHint);
		Append(sb, sections.DuelResultLine);
		Append(sb, sections.VanillaBattleDefeatLine);
		Append(sb, sections.ReleasedPrisonerLine);
		Append(sb, sections.ActivePrisonerStatusLine);
		Append(sb, sections.FeastAttendanceContext);
		Append(sb, sections.ClarificationHint);
		Append(sb, sections.HeroArmyRuntimeFact);
		Append(sb, sections.PlayerArmyRuntimeFact);
		Append(sb, sections.ResidentRecentActions);
		Append(sb, sections.CurrentFamilyStatus);
		Append(sb, sections.NearbySettlementsDetail);
		Append(sb, sections.WeeklyShortReports);
		Append(sb, sections.ActivePolicyContext);
		Append(sb, sections.TriggeredRuleInstructions);
		Append(sb, sections.WeeklyFullReports);
		Append(sb, sections.LoreContext, allowWhitespace: true);
		Append(sb, sections.EntityMainPromptBlock);
		Append(sb, sections.AgendaMainPromptBlock);
		return sb.ToString();
	}

	// Legacy used IsNullOrEmpty for a few Reward/lore sections and IsNullOrWhiteSpace elsewhere;
	// both are preserved so whitespace-only provider output composes exactly as before.
	private static void Append(StringBuilder sb, string section, bool allowWhitespace = false)
	{
		bool skip = allowWhitespace ? string.IsNullOrEmpty(section) : string.IsNullOrWhiteSpace(section);
		if (!skip)
		{
			sb.AppendLine(section);
		}
	}

	internal static string MergePostprocessBlock(string existing, string addition)
	{
		if (string.IsNullOrWhiteSpace(addition))
		{
			return existing ?? "";
		}
		return string.IsNullOrWhiteSpace(existing) ? addition : (existing.TrimEnd() + "\n" + addition);
	}

	internal static HashSet<string> BuildEntityRetrievalRuleIds(IEnumerable<string> auxiliaryRuleHitIds, bool useRewardContext, bool isLoanContext, bool partyTransferHit, bool worldMapPartyCommandHit)
	{
		HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (auxiliaryRuleHitIds != null)
		{
			foreach (string ruleId in auxiliaryRuleHitIds)
			{
				if (!string.IsNullOrWhiteSpace(ruleId))
				{
					ids.Add(ruleId.Trim());
				}
			}
		}
		if (useRewardContext) ids.Add("reward");
		if (isLoanContext) ids.Add("loan");
		if (partyTransferHit) ids.Add("party_transfer");
		if (worldMapPartyCommandHit) ids.Add("worldmap_party_command");
		return ids;
	}

	internal static PromptExtrasMarkers DetectMarkers(string extras)
	{
		PromptExtrasMarkers markers = default(PromptExtrasMarkers);
		if (string.IsNullOrEmpty(extras))
		{
			return markers;
		}
		markers.Duel = HasRuleBlock(extras, "duel");
		markers.Reward = HasRuleBlock(extras, "reward");
		markers.Loan = HasRuleBlock(extras, "loan");
		markers.WorldMap = HasRuleBlock(extras, "worldmap_party_command");
		markers.NpcMajor = HasRuleBlock(extras, "npc_major_actions");
		markers.VanillaIssue = HasRuleBlock(extras, "vanilla_issue");
		markers.ResidentRecentActions = extras.IndexOf(ResidentRecentActionsHeader, StringComparison.Ordinal) >= 0;
		markers.VanillaIssueRuntimeBlock = extras.IndexOf(VanillaIssueRuntimeBlockPrefix, StringComparison.OrdinalIgnoreCase) >= 0;
		return markers;
	}

	internal static bool HasRuleBlock(string extras, string ruleId)
		=> PromptRuleBlockText.Has(extras, ruleId);
}
