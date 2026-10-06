using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace StoryMode.GameComponents.CampaignBehaviors;

public class StoryModeBanditSpawnCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		if (!TutorialPhase.Instance.IsCompleted)
		{
			StoryModeEvents.OnStoryModeTutorialEndedEvent.AddNonSerializedListener(this, OnTutorialEnded);
		}
	}

	private void OnTutorialEnded()
	{
		if (TutorialPhase.Instance.IsSkipped)
		{
			SpawnInitialBanditsAndLooters();
		}
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}

	private void SpawnInitialBanditsAndLooters()
	{
		BanditSpawnCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<BanditSpawnCampaignBehavior>();
		if (campaignBehavior != null)
		{
			campaignBehavior.InitializeInitialHideouts();
			campaignBehavior.SpawnBanditsAroundHideoutAtNewGame();
			campaignBehavior.SpawnLootersAtNewGame();
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
