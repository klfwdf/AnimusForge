using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerAgentApplyDamageModel : AgentApplyDamageModel
{
	public override bool IsDamageIgnored(in AttackInformation attackInformation, in AttackCollisionData collisionData)
	{
		return false;
	}

	public override float ApplyDamageAmplifications(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
	{
		return baseDamage;
	}

	public override float ApplyDamageScaling(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
	{
		return baseDamage;
	}

	public override float ApplyDamageReductions(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
	{
		return baseDamage;
	}

	public override float ApplyGeneralDamageModifiers(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
	{
		float num = baseDamage;
		Agent attackerAgent = attackInformation.AttackerAgent;
		Agent victimAgent = attackInformation.VictimAgent;
		MPPerkObject.MPCombatPerkHandler combatPerkHandler = MPPerkObject.GetCombatPerkHandler(attackerAgent, victimAgent);
		if (combatPerkHandler != null)
		{
			if (collisionData.AttackBlockedWithShield)
			{
				float num2 = 1f + combatPerkHandler.GetShieldDamage(collisionData.CorrectSideShieldBlock) + combatPerkHandler.GetShieldDamageTaken(collisionData.CorrectSideShieldBlock);
				num = MathF.Max(0f, num * num2);
			}
			bool flag = MissionCombatMechanicsHelper.IsCollisionBoneDifferentThanWeaponAttachBone(in collisionData, attackInformation.WeaponAttachBoneIndex);
			MissionWeapon attackerWeapon = attackInformation.AttackerWeapon;
			DamageTypes damageType = ((attackerWeapon.IsEmpty || flag || collisionData.IsAlternativeAttack || collisionData.IsFallDamage || collisionData.IsHorseCharge) ? DamageTypes.Blunt : ((DamageTypes)collisionData.DamageType));
			float num3 = MathF.Max(0f, 1f + combatPerkHandler.GetDamage(attackerWeapon.CurrentUsageItem, damageType, collisionData.IsAlternativeAttack) + combatPerkHandler.GetDamageTaken(attackerWeapon.CurrentUsageItem, damageType));
			if (attackInformation.IsHeadShot && attackerWeapon.CurrentUsageItem != null && (attackerWeapon.CurrentUsageItem.IsConsumable || attackerWeapon.CurrentUsageItem.IsRangedWeapon))
			{
				num3 += combatPerkHandler.GetRangedHeadShotDamage();
			}
			num *= num3;
		}
		return num;
	}

	public override void DecideMissileWeaponFlags(Agent attackerAgent, in MissionWeapon missileWeapon, ref WeaponFlags missileWeaponFlags)
	{
	}

	public override bool DecideCrushedThrough(Agent attackerAgent, Agent defenderAgent, float totalAttackEnergy, Agent.UsageDirection attackDirection, StrikeType strikeType, WeaponComponentData defendItem, bool isPassiveUsage)
	{
		EquipmentIndex equipmentIndex = attackerAgent.GetOffhandWieldedItemIndex();
		if (equipmentIndex == EquipmentIndex.None)
		{
			equipmentIndex = attackerAgent.GetPrimaryWieldedItemIndex();
		}
		WeaponComponentData weaponComponentData = ((equipmentIndex != EquipmentIndex.None) ? attackerAgent.Equipment[equipmentIndex].CurrentUsageItem : null);
		if (weaponComponentData == null || isPassiveUsage || !weaponComponentData.WeaponFlags.HasAnyFlag(WeaponFlags.CanCrushThrough) || strikeType != StrikeType.Swing || attackDirection != Agent.UsageDirection.AttackUp)
		{
			return false;
		}
		float num = 58f;
		if (defendItem != null && defendItem.IsShield)
		{
			num *= 1.2f;
		}
		return totalAttackEnergy > num;
	}

	public override bool CanWeaponDealSneakAttack(in AttackInformation attackInformation, WeaponComponentData weapon)
	{
		return false;
	}

	public override bool CanWeaponDismount(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
	{
		if (!MBMath.IsBetween((int)blow.VictimBodyPart, 0, 6))
		{
			return false;
		}
		if (!attackerAgent.HasMount && blow.StrikeType == StrikeType.Swing && blow.WeaponRecord.WeaponFlags.HasAnyFlag(WeaponFlags.CanHook))
		{
			return true;
		}
		if (blow.StrikeType == StrikeType.Thrust)
		{
			return blow.WeaponRecord.WeaponFlags.HasAnyFlag(WeaponFlags.CanDismount);
		}
		return false;
	}

	public override void CalculateDefendedBlowStunMultipliers(Agent attackerAgent, Agent defenderAgent, CombatCollisionResult collisionResult, WeaponComponentData attackerWeapon, WeaponComponentData defenderWeapon, ref float attackerStunPeriod, ref float defenderStunPeriod)
	{
	}

	public override bool CanWeaponKnockback(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
	{
		if (MBMath.IsBetween((int)collisionData.VictimHitBodyPart, 0, 6) && !attackerWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.CanKnockDown))
		{
			if (!attackerWeapon.IsConsumable && (blow.BlowFlag & BlowFlags.CrushThrough) == 0)
			{
				if (blow.StrikeType == StrikeType.Thrust)
				{
					return blow.WeaponRecord.WeaponFlags.HasAnyFlag(WeaponFlags.WideGrip);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public override bool CanWeaponKnockDown(Agent attackerAgent, Agent victimAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
	{
		if (attackerWeapon.WeaponClass == WeaponClass.Boulder || attackerWeapon.WeaponClass == WeaponClass.BallistaBoulder)
		{
			return true;
		}
		BoneBodyPartType victimHitBodyPart = collisionData.VictimHitBodyPart;
		bool flag = MBMath.IsBetween((int)victimHitBodyPart, 0, 6);
		if (!victimAgent.HasMount && victimHitBodyPart == BoneBodyPartType.Legs)
		{
			flag = true;
		}
		if (flag && blow.WeaponRecord.WeaponFlags.HasAnyFlag(WeaponFlags.CanKnockDown))
		{
			if (!attackerWeapon.IsPolearm || blow.StrikeType != StrikeType.Thrust)
			{
				if (attackerWeapon.IsMeleeWeapon && blow.StrikeType == StrikeType.Swing)
				{
					return MissionCombatMechanicsHelper.DecideSweetSpotCollision(in collisionData);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public override float GetDismountPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		return 0f;
	}

	public override float GetKnockBackPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		return 0f;
	}

	public override float GetKnockDownPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		float num = 0f;
		if (attackerWeapon.WeaponClass == WeaponClass.Boulder || attackerWeapon.WeaponClass == WeaponClass.BallistaBoulder)
		{
			num += 0.25f;
		}
		else if (attackerWeapon.IsMeleeWeapon)
		{
			if (attackCollisionData.VictimHitBodyPart == BoneBodyPartType.Legs && blow.StrikeType == StrikeType.Swing)
			{
				num += 0.1f;
			}
			else if (attackCollisionData.VictimHitBodyPart == BoneBodyPartType.Head)
			{
				num += 0.15f;
			}
		}
		return num;
	}

	public override float GetHorseChargePenetration()
	{
		return 0.4f;
	}

	public override float CalculateStaggerThresholdDamage(Agent defenderAgent, in Blow blow)
	{
		float? num = MPPerkObject.GetPerkHandler(defenderAgent)?.GetDamageInterruptionThreshold();
		if (num.HasValue && num.Value > 0f)
		{
			return num.Value;
		}
		ManagedParametersEnum managedParameterEnum = ((blow.DamageType == DamageTypes.Cut) ? ManagedParametersEnum.DamageInterruptAttackThresholdCut : ((blow.DamageType != DamageTypes.Pierce) ? ManagedParametersEnum.DamageInterruptAttackThresholdBlunt : ManagedParametersEnum.DamageInterruptAttackThresholdPierce));
		return ManagedParameters.Instance.GetManagedParameter(managedParameterEnum);
	}

	public override float CalculateAlternativeAttackDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, WeaponComponentData weapon)
	{
		if (weapon == null)
		{
			return 2f;
		}
		if (weapon.WeaponClass == WeaponClass.LargeShield)
		{
			return 2f;
		}
		if (weapon.WeaponClass == WeaponClass.SmallShield)
		{
			return 1f;
		}
		if (weapon.IsTwoHanded)
		{
			return 2f;
		}
		return 1f;
	}

	public override float CalculatePassiveAttackDamage(BasicCharacterObject attackerCharacter, in AttackCollisionData collisionData, float baseDamage)
	{
		return baseDamage;
	}

	public override MeleeCollisionReaction DecidePassiveAttackCollisionReaction(Agent attacker, Agent defender, bool isFatalHit)
	{
		return MeleeCollisionReaction.Bounced;
	}

	public override float CalculateShieldDamage(in AttackInformation attackInformation, float baseDamage)
	{
		baseDamage *= 1.25f;
		MissionMultiplayerFlagDomination missionBehavior = Mission.Current.GetMissionBehavior<MissionMultiplayerFlagDomination>();
		if (missionBehavior != null && missionBehavior.GetMissionType() == MultiplayerGameType.Captain)
		{
			return baseDamage * 0.75f;
		}
		return baseDamage;
	}

	public override float CalculateSailFireDamage(Agent attackerAgent, IShipOrigin shipOrigin, float baseDamage, bool damageFromShipMachine)
	{
		return 0f;
	}

	public override float CalculateHullFireDamage(float baseFireDamage, IShipOrigin shipOrigin)
	{
		return 0f;
	}

	public override float GetDamageMultiplierForBodyPart(BoneBodyPartType bodyPart, DamageTypes type, bool isHuman, bool isMissile)
	{
		float result = 1f;
		switch (bodyPart)
		{
		case BoneBodyPartType.None:
			result = 1f;
			break;
		case BoneBodyPartType.Head:
			switch (type)
			{
			case DamageTypes.Invalid:
				result = 1.5f;
				break;
			case DamageTypes.Cut:
				result = 1.2f;
				break;
			case DamageTypes.Pierce:
				result = ((!isHuman) ? 1.2f : (isMissile ? 2f : 1.25f));
				break;
			case DamageTypes.Blunt:
				result = 1.2f;
				break;
			}
			break;
		case BoneBodyPartType.Neck:
			switch (type)
			{
			case DamageTypes.Invalid:
				result = 1.5f;
				break;
			case DamageTypes.Cut:
				result = 1.2f;
				break;
			case DamageTypes.Pierce:
				result = ((!isHuman) ? 1.2f : (isMissile ? 2f : 1.25f));
				break;
			case DamageTypes.Blunt:
				result = 1.2f;
				break;
			}
			break;
		case BoneBodyPartType.Chest:
		case BoneBodyPartType.Abdomen:
		case BoneBodyPartType.ShoulderLeft:
		case BoneBodyPartType.ShoulderRight:
		case BoneBodyPartType.ArmLeft:
		case BoneBodyPartType.ArmRight:
			result = ((!isHuman) ? 0.8f : 1f);
			break;
		case BoneBodyPartType.Legs:
			result = 0.8f;
			break;
		}
		return result;
	}

	public override bool CanWeaponIgnoreFriendlyFireChecks(WeaponComponentData weapon)
	{
		if (weapon != null && weapon.IsConsumable && weapon.WeaponFlags.HasAnyFlag(WeaponFlags.CanPenetrateShield) && weapon.WeaponFlags.HasAnyFlag(WeaponFlags.MultiplePenetration))
		{
			return true;
		}
		return false;
	}

	public override bool DecideAgentShrugOffBlow(Agent victimAgent, in AttackCollisionData collisionData, in Blow blow)
	{
		return MissionCombatMechanicsHelper.DecideAgentShrugOffBlow(victimAgent, in collisionData, in blow);
	}

	public override bool DecideAgentDismountedByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
	{
		return MissionCombatMechanicsHelper.DecideAgentDismountedByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
	}

	public override bool DecideAgentKnockedBackByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
	{
		return MissionCombatMechanicsHelper.DecideAgentKnockedBackByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
	}

	public override bool DecideAgentKnockedDownByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
	{
		return MissionCombatMechanicsHelper.DecideAgentKnockedDownByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
	}

	public override bool DecideMountRearedByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
	{
		return MissionCombatMechanicsHelper.DecideMountRearedByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
	}

	public override void DecideWeaponCollisionReaction(in Blow registeredBlow, in AttackCollisionData collisionData, Agent attacker, Agent defender, in MissionWeapon attackerWeapon, bool isFatalHit, bool isShruggedOff, float momentumRemaining, out MeleeCollisionReaction colReaction)
	{
		MissionCombatMechanicsHelper.DecideWeaponCollisionReaction(in registeredBlow, in collisionData, attacker, defender, in attackerWeapon, isFatalHit, isShruggedOff, momentumRemaining, out colReaction);
	}

	public override bool ShouldMissilePassThroughAfterShieldBreak(Agent attackerAgent, WeaponComponentData attackerWeapon)
	{
		return false;
	}

	public override float CalculateRemainingMomentum(float originalMomentum, in Blow b, in AttackCollisionData collisionData, Agent attacker, Agent victim, in MissionWeapon attackerWeapon, bool isCrushThrough)
	{
		return CalculateDefaultRemainingMomentum(originalMomentum, in b, in collisionData, attacker, victim, in attackerWeapon, isCrushThrough);
	}
}
