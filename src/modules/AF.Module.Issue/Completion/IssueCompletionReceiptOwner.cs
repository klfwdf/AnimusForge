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
			questTitleText = "一项原版任务";
		}
		string memoryFact = "你之前交给玩家的原版任务“" + questTitleText + "”已经有结果了。";
		if (!string.IsNullOrWhiteSpace(detailText))
		{
			memoryFact = memoryFact + " 结果：" + detailText + "。";
		}
		if (rewardGold > 0)
		{
			memoryFact = memoryFact + " 该任务的原版奖励 " + rewardGold + " 第纳尔已经由系统自动发放，无需你手动再次支付。";
		}
		else
		{
			memoryFact = memoryFact + " 若有原版任务奖励，也已由系统按原版流程自动结算，无需你手动再次发放。";
		}
		if (!string.IsNullOrWhiteSpace(lastJournal))
		{
			memoryFact = memoryFact + " 最近任务记录：" + lastJournal;
		}
		return memoryFact;
	}

	private static string TranslateCompletionDetail(QuestBase.QuestCompleteDetails detail)
	{
		switch (detail)
		{
		case QuestBase.QuestCompleteDetails.Success:
			return "任务已成功完成";
		case QuestBase.QuestCompleteDetails.Fail:
			return "任务已失败";
		case QuestBase.QuestCompleteDetails.FailWithBetrayal:
			return "任务以背叛结局失败";
		case QuestBase.QuestCompleteDetails.Timeout:
			return "任务已超时结束";
		case QuestBase.QuestCompleteDetails.Cancel:
			return "任务已取消";
		default:
			return detail.ToString();
		}
	}
}
