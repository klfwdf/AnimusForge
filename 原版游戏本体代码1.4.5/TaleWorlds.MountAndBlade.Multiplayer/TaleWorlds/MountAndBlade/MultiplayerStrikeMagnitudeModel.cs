using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerStrikeMagnitudeModel : StrikeMagnitudeCalculationModel
{
	public override float CalculateStrikeMagnitudeForMissile(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float missileSpeed)
	{
		float missileTotalDamage = collisionData.MissileTotalDamage;
		float missileStartingBaseSpeed = collisionData.MissileStartingBaseSpeed;
		float num = missileSpeed;
		float num2 = missileSpeed - missileStartingBaseSpeed;
		if (num2 > 0f)
		{
			MPPerkObject.MPCombatPerkHandler combatPerkHandler = MPPerkObject.GetCombatPerkHandler(attackInformation.AttackerAgent, attackInformation.VictimAgent);
			if (combatPerkHandler != null)
			{
				float num3 = MathF.Max(MathF.Sqrt(1f + combatPerkHandler.GetSpeedBonusEffectiveness(weapon.CurrentUsageItem, (DamageTypes)collisionData.DamageType)) - 1f, 0f);
				num += num2 * num3;
			}
		}
		num /= missileStartingBaseSpeed;
		return num * num * missileTotalDamage;
	}

	public override float CalculateStrikeMagnitudeForSwing(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float swingSpeed, float impactPoint, float extraLinearSpeed)
	{
		float num = extraLinearSpeed;
		if (extraLinearSpeed > 0f)
		{
			MPPerkObject.MPCombatPerkHandler combatPerkHandler = MPPerkObject.GetCombatPerkHandler(attackInformation.AttackerAgent, attackInformation.VictimAgent);
			if (combatPerkHandler != null)
			{
				float num2 = MathF.Max(MathF.Sqrt(1f + combatPerkHandler.GetSpeedBonusEffectiveness(weapon.CurrentUsageItem, (DamageTypes)collisionData.DamageType)) - 1f, 0f);
				num += num * num2;
			}
		}
		WeaponComponentData currentUsageItem = weapon.CurrentUsageItem;
		return CombatStatCalculator.CalculateStrikeMagnitudeForSwing(swingSpeed, impactPoint, weapon.Item.Weight, currentUsageItem.GetRealWeaponLength(), currentUsageItem.TotalInertia, currentUsageItem.CenterOfMass, num);
	}

	public override float CalculateStrikeMagnitudeForUnarmedAttack(in AttackInformation attackInformation, in AttackCollisionData collisionData, float progressEffect, float momentumRemaining)
	{
		return momentumRemaining * progressEffect * ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.FistFightDamageMultiplier);
	}

	public override float CalculateStrikeMagnitudeForThrust(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float thrustWeaponSpeed, float extraLinearSpeed, bool isThrown = false)
	{
		float num = extraLinearSpeed;
		if (extraLinearSpeed > 0f)
		{
			MPPerkObject.MPCombatPerkHandler combatPerkHandler = MPPerkObject.GetCombatPerkHandler(attackInformation.AttackerAgent, attackInformation.VictimAgent);
			if (combatPerkHandler != null)
			{
				float num2 = MathF.Max(MathF.Sqrt(1f + combatPerkHandler.GetSpeedBonusEffectiveness(weapon.CurrentUsageItem, (DamageTypes)collisionData.DamageType)) - 1f, 0f);
				num += num * num2;
			}
		}
		return CombatStatCalculator.CalculateStrikeMagnitudeForThrust(thrustWeaponSpeed, weapon.Item.Weight, num, isThrown);
	}

	public override float ComputeRawDamage(DamageTypes damageType, float magnitude, float armorEffectiveness, float absorbedDamageRatio)
	{
		float bluntDamageFactorByDamageType = GetBluntDamageFactorByDamageType(damageType);
		float num = 100f / (100f + armorEffectiveness);
		float num2 = magnitude * num;
		float num3 = bluntDamageFactorByDamageType * num2;
		if (damageType != DamageTypes.Blunt)
		{
			float num4;
			switch (damageType)
			{
			case DamageTypes.Cut:
				num4 = MathF.Max(0f, magnitude * (1f - 0.6f * armorEffectiveness / (20f + 0.4f * armorEffectiveness)));
				break;
			case DamageTypes.Pierce:
				num4 = MathF.Max(0f, magnitude * (45f / (45f + armorEffectiveness)));
				break;
			default:
				Debug.FailedAssert("Given damage type is invalid.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer\\ComponentInterfaces\\MultiplayerStrikeMagnitudeModel.cs", "ComputeRawDamage", 107);
				return 0f;
			}
			num3 += (1f - bluntDamageFactorByDamageType) * num4;
		}
		return num3 * absorbedDamageRatio;
	}

	public override float GetBluntDamageFactorByDamageType(DamageTypes damageType)
	{
		float result = 0f;
		switch (damageType)
		{
		case DamageTypes.Blunt:
			result = 1f;
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
