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
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CharacterDevelopment
{
	// Token: 0x020003A7 RID: 935
	public class DefaultSkillLevelingManager : ISkillLevelingManager
	{
		// Token: 0x06003598 RID: 13720 RVA: 0x000DF870 File Offset: 0x000DDA70
		public void OnCombatHit(CharacterObject affectorCharacter, CharacterObject affectedCharacter, CharacterObject captain, Hero commander, float speedBonusFromMovement, float shotDifficulty, WeaponComponentData affectorWeapon, float hitPointRatio, CombatXpModel.MissionTypeEnum missionType, bool isAffectorMounted, bool isTeamKill, bool isAffectorUnderCommand, float damageAmount, bool isFatal, bool isSiegeEngineHit, bool isHorseCharge, bool isSneakAttack)
		{
			if (isTeamKill)
			{
				return;
			}
			ExplainedNumber explainedNumber = new ExplainedNumber(1f, false, null);
			if (affectorCharacter.IsHero)
			{
				Hero heroObject = affectorCharacter.HeroObject;
				CombatXpModel combatXpModel = Campaign.Current.Models.CombatXpModel;
				CharacterObject characterObject = heroObject.CharacterObject;
				MobileParty partyBelongedTo = heroObject.PartyBelongedTo;
				explainedNumber = new ExplainedNumber(combatXpModel.GetXpFromHit(characterObject, captain, affectedCharacter, (partyBelongedTo != null) ? partyBelongedTo.Party : null, (int)damageAmount, isFatal, missionType).ResultNumber, false, null);
				SkillObject skillObject;
				if (affectorWeapon != null)
				{
					skillObject = Campaign.Current.Models.CombatXpModel.GetSkillForWeapon(affectorWeapon, isSiegeEngineHit);
					float num = ((skillObject == DefaultSkills.Bow) ? 0.5f : 1f);
					if (shotDifficulty > 0f)
					{
						explainedNumber.AddFactor(num * Campaign.Current.Models.CombatXpModel.GetXpMultiplierFromShotDifficulty(shotDifficulty), null);
					}
				}
				else
				{
					skillObject = (isHorseCharge ? DefaultSkills.Riding : DefaultSkills.Athletics);
				}
				heroObject.AddSkillXp(skillObject, (float)MBRandom.RoundRandomized((float)explainedNumber.RoundedResultNumber));
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
							DefaultSkillLevelingManager.OnGainingRidingExperience(heroObject, (float)MBRandom.RoundRandomized(num3 * (float)explainedNumber.RoundedResultNumber), heroObject.CharacterObject.Equipment.Horse.Item);
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
							heroObject.AddSkillXp(DefaultSkills.Athletics, (float)MBRandom.RoundRandomized(num4 * explainedNumber.ResultNumber));
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
				this.OnTacticsUsed(commander.PartyBelongedTo, (float)MathF.Ceiling(0.02f * (float)explainedNumber.RoundedResultNumber));
			}
		}

		// Token: 0x06003599 RID: 13721 RVA: 0x000DFA94 File Offset: 0x000DDC94
		public void OnSiegeEngineDestroyed(MobileParty party, SiegeEngineType destroyedSiegeEngine)
		{
			if (((party != null) ? party.EffectiveEngineer : null) != null)
			{
				float skillXp = (float)destroyedSiegeEngine.ManDayCost * 20f;
				DefaultSkillLevelingManager.OnPartySkillExercised(party, DefaultSkills.Engineering, skillXp, PartyRole.Engineer);
			}
		}

		// Token: 0x0600359A RID: 13722 RVA: 0x000DFACC File Offset: 0x000DDCCC
		public void OnSimulationCombatKill(CharacterObject affectorCharacter, CharacterObject affectedCharacter, PartyBase affectorParty, PartyBase commanderParty)
		{
			int xpReward = Campaign.Current.Models.PartyTrainingModel.GetXpReward(affectedCharacter);
			if (affectorCharacter.IsHero)
			{
				ItemObject defaultWeapon = CharacterHelper.GetDefaultWeapon(affectorCharacter);
				Hero heroObject = affectorCharacter.HeroObject;
				if (defaultWeapon != null)
				{
					SkillObject skillForWeapon = Campaign.Current.Models.CombatXpModel.GetSkillForWeapon(defaultWeapon.GetWeaponWithUsageIndex(0), false);
					heroObject.AddSkillXp(skillForWeapon, (float)xpReward);
				}
				if (affectorCharacter.IsMounted)
				{
					float f = (float)xpReward * 0.3f;
					DefaultSkillLevelingManager.OnGainingRidingExperience(heroObject, (float)MBRandom.RoundRandomized(f), heroObject.CharacterObject.Equipment.Horse.Item);
				}
				else
				{
					float f2 = (float)xpReward * 0.3f;
					heroObject.AddSkillXp(DefaultSkills.Athletics, (float)MBRandom.RoundRandomized(f2));
				}
			}
			if (commanderParty != null && commanderParty.IsMobile && !commanderParty.MapEvent.IsNavalMapEvent && commanderParty.LeaderHero != null && commanderParty.LeaderHero != affectedCharacter.HeroObject)
			{
				this.OnTacticsUsed(commanderParty.MobileParty, (float)MathF.Ceiling(0.02f * (float)xpReward));
			}
		}

		// Token: 0x0600359B RID: 13723 RVA: 0x000DFBD8 File Offset: 0x000DDDD8
		public void OnTradeProfitMade(PartyBase party, int tradeProfit)
		{
			if (tradeProfit > 0)
			{
				float skillXp = (float)tradeProfit * 0.5f;
				DefaultSkillLevelingManager.OnPartySkillExercised(party.MobileParty, DefaultSkills.Trade, skillXp, PartyRole.PartyLeader);
			}
		}

		// Token: 0x0600359C RID: 13724 RVA: 0x000DFC04 File Offset: 0x000DDE04
		public void OnTradeProfitMade(Hero hero, int tradeProfit)
		{
			if (tradeProfit > 0)
			{
				float skillXp = (float)tradeProfit * 0.5f;
				DefaultSkillLevelingManager.OnPersonalSkillExercised(hero, DefaultSkills.Trade, skillXp, hero == Hero.MainHero);
			}
		}

		// Token: 0x0600359D RID: 13725 RVA: 0x000DFC32 File Offset: 0x000DDE32
		public void OnSettlementProjectFinished(Settlement settlement)
		{
			DefaultSkillLevelingManager.OnSettlementSkillExercised(settlement, DefaultSkills.Steward, 1000f);
		}

		// Token: 0x0600359E RID: 13726 RVA: 0x000DFC44 File Offset: 0x000DDE44
		public void OnSettlementGoverned(Hero governor, Settlement settlement)
		{
			float prosperityChange = settlement.Town.ProsperityChange;
			if (prosperityChange > 0f)
			{
				float skillXp = prosperityChange * 30f;
				DefaultSkillLevelingManager.OnPersonalSkillExercised(governor, DefaultSkills.Steward, skillXp, true);
			}
		}

		// Token: 0x0600359F RID: 13727 RVA: 0x000DFC7C File Offset: 0x000DDE7C
		public void OnInfluenceSpent(Hero hero, float amountSpent)
		{
			if (hero.PartyBelongedTo != null)
			{
				float skillXp = 10f * amountSpent;
				DefaultSkillLevelingManager.OnPartySkillExercised(hero.PartyBelongedTo, DefaultSkills.Steward, skillXp, PartyRole.PartyLeader);
			}
		}

		// Token: 0x060035A0 RID: 13728 RVA: 0x000DFCAC File Offset: 0x000DDEAC
		public void OnGainRelation(Hero hero, Hero gainedRelationWith, float relationChange, ChangeRelationAction.ChangeRelationDetail detail = ChangeRelationAction.ChangeRelationDetail.Default)
		{
			if ((hero.PartyBelongedTo == null && detail != ChangeRelationAction.ChangeRelationDetail.Emissary) || relationChange <= 0f)
			{
				return;
			}
			int charmExperienceFromRelationGain = Campaign.Current.Models.DiplomacyModel.GetCharmExperienceFromRelationGain(gainedRelationWith, relationChange, detail);
			if (hero.PartyBelongedTo != null)
			{
				DefaultSkillLevelingManager.OnPartySkillExercised(hero.PartyBelongedTo, DefaultSkills.Charm, (float)charmExperienceFromRelationGain, PartyRole.PartyLeader);
				return;
			}
			DefaultSkillLevelingManager.OnPersonalSkillExercised(hero, DefaultSkills.Charm, (float)charmExperienceFromRelationGain, true);
		}

		// Token: 0x060035A1 RID: 13729 RVA: 0x000DFD14 File Offset: 0x000DDF14
		public void OnTroopRecruited(Hero hero, int amount, int tier)
		{
			if (amount > 0)
			{
				int num = amount * tier * 2;
				DefaultSkillLevelingManager.OnPersonalSkillExercised(hero, DefaultSkills.Leadership, (float)num, true);
			}
		}

		// Token: 0x060035A2 RID: 13730 RVA: 0x000DFD3C File Offset: 0x000DDF3C
		public void OnBribeGiven(int amount)
		{
			if (amount > 0)
			{
				float skillXp = (float)amount * 0.1f;
				DefaultSkillLevelingManager.OnPartySkillExercised(MobileParty.MainParty, DefaultSkills.Roguery, skillXp, PartyRole.PartyLeader);
			}
		}

		// Token: 0x060035A3 RID: 13731 RVA: 0x000DFD67 File Offset: 0x000DDF67
		public void OnBanditsRecruited(MobileParty mobileParty, CharacterObject bandit, int count)
		{
			if (count > 0)
			{
				DefaultSkillLevelingManager.OnPersonalSkillExercised(mobileParty.LeaderHero, DefaultSkills.Roguery, (float)(count * 2 * bandit.Tier), true);
			}
		}

		// Token: 0x060035A4 RID: 13732 RVA: 0x000DFD8C File Offset: 0x000DDF8C
		public void OnMainHeroReleasedFromCaptivity(float captivityTime)
		{
			float skillXp = captivityTime * 0.5f;
			DefaultSkillLevelingManager.OnPersonalSkillExercised(Hero.MainHero, DefaultSkills.Roguery, skillXp, true);
		}

		// Token: 0x060035A5 RID: 13733 RVA: 0x000DFDB4 File Offset: 0x000DDFB4
		public void OnMainHeroTortured()
		{
			float skillXp = MBRandom.RandomFloatRanged(50f, 100f);
			DefaultSkillLevelingManager.OnPersonalSkillExercised(Hero.MainHero, DefaultSkills.Roguery, skillXp, true);
		}

		// Token: 0x060035A6 RID: 13734 RVA: 0x000DFDE4 File Offset: 0x000DDFE4
		public void OnMainHeroDisguised(bool isNotCaught)
		{
			float skillXp = (isNotCaught ? MBRandom.RandomFloatRanged(10f, 25f) : MBRandom.RandomFloatRanged(1f, 10f));
			DefaultSkillLevelingManager.OnPartySkillExercised(MobileParty.MainParty, DefaultSkills.Roguery, skillXp, PartyRole.PartyLeader);
		}

		// Token: 0x060035A7 RID: 13735 RVA: 0x000DFE28 File Offset: 0x000DE028
		public void OnRaid(MobileParty attackerParty, ItemRoster lootedItems)
		{
			if (attackerParty.LeaderHero != null)
			{
				float skillXp = (float)lootedItems.TradeGoodsTotalValue * 0.5f + (float)(lootedItems.NumberOfMounts * 100) + (float)(lootedItems.NumberOfLivestockAnimals * 25) + (float)(lootedItems.NumberOfPackAnimals * 25);
				DefaultSkillLevelingManager.OnPersonalSkillExercised(attackerParty.LeaderHero, DefaultSkills.Roguery, skillXp, true);
			}
		}

		// Token: 0x060035A8 RID: 13736 RVA: 0x000DFE80 File Offset: 0x000DE080
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
				DefaultSkillLevelingManager.OnPersonalSkillExercised(attackerParty.LeaderHero, DefaultSkills.Roguery, skillXp, true);
			}
		}

		// Token: 0x060035A9 RID: 13737 RVA: 0x000DFF0C File Offset: 0x000DE10C
		public void OnPrisonerSell(MobileParty mobileParty, in TroopRoster prisonerRoster)
		{
			int num = 0;
			for (int i = 0; i < prisonerRoster.Count; i++)
			{
				num += prisonerRoster.data[i].Character.Tier * prisonerRoster.data[i].Number;
			}
			int num2 = num * 2;
			DefaultSkillLevelingManager.OnPartySkillExercised(mobileParty, DefaultSkills.Roguery, (float)num2, PartyRole.PartyLeader);
		}

		// Token: 0x060035AA RID: 13738 RVA: 0x000DFF6C File Offset: 0x000DE16C
		public void OnSurgeryApplied(MobileParty party, bool surgerySuccess, int troopTier)
		{
			float skillXp = (float)(surgerySuccess ? (10 * troopTier) : (5 * troopTier));
			DefaultSkillLevelingManager.OnPartySkillExercised(party, DefaultSkills.Medicine, skillXp, PartyRole.Surgeon);
		}

		// Token: 0x060035AB RID: 13739 RVA: 0x000DFF94 File Offset: 0x000DE194
		public void OnTacticsUsed(MobileParty party, float xp)
		{
			if (xp > 0f)
			{
				DefaultSkillLevelingManager.OnPartySkillExercised(party, DefaultSkills.Tactics, xp, PartyRole.PartyLeader);
			}
		}

		// Token: 0x060035AC RID: 13740 RVA: 0x000DFFAB File Offset: 0x000DE1AB
		public void OnHideoutSpotted(MobileParty party, PartyBase spottedParty)
		{
			DefaultSkillLevelingManager.OnPartySkillExercised(party, DefaultSkills.Scouting, 100f, PartyRole.Scout);
		}

		// Token: 0x060035AD RID: 13741 RVA: 0x000DFFC0 File Offset: 0x000DE1C0
		public void OnTrackDetected(Track track)
		{
			float skillFromTrackDetected = Campaign.Current.Models.MapTrackModel.GetSkillFromTrackDetected(track);
			DefaultSkillLevelingManager.OnPartySkillExercised(MobileParty.MainParty, DefaultSkills.Scouting, skillFromTrackDetected, PartyRole.Scout);
		}

		// Token: 0x060035AE RID: 13742 RVA: 0x000DFFF5 File Offset: 0x000DE1F5
		public void OnTravelOnFoot(Hero hero, float speed)
		{
			hero.AddSkillXp(DefaultSkills.Athletics, (float)(MBRandom.RoundRandomized(0.2f * speed) + 1));
		}

		// Token: 0x060035AF RID: 13743 RVA: 0x000E0014 File Offset: 0x000DE214
		public void OnTravelOnHorse(Hero hero, float speed)
		{
			ItemObject item = hero.CharacterObject.Equipment.Horse.Item;
			DefaultSkillLevelingManager.OnGainingRidingExperience(hero, (float)MBRandom.RoundRandomized(0.3f * speed), item);
		}

		// Token: 0x060035B0 RID: 13744 RVA: 0x000E0050 File Offset: 0x000DE250
		public void OnHeroHealedWhileWaiting(Hero hero, int healingAmount)
		{
			if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.EffectiveSurgeon != null)
			{
				float num = (float)Campaign.Current.Models.PartyHealingModel.GetSkillXpFromHealingTroop(hero.PartyBelongedTo.Party);
				float num2 = ((hero.PartyBelongedTo.CurrentSettlement != null && !hero.PartyBelongedTo.CurrentSettlement.IsCastle) ? 0.2f : 0.1f);
				num *= (float)healingAmount * num2 * (1f + (float)hero.PartyBelongedTo.EffectiveSurgeon.Level * 0.1f);
				DefaultSkillLevelingManager.OnPartySkillExercised(hero.PartyBelongedTo, DefaultSkills.Medicine, num, PartyRole.Surgeon);
			}
		}

		// Token: 0x060035B1 RID: 13745 RVA: 0x000E00FC File Offset: 0x000DE2FC
		public void OnRegularTroopHealedWhileWaiting(MobileParty mobileParty, int healedTroopCount, float averageTier)
		{
			float num = (float)(Campaign.Current.Models.PartyHealingModel.GetSkillXpFromHealingTroop(mobileParty.Party) * healedTroopCount) * averageTier;
			float num2 = ((mobileParty.CurrentSettlement != null && !mobileParty.CurrentSettlement.IsCastle) ? 2f : 1f);
			num *= num2;
			DefaultSkillLevelingManager.OnPartySkillExercised(mobileParty, DefaultSkills.Medicine, num, PartyRole.Surgeon);
		}

		// Token: 0x060035B2 RID: 13746 RVA: 0x000E015C File Offset: 0x000DE35C
		public void OnLeadingArmy(MobileParty mobileParty)
		{
			Army army = mobileParty.Army;
			float skillXp = ((army != null) ? army.EstimatedStrength : mobileParty.Party.EstimatedStrength) * 0.0004f * mobileParty.Army.Morale;
			DefaultSkillLevelingManager.OnPartySkillExercised(mobileParty, DefaultSkills.Leadership, skillXp, PartyRole.PartyLeader);
		}

		// Token: 0x060035B3 RID: 13747 RVA: 0x000E01A8 File Offset: 0x000DE3A8
		public void OnSieging(MobileParty mobileParty)
		{
			int num = mobileParty.MemberRoster.TotalManCount;
			if (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty)
			{
				foreach (MobileParty mobileParty2 in mobileParty.Army.Parties)
				{
					if (mobileParty2 != mobileParty)
					{
						num += mobileParty2.MemberRoster.TotalManCount;
					}
				}
			}
			float skillXp = 0.25f * MathF.Sqrt((float)num);
			DefaultSkillLevelingManager.OnPartySkillExercised(mobileParty, DefaultSkills.Engineering, skillXp, PartyRole.Engineer);
		}

		// Token: 0x060035B4 RID: 13748 RVA: 0x000E0248 File Offset: 0x000DE448
		public void OnSiegeEngineBuilt(MobileParty mobileParty, SiegeEngineType siegeEngine)
		{
			float skillXp = 30f + 2f * (float)siegeEngine.Difficulty;
			DefaultSkillLevelingManager.OnPartySkillExercised(mobileParty, DefaultSkills.Engineering, skillXp, PartyRole.Engineer);
		}

		// Token: 0x060035B5 RID: 13749 RVA: 0x000E0278 File Offset: 0x000DE478
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

		// Token: 0x060035B6 RID: 13750 RVA: 0x000E02E4 File Offset: 0x000DE4E4
		public void OnPersuasionSucceeded(Hero targetHero, SkillObject skill, PersuasionDifficulty difficulty, int argumentDifficultyBonusCoefficient)
		{
			float num = (float)Campaign.Current.Models.PersuasionModel.GetSkillXpFromPersuasion(difficulty, argumentDifficultyBonusCoefficient);
			if (num > 0f)
			{
				targetHero.AddSkillXp(skill, num);
			}
		}

		// Token: 0x060035B7 RID: 13751 RVA: 0x000E031C File Offset: 0x000DE51C
		public void OnPrisonBreakEnd(Hero prisonerHero, bool isSucceeded)
		{
			float rogueryRewardOnPrisonBreak = Campaign.Current.Models.PrisonBreakModel.GetRogueryRewardOnPrisonBreak(prisonerHero, isSucceeded);
			if (rogueryRewardOnPrisonBreak > 0f)
			{
				Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, rogueryRewardOnPrisonBreak);
			}
		}

		// Token: 0x060035B8 RID: 13752 RVA: 0x000E0358 File Offset: 0x000DE558
		public void OnWallBreached(MobileParty party)
		{
			if (((party != null) ? party.EffectiveEngineer : null) != null)
			{
				DefaultSkillLevelingManager.OnPartySkillExercised(party, DefaultSkills.Engineering, 250f, PartyRole.Engineer);
			}
		}

		// Token: 0x060035B9 RID: 13753 RVA: 0x000E037C File Offset: 0x000DE57C
		public void OnForceVolunteers(MobileParty attackerParty, PartyBase forcedParty)
		{
			if (attackerParty.LeaderHero != null)
			{
				int num = MathF.Ceiling(forcedParty.Settlement.Village.Hearth / 10f);
				DefaultSkillLevelingManager.OnPersonalSkillExercised(attackerParty.LeaderHero, DefaultSkills.Roguery, (float)num, true);
			}
		}

		// Token: 0x060035BA RID: 13754 RVA: 0x000E03C0 File Offset: 0x000DE5C0
		public void OnForceSupplies(MobileParty attackerParty, ItemRoster lootedItems, bool attacked)
		{
			if (attackerParty.LeaderHero != null)
			{
				float num = (attacked ? 0.75f : 0.5f);
				float skillXp = (float)(lootedItems.TradeGoodsTotalValue + lootedItems.NumberOfMounts * 200 + lootedItems.NumberOfLivestockAnimals * 50 + lootedItems.NumberOfPackAnimals * 50) * num;
				DefaultSkillLevelingManager.OnPersonalSkillExercised(attackerParty.LeaderHero, DefaultSkills.Roguery, skillXp, true);
			}
		}

		// Token: 0x060035BB RID: 13755 RVA: 0x000E0424 File Offset: 0x000DE624
		public void OnAIPartiesTravel(Hero hero, bool isCaravanParty, TerrainType currentTerrainType)
		{
			int num = ((currentTerrainType == TerrainType.Forest) ? MBRandom.RoundRandomized(5f) : MBRandom.RoundRandomized(3f));
			hero.AddSkillXp(DefaultSkills.Scouting, isCaravanParty ? ((float)num / 2f) : ((float)num));
		}

		// Token: 0x060035BC RID: 13756 RVA: 0x000E0468 File Offset: 0x000DE668
		public void OnTraverseTerrain(MobileParty mobileParty, TerrainType currentTerrainType)
		{
			float num = 0f;
			float lastCalculatedSpeed = mobileParty._lastCalculatedSpeed;
			if (lastCalculatedSpeed > 1f)
			{
				bool flag = currentTerrainType == TerrainType.Desert || currentTerrainType == TerrainType.Dune || currentTerrainType == TerrainType.Forest || currentTerrainType == TerrainType.Snow;
				num = lastCalculatedSpeed * (1f + MathF.Pow((float)mobileParty.MemberRoster.TotalManCount, 0.66f)) * (flag ? 0.25f : 0.15f);
			}
			if (mobileParty.IsCaravan)
			{
				num *= 0.5f;
			}
			if (num >= 5f)
			{
				DefaultSkillLevelingManager.OnPartySkillExercised(mobileParty, DefaultSkills.Scouting, num, PartyRole.Scout);
			}
		}

		// Token: 0x060035BD RID: 13757 RVA: 0x000E04F4 File Offset: 0x000DE6F4
		public void OnBattleEnded(PartyBase party, CharacterObject troop, int excessXp)
		{
			Hero hero = party.LeaderHero ?? party.Owner;
			float num = 0.025f;
			SkillObject skill = DefaultSkills.Leadership;
			if (troop.Occupation == Occupation.Bandit)
			{
				num = 0.05f;
				skill = DefaultSkills.Roguery;
			}
			float xpAmount = (float)excessXp * num;
			hero.AddSkillXp(skill, xpAmount);
		}

		// Token: 0x060035BE RID: 13758 RVA: 0x000E0540 File Offset: 0x000DE740
		public void OnFoodConsumed(MobileParty mobileParty, bool wasStarving)
		{
			if (!wasStarving && mobileParty.ItemRoster.FoodVariety > 3 && mobileParty.EffectiveQuartermaster != null)
			{
				float skillXp = (float)MathF.Round(-mobileParty.BaseFoodChange * 100f) * ((float)mobileParty.ItemRoster.FoodVariety - 2f) / 3f;
				DefaultSkillLevelingManager.OnPartySkillExercised(mobileParty, DefaultSkills.Steward, skillXp, PartyRole.Quartermaster);
			}
		}

		// Token: 0x060035BF RID: 13759 RVA: 0x000E05A1 File Offset: 0x000DE7A1
		public void OnAlleyCleared(Alley alley)
		{
			Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, Campaign.Current.Models.AlleyModel.GetInitialXpGainForMainHero());
		}

		// Token: 0x060035C0 RID: 13760 RVA: 0x000E05C8 File Offset: 0x000DE7C8
		public void OnDailyAlleyTick(Alley alley, Hero alleyLeader)
		{
			Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, Campaign.Current.Models.AlleyModel.GetDailyXpGainForMainHero());
			if (alleyLeader != null && !alleyLeader.IsDead)
			{
				alleyLeader.AddSkillXp(DefaultSkills.Roguery, Campaign.Current.Models.AlleyModel.GetDailyXpGainForAssignedClanMember(alleyLeader));
			}
		}

		// Token: 0x060035C1 RID: 13761 RVA: 0x000E0624 File Offset: 0x000DE824
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

		// Token: 0x060035C2 RID: 13762 RVA: 0x000E0698 File Offset: 0x000DE898
		public void OnHideoutMissionEnd(bool isSucceeded)
		{
			float rogueryXpGainOnHideoutMissionEnd = Campaign.Current.Models.HideoutModel.GetRogueryXpGainOnHideoutMissionEnd(isSucceeded);
			Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, rogueryXpGainOnHideoutMissionEnd);
		}

		// Token: 0x060035C3 RID: 13763 RVA: 0x000E06CB File Offset: 0x000DE8CB
		public void OnWarehouseProduction(EquipmentElement production)
		{
			Hero.MainHero.AddSkillXp(DefaultSkills.Trade, Campaign.Current.Models.WorkshopModel.GetTradeXpPerWarehouseProduction(production));
		}

		// Token: 0x060035C4 RID: 13764 RVA: 0x000E06F4 File Offset: 0x000DE8F4
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
					winnerPartyLeader.HeroDeveloper.AddSkillXp(DefaultSkills.Roguery, rawXp, true, false);
				}
			}
		}

		// Token: 0x060035C5 RID: 13765 RVA: 0x000E0754 File Offset: 0x000DE954
		public void OnShipDamaged(Ship ship, float rawDamage, float finalDamage)
		{
		}

		// Token: 0x060035C6 RID: 13766 RVA: 0x000E0756 File Offset: 0x000DE956
		public void OnShipRepaired(Ship ship, float repairedHitPoints)
		{
		}

		// Token: 0x060035C7 RID: 13767 RVA: 0x000E0758 File Offset: 0x000DE958
		public void OnTravelOnWater(Hero hero, float speed)
		{
		}

		// Token: 0x060035C8 RID: 13768 RVA: 0x000E075A File Offset: 0x000DE95A
		private static void OnPersonalSkillExercised(Hero hero, SkillObject skill, float skillXp, bool shouldNotify = true)
		{
			if (hero != null)
			{
				hero.HeroDeveloper.AddSkillXp(skill, skillXp, true, shouldNotify);
			}
		}

		// Token: 0x060035C9 RID: 13769 RVA: 0x000E0770 File Offset: 0x000DE970
		private static void OnSettlementSkillExercised(Settlement settlement, SkillObject skill, float skillXp)
		{
			Town town = settlement.Town;
			Hero hero = ((town != null) ? town.Governor : null) ?? ((settlement.OwnerClan.Leader.CurrentSettlement == settlement) ? settlement.OwnerClan.Leader : null);
			if (hero == null)
			{
				return;
			}
			hero.AddSkillXp(skill, skillXp);
		}

		// Token: 0x060035CA RID: 13770 RVA: 0x000E07C0 File Offset: 0x000DE9C0
		private static void OnGainingRidingExperience(Hero hero, float baseXpAmount, ItemObject horse)
		{
			if (horse != null)
			{
				float num = 1f + (float)horse.Difficulty * 0.02f;
				hero.AddSkillXp(DefaultSkills.Riding, baseXpAmount * num);
			}
		}

		// Token: 0x060035CB RID: 13771 RVA: 0x000E07F2 File Offset: 0x000DE9F2
		private static void OnPartySkillExercised(MobileParty party, SkillObject skill, float skillXp, PartyRole partyRole = PartyRole.PartyLeader)
		{
			Hero effectiveRoleHolder = party.GetEffectiveRoleHolder(partyRole);
			if (effectiveRoleHolder == null)
			{
				return;
			}
			effectiveRoleHolder.AddSkillXp(skill, skillXp);
		}

		// Token: 0x060035CD RID: 13773 RVA: 0x000E080F File Offset: 0x000DEA0F
		void ISkillLevelingManager.OnPrisonerSell(MobileParty mobileParty, in TroopRoster prisonerRoster)
		{
			this.OnPrisonerSell(mobileParty, prisonerRoster);
		}

		// Token: 0x040010BB RID: 4283
		private const float TacticsXpCoefficient = 0.02f;

		// Token: 0x040010BC RID: 4284
		private const int RogueryXpGainOnSneakAttack = 78;
	}
}
