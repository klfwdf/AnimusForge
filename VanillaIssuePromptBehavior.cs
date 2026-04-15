using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public class VanillaIssuePromptBehavior : CampaignBehaviorBase
{
	public static VanillaIssuePromptBehavior Instance { get; private set; }

	public VanillaIssuePromptBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
		CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, OnNewCompanionAdded);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public bool TryGetRecentCompletionRecord(Hero giver, out string questTitle, out string completionDetail, out int rewardGold, out List<string> recentJournalEntries, bool consumeOnRead = false)
	{
		questTitle = "";
		completionDetail = "";
		rewardGold = 0;
		recentJournalEntries = null;
		return false;
	}

	private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
		try
		{
			Hero questGiver = quest?.QuestGiver;
			if (questGiver == null || string.IsNullOrWhiteSpace(questGiver.StringId))
			{
				return;
			}
			string questTitleText = (quest.Title?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(questTitleText))
			{
				questTitleText = "一项原版任务";
			}
			string detailText = TranslateCompletionDetail(detail);
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
			string memoryFact = "你之前交给玩家的原版任务“" + questTitleText + "”已经有结果了。";
			if (!string.IsNullOrWhiteSpace(detailText))
			{
				memoryFact = memoryFact + " 结果：" + detailText + "。";
			}
			if (quest.RewardGold > 0)
			{
				memoryFact = memoryFact + " 该任务的原版奖励 " + quest.RewardGold + " 第纳尔已经由系统自动发放，无需你手动再次支付。";
			}
			else
			{
				memoryFact = memoryFact + " 若有原版任务奖励，也已由系统按原版流程自动结算，无需你手动再次发放。";
			}
			string lastJournal = (recentJournalEntries.Count > 0 ? (recentJournalEntries[recentJournalEntries.Count - 1] ?? "").Trim() : "");
			if (!string.IsNullOrWhiteSpace(lastJournal))
			{
				memoryFact = memoryFact + " 最近任务记录：" + lastJournal;
			}
			MyBehavior.AppendExternalPlayerFact(questGiver, memoryFact);
		}
		catch
		{
		}
	}

	private void OnNewCompanionAdded(Hero newCompanion)
	{
		try
		{
			if (newCompanion == null || !newCompanion.IsPlayerCompanion)
			{
				return;
			}
			string playerIntro = (ShoutBehavior.BuildPlayerSceneIntroForExternal(newCompanion) ?? "").Trim();
			string factText = "你已经加入了玩家队伍，成为了玩家的同伴。";
			if (!string.IsNullOrWhiteSpace(playerIntro))
			{
				factText = factText + " 你对玩家的认识如下：" + playerIntro;
			}
			MyBehavior.AppendExternalNpcFact(newCompanion, factText);
		}
		catch
		{
		}
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
