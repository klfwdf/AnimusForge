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
	internal const string DefaultPlayerDisplayName = "玩家";
	internal const string ResidentRecentActionsHeader = "【NPC近期行动（近10天，常驻）】";
	internal const string VanillaIssueRuntimeBlockPrefix = "【原版任务上下文";

	internal static string ResolvePlayerDisplayName(string displayName)
		=> string.IsNullOrWhiteSpace(displayName) ? DefaultPlayerDisplayName : displayName;

	internal static string BuildDuelResultLine(bool playerWon, string playerDisplayName)
	{
		string name = ResolvePlayerDisplayName(playerDisplayName);
		return playerWon
			? "【战斗结果】你刚刚在一场正式的决斗中输给了" + name + "。无论失败来自倒地、低血量、逃跑或撤退，这都已经按决斗失败结算；你可以不甘、恼怒或嘴硬，但不能否认自己输了。请认真考虑履行你在决斗前约定的赌注或补偿。"
			: "【战斗结果】你刚刚在一场正式的决斗中打败了" + name + "。你可以据此调整对" + name + "的态度，或提醒" + name + "履行之前约定的赌注。";
	}

	internal static string BuildVanillaBattleDefeatLine(string playerDisplayName)
	{
		string name = ResolvePlayerDisplayName(playerDisplayName);
		return "【原版战斗结果】你刚刚在一场战斗中被" + name + "击败了。你的军队溃败，你必须承认这个事实。根据你的性格，你可以表现得愤怒、不甘、恳求或傲慢，但不能否认战败的事实。";
	}

	internal static string BuildReleasedPrisonerLine(string playerDisplayName)
	{
		string name = ResolvePlayerDisplayName(playerDisplayName);
		return "【释放通知】你之前被" + name + "俘虏关押，现在刚刚获得了自由。你应该意识到自己曾经是囚犯这个事实，并根据你的性格做出适当反应（感激、愤恨、或不屑等）。";
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
		=> !string.IsNullOrEmpty(extras) && extras.IndexOf("【附加规则:" + ruleId + "】", StringComparison.OrdinalIgnoreCase) >= 0;
}
