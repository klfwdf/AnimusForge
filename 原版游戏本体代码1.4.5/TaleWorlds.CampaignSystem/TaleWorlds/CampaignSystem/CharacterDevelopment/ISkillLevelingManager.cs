using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CharacterDevelopment;

public interface ISkillLevelingManager
{
	void OnCombatHit(CharacterObject affectorCharacter, CharacterObject affectedCharacter, CharacterObject captain, Hero commander, float speedBonusFromMovement, float shotDifficulty, WeaponComponentData affectorWeapon, float hitPointRatio, CombatXpModel.MissionTypeEnum missionType, bool isAffectorMounted, bool isTeamKill, bool isAffectorUnderCommand, float damageAmount, bool isFatal, bool isSiegeEngineHit, bool isHorseCharge, bool isSneakAttack);

	void OnSiegeEngineDestroyed(MobileParty party, SiegeEngineType destroyedSiegeEngine);

	void OnSimulationCombatKill(CharacterObject affectorCharacter, CharacterObject affectedCharacter, PartyBase affectorParty, PartyBase commanderParty);

	void OnTradeProfitMade(PartyBase party, int tradeProfit);

	void OnTradeProfitMade(Hero hero, int tradeProfit);

	void OnSettlementProjectFinished(Settlement settlement);

	void OnSettlementGoverned(Hero governor, Settlement settlement);

	void OnInfluenceSpent(Hero hero, float amountSpent);

	void OnGainRelation(Hero hero, Hero gainedRelationWith, float relationChange, ChangeRelationAction.ChangeRelationDetail detail = ChangeRelationAction.ChangeRelationDetail.Default);

	void OnTroopRecruited(Hero hero, int amount, int tier);

	void OnBribeGiven(int amount);

	void OnWarehouseProduction(EquipmentElement production);

	void OnAIPartyLootCasualties(int goldAmount, Hero winnerPartyLeader, PartyBase defeatedParty);

	void OnBanditsRecruited(MobileParty mobileParty, CharacterObject bandit, int count);

	void OnMainHeroReleasedFromCaptivity(float captivityTime);

	void OnMainHeroTortured();

	void OnMainHeroDisguised(bool isNotCaught);

	void OnRaid(MobileParty attackerParty, ItemRoster lootedItems);

	void OnLoot(MobileParty attackerParty, MobileParty forcedParty, ItemRoster lootedItems, bool attacked);

	void OnPrisonerSell(MobileParty mobileParty, in TroopRoster prisonerRoster);

	void OnSurgeryApplied(MobileParty party, bool surgerySuccess, int troopTier);

	void OnTacticsUsed(MobileParty party, float xp);

	void OnHideoutSpotted(MobileParty party, PartyBase spottedParty);

	void OnTrackDetected(Track track);

	void OnTravelOnFoot(Hero hero, float speed);

	void OnTravelOnHorse(Hero hero, float speed);

	void OnTravelOnWater(MobileParty party, float speed);

	void OnHeroHealedWhileWaiting(Hero hero, int healingAmount);

	void OnRegularTroopHealedWhileWaiting(MobileParty mobileParty, int healedTroopCount, float averageTier);

	void OnLeadingArmy(MobileParty mobileParty);

	void OnSieging(MobileParty mobileParty);

	void OnSiegeEngineBuilt(MobileParty mobileParty, SiegeEngineType siegeEngine);

	void OnUpgradeTroops(PartyBase party, CharacterObject troop, CharacterObject upgrade, int numberOfTroops);

	void OnPersuasionSucceeded(Hero targetHero, SkillObject skill, PersuasionDifficulty difficulty, int argumentDifficultyBonusCoefficient);

	void OnPrisonBreakEnd(Hero prisonerHero, bool isSucceeded);

	void OnWallBreached(MobileParty party);

	void OnForceVolunteers(MobileParty attackerParty, PartyBase forcedParty);

	void OnForceSupplies(MobileParty attackerParty, ItemRoster lootedItems, bool attacked);

	void OnAIPartiesTravel(Hero hero, bool isCaravanParty, TerrainType currentTerrainType);

	void OnTraverseTerrain(MobileParty mobileParty, TerrainType currentTerrainType);

	void OnBattleEnded(PartyBase party, CharacterObject troop, int excessXp);

	void OnFoodConsumed(MobileParty mobileParty, bool wasStarving);

	void OnAlleyCleared(Alley alley);

	void OnDailyAlleyTick(Alley alley, Hero alleyLeader);

	void OnBoardGameWonAgainstLord(Hero lord, BoardGameHelper.AIDifficulty difficulty, bool extraXpGain);

	void OnShipDamaged(Ship ship, float rawDamage, float finalDamage);

	void OnShipRepaired(Ship ship, float repairedHitPoints);

	void OnHideoutMissionEnd(bool isSucceeded);

	void OnHideoutClearedAsGhost();
}
