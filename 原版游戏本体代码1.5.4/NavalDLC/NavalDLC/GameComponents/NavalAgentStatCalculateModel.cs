using System.Collections.Generic;
using Helpers;
using NavalDLC.CharacterDevelopment;
using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.GameComponents;

public class NavalAgentStatCalculateModel : AgentStatCalculateModel
{
	private Dictionary<Agent, Figurehead> _agentFigureHeadSpawnMap = new Dictionary<Agent, Figurehead>();

	public void ClearMissionState()
	{
		_agentFigureHeadSpawnMap.Clear();
	}

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
		PartyBase partyBase = agent?.Origin?.BattleCombatant as PartyBase;
		CharacterObject characterObject = ((partyBase != null && partyBase.IsMobile) ? partyBase.MobileParty : null)?.Army?.LeaderParty?.LeaderHero?.CharacterObject;
		Ship ship = ((partyBase == null || partyBase.Ships.Count <= 0) ? null : partyBase?.FlagShip);
		Figurehead figurehead = ship?.Figurehead;
		bool flag = characterObject != null && characterObject.GetPerkValue(NavalPerks.Shipmaster.Commodore) && ship != null && figurehead != null;
		if (flag)
		{
			ApplyFigureheadBonuses(agent, agentDrivenProperties, figurehead);
		}
		foreach (MissionShip allShip in missionBehavior.AllShips)
		{
			Ship ship2 = allShip.ShipOrigin as Ship;
			if ((!flag || ship2 != ship) && allShip.GetIsAgentOnShip(agent, bypassSteppedShipCheck: true))
			{
				Figurehead figurehead2 = ship2?.Figurehead;
				if (figurehead2 != null)
				{
					ApplyFigureheadBonuses(agent, agentDrivenProperties, figurehead2);
				}
				agentDrivenProperties.MeleeWeaponDamageMultiplierBonus += allShip.ShipOrigin.CrewMeleeDamageFactor;
				break;
			}
		}
	}

	private void ApplyFigureheadBonuses(Agent agent, AgentDrivenProperties agentDrivenProperties, Figurehead figureHead)
	{
		float effectAmount = figureHead.EffectAmount;
		if (figureHead == DefaultFigureheads.Hawk || figureHead == DefaultFigureheads.Boar)
		{
			_agentFigureHeadSpawnMap.Add(agent, figureHead);
		}
		else if (figureHead == DefaultFigureheads.Raven)
		{
			agentDrivenProperties.ThrowingWeaponDamageMultiplierBonus += effectAmount;
		}
		else if (figureHead == DefaultFigureheads.SaberToothTiger)
		{
			agentDrivenProperties.ArmorPenetrationMultiplierCrossbow += effectAmount;
			agentDrivenProperties.ArmorPenetrationMultiplierBow += effectAmount;
		}
		else if (figureHead == DefaultFigureheads.Oxen)
		{
			agent.HealthLimit += effectAmount;
			agent.Health += effectAmount;
		}
	}

	public override void InitializeMissionEquipmentAfterDeploymentFinished(Agent agent)
	{
		base.BaseModel.InitializeMissionEquipmentAfterDeploymentFinished(agent);
		if (Mission.Current.IsNavalBattle && agent.IsHuman)
		{
			CharacterObject characterObject = agent.Character as CharacterObject;
			_ = agent.CurrentBattleEnvironment;
			if (characterObject != null)
			{
				CharacterObject characterObject2 = agent.Formation?.Captain?.Character as CharacterObject;
				if (characterObject == characterObject2)
				{
					characterObject2 = null;
				}
				MissionEquipment equipment = agent.Equipment;
				for (int i = 0; i < 5; i++)
				{
					EquipmentIndex equipmentIndex = (EquipmentIndex)i;
					MissionWeapon missionWeapon = equipment[equipmentIndex];
					if (missionWeapon.IsEmpty)
					{
						continue;
					}
					WeaponComponentData currentUsageItem = missionWeapon.CurrentUsageItem;
					if (currentUsageItem != null && currentUsageItem.IsConsumable && currentUsageItem.RelevantSkill != null)
					{
						ExplainedNumber explainedNumber = new ExplainedNumber(0f, includeDescriptions: false, null);
						if (currentUsageItem.RelevantSkill == DefaultSkills.Throwing && characterObject2 != null && characterObject2.GetPerkValue(NavalPerks.Boatswain.WellStocked))
						{
							explainedNumber.Add(NavalPerks.Boatswain.WellStocked.SecondaryBonus);
						}
						int num = MathF.Round(explainedNumber.ResultNumber);
						ExplainedNumber explainedNumber2 = new ExplainedNumber(missionWeapon.Amount + num);
						if ((currentUsageItem.RelevantSkill == DefaultSkills.Bow || currentUsageItem.RelevantSkill == DefaultSkills.Crossbow || currentUsageItem.RelevantSkill == DefaultSkills.Throwing) && characterObject2 != null && characterObject2.GetPerkValue(NavalPerks.Boatswain.WellStocked, agent.CurrentBattleEnvironment, isPrimaryEffect: true, out var effectValue))
						{
							explainedNumber2.AddFactor(effectValue);
						}
						if (characterObject2 != null && characterObject2.GetPerkValue(NavalPerks.Boatswain.ShipwrightsInsight))
						{
							explainedNumber2.AddFactor(NavalPerks.Boatswain.ShipwrightsInsight.SecondaryBonus);
						}
						int num2 = MathF.Round(explainedNumber2.ResultNumber);
						if (num2 != missionWeapon.Amount)
						{
							equipment.SetAmountOfSlot(equipmentIndex, (short)num2, addOverflowToMaxAmount: true);
						}
					}
				}
			}
		}
		NavalShipsLogic missionBehavior = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		if (missionBehavior == null)
		{
			return;
		}
		foreach (MissionShip allShip in missionBehavior.AllShips)
		{
			if (!allShip.GetIsAgentOnShip(agent, bypassSteppedShipCheck: true) || Mission.Current.IsNavalRaidBattle)
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
			for (EquipmentIndex equipmentIndex2 = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex2 < EquipmentIndex.ExtraWeaponSlot; equipmentIndex2++)
			{
				if (agent.Equipment[equipmentIndex2].IsEmpty)
				{
					continue;
				}
				WeaponComponentData weaponComponentDataForUsage = agent.Equipment[equipmentIndex2].GetWeaponComponentDataForUsage(0);
				if (weaponComponentDataForUsage.IsShield)
				{
					if (flag)
					{
						agent.Equipment.SetHitPointsOfSlot(equipmentIndex2, (short)((float)agent.Equipment[equipmentIndex2].ModifiedMaxHitPoints * (1f + allShip.ShipOrigin.CrewShieldHitPointsFactor)), addOverflowToMaxHitPoints: true);
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
							agent.Equipment.SetAmountOfSlot(equipmentIndex2, (short)(agent.Equipment[equipmentIndex2].ModifiedMaxAmount * (1 + allShip.ShipOrigin.AdditionalThrowingWeaponStack)), addOverflowToMaxAmount: true);
							agent.SetWeaponAmountInSlot(equipmentIndex2, agent.Equipment[equipmentIndex2].Amount, enforcePrimaryItem: true);
							flag3 = false;
						}
					}
					else if (weaponComponentDataForUsage.IsAmmo && flag2)
					{
						agent.Equipment.SetAmountOfSlot(equipmentIndex2, (short)(agent.Equipment[equipmentIndex2].ModifiedMaxAmount * (1 + allShip.ShipOrigin.AdditionalArcherQuivers)), addOverflowToMaxAmount: true);
						agent.SetWeaponAmountInSlot(equipmentIndex2, agent.Equipment[equipmentIndex2].Amount, enforcePrimaryItem: true);
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
		if (!Mission.Current.IsNavalBattle || !agent.IsHuman)
		{
			return;
		}
		UpdateNavalHumanStats(agent, agentDrivenProperties);
		MissionShip missionShip = agent.GetComponent<AgentNavalComponent>()?.SteppedShip;
		Figurehead figurehead = (missionShip?.ShipOrigin as Ship)?.Figurehead;
		if (figurehead != null && figurehead == DefaultFigureheads.Siren && agent.Team.Side != (missionShip.Team?.Side ?? BattleSideEnum.None))
		{
			agentDrivenProperties.DamageMultiplierBonus += figurehead.EffectAmount;
		}
		if (_agentFigureHeadSpawnMap.TryGetValue(agent, out var value))
		{
			float effectAmount = value.EffectAmount;
			if (value == DefaultFigureheads.Hawk)
			{
				agentDrivenProperties.WeaponInaccuracy *= 1f - effectAmount;
			}
			else if (value == DefaultFigureheads.Boar)
			{
				effectAmount += 1f;
				agentDrivenProperties.ArmorHead *= effectAmount;
				agentDrivenProperties.ArmorTorso *= effectAmount;
				agentDrivenProperties.ArmorArms *= effectAmount;
				agentDrivenProperties.ArmorLegs *= effectAmount;
			}
		}
	}

	private void UpdateNavalHumanStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(0.3f);
		ExplainedNumber explainedNumber2 = new ExplainedNumber(0.3f);
		ExplainedNumber explainedNumber3 = new ExplainedNumber(0.2f);
		ExplainedNumber explainedNumber4 = new ExplainedNumber(0.3f);
		ExplainedNumber explainedNumber5 = new ExplainedNumber(0.03f);
		ExplainedNumber explainedNumber6 = new ExplainedNumber(0.2f);
		ExplainedNumber explainedNumber7 = new ExplainedNumber(0.2f);
		explainedNumber.LimitMin(0f);
		explainedNumber2.LimitMin(0f);
		explainedNumber3.LimitMin(0f);
		explainedNumber4.LimitMin(0f);
		explainedNumber5.LimitMin(0f);
		explainedNumber6.LimitMin(0f);
		explainedNumber7.LimitMin(0f);
		BasicCharacterObject character = agent.Character;
		BattleEnvironment currentBattleEnvironment = agent.CurrentBattleEnvironment;
		CharacterObject characterObject = character as CharacterObject;
		if (agent.IsHero)
		{
			int effectiveSkill = GetEffectiveSkill(agent, NavalSkills.Mariner);
			SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleCombatPenaltyNegation, ref explainedNumber, effectiveSkill);
			SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleCombatPenaltyNegation, ref explainedNumber2, effectiveSkill);
			SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleCombatPenaltyNegation, ref explainedNumber3, effectiveSkill);
			SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleCombatPenaltyNegation, ref explainedNumber4, effectiveSkill);
			SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleCombatPenaltyNegation, ref explainedNumber5, effectiveSkill);
			SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleCombatPenaltyNegation, ref explainedNumber6, effectiveSkill);
			SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleCombatPenaltyNegation, ref explainedNumber7, effectiveSkill);
		}
		else if (characterObject.IsMariner)
		{
			int skillLevel = MathF.Round(1f / MathF.Abs(NavalSkillEffects.NavalBattleCombatPenaltyNegation.Bonus));
			SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleCombatPenaltyNegation, ref explainedNumber, skillLevel);
			SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleCombatPenaltyNegation, ref explainedNumber2, skillLevel);
			SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleCombatPenaltyNegation, ref explainedNumber3, skillLevel);
			SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleCombatPenaltyNegation, ref explainedNumber4, skillLevel);
			SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleCombatPenaltyNegation, ref explainedNumber5, skillLevel);
			SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleCombatPenaltyNegation, ref explainedNumber6, skillLevel);
			SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleCombatPenaltyNegation, ref explainedNumber7, skillLevel);
		}
		MissionEquipment equipment = agent.Equipment;
		CharacterObject captainCharacter = (agent.Formation?.Captain)?.Character as CharacterObject;
		if (agent.Formation?.Captain == agent)
		{
			captainCharacter = null;
		}
		PerkHelper.AddPerkBonusForCharacter(NavalPerks.Shipmaster.WindRider, currentBattleEnvironment, characterObject, isPrimaryBonus: true, ref explainedNumber6);
		PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Shipmaster.WindRider, currentBattleEnvironment, captainCharacter, ref explainedNumber6);
		PerkHelper.AddPerkBonusForCharacter(NavalPerks.Mariner.RollingThunder, currentBattleEnvironment, characterObject, isPrimaryBonus: true, ref explainedNumber3);
		EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
		WeaponComponentData weaponComponentData = ((primaryWieldedItemIndex != EquipmentIndex.None) ? equipment[primaryWieldedItemIndex].CurrentUsageItem : null);
		if (weaponComponentData != null && weaponComponentData.IsRangedWeapon)
		{
			PerkHelper.AddPerkBonusForCharacter(NavalPerks.Mariner.RollingThunder, currentBattleEnvironment, characterObject, isPrimaryBonus: true, ref explainedNumber2);
			float num = 1f + explainedNumber2.ResultNumber;
			agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= num;
			agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= num;
			agentDrivenProperties.AiShooterErrorWoRangeUpdate += explainedNumber3.ResultNumber;
			agentDrivenProperties.WeaponInaccuracy *= 1f + explainedNumber4.ResultNumber;
			agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians *= 1f + explainedNumber.ResultNumber;
			agentDrivenProperties.WeaponExternalAccelerationAccuracyPenalty += explainedNumber5.ResultNumber;
		}
		agentDrivenProperties.MaxSpeedMultiplier *= 1f - explainedNumber6.ResultNumber;
		agentDrivenProperties.DamageMultiplierBonus -= explainedNumber7.ResultNumber;
		if (characterObject != null)
		{
			SetNavalPerksAndEffectsOnAgent(agent, characterObject, agentDrivenProperties, weaponComponentData);
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
			CharacterObject characterObject = (agent.Formation?.Captain)?.Character as CharacterObject;
			CharacterObject characterObject2 = agent.Character as CharacterObject;
			float breatheHoldMaxDuration = base.BaseModel.GetBreatheHoldMaxDuration(agent, baseBreatheHoldMaxDuration);
			if (characterObject2 == characterObject)
			{
				characterObject = null;
			}
			int effectiveSkill = GetEffectiveSkill(agent, NavalSkills.Mariner);
			ExplainedNumber explainedNumber = new ExplainedNumber(0f, includeDescriptions: false, null);
			if (agent.IsHero)
			{
				SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleUnderwaterBreathingDurationBonus, ref explainedNumber, effectiveSkill);
			}
			else if (characterObject2.IsMariner)
			{
				int skillLevel = MathF.Round(NavalSkillEffects.NavalBattleUnderwaterBreathingDurationBonus.LimitMax / NavalSkillEffects.NavalBattleUnderwaterBreathingDurationBonus.Bonus);
				SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.NavalBattleUnderwaterBreathingDurationBonus, ref explainedNumber, skillLevel);
			}
			breatheHoldMaxDuration += explainedNumber.ResultNumber;
			if (agent.GetBaseArmorEffectivenessForBodyPart(BoneBodyPartType.Chest) > 10f)
			{
				breatheHoldMaxDuration -= 10f;
			}
			ExplainedNumber bonuses = new ExplainedNumber(breatheHoldMaxDuration);
			if (Mission.Current.IsNavalBattle && characterObject != null)
			{
				PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Shipmaster.OldSaltsTouch, agent.CurrentBattleEnvironment, characterObject, ref bonuses);
			}
			return bonuses.ResultNumber;
		}
		return 1E+09f;
	}

	private void SetNavalPerksAndEffectsOnAgent(Agent agent, CharacterObject agentCharacter, AgentDrivenProperties agentDrivenProperties, WeaponComponentData equippedWeaponComponent)
	{
		BattleEnvironment currentBattleEnvironment = agent.CurrentBattleEnvironment;
		CharacterObject characterObject = agent.Formation?.Captain?.Character as CharacterObject;
		if (agent.Formation?.Captain == agent)
		{
			characterObject = null;
		}
		bool flag = equippedWeaponComponent?.IsMeleeWeapon ?? false;
		if (equippedWeaponComponent != null && flag)
		{
			ExplainedNumber bonuses = new ExplainedNumber(agentDrivenProperties.HandlingMultiplier);
			PerkHelper.AddPerkBonusForCharacter(NavalPerks.Mariner.PiratesProwess, currentBattleEnvironment, agentCharacter, isPrimaryBonus: true, ref bonuses);
			agentDrivenProperties.HandlingMultiplier = bonuses.ResultNumber;
		}
		float num = 0f;
		float num2 = 0f;
		bool flag2 = false;
		if (characterObject == null)
		{
			return;
		}
		if (agentCharacter.Tier <= 3)
		{
			ExplainedNumber bonuses2 = new ExplainedNumber(0f, includeDescriptions: false, null);
			PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Boatswain.SpecialArrows, agent.CurrentBattleEnvironment, characterObject, ref bonuses2);
			if (bonuses2.ResultNumber > 0f)
			{
				num += bonuses2.ResultNumber;
				flag2 = true;
			}
		}
		if (flag2)
		{
			float num3 = 1f + num2;
			agentDrivenProperties.ArmorHead = MathF.Max(0f, (agentDrivenProperties.ArmorHead + num) * num3);
			agentDrivenProperties.ArmorTorso = MathF.Max(0f, (agentDrivenProperties.ArmorTorso + num) * num3);
			agentDrivenProperties.ArmorArms = MathF.Max(0f, (agentDrivenProperties.ArmorArms + num) * num3);
			agentDrivenProperties.ArmorLegs = MathF.Max(0f, (agentDrivenProperties.ArmorLegs + num) * num3);
		}
	}
}
