using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.Library;

namespace AnimusForge;

internal static partial class VanillaIssueOfferBridge
{
	private static class IssueRuntimeStateOwner
	{
		internal static bool TryGetRuntimeState(Hero targetHero, out string stateKey, out IssueBase issue, out TurnInProbeResult probe)
		{
			stateKey = "";
			issue = null;
			probe = null;
			if (TryGetOfferableIssue(targetHero, out issue))
			{
				stateKey = "offer";
				return true;
			}
			if (TryGetReadyToTurnInIssue(targetHero, out issue, out probe))
			{
				stateKey = "ready_to_turn_in";
				return true;
			}
			if (TryGetInProgressIssue(targetHero, out issue))
			{
				stateKey = (issue.IsSolvingWithAlternative ? "in_progress_alternative" : "in_progress");
				return true;
			}
			return false;
		}

		internal static bool TryGetOfferableIssue(Hero targetHero, out IssueBase issue)
		{
			issue = targetHero?.Issue;
			return issue != null && issue.IssueOwner == targetHero && issue.IsOngoingWithoutQuest;
		}

		internal static bool TryGetInProgressIssue(Hero targetHero, out IssueBase issue)
		{
			issue = targetHero?.Issue;
			return issue != null && issue.IssueOwner == targetHero && issue.IssueQuest != null && issue.IssueQuest.IsOngoing;
		}

		internal static bool TryGetReadyToTurnInIssue(Hero targetHero, out IssueBase issue, out TurnInProbeResult probe)
		{
			issue = null;
			probe = null;
			if (!TryGetInProgressIssue(targetHero, out issue))
			{
				return false;
			}
			if (issue.IsSolvingWithAlternative)
			{
				return false;
			}
			string text = "";
			bool flag = TryGetExplicitTurnInSignal(issue.IssueQuest, out text);
			if (TryProbeQuestTurnIn(targetHero, issue, execute: false, out probe, out _))
			{
				probe.ExplicitCompletionSummary = text;
				return true;
			}
			if (flag)
			{
				probe = new TurnInProbeResult
				{
					Issue = issue,
					Quest = issue.IssueQuest,
					ExplicitCompletionSummary = text,
					IntroText = text,
					IsConfident = false
				};
				return true;
			}
			return false;
		}

		private static bool TryGetExplicitTurnInSignal(QuestBase quest, out string summary)
		{
			summary = "";
			MBReadOnlyList<JournalLog> journalEntries = quest?.JournalEntries;
			if (journalEntries == null || journalEntries.Count == 0)
			{
				return false;
			}
			for (int num = journalEntries.Count - 1; num >= 0; num--)
			{
				JournalLog journalLog = journalEntries[num];
				if (journalLog == null)
				{
					continue;
				}
				string text = NormalizePromptText(GetText(journalLog.TaskName));
				string text2 = NormalizePromptText(GetText(journalLog.LogText));
				if (journalLog.Range > 0 && journalLog.CurrentProgress >= journalLog.Range)
				{
					string arg = string.IsNullOrWhiteSpace(text) ? text2 : text;
					summary = string.IsNullOrWhiteSpace(arg) ? (IssuePromptTexts.Get("AF_ISSUE_PROMPT_Progress_0") + journalLog.CurrentProgress + "/" + journalLog.Range) : (arg + IssuePromptTexts.Get("AF_ISSUE_PROMPT_Progress_1") + journalLog.CurrentProgress + "/" + journalLog.Range + IssuePromptTexts.Get("AF_ISSUE_PROMPT_Progress_3"));
					return true;
				}
				string text3 = (text + " " + text2).Trim().ToLowerInvariant();
				if (ContainsAny(text3, "you have enough", "return back to", "return to", "go back to", "report back", "speak to", IssueDisplayMatchCatalog.Get("Raw_049"), IssueDisplayMatchCatalog.Get("Raw_050"), IssueDisplayMatchCatalog.Get("Raw_051"), IssueDisplayMatchCatalog.Get("Raw_052"), IssueDisplayMatchCatalog.Get("Raw_053"), IssueDisplayMatchCatalog.Get("Raw_054")))
				{
					summary = string.IsNullOrWhiteSpace(text2) ? text : text2;
					return true;
				}
			}
			return false;
		}

	}
}
