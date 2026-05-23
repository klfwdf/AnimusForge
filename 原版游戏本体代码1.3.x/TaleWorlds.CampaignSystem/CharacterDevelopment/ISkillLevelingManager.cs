using System;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CharacterDevelopment
{
	// Token: 0x020003AA RID: 938
	public interface ISkillLevelingManager
	{
		// Token: 0x060035F5 RID: 13813
		void OnCombatHit(CharacterObject affectorCharacter, CharacterObject affectedCharacter, CharacterObject captain, Hero commander, float speedBonusFromMovement, float shotDifficulty, WeaponComponentData affectorWeapon, float hitPointRatio, CombatXpModel.MissionTypeEnum missionType, bool isAffectorMounted, bool isTeamKill, bool isAffectorUnderCommand, float damageAmount, bool isFatal, bool isSiegeEngineHit, bool isHorseCharge, bool isSneakAttack);

		// Token: 0x060035F6 RID: 13814
		void OnSiegeEngineDestroyed(MobileParty party, SiegeEngineType destroyedSiegeEngine);

		// Token: 0x060035F7 RID: 13815
		void OnSimulationCombatKill(CharacterObject affectorCharacter, CharacterObject affectedCharacter, PartyBase affectorParty, PartyBase commanderParty);

		// Token: 0x060035F8 RID: 13816
		void OnTradeProfitMade(PartyBase party, int tradeProfit);

		// Token: 0x060035F9 RID: 13817
		void OnTradeProfitMade(Hero hero, int tradeProfit);

		// Token: 0x060035FA RID: 13818
		void OnSettlementProjectFinished(Settlement settlement);

		// Token: 0x060035FB RID: 13819
		void OnSettlementGoverned(Hero governor, Settlement settlement);

		// Token: 0x060035FC RID: 13820
		void OnInfluenceSpent(Hero hero, float amountSpent);

		// Token: 0x060035FD RID: 13821
		void OnGainRelation(Hero hero, Hero gainedRelationWith, float relationChange, ChangeRelationAction.ChangeRelationDetail detail = ChangeRelationAction.ChangeRelationDetail.Default);

		// Token: 0x060035FE RID: 13822
		void OnTroopRecruited(Hero hero, int amount, int tier);

		// Token: 0x060035FF RID: 13823
		void OnBribeGiven(int amount);

		// Token: 0x06003600 RID: 13824
		void OnWarehouseProduction(EquipmentElement production);

		// Token: 0x06003601 RID: 13825
		void OnAIPartyLootCasualties(int goldAmount, Hero winnerPartyLeader, PartyBase defeatedParty);

		// Token: 0x06003602 RID: 13826
		void OnBanditsRecruited(MobileParty mobileParty, CharacterObject bandit, int count);

		// Token: 0x06003603 RID: 13827
		void OnMainHeroReleasedFromCaptivity(float captivityTime);

		// Token: 0x06003604 RID: 13828
		void OnMainHeroTortured();

		// Token: 0x06003605 RID: 13829
		void OnMainHeroDisguised(bool isNotCaught);

		// Token: 0x06003606 RID: 13830
		void OnRaid(MobileParty attackerParty, ItemRoster lootedItems);

		// Token: 0x06003607 RID: 13831
		void OnLoot(MobileParty attackerParty, MobileParty forcedParty, ItemRoster lootedItems, bool attacked);

		// Token: 0x06003608 RID: 13832
		void OnPrisonerSell(MobileParty mobileParty, in TroopRoster prisonerRoster);

		// Token: 0x06003609 RID: 13833
		void OnSurgeryApplied(MobileParty party, bool surgerySuccess, int troopTier);

		// Token: 0x0600360A RID: 13834
		void OnTacticsUsed(MobileParty party, float xp);

		// Token: 0x0600360B RID: 13835
		void OnHideoutSpotted(MobileParty party, PartyBase spottedParty);

		// Token: 0x0600360C RID: 13836
		void OnTrackDetected(Track track);

		// Token: 0x0600360D RID: 13837
		void OnTravelOnFoot(Hero hero, float speed);

		// Token: 0x0600360E RID: 13838
		void OnTravelOnHorse(Hero hero, float speed);

		// Token: 0x0600360F RID: 13839
		void OnTravelOnWater(Hero hero, float speed);

		// Token: 0x06003610 RID: 13840
		void OnHeroHealedWhileWaiting(Hero hero, int healingAmount);

		// Token: 0x06003611 RID: 13841
		void OnRegularTroopHealedWhileWaiting(MobileParty mobileParty, int healedTroopCount, float averageTier);

		// Token: 0x06003612 RID: 13842
		void OnLeadingArmy(MobileParty mobileParty);

		// Token: 0x06003613 RID: 13843
		void OnSieging(MobileParty mobileParty);

		// Token: 0x06003614 RID: 13844
		void OnSiegeEngineBuilt(MobileParty mobileParty, SiegeEngineType siegeEngine);

		// Token: 0x06003615 RID: 13845
		void OnUpgradeTroops(PartyBase party, CharacterObject troop, CharacterObject upgrade, int numberOfTroops);

		// Token: 0x06003616 RID: 13846
		void OnPersuasionSucceeded(Hero targetHero, SkillObject skill, PersuasionDifficulty difficulty, int argumentDifficultyBonusCoefficient);

		// Token: 0x06003617 RID: 13847
		void OnPrisonBreakEnd(Hero prisonerHero, bool isSucceeded);

		// Token: 0x06003618 RID: 13848
		void OnWallBreached(MobileParty party);

		// Token: 0x06003619 RID: 13849
		void OnForceVolunteers(MobileParty attackerParty, PartyBase forcedParty);

		// Token: 0x0600361A RID: 13850
		void OnForceSupplies(MobileParty attackerParty, ItemRoster lootedItems, bool attacked);

		// Token: 0x0600361B RID: 13851
		void OnAIPartiesTravel(Hero hero, bool isCaravanParty, TerrainType currentTerrainType);

		// Token: 0x0600361C RID: 13852
		void OnTraverseTerrain(MobileParty mobileParty, TerrainType currentTerrainType);

		// Token: 0x0600361D RID: 13853
		void OnBattleEnded(PartyBase party, CharacterObject troop, int excessXp);

		// Token: 0x0600361E RID: 13854
		void OnFoodConsumed(MobileParty mobileParty, bool wasStarving);

		// Token: 0x0600361F RID: 13855
		void OnAlleyCleared(Alley alley);

		// Token: 0x06003620 RID: 13856
		void OnDailyAlleyTick(Alley alley, Hero alleyLeader);

		// Token: 0x06003621 RID: 13857
		void OnBoardGameWonAgainstLord(Hero lord, BoardGameHelper.AIDifficulty difficulty, bool extraXpGain);

		// Token: 0x06003622 RID: 13858
		void OnShipDamaged(Ship ship, float rawDamage, float finalDamage);

		// Token: 0x06003623 RID: 13859
		void OnShipRepaired(Ship ship, float repairedHitPoints);

		// Token: 0x06003624 RID: 13860
		void OnHideoutMissionEnd(bool isSucceeded);
	}
}
