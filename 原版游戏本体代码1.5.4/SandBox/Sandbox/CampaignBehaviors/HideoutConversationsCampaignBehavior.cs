using SandBox.Missions.MissionLogics.Hideout;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.CampaignBehaviors;

public class HideoutConversationsCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
	}

	private void AddDialogs(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddDialogLine("bandit_hideout_start_defender", "start", "bandit_hideout_defender", "{=nYCXzAYH}You! You've cut quite a swathe through my men there, damn you. How about we settle this, one-on-one?", bandit_hideout_start_defender_on_condition, null);
		campaignGameStarter.AddPlayerLine("bandit_hideout_start_defender_1", "bandit_hideout_defender", "close_window", "{=dzXaXKaC}Very well.", null, bandit_hideout_start_duel_fight_on_consequence);
		campaignGameStarter.AddPlayerLine("bandit_hideout_start_defender_2", "bandit_hideout_defender", "close_window", "{=ukRZd2AA}I don't fight duels with brigands.", null, bandit_hideout_continue_battle_on_consequence, 100, bandit_hideout_continue_battle_on_clickable_condition);
	}

	private bool bandit_hideout_start_defender_on_condition()
	{
		PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
		if (encounteredParty != null && encounteredParty.MapFaction?.IsBanditFaction == true && encounteredParty != null && encounteredParty.Settlement?.IsHideout == true)
		{
			if (Mission.Current?.GetMissionBehavior<HideoutMissionController>() == null)
			{
				return Mission.Current?.GetMissionBehavior<HideoutAmbushMissionController>() != null;
			}
			return true;
		}
		return false;
	}

	private void bandit_hideout_start_duel_fight_on_consequence()
	{
		if (Mission.Current.GetMissionBehavior<HideoutMissionController>() != null)
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += HideoutMissionController.StartBossFightDuelMode;
		}
		else if (Mission.Current.GetMissionBehavior<HideoutAmbushMissionController>() != null)
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += HideoutAmbushMissionController.StartBossFightDuelMode;
		}
	}

	private void bandit_hideout_continue_battle_on_consequence()
	{
		if (Mission.Current.GetMissionBehavior<HideoutMissionController>() != null)
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += HideoutMissionController.StartBossFightBattleMode;
		}
		else if (Mission.Current.GetMissionBehavior<HideoutAmbushMissionController>() != null)
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += HideoutAmbushMissionController.StartBossFightBattleMode;
		}
	}

	private bool bandit_hideout_continue_battle_on_clickable_condition(out TextObject explanation)
	{
		bool flag = false;
		foreach (Agent activeAgent in Mission.Current.PlayerTeam.ActiveAgents)
		{
			if (!activeAgent.IsMount && activeAgent.Character != CharacterObject.PlayerCharacter)
			{
				flag = true;
				break;
			}
		}
		explanation = TextObject.GetEmpty();
		if (!flag)
		{
			explanation = new TextObject("{=F9HxO1iS}You don't have any men.");
		}
		return flag;
	}
}
