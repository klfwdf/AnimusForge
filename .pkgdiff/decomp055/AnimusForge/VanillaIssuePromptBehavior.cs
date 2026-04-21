using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

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
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener((object)this, (Action<QuestBase, QuestCompleteDetails>)OnQuestCompleted);
		CampaignEvents.NewCompanionAdded.AddNonSerializedListener((object)this, (Action<Hero>)OnNewCompanionAdded);
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

	private void OnQuestCompleted(QuestBase quest, QuestCompleteDetails detail)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Hero val = ((quest != null) ? quest.QuestGiver : null);
			if (val == null || string.IsNullOrWhiteSpace(((MBObjectBase)val).StringId))
			{
				return;
			}
			string text = (((object)quest.Title)?.ToString() ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "一项原版任务";
			}
			string text2 = TranslateCompletionDetail(detail);
			List<string> list = new List<string>();
			try
			{
				foreach (JournalLog item in (List<JournalLog>)(object)quest.JournalEntries)
				{
					string text3 = (((object)item?.LogText)?.ToString() ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
					if (!string.IsNullOrWhiteSpace(text3))
					{
						list.Add(text3);
					}
				}
				if (list.Count > 3)
				{
					list = list.GetRange(Math.Max(0, list.Count - 3), Math.Min(3, list.Count));
				}
			}
			catch
			{
			}
			string text4 = "你之前交给玩家的原版任务“" + text + "”已经有结果了。";
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text4 = text4 + " 结果：" + text2 + "。";
			}
			if (quest.RewardGold > 0)
			{
				string text5 = text4;
				int rewardGold = quest.RewardGold;
				text4 = text5 + " 该任务的原版奖励 " + rewardGold + " 第纳尔已经由系统自动发放，无需你手动再次支付。";
			}
			else
			{
				text4 += " 若有原版任务奖励，也已由系统按原版流程自动结算，无需你手动再次发放。";
			}
			string text6 = ((list.Count > 0) ? (list[list.Count - 1] ?? "").Trim() : "");
			if (!string.IsNullOrWhiteSpace(text6))
			{
				text4 = text4 + " 最近任务记录：" + text6;
			}
			MyBehavior.AppendExternalPlayerFact(val, text4);
		}
		catch
		{
		}
	}

	private void OnNewCompanionAdded(Hero newCompanion)
	{
		try
		{
			if (newCompanion != null && newCompanion.IsPlayerCompanion)
			{
				string text = (ShoutBehavior.BuildPlayerSceneIntroForExternal(newCompanion) ?? "").Trim();
				string text2 = "你已经加入了玩家队伍，成为了玩家的同伴。";
				if (!string.IsNullOrWhiteSpace(text))
				{
					text2 = text2 + " 你对玩家的认识如下：" + text;
				}
				MyBehavior.AppendExternalNpcFact(newCompanion, text2);
			}
		}
		catch
		{
		}
	}

	private unsafe static string TranslateCompletionDetail(QuestCompleteDetails detail)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected I4, but got Unknown
		QuestCompleteDetails val = detail;
		QuestCompleteDetails val2 = val;
		return (val2 - 1) switch
		{
			0 => "任务已成功完成", 
			2 => "任务已失败", 
			4 => "任务以背叛结局失败", 
			3 => "任务已超时结束", 
			1 => "任务已取消", 
			_ => ((object)(*(QuestCompleteDetails*)(&detail))/*cast due to .constrained prefix*/).ToString(), 
		};
	}
}
