using System.Linq;
using Helpers;
using StoryMode.Quests.SecondPhase.ConspiracyQuests;
using TaleWorlds.CampaignSystem;
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
		else if (StoryModeManager.Current.MainStoryLine.SecondPhase != null && (StoryModeManager.Current.MainStoryLine.SecondPhase.ConspiracyClan == attackerParty.MapFaction || StoryModeManager.Current.MainStoryLine.SecondPhase.ConspiracyClan == defenderParty.MapFaction))
		{
			QuestBase questBase = Campaign.Current.QuestManager.Quests.FirstOrDefault((QuestBase q) => !q.IsFinalized && q.GetType() == typeof(DisruptSupplyLinesConspiracyQuest));
			if (questBase != null && ((DisruptSupplyLinesConspiracyQuest)questBase).ConspiracyCaravan == defenderParty.MobileParty)
			{
				result = base.BaseModel.GetEncounterMenu(attackerParty, defenderParty, out startBattle, out joinBattle);
			}
			else
			{
				result = "encounter";
				startBattle = true;
				joinBattle = true;
			}
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
