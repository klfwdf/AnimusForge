using System;
using SandBox.Conversation;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements.Locations;

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000D0 RID: 208
	internal class CompanionDismissCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06000979 RID: 2425 RVA: 0x0004767B File Offset: 0x0004587B
		public override void RegisterEvents()
		{
			CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, new Action<Hero, RemoveCompanionAction.RemoveCompanionDetail>(this.OnCompanionRemoved));
		}

		// Token: 0x0600097A RID: 2426 RVA: 0x00047694 File Offset: 0x00045894
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
				if (((agentNavigator != null) ? agentNavigator.GetActiveBehavior() : null) is FollowAgentBehavior)
				{
					agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().RemoveBehavior<FollowAgentBehavior>();
				}
			}
		}

		// Token: 0x0600097B RID: 2427 RVA: 0x00047702 File Offset: 0x00045902
		public override void SyncData(IDataStore dataStore)
		{
		}
	}
}
