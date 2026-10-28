using SandBox.Conversation;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements.Locations;

namespace SandBox.CampaignBehaviors;

internal class CompanionDismissCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, OnCompanionRemoved);
	}

	private void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
	{
		if (LocationComplex.Current != null)
		{
			LocationComplex.Current.RemoveCharacterIfExists(companion);
		}
		if (PlayerEncounter.LocationEncounter != null)
		{
			PlayerEncounter.LocationEncounter.RemoveAccompanyingCharacter(companion);
		}
		if (detail == RemoveCompanionAction.RemoveCompanionDetail.Fire && Hero.MainHero.CurrentSettlement != null)
		{
			AgentNavigator agentNavigator = ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator;
			if (agentNavigator?.GetActiveBehavior() is FollowAgentBehavior)
			{
				agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().RemoveBehavior<FollowAgentBehavior>();
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}
}
