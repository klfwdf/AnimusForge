using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class AgentDrivenProperties
{
	private readonly float[] _statValues;

	internal float[] Values => _statValues;

	public float SwingSpeedMultiplier
	{
		get
		{
			return GetStat(DrivenProperty.SwingSpeedMultiplier);
		}
		set
		{
			SetStat(DrivenProperty.SwingSpeedMultiplier, value);
		}
	}

	public float ThrustOrRangedReadySpeedMultiplier
	{
		get
		{
			return GetStat(DrivenProperty.ThrustOrRangedReadySpeedMultiplier);
		}
		set
		{
			SetStat(DrivenProperty.ThrustOrRangedReadySpeedMultiplier, value);
		}
	}

	public float HandlingMultiplier
	{
		get
		{
			return GetStat(DrivenProperty.HandlingMultiplier);
		}
		set
		{
			SetStat(DrivenProperty.HandlingMultiplier, value);
		}
	}

	public float ReloadSpeed
	{
		get
		{
			return GetStat(DrivenProperty.ReloadSpeed);
		}
		set
		{
			SetStat(DrivenProperty.ReloadSpeed, value);
		}
	}

	public float MissileSpeedMultiplier
	{
		get
		{
			return GetStat(DrivenProperty.MissileSpeedMultiplier);
		}
		set
		{
			SetStat(DrivenProperty.MissileSpeedMultiplier, value);
		}
	}

	public float WeaponInaccuracy
	{
		get
		{
			return GetStat(DrivenProperty.WeaponInaccuracy);
		}
		set
		{
			SetStat(DrivenProperty.WeaponInaccuracy, value);
		}
	}

	public float WeaponMaxMovementAccuracyPenalty
	{
		get
		{
			return GetStat(DrivenProperty.WeaponWorstMobileAccuracyPenalty);
		}
		set
		{
			SetStat(DrivenProperty.WeaponWorstMobileAccuracyPenalty, value);
		}
	}

	public float WeaponMaxUnsteadyAccuracyPenalty
	{
		get
		{
			return GetStat(DrivenProperty.WeaponWorstUnsteadyAccuracyPenalty);
		}
		set
		{
			SetStat(DrivenProperty.WeaponWorstUnsteadyAccuracyPenalty, value);
		}
	}

	public float WeaponBestAccuracyWaitTime
	{
		get
		{
			return GetStat(DrivenProperty.WeaponBestAccuracyWaitTime);
		}
		set
		{
			SetStat(DrivenProperty.WeaponBestAccuracyWaitTime, value);
		}
	}

	public float WeaponUnsteadyBeginTime
	{
		get
		{
			return GetStat(DrivenProperty.WeaponUnsteadyBeginTime);
		}
		set
		{
			SetStat(DrivenProperty.WeaponUnsteadyBeginTime, value);
		}
	}

	public float WeaponUnsteadyEndTime
	{
		get
		{
			return GetStat(DrivenProperty.WeaponUnsteadyEndTime);
		}
		set
		{
			SetStat(DrivenProperty.WeaponUnsteadyEndTime, value);
		}
	}

	public float WeaponRotationalAccuracyPenaltyInRadians
	{
		get
		{
			return GetStat(DrivenProperty.WeaponRotationalAccuracyPenaltyInRadians);
		}
		set
		{
			SetStat(DrivenProperty.WeaponRotationalAccuracyPenaltyInRadians, value);
		}
	}

	public float WeaponExternalAccelerationAccuracyPenalty
	{
		get
		{
			return GetStat(DrivenProperty.WeaponExternalAccelerationAccuracyPenalty);
		}
		set
		{
			SetStat(DrivenProperty.WeaponExternalAccelerationAccuracyPenalty, value);
		}
	}

	public float ArmorEncumbrance
	{
		get
		{
			return GetStat(DrivenProperty.ArmorEncumbrance);
		}
		set
		{
			SetStat(DrivenProperty.ArmorEncumbrance, value);
		}
	}

	public float DamageMultiplierBonus
	{
		get
		{
			return GetStat(DrivenProperty.DamageMultiplierBonus);
		}
		set
		{
			SetStat(DrivenProperty.DamageMultiplierBonus, value);
		}
	}

	public float ThrowingWeaponDamageMultiplierBonus
	{
		get
		{
			return GetStat(DrivenProperty.ThrowingWeaponDamageMultiplierBonus);
		}
		set
		{
			SetStat(DrivenProperty.ThrowingWeaponDamageMultiplierBonus, value);
		}
	}

	public float MeleeWeaponDamageMultiplierBonus
	{
		get
		{
			return GetStat(DrivenProperty.MeleeWeaponDamageMultiplierBonus);
		}
		set
		{
			SetStat(DrivenProperty.MeleeWeaponDamageMultiplierBonus, value);
		}
	}

	public float ArmorPenetrationMultiplierCrossbow
	{
		get
		{
			return GetStat(DrivenProperty.ArmorPenetrationMultiplierCrossbow);
		}
		set
		{
			SetStat(DrivenProperty.ArmorPenetrationMultiplierCrossbow, value);
		}
	}

	public float ArmorPenetrationMultiplierBow
	{
		get
		{
			return GetStat(DrivenProperty.ArmorPenetrationMultiplierBow);
		}
		set
		{
			SetStat(DrivenProperty.ArmorPenetrationMultiplierBow, value);
		}
	}

	public float WeaponsEncumbrance
	{
		get
		{
			return GetStat(DrivenProperty.WeaponsEncumbrance);
		}
		set
		{
			SetStat(DrivenProperty.WeaponsEncumbrance, value);
		}
	}

	public float ArmorHead
	{
		get
		{
			return GetStat(DrivenProperty.ArmorHead);
		}
		set
		{
			SetStat(DrivenProperty.ArmorHead, value);
		}
	}

	public float ArmorTorso
	{
		get
		{
			return GetStat(DrivenProperty.ArmorTorso);
		}
		set
		{
			SetStat(DrivenProperty.ArmorTorso, value);
		}
	}

	public float ArmorLegs
	{
		get
		{
			return GetStat(DrivenProperty.ArmorLegs);
		}
		set
		{
			SetStat(DrivenProperty.ArmorLegs, value);
		}
	}

	public float ArmorArms
	{
		get
		{
			return GetStat(DrivenProperty.ArmorArms);
		}
		set
		{
			SetStat(DrivenProperty.ArmorArms, value);
		}
	}

	public float AttributeRiding
	{
		get
		{
			return GetStat(DrivenProperty.AttributeRiding);
		}
		set
		{
			SetStat(DrivenProperty.AttributeRiding, value);
		}
	}

	public float AttributeShield
	{
		get
		{
			return GetStat(DrivenProperty.AttributeShield);
		}
		set
		{
			SetStat(DrivenProperty.AttributeShield, value);
		}
	}

	public float AttributeShieldMissileCollisionBodySizeAdder
	{
		get
		{
			return GetStat(DrivenProperty.AttributeShieldMissileCollisionBodySizeAdder);
		}
		set
		{
			SetStat(DrivenProperty.AttributeShieldMissileCollisionBodySizeAdder, value);
		}
	}

	public float ShieldBashStunDurationMultiplier
	{
		get
		{
			return GetStat(DrivenProperty.ShieldBashStunDurationMultiplier);
		}
		set
		{
			SetStat(DrivenProperty.ShieldBashStunDurationMultiplier, value);
		}
	}

	public float KickStunDurationMultiplier
	{
		get
		{
			return GetStat(DrivenProperty.KickStunDurationMultiplier);
		}
		set
		{
			SetStat(DrivenProperty.KickStunDurationMultiplier, value);
		}
	}

	public float ReloadMovementPenaltyFactor
	{
		get
		{
			return GetStat(DrivenProperty.ReloadMovementPenaltyFactor);
		}
		set
		{
			SetStat(DrivenProperty.ReloadMovementPenaltyFactor, value);
		}
	}

	public float TopSpeedReachDuration
	{
		get
		{
			return GetStat(DrivenProperty.TopSpeedReachDuration);
		}
		set
		{
			SetStat(DrivenProperty.TopSpeedReachDuration, value);
		}
	}

	public float MaxSpeedMultiplier
	{
		get
		{
			return GetStat(DrivenProperty.MaxSpeedMultiplier);
		}
		set
		{
			SetStat(DrivenProperty.MaxSpeedMultiplier, value);
		}
	}

	public float CombatMaxSpeedMultiplier
	{
		get
		{
			return GetStat(DrivenProperty.CombatMaxSpeedMultiplier);
		}
		set
		{
			SetStat(DrivenProperty.CombatMaxSpeedMultiplier, value);
		}
	}

	public float CrouchedSpeedMultiplier
	{
		get
		{
			return GetStat(DrivenProperty.CrouchedSpeedMultiplier);
		}
		set
		{
			SetStat(DrivenProperty.CrouchedSpeedMultiplier, value);
		}
	}

	public float AttributeHorseArchery
	{
		get
		{
			return GetStat(DrivenProperty.AttributeHorseArchery);
		}
		set
		{
			SetStat(DrivenProperty.AttributeHorseArchery, value);
		}
	}

	public float AttributeCourage
	{
		get
		{
			return GetStat(DrivenProperty.AttributeCourage);
		}
		set
		{
			SetStat(DrivenProperty.AttributeCourage, value);
		}
	}

	public float MountManeuver
	{
		get
		{
			return GetStat(DrivenProperty.MountManeuver);
		}
		set
		{
			SetStat(DrivenProperty.MountManeuver, value);
		}
	}

	public float MountSpeed
	{
		get
		{
			return GetStat(DrivenProperty.MountSpeed);
		}
		set
		{
			SetStat(DrivenProperty.MountSpeed, value);
		}
	}

	public float MountDashAccelerationMultiplier
	{
		get
		{
			return GetStat(DrivenProperty.MountDashAccelerationMultiplier);
		}
		set
		{
			SetStat(DrivenProperty.MountDashAccelerationMultiplier, value);
		}
	}

	public float MountChargeDamage
	{
		get
		{
			return GetStat(DrivenProperty.MountChargeDamage);
		}
		set
		{
			SetStat(DrivenProperty.MountChargeDamage, value);
		}
	}

	public float MountDifficulty
	{
		get
		{
			return GetStat(DrivenProperty.MountDifficulty);
		}
		set
		{
			SetStat(DrivenProperty.MountDifficulty, value);
		}
	}

	public float BipedalRangedReadySpeedMultiplier
	{
		get
		{
			return GetStat(DrivenProperty.BipedalRangedReadySpeedMultiplier);
		}
		set
		{
			SetStat(DrivenProperty.BipedalRangedReadySpeedMultiplier, value);
		}
	}

	public float BipedalRangedReloadSpeedMultiplier
	{
		get
		{
			return GetStat(DrivenProperty.BipedalRangedReloadSpeedMultiplier);
		}
		set
		{
			SetStat(DrivenProperty.BipedalRangedReloadSpeedMultiplier, value);
		}
	}

	public float AiShooterErrorWoRangeUpdate
	{
		get
		{
			return GetStat(DrivenProperty.AiShooterErrorWoRangeUpdate);
		}
		set
		{
			SetStat(DrivenProperty.AiShooterErrorWoRangeUpdate, value);
		}
	}

	public float AiRangedHorsebackMissileRange
	{
		get
		{
			return GetStat(DrivenProperty.AiRangedHorsebackMissileRange);
		}
		set
		{
			SetStat(DrivenProperty.AiRangedHorsebackMissileRange, value);
		}
	}

	public float AiFacingMissileWatch
	{
		get
		{
			return GetStat(DrivenProperty.AiFacingMissileWatch);
		}
		set
		{
			SetStat(DrivenProperty.AiFacingMissileWatch, value);
		}
	}

	public float AiFlyingMissileCheckRadius
	{
		get
		{
			return GetStat(DrivenProperty.AiFlyingMissileCheckRadius);
		}
		set
		{
			SetStat(DrivenProperty.AiFlyingMissileCheckRadius, value);
		}
	}

	public float AiShootFreq
	{
		get
		{
			return GetStat(DrivenProperty.AiShootFreq);
		}
		set
		{
			SetStat(DrivenProperty.AiShootFreq, value);
		}
	}

	public float AiWaitBeforeShootFactor
	{
		get
		{
			return GetStat(DrivenProperty.AiWaitBeforeShootFactor);
		}
		set
		{
			SetStat(DrivenProperty.AiWaitBeforeShootFactor, value);
		}
	}

	public float AIBlockOnDecideAbility
	{
		get
		{
			return GetStat(DrivenProperty.AIBlockOnDecideAbility);
		}
		set
		{
			SetStat(DrivenProperty.AIBlockOnDecideAbility, value);
		}
	}

	public float AIParryOnDecideAbility
	{
		get
		{
			return GetStat(DrivenProperty.AIParryOnDecideAbility);
		}
		set
		{
			SetStat(DrivenProperty.AIParryOnDecideAbility, value);
		}
	}

	public float AiTryChamberAttackOnDecide
	{
		get
		{
			return GetStat(DrivenProperty.AiTryChamberAttackOnDecide);
		}
		set
		{
			SetStat(DrivenProperty.AiTryChamberAttackOnDecide, value);
		}
	}

	public float AIAttackOnParryChance
	{
		get
		{
			return GetStat(DrivenProperty.AIAttackOnParryChance);
		}
		set
		{
			SetStat(DrivenProperty.AIAttackOnParryChance, value);
		}
	}

	public float AiAttackOnParryTiming
	{
		get
		{
			return GetStat(DrivenProperty.AiAttackOnParryTiming);
		}
		set
		{
			SetStat(DrivenProperty.AiAttackOnParryTiming, value);
		}
	}

	public float AIDecideOnAttackChance
	{
		get
		{
			return GetStat(DrivenProperty.AIDecideOnAttackChance);
		}
		set
		{
			SetStat(DrivenProperty.AIDecideOnAttackChance, value);
		}
	}

	public float AIParryOnAttackAbility
	{
		get
		{
			return GetStat(DrivenProperty.AIParryOnAttackAbility);
		}
		set
		{
			SetStat(DrivenProperty.AIParryOnAttackAbility, value);
		}
	}

	public float AiKick
	{
		get
		{
			return GetStat(DrivenProperty.AiKick);
		}
		set
		{
			SetStat(DrivenProperty.AiKick, value);
		}
	}

	public float AiAttackCalculationMaxTimeFactor
	{
		get
		{
			return GetStat(DrivenProperty.AiAttackCalculationMaxTimeFactor);
		}
		set
		{
			SetStat(DrivenProperty.AiAttackCalculationMaxTimeFactor, value);
		}
	}

	public float AiDecideOnAttackWhenReceiveHitTiming
	{
		get
		{
			return GetStat(DrivenProperty.AiDecideOnAttackWhenReceiveHitTiming);
		}
		set
		{
			SetStat(DrivenProperty.AiDecideOnAttackWhenReceiveHitTiming, value);
		}
	}

	public float AiDecideOnAttackContinueAction
	{
		get
		{
			return GetStat(DrivenProperty.AiDecideOnAttackContinueAction);
		}
		set
		{
			SetStat(DrivenProperty.AiDecideOnAttackContinueAction, value);
		}
	}

	public float AiDecideOnAttackingContinue
	{
		get
		{
			return GetStat(DrivenProperty.AiDecideOnAttackingContinue);
		}
		set
		{
			SetStat(DrivenProperty.AiDecideOnAttackingContinue, value);
		}
	}

	public float AIParryOnAttackingContinueAbility
	{
		get
		{
			return GetStat(DrivenProperty.AIParryOnAttackingContinueAbility);
		}
		set
		{
			SetStat(DrivenProperty.AIParryOnAttackingContinueAbility, value);
		}
	}

	public float AIDecideOnRealizeEnemyBlockingAttackAbility
	{
		get
		{
			return GetStat(DrivenProperty.AIDecideOnRealizeEnemyBlockingAttackAbility);
		}
		set
		{
			SetStat(DrivenProperty.AIDecideOnRealizeEnemyBlockingAttackAbility, value);
		}
	}

	public float AIRealizeBlockingFromIncorrectSideAbility
	{
		get
		{
			return GetStat(DrivenProperty.AIRealizeBlockingFromIncorrectSideAbility);
		}
		set
		{
			SetStat(DrivenProperty.AIRealizeBlockingFromIncorrectSideAbility, value);
		}
	}

	public float AiAttackingShieldDefenseChance
	{
		get
		{
			return GetStat(DrivenProperty.AiAttackingShieldDefenseChance);
		}
		set
		{
			SetStat(DrivenProperty.AiAttackingShieldDefenseChance, value);
		}
	}

	public float AiAttackingShieldDefenseTimer
	{
		get
		{
			return GetStat(DrivenProperty.AiAttackingShieldDefenseTimer);
		}
		set
		{
			SetStat(DrivenProperty.AiAttackingShieldDefenseTimer, value);
		}
	}

	public float AiCheckApplyMovementInterval
	{
		get
		{
			return GetStat(DrivenProperty.AiCheckApplyMovementInterval);
		}
		set
		{
			SetStat(DrivenProperty.AiCheckApplyMovementInterval, value);
		}
	}

	public float AiCheckCalculateMovementInterval
	{
		get
		{
			return GetStat(DrivenProperty.AiCheckCalculateMovementInterval);
		}
		set
		{
			SetStat(DrivenProperty.AiCheckCalculateMovementInterval, value);
		}
	}

	public float AiCheckDecideSimpleBehaviorInterval
	{
		get
		{
			return GetStat(DrivenProperty.AiCheckDecideSimpleBehaviorInterval);
		}
		set
		{
			SetStat(DrivenProperty.AiCheckDecideSimpleBehaviorInterval, value);
		}
	}

	public float AiCheckDoSimpleBehaviorInterval
	{
		get
		{
			return GetStat(DrivenProperty.AiCheckDoSimpleBehaviorInterval);
		}
		set
		{
			SetStat(DrivenProperty.AiCheckDoSimpleBehaviorInterval, value);
		}
	}

	public float AiMovementDelayFactor
	{
		get
		{
			return GetStat(DrivenProperty.AiMovementDelayFactor);
		}
		set
		{
			SetStat(DrivenProperty.AiMovementDelayFactor, value);
		}
	}

	public float AiParryDecisionChangeValue
	{
		get
		{
			return GetStat(DrivenProperty.AiParryDecisionChangeValue);
		}
		set
		{
			SetStat(DrivenProperty.AiParryDecisionChangeValue, value);
		}
	}

	public float AiDefendWithShieldDecisionChanceValue
	{
		get
		{
			return GetStat(DrivenProperty.AiDefendWithShieldDecisionChanceValue);
		}
		set
		{
			SetStat(DrivenProperty.AiDefendWithShieldDecisionChanceValue, value);
		}
	}

	public float AiMoveEnemySideTimeValue
	{
		get
		{
			return GetStat(DrivenProperty.AiMoveEnemySideTimeValue);
		}
		set
		{
			SetStat(DrivenProperty.AiMoveEnemySideTimeValue, value);
		}
	}

	public float AiMinimumDistanceToContinueFactor
	{
		get
		{
			return GetStat(DrivenProperty.AiMinimumDistanceToContinueFactor);
		}
		set
		{
			SetStat(DrivenProperty.AiMinimumDistanceToContinueFactor, value);
		}
	}

	public float AiChargeHorsebackTargetDistFactor
	{
		get
		{
			return GetStat(DrivenProperty.AiChargeHorsebackTargetDistFactor);
		}
		set
		{
			SetStat(DrivenProperty.AiChargeHorsebackTargetDistFactor, value);
		}
	}

	public float AiRangerLeadErrorMin
	{
		get
		{
			return GetStat(DrivenProperty.AiRangerLeadErrorMin);
		}
		set
		{
			SetStat(DrivenProperty.AiRangerLeadErrorMin, value);
		}
	}

	public float AiRangerLeadErrorMax
	{
		get
		{
			return GetStat(DrivenProperty.AiRangerLeadErrorMax);
		}
		set
		{
			SetStat(DrivenProperty.AiRangerLeadErrorMax, value);
		}
	}

	public float AiRangerVerticalErrorMultiplier
	{
		get
		{
			return GetStat(DrivenProperty.AiRangerVerticalErrorMultiplier);
		}
		set
		{
			SetStat(DrivenProperty.AiRangerVerticalErrorMultiplier, value);
		}
	}

	public float AiRangerHorizontalErrorMultiplier
	{
		get
		{
			return GetStat(DrivenProperty.AiRangerHorizontalErrorMultiplier);
		}
		set
		{
			SetStat(DrivenProperty.AiRangerHorizontalErrorMultiplier, value);
		}
	}

	public float AIAttackOnDecideChance
	{
		get
		{
			return GetStat(DrivenProperty.AIAttackOnDecideChance);
		}
		set
		{
			SetStat(DrivenProperty.AIAttackOnDecideChance, value);
		}
	}

	public float AiRaiseShieldDelayTimeBase
	{
		get
		{
			return GetStat(DrivenProperty.AiRaiseShieldDelayTimeBase);
		}
		set
		{
			SetStat(DrivenProperty.AiRaiseShieldDelayTimeBase, value);
		}
	}

	public float AiUseShieldAgainstEnemyMissileProbability
	{
		get
		{
			return GetStat(DrivenProperty.AiUseShieldAgainstEnemyMissileProbability);
		}
		set
		{
			SetStat(DrivenProperty.AiUseShieldAgainstEnemyMissileProbability, value);
		}
	}

	public int AiSpeciesIndex
	{
		get
		{
			return MathF.Round(GetStat(DrivenProperty.AiSpeciesIndex));
		}
		set
		{
			SetStat(DrivenProperty.AiSpeciesIndex, value);
		}
	}

	public float AiRandomizedDefendDirectionChance
	{
		get
		{
			return GetStat(DrivenProperty.AiRandomizedDefendDirectionChance);
		}
		set
		{
			SetStat(DrivenProperty.AiRandomizedDefendDirectionChance, value);
		}
	}

	public float AiShooterError
	{
		get
		{
			return GetStat(DrivenProperty.AiShooterError);
		}
		set
		{
			SetStat(DrivenProperty.AiShooterError, value);
		}
	}

	public float AiWeaponFavorMultiplierMelee
	{
		get
		{
			return GetStat(DrivenProperty.AiWeaponFavorMultiplierMelee);
		}
		set
		{
			SetStat(DrivenProperty.AiWeaponFavorMultiplierMelee, value);
		}
	}

	public float AiWeaponFavorMultiplierRanged
	{
		get
		{
			return GetStat(DrivenProperty.AiWeaponFavorMultiplierRanged);
		}
		set
		{
			SetStat(DrivenProperty.AiWeaponFavorMultiplierRanged, value);
		}
	}

	public float AiWeaponFavorMultiplierPolearm
	{
		get
		{
			return GetStat(DrivenProperty.AiWeaponFavorMultiplierPolearm);
		}
		set
		{
			SetStat(DrivenProperty.AiWeaponFavorMultiplierPolearm, value);
		}
	}

	public float AISetNoAttackTimerAfterBeingHitAbility
	{
		get
		{
			return GetStat(DrivenProperty.AISetNoAttackTimerAfterBeingHitAbility);
		}
		set
		{
			SetStat(DrivenProperty.AISetNoAttackTimerAfterBeingHitAbility, value);
		}
	}

	public float AISetNoAttackTimerAfterBeingParriedAbility
	{
		get
		{
			return GetStat(DrivenProperty.AISetNoAttackTimerAfterBeingParriedAbility);
		}
		set
		{
			SetStat(DrivenProperty.AISetNoAttackTimerAfterBeingParriedAbility, value);
		}
	}

	public float AISetNoDefendTimerAfterHittingAbility
	{
		get
		{
			return GetStat(DrivenProperty.AISetNoDefendTimerAfterHittingAbility);
		}
		set
		{
			SetStat(DrivenProperty.AISetNoDefendTimerAfterHittingAbility, value);
		}
	}

	public float AISetNoDefendTimerAfterParryingAbility
	{
		get
		{
			return GetStat(DrivenProperty.AISetNoDefendTimerAfterParryingAbility);
		}
		set
		{
			SetStat(DrivenProperty.AISetNoDefendTimerAfterParryingAbility, value);
		}
	}

	public float AIEstimateStunDurationPrecision
	{
		get
		{
			return GetStat(DrivenProperty.AIEstimateStunDurationPrecision);
		}
		set
		{
			SetStat(DrivenProperty.AIEstimateStunDurationPrecision, value);
		}
	}

	public float AIHoldingReadyMaxDuration
	{
		get
		{
			return GetStat(DrivenProperty.AIHoldingReadyMaxDuration);
		}
		set
		{
			SetStat(DrivenProperty.AIHoldingReadyMaxDuration, value);
		}
	}

	public float AIHoldingReadyVariationPercentage
	{
		get
		{
			return GetStat(DrivenProperty.AIHoldingReadyVariationPercentage);
		}
		set
		{
			SetStat(DrivenProperty.AIHoldingReadyVariationPercentage, value);
		}
	}

	public float OffhandWeaponDefendSpeedMultiplier
	{
		get
		{
			return GetStat(DrivenProperty.OffhandWeaponDefendSpeedMultiplier);
		}
		set
		{
			SetStat(DrivenProperty.OffhandWeaponDefendSpeedMultiplier, value);
		}
	}

	public AgentDrivenProperties()
	{
		_statValues = new float[98];
	}

	public float GetStat(DrivenProperty propertyEnum)
	{
		return _statValues[(int)propertyEnum];
	}

	public void SetStat(DrivenProperty propertyEnum, float value)
	{
		_statValues[(int)propertyEnum] = value;
	}

	internal float[] InitializeDrivenProperties(Agent agent, Equipment spawnEquipment, AgentBuildData agentBuildData)
	{
		MissionGameModels.Current.AgentStatCalculateModel.InitializeAgentStats(agent, spawnEquipment, this, agentBuildData);
		MissionGameModels.Current.AgentStatCalculateModel.UpdateAgentStats(agent, this);
		return _statValues;
	}

	internal float[] UpdateDrivenProperties(Agent agent)
	{
		MissionGameModels.Current.AgentStatCalculateModel.UpdateAgentStats(agent, this);
		return _statValues;
	}
}
