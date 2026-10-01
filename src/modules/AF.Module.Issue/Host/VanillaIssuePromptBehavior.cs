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
		VanillaIssueOfferBridge.ClearPendingAlternativeDispatchForCampaign();
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
		CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, OnNewCompanionAdded);
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (dataStore.IsLoading)
		{
			VanillaIssueOfferBridge.ClearPendingAlternativeDispatchForCampaign();
		}
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
			if (IssueCompletionReceiptOwner.TryBuildCompletionReceipt(quest, detail, out Hero questGiver, out string memoryFact))
			{
				MyBehavior.AppendExternalPlayerFact(questGiver, memoryFact);
			}
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

}
