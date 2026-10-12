using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal static class IssueCompletionReceiptOwner
{
	internal static bool TryBuildCompletionReceipt(QuestBase quest, QuestBase.QuestCompleteDetails detail, out Hero giver, out string memoryFact)
	{
		giver = null;
		memoryFact = "";
		// DebtPromiseQuest is an AnimusForge ledger entry and must never be described to the LLM as a vanilla issue.
		if (quest == null || quest is DebtPromiseQuest)
		{
			return false;
		}
		giver = quest.QuestGiver;
		if (giver == null || string.IsNullOrWhiteSpace(giver.StringId))
		{
			return false;
		}
		List<string> recentJournalEntries = new List<string>();
		try
		{
			foreach (JournalLog item in quest.JournalEntries)
			{
				string text = (item?.LogText?.ToString() ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
				if (!string.IsNullOrWhiteSpace(text))
				{
					recentJournalEntries.Add(text);
				}
			}
			if (recentJournalEntries.Count > 3)
			{
				recentJournalEntries = recentJournalEntries.GetRange(Math.Max(0, recentJournalEntries.Count - 3), Math.Min(3, recentJournalEntries.Count));
			}
		}
		catch
		{
		}
		string lastJournal = (recentJournalEntries.Count > 0 ? (recentJournalEntries[recentJournalEntries.Count - 1] ?? "").Trim() : "");
		memoryFact = BuildMemoryFact((quest.Title?.ToString() ?? "").Trim(), TranslateCompletionDetail(detail), quest.RewardGold, lastJournal);
		return true;
	}

	internal static string BuildMemoryFact(string questTitleText, string detailText, int rewardGold, string lastJournal)
	{
		if (string.IsNullOrWhiteSpace(questTitleText))
		{
			questTitleText = IssueDisplayMatchCatalog.Get("Raw_033");
		}
		string memoryFact = IssueDisplayMatchCatalog.Get("Raw_034") + questTitleText + IssueDisplayMatchCatalog.Get("Raw_035");
		if (!string.IsNullOrWhiteSpace(detailText))
		{
			memoryFact = memoryFact + IssueDisplayMatchCatalog.Get("Raw_036") + detailText + "。";
		}
		if (rewardGold > 0)
		{
			memoryFact = memoryFact + IssueDisplayMatchCatalog.Get("Raw_037") + rewardGold + IssueDisplayMatchCatalog.Get("Raw_038");
		}
		else
		{
			memoryFact = memoryFact + IssueDisplayMatchCatalog.Get("Raw_039");
		}
		if (!string.IsNullOrWhiteSpace(lastJournal))
		{
			memoryFact = memoryFact + IssueDisplayMatchCatalog.Get("Raw_040") + lastJournal;
		}
		return memoryFact;
	}

	private static string TranslateCompletionDetail(QuestBase.QuestCompleteDetails detail)
	{
		switch (detail)
		{
		case QuestBase.QuestCompleteDetails.Success:
			return IssueDisplayMatchCatalog.Get("Raw_041");
		case QuestBase.QuestCompleteDetails.Fail:
			return IssueDisplayMatchCatalog.Get("Raw_042");
		case QuestBase.QuestCompleteDetails.FailWithBetrayal:
			return IssueDisplayMatchCatalog.Get("Raw_043");
		case QuestBase.QuestCompleteDetails.Timeout:
			return IssueDisplayMatchCatalog.Get("Raw_044");
		case QuestBase.QuestCompleteDetails.Cancel:
			return IssueDisplayMatchCatalog.Get("Raw_045");
		default:
			return detail.ToString();
		}
	}
}
