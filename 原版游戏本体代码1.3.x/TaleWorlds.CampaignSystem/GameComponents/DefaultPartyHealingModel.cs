using System;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000131 RID: 305
	public class DefaultPartyHealingModel : PartyHealingModel
	{
		// Token: 0x060018E3 RID: 6371 RVA: 0x0007A134 File Offset: 0x00078334
		public override float GetSurgeryChance(PartyBase party)
		{
			MobileParty mobileParty = party.MobileParty;
			int? num;
			if (mobileParty == null)
			{
				num = null;
			}
			else
			{
				Hero effectiveSurgeon = mobileParty.EffectiveSurgeon;
				num = ((effectiveSurgeon != null) ? new int?(effectiveSurgeon.GetSkillValue(DefaultSkills.Medicine)) : null);
			}
			int num2 = num ?? 0;
			return 0.0015f * (float)num2;
		}

		// Token: 0x060018E4 RID: 6372 RVA: 0x0007A198 File Offset: 0x00078398
		public override float GetSiegeBombardmentHitSurgeryChance(PartyBase party)
		{
			float num = 0f;
			if (party != null && party.IsMobile && party.MobileParty.HasPerk(DefaultPerks.Medicine.SiegeMedic, false))
			{
				num += DefaultPerks.Medicine.SiegeMedic.PrimaryBonus;
			}
			return num;
		}

		// Token: 0x060018E5 RID: 6373 RVA: 0x0007A1D8 File Offset: 0x000783D8
		public override float GetSurvivalChance(PartyBase party, CharacterObject character, DamageTypes damageType, bool canDamageKillEvenIfBlunt, PartyBase enemyParty = null)
		{
			if ((damageType == DamageTypes.Blunt && !canDamageKillEvenIfBlunt) || (character.IsHero && CampaignOptions.BattleDeath == CampaignOptions.Difficulty.VeryEasy) || (character.IsPlayerCharacter && CampaignOptions.BattleDeath == CampaignOptions.Difficulty.Easy))
			{
				return 1f;
			}
			ExplainedNumber explainedNumber = new ExplainedNumber(1f, false, null);
			float result;
			if (((party != null) ? party.MobileParty : null) != null)
			{
				MobileParty mobileParty = party.MobileParty;
				SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.SurgeonSurvivalBonus, mobileParty, ref explainedNumber);
				if (((enemyParty != null) ? enemyParty.MobileParty : null) != null && enemyParty.MobileParty.HasPerk(DefaultPerks.Medicine.DoctorsOath, false))
				{
					DefaultPartyHealingModel.AddDoctorsOathSkillBonusForParty(enemyParty.MobileParty, ref explainedNumber);
					SkillLevelingManager.OnSurgeryApplied(enemyParty.MobileParty, false, character.Tier);
				}
				explainedNumber.Add((float)character.Level * 0.02f, null, null);
				if (!character.IsHero && party.MapEvent != null && character.Tier < 3)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.PhysicianOfPeople, party.MobileParty, false, ref explainedNumber, party.MobileParty.IsCurrentlyAtSea);
				}
				if (character.IsHero)
				{
					explainedNumber.Add(character.GetTotalArmorSum(Equipment.EquipmentType.Battle) * 0.01f, null, null);
					explainedNumber.Add(character.Age * -0.01f, null, null);
					explainedNumber.AddFactor(50f, null);
				}
				ExplainedNumber explainedNumber2 = new ExplainedNumber(1f / explainedNumber.ResultNumber, false, null);
				if (character.IsHero)
				{
					if (party.IsMobile && party.MobileParty.HasPerk(DefaultPerks.Medicine.CheatDeath, true))
					{
						explainedNumber2.AddFactor(DefaultPerks.Medicine.CheatDeath.SecondaryBonus, DefaultPerks.Medicine.CheatDeath.Name);
					}
					if (character.HeroObject.Clan == Clan.PlayerClan)
					{
						float clanMemberDeathChanceMultiplier = Campaign.Current.Models.DifficultyModel.GetClanMemberDeathChanceMultiplier();
						if (!clanMemberDeathChanceMultiplier.ApproximatelyEqualsTo(0f, 1E-05f))
						{
							explainedNumber2.AddFactor(clanMemberDeathChanceMultiplier, GameTexts.FindText("str_game_difficulty", null));
						}
					}
				}
				result = 1f - MBMath.ClampFloat(explainedNumber2.ResultNumber, 0f, 1f);
			}
			else if (character.IsHero && character.HeroObject.IsPrisoner)
			{
				result = 1f - character.Age * 0.0035f;
			}
			else if (explainedNumber.ResultNumber.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				result = 0f;
			}
			else
			{
				result = 1f - 1f / explainedNumber.ResultNumber;
			}
			return result;
		}

		// Token: 0x060018E6 RID: 6374 RVA: 0x0007A43B File Offset: 0x0007863B
		public override int GetSkillXpFromHealingTroop(PartyBase party)
		{
			return 5;
		}

		// Token: 0x060018E7 RID: 6375 RVA: 0x0007A440 File Offset: 0x00078640
		public override ExplainedNumber GetDailyHealingForRegulars(PartyBase party, bool isPrisoners, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			if (isPrisoners)
			{
				result.Add(1f, null, null);
			}
			else if (party != null && party.IsMobile)
			{
				MobileParty mobileParty = party.MobileParty;
				if (party.IsStarving || (mobileParty.IsGarrison && mobileParty.CurrentSettlement.IsStarving))
				{
					if (mobileParty.IsGarrison)
					{
						if (SettlementHelper.IsGarrisonStarving(mobileParty.CurrentSettlement))
						{
							int num = MBRandom.RoundRandomized((float)party.MemberRoster.TotalRegulars * 0.1f);
							result.Add((float)(-(float)num), DefaultPartyHealingModel._starvingText, null);
						}
					}
					else
					{
						int totalRegulars = party.MemberRoster.TotalRegulars;
						result.Add((float)(-(float)totalRegulars) * 0.25f, DefaultPartyHealingModel._starvingText, null);
					}
				}
				else
				{
					result.Add(5f, null, null);
					if (mobileParty.IsGarrison)
					{
						if (mobileParty.CurrentSettlement.IsTown)
						{
							SkillHelper.AddSkillBonusForTown(DefaultSkillEffects.GovernorHealingRateBonus, mobileParty.CurrentSettlement.Town, ref result);
						}
					}
					else
					{
						SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.HealingRateBonusForRegulars, mobileParty, ref result);
					}
					if (!mobileParty.IsGarrison && !mobileParty.IsMilitia)
					{
						if (!mobileParty.IsMoving)
						{
							PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.TriageTent, mobileParty, true, ref result, mobileParty.IsCurrentlyAtSea);
						}
						else if (!mobileParty.IsCurrentlyAtSea)
						{
							PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.WalkItOff, mobileParty, true, ref result, mobileParty.IsCurrentlyAtSea);
							PerkHelper.AddPerkBonusForParty(DefaultPerks.Athletics.WalkItOff, mobileParty, true, ref result, false);
						}
					}
					if (mobileParty.Morale >= Campaign.Current.Models.PartyMoraleModel.HighMoraleValue)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.BestMedicine, mobileParty, true, ref result, mobileParty.IsCurrentlyAtSea);
					}
					if (mobileParty.CurrentSettlement != null && !mobileParty.CurrentSettlement.IsHideout)
					{
						if (mobileParty.CurrentSettlement.IsFortification)
						{
							result.Add(10f, DefaultPartyHealingModel._settlementText, null);
						}
						if (party.SiegeEvent == null && !mobileParty.CurrentSettlement.IsUnderSiege && !mobileParty.CurrentSettlement.IsRaided && !mobileParty.CurrentSettlement.IsUnderRaid)
						{
							if (mobileParty.CurrentSettlement.IsTown)
							{
								PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.PristineStreets, mobileParty, false, ref result, false);
							}
							PerkHelper.AddPerkBonusForParty(DefaultPerks.Athletics.AGoodDaysRest, mobileParty, true, ref result, false);
							PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.GoodLogdings, mobileParty, true, ref result, false);
						}
					}
					else if (!mobileParty.IsMoving && mobileParty.LastVisitedSettlement != null && mobileParty.LastVisitedSettlement.IsVillage && mobileParty.LastVisitedSettlement.Position.DistanceSquared(party.Position) < 2f && !mobileParty.LastVisitedSettlement.IsUnderRaid && !mobileParty.LastVisitedSettlement.IsRaided)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.BushDoctor, mobileParty, false, ref result, false);
					}
					if (mobileParty.Army != null)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.Rearguard, mobileParty, true, ref result, mobileParty.IsCurrentlyAtSea);
					}
					if (party.ItemRoster.FoodVariety > 0 && mobileParty.HasPerk(DefaultPerks.Medicine.PerfectHealth, false))
					{
						float num2 = DefaultPerks.Medicine.PerfectHealth.PrimaryBonus;
						if (party.IsMobile && party.MobileParty.IsCurrentlyAtSea)
						{
							num2 *= 0.5f;
						}
						result.AddFactor((float)mobileParty.ItemRoster.FoodVariety * num2, DefaultPerks.Medicine.PerfectHealth.Name);
					}
					if (mobileParty.HasPerk(DefaultPerks.Medicine.HelpingHands, false))
					{
						float num3 = (float)MathF.Floor((float)party.MemberRoster.TotalManCount / 10f);
						float num4 = DefaultPerks.Medicine.HelpingHands.PrimaryBonus;
						if (mobileParty.IsCurrentlyAtSea)
						{
							num4 *= 0.5f;
						}
						float value = num3 * num4;
						result.AddFactor(value, DefaultPerks.Medicine.HelpingHands.Name);
					}
				}
				if (mobileParty.IsInRaftState)
				{
					int totalRegulars2 = party.MemberRoster.TotalRegulars;
					result.Add((float)(-(float)totalRegulars2) * 0.25f, DefaultPartyHealingModel._raftStateText, null);
				}
			}
			return result;
		}

		// Token: 0x060018E8 RID: 6376 RVA: 0x0007A808 File Offset: 0x00078A08
		public override ExplainedNumber GetDailyHealingHpForHeroes(PartyBase party, bool isPrisoners, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			if (isPrisoners)
			{
				result.Add(20f, null, null);
			}
			else if (party == null)
			{
				result.Add(11f, null, null);
			}
			else if (party.IsMobile)
			{
				MobileParty mobileParty = party.MobileParty;
				if (party.IsStarving && mobileParty.CurrentSettlement == null)
				{
					return new ExplainedNumber(-19f, includeDescriptions, DefaultPartyHealingModel._starvingText);
				}
				result.Add(11f, null, null);
				if (!mobileParty.IsGarrison && !mobileParty.IsMilitia)
				{
					if (!mobileParty.IsMoving)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.TriageTent, mobileParty, true, ref result, mobileParty.IsCurrentlyAtSea);
					}
					else if (!mobileParty.IsCurrentlyAtSea)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.WalkItOff, mobileParty, true, ref result, mobileParty.IsCurrentlyAtSea);
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Athletics.WalkItOff, mobileParty, true, ref result, false);
					}
				}
				if (mobileParty.Morale >= Campaign.Current.Models.PartyMoraleModel.HighMoraleValue)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.BestMedicine, mobileParty, true, ref result, mobileParty.IsCurrentlyAtSea);
				}
				if (mobileParty.CurrentSettlement != null && !mobileParty.CurrentSettlement.IsHideout)
				{
					if (mobileParty.CurrentSettlement.IsFortification)
					{
						result.Add(8f, DefaultPartyHealingModel._settlementText, null);
					}
					if (mobileParty.CurrentSettlement.IsTown)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.PristineStreets, mobileParty, false, ref result, false);
					}
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Athletics.AGoodDaysRest, mobileParty, true, ref result, false);
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.GoodLogdings, mobileParty, true, ref result, false);
				}
				else if (!mobileParty.IsMoving && mobileParty.LastVisitedSettlement != null && mobileParty.LastVisitedSettlement.IsVillage && mobileParty.LastVisitedSettlement.Position.DistanceSquared(party.Position) < 2f && !mobileParty.LastVisitedSettlement.IsUnderRaid && !mobileParty.LastVisitedSettlement.IsRaided)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.BushDoctor, mobileParty, false, ref result, false);
				}
				SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.HealingRateBonusForHeroes, mobileParty, ref result);
			}
			return result;
		}

		// Token: 0x060018E9 RID: 6377 RVA: 0x0007AA00 File Offset: 0x00078C00
		public override int GetHeroesEffectedHealingAmount(Hero hero, float healingRate)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(healingRate, false, null);
			bool shouldApplyNavalMultiplier = (hero.PartyBelongedTo != null && hero.PartyBelongedTo.IsCurrentlyAtSea) || (hero.PartyBelongedToAsPrisoner != null && hero.PartyBelongedToAsPrisoner.IsMobile && hero.PartyBelongedToAsPrisoner.MobileParty.IsCurrentlyAtSea);
			PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Medicine.SelfMedication, hero.CharacterObject, true, ref explainedNumber, shouldApplyNavalMultiplier);
			float resultNumber = explainedNumber.ResultNumber;
			if (resultNumber - (float)((int)resultNumber) > MBRandom.RandomFloat)
			{
				return (int)resultNumber + 1;
			}
			return (int)resultNumber;
		}

		// Token: 0x060018EA RID: 6378 RVA: 0x0007AA88 File Offset: 0x00078C88
		public override ExplainedNumber GetBattleEndHealingAmount(PartyBase party, Hero hero)
		{
			ExplainedNumber result = new ExplainedNumber(0f, false, null);
			if (hero.GetPerkValue(DefaultPerks.Medicine.PreventiveMedicine))
			{
				result.Add(DefaultPerks.Medicine.PreventiveMedicine.SecondaryBonus * (float)(hero.MaxHitPoints - hero.HitPoints), DefaultPerks.Medicine.PreventiveMedicine.Name, null);
			}
			if (party.MapEventSide == party.MapEvent.AttackerSide && hero.GetPerkValue(DefaultPerks.Medicine.WalkItOff))
			{
				result.Add(DefaultPerks.Medicine.WalkItOff.SecondaryBonus, DefaultPerks.Medicine.WalkItOff.Name, null);
			}
			return result;
		}

		// Token: 0x060018EB RID: 6379 RVA: 0x0007AB18 File Offset: 0x00078D18
		private static void AddDoctorsOathSkillBonusForParty(MobileParty party, ref ExplainedNumber explainedNumber)
		{
			Hero effectiveRoleHolder = party.GetEffectiveRoleHolder(PartyRole.Surgeon);
			CharacterObject characterObject = ((effectiveRoleHolder != null) ? effectiveRoleHolder.CharacterObject : null) ?? SkillHelper.GetEffectivePartyLeaderForSkill(party.Party);
			if (characterObject != null)
			{
				int skillValue = characterObject.GetSkillValue(DefaultSkillEffects.SurgeonSurvivalBonus.EffectedSkill);
				float skillEffectValue = DefaultSkillEffects.SurgeonSurvivalBonus.GetSkillEffectValue(skillValue);
				explainedNumber.Add(skillEffectValue * 0.4f, explainedNumber.IncludeDescriptions ? GameTexts.FindText("role", PartyRole.Surgeon.ToString()) : null, null);
			}
		}

		// Token: 0x04000808 RID: 2056
		private const int StarvingEffectHeroes = -19;

		// Token: 0x04000809 RID: 2057
		private const int FortificationEffectForHeroes = 8;

		// Token: 0x0400080A RID: 2058
		private const int FortificationEffectForRegulars = 10;

		// Token: 0x0400080B RID: 2059
		private const int BaseDailyHealingForHeroes = 11;

		// Token: 0x0400080C RID: 2060
		private const int DailyHealingForPrisonerHeroes = 20;

		// Token: 0x0400080D RID: 2061
		private const int DailyHealingForPrisonerRegulars = 1;

		// Token: 0x0400080E RID: 2062
		private const int BaseDailyHealingForTroops = 5;

		// Token: 0x0400080F RID: 2063
		private const int SkillEXPFromHealingTroops = 5;

		// Token: 0x04000810 RID: 2064
		private const float StarvingWoundedEffectRatio = 0.25f;

		// Token: 0x04000811 RID: 2065
		private const float StarvingWoundedEffectRatioForGarrison = 0.1f;

		// Token: 0x04000812 RID: 2066
		private const float DriftingWoundedEffectRatio = 0.25f;

		// Token: 0x04000813 RID: 2067
		private const float DoctorsOathMultiplier = 0.4f;

		// Token: 0x04000814 RID: 2068
		private static readonly TextObject _starvingText = new TextObject("{=jZYUdkXF}Starving", null);

		// Token: 0x04000815 RID: 2069
		private static readonly TextObject _settlementText = new TextObject("{=M0Gpl0dH}In Settlement", null);

		// Token: 0x04000816 RID: 2070
		private static readonly TextObject _raftStateText = new TextObject("{=dNJLG7O5}Stranded at sea", null);
	}
}
