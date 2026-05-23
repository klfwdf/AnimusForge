using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultPartyHealingModel : PartyHealingModel
{
	private const int StarvingEffectHeroes = -19;

	private const int FortificationEffectForHeroes = 8;

	private const int FortificationEffectForRegulars = 10;

	private const int BaseDailyHealingForHeroes = 11;

	private const int DailyHealingForPrisonerHeroes = 20;

	private const int DailyHealingForPrisonerRegulars = 1;

	private const int BaseDailyHealingForTroops = 5;

	private const int SkillEXPFromHealingTroops = 5;

	private const float StarvingWoundedEffectRatio = 0.25f;

	private const float StarvingWoundedEffectRatioForGarrison = 0.1f;

	private const float DriftingWoundedEffectRatio = 0.25f;

	private const float AISurgeonSurvivalMultiplier = 0.25f;

	private const float DoctorsOathMultiplier = 0.1f;

	private static readonly TextObject _starvingText = new TextObject("{=jZYUdkXF}Starving");

	private static readonly TextObject _settlementText = new TextObject("{=M0Gpl0dH}In Settlement");

	private static readonly TextObject _raftStateText = new TextObject("{=dNJLG7O5}Stranded at sea");

	public override float GetSurgeryChance(PartyBase party)
	{
		int num = party.MobileParty?.EffectiveSurgeon?.GetSkillValue(DefaultSkills.Medicine) ?? 0;
		return 0.0015f * (float)num;
	}

	public override float GetSiegeBombardmentHitSurgeryChance(PartyBase party)
	{
		float num = 0f;
		if (party != null && party.IsMobile && party.MobileParty.HasPerk(DefaultPerks.Medicine.SiegeMedic))
		{
			num += DefaultPerks.Medicine.SiegeMedic.PrimaryBonus;
		}
		return num;
	}

	public override float GetSurvivalChance(PartyBase party, CharacterObject character, DamageTypes damageType, bool canDamageKillEvenIfBlunt, PartyBase enemyParty = null)
	{
		if ((damageType == DamageTypes.Blunt && !canDamageKillEvenIfBlunt) || (character.IsHero && CampaignOptions.BattleDeath == CampaignOptions.Difficulty.VeryEasy) || (character.IsPlayerCharacter && CampaignOptions.BattleDeath == CampaignOptions.Difficulty.Easy))
		{
			return 1f;
		}
		ExplainedNumber survivalDenominator = new ExplainedNumber(1f);
		if (party?.MobileParty != null)
		{
			MobileParty mobileParty = party.MobileParty;
			AddSurgeonSurvivalBonus(mobileParty, ref survivalDenominator);
			if (enemyParty?.MobileParty != null && enemyParty.MobileParty.HasPerk(DefaultPerks.Medicine.DoctorsOath))
			{
				AddDoctorsOathSkillBonusForParty(enemyParty.MobileParty, ref survivalDenominator);
				SkillLevelingManager.OnSurgeryApplied(enemyParty.MobileParty, surgerySuccess: false, character.Tier);
			}
			survivalDenominator.Add((float)character.Level * 0.02f);
			if (!character.IsHero && party.MapEvent != null && character.Tier < 3)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.PhysicianOfPeople, party.MobileParty, isPrimaryBonus: false, ref survivalDenominator, party.MobileParty.IsCurrentlyAtSea);
			}
			if (character.IsHero)
			{
				survivalDenominator.Add(character.GetTotalArmorSum() * 0.01f);
				survivalDenominator.Add(character.Age * -0.01f);
				survivalDenominator.AddFactor(50f);
			}
			ExplainedNumber explainedNumber = new ExplainedNumber(1f / survivalDenominator.ResultNumber);
			if (character.IsHero)
			{
				if (party.IsMobile && party.MobileParty.HasPerk(DefaultPerks.Medicine.CheatDeath, checkSecondaryRole: true))
				{
					explainedNumber.AddFactor(DefaultPerks.Medicine.CheatDeath.SecondaryBonus, DefaultPerks.Medicine.CheatDeath.Name);
				}
				if (character.HeroObject.Clan == Clan.PlayerClan)
				{
					float clanMemberDeathChanceMultiplier = Campaign.Current.Models.DifficultyModel.GetClanMemberDeathChanceMultiplier();
					if (!clanMemberDeathChanceMultiplier.ApproximatelyEqualsTo(0f))
					{
						explainedNumber.AddFactor(clanMemberDeathChanceMultiplier, GameTexts.FindText("str_game_difficulty"));
					}
				}
			}
			return 1f - MBMath.ClampFloat(explainedNumber.ResultNumber, 0f, 1f);
		}
		if (character.IsHero && character.HeroObject.IsPrisoner)
		{
			return 1f - character.Age * 0.0035f;
		}
		if (survivalDenominator.ResultNumber.ApproximatelyEqualsTo(0f))
		{
			return 0f;
		}
		return 1f - 1f / survivalDenominator.ResultNumber;
	}

	public override int GetSkillXpFromHealingTroop(PartyBase party)
	{
		return 5;
	}

	public override ExplainedNumber GetDailyHealingForRegulars(PartyBase party, bool isPrisoners, bool includeDescriptions = false)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(0f, includeDescriptions);
		if (isPrisoners)
		{
			explainedNumber.Add(1f);
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
						explainedNumber.Add(-num, _starvingText);
					}
				}
				else
				{
					int totalRegulars = party.MemberRoster.TotalRegulars;
					explainedNumber.Add((float)(-totalRegulars) * 0.25f, _starvingText);
				}
			}
			else
			{
				explainedNumber.Add(5f);
				if (mobileParty.IsGarrison)
				{
					if (mobileParty.CurrentSettlement.IsTown)
					{
						SkillHelper.AddSkillBonusForTown(DefaultSkillEffects.GovernorHealingRateBonus, mobileParty.CurrentSettlement.Town, ref explainedNumber);
					}
				}
				else
				{
					SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.HealingRateBonusForRegulars, mobileParty, ref explainedNumber);
				}
				if (!mobileParty.IsGarrison && !mobileParty.IsMilitia)
				{
					if (!mobileParty.IsMoving)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.TriageTent, mobileParty, isPrimaryBonus: true, ref explainedNumber, mobileParty.IsCurrentlyAtSea);
					}
					else if (!mobileParty.IsCurrentlyAtSea)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.WalkItOff, mobileParty, isPrimaryBonus: true, ref explainedNumber, mobileParty.IsCurrentlyAtSea);
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Athletics.WalkItOff, mobileParty, isPrimaryBonus: true, ref explainedNumber);
					}
				}
				if (mobileParty.Morale >= Campaign.Current.Models.PartyMoraleModel.HighMoraleValue)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.BestMedicine, mobileParty, isPrimaryBonus: true, ref explainedNumber, mobileParty.IsCurrentlyAtSea);
				}
				if (mobileParty.CurrentSettlement != null && !mobileParty.CurrentSettlement.IsHideout)
				{
					if (mobileParty.CurrentSettlement.IsFortification)
					{
						explainedNumber.Add(10f, _settlementText);
					}
					if (party.SiegeEvent == null && !mobileParty.CurrentSettlement.IsUnderSiege && !mobileParty.CurrentSettlement.IsRaided && !mobileParty.CurrentSettlement.IsUnderRaid)
					{
						if (mobileParty.CurrentSettlement.IsTown)
						{
							PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.PristineStreets, mobileParty, isPrimaryBonus: false, ref explainedNumber);
						}
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Athletics.AGoodDaysRest, mobileParty, isPrimaryBonus: true, ref explainedNumber);
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.GoodLogdings, mobileParty, isPrimaryBonus: true, ref explainedNumber);
					}
				}
				else if (!mobileParty.IsMoving && mobileParty.LastVisitedSettlement != null && mobileParty.LastVisitedSettlement.IsVillage && mobileParty.LastVisitedSettlement.Position.DistanceSquared(party.Position) < 2f && !mobileParty.LastVisitedSettlement.IsUnderRaid && !mobileParty.LastVisitedSettlement.IsRaided)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.BushDoctor, mobileParty, isPrimaryBonus: false, ref explainedNumber);
				}
				if (mobileParty.Army != null)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.Rearguard, mobileParty, isPrimaryBonus: true, ref explainedNumber, mobileParty.IsCurrentlyAtSea);
				}
				if (party.ItemRoster.FoodVariety > 0 && mobileParty.HasPerk(DefaultPerks.Medicine.PerfectHealth))
				{
					float num2 = DefaultPerks.Medicine.PerfectHealth.PrimaryBonus;
					if (party.IsMobile && party.MobileParty.IsCurrentlyAtSea)
					{
						num2 *= 0.5f;
					}
					explainedNumber.AddFactor((float)mobileParty.ItemRoster.FoodVariety * num2, DefaultPerks.Medicine.PerfectHealth.Name);
				}
				if (mobileParty.HasPerk(DefaultPerks.Medicine.HelpingHands))
				{
					int num3 = MathF.Floor((float)party.MemberRoster.TotalManCount / 10f);
					float num4 = DefaultPerks.Medicine.HelpingHands.PrimaryBonus;
					if (mobileParty.IsCurrentlyAtSea)
					{
						num4 *= 0.5f;
					}
					float value = (float)num3 * num4;
					explainedNumber.AddFactor(value, DefaultPerks.Medicine.HelpingHands.Name);
				}
			}
			if (mobileParty.IsInRaftState)
			{
				int totalRegulars2 = party.MemberRoster.TotalRegulars;
				explainedNumber.Add((float)(-totalRegulars2) * 0.25f, _raftStateText);
			}
		}
		return explainedNumber;
	}

	public override ExplainedNumber GetDailyHealingHpForHeroes(PartyBase party, bool isPrisoners, bool includeDescriptions = false)
	{
		ExplainedNumber stat = new ExplainedNumber(0f, includeDescriptions);
		if (isPrisoners)
		{
			stat.Add(20f);
		}
		else if (party == null)
		{
			stat.Add(11f);
		}
		else if (party.IsMobile)
		{
			MobileParty mobileParty = party.MobileParty;
			if (party.IsStarving && mobileParty.CurrentSettlement == null)
			{
				return new ExplainedNumber(-19f, includeDescriptions, _starvingText);
			}
			stat.Add(11f);
			if (!mobileParty.IsGarrison && !mobileParty.IsMilitia)
			{
				if (!mobileParty.IsMoving)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.TriageTent, mobileParty, isPrimaryBonus: true, ref stat, mobileParty.IsCurrentlyAtSea);
				}
				else if (!mobileParty.IsCurrentlyAtSea)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.WalkItOff, mobileParty, isPrimaryBonus: true, ref stat, mobileParty.IsCurrentlyAtSea);
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Athletics.WalkItOff, mobileParty, isPrimaryBonus: true, ref stat);
				}
			}
			if (mobileParty.Morale >= Campaign.Current.Models.PartyMoraleModel.HighMoraleValue)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.BestMedicine, mobileParty, isPrimaryBonus: true, ref stat, mobileParty.IsCurrentlyAtSea);
			}
			if (mobileParty.CurrentSettlement != null && !mobileParty.CurrentSettlement.IsHideout)
			{
				if (mobileParty.CurrentSettlement.IsFortification)
				{
					stat.Add(8f, _settlementText);
				}
				if (mobileParty.CurrentSettlement.IsTown)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.PristineStreets, mobileParty, isPrimaryBonus: false, ref stat);
				}
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Athletics.AGoodDaysRest, mobileParty, isPrimaryBonus: true, ref stat);
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.GoodLogdings, mobileParty, isPrimaryBonus: true, ref stat);
			}
			else if (!mobileParty.IsMoving && mobileParty.LastVisitedSettlement != null && mobileParty.LastVisitedSettlement.IsVillage && mobileParty.LastVisitedSettlement.Position.DistanceSquared(party.Position) < 2f && !mobileParty.LastVisitedSettlement.IsUnderRaid && !mobileParty.LastVisitedSettlement.IsRaided)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.BushDoctor, mobileParty, isPrimaryBonus: false, ref stat);
			}
			SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.HealingRateBonusForHeroes, mobileParty, ref stat);
		}
		return stat;
	}

	public override int GetHeroesEffectedHealingAmount(Hero hero, float healingRate)
	{
		ExplainedNumber bonuses = new ExplainedNumber(healingRate);
		bool shouldApplyNavalMultiplier = (hero.PartyBelongedTo != null && hero.PartyBelongedTo.IsCurrentlyAtSea) || (hero.PartyBelongedToAsPrisoner != null && hero.PartyBelongedToAsPrisoner.IsMobile && hero.PartyBelongedToAsPrisoner.MobileParty.IsCurrentlyAtSea);
		PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Medicine.SelfMedication, hero.CharacterObject, isPrimaryBonus: true, ref bonuses, shouldApplyNavalMultiplier);
		float resultNumber = bonuses.ResultNumber;
		if (resultNumber - (float)(int)resultNumber > MBRandom.RandomFloat)
		{
			return (int)resultNumber + 1;
		}
		return (int)resultNumber;
	}

	public override ExplainedNumber GetBattleEndHealingAmount(PartyBase party, Hero hero)
	{
		ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions: false, null);
		if (hero.GetPerkValue(DefaultPerks.Medicine.PreventiveMedicine))
		{
			result.Add(DefaultPerks.Medicine.PreventiveMedicine.SecondaryBonus * (float)(hero.MaxHitPoints - hero.HitPoints), DefaultPerks.Medicine.PreventiveMedicine.Name);
		}
		if (party.MapEventSide == party.MapEvent.AttackerSide && hero.GetPerkValue(DefaultPerks.Medicine.WalkItOff))
		{
			result.Add(DefaultPerks.Medicine.WalkItOff.SecondaryBonus, DefaultPerks.Medicine.WalkItOff.Name);
		}
		return result;
	}

	private static void AddDoctorsOathSkillBonusForParty(MobileParty enemyParty, ref ExplainedNumber explainedNumber)
	{
		CharacterObject characterObject = enemyParty.GetEffectiveRoleHolder(PartyRole.Surgeon)?.CharacterObject ?? SkillHelper.GetEffectivePartyLeaderForSkill(enemyParty.Party);
		if (characterObject != null)
		{
			bool flag = enemyParty.MapEvent?.IsPlayerMapEvent ?? false;
			int skillValue = characterObject.GetSkillValue(DefaultSkillEffects.SurgeonSurvivalBonus.EffectedSkill);
			float skillEffectValue = DefaultSkillEffects.SurgeonSurvivalBonus.GetSkillEffectValue(skillValue);
			explainedNumber.Add(skillEffectValue * (flag ? 1f : 0.1f), explainedNumber.IncludeDescriptions ? GameTexts.FindText("role", PartyRole.Surgeon.ToString()) : null);
		}
	}

	private void AddSurgeonSurvivalBonus(MobileParty mobileParty, ref ExplainedNumber survivalDenominator)
	{
		CharacterObject characterObject = mobileParty.GetEffectiveRoleHolder(PartyRole.Surgeon)?.CharacterObject ?? SkillHelper.GetEffectivePartyLeaderForSkill(mobileParty.Party);
		if (characterObject != null)
		{
			bool flag = mobileParty.MapEvent?.IsPlayerMapEvent ?? false;
			int skillValue = characterObject.GetSkillValue(DefaultSkillEffects.SurgeonSurvivalBonus.EffectedSkill);
			float skillEffectValue = DefaultSkillEffects.SurgeonSurvivalBonus.GetSkillEffectValue(skillValue);
			survivalDenominator.Add(skillEffectValue * (flag ? 1f : 0.25f), survivalDenominator.IncludeDescriptions ? GameTexts.FindText("role", PartyRole.Surgeon.ToString()) : null);
		}
	}
}
