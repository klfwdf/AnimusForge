using System.Collections.Generic;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class BattleRewardModel : MBGameModel<BattleRewardModel>
{
	public abstract float GetBannerLootChanceFromDefeatedHero(Hero defeatedHero);

	public abstract ItemObject GetBannerRewardForWinningMapEvent(MapEvent mapEvent);

	public abstract int GetPlayerGainedRelationAmount(MapEvent mapEvent, Hero hero);

	public abstract ExplainedNumber CalculateRenownGain(PartyBase winnerParty, float renownValueOfBattleForWinnerSide, float contributionShareOfWinnerParty, float renownMultiplierForWinnerSide, bool includeDescriptions);

	public abstract ExplainedNumber CalculateInfluenceGain(PartyBase winnerParty, float influenceValueOfBattleForWinnerSide, float contributionShareOfWinnerParty, float influenceMultiplierForWinnerSide, bool includeDescriptions);

	public abstract ExplainedNumber CalculateMoraleGainVictory(PartyBase winnerParty, float renownValueOfBattleForWinnerSide, float contributionShareOfWinnerParty, bool includeDescriptions);

	public abstract float CalculateMoraleChangeOnRoundVictory(PartyBase party, MapEventSide partySide, BattleSideEnum roundWinner);

	public abstract int CalculateGoldLossAfterDefeat(Hero partyLeaderHero);

	public abstract EquipmentElement GetLootedItemFromTroop(CharacterObject character, float targetValue);

	public abstract float GetExpectedLootedItemValueFromCasualty(Hero winnerPartyLeaderHero, CharacterObject casualtyCharacter);

	public abstract int CalculatePlunderedGoldAmountFromDefeatedParty(PartyBase defeatedParty);

	public abstract MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootGoldChances(MBReadOnlyList<MapEventParty> winnerParties);

	public abstract float GetMainPartyMemberScatterChance();

	public abstract float GetAITradePenalty();

	public abstract void GetCaptureMemberChancesForWinnerParties(MapEvent endedMapEvent, MBReadOnlyList<MapEventParty> winnerParties, out MBList<KeyValuePair<MapEventParty, float>> woundedMemberChances, out MBList<KeyValuePair<MapEventParty, float>> healthyMemberChances);

	public abstract MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootPrisonerChances(MBReadOnlyList<MapEventParty> winnerParties, TroopRosterElement prisonerElement);

	public abstract MBList<KeyValuePair<MapEventParty, float>> GetLootItemChancesForWinnerParties(MBReadOnlyList<MapEventParty> winnerParties, PartyBase defeatedParty);

	public abstract MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootCasualtyChances(MBReadOnlyList<MapEventParty> winnerParties, PartyBase defeatedParty);

	public abstract float CalculateShipDamageAfterDefeat(Ship ship);

	public abstract MBReadOnlyList<KeyValuePair<Ship, MapEventParty>> DistributeDefeatedPartyShipsAmongWinners(MapEvent mapEvent, MBReadOnlyList<Ship> shipsToLoot, MBReadOnlyList<MapEventParty> winnerParties);

	public abstract float GetSunkenShipMoraleEffect(PartyBase shipOwner, Ship ship);

	public abstract float GetShipSiegeEngineHitMoraleEffect(Ship ship, SiegeEngineType siegeEngineType);

	public abstract Figurehead GetFigureheadLoot(MBReadOnlyList<MapEventParty> defeatedParties, PartyBase defeatedSideLeaderParty);

	public abstract MBReadOnlyList<MapEventParty> GetWinnerPartiesThatCanPlunderGoldFromShips(MBReadOnlyList<MapEventParty> winnerParties);

	public abstract bool CanTroopBeTakenPrisoner(CharacterObject troop);
}
