using System;
using SandBox.Missions.MissionLogics.Hideout;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000D7 RID: 215
	public class HideoutConversationsCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06000A14 RID: 2580 RVA: 0x0004CEFF File Offset: 0x0004B0FF
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
		}

		// Token: 0x06000A15 RID: 2581 RVA: 0x0004CF18 File Offset: 0x0004B118
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06000A16 RID: 2582 RVA: 0x0004CF1A File Offset: 0x0004B11A
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddDialogs(campaignGameStarter);
		}

		// Token: 0x06000A17 RID: 2583 RVA: 0x0004CF24 File Offset: 0x0004B124
		private void AddDialogs(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddDialogLine("bandit_hideout_start_defender", "start", "bandit_hideout_defender", "{=nYCXzAYH}You! You've cut quite a swathe through my men there, damn you. How about we settle this, one-on-one?", new ConversationSentence.OnConditionDelegate(this.bandit_hideout_start_defender_on_condition), null, 100, null);
			campaignGameStarter.AddPlayerLine("bandit_hideout_start_defender_1", "bandit_hideout_defender", "close_window", "{=dzXaXKaC}Very well.", null, new ConversationSentence.OnConsequenceDelegate(this.bandit_hideout_start_duel_fight_on_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("bandit_hideout_start_defender_2", "bandit_hideout_defender", "close_window", "{=ukRZd2AA}I don't fight duels with brigands.", null, new ConversationSentence.OnConsequenceDelegate(this.bandit_hideout_continue_battle_on_consequence), 100, new ConversationSentence.OnClickableConditionDelegate(this.bandit_hideout_continue_battle_on_clickable_condition), null);
		}

		// Token: 0x06000A18 RID: 2584 RVA: 0x0004CFC0 File Offset: 0x0004B1C0
		private bool bandit_hideout_start_defender_on_condition()
		{
			PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
			return encounteredParty != null && !encounteredParty.IsMobile && encounteredParty.MapFaction.IsBanditFaction && (encounteredParty.MapFaction.IsBanditFaction && encounteredParty.IsSettlement && encounteredParty.Settlement.IsHideout && Mission.Current != null) && (Mission.Current.GetMissionBehavior<HideoutMissionController>() != null || Mission.Current.GetMissionBehavior<HideoutAmbushMissionController>() != null);
		}

		// Token: 0x06000A19 RID: 2585 RVA: 0x0004D034 File Offset: 0x0004B234
		private void bandit_hideout_start_duel_fight_on_consequence()
		{
			if (Mission.Current.GetMissionBehavior<HideoutMissionController>() != null)
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += HideoutMissionController.StartBossFightDuelMode;
				return;
			}
			if (Mission.Current.GetMissionBehavior<HideoutAmbushMissionController>() != null)
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += HideoutAmbushMissionController.StartBossFightDuelMode;
			}
		}

		// Token: 0x06000A1A RID: 2586 RVA: 0x0004D090 File Offset: 0x0004B290
		private void bandit_hideout_continue_battle_on_consequence()
		{
			if (Mission.Current.GetMissionBehavior<HideoutMissionController>() != null)
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += HideoutMissionController.StartBossFightBattleMode;
				return;
			}
			if (Mission.Current.GetMissionBehavior<HideoutAmbushMissionController>() != null)
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += HideoutAmbushMissionController.StartBossFightBattleMode;
			}
		}

		// Token: 0x06000A1B RID: 2587 RVA: 0x0004D0EC File Offset: 0x0004B2EC
		private bool bandit_hideout_continue_battle_on_clickable_condition(out TextObject explanation)
		{
			bool flag = false;
			foreach (Agent agent in Mission.Current.PlayerTeam.ActiveAgents)
			{
				if (!agent.IsMount && agent.Character != CharacterObject.PlayerCharacter)
				{
					flag = true;
					break;
				}
			}
			explanation = TextObject.GetEmpty();
			if (!flag)
			{
				explanation = new TextObject("{=F9HxO1iS}You don't have any men.", null);
			}
			return flag;
		}
	}
}
