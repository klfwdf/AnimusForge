using NavalDLC.CharacterDevelopment;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.ComponentInterfaces;

public class NavalCustomBattleAgentStatCalculateModel : AgentStatCalculateModel
{
	private const int MinMarinerSkillToConsiderAgentAsMariner = 40;

	public override float GetDifficultyModifier()
	{
		return base.BaseModel.GetDifficultyModifier();
	}

	public override bool CanAgentRideMount(Agent agent, Agent targetMount)
	{
		return base.BaseModel.CanAgentRideMount(agent, targetMount);
	}

	public override void InitializeAgentStatsAfterDeploymentFinished(Agent agent)
	{
		base.BaseModel.InitializeAgentStatsAfterDeploymentFinished(agent);
		NavalShipsLogic missionBehavior = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		AgentDrivenProperties agentDrivenProperties = agent.AgentDrivenProperties;
		if (missionBehavior == null)
		{
			return;
		}
		foreach (MissionShip allShip in missionBehavior.AllShips)
		{
			if (allShip.GetIsAgentOnShip(agent, bypassSteppedShipCheck: true))
			{
				agentDrivenProperties.MeleeWeaponDamageMultiplierBonus += allShip.ShipOrigin.CrewMeleeDamageFactor;
				break;
			}
		}
	}

	public override void InitializeMissionEquipmentAfterDeploymentFinished(Agent agent)
	{
		base.BaseModel.InitializeMissionEquipmentAfterDeploymentFinished(agent);
		NavalShipsLogic missionBehavior = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		if (missionBehavior == null || Mission.Current.IsNavalRaidBattle)
		{
			return;
		}
		foreach (MissionShip allShip in missionBehavior.AllShips)
		{
			if (!allShip.GetIsAgentOnShip(agent, bypassSteppedShipCheck: true))
			{
				continue;
			}
			bool flag = MathF.Abs(allShip.ShipOrigin.CrewShieldHitPointsFactor) > 1E-05f;
			bool flag2 = allShip.ShipOrigin.AdditionalArcherQuivers != 0;
			bool flag3 = allShip.ShipOrigin.AdditionalThrowingWeaponStack != 0;
			if (!(flag || flag2 || flag3))
			{
				break;
			}
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ExtraWeaponSlot; equipmentIndex++)
			{
				if (agent.Equipment[equipmentIndex].IsEmpty)
				{
					continue;
				}
				WeaponComponentData weaponComponentDataForUsage = agent.Equipment[equipmentIndex].GetWeaponComponentDataForUsage(0);
				if (weaponComponentDataForUsage.IsShield)
				{
					if (flag)
					{
						agent.Equipment.SetHitPointsOfSlot(equipmentIndex, (short)((float)agent.Equipment[equipmentIndex].ModifiedMaxHitPoints * (1f + allShip.ShipOrigin.CrewShieldHitPointsFactor)), addOverflowToMaxHitPoints: true);
						flag = false;
					}
				}
				else
				{
					if (!weaponComponentDataForUsage.IsConsumable)
					{
						continue;
					}
					if (weaponComponentDataForUsage.IsRangedWeapon)
					{
						if (flag3)
						{
							agent.Equipment.SetAmountOfSlot(equipmentIndex, (short)(agent.Equipment[equipmentIndex].ModifiedMaxAmount * (1 + allShip.ShipOrigin.AdditionalThrowingWeaponStack)), addOverflowToMaxAmount: true);
							agent.SetWeaponAmountInSlot(equipmentIndex, agent.Equipment[equipmentIndex].Amount, enforcePrimaryItem: true);
							flag3 = false;
						}
					}
					else if (weaponComponentDataForUsage.IsAmmo && flag2)
					{
						agent.Equipment.SetAmountOfSlot(equipmentIndex, (short)(agent.Equipment[equipmentIndex].ModifiedMaxAmount * (1 + allShip.ShipOrigin.AdditionalArcherQuivers)), addOverflowToMaxAmount: true);
						agent.SetWeaponAmountInSlot(equipmentIndex, agent.Equipment[equipmentIndex].Amount, enforcePrimaryItem: true);
						flag2 = false;
					}
				}
			}
			break;
		}
	}

	public override void InitializeAgentStats(Agent agent, Equipment spawnEquipment, AgentDrivenProperties agentDrivenProperties, AgentBuildData agentBuildData)
	{
		base.BaseModel.InitializeAgentStats(agent, spawnEquipment, agentDrivenProperties, agentBuildData);
	}

	public override void InitializeMissionEquipment(Agent agent)
	{
		base.BaseModel.InitializeMissionEquipment(agent);
	}

	public override void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
	{
		base.BaseModel.UpdateAgentStats(agent, agentDrivenProperties);
		if (Mission.Current.IsNavalBattle && agent.IsHuman)
		{
			UpdateNavalHumanStats(agent, agentDrivenProperties);
		}
	}

	private void UpdateNavalHumanStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
	{
		bool flag = GetEffectiveSkill(agent, NavalSkills.Mariner) >= 40;
		MissionEquipment equipment = agent.Equipment;
		EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
		WeaponComponentData weaponComponentData = ((primaryWieldedItemIndex != EquipmentIndex.None) ? equipment[primaryWieldedItemIndex].CurrentUsageItem : null);
		if (weaponComponentData != null && weaponComponentData.IsRangedWeapon && !flag)
		{
			float num = 1.3f;
			agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= num;
			agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= num;
			agentDrivenProperties.AiShooterErrorWoRangeUpdate += 0.2f;
			agentDrivenProperties.WeaponInaccuracy *= 1.3f;
			agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians *= 1.3f;
			agentDrivenProperties.WeaponExternalAccelerationAccuracyPenalty += 0.03f;
		}
		if (!flag)
		{
			agentDrivenProperties.MaxSpeedMultiplier *= 0.8f;
			agentDrivenProperties.DamageMultiplierBonus -= 0.2f;
		}
	}

	public override int GetEffectiveSkill(Agent agent, SkillObject skill)
	{
		return base.BaseModel.GetEffectiveSkill(agent, skill);
	}

	public override float GetWeaponDamageMultiplier(Agent agent, WeaponComponentData weapon)
	{
		return base.BaseModel.GetWeaponDamageMultiplier(agent, weapon);
	}

	public override float GetEquipmentStealthBonus(Agent agent)
	{
		return base.BaseModel.GetEquipmentStealthBonus(agent);
	}

	public override float GetSneakAttackMultiplier(Agent agent, WeaponComponentData weapon)
	{
		return base.BaseModel.GetSneakAttackMultiplier(agent, weapon);
	}

	public override float GetKnockBackResistance(Agent agent)
	{
		return base.BaseModel.GetKnockBackResistance(agent);
	}

	public override float GetKnockDownResistance(Agent agent, StrikeType strikeType = StrikeType.Invalid)
	{
		return base.BaseModel.GetKnockDownResistance(agent, strikeType);
	}

	public override float GetDismountResistance(Agent agent)
	{
		return base.BaseModel.GetDismountResistance(agent);
	}

	public override float GetWeaponInaccuracy(Agent agent, WeaponComponentData weapon, int weaponSkill)
	{
		return base.BaseModel.GetWeaponInaccuracy(agent, weapon, weaponSkill);
	}

	public override float GetInteractionDistance(Agent agent)
	{
		return base.BaseModel.GetInteractionDistance(agent);
	}

	public override float GetMaxCameraZoom(Agent agent)
	{
		return base.BaseModel.GetMaxCameraZoom(agent);
	}

	public override string GetMissionDebugInfoForAgent(Agent agent)
	{
		return base.BaseModel.GetMissionDebugInfoForAgent(agent);
	}

	public override float GetEffectiveMaxHealth(Agent agent)
	{
		return base.BaseModel.GetEffectiveMaxHealth(agent);
	}

	public override float GetEnvironmentSpeedFactor(Agent agent)
	{
		return base.BaseModel.GetEnvironmentSpeedFactor(agent);
	}

	public override float GetBreatheHoldMaxDuration(Agent agent, float baseBreatheHoldMaxDuration)
	{
		if (agent.IsHuman)
		{
			float num = base.BaseModel.GetBreatheHoldMaxDuration(agent, baseBreatheHoldMaxDuration);
			if (GetEffectiveSkill(agent, NavalSkills.Mariner) >= 40)
			{
				num += 20f;
			}
			return num;
		}
		return 1E+09f;
	}
}
