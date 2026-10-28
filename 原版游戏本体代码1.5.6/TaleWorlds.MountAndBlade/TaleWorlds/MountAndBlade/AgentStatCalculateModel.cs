using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class AgentStatCalculateModel : MBGameModel<AgentStatCalculateModel>
{
	protected const float MaxHorizontalErrorRadian = System.MathF.PI / 90f;

	private float _AILevelMultiplier = 1f;

	public abstract void InitializeAgentStats(Agent agent, Equipment spawnEquipment, AgentDrivenProperties agentDrivenProperties, AgentBuildData agentBuildData);

	public virtual void InitializeMissionEquipment(Agent agent)
	{
	}

	public virtual void InitializeAgentStatsAfterDeploymentFinished(Agent agent)
	{
	}

	public virtual void InitializeMissionEquipmentAfterDeploymentFinished(Agent agent)
	{
	}

	public abstract void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties);

	public abstract float GetDifficultyModifier();

	public abstract bool CanAgentRideMount(Agent agent, Agent targetMount);

	public virtual bool HasHeavyArmor(Agent agent)
	{
		return agent.GetBaseArmorEffectivenessForBodyPart(BoneBodyPartType.Chest) >= 24f;
	}

	public virtual float GetEffectiveArmorEncumbrance(Agent agent, Equipment equipment)
	{
		return equipment.GetTotalWeightOfArmor(agent.IsHuman);
	}

	public virtual float GetEffectiveMaxHealth(Agent agent)
	{
		return agent.BaseHealthLimit;
	}

	public virtual float GetEnvironmentSpeedFactor(Agent agent)
	{
		Scene scene = agent.Mission.Scene;
		float num = 1f;
		if (!scene.IsAtmosphereIndoor)
		{
			if (scene.GetRainDensity() > 0f)
			{
				num *= 0.9f;
			}
			if (!agent.IsHuman && !scene.IsDayTime)
			{
				num *= 0.9f;
			}
		}
		return num;
	}

	public float CalculateAIAttackOnDecideMaxValue()
	{
		if (GetDifficultyModifier() <= 0.5f)
		{
			return 0.16f;
		}
		return 0.48f;
	}

	public virtual float GetWeaponInaccuracy(Agent agent, WeaponComponentData weapon, int weaponSkill)
	{
		float a = 0f;
		if (weapon.IsRangedWeapon)
		{
			a = ((weapon.WeaponClass != WeaponClass.Sling) ? ((100f - (float)weapon.Accuracy) * (1f - 0.002f * (float)weaponSkill) * 0.001f) : ((100f - (float)weapon.Accuracy) * (1f - 0.003f * (float)weaponSkill) * 0.001f));
		}
		else if (weapon.WeaponFlags.HasAllFlags(WeaponFlags.WideGrip))
		{
			a = 1f - (float)weaponSkill * 0.01f;
		}
		return TaleWorlds.Library.MathF.Max(a, 0f);
	}

	public virtual float GetDetachmentCostMultiplierOfAgent(Agent agent, IDetachment detachment)
	{
		if (agent.Banner != null)
		{
			return 10f;
		}
		return 1f;
	}

	public virtual float GetInteractionDistance(Agent agent)
	{
		return 1.5f;
	}

	public virtual float GetMaxCameraZoom(Agent agent)
	{
		return 1f;
	}

	public virtual int GetEffectiveSkill(Agent agent, SkillObject skill)
	{
		return agent.Character.GetSkillValue(skill);
	}

	public virtual int GetEffectiveSkillForWeapon(Agent agent, WeaponComponentData weapon)
	{
		return GetEffectiveSkill(agent, weapon.RelevantSkill);
	}

	public abstract float GetWeaponDamageMultiplier(Agent agent, WeaponComponentData weapon);

	public abstract float GetEquipmentStealthBonus(Agent agent);

	public abstract float GetSneakAttackMultiplier(Agent agent, WeaponComponentData weapon);

	public abstract float GetKnockBackResistance(Agent agent);

	public abstract float GetKnockDownResistance(Agent agent, StrikeType strikeType = StrikeType.Invalid);

	public abstract float GetDismountResistance(Agent agent);

	public abstract float GetBreatheHoldMaxDuration(Agent agent, float baseBreatheHoldMaxDuration);

	public virtual string GetMissionDebugInfoForAgent(Agent agent)
	{
		return "Debug info not supported in this model";
	}

	public void ResetAILevelMultiplier()
	{
		_AILevelMultiplier = 1f;
	}

	public void SetAILevelMultiplier(float multiplier)
	{
		_AILevelMultiplier = multiplier;
	}

	protected int GetMeleeSkill(Agent agent, WeaponComponentData equippedItem, WeaponComponentData secondaryItem)
	{
		SkillObject skill = DefaultSkills.Athletics;
		if (equippedItem != null)
		{
			SkillObject relevantSkill = equippedItem.RelevantSkill;
			skill = ((relevantSkill == DefaultSkills.OneHanded || relevantSkill == DefaultSkills.Polearm) ? relevantSkill : ((relevantSkill != DefaultSkills.TwoHanded) ? DefaultSkills.OneHanded : ((secondaryItem == null) ? DefaultSkills.TwoHanded : DefaultSkills.OneHanded)));
		}
		return GetEffectiveSkill(agent, skill);
	}

	protected float CalculateAILevel(Agent agent, int relevantSkillLevel)
	{
		float difficultyModifier = GetDifficultyModifier();
		return MBMath.ClampFloat((float)relevantSkillLevel / 300f * ((difficultyModifier <= 0f) ? 0.1f : ((difficultyModifier <= 0.5f) ? 0.32f : 0.96f)), 0f, 1f);
	}

	protected void SetAiRelatedProperties(Agent agent, AgentDrivenProperties agentDrivenProperties, WeaponComponentData equippedItem, WeaponComponentData secondaryItem)
	{
		int meleeSkill = GetMeleeSkill(agent, equippedItem, secondaryItem);
		SkillObject skill = ((equippedItem == null) ? DefaultSkills.Athletics : equippedItem.RelevantSkill);
		int effectiveSkill = GetEffectiveSkill(agent, skill);
		agentDrivenProperties.AiShooterErrorWoRangeUpdate = 0f;
		float num = CalculateAILevel(agent, meleeSkill) * _AILevelMultiplier;
		float num2 = CalculateAILevel(agent, effectiveSkill) * _AILevelMultiplier;
		float num3 = num + agent.Defensiveness;
		float difficultyModifier = GetDifficultyModifier();
		agentDrivenProperties.AiRangedHorsebackMissileRange = 0.3f + 0.4f * num2;
		agentDrivenProperties.AiFacingMissileWatch = -0.96f + num * 0.06f;
		agentDrivenProperties.AiFlyingMissileCheckRadius = 8f - 6f * num;
		agentDrivenProperties.AiShootFreq = 0.3f + 0.7f * num2;
		agentDrivenProperties.AiWaitBeforeShootFactor = (agent.PropertyModifiers.resetAiWaitBeforeShootFactor ? 0f : (1f - 0.5f * num2));
		agentDrivenProperties.AIBlockOnDecideAbility = MBMath.Lerp(0.5f, 0.99f, MBMath.ClampFloat(TaleWorlds.Library.MathF.Pow(num, 0.5f), 0f, 1f));
		agentDrivenProperties.AIParryOnDecideAbility = MBMath.Lerp(0.5f, 0.95f, MBMath.ClampFloat(num, 0f, 1f));
		agentDrivenProperties.AiTryChamberAttackOnDecide = (num - 0.15f) * 0.1f;
		agentDrivenProperties.AIAttackOnParryChance = 0.08f - 0.02f * agent.Defensiveness;
		agentDrivenProperties.AiAttackOnParryTiming = -0.2f + 0.3f * num;
		agentDrivenProperties.AIDecideOnAttackChance = 0.5f * agent.Defensiveness;
		agentDrivenProperties.AIParryOnAttackAbility = MBMath.ClampFloat(num, 0f, 1f);
		agentDrivenProperties.AiKick = -0.1f + ((num > 0.4f) ? 0.4f : num);
		agentDrivenProperties.AiAttackCalculationMaxTimeFactor = num;
		agentDrivenProperties.AiDecideOnAttackWhenReceiveHitTiming = -0.25f * (1f - num);
		agentDrivenProperties.AiDecideOnAttackContinueAction = -0.5f * (1f - num);
		agentDrivenProperties.AiDecideOnAttackingContinue = 0.1f * num;
		agentDrivenProperties.AIParryOnAttackingContinueAbility = MBMath.Lerp(0.5f, 0.95f, MBMath.ClampFloat(num, 0f, 1f));
		agentDrivenProperties.AIDecideOnRealizeEnemyBlockingAttackAbility = MBMath.ClampFloat(TaleWorlds.Library.MathF.Pow(num, 2.5f) - 0.1f, 0f, 1f);
		agentDrivenProperties.AIRealizeBlockingFromIncorrectSideAbility = MBMath.ClampFloat(TaleWorlds.Library.MathF.Pow(num, 2.5f) - 0.01f, 0f, 1f);
		agentDrivenProperties.AiAttackingShieldDefenseChance = 0.2f + 0.3f * num;
		agentDrivenProperties.AiAttackingShieldDefenseTimer = -0.3f + 0.3f * num;
		agentDrivenProperties.AiRandomizedDefendDirectionChance = 1f - TaleWorlds.Library.MathF.Pow(num, 3f);
		agentDrivenProperties.AiShooterError = 0.008f;
		agentDrivenProperties.AISetNoAttackTimerAfterBeingHitAbility = MBMath.Lerp(0.33f, 1f, num);
		agentDrivenProperties.AISetNoAttackTimerAfterBeingParriedAbility = MBMath.Lerp(0.2f, 1f, num * num);
		agentDrivenProperties.AISetNoDefendTimerAfterHittingAbility = MBMath.Lerp(0.1f, 0.99f, num * num);
		agentDrivenProperties.AISetNoDefendTimerAfterParryingAbility = MBMath.Lerp(0.15f, 1f, num * num);
		agentDrivenProperties.AIEstimateStunDurationPrecision = 1f - MBMath.Lerp(0.2f, 1f, num);
		agentDrivenProperties.AIHoldingReadyMaxDuration = MBMath.Lerp(0.25f, 0f, TaleWorlds.Library.MathF.Min(1f, num * 2f));
		agentDrivenProperties.AIHoldingReadyVariationPercentage = num;
		agentDrivenProperties.AiRaiseShieldDelayTimeBase = -0.75f + 0.5f * num;
		agentDrivenProperties.AiUseShieldAgainstEnemyMissileProbability = 0.1f + num * 0.6f + num3 * 0.2f;
		agentDrivenProperties.AiCheckApplyMovementInterval = (2f - difficultyModifier) * (0.05f + 0.005f * (1.1f - num));
		agentDrivenProperties.AiCheckCalculateMovementInterval = ((agent.HasMount || agent.IsMount) ? 0.25f : ((2f - difficultyModifier) * 0.25f));
		agentDrivenProperties.AiCheckDecideSimpleBehaviorInterval = (2f - difficultyModifier) * (agent.GetAgentFlags().HasAnyFlag(AgentFlag.CanWieldWeapon) ? 1.5f : 0.2f);
		agentDrivenProperties.AiCheckDoSimpleBehaviorInterval = 2f - difficultyModifier;
		agentDrivenProperties.AiMovementDelayFactor = 4f / (3f + num2);
		agentDrivenProperties.AiParryDecisionChangeValue = 0.05f + 0.7f * num;
		agentDrivenProperties.AiDefendWithShieldDecisionChanceValue = TaleWorlds.Library.MathF.Min(2f, 0.5f + num + 0.6f * num3);
		agentDrivenProperties.AiMoveEnemySideTimeValue = -2.5f + 0.5f * num;
		agentDrivenProperties.AiMinimumDistanceToContinueFactor = 2f + 0.3f * (3f - num);
		agentDrivenProperties.AiChargeHorsebackTargetDistFactor = 1.5f * (3f - num);
		agentDrivenProperties.AiWaitBeforeShootFactor = (agent.PropertyModifiers.resetAiWaitBeforeShootFactor ? 0f : (1f - 0.5f * num2));
		float num4 = 1f - num2;
		agentDrivenProperties.AiRangerLeadErrorMin = (0f - num4) * 0.35f;
		agentDrivenProperties.AiRangerLeadErrorMax = num4 * 0.2f;
		agentDrivenProperties.AiRangerVerticalErrorMultiplier = num4 * 0.1f;
		agentDrivenProperties.AiRangerHorizontalErrorMultiplier = num4 * (System.MathF.PI / 90f);
		agentDrivenProperties.AIAttackOnDecideChance = TaleWorlds.Library.MathF.Clamp(0.1f * CalculateAIAttackOnDecideMaxValue() * (3f - agent.Defensiveness), 0.05f, 1f);
		agentDrivenProperties.SetStat(DrivenProperty.UseRealisticBlocking, (agent.Controller != AgentControllerType.Player) ? 1f : 0f);
		agentDrivenProperties.AiWeaponFavorMultiplierMelee = 1f;
		agentDrivenProperties.AiWeaponFavorMultiplierRanged = 1f;
		agentDrivenProperties.AiWeaponFavorMultiplierPolearm = 1f;
	}

	protected void SetAllWeaponInaccuracy(Agent agent, AgentDrivenProperties agentDrivenProperties, int equippedIndex, WeaponComponentData equippedWeaponComponent)
	{
		if (equippedWeaponComponent != null)
		{
			agentDrivenProperties.WeaponInaccuracy = GetWeaponInaccuracy(agent, equippedWeaponComponent, GetEffectiveSkillForWeapon(agent, equippedWeaponComponent));
		}
		else
		{
			agentDrivenProperties.WeaponInaccuracy = 0f;
		}
	}
}
