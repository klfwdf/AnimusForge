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
	// Token: 0x020003AB RID: 939
	public static class SkillLevelingManager
	{
		// Token: 0x17000CDA RID: 3290
		// (get) Token: 0x06003625 RID: 13861 RVA: 0x000E0FA1 File Offset: 0x000DF1A1
		private static ISkillLevelingManager Instance
		{
			get
			{
				return Campaign.Current.SkillLevelingManager;
			}
		}

		// Token: 0x06003626 RID: 13862 RVA: 0x000E0FB0 File Offset: 0x000DF1B0
		public static void OnCombatHit(CharacterObject affectorCharacter, CharacterObject affectedCharacter, CharacterObject captain, Hero commander, float speedBonusFromMovement, float shotDifficulty, WeaponComponentData affectorWeapon, float hitPointRatio, CombatXpModel.MissionTypeEnum missionType, bool isAffectorMounted, bool isTeamKill, bool isAffectorUnderCommand, float damageAmount, bool isFatal, bool isSiegeEngineHit, bool isHorseCharge, bool isSneakAttack)
		{
			SkillLevelingManager.Instance.OnCombatHit(affectorCharacter, affectedCharacter, captain, commander, speedBonusFromMovement, shotDifficulty, affectorWeapon, hitPointRatio, missionType, isAffectorMounted, isTeamKill, isAffectorUnderCommand, damageAmount, isFatal, isSiegeEngineHit, isHorseCharge, isSneakAttack);
		}

		// Token: 0x06003627 RID: 13863 RVA: 0x000E0FE5 File Offset: 0x000DF1E5
		public static void OnSiegeEngineDestroyed(MobileParty party, SiegeEngineType destroyedSiegeEngine)
		{
			SkillLevelingManager.Instance.OnSiegeEngineDestroyed(party, destroyedSiegeEngine);
		}

		// Token: 0x06003628 RID: 13864 RVA: 0x000E0FF3 File Offset: 0x000DF1F3
		public static void OnWallBreached(MobileParty party)
		{
			SkillLevelingManager.Instance.OnWallBreached(party);
		}

		// Token: 0x06003629 RID: 13865 RVA: 0x000E1000 File Offset: 0x000DF200
		public static void OnSimulationCombatKill(CharacterObject affectorCharacter, CharacterObject affectedCharacter, PartyBase affectorParty, PartyBase commanderParty)
		{
			SkillLevelingManager.Instance.OnSimulationCombatKill(affectorCharacter, affectedCharacter, affectorParty, commanderParty);
		}

		// Token: 0x0600362A RID: 13866 RVA: 0x000E1010 File Offset: 0x000DF210
		public static void OnTradeProfitMade(PartyBase party, int tradeProfit)
		{
			SkillLevelingManager.Instance.OnTradeProfitMade(party, tradeProfit);
		}

		// Token: 0x0600362B RID: 13867 RVA: 0x000E101E File Offset: 0x000DF21E
		public static void OnTradeProfitMade(Hero hero, int tradeProfit)
		{
			SkillLevelingManager.Instance.OnTradeProfitMade(hero, tradeProfit);
		}

		// Token: 0x0600362C RID: 13868 RVA: 0x000E102C File Offset: 0x000DF22C
		public static void OnSettlementProjectFinished(Settlement settlement)
		{
			SkillLevelingManager.Instance.OnSettlementProjectFinished(settlement);
		}

		// Token: 0x0600362D RID: 13869 RVA: 0x000E1039 File Offset: 0x000DF239
		public static void OnSettlementGoverned(Hero governor, Settlement settlement)
		{
			SkillLevelingManager.Instance.OnSettlementGoverned(governor, settlement);
		}

		// Token: 0x0600362E RID: 13870 RVA: 0x000E1047 File Offset: 0x000DF247
		public static void OnInfluenceSpent(Hero hero, float amountSpent)
		{
			SkillLevelingManager.Instance.OnInfluenceSpent(hero, amountSpent);
		}

		// Token: 0x0600362F RID: 13871 RVA: 0x000E1055 File Offset: 0x000DF255
		public static void OnGainRelation(Hero hero, Hero gainedRelationWith, float relationChange, ChangeRelationAction.ChangeRelationDetail detail = ChangeRelationAction.ChangeRelationDetail.Default)
		{
			SkillLevelingManager.Instance.OnGainRelation(hero, gainedRelationWith, relationChange, detail);
		}

		// Token: 0x06003630 RID: 13872 RVA: 0x000E1065 File Offset: 0x000DF265
		public static void OnTroopRecruited(Hero hero, int amount, int tier)
		{
			SkillLevelingManager.Instance.OnTroopRecruited(hero, amount, tier);
		}

		// Token: 0x06003631 RID: 13873 RVA: 0x000E1074 File Offset: 0x000DF274
		public static void OnBribeGiven(int amount)
		{
			SkillLevelingManager.Instance.OnBribeGiven(amount);
		}

		// Token: 0x06003632 RID: 13874 RVA: 0x000E1081 File Offset: 0x000DF281
		public static void OnBanditsRecruited(MobileParty mobileParty, CharacterObject bandit, int count)
		{
			SkillLevelingManager.Instance.OnBanditsRecruited(mobileParty, bandit, count);
		}

		// Token: 0x06003633 RID: 13875 RVA: 0x000E1090 File Offset: 0x000DF290
		public static void OnMainHeroReleasedFromCaptivity(float captivityTime)
		{
			SkillLevelingManager.Instance.OnMainHeroReleasedFromCaptivity(captivityTime);
		}

		// Token: 0x06003634 RID: 13876 RVA: 0x000E109D File Offset: 0x000DF29D
		public static void OnMainHeroTortured()
		{
			SkillLevelingManager.Instance.OnMainHeroTortured();
		}

		// Token: 0x06003635 RID: 13877 RVA: 0x000E10A9 File Offset: 0x000DF2A9
		public static void OnMainHeroDisguised(bool isNotCaught)
		{
			SkillLevelingManager.Instance.OnMainHeroDisguised(isNotCaught);
		}

		// Token: 0x06003636 RID: 13878 RVA: 0x000E10B6 File Offset: 0x000DF2B6
		public static void OnRaid(MobileParty attackerParty, ItemRoster lootedItems)
		{
			SkillLevelingManager.Instance.OnRaid(attackerParty, lootedItems);
		}

		// Token: 0x06003637 RID: 13879 RVA: 0x000E10C4 File Offset: 0x000DF2C4
		public static void OnLoot(MobileParty attackerParty, MobileParty forcedParty, ItemRoster lootedItems, bool attacked)
		{
			SkillLevelingManager.Instance.OnLoot(attackerParty, forcedParty, lootedItems, attacked);
		}

		// Token: 0x06003638 RID: 13880 RVA: 0x000E10D4 File Offset: 0x000DF2D4
		public static void OnForceVolunteers(MobileParty attackerParty, PartyBase forcedParty)
		{
			SkillLevelingManager.Instance.OnForceVolunteers(attackerParty, forcedParty);
		}

		// Token: 0x06003639 RID: 13881 RVA: 0x000E10E2 File Offset: 0x000DF2E2
		public static void OnForceSupplies(MobileParty attackerParty, ItemRoster lootedItems, bool attacked)
		{
			SkillLevelingManager.Instance.OnForceSupplies(attackerParty, lootedItems, attacked);
		}

		// Token: 0x0600363A RID: 13882 RVA: 0x000E10F1 File Offset: 0x000DF2F1
		public static void OnPrisonerSell(MobileParty mobileParty, in TroopRoster prisonerRoster)
		{
			SkillLevelingManager.Instance.OnPrisonerSell(mobileParty, prisonerRoster);
		}

		// Token: 0x0600363B RID: 13883 RVA: 0x000E10FF File Offset: 0x000DF2FF
		public static void OnSurgeryApplied(MobileParty party, bool surgerySuccess, int troopTier)
		{
			SkillLevelingManager.Instance.OnSurgeryApplied(party, surgerySuccess, troopTier);
		}

		// Token: 0x0600363C RID: 13884 RVA: 0x000E110E File Offset: 0x000DF30E
		public static void OnTacticsUsed(MobileParty party, float xp)
		{
			SkillLevelingManager.Instance.OnTacticsUsed(party, xp);
		}

		// Token: 0x0600363D RID: 13885 RVA: 0x000E111C File Offset: 0x000DF31C
		public static void OnHideoutSpotted(MobileParty party, PartyBase spottedParty)
		{
			SkillLevelingManager.Instance.OnHideoutSpotted(party, spottedParty);
		}

		// Token: 0x0600363E RID: 13886 RVA: 0x000E112A File Offset: 0x000DF32A
		public static void OnTrackDetected(Track track)
		{
			SkillLevelingManager.Instance.OnTrackDetected(track);
		}

		// Token: 0x0600363F RID: 13887 RVA: 0x000E1137 File Offset: 0x000DF337
		public static void OnTravelOnFoot(Hero hero, float speed)
		{
			SkillLevelingManager.Instance.OnTravelOnFoot(hero, speed);
		}

		// Token: 0x06003640 RID: 13888 RVA: 0x000E1145 File Offset: 0x000DF345
		public static void OnTravelOnHorse(Hero hero, float speed)
		{
			SkillLevelingManager.Instance.OnTravelOnHorse(hero, speed);
		}

		// Token: 0x06003641 RID: 13889 RVA: 0x000E1153 File Offset: 0x000DF353
		public static void OnTravelOnWater(Hero hero, float speed)
		{
			SkillLevelingManager.Instance.OnTravelOnWater(hero, speed);
		}

		// Token: 0x06003642 RID: 13890 RVA: 0x000E1161 File Offset: 0x000DF361
		public static void OnAIPartiesTravel(Hero hero, bool isCaravanParty, TerrainType currentTerrainType)
		{
			SkillLevelingManager.Instance.OnAIPartiesTravel(hero, isCaravanParty, currentTerrainType);
		}

		// Token: 0x06003643 RID: 13891 RVA: 0x000E1170 File Offset: 0x000DF370
		public static void OnTraverseTerrain(MobileParty mobileParty, TerrainType currentTerrainType)
		{
			SkillLevelingManager.Instance.OnTraverseTerrain(mobileParty, currentTerrainType);
		}

		// Token: 0x06003644 RID: 13892 RVA: 0x000E117E File Offset: 0x000DF37E
		public static void OnBattleEnded(PartyBase party, CharacterObject troop, int excessXp)
		{
			SkillLevelingManager.Instance.OnBattleEnded(party, troop, excessXp);
		}

		// Token: 0x06003645 RID: 13893 RVA: 0x000E118D File Offset: 0x000DF38D
		public static void OnHeroHealedWhileWaiting(Hero hero, int healingAmount)
		{
			SkillLevelingManager.Instance.OnHeroHealedWhileWaiting(hero, healingAmount);
		}

		// Token: 0x06003646 RID: 13894 RVA: 0x000E119B File Offset: 0x000DF39B
		public static void OnRegularTroopHealedWhileWaiting(MobileParty mobileParty, int healedTroopCount, float averageTier)
		{
			SkillLevelingManager.Instance.OnRegularTroopHealedWhileWaiting(mobileParty, healedTroopCount, averageTier);
		}

		// Token: 0x06003647 RID: 13895 RVA: 0x000E11AA File Offset: 0x000DF3AA
		public static void OnLeadingArmy(MobileParty mobileParty)
		{
			SkillLevelingManager.Instance.OnLeadingArmy(mobileParty);
		}

		// Token: 0x06003648 RID: 13896 RVA: 0x000E11B7 File Offset: 0x000DF3B7
		public static void OnSieging(MobileParty mobileParty)
		{
			SkillLevelingManager.Instance.OnSieging(mobileParty);
		}

		// Token: 0x06003649 RID: 13897 RVA: 0x000E11C4 File Offset: 0x000DF3C4
		public static void OnSiegeEngineBuilt(MobileParty mobileParty, SiegeEngineType siegeEngine)
		{
			SkillLevelingManager.Instance.OnSiegeEngineBuilt(mobileParty, siegeEngine);
		}

		// Token: 0x0600364A RID: 13898 RVA: 0x000E11D2 File Offset: 0x000DF3D2
		public static void OnUpgradeTroops(PartyBase party, CharacterObject troop, CharacterObject upgrade, int numberOfTroops)
		{
			SkillLevelingManager.Instance.OnUpgradeTroops(party, troop, upgrade, numberOfTroops);
		}

		// Token: 0x0600364B RID: 13899 RVA: 0x000E11E2 File Offset: 0x000DF3E2
		public static void OnPersuasionSucceeded(Hero targetHero, SkillObject skill, PersuasionDifficulty difficulty, int argumentDifficultyBonusCoefficient)
		{
			SkillLevelingManager.Instance.OnPersuasionSucceeded(targetHero, skill, difficulty, argumentDifficultyBonusCoefficient);
		}

		// Token: 0x0600364C RID: 13900 RVA: 0x000E11F2 File Offset: 0x000DF3F2
		public static void OnPrisonBreakEnd(Hero prisonerHero, bool isSucceeded)
		{
			SkillLevelingManager.Instance.OnPrisonBreakEnd(prisonerHero, isSucceeded);
		}

		// Token: 0x0600364D RID: 13901 RVA: 0x000E1200 File Offset: 0x000DF400
		public static void OnFoodConsumed(MobileParty mobileParty, bool wasStarving)
		{
			SkillLevelingManager.Instance.OnFoodConsumed(mobileParty, wasStarving);
		}

		// Token: 0x0600364E RID: 13902 RVA: 0x000E120E File Offset: 0x000DF40E
		public static void OnAlleyCleared(Alley alley)
		{
			SkillLevelingManager.Instance.OnAlleyCleared(alley);
		}

		// Token: 0x0600364F RID: 13903 RVA: 0x000E121B File Offset: 0x000DF41B
		public static void OnDailyAlleyTick(Alley alley, Hero alleyLeader)
		{
			SkillLevelingManager.Instance.OnDailyAlleyTick(alley, alleyLeader);
		}

		// Token: 0x06003650 RID: 13904 RVA: 0x000E1229 File Offset: 0x000DF429
		public static void OnBoardGameWonAgainstLord(Hero lord, BoardGameHelper.AIDifficulty difficulty, bool extraXpGain)
		{
			SkillLevelingManager.Instance.OnBoardGameWonAgainstLord(lord, difficulty, extraXpGain);
		}

		// Token: 0x06003651 RID: 13905 RVA: 0x000E1238 File Offset: 0x000DF438
		public static void OnProductionProducedToWarehouse(EquipmentElement production)
		{
			SkillLevelingManager.Instance.OnWarehouseProduction(production);
		}

		// Token: 0x06003652 RID: 13906 RVA: 0x000E1245 File Offset: 0x000DF445
		public static void OnAIPartyLootCasualties(int goldAmount, Hero winnerPartyLeader, PartyBase defeatedParty)
		{
			SkillLevelingManager.Instance.OnAIPartyLootCasualties(goldAmount, winnerPartyLeader, defeatedParty);
		}

		// Token: 0x06003653 RID: 13907 RVA: 0x000E1254 File Offset: 0x000DF454
		public static void OnShipDamaged(Ship ship, float rawDamage, float finalDamage)
		{
			SkillLevelingManager.Instance.OnShipDamaged(ship, rawDamage, finalDamage);
		}

		// Token: 0x06003654 RID: 13908 RVA: 0x000E1263 File Offset: 0x000DF463
		public static void OnShipRepaired(Ship ship, float repairedHitPoints)
		{
			SkillLevelingManager.Instance.OnShipRepaired(ship, repairedHitPoints);
		}
	}
}
