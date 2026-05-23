using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace TaleWorlds.MountAndBlade;

public class DefaultStrikeMagnitudeModel : StrikeMagnitudeCalculationModel
{
	public override float CalculateStrikeMagnitudeForMissile(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float missileSpeed)
	{
		float missileTotalDamage = collisionData.MissileTotalDamage;
		float missileStartingBaseSpeed = collisionData.MissileStartingBaseSpeed;
		float num = missileSpeed / missileStartingBaseSpeed;
		return num * num * missileTotalDamage;
	}

	public override float CalculateStrikeMagnitudeForSwing(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float swingSpeed, float impactPointAsPercent, float extraLinearSpeed)
	{
		WeaponComponentData currentUsageItem = weapon.CurrentUsageItem;
		return CombatStatCalculator.CalculateStrikeMagnitudeForSwing(swingSpeed, impactPointAsPercent, weapon.Item.Weight, currentUsageItem.GetRealWeaponLength(), currentUsageItem.TotalInertia, currentUsageItem.CenterOfMass, extraLinearSpeed);
	}

	public override float CalculateStrikeMagnitudeForUnarmedAttack(in AttackInformation attackInformation, in AttackCollisionData collisionData, float progressEffect, float momentumRemaining)
	{
		return momentumRemaining * progressEffect * ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.FistFightDamageMultiplier);
	}

	public override float CalculateStrikeMagnitudeForThrust(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float thrustWeaponSpeed, float extraLinearSpeed, bool isThrown = false)
	{
		return CombatStatCalculator.CalculateStrikeMagnitudeForThrust(thrustWeaponSpeed, weapon.Item.Weight, extraLinearSpeed, isThrown);
	}

	public override float ComputeRawDamage(DamageTypes damageType, float magnitude, float armorEffectiveness, float absorbedDamageRatio)
	{
		float bluntDamageFactorByDamageType = GetBluntDamageFactorByDamageType(damageType);
		float num = 50f / (50f + armorEffectiveness);
		float num2 = magnitude * num;
		float num3 = bluntDamageFactorByDamageType * num2;
		float num4;
		switch (damageType)
		{
		case DamageTypes.Cut:
			num4 = MathF.Max(0f, num2 - armorEffectiveness * 0.5f);
			break;
		case DamageTypes.Pierce:
			num4 = MathF.Max(0f, num2 - armorEffectiveness * 0.33f);
			break;
		case DamageTypes.Blunt:
			num4 = MathF.Max(0f, num2 - armorEffectiveness * 0.2f);
			break;
		default:
			Debug.FailedAssert("Given damage type is invalid.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\ComponentInterfaces\\DefaultStrikeMagnitudeModel.cs", "ComputeRawDamage", 70);
			return 0f;
		}
		num3 += (1f - bluntDamageFactorByDamageType) * num4;
		return num3 * absorbedDamageRatio;
	}

	public override float GetBluntDamageFactorByDamageType(DamageTypes damageType)
	{
		float result = 0f;
		switch (damageType)
		{
		case DamageTypes.Blunt:
			result = 0.6f;
			break;
		case DamageTypes.Cut:
			result = 0.1f;
			break;
		case DamageTypes.Pierce:
			result = 0.25f;
			break;
		}
		return result;
	}

	public override float CalculateHorseArcheryFactor(BasicCharacterObject characterObject)
	{
		return 100f;
	}

	public override float CalculateBaseBlowMagnitudeForPassiveUsage(in AttackInformation attackInformation, in AttackCollisionData collisionData, float extraLinearSpeed)
	{
		return CombatStatCalculator.CalculateBaseBlowMagnitudeForPassiveUsage(attackInformation.AttackerWeapon.Item.Weight, extraLinearSpeed);
	}
}
