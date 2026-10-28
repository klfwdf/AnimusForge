using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace NavalDLC.GameComponents;

public class NavalStrikeMagnitudeModel : StrikeMagnitudeCalculationModel
{
	public override float CalculateHorseArcheryFactor(BasicCharacterObject characterObject)
	{
		return base.BaseModel.CalculateHorseArcheryFactor(characterObject);
	}

	public override float CalculateStrikeMagnitudeForMissile(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float missileSpeed)
	{
		return base.BaseModel.CalculateStrikeMagnitudeForMissile(in attackInformation, in collisionData, in weapon, missileSpeed);
	}

	public override float CalculateStrikeMagnitudeForSwing(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float swingSpeed, float impactPointAsPercent, float extraLinearSpeed)
	{
		return base.BaseModel.CalculateStrikeMagnitudeForSwing(in attackInformation, in collisionData, in weapon, swingSpeed, impactPointAsPercent, extraLinearSpeed);
	}

	public override float CalculateStrikeMagnitudeForUnarmedAttack(in AttackInformation attackInformation, in AttackCollisionData collisionData, float progressEffect, float momentumRemaining)
	{
		return base.BaseModel.CalculateStrikeMagnitudeForUnarmedAttack(in attackInformation, in collisionData, progressEffect, momentumRemaining);
	}

	public override float CalculateStrikeMagnitudeForThrust(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float thrustWeaponSpeed, float extraLinearSpeed, bool isThrown = false)
	{
		return base.BaseModel.CalculateStrikeMagnitudeForThrust(in attackInformation, in collisionData, in weapon, thrustWeaponSpeed, extraLinearSpeed, isThrown);
	}

	public override float CalculateBaseBlowMagnitudeForPassiveUsage(in AttackInformation attackInformation, in AttackCollisionData collisionData, float extraLinearSpeed)
	{
		return base.BaseModel.CalculateBaseBlowMagnitudeForPassiveUsage(in attackInformation, in collisionData, extraLinearSpeed);
	}

	public override float ComputeRawDamage(DamageTypes damageType, float magnitude, float armorEffectiveness, float absorbedDamageRatio)
	{
		return base.BaseModel.ComputeRawDamage(damageType, magnitude, armorEffectiveness, absorbedDamageRatio);
	}

	public override float GetBluntDamageFactorByDamageType(DamageTypes damageType)
	{
		return base.BaseModel.GetBluntDamageFactorByDamageType(damageType);
	}

	public override float CalculateAdjustedArmorForBlow(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseArmor, BasicCharacterObject attackerCharacter, BasicCharacterObject attackerCaptainCharacter, BasicCharacterObject victimCharacter, BasicCharacterObject victimCaptainCharacter, WeaponComponentData weaponComponent)
	{
		bool flag = false;
		float num = base.BaseModel.CalculateAdjustedArmorForBlow(in attackInformation, in collisionData, baseArmor, attackerCharacter, attackerCaptainCharacter, victimCharacter, victimCaptainCharacter, weaponComponent);
		CharacterObject characterObject = attackerCharacter as CharacterObject;
		CharacterObject characterObject2 = attackerCaptainCharacter as CharacterObject;
		BattleEnvironment attackerBattleEnvironment = attackInformation.AttackerBattleEnvironment;
		if (attackerCharacter == characterObject2)
		{
			characterObject2 = null;
		}
		if (num > 0f && characterObject != null)
		{
			if (weaponComponent != null)
			{
				float bonus2;
				if (weaponComponent.RelevantSkill == DefaultSkills.Crossbow && characterObject.GetPerkValue(DefaultPerks.Crossbow.Piercer) && PerkHelper.TryGetPerkEffectBonus(DefaultPerks.Crossbow.Piercer, isPrimary: true, out var bonus, attackInformation.AttackerBattleEnvironment) && baseArmor < bonus)
				{
					flag = true;
				}
				else if (weaponComponent.WeaponClass == WeaponClass.SlingStone && collisionData.VictimHitBodyPart == BoneBodyPartType.Head && characterObject.GetPerkValue(DefaultPerks.Throwing.SlingingCompetitions) && PerkHelper.TryGetPerkEffectBonus(DefaultPerks.Throwing.SlingingCompetitions, isPrimary: true, out bonus2, attackInformation.AttackerBattleEnvironment))
				{
					flag = true;
				}
			}
			if (flag)
			{
				num = 0f;
			}
			else
			{
				ExplainedNumber bonuses = new ExplainedNumber(baseArmor);
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.TwoHanded.Vandal, characterObject, isPrimaryBonus: true, ref bonuses, attackerBattleEnvironment);
				if (weaponComponent != null)
				{
					if (weaponComponent.RelevantSkill == DefaultSkills.OneHanded)
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.ChinkInTheArmor, characterObject, isPrimaryBonus: true, ref bonuses, attackerBattleEnvironment);
					}
					else if (weaponComponent.RelevantSkill == DefaultSkills.Bow)
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.Bodkin, characterObject, isPrimaryBonus: true, ref bonuses, attackerBattleEnvironment);
						if (characterObject2 != null)
						{
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Bow.Bodkin, characterObject2, ref bonuses, attackerBattleEnvironment);
						}
					}
					else if (weaponComponent.RelevantSkill == DefaultSkills.Crossbow)
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Crossbow.Puncture, characterObject, isPrimaryBonus: true, ref bonuses, attackerBattleEnvironment);
						if (characterObject2 != null)
						{
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Crossbow.Puncture, characterObject2, ref bonuses, attackerBattleEnvironment);
						}
					}
					else if (weaponComponent.RelevantSkill == DefaultSkills.Throwing)
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.WeakSpot, characterObject, isPrimaryBonus: true, ref bonuses, attackerBattleEnvironment);
						if (characterObject2 != null)
						{
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Throwing.WeakSpot, characterObject2, ref bonuses, attackerBattleEnvironment);
						}
					}
					if (weaponComponent.IsMeleeWeapon)
					{
						PerkHelper.AddPerkBonusForCharacter(NavalPerks.Mariner.ShatteringBlow, characterObject, isPrimaryBonus: true, ref bonuses, attackerBattleEnvironment);
						if (characterObject2 != null)
						{
							PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Mariner.ShatteringBlow, characterObject2, ref bonuses, attackerBattleEnvironment);
						}
					}
					else if (weaponComponent.IsConsumable && weaponComponent.RelevantSkill != null)
					{
						PerkHelper.AddPerkBonusForCharacter(NavalPerks.Mariner.ShatteringVolley, characterObject, isPrimaryBonus: true, ref bonuses, attackerBattleEnvironment);
						if (characterObject2 != null)
						{
							PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Mariner.ShatteringVolley, characterObject2, ref bonuses, attackerBattleEnvironment);
						}
					}
				}
				float num2 = bonuses.ResultNumber - baseArmor;
				num = MathF.Max(0f, baseArmor - num2);
			}
		}
		return num;
	}
}
