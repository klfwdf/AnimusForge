using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.GameComponents;

public class SandboxAgentStatCalculateModel : AgentStatCalculateModel
{
	public override float GetDifficultyModifier()
	{
		return Campaign.Current?.Models?.DifficultyModel?.GetCombatAIDifficultyMultiplier() ?? 1f;
	}

	public override bool CanAgentRideMount(Agent agent, Agent targetMount)
	{
		return agent.CheckSkillForMounting(targetMount);
	}

	public override void InitializeAgentStats(Agent agent, Equipment spawnEquipment, AgentDrivenProperties agentDrivenProperties, AgentBuildData agentBuildData)
	{
		agentDrivenProperties.ArmorEncumbrance = GetEffectiveArmorEncumbrance(agent, spawnEquipment);
		if (agent.IsHero)
		{
			CharacterObject obj = agent.Character as CharacterObject;
			AgentFlag agentFlag = agent.GetAgentFlags();
			if (obj.GetPerkValue(DefaultPerks.Bow.HorseMaster))
			{
				agentFlag |= AgentFlag.CanUseAllBowsMounted;
			}
			if (obj.GetPerkValue(DefaultPerks.Crossbow.MountedCrossbowman))
			{
				agentFlag |= AgentFlag.CanReloadAllXBowsMounted;
			}
			if (obj.GetPerkValue(DefaultPerks.TwoHanded.ProjectileDeflection))
			{
				agentFlag |= AgentFlag.CanDeflectArrowsWith2HSword;
			}
			agent.SetAgentFlags(agentFlag);
		}
		else
		{
			agent.HealthLimit = GetEffectiveMaxHealth(agent);
			agent.Health = agent.HealthLimit;
		}
		agentDrivenProperties.OffhandWeaponDefendSpeedMultiplier = 1f;
		MissionGameModels.Current.AgentStatCalculateModel.UpdateAgentStats(agent, agentDrivenProperties);
	}

	public override void InitializeMissionEquipment(Agent agent)
	{
		if (!agent.IsHuman || !(agent.Character is CharacterObject characterObject))
		{
			return;
		}
		PartyBase partyBase = (PartyBase)(agent?.Origin?.BattleCombatant);
		MapEvent mapEvent = partyBase?.MapEvent;
		MobileParty mobileParty = ((partyBase != null && partyBase.IsMobile) ? partyBase.MobileParty : null);
		CharacterObject characterObject2 = PartyBaseHelper.GetVisualPartyLeader(partyBase);
		if (characterObject2 == characterObject)
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
			if (currentUsageItem == null)
			{
				continue;
			}
			if (currentUsageItem.IsConsumable && currentUsageItem.RelevantSkill != null)
			{
				ExplainedNumber bonuses = new ExplainedNumber(0f, includeDescriptions: false, null);
				if (currentUsageItem.RelevantSkill == DefaultSkills.Bow)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.DeepQuivers, characterObject, isPrimaryBonus: true, ref bonuses);
					if (characterObject2 != null && characterObject2.GetPerkValue(DefaultPerks.Bow.DeepQuivers))
					{
						bonuses.Add(DefaultPerks.Bow.DeepQuivers.SecondaryBonus);
					}
				}
				else if (currentUsageItem.RelevantSkill == DefaultSkills.Crossbow)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Crossbow.Fletcher, characterObject, isPrimaryBonus: true, ref bonuses);
					if (characterObject2 != null && characterObject2.GetPerkValue(DefaultPerks.Crossbow.Fletcher))
					{
						bonuses.Add(DefaultPerks.Crossbow.Fletcher.SecondaryBonus);
					}
				}
				else if (currentUsageItem.RelevantSkill == DefaultSkills.Throwing)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.WellPrepared, characterObject, isPrimaryBonus: true, ref bonuses);
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.Resourceful, characterObject, isPrimaryBonus: true, ref bonuses);
					if (agent.HasMount)
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.Saddlebags, characterObject, isPrimaryBonus: true, ref bonuses);
					}
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Throwing.WellPrepared, mobileParty, isPrimaryBonus: false, ref bonuses);
				}
				int num = TaleWorlds.Library.MathF.Round(bonuses.ResultNumber);
				ExplainedNumber stat = new ExplainedNumber(missionWeapon.Amount + num);
				if (mobileParty != null && mapEvent != null && mapEvent.AttackerSide == partyBase.MapEventSide && mapEvent.EventType == MapEvent.BattleTypes.Siege)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Engineering.MilitaryPlanner, mobileParty, isPrimaryBonus: true, ref stat);
				}
				int num2 = TaleWorlds.Library.MathF.Round(stat.ResultNumber);
				if (num2 != missionWeapon.Amount)
				{
					equipment.SetAmountOfSlot(equipmentIndex, (short)num2, addOverflowToMaxAmount: true);
				}
			}
			else if (currentUsageItem.IsShield)
			{
				ExplainedNumber bonuses2 = new ExplainedNumber(missionWeapon.HitPoints);
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Engineering.Scaffolds, characterObject, isPrimaryBonus: false, ref bonuses2);
				int num3 = TaleWorlds.Library.MathF.Round(bonuses2.ResultNumber);
				if (num3 != missionWeapon.HitPoints)
				{
					equipment.SetHitPointsOfSlot(equipmentIndex, (short)num3, addOverflowToMaxHitPoints: true);
				}
			}
		}
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

	public override int GetEffectiveSkill(Agent agent, SkillObject skill)
	{
		ExplainedNumber bonuses = new ExplainedNumber(base.GetEffectiveSkill(agent, skill));
		CharacterObject characterObject = agent.Character as CharacterObject;
		Formation formation = agent.Formation;
		PartyBase partyBase = (PartyBase)(agent.Origin?.BattleCombatant);
		MobileParty mobileParty = ((partyBase != null && partyBase.IsMobile) ? partyBase.MobileParty : null);
		CharacterObject characterObject2 = formation?.Captain?.Character as CharacterObject;
		if (characterObject2 == characterObject)
		{
			characterObject2 = null;
		}
		if (characterObject2 != null)
		{
			bool flag = skill == DefaultSkills.Bow || skill == DefaultSkills.Crossbow || skill == DefaultSkills.Throwing;
			bool flag2 = skill == DefaultSkills.OneHanded || skill == DefaultSkills.TwoHanded || skill == DefaultSkills.Polearm;
			if ((characterObject.IsInfantry && flag) || (characterObject.IsRanged && flag2))
			{
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Throwing.FlexibleFighter, characterObject2, ref bonuses);
			}
		}
		if (skill == DefaultSkills.Bow)
		{
			if (characterObject2 != null)
			{
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Bow.DeadAim, characterObject2, ref bonuses);
				if (characterObject.HasMount())
				{
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Bow.HorseMaster, characterObject2, ref bonuses);
				}
			}
		}
		else if (skill == DefaultSkills.Throwing)
		{
			if (characterObject2 != null)
			{
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Athletics.StrongArms, characterObject2, ref bonuses);
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Throwing.RunningThrow, characterObject2, ref bonuses);
			}
		}
		else if (skill == DefaultSkills.Crossbow && characterObject2 != null)
		{
			PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Crossbow.DonkeysSwiftness, characterObject2, ref bonuses);
		}
		if (mobileParty != null && !mobileParty.IsCurrentlyAtSea && mobileParty.HasPerk(DefaultPerks.Roguery.OneOfTheFamily) && characterObject.Occupation == Occupation.Bandit && skill.Attributes.Any((CharacterAttribute attribute) => attribute == DefaultCharacterAttributes.Vigor || attribute == DefaultCharacterAttributes.Control))
		{
			bonuses.Add(DefaultPerks.Roguery.OneOfTheFamily.PrimaryBonus, DefaultPerks.Roguery.OneOfTheFamily.Name);
		}
		if (characterObject.HasMount())
		{
			if (skill == DefaultSkills.Riding && characterObject2 != null)
			{
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Riding.NimbleSteed, characterObject2, ref bonuses);
			}
		}
		else
		{
			if (mobileParty != null && formation != null)
			{
				bool num = skill == DefaultSkills.OneHanded || skill == DefaultSkills.TwoHanded || skill == DefaultSkills.Polearm;
				bool flag3 = formation.ArrangementOrder.OrderEnum == ArrangementOrder.ArrangementOrderEnum.ShieldWall;
				if (num && flag3)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Polearm.Phalanx, mobileParty, isPrimaryBonus: true, ref bonuses);
				}
			}
			if (characterObject2 != null)
			{
				if (skill == DefaultSkills.OneHanded)
				{
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.OneHanded.WrappedHandles, characterObject2, ref bonuses);
				}
				else if (skill == DefaultSkills.TwoHanded)
				{
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.TwoHanded.StrongGrip, characterObject2, ref bonuses);
				}
				else if (skill == DefaultSkills.Polearm)
				{
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Polearm.CleanThrust, characterObject2, ref bonuses);
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Polearm.CounterWeight, characterObject2, ref bonuses);
				}
			}
		}
		return (int)bonuses.ResultNumber;
	}

	public override float GetWeaponDamageMultiplier(Agent agent, WeaponComponentData weapon)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(1f);
		SkillObject skillObject = weapon?.RelevantSkill;
		if (agent.Character is CharacterObject && skillObject != null)
		{
			if (skillObject == DefaultSkills.OneHanded)
			{
				int effectiveSkill = GetEffectiveSkill(agent, skillObject);
				SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.OneHandedDamage, ref explainedNumber, effectiveSkill);
			}
			else if (skillObject == DefaultSkills.TwoHanded)
			{
				int effectiveSkill2 = GetEffectiveSkill(agent, skillObject);
				SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.TwoHandedDamage, ref explainedNumber, effectiveSkill2);
			}
			else if (skillObject == DefaultSkills.Polearm)
			{
				int effectiveSkill3 = GetEffectiveSkill(agent, skillObject);
				SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.PolearmDamage, ref explainedNumber, effectiveSkill3);
			}
			else if (skillObject == DefaultSkills.Bow)
			{
				int effectiveSkill4 = GetEffectiveSkill(agent, skillObject);
				SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.BowDamage, ref explainedNumber, effectiveSkill4);
			}
			else if (skillObject == DefaultSkills.Throwing)
			{
				int effectiveSkill5 = GetEffectiveSkill(agent, skillObject);
				SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.ThrowingDamage, ref explainedNumber, effectiveSkill5);
			}
		}
		return Math.Max(0f, explainedNumber.ResultNumber);
	}

	private float GetArmorStealthBonus(EquipmentElement armorElement, int maxBodyPartBonus)
	{
		if (!armorElement.IsEmpty && armorElement.Item != null && armorElement.Item.HasArmorComponent)
		{
			return armorElement.GetModifiedStealthFactor();
		}
		return 0f;
	}

	public override float GetEquipmentStealthBonus(Agent agent)
	{
		Equipment spawnEquipment = agent.SpawnEquipment;
		return 0f + GetArmorStealthBonus(spawnEquipment[EquipmentIndex.NumAllWeaponSlots], 25) + GetArmorStealthBonus(spawnEquipment[EquipmentIndex.Cape], 15) + GetArmorStealthBonus(spawnEquipment[EquipmentIndex.Body], 30) + GetArmorStealthBonus(spawnEquipment[EquipmentIndex.Gloves], 10) + GetArmorStealthBonus(spawnEquipment[EquipmentIndex.Leg], 20);
	}

	public override float GetSneakAttackMultiplier(Agent agent, WeaponComponentData weapon)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(1f);
		if (weapon != null && agent.Character is CharacterObject)
		{
			int effectiveSkill = GetEffectiveSkill(agent, DefaultSkills.Roguery);
			SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.SneakDamage, ref explainedNumber, effectiveSkill);
			if ((weapon != null && weapon.WeaponClass == WeaponClass.Dagger) || (weapon != null && weapon.WeaponClass == WeaponClass.ThrowingKnife))
			{
				explainedNumber.AddFactor(2f);
			}
		}
		return explainedNumber.ResultNumber;
	}

	public override float GetKnockBackResistance(Agent agent)
	{
		if (agent.IsHuman)
		{
			int effectiveSkill = GetEffectiveSkill(agent, DefaultSkills.Athletics);
			return DefaultSkillEffects.KnockBackResistance.GetSkillEffectValue(effectiveSkill);
		}
		return float.MaxValue;
	}

	public override float GetKnockDownResistance(Agent agent, StrikeType strikeType = StrikeType.Invalid)
	{
		if (agent.IsHuman)
		{
			int effectiveSkill = GetEffectiveSkill(agent, DefaultSkills.Athletics);
			float num = DefaultSkillEffects.KnockDownResistance.GetSkillEffectValue(effectiveSkill);
			if (agent.HasMount)
			{
				num += 0.1f;
			}
			else if (strikeType == StrikeType.Thrust)
			{
				num += 0.15f;
			}
			return num;
		}
		return float.MaxValue;
	}

	public override float GetDismountResistance(Agent agent)
	{
		if (agent.IsHuman)
		{
			int effectiveSkill = GetEffectiveSkill(agent, DefaultSkills.Riding);
			return DefaultSkillEffects.DismountResistance.GetSkillEffectValue(effectiveSkill);
		}
		return float.MaxValue;
	}

	public override float GetBreatheHoldMaxDuration(Agent agent, float baseBreatheHoldMaxDuration)
	{
		return baseBreatheHoldMaxDuration;
	}

	public override float GetWeaponInaccuracy(Agent agent, WeaponComponentData weapon, int weaponSkill)
	{
		CharacterObject characterObject = agent.Character as CharacterObject;
		CharacterObject characterObject2 = agent.Formation?.Captain?.Character as CharacterObject;
		if (characterObject == characterObject2)
		{
			characterObject2 = null;
		}
		float a = 0f;
		if (weapon.IsRangedWeapon)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(1f);
			if (characterObject != null)
			{
				if (weapon.RelevantSkill == DefaultSkills.Bow)
				{
					SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.BowAccuracy, ref explainedNumber, weaponSkill);
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Bow.QuickAdjustments, characterObject2, ref explainedNumber);
				}
				else if (weapon.RelevantSkill == DefaultSkills.Crossbow)
				{
					SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.CrossbowAccuracy, ref explainedNumber, weaponSkill);
				}
				else if (weapon.RelevantSkill == DefaultSkills.Throwing)
				{
					if (weapon.WeaponClass == WeaponClass.Sling)
					{
						explainedNumber = new ExplainedNumber(TaleWorlds.Library.MathF.Max(1f - 0.007f * (float)weaponSkill, 0.2f));
					}
					else
					{
						SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.ThrowingAccuracy, ref explainedNumber, weaponSkill);
					}
				}
			}
			a = (100f - (float)weapon.Accuracy) * explainedNumber.ResultNumber * 0.001f;
		}
		else if (weapon.WeaponFlags.HasAllFlags(WeaponFlags.WideGrip))
		{
			a = 1f - (float)weaponSkill * 0.01f;
		}
		return TaleWorlds.Library.MathF.Max(a, 0f);
	}

	public override float GetInteractionDistance(Agent agent)
	{
		if (agent.HasMount && agent.Character is CharacterObject characterObject && characterObject.GetPerkValue(DefaultPerks.Throwing.LongReach))
		{
			return 3f;
		}
		return base.GetInteractionDistance(agent);
	}

	public override float GetMaxCameraZoom(Agent agent)
	{
		CharacterObject characterObject = agent.Character as CharacterObject;
		ExplainedNumber bonuses = new ExplainedNumber(1f);
		if (characterObject != null)
		{
			MissionEquipment equipment = agent.Equipment;
			EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
			WeaponComponentData weaponComponentData = ((primaryWieldedItemIndex != EquipmentIndex.None) ? equipment[primaryWieldedItemIndex].CurrentUsageItem : null);
			if (weaponComponentData != null)
			{
				if (weaponComponentData.RelevantSkill == DefaultSkills.Bow)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.EagleEye, characterObject, isPrimaryBonus: true, ref bonuses);
				}
				else if (weaponComponentData.RelevantSkill == DefaultSkills.Crossbow)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Crossbow.LongShots, characterObject, isPrimaryBonus: true, ref bonuses);
				}
				else if (weaponComponentData.RelevantSkill == DefaultSkills.Throwing)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.Focus, characterObject, isPrimaryBonus: true, ref bonuses);
				}
			}
		}
		return bonuses.ResultNumber;
	}

	public List<PerkObject> GetPerksOfAgent(CharacterObject agentCharacter, SkillObject skill = null, bool filterPartyRole = false, PartyRole partyRole = PartyRole.Personal)
	{
		List<PerkObject> list = new List<PerkObject>();
		if (agentCharacter != null)
		{
			foreach (PerkObject item in PerkObject.All)
			{
				if (!agentCharacter.GetPerkValue(item) || (skill != null && skill != item.Skill))
				{
					continue;
				}
				if (filterPartyRole)
				{
					if (item.PrimaryRole == partyRole || item.SecondaryRole == partyRole)
					{
						list.Add(item);
					}
				}
				else
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	public override string GetMissionDebugInfoForAgent(Agent agent)
	{
		string text = "";
		text += "Base: Initial stats modified only by skills\n";
		text += "Effective (Eff): Stats that are modified by perks & mission effects\n\n";
		string text2 = "{0,-20}";
		text = text + string.Format(text2, "Name") + ": " + agent.Name + "\n";
		text = text + string.Format(text2, "Age") + ": " + (int)agent.Age + "\n";
		text = text + string.Format(text2, "Health") + ": " + agent.Health + "\n";
		int num = (agent.IsHuman ? agent.Character.MaxHitPoints() : agent.Monster.HitPoints);
		text = text + string.Format(text2, "Max.Health") + ": " + num + "(Base)\n";
		text = text + string.Format(text2, "") + "  " + MissionGameModels.Current.AgentStatCalculateModel.GetEffectiveMaxHealth(agent) + "(Eff)\n";
		text = text + string.Format(text2, "Team") + ": " + ((agent.Team == null) ? "N/A" : (agent.Team.IsAttacker ? "Attacker" : "Defender")) + "\n";
		if (agent.IsHuman)
		{
			string format = text2 + ": {1,4:G}, {2,4:G}";
			text += "-------------------------------------\n";
			text = text + string.Format(text2 + ": {1,4}, {2,4}", "Skills", "Base", "Eff") + "\n";
			text += "-------------------------------------\n";
			foreach (SkillObject item in Skills.All)
			{
				int skillValue = agent.Character.GetSkillValue(item);
				int effectiveSkill = MissionGameModels.Current.AgentStatCalculateModel.GetEffectiveSkill(agent, item);
				string text3 = string.Format(format, item.Name, skillValue, effectiveSkill);
				text = text + text3 + "\n";
			}
			text += "-------------------------------------\n";
			CharacterObject agentCharacter = agent.Character as CharacterObject;
			string debugPerkInfoForAgent = GetDebugPerkInfoForAgent(agentCharacter);
			if (debugPerkInfoForAgent.Length > 0)
			{
				text = text + string.Format(text2 + ": ", "Perks") + "\n";
				text += "-------------------------------------\n";
				text += debugPerkInfoForAgent;
				text += "-------------------------------------\n";
			}
			CharacterObject characterObject = agent.Formation?.Captain?.Character as CharacterObject;
			string debugPerkInfoForAgent2 = GetDebugPerkInfoForAgent(characterObject, filterPartyRole: true, PartyRole.Captain);
			if (debugPerkInfoForAgent2.Length > 0)
			{
				text = string.Concat(text, string.Format(text2 + ": ", "Captain Perks"), characterObject.Name, "\n");
				text += "-------------------------------------\n";
				text += debugPerkInfoForAgent2;
				text += "-------------------------------------\n";
			}
			CharacterObject visualPartyLeader = PartyBaseHelper.GetVisualPartyLeader(((PartyBase)(agent.Origin?.BattleCombatant))?.MobileParty?.Party);
			string debugPerkInfoForAgent3 = GetDebugPerkInfoForAgent(visualPartyLeader, filterPartyRole: true, PartyRole.PartyLeader);
			if (debugPerkInfoForAgent3.Length > 0)
			{
				text = string.Concat(text, string.Format(text2 + ": ", "Party Leader Perks"), visualPartyLeader.Name, "\n");
				text += "-------------------------------------\n";
				text += debugPerkInfoForAgent3;
				text += "-------------------------------------\n";
			}
		}
		return text;
	}

	public override float GetEffectiveArmorEncumbrance(Agent agent, Equipment equipment)
	{
		float totalWeightOfArmor = equipment.GetTotalWeightOfArmor(agent.IsHuman);
		float num = 1f;
		if (agent.Character is CharacterObject characterObject && characterObject.GetPerkValue(DefaultPerks.Athletics.FormFittingArmor))
		{
			num += DefaultPerks.Athletics.FormFittingArmor.PrimaryBonus;
		}
		return TaleWorlds.Library.MathF.Max(0f, totalWeightOfArmor * num);
	}

	public override float GetEffectiveMaxHealth(Agent agent)
	{
		if (agent.IsHero)
		{
			return agent.Character.MaxHitPoints();
		}
		float baseHealthLimit = agent.BaseHealthLimit;
		ExplainedNumber stat = new ExplainedNumber(baseHealthLimit);
		if (agent.IsHuman)
		{
			CharacterObject characterObject = agent.Character as CharacterObject;
			MobileParty mobileParty = ((PartyBase)((agent?.Origin)?.BattleCombatant))?.MobileParty;
			CharacterObject characterObject2 = mobileParty?.LeaderHero?.CharacterObject;
			if (characterObject != null && characterObject2 != null)
			{
				if (!mobileParty.IsCurrentlyAtSea)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.TwoHanded.ThickHides, mobileParty, isPrimaryBonus: false, ref stat);
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Polearm.HardyFrontline, mobileParty, isPrimaryBonus: true, ref stat);
				}
				if (characterObject.IsRanged)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Crossbow.PickedShots, mobileParty, isPrimaryBonus: false, ref stat);
				}
				if (!agent.HasMount)
				{
					if (!mobileParty.IsCurrentlyAtSea)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Athletics.WellBuilt, mobileParty, isPrimaryBonus: false, ref stat);
					}
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Polearm.HardKnock, mobileParty, isPrimaryBonus: false, ref stat);
					if (!mobileParty.IsCurrentlyAtSea && characterObject.IsInfantry)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.OneHanded.UnwaveringDefense, mobileParty, isPrimaryBonus: false, ref stat);
					}
				}
				if (characterObject2.GetPerkValue(DefaultPerks.Medicine.MinisterOfHealth))
				{
					int num = (int)((float)TaleWorlds.Library.MathF.Max(characterObject2.GetSkillValue(DefaultSkills.Medicine) - Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus, 0) * DefaultPerks.Medicine.MinisterOfHealth.PrimaryBonus);
					if (num > 0)
					{
						stat.Add(num);
					}
				}
			}
		}
		else
		{
			Agent riderAgent = agent.RiderAgent;
			if (riderAgent != null)
			{
				CharacterObject character = riderAgent?.Character as CharacterObject;
				MobileParty party = ((PartyBase)(riderAgent?.Origin?.BattleCombatant))?.MobileParty;
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Medicine.Sledges, party, isPrimaryBonus: false, ref stat);
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Riding.Veterinary, character, isPrimaryBonus: true, ref stat);
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Riding.Veterinary, party, isPrimaryBonus: false, ref stat);
			}
		}
		return stat.ResultNumber;
	}

	public override float GetEnvironmentSpeedFactor(Agent agent)
	{
		_ = agent.Mission.Scene;
		float num = 1f;
		if (!agent.Mission.Scene.IsAtmosphereIndoor)
		{
			if (agent.Mission.Scene.GetRainDensity() > 0f)
			{
				num *= 0.9f;
			}
			if (!agent.IsHuman && CampaignTime.Now.IsNightTime)
			{
				num *= 0.9f;
			}
		}
		return num;
	}

	private string GetDebugPerkInfoForAgent(CharacterObject agentCharacter, bool filterPartyRole = false, PartyRole partyRole = PartyRole.Personal)
	{
		string text = "";
		string format = "{0,-18}";
		if (GetPerksOfAgent(agentCharacter, null, filterPartyRole, partyRole).Count > 0)
		{
			foreach (SkillObject item in Skills.All)
			{
				List<PerkObject> perksOfAgent = GetPerksOfAgent(agentCharacter, item, filterPartyRole, partyRole);
				if (perksOfAgent == null || perksOfAgent.Count <= 0)
				{
					continue;
				}
				string text2 = string.Format(format, item.Name) + ": ";
				int num = 5;
				int num2 = 0;
				foreach (PerkObject item2 in perksOfAgent)
				{
					string text3 = item2.Name.ToString();
					if (num2 == num)
					{
						text2 = text2 + "\n" + string.Format(format, "") + "  ";
						num2 = 0;
					}
					text2 = text2 + text3 + ", ";
					num2++;
				}
				text2 = text2.Remove(text2.LastIndexOf(","));
				text = text + text2 + "\n";
			}
		}
		return text;
	}

	private void UpdateHumanStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
	{
		Equipment spawnEquipment = agent.SpawnEquipment;
		agentDrivenProperties.ArmorHead = spawnEquipment.GetHeadArmorSum();
		agentDrivenProperties.ArmorTorso = spawnEquipment.GetHumanBodyArmorSum();
		agentDrivenProperties.ArmorLegs = spawnEquipment.GetLegArmorSum();
		agentDrivenProperties.ArmorArms = spawnEquipment.GetArmArmorSum();
		BasicCharacterObject character = agent.Character;
		CharacterObject characterObject = character as CharacterObject;
		MissionEquipment equipment = agent.Equipment;
		float num = equipment.GetTotalWeightOfWeapons();
		float effectiveArmorEncumbrance = GetEffectiveArmorEncumbrance(agent, spawnEquipment);
		int weight = agent.Monster.Weight;
		EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
		EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
		if (primaryWieldedItemIndex != EquipmentIndex.None)
		{
			ItemObject item = equipment[primaryWieldedItemIndex].Item;
			WeaponComponent weaponComponent = item.WeaponComponent;
			if (weaponComponent != null)
			{
				ItemObject.ItemTypeEnum itemType = weaponComponent.GetItemType();
				bool flag = false;
				if (characterObject != null)
				{
					bool flag2 = itemType == ItemObject.ItemTypeEnum.Bow && characterObject.GetPerkValue(DefaultPerks.Bow.RangersSwiftness);
					bool flag3 = itemType == ItemObject.ItemTypeEnum.Crossbow && characterObject.GetPerkValue(DefaultPerks.Crossbow.LooseAndMove);
					flag = flag2 || flag3;
				}
				if (!flag)
				{
					float realWeaponLength = weaponComponent.PrimaryWeapon.GetRealWeaponLength();
					num += 4f * TaleWorlds.Library.MathF.Sqrt(realWeaponLength) * item.Weight;
				}
			}
		}
		if (offhandWieldedItemIndex != EquipmentIndex.None)
		{
			ItemObject item2 = equipment[offhandWieldedItemIndex].Item;
			WeaponComponentData primaryWeapon = item2.PrimaryWeapon;
			if (primaryWeapon != null && primaryWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.CanBlockRanged) && (characterObject == null || !characterObject.GetPerkValue(DefaultPerks.OneHanded.ShieldBearer)))
			{
				num += 1.5f * item2.Weight;
			}
		}
		agentDrivenProperties.WeaponExternalAccelerationAccuracyPenalty = 0f;
		agentDrivenProperties.WeaponsEncumbrance = num;
		agentDrivenProperties.ArmorEncumbrance = effectiveArmorEncumbrance;
		float num2 = effectiveArmorEncumbrance + num;
		EquipmentIndex primaryWieldedItemIndex2 = agent.GetPrimaryWieldedItemIndex();
		WeaponComponentData weaponComponentData = ((primaryWieldedItemIndex2 != EquipmentIndex.None) ? equipment[primaryWieldedItemIndex2].CurrentUsageItem : null);
		EquipmentIndex offhandWieldedItemIndex2 = agent.GetOffhandWieldedItemIndex();
		WeaponComponentData secondaryItem = ((offhandWieldedItemIndex2 != EquipmentIndex.None) ? equipment[offhandWieldedItemIndex2].CurrentUsageItem : null);
		agentDrivenProperties.SwingSpeedMultiplier = 0.93f;
		agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier = 0.93f;
		agentDrivenProperties.HandlingMultiplier = 1f;
		agentDrivenProperties.ShieldBashStunDurationMultiplier = 1f;
		agentDrivenProperties.KickStunDurationMultiplier = 1f;
		agentDrivenProperties.ReloadSpeed = 0.93f;
		agentDrivenProperties.MissileSpeedMultiplier = 1f;
		agentDrivenProperties.ReloadMovementPenaltyFactor = 1f;
		agentDrivenProperties.DamageMultiplierBonus = 0f;
		SetAllWeaponInaccuracy(agent, agentDrivenProperties, (int)primaryWieldedItemIndex2, weaponComponentData);
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
					agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = TaleWorlds.Library.MathF.Max(0f, 0.125f * num3);
					agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = TaleWorlds.Library.MathF.Max(0f, 0.1f * num3);
				}
				else
				{
					float num4 = TaleWorlds.Library.MathF.Max(0f, (1f - (float)effectiveSkillForWeapon / 500f) * (1f - (float)effectiveSkill2 / 1800f));
					agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = TaleWorlds.Library.MathF.Max(0f, 0.025f * num4);
					agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = TaleWorlds.Library.MathF.Max(0f, 0.12f * num4);
				}
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
				if (weaponComponentData2.WeaponClass == WeaponClass.Bow)
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
				}
				else if (weaponComponentData2.WeaponClass == WeaponClass.Javelin || weaponComponentData2.WeaponClass == WeaponClass.ThrowingAxe || weaponComponentData2.WeaponClass == WeaponClass.ThrowingKnife)
				{
					agentDrivenProperties.WeaponBestAccuracyWaitTime = 0.2f + (89f - (float)thrustSpeed) * 0.009f;
					agentDrivenProperties.WeaponUnsteadyBeginTime = 2.5f + (float)effectiveSkillForWeapon * 0.01f;
					agentDrivenProperties.WeaponUnsteadyEndTime = 10f + agentDrivenProperties.WeaponUnsteadyBeginTime;
					agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.025f;
				}
				else if (weaponComponentData2.WeaponClass == WeaponClass.Sling)
				{
					agentDrivenProperties.WeaponBestAccuracyWaitTime = 2.6f + (89f - (float)thrustSpeed) * 0.12f;
					agentDrivenProperties.WeaponUnsteadyBeginTime = 3f + (float)effectiveSkillForWeapon * 0.064f;
					agentDrivenProperties.WeaponUnsteadyEndTime = 22f + agentDrivenProperties.WeaponUnsteadyBeginTime;
					agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.2f;
				}
				else
				{
					agentDrivenProperties.WeaponBestAccuracyWaitTime = 0.1f;
					agentDrivenProperties.WeaponUnsteadyBeginTime = 0f;
					agentDrivenProperties.WeaponUnsteadyEndTime = 0f;
					agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.1f;
				}
			}
			else if (weaponComponentData2.WeaponFlags.HasAllFlags(WeaponFlags.WideGrip))
			{
				agentDrivenProperties.WeaponUnsteadyBeginTime = 1f + (float)effectiveSkillForWeapon * 0.005f;
				agentDrivenProperties.WeaponUnsteadyEndTime = 3f + (float)effectiveSkillForWeapon * 0.01f;
			}
		}
		agentDrivenProperties.TopSpeedReachDuration = 2.5f + TaleWorlds.Library.MathF.Max(5f - (1f + (float)effectiveSkill * 0.01f), 1f) / 3.5f - TaleWorlds.Library.MathF.Min((float)weight / ((float)weight + num2), 0.8f);
		ExplainedNumber explainedNumber = new ExplainedNumber(0.7f);
		ExplainedNumber explainedNumber2 = new ExplainedNumber(0.7f);
		SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.AthleticsSpeedFactor, ref explainedNumber, effectiveSkill);
		SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.AthleticsSpeedFactor, ref explainedNumber2, 300);
		ExplainedNumber explainedNumber3 = new ExplainedNumber(0.2f);
		explainedNumber3.LimitMin(0f);
		SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.AthleticsWeightFactor, ref explainedNumber3, effectiveSkill);
		float num5 = explainedNumber3.ResultNumber * num2 / (float)weight;
		float num6 = MBMath.ClampFloat(explainedNumber.ResultNumber - num5, 0f, explainedNumber2.ResultNumber);
		agentDrivenProperties.MaxSpeedMultiplier = GetEnvironmentSpeedFactor(agent) * num6;
		float managedParameter = TaleWorlds.Core.ManagedParameters.Instance.GetManagedParameter(TaleWorlds.Core.ManagedParametersEnum.BipedalCombatSpeedMinMultiplier);
		float managedParameter2 = TaleWorlds.Core.ManagedParameters.Instance.GetManagedParameter(TaleWorlds.Core.ManagedParametersEnum.BipedalCombatSpeedMaxMultiplier);
		float amount = TaleWorlds.Library.MathF.Min(num2 / (float)weight, 1f);
		agentDrivenProperties.CombatMaxSpeedMultiplier = TaleWorlds.Library.MathF.Min(MBMath.Lerp(managedParameter2, managedParameter, amount), 1f);
		agentDrivenProperties.CrouchedSpeedMultiplier = 1f;
		agentDrivenProperties.AttributeShieldMissileCollisionBodySizeAdder = 0.3f;
		float num7 = agent.MountAgent?.GetAgentDrivenPropertyValue(DrivenProperty.AttributeRiding) ?? 1f;
		agentDrivenProperties.AttributeRiding = (float)effectiveSkill2 * num7;
		agentDrivenProperties.AttributeHorseArchery = MissionGameModels.Current.StrikeMagnitudeModel.CalculateHorseArcheryFactor(character);
		agentDrivenProperties.BipedalRangedReadySpeedMultiplier = TaleWorlds.Core.ManagedParameters.Instance.GetManagedParameter(TaleWorlds.Core.ManagedParametersEnum.BipedalRangedReadySpeedMultiplier);
		agentDrivenProperties.BipedalRangedReloadSpeedMultiplier = TaleWorlds.Core.ManagedParameters.Instance.GetManagedParameter(TaleWorlds.Core.ManagedParametersEnum.BipedalRangedReloadSpeedMultiplier);
		if (characterObject != null)
		{
			if (weaponComponentData != null)
			{
				SetWeaponSkillEffectsOnAgent(agent, characterObject, agentDrivenProperties, weaponComponentData);
				if (agent.HasMount)
				{
					SetMountedPenaltiesOnAgent(agent, agentDrivenProperties, weaponComponentData);
				}
			}
			SetPerkAndBannerEffectsOnAgent(agent, characterObject, agentDrivenProperties, weaponComponentData);
		}
		SetAiRelatedProperties(agent, agentDrivenProperties, weaponComponentData, secondaryItem);
		float num8 = 1f;
		if (!agent.Mission.Scene.IsAtmosphereIndoor)
		{
			float rainDensity = agent.Mission.Scene.GetRainDensity();
			float fog = agent.Mission.Scene.GetFog();
			if (rainDensity > 0f || fog > 0f)
			{
				num8 += TaleWorlds.Library.MathF.Min(0.3f, rainDensity + fog);
			}
			if (CampaignTime.Now.IsNightTime)
			{
				num8 += 0.1f;
			}
		}
		agentDrivenProperties.AiShooterError *= num8;
	}

	private void UpdateHorseStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
	{
		Equipment spawnEquipment = agent.SpawnEquipment;
		EquipmentElement mountElement = spawnEquipment[EquipmentIndex.ArmorItemEndSlot];
		ItemObject item = mountElement.Item;
		EquipmentElement harness = spawnEquipment[EquipmentIndex.HorseHarness];
		agentDrivenProperties.AiSpeciesIndex = (int)item.Id.InternalValue;
		agentDrivenProperties.AttributeRiding = 0.8f + ((harness.Item != null) ? 0.2f : 0f);
		float num = 0f;
		for (int i = 1; i < 12; i++)
		{
			if (spawnEquipment[i].Item != null)
			{
				num += (float)spawnEquipment[i].GetModifiedMountBodyArmor();
			}
		}
		agentDrivenProperties.ArmorTorso = num;
		int modifiedMountManeuver = mountElement.GetModifiedMountManeuver(in harness);
		int num2 = mountElement.GetModifiedMountSpeed(in harness) + 1;
		int num3 = 0;
		float environmentSpeedFactor = GetEnvironmentSpeedFactor(agent);
		bool flag = Campaign.Current.Models.MapWeatherModel.GetWeatherEffectOnTerrainForPosition(MobileParty.MainParty.Position.ToVec2()) == MapWeatherModel.WeatherEventEffectOnTerrain.Wet;
		Agent riderAgent = agent.RiderAgent;
		if (riderAgent != null)
		{
			CharacterObject characterObject = riderAgent.Character as CharacterObject;
			Formation formation = riderAgent.Formation;
			Agent agent2 = formation?.Captain;
			if (agent2 == riderAgent)
			{
				agent2 = null;
			}
			CharacterObject captainCharacter = agent2?.Character as CharacterObject;
			BannerComponent activeBanner = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(formation);
			ExplainedNumber explainedNumber = new ExplainedNumber(modifiedMountManeuver);
			ExplainedNumber explainedNumber2 = new ExplainedNumber(num2);
			num3 = GetEffectiveSkill(agent.RiderAgent, DefaultSkills.Riding);
			SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.HorseManeuver, ref explainedNumber, num3);
			SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.HorseSpeed, ref explainedNumber2, num3);
			if (activeBanner != null)
			{
				BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.IncreasedMountMovementSpeed, activeBanner, ref explainedNumber2);
			}
			PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Riding.NimbleSteed, characterObject, isPrimaryBonus: true, ref explainedNumber);
			PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Riding.SweepingWind, characterObject, isPrimaryBonus: true, ref explainedNumber2);
			ExplainedNumber bonuses = new ExplainedNumber(agentDrivenProperties.ArmorTorso);
			PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Riding.ToughSteed, captainCharacter, ref bonuses);
			PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Riding.ToughSteed, characterObject, isPrimaryBonus: true, ref bonuses);
			if (characterObject.GetPerkValue(DefaultPerks.Riding.TheWayOfTheSaddle))
			{
				float value = (float)TaleWorlds.Library.MathF.Max(num3 - Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus, 0) * DefaultPerks.Riding.TheWayOfTheSaddle.PrimaryBonus;
				explainedNumber.Add(value);
			}
			if (harness.Item == null)
			{
				explainedNumber.AddFactor(-0.1f);
				explainedNumber2.AddFactor(-0.1f);
			}
			if (flag)
			{
				explainedNumber2.AddFactor(-0.25f);
			}
			agentDrivenProperties.ArmorTorso = bonuses.ResultNumber;
			agentDrivenProperties.MountManeuver = explainedNumber.ResultNumber;
			agentDrivenProperties.MountSpeed = environmentSpeedFactor * 0.22f * (1f + explainedNumber2.ResultNumber);
		}
		else
		{
			agentDrivenProperties.MountManeuver = modifiedMountManeuver;
			agentDrivenProperties.MountSpeed = environmentSpeedFactor * 0.22f * (float)(1 + num2);
		}
		float num4 = mountElement.Weight / 2f + (harness.IsEmpty ? 0f : harness.Weight);
		agentDrivenProperties.MountDashAccelerationMultiplier = ((!(num4 > 200f)) ? 1f : ((num4 < 300f) ? (1f - (num4 - 200f) / 111f) : 0.1f));
		if (flag)
		{
			agentDrivenProperties.MountDashAccelerationMultiplier *= 0.75f;
		}
		agentDrivenProperties.TopSpeedReachDuration = Game.Current.BasicModels.RidingModel.CalculateAcceleration(in mountElement, in harness, num3);
		agentDrivenProperties.MountChargeDamage = (float)mountElement.GetModifiedMountCharge(in harness) * 0.004f;
		agentDrivenProperties.MountDifficulty = mountElement.Item.Difficulty;
	}

	private void SetPerkAndBannerEffectsOnAgent(Agent agent, CharacterObject agentCharacter, AgentDrivenProperties agentDrivenProperties, WeaponComponentData equippedWeaponComponent)
	{
		CharacterObject characterObject = agent.Formation?.Captain?.Character as CharacterObject;
		if (agent.Formation?.Captain == agent)
		{
			characterObject = null;
		}
		ItemObject itemObject = null;
		EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
		if (offhandWieldedItemIndex != EquipmentIndex.None)
		{
			itemObject = agent.Equipment[offhandWieldedItemIndex].Item;
		}
		BannerComponent activeBanner = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(agent.Formation);
		bool flag = equippedWeaponComponent?.IsRangedWeapon ?? false;
		bool flag2 = equippedWeaponComponent?.IsMeleeWeapon ?? false;
		bool flag3 = itemObject?.PrimaryWeapon.IsShield ?? false;
		ExplainedNumber bonuses = new ExplainedNumber(agentDrivenProperties.CombatMaxSpeedMultiplier);
		ExplainedNumber bonuses2 = new ExplainedNumber(agentDrivenProperties.MaxSpeedMultiplier);
		PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.FleetOfFoot, agentCharacter, isPrimaryBonus: true, ref bonuses);
		ExplainedNumber bonuses3 = new ExplainedNumber(agentDrivenProperties.KickStunDurationMultiplier);
		PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Roguery.DirtyFighting, agentCharacter, isPrimaryBonus: true, ref bonuses3);
		agentDrivenProperties.KickStunDurationMultiplier = bonuses3.ResultNumber;
		if (equippedWeaponComponent != null)
		{
			ExplainedNumber bonuses4 = new ExplainedNumber(agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier);
			if (flag2)
			{
				ExplainedNumber bonuses5 = new ExplainedNumber(agentDrivenProperties.SwingSpeedMultiplier);
				ExplainedNumber bonuses6 = new ExplainedNumber(agentDrivenProperties.HandlingMultiplier);
				if (!agent.HasMount)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Athletics.Fury, agentCharacter, isPrimaryBonus: true, ref bonuses6);
					if (characterObject != null)
					{
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Athletics.Fury, characterObject, ref bonuses6);
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.TwoHanded.OnTheEdge, characterObject, ref bonuses5);
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.TwoHanded.BladeMaster, characterObject, ref bonuses5);
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Polearm.SwiftSwing, characterObject, ref bonuses5);
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.TwoHanded.BladeMaster, characterObject, ref bonuses4);
					}
				}
				if (equippedWeaponComponent.RelevantSkill == DefaultSkills.OneHanded)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.SwiftStrike, agentCharacter, isPrimaryBonus: true, ref bonuses5);
					PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.OneHanded.WayOfTheSword, agentCharacter, DefaultSkills.OneHanded, applyPrimaryBonus: true, ref bonuses5, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus);
					PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.OneHanded.WayOfTheSword, agentCharacter, DefaultSkills.OneHanded, applyPrimaryBonus: true, ref bonuses4, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus);
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.WrappedHandles, agentCharacter, isPrimaryBonus: true, ref bonuses6);
				}
				else if (equippedWeaponComponent.RelevantSkill == DefaultSkills.TwoHanded)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.TwoHanded.OnTheEdge, agentCharacter, isPrimaryBonus: true, ref bonuses5);
					PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.TwoHanded.WayOfTheGreatAxe, agentCharacter, DefaultSkills.TwoHanded, applyPrimaryBonus: true, ref bonuses5, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus);
					PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.TwoHanded.WayOfTheGreatAxe, agentCharacter, DefaultSkills.TwoHanded, applyPrimaryBonus: true, ref bonuses4, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus);
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.TwoHanded.StrongGrip, agentCharacter, isPrimaryBonus: true, ref bonuses6);
				}
				else if (equippedWeaponComponent.RelevantSkill == DefaultSkills.Polearm)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Polearm.Footwork, agentCharacter, isPrimaryBonus: true, ref bonuses);
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Polearm.SwiftSwing, agentCharacter, isPrimaryBonus: true, ref bonuses5);
					PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Polearm.WayOfTheSpear, agentCharacter, DefaultSkills.Polearm, applyPrimaryBonus: true, ref bonuses5, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus);
					PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Polearm.WayOfTheSpear, agentCharacter, DefaultSkills.Polearm, applyPrimaryBonus: true, ref bonuses4, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus);
					if (equippedWeaponComponent.SwingDamageType != DamageTypes.Invalid)
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Polearm.CounterWeight, agentCharacter, isPrimaryBonus: true, ref bonuses6);
					}
				}
				agentDrivenProperties.SwingSpeedMultiplier = bonuses5.ResultNumber;
				agentDrivenProperties.HandlingMultiplier = bonuses6.ResultNumber;
			}
			if (flag)
			{
				ExplainedNumber bonuses7 = new ExplainedNumber(agentDrivenProperties.WeaponInaccuracy);
				ExplainedNumber bonuses8 = new ExplainedNumber(agentDrivenProperties.WeaponMaxMovementAccuracyPenalty);
				ExplainedNumber bonuses9 = new ExplainedNumber(agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty);
				ExplainedNumber bonuses10 = new ExplainedNumber(agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians);
				ExplainedNumber bonuses11 = new ExplainedNumber(agentDrivenProperties.WeaponUnsteadyBeginTime);
				ExplainedNumber bonuses12 = new ExplainedNumber(agentDrivenProperties.WeaponUnsteadyEndTime);
				ExplainedNumber bonuses13 = new ExplainedNumber(agentDrivenProperties.ReloadMovementPenaltyFactor);
				ExplainedNumber bonuses14 = new ExplainedNumber(agentDrivenProperties.ReloadSpeed);
				ExplainedNumber bonuses15 = new ExplainedNumber(agentDrivenProperties.MissileSpeedMultiplier);
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.NockingPoint, agentCharacter, isPrimaryBonus: true, ref bonuses13);
				if (characterObject != null)
				{
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Crossbow.LooseAndMove, characterObject, ref bonuses2);
				}
				if (activeBanner != null)
				{
					BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.DecreasedRangedAccuracyPenalty, activeBanner, ref bonuses7);
				}
				if (agent.HasMount)
				{
					if (agentCharacter.GetPerkValue(DefaultPerks.Riding.Sagittarius))
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Riding.Sagittarius, agentCharacter, isPrimaryBonus: true, ref bonuses8);
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Riding.Sagittarius, agentCharacter, isPrimaryBonus: true, ref bonuses9);
					}
					if (characterObject != null && characterObject.GetPerkValue(DefaultPerks.Riding.Sagittarius))
					{
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Riding.Sagittarius, characterObject, ref bonuses8);
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Riding.Sagittarius, characterObject, ref bonuses9);
					}
					if (equippedWeaponComponent.RelevantSkill == DefaultSkills.Bow && agentCharacter.GetPerkValue(DefaultPerks.Bow.MountedArchery))
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.MountedArchery, agentCharacter, isPrimaryBonus: true, ref bonuses8);
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.MountedArchery, agentCharacter, isPrimaryBonus: true, ref bonuses9);
					}
					if (equippedWeaponComponent.RelevantSkill == DefaultSkills.Throwing && agentCharacter.GetPerkValue(DefaultPerks.Throwing.MountedSkirmisher))
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.MountedSkirmisher, agentCharacter, isPrimaryBonus: true, ref bonuses8);
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.MountedSkirmisher, agentCharacter, isPrimaryBonus: true, ref bonuses9);
					}
				}
				bool flag4 = false;
				if (equippedWeaponComponent.RelevantSkill == DefaultSkills.Bow)
				{
					flag4 = true;
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.BowControl, agentCharacter, isPrimaryBonus: true, ref bonuses8);
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.RapidFire, agentCharacter, isPrimaryBonus: true, ref bonuses14);
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.QuickAdjustments, agentCharacter, isPrimaryBonus: true, ref bonuses10);
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.Discipline, agentCharacter, isPrimaryBonus: true, ref bonuses11);
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.Discipline, agentCharacter, isPrimaryBonus: true, ref bonuses12);
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.QuickDraw, agentCharacter, isPrimaryBonus: true, ref bonuses4);
					if (characterObject != null)
					{
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Bow.RapidFire, characterObject, ref bonuses14);
						if (!agent.HasMount)
						{
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Bow.NockingPoint, characterObject, ref bonuses2);
						}
					}
					PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Bow.Deadshot, agentCharacter, DefaultSkills.Bow, applyPrimaryBonus: true, ref bonuses14, Campaign.Current.Models.CharacterDevelopmentModel.MinSkillRequiredForEpicPerkBonus);
				}
				else if (equippedWeaponComponent.RelevantSkill == DefaultSkills.Crossbow)
				{
					flag4 = true;
					if (agent.HasMount)
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Crossbow.Steady, agentCharacter, isPrimaryBonus: true, ref bonuses8);
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Crossbow.Steady, agentCharacter, isPrimaryBonus: true, ref bonuses10);
					}
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Crossbow.WindWinder, agentCharacter, isPrimaryBonus: true, ref bonuses14);
					if (characterObject != null)
					{
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Crossbow.WindWinder, characterObject, ref bonuses14);
					}
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Crossbow.DonkeysSwiftness, agentCharacter, isPrimaryBonus: true, ref bonuses8);
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Crossbow.Marksmen, agentCharacter, isPrimaryBonus: true, ref bonuses4);
					PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Crossbow.MightyPull, agentCharacter, DefaultSkills.Crossbow, applyPrimaryBonus: true, ref bonuses14, Campaign.Current.Models.CharacterDevelopmentModel.MinSkillRequiredForEpicPerkBonus);
				}
				else if (equippedWeaponComponent.RelevantSkill == DefaultSkills.Throwing)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.QuickDraw, agentCharacter, isPrimaryBonus: true, ref bonuses14);
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.PerfectTechnique, agentCharacter, isPrimaryBonus: true, ref bonuses15);
					if (characterObject != null)
					{
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Throwing.QuickDraw, characterObject, ref bonuses14);
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Throwing.PerfectTechnique, characterObject, ref bonuses15);
					}
					PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Throwing.UnstoppableForce, agentCharacter, DefaultSkills.Throwing, applyPrimaryBonus: true, ref bonuses15, Campaign.Current.Models.CharacterDevelopmentModel.MinSkillRequiredForEpicPerkBonus);
				}
				if (flag4 && Campaign.Current.Models.MapWeatherModel.GetWeatherEffectOnTerrainForPosition(MobileParty.MainParty.Position.ToVec2()) == MapWeatherModel.WeatherEventEffectOnTerrain.Wet)
				{
					bonuses15.AddFactor(-0.2f);
				}
				agentDrivenProperties.ReloadMovementPenaltyFactor = bonuses13.ResultNumber;
				agentDrivenProperties.ReloadSpeed = bonuses14.ResultNumber;
				agentDrivenProperties.MissileSpeedMultiplier = bonuses15.ResultNumber;
				agentDrivenProperties.WeaponInaccuracy = bonuses7.ResultNumber;
				agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = bonuses8.ResultNumber;
				agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = bonuses9.ResultNumber;
				agentDrivenProperties.WeaponUnsteadyBeginTime = bonuses11.ResultNumber;
				agentDrivenProperties.WeaponUnsteadyEndTime = bonuses12.ResultNumber;
				agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = bonuses10.ResultNumber;
			}
			agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier = bonuses4.ResultNumber;
		}
		if (flag3)
		{
			ExplainedNumber bonuses16 = new ExplainedNumber(agentDrivenProperties.AttributeShieldMissileCollisionBodySizeAdder);
			if (characterObject != null)
			{
				Formation formation = agent.Formation;
				if (formation != null && formation.ArrangementOrder.OrderEnum == ArrangementOrder.ArrangementOrderEnum.ShieldWall)
				{
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.OneHanded.ShieldWall, characterObject, ref bonuses16);
				}
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.OneHanded.ArrowCatcher, characterObject, ref bonuses16);
			}
			PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.ArrowCatcher, agentCharacter, isPrimaryBonus: true, ref bonuses16);
			agentDrivenProperties.AttributeShieldMissileCollisionBodySizeAdder = bonuses16.ResultNumber;
			ExplainedNumber bonuses17 = new ExplainedNumber(agentDrivenProperties.ShieldBashStunDurationMultiplier);
			PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.Basher, agentCharacter, isPrimaryBonus: true, ref bonuses17);
			agentDrivenProperties.ShieldBashStunDurationMultiplier = bonuses17.ResultNumber;
		}
		else
		{
			PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Athletics.MorningExercise, agentCharacter, isPrimaryBonus: true, ref bonuses2);
			PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Medicine.SelfMedication, agentCharacter, isPrimaryBonus: false, ref bonuses2);
			if (!(flag3 || flag))
			{
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Athletics.Sprint, agentCharacter, isPrimaryBonus: true, ref bonuses2);
			}
			if (equippedWeaponComponent == null && itemObject == null)
			{
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Roguery.FleetFooted, agentCharacter, isPrimaryBonus: true, ref bonuses2);
			}
			if (characterObject != null)
			{
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Athletics.MorningExercise, characterObject, ref bonuses2);
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.OneHanded.ShieldBearer, characterObject, ref bonuses2);
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.OneHanded.FleetOfFoot, characterObject, ref bonuses2);
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.TwoHanded.RecklessCharge, characterObject, ref bonuses2);
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Polearm.Footwork, characterObject, ref bonuses2);
				if (agentCharacter.Tier >= 3)
				{
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Athletics.FormFittingArmor, characterObject, ref bonuses2);
				}
				if (agentCharacter.IsInfantry)
				{
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Athletics.Sprint, characterObject, ref bonuses2);
				}
			}
		}
		float num = 0f;
		float num2 = 0f;
		bool flag5 = false;
		if (characterObject != null)
		{
			if (agent.HasMount && characterObject.GetPerkValue(DefaultPerks.Riding.DauntlessSteed))
			{
				num += DefaultPerks.Riding.DauntlessSteed.SecondaryBonus;
				flag5 = true;
			}
			else if (!agent.HasMount && characterObject.GetPerkValue(DefaultPerks.Athletics.IgnorePain))
			{
				num += DefaultPerks.Athletics.IgnorePain.SecondaryBonus;
				flag5 = true;
			}
			if (characterObject.GetPerkValue(DefaultPerks.Engineering.Metallurgy))
			{
				num += DefaultPerks.Engineering.Metallurgy.SecondaryBonus;
				flag5 = true;
			}
		}
		if (!agent.HasMount && agentCharacter.GetPerkValue(DefaultPerks.Athletics.IgnorePain))
		{
			num2 += DefaultPerks.Athletics.IgnorePain.PrimaryBonus;
			flag5 = true;
		}
		if (flag5)
		{
			float num3 = 1f + num2;
			agentDrivenProperties.ArmorHead = TaleWorlds.Library.MathF.Max(0f, (agentDrivenProperties.ArmorHead + num) * num3);
			agentDrivenProperties.ArmorTorso = TaleWorlds.Library.MathF.Max(0f, (agentDrivenProperties.ArmorTorso + num) * num3);
			agentDrivenProperties.ArmorArms = TaleWorlds.Library.MathF.Max(0f, (agentDrivenProperties.ArmorArms + num) * num3);
			agentDrivenProperties.ArmorLegs = TaleWorlds.Library.MathF.Max(0f, (agentDrivenProperties.ArmorLegs + num) * num3);
		}
		if (Mission.Current != null && Mission.Current.HasValidTerrainType)
		{
			switch (Mission.Current.TerrainType)
			{
			case TerrainType.Snow:
			case TerrainType.Forest:
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Tactics.ExtendedSkirmish, characterObject, ref bonuses2);
				break;
			case TerrainType.Plain:
			case TerrainType.Desert:
			case TerrainType.Steppe:
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Tactics.DecisiveBattle, characterObject, ref bonuses2);
				break;
			}
		}
		if (agentCharacter.Tier >= 3 && agentCharacter.IsInfantry)
		{
			PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Athletics.FormFittingArmor, characterObject, ref bonuses2);
		}
		if (agent.Formation != null && agent.Formation.CountOfUnits <= 15)
		{
			PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Tactics.SmallUnitTactics, characterObject, ref bonuses2);
		}
		if (activeBanner != null)
		{
			BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.IncreasedTroopMovementSpeed, activeBanner, ref bonuses2);
		}
		agentDrivenProperties.MaxSpeedMultiplier = bonuses2.ResultNumber;
		agentDrivenProperties.CombatMaxSpeedMultiplier = bonuses.ResultNumber;
	}

	private void SetWeaponSkillEffectsOnAgent(Agent agent, CharacterObject agentCharacter, AgentDrivenProperties agentDrivenProperties, WeaponComponentData equippedWeaponComponent)
	{
		if (equippedWeaponComponent != null)
		{
			int effectiveSkill = GetEffectiveSkill(agent, equippedWeaponComponent.RelevantSkill);
			ExplainedNumber explainedNumber = new ExplainedNumber(agentDrivenProperties.SwingSpeedMultiplier);
			ExplainedNumber explainedNumber2 = new ExplainedNumber(agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier);
			ExplainedNumber explainedNumber3 = new ExplainedNumber(agentDrivenProperties.ReloadSpeed);
			if (equippedWeaponComponent.RelevantSkill == DefaultSkills.OneHanded)
			{
				SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.OneHandedSpeed, ref explainedNumber, effectiveSkill);
				SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.OneHandedSpeed, ref explainedNumber2, effectiveSkill);
			}
			else if (equippedWeaponComponent.RelevantSkill == DefaultSkills.TwoHanded)
			{
				SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.TwoHandedSpeed, ref explainedNumber, effectiveSkill);
				SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.TwoHandedSpeed, ref explainedNumber2, effectiveSkill);
			}
			else if (equippedWeaponComponent.RelevantSkill == DefaultSkills.Polearm)
			{
				SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.PolearmSpeed, ref explainedNumber, effectiveSkill);
				SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.PolearmSpeed, ref explainedNumber2, effectiveSkill);
			}
			else if (equippedWeaponComponent.RelevantSkill == DefaultSkills.Crossbow)
			{
				SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.CrossbowReloadSpeed, ref explainedNumber3, effectiveSkill);
			}
			else if (equippedWeaponComponent.RelevantSkill == DefaultSkills.Throwing)
			{
				SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.ThrowingSpeed, ref explainedNumber2, effectiveSkill);
			}
			agentDrivenProperties.SwingSpeedMultiplier = explainedNumber.ResultNumber;
			agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier = explainedNumber2.ResultNumber;
			agentDrivenProperties.ReloadSpeed = explainedNumber3.ResultNumber;
		}
		int effectiveSkill2 = GetEffectiveSkill(agent, DefaultSkills.Roguery);
		ExplainedNumber explainedNumber4 = new ExplainedNumber(1f);
		SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.CrouchedSpeed, ref explainedNumber4, effectiveSkill2);
		agentDrivenProperties.CrouchedSpeedMultiplier = explainedNumber4.ResultNumber;
	}

	private void SetMountedPenaltiesOnAgent(Agent agent, AgentDrivenProperties agentDrivenProperties, WeaponComponentData equippedWeaponComponent)
	{
		int effectiveSkill = GetEffectiveSkill(agent, DefaultSkills.Riding);
		float skillEffectValue = DefaultSkillEffects.MountedWeaponSpeedPenalty.GetSkillEffectValue(effectiveSkill);
		if (skillEffectValue < 0f)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(agentDrivenProperties.WeaponBestAccuracyWaitTime);
			ExplainedNumber explainedNumber2 = new ExplainedNumber(agentDrivenProperties.SwingSpeedMultiplier);
			ExplainedNumber explainedNumber3 = new ExplainedNumber(agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier);
			ExplainedNumber explainedNumber4 = new ExplainedNumber(agentDrivenProperties.ReloadSpeed);
			SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.MountedWeaponSpeedPenalty, ref explainedNumber2, effectiveSkill);
			SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.MountedWeaponSpeedPenalty, ref explainedNumber3, effectiveSkill);
			SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.MountedWeaponSpeedPenalty, ref explainedNumber4, effectiveSkill);
			explainedNumber.AddFactor(-1f * skillEffectValue);
			agentDrivenProperties.SwingSpeedMultiplier = Math.Max(0f, explainedNumber2.ResultNumber);
			agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier = Math.Max(0f, explainedNumber3.ResultNumber);
			agentDrivenProperties.ReloadSpeed = Math.Max(0f, explainedNumber4.ResultNumber);
			agentDrivenProperties.WeaponBestAccuracyWaitTime = Math.Max(0f, explainedNumber.ResultNumber);
		}
		float num = 5f - (float)effectiveSkill * 0.05f;
		if (num > 0f)
		{
			ExplainedNumber explainedNumber5 = new ExplainedNumber(agentDrivenProperties.WeaponInaccuracy);
			explainedNumber5.AddFactor(num);
			agentDrivenProperties.WeaponInaccuracy = Math.Max(0f, explainedNumber5.ResultNumber);
		}
	}

	public static float CalculateMaximumSpeedMultiplier(int athletics, float baseWeight, float totalEncumbrance)
	{
		return TaleWorlds.Library.MathF.Min((200f + (float)athletics) / 300f * (baseWeight * 2f / (baseWeight * 2f + totalEncumbrance)), 1f);
	}
}
