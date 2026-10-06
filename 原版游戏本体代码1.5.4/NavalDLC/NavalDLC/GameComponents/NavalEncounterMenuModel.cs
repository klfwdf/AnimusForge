using System.Linq;
using Helpers;
using NavalDLC.Storyline;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace NavalDLC.GameComponents;

public class NavalEncounterMenuModel : EncounterGameMenuModel
{
	public override string GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty, out bool startBattle, out bool joinBattle)
	{
		PartyBase encounteredPartyBase = MapEventHelper.GetEncounteredPartyBase(attackerParty, defenderParty);
		if (NavalStorylineData.IsNavalStoryLineActive() && encounteredPartyBase.IsMobile && encounteredPartyBase.MobileParty.StringId == "free_the_sea_hounds_captives_initial_quest_party")
		{
			startBattle = false;
			joinBattle = false;
			return "act_3_quest_5_encounter_menu";
		}
		if (NavalStorylineData.IsNavalStoryLineActive() && defenderParty.IsSettlement && defenderParty.Settlement.IsTown && defenderParty.Settlement.HasPort)
		{
			startBattle = false;
			joinBattle = false;
			return "naval_storyline_virtualport";
		}
		if (NavalStorylineData.IsNavalStoryLineActive() && defenderParty.IsSettlement && defenderParty.Settlement.IsVillage && defenderParty.Settlement.HasPort)
		{
			startBattle = false;
			joinBattle = false;
			return "naval_storyline_encounter_blocking";
		}
		if (NavalStorylineData.IsNavalStoryLineActive() && Settlement.CurrentSettlement == null)
		{
			bool num = attackerParty.IsMobile && attackerParty.MobileParty.IsBandit;
			bool flag = defenderParty.IsMobile && defenderParty.MobileParty.IsBandit;
			if (!num && !flag && (!defenderParty.IsMobile || attackerParty != PartyBase.MainParty || !defenderParty.IsNavalStorylineQuestParty()) && (!attackerParty.IsMobile || defenderParty != PartyBase.MainParty || !attackerParty.IsNavalStorylineQuestParty()))
			{
				startBattle = false;
				joinBattle = false;
				return "naval_storyline_encounter_blocking";
			}
		}
		string encounterMenu = base.BaseModel.GetEncounterMenu(attackerParty, defenderParty, out startBattle, out joinBattle);
		PartyBase party = ((attackerParty == PartyBase.MainParty) ? defenderParty : attackerParty);
		if (NavalStorylineData.IsNavalStoryLineActive() && party.IsNavalStorylineQuestParty())
		{
			switch (encounterMenu)
			{
			case "encounter_meeting":
				return "naval_storyline_encounter_meeting";
			case "encounter":
				return "naval_storyline_encounter";
			case "join_encounter":
				return "naval_storyline_join_encounter";
			}
		}
		return encounterMenu;
	}

	public override string GetGenericStateMenu()
	{
		string genericStateMenu = base.BaseModel.GetGenericStateMenu();
		if (NavalStorylineData.IsNavalStoryLineActive() && genericStateMenu == "encounter")
		{
			MapEvent mapEvent = MobileParty.MainParty.MapEvent;
			if (mapEvent.PartiesOnSide(mapEvent.GetOtherSide(mapEvent.PlayerSide)).Any((MapEventParty x) => x.Party.IsNavalStorylineQuestParty()))
			{
				return "naval_storyline_encounter";
			}
		}
		return genericStateMenu;
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
