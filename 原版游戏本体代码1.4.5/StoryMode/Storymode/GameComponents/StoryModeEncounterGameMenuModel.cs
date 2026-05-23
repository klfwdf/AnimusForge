using Helpers;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace StoryMode.GameComponents;

public class StoryModeEncounterGameMenuModel : EncounterGameMenuModel
{
	public override string GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty, out bool startBattle, out bool joinBattle)
	{
		Settlement settlement = MapEventHelper.GetEncounteredPartyBase(attackerParty, defenderParty).Settlement;
		string result;
		if (settlement != null && settlement.SettlementComponent is TrainingField)
		{
			result = "training_field_menu";
			startBattle = false;
			joinBattle = false;
		}
		else if (StoryModeManager.Current.MainStoryLine.IsPlayerInteractionRestricted)
		{
			result = "storymode_game_menu_blocker";
			startBattle = false;
			joinBattle = false;
		}
		else
		{
			result = base.BaseModel.GetEncounterMenu(attackerParty, defenderParty, out startBattle, out joinBattle);
		}
		return result;
	}

	public override string GetGenericStateMenu()
	{
		return base.BaseModel.GetGenericStateMenu();
	}

	public override string GetNewPartyJoinMenu(MobileParty newParty)
	{
		return base.BaseModel.GetNewPartyJoinMenu(newParty);
	}

	public override string GetRaidCompleteMenu()
	{
		return base.BaseModel.GetRaidCompleteMenu();
	}

	public override bool IsPlunderMenu(string menuId)
	{
		return base.BaseModel.IsPlunderMenu(menuId);
	}
}
