using System.Collections.Generic;
using Helpers;
using NavalDLC.CharacterDevelopment;
using NavalDLC.Storyline;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.GameComponents;

public class NavalDLCEncounterModel : EncounterModel
{
	public override float NeededMaximumLandDistanceForEncounteringMobileParty => base.BaseModel.NeededMaximumLandDistanceForEncounteringMobileParty;

	public override float NeededMaximumNavalDistanceForEncounteringMobileParty => 1.5f;

	public override float MaximumAllowedLandDistanceForEncounteringMobilePartyInArmy => base.BaseModel.MaximumAllowedLandDistanceForEncounteringMobilePartyInArmy;

	public override float MaximumAllowedNavalDistanceForEncounteringMobilePartyInArmy => 2.5f;

	public override float NeededMaximumDistanceForEncounteringTown => base.BaseModel.NeededMaximumDistanceForEncounteringTown;

	public override float NeededMaximumDistanceForEncounteringBlockade => base.BaseModel.NeededMaximumDistanceForEncounteringBlockade;

	public override float NeededMaximumDistanceForEncounteringVillage => base.BaseModel.NeededMaximumDistanceForEncounteringVillage;

	public override float GetEncounterJoiningRadius => base.BaseModel.GetEncounterJoiningRadius;

	public override float GetSettlementBeingNearFieldBattleRadius => base.BaseModel.GetSettlementBeingNearFieldBattleRadius;

	public override float PlayerParleyDistance => base.BaseModel.PlayerParleyDistance;

	public override int MinimumNumberOfMenForAttackingVillageViaScene => 15;

	public override bool CanMainHeroDoParleyWithParty(PartyBase partyBase, out TextObject explanation)
	{
		bool flag = base.BaseModel.CanMainHeroDoParleyWithParty(partyBase, out explanation);
		if (flag)
		{
			if (MobileParty.MainParty.IsCurrentlyAtSea)
			{
				explanation = new TextObject("{=eWxpOYAe}You can't start parley while at sea.");
				flag = false;
			}
			else if (MobileParty.MainParty.IsTransitionInProgress)
			{
				explanation = new TextObject("{=boWTBYUF}You can't start parley while embarking.");
				flag = false;
			}
		}
		return flag;
	}

	public override MapEventComponent CreateMapEventComponentForEncounter(PartyBase attackerParty, PartyBase defenderParty, MapEvent.BattleTypes battleType)
	{
		return base.BaseModel.CreateMapEventComponentForEncounter(attackerParty, defenderParty, battleType);
	}

	public override void FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter(List<MobileParty> partiesToJoinPlayerSide, List<MobileParty> partiesToJoinEnemySide)
	{
		base.BaseModel.FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter(partiesToJoinPlayerSide, partiesToJoinEnemySide);
		if (!NavalStorylineData.IsNavalStoryLineActive())
		{
			return;
		}
		for (int num = partiesToJoinPlayerSide.Count - 1; num >= 0; num--)
		{
			if (!partiesToJoinPlayerSide[num].IsNavalStorylineQuestParty())
			{
				partiesToJoinPlayerSide.RemoveAt(num);
			}
		}
		for (int num2 = partiesToJoinEnemySide.Count - 1; num2 >= 0; num2--)
		{
			if (!partiesToJoinEnemySide[num2].IsNavalStorylineQuestParty())
			{
				partiesToJoinEnemySide.RemoveAt(num2);
			}
		}
	}

	public override bool CanPlayerForceBanditsToJoin(out TextObject explanation)
	{
		if (MobileParty.MainParty.IsCurrentlyAtSea)
		{
			bool perkValue = Hero.MainHero.GetPerkValue(NavalPerks.Mariner.Arr);
			explanation = (perkValue ? null : new TextObject("{=MaetSSa1}You need '{PERK}' perk to make this party join you.").SetTextVariable("PERK", NavalPerks.Mariner.Arr.Name));
			return perkValue;
		}
		return base.BaseModel.CanPlayerForceBanditsToJoin(out explanation);
	}

	public override float GetMapEventSideRunAwayChance(MapEventSide mapEventSide)
	{
		return base.BaseModel.GetMapEventSideRunAwayChance(mapEventSide);
	}

	public override ExplainedNumber GetBribeChance(MobileParty defenderParty, MobileParty attackerParty)
	{
		ExplainedNumber bonuses = base.BaseModel.GetBribeChance(defenderParty, attackerParty);
		if (defenderParty.IsBandit && defenderParty.HasNavalNavigationCapability)
		{
			PerkHelper.AddPerkBonusForCharacter(NavalPerks.Mariner.Arr, attackerParty.CurrentBattleEnvironment, attackerParty.LeaderHero.CharacterObject, isPrimaryBonus: true, ref bonuses);
		}
		return bonuses;
	}

	public override int GetCharacterSergeantScore(Hero hero)
	{
		return base.BaseModel.GetCharacterSergeantScore(hero);
	}

	public override IEnumerable<PartyBase> GetDefenderPartiesOfSettlement(Settlement settlement, MapEvent.BattleTypes mapEventType)
	{
		return base.BaseModel.GetDefenderPartiesOfSettlement(settlement, mapEventType);
	}

	public override Hero GetLeaderOfMapEvent(MapEvent mapEvent, BattleSideEnum side)
	{
		return base.BaseModel.GetLeaderOfMapEvent(mapEvent, side);
	}

	public override Hero GetLeaderOfSiegeEvent(SiegeEvent siegeEvent, BattleSideEnum side)
	{
		return base.BaseModel.GetLeaderOfSiegeEvent(siegeEvent, side);
	}

	public override PartyBase GetNextDefenderPartyOfSettlement(Settlement settlement, ref int partyIndex, MapEvent.BattleTypes mapEventType)
	{
		return base.BaseModel.GetNextDefenderPartyOfSettlement(settlement, ref partyIndex, mapEventType);
	}

	public override float GetSurrenderChance(MobileParty defenderParty, MobileParty attackerParty)
	{
		return base.BaseModel.GetSurrenderChance(defenderParty, attackerParty);
	}

	public override bool IsEncounterExemptFromHostileActions(PartyBase side1, PartyBase side2)
	{
		return base.BaseModel.IsEncounterExemptFromHostileActions(side1, side2);
	}

	public override bool IsPartyUnderPlayerCommand(PartyBase party)
	{
		if (party.IsMobile && !party.MobileParty.IsMainParty && party.MobileParty.IsCurrentlyUsedByAQuest && NavalStorylineData.IsNavalStoryLineActive() && NavalStorylineData.GetStorylineStage() == NavalStorylineData.NavalStorylineStage.Act2)
		{
			return false;
		}
		return base.BaseModel.IsPartyUnderPlayerCommand(party);
	}

	public override MBReadOnlyList<MobileParty> GetPartiesToTeleportOnMapEventFinalize(MapEvent mapEvent)
	{
		MBReadOnlyList<MobileParty> partiesToTeleportOnMapEventFinalize = base.BaseModel.GetPartiesToTeleportOnMapEventFinalize(mapEvent);
		MBList<MobileParty> mBList = new MBList<MobileParty>();
		foreach (MobileParty item in partiesToTeleportOnMapEventFinalize)
		{
			if (!item.IsCurrentlyAtSea || item.HasNavalNavigationCapability)
			{
				mBList.Add(item);
			}
		}
		return mBList;
	}
}
