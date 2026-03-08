using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000007 RID: 7
	public enum DrivenProperty
	{
		// Token: 0x0400004B RID: 75
		None = -1,
		// Token: 0x0400004C RID: 76
		AiRangedHorsebackMissileRange,
		// Token: 0x0400004D RID: 77
		AiFacingMissileWatch,
		// Token: 0x0400004E RID: 78
		AiFlyingMissileCheckRadius,
		// Token: 0x0400004F RID: 79
		AiShootFreq,
		// Token: 0x04000050 RID: 80
		AiWaitBeforeShootFactor,
		// Token: 0x04000051 RID: 81
		AIBlockOnDecideAbility,
		// Token: 0x04000052 RID: 82
		AIParryOnDecideAbility,
		// Token: 0x04000053 RID: 83
		AiTryChamberAttackOnDecide,
		// Token: 0x04000054 RID: 84
		AIAttackOnParryChance,
		// Token: 0x04000055 RID: 85
		AiAttackOnParryTiming,
		// Token: 0x04000056 RID: 86
		AIDecideOnAttackChance,
		// Token: 0x04000057 RID: 87
		AIParryOnAttackAbility,
		// Token: 0x04000058 RID: 88
		AiKick,
		// Token: 0x04000059 RID: 89
		AiAttackCalculationMaxTimeFactor,
		// Token: 0x0400005A RID: 90
		AiDecideOnAttackWhenReceiveHitTiming,
		// Token: 0x0400005B RID: 91
		AiDecideOnAttackContinueAction,
		// Token: 0x0400005C RID: 92
		AiDecideOnAttackingContinue,
		// Token: 0x0400005D RID: 93
		AIParryOnAttackingContinueAbility,
		// Token: 0x0400005E RID: 94
		AIDecideOnRealizeEnemyBlockingAttackAbility,
		// Token: 0x0400005F RID: 95
		AIRealizeBlockingFromIncorrectSideAbility,
		// Token: 0x04000060 RID: 96
		AiAttackingShieldDefenseChance,
		// Token: 0x04000061 RID: 97
		AiAttackingShieldDefenseTimer,
		// Token: 0x04000062 RID: 98
		AiCheckApplyMovementInterval,
		// Token: 0x04000063 RID: 99
		AiCheckCalculateMovementInterval,
		// Token: 0x04000064 RID: 100
		AiCheckDecideSimpleBehaviorInterval,
		// Token: 0x04000065 RID: 101
		AiCheckDoSimpleBehaviorInterval,
		// Token: 0x04000066 RID: 102
		AiMovementDelayFactor,
		// Token: 0x04000067 RID: 103
		AiParryDecisionChangeValue,
		// Token: 0x04000068 RID: 104
		AiDefendWithShieldDecisionChanceValue,
		// Token: 0x04000069 RID: 105
		AiMoveEnemySideTimeValue,
		// Token: 0x0400006A RID: 106
		AiMinimumDistanceToContinueFactor,
		// Token: 0x0400006B RID: 107
		AiChargeHorsebackTargetDistFactor,
		// Token: 0x0400006C RID: 108
		AiRangerLeadErrorMin,
		// Token: 0x0400006D RID: 109
		AiRangerLeadErrorMax,
		// Token: 0x0400006E RID: 110
		AiRangerVerticalErrorMultiplier,
		// Token: 0x0400006F RID: 111
		AiRangerHorizontalErrorMultiplier,
		// Token: 0x04000070 RID: 112
		AIAttackOnDecideChance,
		// Token: 0x04000071 RID: 113
		AiRaiseShieldDelayTimeBase,
		// Token: 0x04000072 RID: 114
		AiUseShieldAgainstEnemyMissileProbability,
		// Token: 0x04000073 RID: 115
		AiSpeciesIndex,
		// Token: 0x04000074 RID: 116
		AiRandomizedDefendDirectionChance,
		// Token: 0x04000075 RID: 117
		AiShooterError,
		// Token: 0x04000076 RID: 118
		AiWeaponFavorMultiplierMelee,
		// Token: 0x04000077 RID: 119
		AiWeaponFavorMultiplierRanged,
		// Token: 0x04000078 RID: 120
		AiWeaponFavorMultiplierPolearm,
		// Token: 0x04000079 RID: 121
		AISetNoAttackTimerAfterBeingHitAbility,
		// Token: 0x0400007A RID: 122
		AISetNoAttackTimerAfterBeingParriedAbility,
		// Token: 0x0400007B RID: 123
		AISetNoDefendTimerAfterHittingAbility,
		// Token: 0x0400007C RID: 124
		AISetNoDefendTimerAfterParryingAbility,
		// Token: 0x0400007D RID: 125
		AIEstimateStunDurationPrecision,
		// Token: 0x0400007E RID: 126
		AIHoldingReadyMaxDuration,
		// Token: 0x0400007F RID: 127
		AIHoldingReadyVariationPercentage,
		// Token: 0x04000080 RID: 128
		MountChargeDamage,
		// Token: 0x04000081 RID: 129
		MountDifficulty,
		// Token: 0x04000082 RID: 130
		ArmorEncumbrance,
		// Token: 0x04000083 RID: 131
		ArmorHead,
		// Token: 0x04000084 RID: 132
		ArmorTorso,
		// Token: 0x04000085 RID: 133
		ArmorLegs,
		// Token: 0x04000086 RID: 134
		ArmorArms,
		// Token: 0x04000087 RID: 135
		UseRealisticBlocking,
		// Token: 0x04000088 RID: 136
		ThrowingWeaponDamageMultiplierBonus,
		// Token: 0x04000089 RID: 137
		MeleeWeaponDamageMultiplierBonus,
		// Token: 0x0400008A RID: 138
		ArmorPenetrationMultiplierCrossbow,
		// Token: 0x0400008B RID: 139
		ArmorPenetrationMultiplierBow,
		// Token: 0x0400008C RID: 140
		WeaponsEncumbrance,
		// Token: 0x0400008D RID: 141
		DamageMultiplierBonus,
		// Token: 0x0400008E RID: 142
		SwingSpeedMultiplier,
		// Token: 0x0400008F RID: 143
		ThrustOrRangedReadySpeedMultiplier,
		// Token: 0x04000090 RID: 144
		HandlingMultiplier,
		// Token: 0x04000091 RID: 145
		ReloadSpeed,
		// Token: 0x04000092 RID: 146
		MissileSpeedMultiplier,
		// Token: 0x04000093 RID: 147
		WeaponInaccuracy,
		// Token: 0x04000094 RID: 148
		AiShooterErrorWoRangeUpdate,
		// Token: 0x04000095 RID: 149
		WeaponWorstMobileAccuracyPenalty,
		// Token: 0x04000096 RID: 150
		WeaponWorstUnsteadyAccuracyPenalty,
		// Token: 0x04000097 RID: 151
		WeaponBestAccuracyWaitTime,
		// Token: 0x04000098 RID: 152
		WeaponUnsteadyBeginTime,
		// Token: 0x04000099 RID: 153
		WeaponUnsteadyEndTime,
		// Token: 0x0400009A RID: 154
		WeaponRotationalAccuracyPenaltyInRadians,
		// Token: 0x0400009B RID: 155
		AttributeRiding,
		// Token: 0x0400009C RID: 156
		AttributeShield,
		// Token: 0x0400009D RID: 157
		AttributeShieldMissileCollisionBodySizeAdder,
		// Token: 0x0400009E RID: 158
		ShieldBashStunDurationMultiplier,
		// Token: 0x0400009F RID: 159
		KickStunDurationMultiplier,
		// Token: 0x040000A0 RID: 160
		ReloadMovementPenaltyFactor,
		// Token: 0x040000A1 RID: 161
		TopSpeedReachDuration,
		// Token: 0x040000A2 RID: 162
		MaxSpeedMultiplier,
		// Token: 0x040000A3 RID: 163
		CombatMaxSpeedMultiplier,
		// Token: 0x040000A4 RID: 164
		CrouchedSpeedMultiplier,
		// Token: 0x040000A5 RID: 165
		AttributeHorseArchery,
		// Token: 0x040000A6 RID: 166
		AttributeCourage,
		// Token: 0x040000A7 RID: 167
		MountManeuver,
		// Token: 0x040000A8 RID: 168
		MountSpeed,
		// Token: 0x040000A9 RID: 169
		MountDashAccelerationMultiplier,
		// Token: 0x040000AA RID: 170
		BipedalRangedReadySpeedMultiplier,
		// Token: 0x040000AB RID: 171
		BipedalRangedReloadSpeedMultiplier,
		// Token: 0x040000AC RID: 172
		OffhandWeaponDefendSpeedMultiplier,
		// Token: 0x040000AD RID: 173
		Count,
		// Token: 0x040000AE RID: 174
		DrivenPropertiesCalculatedAtSpawnEnd = 64
	}
}
