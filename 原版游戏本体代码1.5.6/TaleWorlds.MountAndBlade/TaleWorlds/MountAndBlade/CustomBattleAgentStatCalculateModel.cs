using System;
using MBHelpers;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class CustomBattleAgentStatCalculateModel : AgentStatCalculateModel
{
	public override float GetDifficultyModifier()
	{
		return 1f;
	}

	public override bool CanAgentRideMount(Agent agent, Agent targetMount)
	{
		return agent.CheckSkillForMounting(targetMount);
	}

	public override void InitializeAgentStats(Agent agent, Equipment spawnEquipment, AgentDrivenProperties agentDrivenProperties, AgentBuildData agentBuildData)
	{
		agentDrivenProperties.ArmorEncumbrance = spawnEquipment.GetTotalWeightOfArmor(agent.IsHuman);
		if (agent.IsHuman)
		{
			agentDrivenProperties.ArmorHead = spawnEquipment.GetHeadArmorSum();
			agentDrivenProperties.ArmorTorso = spawnEquipment.GetHumanBodyArmorSum();
			agentDrivenProperties.ArmorLegs = spawnEquipment.GetLegArmorSum();
			agentDrivenProperties.ArmorArms = spawnEquipment.GetArmArmorSum();
		}
		else
		{
			agentDrivenProperties.AiSpeciesIndex = (int)spawnEquipment[EquipmentIndex.ArmorItemEndSlot].Item.Id.InternalValue;
			agentDrivenProperties.AttributeRiding = 0.8f + ((spawnEquipment[EquipmentIndex.HorseHarness].Item != null) ? 0.2f : 0f);
			float num = 0f;
			for (int i = 1; i < 12; i++)
			{
				if (spawnEquipment[i].Item != null)
				{
					num += (float)spawnEquipment[i].GetModifiedMountBodyArmor();
				}
			}
			agentDrivenProperties.ArmorTorso = num;
			ItemObject item = spawnEquipment[EquipmentIndex.ArmorItemEndSlot].Item;
			if (item != null)
			{
				_ = item.HorseComponent;
				EquipmentElement equipmentElement = spawnEquipment[EquipmentIndex.ArmorItemEndSlot];
				agentDrivenProperties.MountChargeDamage = (float)equipmentElement.GetModifiedMountCharge(spawnEquipment[EquipmentIndex.HorseHarness]) * 0.01f;
				agentDrivenProperties.MountDifficulty = equipmentElement.Item.Difficulty;
			}
		}
		agentDrivenProperties.OffhandWeaponDefendSpeedMultiplier = 1f;
	}

	public override void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
	{
		if (agent.IsHuman)
		{
			UpdateHumanStats(agent, agentDrivenProperties);
		}
		else
		{
			UpdateHorseStats(agent, agentDrivenProperties);
		}
	}

	public override float GetWeaponDamageMultiplier(Agent agent, WeaponComponentData weapon)
	{
		float num = 1f;
		SkillObject skillObject = weapon?.RelevantSkill;
		if (skillObject != null)
		{
			int effectiveSkill = MissionGameModels.Current.AgentStatCalculateModel.GetEffectiveSkill(agent, skillObject);
			if (skillObject == DefaultSkills.OneHanded)
			{
				num += (float)effectiveSkill * 0.0015f;
			}
			else if (skillObject == DefaultSkills.TwoHanded)
			{
				num += (float)effectiveSkill * 0.0016f;
			}
			else if (skillObject == DefaultSkills.Polearm)
			{
				num += (float)effectiveSkill * 0.0007f;
			}
			else if (skillObject == DefaultSkills.Bow)
			{
				num += (float)effectiveSkill * 0.0011f;
			}
			else if (skillObject == DefaultSkills.Throwing)
			{
				num += (float)effectiveSkill * 0.0006f;
			}
		}
		return Math.Max(0f, num);
	}

	public override float GetEquipmentStealthBonus(Agent agent)
	{
		return 0f;
	}

	public override float GetSneakAttackMultiplier(Agent agent, WeaponComponentData weapon)
	{
		BasicCharacterObject character = agent.Character;
		float num = 1f;
		if (weapon != null && character != null)
		{
			int skillValue = character.GetSkillValue(DefaultSkills.Roguery);
			num += 0.5f + (float)skillValue * 0.002f;
			if (weapon.WeaponClass == WeaponClass.Dagger)
			{
				num *= 3f;
			}
			else if (weapon.WeaponClass == WeaponClass.ThrowingKnife)
			{
				num *= 2f;
			}
		}
		return Math.Max(0f, num);
	}

	public override float GetKnockBackResistance(Agent agent)
	{
		if (agent.IsHuman)
		{
			int effectiveSkill = GetEffectiveSkill(agent, DefaultSkills.Athletics);
			float val = 0.15f + (float)effectiveSkill * 0.001f;
			return Math.Max(0f, val);
		}
		return float.MaxValue;
	}

	public override float GetKnockDownResistance(Agent agent, StrikeType strikeType = StrikeType.Invalid)
	{
		if (agent.IsHuman)
		{
			int effectiveSkill = GetEffectiveSkill(agent, DefaultSkills.Athletics);
			float num = 0.4f + (float)effectiveSkill * 0.001f;
			if (agent.HasMount)
			{
				num += 0.1f;
			}
			else if (strikeType == StrikeType.Thrust)
			{
				num += 0.15f;
			}
			return Math.Max(0f, num);
		}
		return float.MaxValue;
	}

	public override float GetDismountResistance(Agent agent)
	{
		if (agent.IsHuman)
		{
			int effectiveSkill = GetEffectiveSkill(agent, DefaultSkills.Riding);
			float val = 0.4f + (float)effectiveSkill * 0.001f;
			return Math.Max(0f, val);
		}
		return float.MaxValue;
	}

	public override float GetWeaponInaccuracy(Agent agent, WeaponComponentData weapon, int weaponSkill)
	{
		if (weapon == null || !weapon.IsRangedWeapon || weapon.RelevantSkill != DefaultSkills.Throwing || weapon.WeaponClass != WeaponClass.Sling)
		{
			return base.GetWeaponInaccuracy(agent, weapon, weaponSkill);
		}
		float num = TaleWorlds.Library.MathF.Max(0.2f, 1f - 0.007f * (float)weaponSkill);
		return (100f - (float)weapon.Accuracy) * 0.001f * num;
	}

	public override float GetBreatheHoldMaxDuration(Agent agent, float baseBreatheHoldMaxDuration)
	{
		return baseBreatheHoldMaxDuration;
	}

	private int GetSkillValueForItem(Agent agent, ItemObject primaryItem)
	{
		return GetEffectiveSkill(agent, (primaryItem != null) ? primaryItem.RelevantSkill : DefaultSkills.Athletics);
	}

	private void UpdateHumanStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
	{
		BasicCharacterObject character = agent.Character;
		MissionEquipment equipment = agent.Equipment;
		float num = equipment.GetTotalWeightOfWeapons();
		int weight = agent.Monster.Weight;
		float num2 = agentDrivenProperties.ArmorEncumbrance + num;
		EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
		EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
		if (primaryWieldedItemIndex != EquipmentIndex.None)
		{
			ItemObject item = equipment[primaryWieldedItemIndex].Item;
			WeaponComponent weaponComponent = item.WeaponComponent;
			if (weaponComponent != null)
			{
				float realWeaponLength = weaponComponent.PrimaryWeapon.GetRealWeaponLength();
				num += 1.5f * item.Weight * TaleWorlds.Library.MathF.Sqrt(realWeaponLength);
			}
		}
		if (offhandWieldedItemIndex != EquipmentIndex.None)
		{
			ItemObject item2 = equipment[offhandWieldedItemIndex].Item;
			num += 1.5f * item2.Weight;
		}
		agentDrivenProperties.WeaponExternalAccelerationAccuracyPenalty = 0f;
		agentDrivenProperties.WeaponsEncumbrance = num;
		WeaponComponentData weaponComponentData = ((primaryWieldedItemIndex != EquipmentIndex.None) ? equipment[primaryWieldedItemIndex].CurrentUsageItem : null);
		ItemObject primaryItem = ((primaryWieldedItemIndex != EquipmentIndex.None) ? equipment[primaryWieldedItemIndex].Item : null);
		WeaponComponentData secondaryItem = ((offhandWieldedItemIndex != EquipmentIndex.None) ? equipment[offhandWieldedItemIndex].CurrentUsageItem : null);
		agentDrivenProperties.SwingSpeedMultiplier = 0.93f + 0.0007f * (float)GetSkillValueForItem(agent, primaryItem);
		agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier = agentDrivenProperties.SwingSpeedMultiplier;
		agentDrivenProperties.HandlingMultiplier = 1f;
		agentDrivenProperties.ShieldBashStunDurationMultiplier = 1f;
		agentDrivenProperties.KickStunDurationMultiplier = 1f;
		agentDrivenProperties.ReloadSpeed = 0.93f + 0.0007f * (float)GetSkillValueForItem(agent, primaryItem);
		agentDrivenProperties.MissileSpeedMultiplier = 1f;
		agentDrivenProperties.ReloadMovementPenaltyFactor = 1f;
		agentDrivenProperties.DamageMultiplierBonus = 0f;
		SetAllWeaponInaccuracy(agent, agentDrivenProperties, (int)primaryWieldedItemIndex, weaponComponentData);
		int effectiveSkill = GetEffectiveSkill(agent, DefaultSkills.Athletics);
		int effectiveSkill2 = GetEffectiveSkill(agent, DefaultSkills.Riding);
		if (weaponComponentData != null)
		{
			WeaponComponentData weaponComponentData2 = weaponComponentData;
			int effectiveSkillForWeapon = GetEffectiveSkillForWeapon(agent, weaponComponentData2);
			if (weaponComponentData2.IsRangedWeapon)
			{
				int thrustSpeed = weaponComponentData2.ThrustSpeed;
				if (!agent.HasMount)
				{
					float num3 = TaleWorlds.Library.MathF.Max(0f, 1f - (float)effectiveSkillForWeapon / 500f);
					agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = 0.125f * num3;
					agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = 0.1f * num3;
				}
				else
				{
					float num4 = TaleWorlds.Library.MathF.Max(0f, (1f - (float)effectiveSkillForWeapon / 500f) * (1f - (float)effectiveSkill2 / 1800f));
					agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = 0.025f * num4;
					agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = 0.12f * num4;
				}
				agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = TaleWorlds.Library.MathF.Max(0f, agentDrivenProperties.WeaponMaxMovementAccuracyPenalty);
				agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = TaleWorlds.Library.MathF.Max(0f, agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty);
				if (weaponComponentData2.RelevantSkill == DefaultSkills.Bow)
				{
					float value = ((float)thrustSpeed - 45f) / 90f;
					value = MBMath.ClampFloat(value, 0f, 1f);
					agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= 6f;
					agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= 4.5f / MBMath.Lerp(0.75f, 2f, value);
				}
				else if (weaponComponentData2.RelevantSkill == DefaultSkills.Throwing)
				{
					if (weaponComponentData2.WeaponClass == WeaponClass.Sling)
					{
						float value2 = ((float)thrustSpeed - 30f) / 90f;
						value2 = MBMath.ClampFloat(value2, 0f, 1f);
						agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= 5f;
						agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= 2.4f * MBMath.Lerp(2.4f, 1.2f, value2);
					}
					else
					{
						float value3 = ((float)thrustSpeed - 91f) / 14f;
						value3 = MBMath.ClampFloat(value3, 0f, 1f);
						agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= 0.5f;
						agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= 1.5f * MBMath.Lerp(1.5f, 0.8f, value3);
					}
				}
				else if (weaponComponentData2.RelevantSkill == DefaultSkills.Crossbow)
				{
					agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= 2.5f;
					agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= 1.2f;
				}
				switch (weaponComponentData2.WeaponClass)
				{
				case WeaponClass.Bow:
				{
					agentDrivenProperties.WeaponBestAccuracyWaitTime = 0.3f + (95.75f - (float)thrustSpeed) * 0.005f;
					float value4 = ((float)thrustSpeed - 45f) / 90f;
					value4 = MBMath.ClampFloat(value4, 0f, 1f);
					agentDrivenProperties.WeaponUnsteadyBeginTime = 0.6f + (float)effectiveSkillForWeapon * 0.01f * MBMath.Lerp(2f, 4f, value4);
					if (agent.IsAIControlled)
					{
						agentDrivenProperties.WeaponUnsteadyBeginTime *= 4f;
					}
					agentDrivenProperties.WeaponUnsteadyEndTime = 2f + agentDrivenProperties.WeaponUnsteadyBeginTime;
					agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.1f;
					break;
				}
				case WeaponClass.ThrowingAxe:
				case WeaponClass.ThrowingKnife:
				case WeaponClass.Javelin:
					agentDrivenProperties.WeaponBestAccuracyWaitTime = 0.2f + (89f - (float)thrustSpeed) * 0.009f;
					agentDrivenProperties.WeaponUnsteadyBeginTime = 2.5f + (float)effectiveSkillForWeapon * 0.01f;
					agentDrivenProperties.WeaponUnsteadyEndTime = 10f + agentDrivenProperties.WeaponUnsteadyBeginTime;
					agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.025f;
					break;
				case WeaponClass.Sling:
					agentDrivenProperties.WeaponBestAccuracyWaitTime = 2.6f + (89f - (float)thrustSpeed) * 0.12f;
					agentDrivenProperties.WeaponUnsteadyBeginTime = 3f + (float)effectiveSkillForWeapon * 0.064f;
					agentDrivenProperties.WeaponUnsteadyEndTime = 22f + agentDrivenProperties.WeaponUnsteadyBeginTime;
					agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.2f;
					break;
				default:
					agentDrivenProperties.WeaponBestAccuracyWaitTime = 0.1f;
					agentDrivenProperties.WeaponUnsteadyBeginTime = 0f;
					agentDrivenProperties.WeaponUnsteadyEndTime = 0f;
					agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.1f;
					break;
				}
			}
			else if (weaponComponentData2.WeaponFlags.HasAllFlags(WeaponFlags.WideGrip))
			{
				agentDrivenProperties.WeaponUnsteadyBeginTime = 1f + (float)effectiveSkillForWeapon * 0.005f;
				agentDrivenProperties.WeaponUnsteadyEndTime = 3f + (float)effectiveSkillForWeapon * 0.01f;
			}
		}
		agentDrivenProperties.TopSpeedReachDuration = 2f / TaleWorlds.Library.MathF.Max((200f + (float)effectiveSkill) / 300f * ((float)weight / ((float)weight + num2)), 0.3f);
		float num5 = 0.7f + 0.00070000015f * (float)effectiveSkill;
		float num6 = TaleWorlds.Library.MathF.Max(0.2f * (1f - (float)effectiveSkill * 0.001f), 0f) * num2 / (float)weight;
		float num7 = MBMath.ClampFloat(num5 - num6, 0f, 0.91f);
		agentDrivenProperties.MaxSpeedMultiplier = GetEnvironmentSpeedFactor(agent) * num7;
		float managedParameter = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalCombatSpeedMinMultiplier);
		float managedParameter2 = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalCombatSpeedMaxMultiplier);
		float amount = TaleWorlds.Library.MathF.Min(num2 / (float)weight, 1f);
		agentDrivenProperties.CombatMaxSpeedMultiplier = TaleWorlds.Library.MathF.Min(MBMath.Lerp(managedParameter2, managedParameter, amount), 1f);
		int effectiveSkill3 = GetEffectiveSkill(agent, DefaultSkills.Roguery);
		agentDrivenProperties.CrouchedSpeedMultiplier = 1f + (float)effectiveSkill3 * 0.001f;
		agentDrivenProperties.AttributeShieldMissileCollisionBodySizeAdder = 0.3f;
		float num8 = agent.MountAgent?.GetAgentDrivenPropertyValue(DrivenProperty.AttributeRiding) ?? 1f;
		agentDrivenProperties.AttributeRiding = (float)effectiveSkill2 * num8;
		agentDrivenProperties.AttributeHorseArchery = MissionGameModels.Current.StrikeMagnitudeModel.CalculateHorseArcheryFactor(character);
		agentDrivenProperties.BipedalRangedReadySpeedMultiplier = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRangedReadySpeedMultiplier);
		agentDrivenProperties.BipedalRangedReloadSpeedMultiplier = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRangedReloadSpeedMultiplier);
		if (agent.Character != null)
		{
			if (agent.HasMount && weaponComponentData != null)
			{
				SetMountedWeaponPenaltiesOnAgent(agent, agentDrivenProperties, weaponComponentData);
			}
			SetBannerEffectsOnAgent(agent, agentDrivenProperties, weaponComponentData);
		}
		SetAiRelatedProperties(agent, agentDrivenProperties, weaponComponentData, secondaryItem);
		float num9 = 1f;
		if (!agent.Mission.Scene.IsAtmosphereIndoor)
		{
			float rainDensity = agent.Mission.Scene.GetRainDensity();
			float fog = agent.Mission.Scene.GetFog();
			if (rainDensity > 0f || fog > 0f)
			{
				num9 += TaleWorlds.Library.MathF.Min(0.3f, rainDensity + fog);
			}
			if (!agent.Mission.Scene.IsDayTime)
			{
				num9 += 0.1f;
			}
		}
		agentDrivenProperties.AiShooterError *= num9;
	}

	private void UpdateHorseStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
	{
		Equipment spawnEquipment = agent.SpawnEquipment;
		EquipmentElement mountElement = spawnEquipment[EquipmentIndex.ArmorItemEndSlot];
		EquipmentElement harness = spawnEquipment[EquipmentIndex.HorseHarness];
		_ = mountElement.Item;
		float num = mountElement.GetModifiedMountSpeed(in harness) + 1;
		int modifiedMountManeuver = mountElement.GetModifiedMountManeuver(in harness);
		int num2 = 0;
		float environmentSpeedFactor = GetEnvironmentSpeedFactor(agent);
		if (agent.RiderAgent != null)
		{
			num2 = GetEffectiveSkill(agent.RiderAgent, DefaultSkills.Riding);
			FactoredNumber bonuses = new FactoredNumber(num);
			FactoredNumber factoredNumber = new FactoredNumber(modifiedMountManeuver);
			bonuses.AddFactor((float)num2 * 0.001f);
			factoredNumber.AddFactor((float)num2 * 0.0004f);
			Formation formation = agent.RiderAgent.Formation;
			BannerComponent activeBanner = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(formation);
			if (activeBanner != null)
			{
				BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.IncreasedMountMovementSpeed, activeBanner, ref bonuses);
			}
			agentDrivenProperties.MountManeuver = factoredNumber.ResultNumber;
			agentDrivenProperties.MountSpeed = environmentSpeedFactor * 0.22f * (1f + bonuses.ResultNumber);
		}
		else
		{
			agentDrivenProperties.MountManeuver = modifiedMountManeuver;
			agentDrivenProperties.MountSpeed = environmentSpeedFactor * 0.22f * (1f + num);
		}
		float num3 = mountElement.Weight / 2f + (harness.IsEmpty ? 0f : harness.Weight);
		agentDrivenProperties.MountDashAccelerationMultiplier = ((!(num3 > 200f)) ? 1f : ((num3 < 300f) ? (1f - (num3 - 200f) / 111f) : 0.1f));
		agentDrivenProperties.TopSpeedReachDuration = Game.Current.BasicModels.RidingModel.CalculateAcceleration(in mountElement, in harness, num2);
	}

	private void SetBannerEffectsOnAgent(Agent agent, AgentDrivenProperties agentDrivenProperties, WeaponComponentData equippedWeaponComponent)
	{
		BannerComponent activeBanner = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(agent.Formation);
		if (activeBanner != null)
		{
			bool num = equippedWeaponComponent?.IsRangedWeapon ?? false;
			FactoredNumber bonuses = new FactoredNumber(agentDrivenProperties.MaxSpeedMultiplier);
			FactoredNumber bonuses2 = new FactoredNumber(agentDrivenProperties.WeaponInaccuracy);
			if (num && equippedWeaponComponent != null)
			{
				BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.DecreasedRangedAccuracyPenalty, activeBanner, ref bonuses2);
			}
			BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.IncreasedTroopMovementSpeed, activeBanner, ref bonuses);
			agentDrivenProperties.MaxSpeedMultiplier = bonuses.ResultNumber;
			agentDrivenProperties.WeaponInaccuracy = bonuses2.ResultNumber;
		}
	}

	private void SetMountedWeaponPenaltiesOnAgent(Agent agent, AgentDrivenProperties agentDrivenProperties, WeaponComponentData equippedWeaponComponent)
	{
		int effectiveSkill = GetEffectiveSkill(agent, DefaultSkills.Riding);
		float num = 0.3f - (float)effectiveSkill * 0.003f;
		if (num > 0f)
		{
			float val = agentDrivenProperties.SwingSpeedMultiplier * (1f - num);
			float val2 = agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier * (1f - num);
			float val3 = agentDrivenProperties.ReloadSpeed * (1f - num);
			float val4 = agentDrivenProperties.WeaponBestAccuracyWaitTime * (1f + num);
			agentDrivenProperties.SwingSpeedMultiplier = Math.Max(0f, val);
			agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier = Math.Max(0f, val2);
			agentDrivenProperties.ReloadSpeed = Math.Max(0f, val3);
			agentDrivenProperties.WeaponBestAccuracyWaitTime = Math.Max(0f, val4);
		}
		float num2 = 15f - (float)effectiveSkill * 0.15f;
		if (num2 > 0f)
		{
			float val5 = agentDrivenProperties.WeaponInaccuracy * (1f + num2);
			agentDrivenProperties.WeaponInaccuracy = Math.Max(0f, val5);
		}
	}
}
