using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal static partial class VanillaIssueOfferBridge
{
	private static class IssueRuntimePromptOwner
	{
		internal static List<PostprocessRuleEntry> BuildRuntimePostprocessRules(Hero targetHero)
		{
			List<PostprocessRuleEntry> list = new List<PostprocessRuleEntry>();
			try
			{
				if (!IssueRuntimeStateOwner.TryGetRuntimeState(targetHero, out var stateKey, out var _, out var _))
				{
					return list;
				}
				List<PostprocessRuleEntry> guardrailRulePostprocessRules = AIConfigHandler.GetGuardrailRulePostprocessRules("vanilla_issue") ?? new List<PostprocessRuleEntry>();
				foreach (PostprocessRuleEntry item in guardrailRulePostprocessRules)
				{
					string text = (item?.Tag ?? "").Trim();
					if (string.IsNullOrWhiteSpace(text))
					{
						continue;
					}
					bool flag = false;
					switch ((stateKey ?? "").Trim().ToLowerInvariant())
					{
					case "offer":
						flag = text.Equals("[ACTION:ISSUE_ACCEPT_SELF]", StringComparison.OrdinalIgnoreCase) || text.StartsWith("[ACTION:ISSUE_ACCEPT_ALT:COMPANION=", StringComparison.OrdinalIgnoreCase);
						break;
					case "ready_to_turn_in":
						flag = text.Equals("[ACTION:QUEST_TURN_IN]", StringComparison.OrdinalIgnoreCase);
						break;
					}
					if (flag)
					{
						list.Add(new PostprocessRuleEntry
						{
							Tag = item.Tag,
							Description = item.Description
						});
					}
				}
			}
			catch
			{
			}
			return list;
		}

		internal static bool TryBuildRuntimePromptBlock(Hero targetHero, out string stateKey, out string promptText)
		{
			promptText = "";
			if (!IssueRuntimeStateOwner.TryGetRuntimeState(targetHero, out stateKey, out var issue, out var probe))
			{
				return false;
			}
			switch (stateKey)
			{
			case "offer":
				promptText = BuildOfferPromptBlock(targetHero, issue);
				break;
			case "ready_to_turn_in":
				promptText = BuildReadyToTurnInPromptBlock(targetHero, issue, probe);
				break;
			case "in_progress":
			case "in_progress_alternative":
				promptText = BuildInProgressPromptBlock(targetHero, issue);
				break;
			}
			return !string.IsNullOrWhiteSpace(promptText);
		}

		private static string BuildOfferPromptBlock(Hero targetHero, IssueBase issue)
		{
			string text = NormalizePromptText(GetText(issue.Title));
			string text2 = NormalizePromptText(GetText(issue.Description));
			string text3 = NormalizePromptText(GetText(issue.IssueBriefByIssueGiver));
			string text4 = NormalizePromptText(GetText(issue.IssueQuestSolutionExplanationByIssueGiver));
			bool flag = TryCheckQuestPreconditions(issue, targetHero, out var text5);
			bool flag2 = issue.IsThereAlternativeSolution;
			List<CompanionCandidate> list = new List<CompanionCandidate>();
			bool flag3 = flag && flag2 && IssueActionOwner.TryBuildAlternativeCandidates(issue, out list, out _);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_OfferHeader", "[Vanilla quest context (for understanding only; do not copy verbatim)]"));
			stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_OfferOpening", "You currently have a vanilla quest you can offer to the player."));
			if (!string.IsNullOrWhiteSpace(text))
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_OfferTitle", "Quest title: {TITLE}", ("TITLE", text)));
			}
			if (!string.IsNullOrWhiteSpace(text2))
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_OfferSummary", "Quest summary: {SUMMARY}", ("SUMMARY", text2)));
			}
			if (!string.IsNullOrWhiteSpace(text3))
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_OfferBrief", "Your vanilla explanation to the player: {BRIEF}", ("BRIEF", text3)));
			}
			if (!string.IsNullOrWhiteSpace(text4))
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_OfferPersonalSolution", "Vanilla explanation of the player's personal solution: {DETAIL}", ("DETAIL", text4)));
			}
			int issueRewardGold = GetIssueRewardGold(issue);
			if (issueRewardGold > 0)
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_OfferBaseReward", "Vanilla base reward reference: {GOLD} denars. This is reference information only; you do not need to quote the exact amount.", ("GOLD", issueRewardGold)));
			}
			if (flag)
			{
				stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_OfferEligible", "The player currently meets the acceptance conditions. If the player clearly agrees to take this quest, give only a natural verbal confirmation or acceptance in the body; do not output any action tag. Whether the quest is actually accepted is determined by post-processing from your response."));
			}
			else
			{
				stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_OfferReject", "The player does not meet the vanilla prerequisites for accepting this quest; refuse firmly."));
				if (!string.IsNullOrWhiteSpace(text5))
				{
					stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_OfferReason", "Vanilla reason this quest cannot currently be offered: {REASON}", ("REASON", IssueDisplayTexts.FailureReasonForPrompt(text5))));
				}
			}
			if (flag2 && flag3)
			{
				stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_OfferAlternativeAvailable", "This quest also supports being handled by one of the player's companions."));
				stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_OfferAlternativeConsent", "If the player explicitly asks for a companion to handle it and clearly names one of the candidates below, give only a natural verbal agreement in the body to that companion taking the assignment; post-processing decides whether it is actually assigned to them."));
				stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_OfferAskCompanion", "If the player only says to send a companion without naming anyone, ask a follow-up question first; do not treat the assignment as decided."));
				stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_OfferCandidateList", "You may choose only from the following candidates; never invent a companion:"));
				foreach (CompanionCandidate item in list)
				{
					stringBuilder.AppendLine(IssueDisplayTexts.AlternativeCandidatePromptLineForPrompt(item.PromptLine));
				}
				stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_OfferTroopDispatchInfo", "If post-processing confirms a companion assignment, the native troop-dispatch screen will open next to choose accompanying soldiers. In your response, decide only whether to accept and who will lead."));
			}
			return stringBuilder.ToString().Trim();
		}

		private static string BuildInProgressPromptBlock(Hero targetHero, IssueBase issue)
		{
			QuestBase issueQuest = issue?.IssueQuest;
			if (targetHero == null || issue == null || issueQuest == null || !issueQuest.IsOngoing)
			{
				return "";
			}
			string safeIssueTitle = IssueDisplayTexts.IssueTitleForPrompt(GetSafeIssueTitle(issue));
			string text = NormalizePromptText(GetText(issueQuest.Title));
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_InProgressHeader", "[Vanilla quest context: in progress]"));
			stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_InProgressInstruction", "You have already assigned a vanilla quest to the player. Do not offer it again as if for the first time, and do not output any quest-acceptance tag."));
			if (!string.IsNullOrWhiteSpace(safeIssueTitle))
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_SharedQuestTitle", "Quest title: {TITLE}", ("TITLE", safeIssueTitle)));
			}
			if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, safeIssueTitle, StringComparison.Ordinal))
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_InProgressQuestName", "Vanilla quest name: {TITLE}", ("TITLE", text)));
			}
			string currentState = issue.IsSolvingWithAlternative
				? IssuePromptTexts.Text("AF_ISSUE_PROMPT_InProgressStateAlternative", "The player delegated the quest to a companion. If the player claims the quest is complete, they are lying.")
				: IssuePromptTexts.Text("AF_ISSUE_PROMPT_InProgressStateSelf", "The player is carrying out the quest personally; it is not complete yet. If the player claims it is complete, they are lying.");
			stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_InProgressCurrentState", "Current state: {STATE}", ("STATE", currentState)));
			if (issue.IsSolvingWithAlternative)
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_InProgressCompanion", "Current companion leading the mission: {HERO}", ("HERO", IssueDisplayTexts.CompanionNameForDisplay(GetHeroName(issue.AlternativeSolutionHero)))));
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_InProgressDuration", "Vanilla estimated total duration: {DAYS} days.", ("DAYS", Math.Max(1, issue.GetTotalAlternativeSolutionDurationInDays()))));
			}
			try
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_InProgressDeadline", "Vanilla quest deadline: {DATE}", ("DATE", issueQuest.QuestDueTime.ToString())));
			}
			catch
			{
			}
			AppendRecentJournalLinesForPrompt(stringBuilder, issueQuest.JournalEntries);
			stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_InProgressDiscussion", "Respond to the player's words about progress, remind them of requirements, and say whether the task appears close to completion; do not reintroduce the question of accepting it."));
			stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_NoActionTags", "Never output [ACTION:ISSUE_ACCEPT_SELF], [ACTION:ISSUE_ACCEPT_ALT:*], or [ACTION:QUEST_TURN_IN] in the body; post-processing alone decides whether to trigger them."));
			return stringBuilder.ToString().Trim();
		}

		private static string BuildReadyToTurnInPromptBlock(Hero targetHero, IssueBase issue, TurnInProbeResult probe)
		{
			QuestBase issueQuest = issue?.IssueQuest;
			if (targetHero == null || issue == null || issueQuest == null || probe == null)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_TurnInHeader", "[Vanilla quest context: ready to turn in]"));
			stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_TurnInIntro", "The vanilla system has confirmed a turn-in path is currently available. Do not issue the quest again; focus on checking and confirming results and discussing completion."));
			string safeIssueTitle = IssueDisplayTexts.IssueTitleForPrompt(GetSafeIssueTitle(issue));
			if (!string.IsNullOrWhiteSpace(safeIssueTitle))
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_SharedQuestTitle", "Quest title: {TITLE}", ("TITLE", safeIssueTitle)));
			}
			string text = NormalizePromptText(GetText(issueQuest.Title));
			if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, safeIssueTitle, StringComparison.Ordinal))
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_InProgressQuestName", "Vanilla quest name: {TITLE}", ("TITLE", text)));
			}
			if (!string.IsNullOrWhiteSpace(probe.IntroText))
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_TurnInDiscussIntro", "Opening meaning from the current vanilla discuss flow: {TEXT}", ("TEXT", probe.IntroText)));
			}
			if (!string.IsNullOrWhiteSpace(probe.ExplicitCompletionSummary))
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_TurnInCompletionSignal", "Explicit completion signal detected by the system: {TEXT}", ("TEXT", probe.ExplicitCompletionSummary)));
			}
			AppendRecentJournalLinesForPrompt(stringBuilder, issueQuest.JournalEntries);
			if (probe.VisibleOptions != null && probe.VisibleOptions.Count > 0)
			{
				stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_TurnInVisibleOptions", "Summary of currently visible vanilla player options:"));
				foreach (string visibleOption in probe.VisibleOptions)
				{
					stringBuilder.AppendLine("- " + visibleOption);
				}
			}
			stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_TurnInReady", "The player has met the quest's completion conditions. If they say they are turning it in, confirm it is done, or come for inspection, naturally confirm completion or point out that it is still incomplete; do not output any action tag. Post-processing decides whether it is actually turned in."));
			stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_TurnInUnclear", "If the player is only asking for progress, has not finished, or is unclear, respond normally and do not state that the quest has been inspected and completed."));
			stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_NoActionTags", "Never output [ACTION:ISSUE_ACCEPT_SELF], [ACTION:ISSUE_ACCEPT_ALT:*], or [ACTION:QUEST_TURN_IN] in the body; post-processing alone decides whether to trigger them."));
			return stringBuilder.ToString().Trim();
		}

		private static string BuildRecentCompletionPromptBlock(Hero targetHero)
		{
			if (targetHero == null)
			{
				return "";
			}
			if (VanillaIssuePromptBehavior.Instance == null || !VanillaIssuePromptBehavior.Instance.TryGetRecentCompletionRecord(targetHero, out var questTitle, out var completionDetail, out var rewardGold, out var recentJournalEntries, consumeOnRead: true))
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_RecentHeader", "[Vanilla quest context: just completed]"));
			stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_RecentInstruction", "The player has just completed a vanilla quest you assigned. Do not issue it again or output any quest-acceptance tag."));
			if (!string.IsNullOrWhiteSpace(questTitle))
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_RecentQuest", "Recently completed quest: {TITLE}", ("TITLE", IssueDisplayTexts.IssueTitleForPrompt(NormalizePromptText(questTitle)))));
			}
			if (!string.IsNullOrWhiteSpace(completionDetail))
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_RecentResult", "Vanilla completion result: {DETAIL}", ("DETAIL", completionDetail)));
			}
			if (rewardGold > 0)
			{
				stringBuilder.AppendLine(IssuePromptTexts.Format("AF_ISSUE_PROMPT_RecentReward", "Vanilla reward reference: {GOLD} denars.", ("GOLD", rewardGold)));
			}
			if (recentJournalEntries != null && recentJournalEntries.Count > 0)
			{
				stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_RecentJournal", "Recent vanilla quest records:"));
				foreach (string recentJournalEntry in recentJournalEntries)
				{
					stringBuilder.AppendLine("- " + NormalizePromptText(recentJournalEntry));
				}
			}
			stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_RecentScope", "This recently completed fact applies only to the next conversation and means the old quest has already been resolved."));
			stringBuilder.AppendLine(IssuePromptTexts.Text("AF_ISSUE_PROMPT_RecentOtherQuests", "If you have another vanilla quest to discuss now, you may continue with a new one; do not treat this completed old quest as unfinished or assign the same old quest again."));
			return stringBuilder.ToString().Trim();
		}

		internal static string BuildNoAvailableIssuePromptBlock(Hero targetHero)
		{
			if (targetHero == null)
			{
				return "";
			}
			string text = targetHero.Name?.ToString();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = IssuePromptTexts.Text("AF_ISSUE_PROMPT_GiverFallback", "you");
			}
			return IssuePromptTexts.Format("AF_ISSUE_PROMPT_NoAvailable", "[Vanilla quest context: no available quest] {GIVER} currently has no vanilla quest to offer.", ("GIVER", text));
		}

		private static void AppendRecentJournalLinesForPrompt(StringBuilder target, MBReadOnlyList<JournalLog> journalEntries)
		{
			if (target == null || journalEntries == null || journalEntries.Count == 0) return;
			StringBuilder section = new StringBuilder();
			AppendRecentJournalLines(section, journalEntries);
			if (section.Length > 0)
				target.Append(IssueDisplayTexts.RecentJournalSectionForPrompt(section.ToString()));
		}
	}
}
