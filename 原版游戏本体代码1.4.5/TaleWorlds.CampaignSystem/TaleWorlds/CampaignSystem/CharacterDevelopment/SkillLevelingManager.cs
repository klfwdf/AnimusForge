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

public static class SkillLevelingManager
{
	private static ISkillLevelingManager Instance => Campaign.Current.SkillLevelingManager;

	public static void OnCombatHit(CharacterObject affectorCharacter, CharacterObject affectedCharacter, CharacterObject captain, Hero commander, float speedBonusFromMovement, float shotDifficulty, WeaponComponentData affectorWeapon, float hitPointRatio, CombatXpModel.MissionTypeEnum missionType, bool isAffectorMounted, bool isTeamKill, bool isAffectorUnderCommand, float damageAmount, bool isFatal, bool isSiegeEngineHit, bool isHorseCharge, bool isSneakAttack)
	{
		Instance.OnCombatHit(affectorCharacter, affectedCharacter, captain, commander, speedBonusFromMovement, shotDifficulty, affectorWeapon, hitPointRatio, missionType, isAffectorMounted, isTeamKill, isAffectorUnderCommand, damageAmount, isFatal, isSiegeEngineHit, isHorseCharge, isSneakAttack);
	}

	public static void OnSiegeEngineDestroyed(MobileParty party, SiegeEngineType destroyedSiegeEngine)
	{
		Instance.OnSiegeEngineDestroyed(party, destroyedSiegeEngine);
	}

	public static void OnWallBreached(MobileParty party)
	{
		Instance.OnWallBreached(party);
	}

	public static void OnSimulationCombatKill(CharacterObject affectorCharacter, CharacterObject affectedCharacter, PartyBase affectorParty, PartyBase commanderParty)
	{
		Instance.OnSimulationCombatKill(affectorCharacter, affectedCharacter, affectorParty, commanderParty);
	}

	public static void OnTradeProfitMade(PartyBase party, int tradeProfit)
	{
		Instance.OnTradeProfitMade(party, tradeProfit);
	}

	public static void OnTradeProfitMade(Hero hero, int tradeProfit)
	{
		Instance.OnTradeProfitMade(hero, tradeProfit);
	}

	public static void OnSettlementProjectFinished(Settlement settlement)
	{
		Instance.OnSettlementProjectFinished(settlement);
	}

	public static void OnSettlementGoverned(Hero governor, Settlement settlement)
	{
		Instance.OnSettlementGoverned(governor, settlement);
	}

	public static void OnInfluenceSpent(Hero hero, float amountSpent)
	{
		Instance.OnInfluenceSpent(hero, amountSpent);
	}

	public static void OnGainRelation(Hero hero, Hero gainedRelationWith, float relationChange, ChangeRelationAction.ChangeRelationDetail detail = ChangeRelationAction.ChangeRelationDetail.Default)
	{
		Instance.OnGainRelation(hero, gainedRelationWith, relationChange, detail);
	}

	public static void OnTroopRecruited(Hero hero, int amount, int tier)
	{
		Instance.OnTroopRecruited(hero, amount, tier);
	}

	public static void OnBribeGiven(int amount)
	{
		Instance.OnBribeGiven(amount);
	}

	public static void OnBanditsRecruited(MobileParty mobileParty, CharacterObject bandit, int count)
	{
		Instance.OnBanditsRecruited(mobileParty, bandit, count);
	}

	public static void OnMainHeroReleasedFromCaptivity(float captivityTime)
	{
		Instance.OnMainHeroReleasedFromCaptivity(captivityTime);
	}

	public static void OnMainHeroTortured()
	{
		Instance.OnMainHeroTortured();
	}

	public static void OnMainHeroDisguised(bool isNotCaught)
	{
		Instance.OnMainHeroDisguised(isNotCaught);
	}

	public static void OnRaid(MobileParty attackerParty, ItemRoster lootedItems)
	{
		Instance.OnRaid(attackerParty, lootedItems);
	}

	public static void OnLoot(MobileParty attackerParty, MobileParty forcedParty, ItemRoster lootedItems, bool attacked)
	{
		Instance.OnLoot(attackerParty, forcedParty, lootedItems, attacked);
	}

	public static void OnForceVolunteers(MobileParty attackerParty, PartyBase forcedParty)
	{
		Instance.OnForceVolunteers(attackerParty, forcedParty);
	}

	public static void OnForceSupplies(MobileParty attackerParty, ItemRoster lootedItems, bool attacked)
	{
		Instance.OnForceSupplies(attackerParty, lootedItems, attacked);
	}

	public static void OnPrisonerSell(MobileParty mobileParty, in TroopRoster prisonerRoster)
	{
		Instance.OnPrisonerSell(mobileParty, in prisonerRoster);
	}

	public static void OnSurgeryApplied(MobileParty party, bool surgerySuccess, int troopTier)
	{
		Instance.OnSurgeryApplied(party, surgerySuccess, troopTier);
	}

	public static void OnTacticsUsed(MobileParty party, float xp)
	{
		Instance.OnTacticsUsed(party, xp);
	}

	public static void OnHideoutSpotted(MobileParty party, PartyBase spottedParty)
	{
		Instance.OnHideoutSpotted(party, spottedParty);
	}

	public static void OnTrackDetected(Track track)
	{
		Instance.OnTrackDetected(track);
	}

	public static void OnTravelOnFoot(Hero hero, float speed)
	{
		Instance.OnTravelOnFoot(hero, speed);
	}

	public static void OnTravelOnHorse(Hero hero, float speed)
	{
		Instance.OnTravelOnHorse(hero, speed);
	}

	public static void OnTravelOnWater(MobileParty party, float speed)
	{
		Instance.OnTravelOnWater(party, speed);
	}

	public static void OnAIPartiesTravel(Hero hero, bool isCaravanParty, TerrainType currentTerrainType)
	{
		Instance.OnAIPartiesTravel(hero, isCaravanParty, currentTerrainType);
	}

	public static void OnTraverseTerrain(MobileParty mobileParty, TerrainType currentTerrainType)
	{
		Instance.OnTraverseTerrain(mobileParty, currentTerrainType);
	}

	public static void OnBattleEnded(PartyBase party, CharacterObject troop, int excessXp)
	{
		Instance.OnBattleEnded(party, troop, excessXp);
	}

	public static void OnHeroHealedWhileWaiting(Hero hero, int healingAmount)
	{
		Instance.OnHeroHealedWhileWaiting(hero, healingAmount);
	}

	public static void OnRegularTroopHealedWhileWaiting(MobileParty mobileParty, int healedTroopCount, float averageTier)
	{
		Instance.OnRegularTroopHealedWhileWaiting(mobileParty, healedTroopCount, averageTier);
	}

	public static void OnLeadingArmy(MobileParty mobileParty)
	{
		Instance.OnLeadingArmy(mobileParty);
	}

	public static void OnSieging(MobileParty mobileParty)
	{
		Instance.OnSieging(mobileParty);
	}

	public static void OnSiegeEngineBuilt(MobileParty mobileParty, SiegeEngineType siegeEngine)
	{
		Instance.OnSiegeEngineBuilt(mobileParty, siegeEngine);
	}

	public static void OnUpgradeTroops(PartyBase party, CharacterObject troop, CharacterObject upgrade, int numberOfTroops)
	{
		Instance.OnUpgradeTroops(party, troop, upgrade, numberOfTroops);
	}

	public static void OnPersuasionSucceeded(Hero targetHero, SkillObject skill, PersuasionDifficulty difficulty, int argumentDifficultyBonusCoefficient)
	{
		Instance.OnPersuasionSucceeded(targetHero, skill, difficulty, argumentDifficultyBonusCoefficient);
	}

	public static void OnPrisonBreakEnd(Hero prisonerHero, bool isSucceeded)
	{
		Instance.OnPrisonBreakEnd(prisonerHero, isSucceeded);
	}

	public static void OnFoodConsumed(MobileParty mobileParty, bool wasStarving)
	{
		Instance.OnFoodConsumed(mobileParty, wasStarving);
	}

	public static void OnAlleyCleared(Alley alley)
	{
		Instance.OnAlleyCleared(alley);
	}

	public static void OnDailyAlleyTick(Alley alley, Hero alleyLeader)
	{
		Instance.OnDailyAlleyTick(alley, alleyLeader);
	}

	public static void OnBoardGameWonAgainstLord(Hero lord, BoardGameHelper.AIDifficulty difficulty, bool extraXpGain)
	{
		Instance.OnBoardGameWonAgainstLord(lord, difficulty, extraXpGain);
	}

	public static void OnProductionProducedToWarehouse(EquipmentElement production)
	{
		Instance.OnWarehouseProduction(production);
	}

	public static void OnAIPartyLootCasualties(int goldAmount, Hero winnerPartyLeader, PartyBase defeatedParty)
	{
		Instance.OnAIPartyLootCasualties(goldAmount, winnerPartyLeader, defeatedParty);
	}

	public static void OnShipDamaged(Ship ship, float rawDamage, float finalDamage)
	{
		Instance.OnShipDamaged(ship, rawDamage, finalDamage);
	}

	public static void OnShipRepaired(Ship ship, float repairedHitPoints)
	{
		Instance.OnShipRepaired(ship, repairedHitPoints);
	}
}
