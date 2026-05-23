using System.Linq;
using StoryMode.Quests.SecondPhase;
using StoryMode.Quests.SecondPhase.ConspiracyQuests;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace StoryMode.GameComponents.CampaignBehaviors;

public class SecondPhaseCampaignBehavior : CampaignBehaviorBase
{
	private int _conspiracyQuestTriggerDayCounter;

	private bool _isConspiracySetUpStarted;

	public SecondPhaseCampaignBehavior()
	{
		_conspiracyQuestTriggerDayCounter = 0;
		_isConspiracySetUpStarted = false;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTick);
		CampaignEvents.OnQuestStartedEvent.AddNonSerializedListener(this, OnQuestStarted);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		StoryModeEvents.OnConspiracyActivatedEvent.AddNonSerializedListener(this, OnConspiracyActivated);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_conspiracyQuestTriggerDayCounter", ref _conspiracyQuestTriggerDayCounter);
		dataStore.SyncData("_isConspiracySetUpStarted", ref _isConspiracySetUpStarted);
	}

	private void WeeklyTick()
	{
		int num = 14 + MBRandom.RandomIntWithSeed((uint)(SecondPhase.Instance?.LastConspiracyQuestCreationTime.ToMilliseconds ?? 53.0), 2000u) % 8;
		if (_isConspiracySetUpStarted && StoryModeManager.Current.MainStoryLine.ThirdPhase == null && SecondPhase.Instance.ConspiracyStrength < 2000f && SecondPhase.Instance.LastConspiracyQuestCreationTime.ElapsedDaysUntilNow >= (float)num && !IsThereActiveConspiracyQuest())
		{
			SecondPhase.Instance.CreateNextConspiracyQuest();
		}
	}

	private void OnQuestStarted(QuestBase quest)
	{
		if (quest is AssembleEmpireQuestBehavior.AssembleEmpireQuest || quest is WeakenEmpireQuestBehavior.WeakenEmpireQuest)
		{
			StoryModeManager.Current.MainStoryLine.CompleteFirstPhase();
			_isConspiracySetUpStarted = true;
		}
	}

	private void DailyTick()
	{
		if (_isConspiracySetUpStarted && _conspiracyQuestTriggerDayCounter < 10)
		{
			_conspiracyQuestTriggerDayCounter++;
			if (_conspiracyQuestTriggerDayCounter >= 10)
			{
				new ConspiracyProgressQuest().StartQuest();
			}
		}
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		SecondPhase.Instance?.OnSessionLaunched();
	}

	private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
	{
		foreach (MobileParty item in Campaign.Current.CustomParties.ToList())
		{
			if (!item.Name.HasSameValue(new TextObject("{=eVzg5Mtl}Conspiracy Caravan")))
			{
				continue;
			}
			bool flag = true;
			foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
			{
				if (quest.GetType() == typeof(DisruptSupplyLinesConspiracyQuest) && ((DisruptSupplyLinesConspiracyQuest)quest).ConspiracyCaravan == item)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				DestroyPartyAction.Apply(null, item);
			}
		}
	}

	private void OnConspiracyActivated()
	{
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}

	private bool IsThereActiveConspiracyQuest()
	{
		foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
		{
			if (quest.IsOngoing && typeof(ConspiracyQuestBase) == quest.GetType().BaseType)
			{
				return true;
			}
		}
		return false;
	}
}
