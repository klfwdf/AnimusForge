using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerAgentStatCalculateModel : AgentStatCalculateModel
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
		if (!agent.IsHuman)
		{
			InitializeHorseAgentStats(agent, spawnEquipment, agentDrivenProperties);
		}
		else
		{
			agentDrivenProperties = InitializeHumanAgentStats(agent, agentDrivenProperties, agentBuildData);
		}
		agentDrivenProperties.OffhandWeaponDefendSpeedMultiplier = 1f;
	}

	private AgentDrivenProperties InitializeHumanAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties, AgentBuildData agentBuildData)
	{
		MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character);
		if (mPHeroClassForCharacter != null)
		{
			FillAgentStatsFromData(ref agentDrivenProperties, agent, mPHeroClassForCharacter, agentBuildData?.AgentMissionPeer, agentBuildData?.OwningAgentMissionPeer);
			agentDrivenProperties.SetStat(DrivenProperty.UseRealisticBlocking, MultiplayerOptions.OptionType.UseRealisticBlocking.GetBoolValue() ? 1f : 0f);
		}
		if (mPHeroClassForCharacter != null)
		{
			agent.BaseHealthLimit = mPHeroClassForCharacter.Health;
		}
		else
		{
			agent.BaseHealthLimit = 100f;
		}
		agent.HealthLimit = agent.BaseHealthLimit;
		agent.Health = agent.HealthLimit;
		return agentDrivenProperties;
	}

	private static void InitializeHorseAgentStats(Agent agent, Equipment spawnEquipment, AgentDrivenProperties agentDrivenProperties)
	{
		agentDrivenProperties.AiSpeciesIndex = agent.Monster.FamilyType;
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
		_ = spawnEquipment[EquipmentIndex.ArmorItemEndSlot].Item.HorseComponent;
		EquipmentElement equipmentElement = spawnEquipment[EquipmentIndex.ArmorItemEndSlot];
		agentDrivenProperties.MountChargeDamage = (float)equipmentElement.GetModifiedMountCharge(spawnEquipment[EquipmentIndex.HorseHarness]) * 0.01f;
		agentDrivenProperties.MountDifficulty = equipmentElement.Item.Difficulty;
	}

	public override float GetWeaponDamageMultiplier(Agent agent, WeaponComponentData weapon)
	{
		return 1f;
	}

	public override float GetEquipmentStealthBonus(Agent agent)
	{
		return 0f;
	}

	public override float GetSneakAttackMultiplier(Agent agent, WeaponComponentData weapon)
	{
		return 1f;
	}

	public override float GetKnockBackResistance(Agent agent)
	{
		return agent.Character.KnockbackResistance;
	}

	public override float GetKnockDownResistance(Agent agent, StrikeType strikeType = StrikeType.Invalid)
	{
		float num = agent.Character.KnockdownResistance;
		if (agent.HasMount)
		{
			num += 0.1f;
		}
		else if (strikeType == StrikeType.Thrust)
		{
			num += 0.25f;
		}
		return num;
	}

	public override float GetDismountResistance(Agent agent)
	{
		return agent.Character.DismountResistance;
	}

	public override float GetWeaponInaccuracy(Agent agent, WeaponComponentData weapon, int weaponSkill)
	{
		float b = 0f;
		if (weapon != null && weapon.IsRangedWeapon)
		{
			float num = 1f;
			if (weapon.RelevantSkill == DefaultSkills.Throwing && weapon.WeaponClass == WeaponClass.Sling)
			{
				num = TaleWorlds.Library.MathF.Max(1f - 0.007f * (float)weaponSkill, 0.2f);
			}
			else
			{
				num = 1f - 0.002f * (float)weaponSkill;
				if (weapon.WeaponClass == WeaponClass.ThrowingAxe)
				{
					num *= 2f;
				}
			}
			b = (100f - (float)weapon.Accuracy) * num * 0.001f;
		}
		else if (weapon != null && weapon.WeaponFlags.HasAllFlags(WeaponFlags.WideGrip))
		{
			b = 1f - (float)weaponSkill * 0.01f;
		}
		return TaleWorlds.Library.MathF.Max(0f, b);
	}

	public override float GetBreatheHoldMaxDuration(Agent agent, float baseBreatheHoldMaxDuration)
	{
		return baseBreatheHoldMaxDuration;
	}

	public override void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
	{
		if (agent.IsHuman)
		{
			UpdateHumanAgentStats(agent, agentDrivenProperties);
		}
		else if (agent.IsMount)
		{
			UpdateMountAgentStats(agent, agentDrivenProperties);
		}
	}

	private void UpdateMountAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
	{
		MPPerkObject.MPPerkHandler perkHandler = MPPerkObject.GetPerkHandler(agent.RiderAgent);
		EquipmentElement mountElement = agent.SpawnEquipment[EquipmentIndex.ArmorItemEndSlot];
		EquipmentElement harness = agent.SpawnEquipment[EquipmentIndex.HorseHarness];
		agentDrivenProperties.MountManeuver = (float)mountElement.GetModifiedMountManeuver(in harness) * (1f + (perkHandler?.GetMountManeuver() ?? 0f));
		agentDrivenProperties.MountSpeed = (float)(mountElement.GetModifiedMountSpeed(in harness) + 1) * 0.22f * (1f + (perkHandler?.GetMountSpeed() ?? 0f));
		int num = agent.RiderAgent?.Character.GetSkillValue(DefaultSkills.Riding) ?? 100;
		agentDrivenProperties.TopSpeedReachDuration = Game.Current.BasicModels.RidingModel.CalculateAcceleration(in mountElement, in harness, num);
		agentDrivenProperties.MountSpeed *= 1f + (float)num * 0.0032f;
		agentDrivenProperties.MountManeuver *= 1f + (float)num * 0.0035f;
		float num2 = mountElement.Weight / 2f + (harness.IsEmpty ? 0f : harness.Weight);
		agentDrivenProperties.MountDashAccelerationMultiplier = ((!(num2 > 200f)) ? 1f : ((num2 < 300f) ? (1f - (num2 - 200f) / 111f) : 0.1f));
	}

	public override int GetEffectiveSkillForWeapon(Agent agent, WeaponComponentData weapon)
	{
		int num = base.GetEffectiveSkillForWeapon(agent, weapon);
		if (num > 0 && weapon.IsRangedWeapon)
		{
			MPPerkObject.MPPerkHandler perkHandler = MPPerkObject.GetPerkHandler(agent);
			if (perkHandler != null)
			{
				num = TaleWorlds.Library.MathF.Ceiling((float)num * (perkHandler.GetRangedAccuracy() + 1f));
			}
		}
		return num;
	}

	private void UpdateHumanAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
	{
		MPPerkObject.MPPerkHandler perkHandler = MPPerkObject.GetPerkHandler(agent);
		BasicCharacterObject character = agent.Character;
		MissionEquipment equipment = agent.Equipment;
		float totalWeightOfWeapons = equipment.GetTotalWeightOfWeapons();
		totalWeightOfWeapons *= 1f + (perkHandler?.GetEncumbrance(isOnBody: true) ?? 0f);
		EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
		EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
		if (primaryWieldedItemIndex != EquipmentIndex.None)
		{
			ItemObject item = equipment[primaryWieldedItemIndex].Item;
			WeaponComponent weaponComponent = item.WeaponComponent;
			if (weaponComponent != null)
			{
				float realWeaponLength = weaponComponent.PrimaryWeapon.GetRealWeaponLength();
				float num = ((weaponComponent.GetItemType() == ItemObject.ItemTypeEnum.Bow) ? 4f : 1.5f) * item.Weight * TaleWorlds.Library.MathF.Sqrt(realWeaponLength);
				num *= 1f + (perkHandler?.GetEncumbrance(isOnBody: false) ?? 0f);
				totalWeightOfWeapons += num;
			}
		}
		if (offhandWieldedItemIndex != EquipmentIndex.None)
		{
			ItemObject item2 = equipment[offhandWieldedItemIndex].Item;
			float num2 = 1.5f * item2.Weight;
			num2 *= 1f + (perkHandler?.GetEncumbrance(isOnBody: false) ?? 0f);
			totalWeightOfWeapons += num2;
		}
		agentDrivenProperties.WeaponExternalAccelerationAccuracyPenalty = 0f;
		agentDrivenProperties.WeaponsEncumbrance = totalWeightOfWeapons;
		EquipmentIndex primaryWieldedItemIndex2 = agent.GetPrimaryWieldedItemIndex();
		WeaponComponentData weaponComponentData = ((primaryWieldedItemIndex2 != EquipmentIndex.None) ? equipment[primaryWieldedItemIndex2].CurrentUsageItem : null);
		ItemObject primaryItem = ((primaryWieldedItemIndex2 != EquipmentIndex.None) ? equipment[primaryWieldedItemIndex2].Item : null);
		EquipmentIndex offhandWieldedItemIndex2 = agent.GetOffhandWieldedItemIndex();
		WeaponComponentData secondaryItem = ((offhandWieldedItemIndex2 != EquipmentIndex.None) ? equipment[offhandWieldedItemIndex2].CurrentUsageItem : null);
		agentDrivenProperties.SwingSpeedMultiplier = 0.93f + 0.0007f * (float)GetSkillValueForItem(character, primaryItem);
		agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier = agentDrivenProperties.SwingSpeedMultiplier;
		agentDrivenProperties.HandlingMultiplier = 1f;
		agentDrivenProperties.ShieldBashStunDurationMultiplier = 1f;
		agentDrivenProperties.KickStunDurationMultiplier = 1f;
		agentDrivenProperties.ReloadSpeed = 0.93f + 0.0007f * (float)GetSkillValueForItem(character, primaryItem);
		agentDrivenProperties.MissileSpeedMultiplier = 1f;
		agentDrivenProperties.ReloadMovementPenaltyFactor = 1f;
		agentDrivenProperties.DamageMultiplierBonus = 0f;
		SetAllWeaponInaccuracy(agent, agentDrivenProperties, (int)primaryWieldedItemIndex2, weaponComponentData);
		MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character);
		float num3 = (mPHeroClassForCharacter.IsTroopCharacter(agent.Character) ? mPHeroClassForCharacter.TroopMovementSpeedMultiplier : mPHeroClassForCharacter.HeroMovementSpeedMultiplier);
		agentDrivenProperties.MaxSpeedMultiplier = 1.05f * (num3 * (100f / (100f + totalWeightOfWeapons)));
		int skillValue = character.GetSkillValue(DefaultSkills.Riding);
		bool flag = false;
		bool flag2 = false;
		if (weaponComponentData != null)
		{
			WeaponComponentData weaponComponentData2 = weaponComponentData;
			int effectiveSkillForWeapon = GetEffectiveSkillForWeapon(agent, weaponComponentData2);
			if (perkHandler != null)
			{
				agentDrivenProperties.MissileSpeedMultiplier *= perkHandler.GetThrowingWeaponSpeed(weaponComponentData) + 1f;
			}
			if (weaponComponentData2.IsRangedWeapon)
			{
				int thrustSpeed = weaponComponentData2.ThrustSpeed;
				if (!agent.HasMount)
				{
					float num4 = TaleWorlds.Library.MathF.Max(0f, 1f - (float)effectiveSkillForWeapon / 500f);
					agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = 0.125f * num4;
					agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = 0.1f * num4;
				}
				else
				{
					float num5 = TaleWorlds.Library.MathF.Max(0f, (1f - (float)effectiveSkillForWeapon / 500f) * (1f - (float)skillValue / 1800f));
					agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = 0.025f * num5;
					agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = 0.06f * num5;
				}
				agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = TaleWorlds.Library.MathF.Max(0f, agentDrivenProperties.WeaponMaxMovementAccuracyPenalty);
				agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = TaleWorlds.Library.MathF.Max(0f, agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty);
				if (weaponComponentData2.RelevantSkill == DefaultSkills.Bow)
				{
					float value = ((float)thrustSpeed - 60f) / 75f;
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
						float value3 = ((float)thrustSpeed - 85f) / 17f;
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
					flag = true;
					agentDrivenProperties.WeaponBestAccuracyWaitTime = 0.3f + (95.75f - (float)thrustSpeed) * 0.005f;
					float value4 = ((float)thrustSpeed - 60f) / 75f;
					value4 = MBMath.ClampFloat(value4, 0f, 1f);
					agentDrivenProperties.WeaponUnsteadyBeginTime = 0.1f + (float)effectiveSkillForWeapon * 0.01f * MBMath.Lerp(1f, 2f, value4);
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
					if (weaponComponentData2.WeaponClass == WeaponClass.ThrowingAxe)
					{
						agentDrivenProperties.WeaponInaccuracy *= 6.6f;
					}
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
				flag2 = true;
				agentDrivenProperties.WeaponUnsteadyBeginTime = 1f + (float)effectiveSkillForWeapon * 0.005f;
				agentDrivenProperties.WeaponUnsteadyEndTime = 3f + (float)effectiveSkillForWeapon * 0.01f;
			}
		}
		agentDrivenProperties.AttributeShieldMissileCollisionBodySizeAdder = 0.3f;
		float num6 = agent.MountAgent?.GetAgentDrivenPropertyValue(DrivenProperty.AttributeRiding) ?? 1f;
		agentDrivenProperties.AttributeRiding = (float)skillValue * num6;
		agentDrivenProperties.AttributeHorseArchery = MissionGameModels.Current.StrikeMagnitudeModel.CalculateHorseArcheryFactor(character);
		agentDrivenProperties.BipedalRangedReadySpeedMultiplier = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRangedReadySpeedMultiplier);
		agentDrivenProperties.BipedalRangedReloadSpeedMultiplier = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRangedReloadSpeedMultiplier);
		if (perkHandler != null)
		{
			for (int i = 64; i < 98; i++)
			{
				DrivenProperty drivenProperty = (DrivenProperty)i;
				if (((drivenProperty != DrivenProperty.WeaponUnsteadyBeginTime && drivenProperty != DrivenProperty.WeaponUnsteadyEndTime) || flag || flag2) && (drivenProperty != DrivenProperty.WeaponRotationalAccuracyPenaltyInRadians || flag))
				{
					float stat = agentDrivenProperties.GetStat(drivenProperty);
					agentDrivenProperties.SetStat(drivenProperty, stat + perkHandler.GetDrivenPropertyBonus(drivenProperty, stat));
				}
			}
		}
		if (agent.Character != null && agent.HasMount && weaponComponentData != null)
		{
			SetMountedWeaponPenaltiesOnAgent(agent, agentDrivenProperties, weaponComponentData);
		}
		SetAiRelatedProperties(agent, agentDrivenProperties, weaponComponentData, secondaryItem);
	}

	private void FillAgentStatsFromData(ref AgentDrivenProperties agentDrivenProperties, Agent agent, MultiplayerClassDivisions.MPHeroClass heroClass, MissionPeer missionPeer, MissionPeer owningMissionPeer)
	{
		MissionPeer missionPeer2 = missionPeer ?? owningMissionPeer;
		if (missionPeer2 != null)
		{
			MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(missionPeer2);
			bool isPlayer = missionPeer != null;
			for (int i = 0; i < 64; i++)
			{
				DrivenProperty drivenProperty = (DrivenProperty)i;
				float stat = agentDrivenProperties.GetStat(drivenProperty);
				if (drivenProperty == DrivenProperty.ArmorHead || drivenProperty == DrivenProperty.ArmorTorso || drivenProperty == DrivenProperty.ArmorLegs || drivenProperty == DrivenProperty.ArmorArms)
				{
					agentDrivenProperties.SetStat(drivenProperty, stat + (float)heroClass.ArmorValue + onSpawnPerkHandler.GetDrivenPropertyBonusOnSpawn(isPlayer, drivenProperty, stat));
				}
				else
				{
					agentDrivenProperties.SetStat(drivenProperty, stat + onSpawnPerkHandler.GetDrivenPropertyBonusOnSpawn(isPlayer, drivenProperty, stat));
				}
			}
		}
		float topSpeedReachDuration = (heroClass.IsTroopCharacter(agent.Character) ? heroClass.TroopTopSpeedReachDuration : heroClass.HeroTopSpeedReachDuration);
		agentDrivenProperties.TopSpeedReachDuration = topSpeedReachDuration;
		float managedParameter = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalCombatSpeedMinMultiplier);
		float managedParameter2 = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalCombatSpeedMaxMultiplier);
		float num = (heroClass.IsTroopCharacter(agent.Character) ? heroClass.TroopCombatMovementSpeedMultiplier : heroClass.HeroCombatMovementSpeedMultiplier);
		agentDrivenProperties.CombatMaxSpeedMultiplier = managedParameter + (managedParameter2 - managedParameter) * num;
		agentDrivenProperties.CrouchedSpeedMultiplier = 1f;
	}

	private int GetSkillValueForItem(BasicCharacterObject characterObject, ItemObject primaryItem)
	{
		return characterObject.GetSkillValue((primaryItem != null) ? primaryItem.RelevantSkill : DefaultSkills.Athletics);
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

	public static float CalculateMaximumSpeedMultiplier(Agent agent)
	{
		MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character);
		if (!mPHeroClassForCharacter.IsTroopCharacter(agent.Character))
		{
			return mPHeroClassForCharacter.HeroMovementSpeedMultiplier;
		}
		return mPHeroClassForCharacter.TroopMovementSpeedMultiplier;
	}
}
