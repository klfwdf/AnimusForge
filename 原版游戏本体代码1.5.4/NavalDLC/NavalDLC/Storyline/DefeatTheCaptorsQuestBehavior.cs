using NavalDLC.Storyline.Quests;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;

namespace NavalDLC.Storyline;

public class DefeatTheCaptorsQuestBehavior : CampaignBehaviorBase
{
	private DefeatTheCaptorsQuest _cachedQuest;

	private static DefeatTheCaptorsQuest Instance
	{
		get
		{
			DefeatTheCaptorsQuestBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<DefeatTheCaptorsQuestBehavior>();
			if (campaignBehavior._cachedQuest != null && campaignBehavior._cachedQuest.IsOngoing)
			{
				return campaignBehavior._cachedQuest;
			}
			foreach (QuestBase quest in Campaign.Current.QuestManager.Quests)
			{
				if (quest is DefeatTheCaptorsQuest cachedQuest)
				{
					campaignBehavior._cachedQuest = cachedQuest;
					return campaignBehavior._cachedQuest;
				}
			}
			return null;
		}
	}

	public override void RegisterEvents()
	{
		if (!NavalStorylineData.IsNavalStorylineCanceled())
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		}
	}

	private void OnSessionLaunched(CampaignGameStarter gameStarter)
	{
		gameStarter.AddGameMenu("defeat_the_captors_after_fight", "{=GDwBJZQr}For a brief moment, your captors seem to have forgotten about you, offering you a chance to break free from your shackles.", defeat_the_captors_after_fight_on_init);
		gameStarter.AddGameMenuOption("defeat_the_captors_after_fight", "defeat_the_captors_after_fight_attack", "{=zxMOqlhs}Attack", defeat_the_captors_fight_on_condition, defeat_the_captors_fight_on_consequence);
	}

	private void defeat_the_captors_after_fight_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("encounter_naval");
	}

	private bool defeat_the_captors_fight_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return Instance != null;
	}

	private void defeat_the_captors_fight_on_consequence(MenuCallbackArgs args)
	{
		if (Instance != null)
		{
			Hero.MainHero.Heal(Hero.MainHero.MaxHitPoints);
			Instance.StartMission();
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
