using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.CharacterDevelopment;

public class NavalSkillLevellingManager : ISkillLevelingManager
{
	private readonly DefaultSkillLevelingManager _defaultSkillLevelingManager = new DefaultSkillLevelingManager();

	private const float NavalAutoBattleXpCoefficient = 0.02f;

	public void OnCombatHit(CharacterObject affectorCharacter, CharacterObject affectedCharacter, CharacterObject captain, Hero commander, float speedBonusFromMovement, float shotDifficulty, WeaponComponentData affectorWeapon, float hitPointRatio, CombatXpModel.MissionTypeEnum missionType, bool isAffectorMounted, bool isTeamKill, bool isAffectorUnderCommand, float damageAmount, bool isFatal, bool isSiegeEngineHit, bool isHorseCharge, bool isSneakAttack)
	{
		if (Mission.Current.IsNavalBattle && affectorCharacter.IsHero && !isTeamKill)
		{
			Hero heroObject = affectorCharacter.HeroObject;
			float f = Campaign.Current.Models.CombatXpModel.GetXpFromHit(heroObject.CharacterObject, captain, affectedCharacter, heroObject.PartyBelongedTo?.Party, (int)damageAmount, isFatal, missionType).RoundedResultNumber;
			heroObject.AddSkillXp(NavalSkills.Mariner, MBRandom.RoundRandomized(f));
		}
		_defaultSkillLevelingManager.OnCombatHit(affectorCharacter, affectedCharacter, captain, commander, speedBonusFromMovement, shotDifficulty, affectorWeapon, hitPointRatio, missionType, isAffectorMounted, isTeamKill, isAffectorUnderCommand, damageAmount, isFatal, isSiegeEngineHit, isHorseCharge, isSneakAttack);
	}

	public void OnSiegeEngineDestroyed(MobileParty party, SiegeEngineType destroyedSiegeEngine)
	{
		_defaultSkillLevelingManager.OnSiegeEngineDestroyed(party, destroyedSiegeEngine);
	}

	public void OnSimulationCombatKill(CharacterObject affectorCharacter, CharacterObject affectedCharacter, PartyBase affectorParty, PartyBase commanderParty)
	{
		_defaultSkillLevelingManager.OnSimulationCombatKill(affectorCharacter, affectedCharacter, affectorParty, commanderParty);
		int xpReward = Campaign.Current.Models.PartyTrainingModel.GetXpReward(affectedCharacter);
		if (commanderParty != null && commanderParty.IsMobile && commanderParty.MapEvent.IsNavalMapEvent && commanderParty.LeaderHero != null && commanderParty.LeaderHero != affectedCharacter.HeroObject)
		{
			OnPartySkillExercised(commanderParty.MobileParty, NavalSkills.Mariner, (float)xpReward * 0.02f);
		}
	}

	public void OnTradeProfitMade(PartyBase party, int tradeProfit)
	{
		_defaultSkillLevelingManager.OnTradeProfitMade(party, tradeProfit);
	}

	public void OnTradeProfitMade(Hero hero, int tradeProfit)
	{
		_defaultSkillLevelingManager.OnTradeProfitMade(hero, tradeProfit);
	}

	public void OnSettlementProjectFinished(Settlement settlement)
	{
		_defaultSkillLevelingManager.OnSettlementProjectFinished(settlement);
	}

	public void OnSettlementGoverned(Hero governor, Settlement settlement)
	{
		_defaultSkillLevelingManager.OnSettlementGoverned(governor, settlement);
	}

	public void OnInfluenceSpent(Hero hero, float amountSpent)
	{
		_defaultSkillLevelingManager.OnInfluenceSpent(hero, amountSpent);
	}

	public void OnGainRelation(Hero hero, Hero gainedRelationWith, float relationChange, ChangeRelationAction.ChangeRelationDetail detail = ChangeRelationAction.ChangeRelationDetail.Default)
	{
		_defaultSkillLevelingManager.OnGainRelation(hero, gainedRelationWith, relationChange, detail);
	}

	public void OnTroopRecruited(Hero hero, int amount, int tier)
	{
		_defaultSkillLevelingManager.OnTroopRecruited(hero, amount, tier);
	}

	public void OnBribeGiven(int amount)
	{
		_defaultSkillLevelingManager.OnBribeGiven(amount);
	}

	public void OnWarehouseProduction(EquipmentElement production)
	{
		_defaultSkillLevelingManager.OnWarehouseProduction(production);
	}

	public void OnAIPartyLootCasualties(int goldAmount, Hero winnerPartyLeader, PartyBase defeatedParty)
	{
		_defaultSkillLevelingManager.OnAIPartyLootCasualties(goldAmount, winnerPartyLeader, defeatedParty);
	}

	public void OnBanditsRecruited(MobileParty mobileParty, CharacterObject bandit, int count)
	{
		_defaultSkillLevelingManager.OnBanditsRecruited(mobileParty, bandit, count);
	}

	public void OnMainHeroReleasedFromCaptivity(float captivityTime)
	{
		_defaultSkillLevelingManager.OnMainHeroReleasedFromCaptivity(captivityTime);
	}

	public void OnMainHeroTortured()
	{
		_defaultSkillLevelingManager.OnMainHeroTortured();
	}

	public void OnMainHeroDisguised(bool isNotCaught)
	{
		_defaultSkillLevelingManager.OnMainHeroDisguised(isNotCaught);
	}

	public void OnRaid(MobileParty attackerParty, ItemRoster lootedItems)
	{
		_defaultSkillLevelingManager.OnRaid(attackerParty, lootedItems);
	}

	public void OnLoot(MobileParty attackerParty, MobileParty forcedParty, ItemRoster lootedItems, bool attacked)
	{
		_defaultSkillLevelingManager.OnLoot(attackerParty, forcedParty, lootedItems, attacked);
	}

	public void OnPrisonerSell(MobileParty mobileParty, in TroopRoster prisonerRoster)
	{
		_defaultSkillLevelingManager.OnPrisonerSell(mobileParty, in prisonerRoster);
	}

	public void OnSurgeryApplied(MobileParty party, bool surgerySuccess, int troopTier)
	{
		_defaultSkillLevelingManager.OnSurgeryApplied(party, surgerySuccess, troopTier);
	}

	public void OnTacticsUsed(MobileParty party, float xp)
	{
		_defaultSkillLevelingManager.OnTacticsUsed(party, xp);
	}

	public void OnHideoutSpotted(MobileParty party, PartyBase spottedParty)
	{
		_defaultSkillLevelingManager.OnHideoutSpotted(party, spottedParty);
	}

	public void OnTrackDetected(Track track)
	{
		_defaultSkillLevelingManager.OnTrackDetected(track);
	}

	public void OnTravelOnFoot(Hero hero)
	{
		_defaultSkillLevelingManager.OnTravelOnFoot(hero);
	}

	public void OnTravelOnHorse(Hero hero)
	{
		_defaultSkillLevelingManager.OnTravelOnHorse(hero);
	}

	public void OnHeroHealedWhileWaiting(Hero hero, int healingAmount)
	{
		_defaultSkillLevelingManager.OnHeroHealedWhileWaiting(hero, healingAmount);
	}

	public void OnRegularTroopHealedWhileWaiting(MobileParty mobileParty, int healedTroopCount, float averageTier)
	{
		_defaultSkillLevelingManager.OnRegularTroopHealedWhileWaiting(mobileParty, healedTroopCount, averageTier);
	}

	public void OnLeadingArmy(MobileParty mobileParty)
	{
		_defaultSkillLevelingManager.OnLeadingArmy(mobileParty);
	}

	public void OnSieging(MobileParty mobileParty)
	{
		_defaultSkillLevelingManager.OnSieging(mobileParty);
	}

	public void OnSiegeEngineBuilt(MobileParty mobileParty, SiegeEngineType siegeEngine)
	{
		_defaultSkillLevelingManager.OnSiegeEngineBuilt(mobileParty, siegeEngine);
	}

	public void OnUpgradeTroops(PartyBase party, CharacterObject troop, CharacterObject upgrade, int numberOfTroops)
	{
		_defaultSkillLevelingManager.OnUpgradeTroops(party, troop, upgrade, numberOfTroops);
	}

	public void OnPersuasionSucceeded(Hero targetHero, SkillObject skill, PersuasionDifficulty difficulty, int argumentDifficultyBonusCoefficient)
	{
		_defaultSkillLevelingManager.OnPersuasionSucceeded(targetHero, skill, difficulty, argumentDifficultyBonusCoefficient);
	}

	public void OnPrisonBreakEnd(Hero prisonerHero, bool isSucceeded)
	{
		_defaultSkillLevelingManager.OnPrisonBreakEnd(prisonerHero, isSucceeded);
	}

	public void OnWallBreached(MobileParty party)
	{
		_defaultSkillLevelingManager.OnWallBreached(party);
	}

	public void OnForceVolunteers(MobileParty attackerParty, PartyBase forcedParty)
	{
		_defaultSkillLevelingManager.OnForceVolunteers(attackerParty, forcedParty);
	}

	public void OnForceSupplies(MobileParty attackerParty, ItemRoster lootedItems, bool attacked)
	{
		_defaultSkillLevelingManager.OnForceSupplies(attackerParty, lootedItems, attacked);
	}

	public void OnAIPartiesTravel(Hero hero, bool isCaravanParty, TerrainType currentTerrainType)
	{
		_defaultSkillLevelingManager.OnAIPartiesTravel(hero, isCaravanParty, currentTerrainType);
	}

	public void OnTraverseTerrain(MobileParty mobileParty, TerrainType currentTerrainType)
	{
		_defaultSkillLevelingManager.OnTraverseTerrain(mobileParty, currentTerrainType);
	}

	public void OnBattleEnded(PartyBase party, CharacterObject troop, int excessXp)
	{
		_defaultSkillLevelingManager.OnBattleEnded(party, troop, excessXp);
	}

	public void OnFoodConsumed(MobileParty mobileParty, bool wasStarving)
	{
		_defaultSkillLevelingManager.OnFoodConsumed(mobileParty, wasStarving);
	}

	public void OnAlleyCleared(Alley alley)
	{
		_defaultSkillLevelingManager.OnAlleyCleared(alley);
	}

	public void OnDailyAlleyTick(Alley alley, Hero alleyLeader)
	{
		_defaultSkillLevelingManager.OnDailyAlleyTick(alley, alleyLeader);
	}

	public void OnBoardGameWonAgainstLord(Hero lord, BoardGameHelper.AIDifficulty difficulty, bool extraXpGain)
	{
		_defaultSkillLevelingManager.OnBoardGameWonAgainstLord(lord, difficulty, extraXpGain);
	}

	public void OnShipDamaged(Ship ship, float rawDamage, float finalDamage)
	{
		_defaultSkillLevelingManager.OnShipDamaged(ship, rawDamage, finalDamage);
	}

	public void OnShipRepaired(Ship ship, float repairedHitPoints)
	{
		float num = repairedHitPoints * 0.2f;
		if (ship.Owner != null && ship.Owner.IsMobile && num > 0f)
		{
			OnPartySkillExercised(ship.Owner.MobileParty, NavalSkills.Boatswain, num, PartyRole.FirstMate);
		}
		_defaultSkillLevelingManager.OnShipRepaired(ship, repairedHitPoints);
	}

	public void OnHighMorale(MobileParty mobileParty)
	{
		if (mobileParty.IsCurrentlyAtSea && !mobileParty.IsInNavalAutoTravel)
		{
			float skillXp = (mobileParty.Morale - Campaign.Current.Models.PartyMoraleModel.HighMoraleValue + 1f) * (float)mobileParty.MemberRoster.TotalManCount * 0.01f;
			OnPartySkillExercised(mobileParty, NavalSkills.Boatswain, skillXp, PartyRole.FirstMate);
		}
		_defaultSkillLevelingManager.OnHighMorale(mobileParty);
	}

	public void OnHideoutMissionEnd(bool isSucceeded)
	{
		_defaultSkillLevelingManager.OnHideoutMissionEnd(isSucceeded);
	}

	public void OnHideoutClearedAsGhost()
	{
		_defaultSkillLevelingManager.OnHideoutClearedAsGhost();
	}

	public void OnTravelOnWater(MobileParty party)
	{
		float speed = party.Speed;
		int count = party.Ships.Count;
		float num = 0f;
		int num2 = 0;
		if (party.Army != null)
		{
			num = ((party.Army.LeaderParty == party) ? 3f : 1.5f);
			num2 = party.Army.LeaderParty.AttachedParties.SumQ((MobileParty x) => x.Ships.Count) + party.Army.LeaderParty.Ships.Count - count;
		}
		float num3 = 1f + (1.5f * MathF.Sqrt(count - 1) + num * MathF.Sqrt(num2));
		int num4 = MBRandom.RoundRandomized(6f * speed * num3);
		OnPartySkillExercised(party, NavalSkills.Shipmaster, num4, PartyRole.Navigator);
		_defaultSkillLevelingManager.OnTravelOnWater(party);
	}

	private static void OnPartySkillExercised(MobileParty party, SkillObject skill, float skillXp, PartyRole partyRole = PartyRole.PartyLeader)
	{
		party.GetEffectiveRoleHolder(partyRole)?.AddSkillXp(skill, skillXp);
	}

	void ISkillLevelingManager.OnPrisonerSell(MobileParty mobileParty, in TroopRoster prisonerRoster)
	{
		OnPrisonerSell(mobileParty, in prisonerRoster);
	}
}
