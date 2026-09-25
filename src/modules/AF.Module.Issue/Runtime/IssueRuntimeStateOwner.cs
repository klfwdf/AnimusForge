using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Issues;

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

	}
}
