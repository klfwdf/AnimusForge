using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ComponentInterfaces;

public abstract class AgentApplyDamageModel : MBGameModel<AgentApplyDamageModel>
{
	public float CalculateDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
	{
		AgentApplyDamageModel agentApplyDamageModel = MissionGameModels.Current.AgentApplyDamageModel;
		if (agentApplyDamageModel.IsDamageIgnored(in attackInformation, in collisionData))
		{
			return 0f;
		}
		float baseDamage2 = agentApplyDamageModel.ApplyDamageAmplifications(in attackInformation, in collisionData, baseDamage);
		baseDamage2 = agentApplyDamageModel.ApplyDamageScaling(in attackInformation, in collisionData, baseDamage2);
		baseDamage2 = agentApplyDamageModel.ApplyDamageReductions(in attackInformation, in collisionData, baseDamage2);
		baseDamage2 = agentApplyDamageModel.ApplyGeneralDamageModifiers(in attackInformation, in collisionData, baseDamage2);
		return MathF.Max(0f, baseDamage2);
	}

	public abstract bool IsDamageIgnored(in AttackInformation attackInformation, in AttackCollisionData collisionData);

	public abstract float ApplyDamageAmplifications(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage);

	public abstract float ApplyDamageScaling(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage);

	public abstract float ApplyDamageReductions(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage);

	public abstract float ApplyGeneralDamageModifiers(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage);

	public abstract void DecideMissileWeaponFlags(Agent attackerAgent, in MissionWeapon missileWeapon, ref WeaponFlags missileWeaponFlags);

	public abstract void CalculateDefendedBlowStunMultipliers(Agent attackerAgent, Agent defenderAgent, CombatCollisionResult collisionResult, WeaponComponentData attackerWeapon, WeaponComponentData defenderWeapon, ref float attackerStunPeriod, ref float defenderStunPeriod);

	public abstract float CalculateStaggerThresholdDamage(Agent defenderAgent, in Blow blow);

	public abstract float CalculateAlternativeAttackDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, WeaponComponentData weapon);

	public abstract float CalculatePassiveAttackDamage(BasicCharacterObject attackerCharacter, in AttackCollisionData collisionData, float baseDamage);

	public abstract MeleeCollisionReaction DecidePassiveAttackCollisionReaction(Agent attacker, Agent defender, bool isFatalHit);

	public abstract void DecideWeaponCollisionReaction(in Blow registeredBlow, in AttackCollisionData collisionData, Agent attacker, Agent defender, in MissionWeapon attackerWeapon, bool isFatalHit, bool isShruggedOff, float momentumRemaining, out MeleeCollisionReaction colReaction);

	public abstract float CalculateShieldDamage(in AttackInformation attackInformation, float baseDamage);

	public abstract float CalculateSailFireDamage(Agent attackerAgent, IShipOrigin shipOrigin, float baseDamage, bool damageFromShipMachine);

	public abstract float CalculateHullFireDamage(float baseFireDamage, IShipOrigin shipOrigin);

	public abstract float GetDamageMultiplierForBodyPart(BoneBodyPartType bodyPart, DamageTypes type, bool isHuman, bool isMissile);

	public abstract bool CanWeaponIgnoreFriendlyFireChecks(WeaponComponentData weapon);

	public abstract bool CanWeaponDealSneakAttack(in AttackInformation attackInformation, WeaponComponentData weapon);

	public abstract bool CanWeaponDismount(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData);

	public abstract bool CanWeaponKnockback(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData);

	public abstract bool CanWeaponKnockDown(Agent attackerAgent, Agent victimAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData);

	public abstract bool DecideCrushedThrough(Agent attackerAgent, Agent defenderAgent, float totalAttackEnergy, Agent.UsageDirection attackDirection, StrikeType strikeType, WeaponComponentData defendItem, bool isPassiveUsageHit);

	public abstract float CalculateRemainingMomentum(float originalMomentum, in Blow b, in AttackCollisionData collisionData, Agent attacker, Agent victim, in MissionWeapon attackerWeapon, bool isCrushThrough);

	protected float CalculateDefaultRemainingMomentum(float originalMomentum, in Blow b, in AttackCollisionData collisionData, Agent attacker, Agent victim, in MissionWeapon attackerWeapon, bool isCrushThrough)
	{
		float num = 0f;
		if (isCrushThrough)
		{
			num = originalMomentum * 0.3f;
		}
		else if (b.InflictedDamage > 0 && !collisionData.AttackBlockedWithShield && !collisionData.CollidedWithShieldOnBack && collisionData.IsColliderAgent && !collisionData.IsHorseCharge)
		{
			if (attacker != null && attacker.IsDoingPassiveAttack)
			{
				num = originalMomentum * 0.5f;
			}
			else if (!MissionCombatMechanicsHelper.HitWithAnotherBone(in collisionData, attacker, in attackerWeapon) && !attackerWeapon.IsEmpty && b.StrikeType != StrikeType.Thrust && !attackerWeapon.IsEmpty && attackerWeapon.CurrentUsageItem.CanHitMultipleTargets)
			{
				num = originalMomentum * (1f - b.AbsorbedByArmor / (float)b.InflictedDamage);
				num *= 0.5f;
				if (num < 0.25f)
				{
					num = 0f;
				}
			}
		}
		return num;
	}

	public abstract bool DecideAgentShrugOffBlow(Agent victimAgent, in AttackCollisionData collisionData, in Blow blow);

	public abstract bool DecideAgentDismountedByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow);

	public abstract bool DecideAgentKnockedBackByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow);

	public abstract bool DecideAgentKnockedDownByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow);

	public abstract bool DecideMountRearedByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow);

	public abstract bool ShouldMissilePassThroughAfterShieldBreak(Agent attackerAgent, WeaponComponentData attackerWeapon);

	public abstract float GetDismountPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData);

	public abstract float GetKnockBackPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData);

	public abstract float GetKnockDownPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData);

	public abstract float GetHorseChargePenetration();
}
