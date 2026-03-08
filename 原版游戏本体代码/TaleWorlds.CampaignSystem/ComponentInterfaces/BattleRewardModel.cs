using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001B5 RID: 437
	public abstract class BattleRewardModel : MBGameModel<BattleRewardModel>
	{
		// Token: 0x06001D39 RID: 7481
		public abstract float GetBannerLootChanceFromDefeatedHero(Hero defeatedHero);

		// Token: 0x06001D3A RID: 7482
		public abstract ItemObject GetBannerRewardForWinningMapEvent(MapEvent mapEvent);

		// Token: 0x06001D3B RID: 7483
		public abstract int GetPlayerGainedRelationAmount(MapEvent mapEvent, Hero hero);

		// Token: 0x06001D3C RID: 7484
		public abstract ExplainedNumber CalculateRenownGain(PartyBase party, float renownValueOfBattle, float contributionShare);

		// Token: 0x06001D3D RID: 7485
		public abstract ExplainedNumber CalculateInfluenceGain(PartyBase party, float influenceValueOfBattle, float contributionShare);

		// Token: 0x06001D3E RID: 7486
		public abstract ExplainedNumber CalculateMoraleGainVictory(PartyBase party, float renownValueOfBattle, float contributionShare, MapEvent battle);

		// Token: 0x06001D3F RID: 7487
		public abstract ExplainedNumber CalculateMoraleChangeOnRoundVictory(PartyBase party, MapEventSide partySide, BattleSideEnum roundWinner);

		// Token: 0x06001D40 RID: 7488
		public abstract int CalculateGoldLossAfterDefeat(Hero partyLeaderHero);

		// Token: 0x06001D41 RID: 7489
		public abstract EquipmentElement GetLootedItemFromTroop(CharacterObject character, float targetValue);

		// Token: 0x06001D42 RID: 7490
		public abstract float GetExpectedLootedItemValueFromCasualty(Hero winnerPartyLeaderHero, CharacterObject casualtyCharacter);

		// Token: 0x06001D43 RID: 7491
		public abstract int CalculatePlunderedGoldAmountFromDefeatedParty(PartyBase defeatedParty);

		// Token: 0x06001D44 RID: 7492
		public abstract MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootGoldChances(MBReadOnlyList<MapEventParty> winnerParties);

		// Token: 0x06001D45 RID: 7493
		public abstract float GetMainPartyMemberScatterChance();

		// Token: 0x06001D46 RID: 7494
		public abstract float GetAITradePenalty();

		// Token: 0x06001D47 RID: 7495
		public abstract MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootMemberChancesForWinnerParties(MBReadOnlyList<MapEventParty> winnerParties);

		// Token: 0x06001D48 RID: 7496
		public abstract MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootPrisonerChances(MBReadOnlyList<MapEventParty> winnerParties, TroopRosterElement prisonerElement);

		// Token: 0x06001D49 RID: 7497
		public abstract MBList<KeyValuePair<MapEventParty, float>> GetLootItemChancesForWinnerParties(MBReadOnlyList<MapEventParty> winnerParties, PartyBase defeatedParty);

		// Token: 0x06001D4A RID: 7498
		public abstract MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootCasualtyChances(MBReadOnlyList<MapEventParty> winnerParties, PartyBase defeatedParty);

		// Token: 0x06001D4B RID: 7499
		public abstract float CalculateShipDamageAfterDefeat(Ship ship);

		// Token: 0x06001D4C RID: 7500
		public abstract MBReadOnlyList<KeyValuePair<Ship, MapEventParty>> DistributeDefeatedPartyShipsAmongWinners(MapEvent mapEvent, MBReadOnlyList<Ship> shipsToLoot, MBReadOnlyList<MapEventParty> winnerParties);

		// Token: 0x06001D4D RID: 7501
		public abstract float GetSunkenShipMoraleEffect(PartyBase shipOwner, Ship ship);

		// Token: 0x06001D4E RID: 7502
		public abstract float GetShipSiegeEngineHitMoraleEffect(Ship ship, SiegeEngineType siegeEngineType);

		// Token: 0x06001D4F RID: 7503
		public abstract Figurehead GetFigureheadLoot(MBReadOnlyList<MapEventParty> defeatedParties, PartyBase defeatedSideLeaderParty);

		// Token: 0x06001D50 RID: 7504
		public abstract MBReadOnlyList<MapEventParty> GetWinnerPartiesThatCanPlunderGoldFromShips(MBReadOnlyList<MapEventParty> winnerParties);
	}
}
