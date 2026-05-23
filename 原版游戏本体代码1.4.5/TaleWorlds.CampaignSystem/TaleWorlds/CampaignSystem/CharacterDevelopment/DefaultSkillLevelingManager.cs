using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CharacterDevelopment;

public class DefaultSkillLevelingManager : ISkillLevelingManager
{
	private const float TacticsXpCoefficient = 0.02f;

	private const int RogueryXpGainOnSneakAttack = 78;

	public void OnCombatHit(CharacterObject affectorCharacter, CharacterObject affectedCharacter, CharacterObject captain, Hero commander, float speedBonusFromMovement, float shotDifficulty, WeaponComponentData affectorWeapon, float hitPointRatio, CombatXpModel.MissionTypeEnum missionType, bool isAffectorMounted, bool isTeamKill, bool isAffectorUnderCommand, float damageAmount, bool isFatal, bool isSiegeEngineHit, bool isHorseCharge, bool isSneakAttack)
	{
		if (isTeamKill)
		{
			return;
		}
		ExplainedNumber explainedNumber = new ExplainedNumber(1f);
		if (affectorCharacter.IsHero)
		{
			Hero heroObject = affectorCharacter.HeroObject;
			explainedNumber = new ExplainedNumber(Campaign.Current.Models.CombatXpModel.GetXpFromHit(heroObject.CharacterObject, captain, affectedCharacter, heroObject.PartyBelongedTo?.Party, (int)damageAmount, isFatal, missionType).ResultNumber);
			SkillObject skillObject = null;
			if (affectorWeapon != null)
			{
				skillObject = Campaign.Current.Models.CombatXpModel.GetSkillForWeapon(affectorWeapon, isSiegeEngineHit);
				float num = ((skillObject == DefaultSkills.Bow) ? 0.5f : 1f);
				if (shotDifficulty > 0f)
				{
					explainedNumber.AddFactor(num * Campaign.Current.Models.CombatXpModel.GetXpMultiplierFromShotDifficulty(shotDifficulty));
				}
			}
			else
			{
				skillObject = (isHorseCharge ? DefaultSkills.Riding : DefaultSkills.Athletics);
			}
			heroObject.AddSkillXp(skillObject, MBRandom.RoundRandomized(explainedNumber.RoundedResultNumber));
			if (!isSiegeEngineHit && !isHorseCharge)
			{
				float num2 = shotDifficulty * 0.15f;
				if (isAffectorMounted)
				{
					float num3 = 0.5f;
					if (num2 > 0f)
					{
						num3 += num2;
					}
					if (speedBonusFromMovement > 0f)
					{
						num3 *= 1f + speedBonusFromMovement;
					}
					if (num3 > 0f)
					{
						OnGainingRidingExperience(heroObject, MBRandom.RoundRandomized(num3 * (float)explainedNumber.RoundedResultNumber), heroObject.CharacterObject.Equipment.Horse.Item);
					}
				}
				else
				{
					float num4 = 1f;
					if (num2 > 0f)
					{
						num4 += num2;
					}
					if (speedBonusFromMovement > 0f)
					{
						num4 += 1.5f * speedBonusFromMovement;
					}
					if (num4 > 0f)
					{
						heroObject.AddSkillXp(DefaultSkills.Athletics, MBRandom.RoundRandomized(num4 * explainedNumber.ResultNumber));
					}
				}
			}
			if (isSneakAttack)
			{
				heroObject.AddSkillXp(DefaultSkills.Roguery, 78f);
			}
		}
		if (commander != null && commander != affectorCharacter.HeroObject && commander.PartyBelongedTo != null)
		{
			OnTacticsUsed(commander.PartyBelongedTo, MathF.Ceiling(0.02f * (float)explainedNumber.RoundedResultNumber));
		}
	}

	public void OnSiegeEngineDestroyed(MobileParty party, SiegeEngineType destroyedSiegeEngine)
	{
		if (party?.EffectiveEngineer != null)
		{
			float skillXp = (float)destroyedSiegeEngine.ManDayCost * 20f;
			OnPartySkillExercised(party, DefaultSkills.Engineering, skillXp, PartyRole.Engineer);
		}
	}

	public void OnSimulationCombatKill(CharacterObject affectorCharacter, CharacterObject affectedCharacter, PartyBase affectorParty, PartyBase commanderParty)
	{
		int xpReward = Campaign.Current.Models.PartyTrainingModel.GetXpReward(affectedCharacter);
		if (affectorCharacter.IsHero)
		{
			ItemObject defaultWeapon = CharacterHelper.GetDefaultWeapon(affectorCharacter);
			Hero heroObject = affectorCharacter.HeroObject;
			if (defaultWeapon != null)
			{
				SkillObject skillForWeapon = Campaign.Current.Models.CombatXpModel.GetSkillForWeapon(defaultWeapon.GetWeaponWithUsageIndex(0), isSiegeEngineHit: false);
				heroObject.AddSkillXp(skillForWeapon, xpReward);
			}
			if (affectorCharacter.IsMounted)
			{
				float f = (float)xpReward * 0.3f;
				OnGainingRidingExperience(heroObject, MBRandom.RoundRandomized(f), heroObject.CharacterObject.Equipment.Horse.Item);
			}
			else
			{
				float f2 = (float)xpReward * 0.3f;
				heroObject.AddSkillXp(DefaultSkills.Athletics, MBRandom.RoundRandomized(f2));
			}
		}
		if (commanderParty != null && commanderParty.IsMobile && !commanderParty.MapEvent.IsNavalMapEvent && commanderParty.LeaderHero != null && commanderParty.LeaderHero != affectedCharacter.HeroObject)
		{
			OnTacticsUsed(commanderParty.MobileParty, MathF.Ceiling(0.02f * (float)xpReward));
		}
	}

	public void OnTradeProfitMade(PartyBase party, int tradeProfit)
	{
		if (tradeProfit > 0)
		{
			float skillXp = (float)tradeProfit * 0.5f;
			OnPartySkillExercised(party.MobileParty, DefaultSkills.Trade, skillXp);
		}
	}

	public void OnTradeProfitMade(Hero hero, int tradeProfit)
	{
		if (tradeProfit > 0)
		{
			float skillXp = (float)tradeProfit * 0.5f;
			OnPersonalSkillExercised(hero, DefaultSkills.Trade, skillXp, hero == Hero.MainHero);
		}
	}

	public void OnSettlementProjectFinished(Settlement settlement)
	{
		OnSettlementSkillExercised(settlement, DefaultSkills.Steward, 1000f);
	}

	public void OnSettlementGoverned(Hero governor, Settlement settlement)
	{
		float prosperityChange = settlement.Town.ProsperityChange;
		if (prosperityChange > 0f)
		{
			float skillXp = prosperityChange * 30f;
			OnPersonalSkillExercised(governor, DefaultSkills.Steward, skillXp);
		}
	}

	public void OnInfluenceSpent(Hero hero, float amountSpent)
	{
		if (hero.PartyBelongedTo != null)
		{
			float skillXp = 10f * amountSpent;
			OnPartySkillExercised(hero.PartyBelongedTo, DefaultSkills.Steward, skillXp);
		}
	}

	public void OnGainRelation(Hero hero, Hero gainedRelationWith, float relationChange, ChangeRelationAction.ChangeRelationDetail detail = ChangeRelationAction.ChangeRelationDetail.Default)
	{
		if ((hero.PartyBelongedTo != null || detail == ChangeRelationAction.ChangeRelationDetail.Emissary) && !(relationChange <= 0f))
		{
			int charmExperienceFromRelationGain = Campaign.Current.Models.DiplomacyModel.GetCharmExperienceFromRelationGain(gainedRelationWith, relationChange, detail);
			if (hero.PartyBelongedTo != null)
			{
				OnPartySkillExercised(hero.PartyBelongedTo, DefaultSkills.Charm, charmExperienceFromRelationGain);
			}
			else
			{
				OnPersonalSkillExercised(hero, DefaultSkills.Charm, charmExperienceFromRelationGain);
			}
		}
	}

	public void OnTroopRecruited(Hero hero, int amount, int tier)
	{
		if (amount > 0)
		{
			int num = amount * tier * 2;
			OnPersonalSkillExercised(hero, DefaultSkills.Leadership, num);
		}
	}

	public void OnBribeGiven(int amount)
	{
		if (amount > 0)
		{
			float skillXp = (float)amount * 0.1f;
			OnPartySkillExercised(MobileParty.MainParty, DefaultSkills.Roguery, skillXp);
		}
	}

	public void OnBanditsRecruited(MobileParty mobileParty, CharacterObject bandit, int count)
	{
		if (count > 0)
		{
			OnPersonalSkillExercised(mobileParty.LeaderHero, DefaultSkills.Roguery, count * 2 * bandit.Tier);
		}
	}

	public void OnMainHeroReleasedFromCaptivity(float captivityTime)
	{
		float skillXp = captivityTime * 0.5f;
		OnPersonalSkillExercised(Hero.MainHero, DefaultSkills.Roguery, skillXp);
	}

	public void OnMainHeroTortured()
	{
		float skillXp = MBRandom.RandomFloatRanged(50f, 100f);
		OnPersonalSkillExercised(Hero.MainHero, DefaultSkills.Roguery, skillXp);
	}

	public void OnMainHeroDisguised(bool isNotCaught)
	{
		float skillXp = (isNotCaught ? MBRandom.RandomFloatRanged(10f, 25f) : MBRandom.RandomFloatRanged(1f, 10f));
		OnPartySkillExercised(MobileParty.MainParty, DefaultSkills.Roguery, skillXp);
	}

	public void OnRaid(MobileParty attackerParty, ItemRoster lootedItems)
	{
		if (attackerParty.LeaderHero != null)
		{
			float skillXp = (float)lootedItems.TradeGoodsTotalValue * 0.5f + (float)(lootedItems.NumberOfMounts * 100) + (float)(lootedItems.NumberOfLivestockAnimals * 25) + (float)(lootedItems.NumberOfPackAnimals * 25);
			OnPersonalSkillExercised(attackerParty.LeaderHero, DefaultSkills.Roguery, skillXp);
		}
	}

	public void OnLoot(MobileParty attackerParty, MobileParty forcedParty, ItemRoster lootedItems, bool attacked)
	{
		if (attackerParty.LeaderHero != null)
		{
			float num = 0f;
			if (forcedParty.IsVillager)
			{
				num = (attacked ? 0.75f : 0.5f);
			}
			else if (forcedParty.IsCaravan)
			{
				num = (attacked ? 0.15f : 0.1f);
			}
			float skillXp = (float)(lootedItems.TradeGoodsTotalValue + lootedItems.NumberOfMounts * 200 + lootedItems.NumberOfLivestockAnimals * 50 + lootedItems.NumberOfPackAnimals * 50) * num;
			OnPersonalSkillExercised(attackerParty.LeaderHero, DefaultSkills.Roguery, skillXp);
		}
	}

	public void OnPrisonerSell(MobileParty mobileParty, in TroopRoster prisonerRoster)
	{
		int num = 0;
		for (int i = 0; i < prisonerRoster.Count; i++)
		{
			num += prisonerRoster.data[i].Character.Tier * prisonerRoster.data[i].Number;
		}
		int num2 = num * 2;
		OnPartySkillExercised(mobileParty, DefaultSkills.Roguery, num2);
	}

	public void OnSurgeryApplied(MobileParty party, bool surgerySuccess, int troopTier)
	{
		float skillXp = (surgerySuccess ? (10 * troopTier) : (5 * troopTier));
		OnPartySkillExercised(party, DefaultSkills.Medicine, skillXp, PartyRole.Surgeon);
	}

	public void OnTacticsUsed(MobileParty party, float xp)
	{
		if (xp > 0f)
		{
			OnPartySkillExercised(party, DefaultSkills.Tactics, xp);
		}
	}

	public void OnHideoutSpotted(MobileParty party, PartyBase spottedParty)
	{
		OnPartySkillExercised(party, DefaultSkills.Scouting, 100f, PartyRole.Scout);
	}

	public void OnTrackDetected(Track track)
	{
		float skillFromTrackDetected = Campaign.Current.Models.MapTrackModel.GetSkillFromTrackDetected(track);
		OnPartySkillExercised(MobileParty.MainParty, DefaultSkills.Scouting, skillFromTrackDetected, PartyRole.Scout);
	}

	public void OnTravelOnFoot(Hero hero, float speed)
	{
		hero.AddSkillXp(DefaultSkills.Athletics, MBRandom.RoundRandomized(0.2f * speed) + 1);
	}

	public void OnTravelOnHorse(Hero hero, float speed)
	{
		ItemObject item = hero.CharacterObject.Equipment.Horse.Item;
		OnGainingRidingExperience(hero, MBRandom.RoundRandomized(0.3f * speed), item);
	}

	public void OnHeroHealedWhileWaiting(Hero hero, int healingAmount)
	{
		if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.EffectiveSurgeon != null)
		{
			float num = Campaign.Current.Models.PartyHealingModel.GetSkillXpFromHealingTroop(hero.PartyBelongedTo.Party);
			float num2 = ((hero.PartyBelongedTo.CurrentSettlement != null && !hero.PartyBelongedTo.CurrentSettlement.IsCastle) ? 0.2f : 0.1f);
			num *= (float)healingAmount * num2 * (1f + (float)hero.PartyBelongedTo.EffectiveSurgeon.Level * 0.1f);
			OnPartySkillExercised(hero.PartyBelongedTo, DefaultSkills.Medicine, num, PartyRole.Surgeon);
		}
	}

	public void OnRegularTroopHealedWhileWaiting(MobileParty mobileParty, int healedTroopCount, float averageTier)
	{
		float num = (float)(Campaign.Current.Models.PartyHealingModel.GetSkillXpFromHealingTroop(mobileParty.Party) * healedTroopCount) * averageTier;
		float num2 = ((mobileParty.CurrentSettlement != null && !mobileParty.CurrentSettlement.IsCastle) ? 2f : 1f);
		num *= num2;
		OnPartySkillExercised(mobileParty, DefaultSkills.Medicine, num, PartyRole.Surgeon);
	}

	public void OnLeadingArmy(MobileParty mobileParty)
	{
		float skillXp = (mobileParty.Army?.EstimatedStrength ?? mobileParty.Party.EstimatedStrength) * 0.0004f * mobileParty.Army.Morale;
		OnPartySkillExercised(mobileParty, DefaultSkills.Leadership, skillXp);
	}

	public void OnSieging(MobileParty mobileParty)
	{
		int num = mobileParty.MemberRoster.TotalManCount;
		if (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty)
		{
			foreach (MobileParty party in mobileParty.Army.Parties)
			{
				if (party != mobileParty)
				{
					num += party.MemberRoster.TotalManCount;
				}
			}
		}
		float skillXp = 0.25f * MathF.Sqrt(num);
		OnPartySkillExercised(mobileParty, DefaultSkills.Engineering, skillXp, PartyRole.Engineer);
	}

	public void OnSiegeEngineBuilt(MobileParty mobileParty, SiegeEngineType siegeEngine)
	{
		float skillXp = 30f + 2f * (float)siegeEngine.Difficulty;
		OnPartySkillExercised(mobileParty, DefaultSkills.Engineering, skillXp, PartyRole.Engineer);
	}

	public void OnUpgradeTroops(PartyBase party, CharacterObject troop, CharacterObject upgrade, int numberOfTroops)
	{
		Hero hero = party.LeaderHero ?? party.Owner;
		if (hero != null)
		{
			SkillObject skill = DefaultSkills.Leadership;
			float num = 0.025f;
			if (troop.Occupation == Occupation.Bandit)
			{
				skill = DefaultSkills.Roguery;
				num = 0.05f;
			}
			float xpAmount = (float)Campaign.Current.Models.PartyTroopUpgradeModel.GetXpCostForUpgrade(party, troop, upgrade) * num * (float)numberOfTroops;
			hero.AddSkillXp(skill, xpAmount);
		}
	}

	public void OnPersuasionSucceeded(Hero targetHero, SkillObject skill, PersuasionDifficulty difficulty, int argumentDifficultyBonusCoefficient)
	{
		float num = Campaign.Current.Models.PersuasionModel.GetSkillXpFromPersuasion(difficulty, argumentDifficultyBonusCoefficient);
		if (num > 0f)
		{
			targetHero.AddSkillXp(skill, num);
		}
	}

	public void OnPrisonBreakEnd(Hero prisonerHero, bool isSucceeded)
	{
		float rogueryRewardOnPrisonBreak = Campaign.Current.Models.PrisonBreakModel.GetRogueryRewardOnPrisonBreak(prisonerHero, isSucceeded);
		if (rogueryRewardOnPrisonBreak > 0f)
		{
			Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, rogueryRewardOnPrisonBreak);
		}
	}

	public void OnWallBreached(MobileParty party)
	{
		if (party?.EffectiveEngineer != null)
		{
			OnPartySkillExercised(party, DefaultSkills.Engineering, 250f, PartyRole.Engineer);
		}
	}

	public void OnForceVolunteers(MobileParty attackerParty, PartyBase forcedParty)
	{
		if (attackerParty.LeaderHero != null)
		{
			int num = MathF.Ceiling(forcedParty.Settlement.Village.Hearth / 10f);
			OnPersonalSkillExercised(attackerParty.LeaderHero, DefaultSkills.Roguery, num);
		}
	}

	public void OnForceSupplies(MobileParty attackerParty, ItemRoster lootedItems, bool attacked)
	{
		if (attackerParty.LeaderHero != null)
		{
			float num = (attacked ? 0.75f : 0.5f);
			float skillXp = (float)(lootedItems.TradeGoodsTotalValue + lootedItems.NumberOfMounts * 200 + lootedItems.NumberOfLivestockAnimals * 50 + lootedItems.NumberOfPackAnimals * 50) * num;
			OnPersonalSkillExercised(attackerParty.LeaderHero, DefaultSkills.Roguery, skillXp);
		}
	}

	public void OnAIPartiesTravel(Hero hero, bool isCaravanParty, TerrainType currentTerrainType)
	{
		int num = ((currentTerrainType == TerrainType.Forest) ? MBRandom.RoundRandomized(5f) : MBRandom.RoundRandomized(3f));
		hero.AddSkillXp(DefaultSkills.Scouting, isCaravanParty ? ((float)num / 2f) : ((float)num));
	}

	public void OnTraverseTerrain(MobileParty mobileParty, TerrainType currentTerrainType)
	{
		float num = 0f;
		float lastCalculatedSpeed = mobileParty._lastCalculatedSpeed;
		if (lastCalculatedSpeed > 1f)
		{
			bool flag = currentTerrainType == TerrainType.Desert || currentTerrainType == TerrainType.Dune || currentTerrainType == TerrainType.Forest || currentTerrainType == TerrainType.Snow;
			num = lastCalculatedSpeed * (1f + MathF.Pow(mobileParty.MemberRoster.TotalManCount, 0.66f)) * (flag ? 0.25f : 0.15f);
		}
		if (mobileParty.IsCaravan)
		{
			num *= 0.5f;
		}
		if (num >= 5f)
		{
			OnPartySkillExercised(mobileParty, DefaultSkills.Scouting, num, PartyRole.Scout);
		}
	}

	public void OnBattleEnded(PartyBase party, CharacterObject troop, int excessXp)
	{
		Hero obj = party.LeaderHero ?? party.Owner;
		float num = 0.025f;
		SkillObject skill = DefaultSkills.Leadership;
		if (troop.Occupation == Occupation.Bandit)
		{
			num = 0.05f;
			skill = DefaultSkills.Roguery;
		}
		float xpAmount = (float)excessXp * num;
		obj.AddSkillXp(skill, xpAmount);
	}

	public void OnFoodConsumed(MobileParty mobileParty, bool wasStarving)
	{
		if (!wasStarving && mobileParty.ItemRoster.FoodVariety > 3 && mobileParty.EffectiveQuartermaster != null)
		{
			float skillXp = (float)MathF.Round((0f - mobileParty.BaseFoodChange) * 100f) * ((float)mobileParty.ItemRoster.FoodVariety - 2f) / 3f;
			OnPartySkillExercised(mobileParty, DefaultSkills.Steward, skillXp, PartyRole.Quartermaster);
		}
	}

	public void OnAlleyCleared(Alley alley)
	{
		Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, Campaign.Current.Models.AlleyModel.GetInitialXpGainForMainHero());
	}

	public void OnDailyAlleyTick(Alley alley, Hero alleyLeader)
	{
		Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, Campaign.Current.Models.AlleyModel.GetDailyXpGainForMainHero());
		if (alleyLeader != null && !alleyLeader.IsDead)
		{
			alleyLeader.AddSkillXp(DefaultSkills.Roguery, Campaign.Current.Models.AlleyModel.GetDailyXpGainForAssignedClanMember(alleyLeader));
		}
	}

	public void OnBoardGameWonAgainstLord(Hero lord, BoardGameHelper.AIDifficulty difficulty, bool extraXpGain)
	{
		switch (difficulty)
		{
		case BoardGameHelper.AIDifficulty.Easy:
			Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 20f);
			break;
		case BoardGameHelper.AIDifficulty.Normal:
			Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 50f);
			break;
		case BoardGameHelper.AIDifficulty.Hard:
			Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 100f);
			break;
		}
		if (extraXpGain)
		{
			lord.AddSkillXp(DefaultSkills.Steward, 100f);
		}
	}

	public void OnHideoutClearedAsGhost()
	{
		TextObject textObject = new TextObject("{=Obuhsttm}Ghost bonus: {XP} Roguery exp! (Base: {BASE_XP})");
		float rogueryXpGainAsGhost = Campaign.Current.Models.HideoutModel.GetRogueryXpGainAsGhost();
		float skillXp = Hero.MainHero.HeroDeveloper.GetSkillXp(DefaultSkills.Roguery);
		Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, rogueryXpGainAsGhost);
		float f = Hero.MainHero.HeroDeveloper.GetSkillXp(DefaultSkills.Roguery) - skillXp;
		textObject.SetTextVariable("BASE_XP", MathF.Floor(rogueryXpGainAsGhost));
		textObject.SetTextVariable("XP", MathF.Floor(f));
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0f, 0f, 1f)));
	}

	public void OnHideoutMissionEnd(bool isSucceeded)
	{
		float rogueryXpGainOnHideoutMissionEnd = Campaign.Current.Models.HideoutModel.GetRogueryXpGainOnHideoutMissionEnd(isSucceeded);
		Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, rogueryXpGainOnHideoutMissionEnd);
	}

	public void OnWarehouseProduction(EquipmentElement production)
	{
		Hero.MainHero.AddSkillXp(DefaultSkills.Trade, Campaign.Current.Models.WorkshopModel.GetTradeXpPerWarehouseProduction(production));
	}

	public void OnAIPartyLootCasualties(int goldAmount, Hero winnerPartyLeader, PartyBase defeatedParty)
	{
		if (defeatedParty.IsMobile)
		{
			float num = -1f;
			MobileParty mobileParty = defeatedParty.MobileParty;
			if (mobileParty.IsVillager)
			{
				num = 0.75f;
			}
			else if (mobileParty.IsCaravan)
			{
				num = 0.15f;
			}
			if (num > 0f)
			{
				float rawXp = (float)goldAmount * num;
				winnerPartyLeader.HeroDeveloper.AddSkillXp(DefaultSkills.Roguery, rawXp, isAffectedByFocusFactor: true, shouldNotify: false);
			}
		}
	}

	public void OnShipDamaged(Ship ship, float rawDamage, float finalDamage)
	{
	}

	public void OnShipRepaired(Ship ship, float repairedHitPoints)
	{
	}

	public void OnTravelOnWater(MobileParty party, float speed)
	{
	}

	private static void OnPersonalSkillExercised(Hero hero, SkillObject skill, float skillXp, bool shouldNotify = true)
	{
		hero?.HeroDeveloper.AddSkillXp(skill, skillXp, isAffectedByFocusFactor: true, shouldNotify);
	}

	private static void OnSettlementSkillExercised(Settlement settlement, SkillObject skill, float skillXp)
	{
		(settlement.Town?.Governor ?? ((settlement.OwnerClan.Leader.CurrentSettlement == settlement) ? settlement.OwnerClan.Leader : null))?.AddSkillXp(skill, skillXp);
	}

	private static void OnGainingRidingExperience(Hero hero, float baseXpAmount, ItemObject horse)
	{
		if (horse != null)
		{
			float num = 1f + (float)horse.Difficulty * 0.02f;
			hero.AddSkillXp(DefaultSkills.Riding, baseXpAmount * num);
		}
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
