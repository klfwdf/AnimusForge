using Helpers;
using NavalDLC.CharacterDevelopment;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.Objects.UsableMachines;
using NavalDLC.Storyline;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace NavalDLC.GameComponents;

public class NavalAgentApplyDamageModel : AgentApplyDamageModel
{
	private const float SallyOutSiegeEngineDamageMultiplier = 4.5f;

	private NavalShipsLogic GetNavalShipsLogic()
	{
		return Mission.Current.GetMissionBehavior<NavalShipsLogic>();
	}

	public override bool IsDamageIgnored(in AttackInformation attackInformation, in AttackCollisionData collisionData)
	{
		return base.BaseModel.IsDamageIgnored(in attackInformation, in collisionData);
	}

	public override float ApplyDamageAmplifications(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
	{
		float baseNumber = base.BaseModel.ApplyDamageAmplifications(in attackInformation, in collisionData, baseDamage);
		bool isNavalBattle = Mission.Current.IsNavalBattle;
		Agent agent = (attackInformation.IsAttackerAgentMount ? attackInformation.AttackerAgent.RiderAgent : attackInformation.AttackerAgent);
		CharacterObject characterObject = (attackInformation.IsAttackerAgentMount ? attackInformation.AttackerRiderAgentCharacter : attackInformation.AttackerAgentCharacter) as CharacterObject;
		CharacterObject captainCharacter = attackInformation.AttackerCaptainCharacter as CharacterObject;
		BattleEnvironment attackerBattleEnvironment = attackInformation.AttackerBattleEnvironment;
		Agent agent2 = (attackInformation.IsVictimAgentMount ? attackInformation.AttackerAgent.RiderAgent : attackInformation.VictimAgent);
		_ = attackInformation.IsVictimAgentMount;
		CharacterObject captainCharacter2 = attackInformation.VictimCaptainCharacter as CharacterObject;
		BattleEnvironment victimBattleEnvironment = attackInformation.VictimBattleEnvironment;
		bool flag = collisionData.AttackBlockedWithShield || collisionData.CollidedWithShieldOnBack;
		ExplainedNumber bonuses = new ExplainedNumber(baseNumber);
		WeaponComponentData currentUsageItem = attackInformation.AttackerWeapon.CurrentUsageItem;
		if (characterObject != null)
		{
			if (currentUsageItem != null)
			{
				if (currentUsageItem.IsMeleeWeapon)
				{
					if (Mission.Current.IsNavalBattle)
					{
						if (currentUsageItem.RelevantSkill == DefaultSkills.OneHanded)
						{
							PerkHelper.AddPerkBonusForCharacter(NavalPerks.Shipmaster.TheCorsairsEdge, attackerBattleEnvironment, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						if (currentUsageItem.WeaponClass == WeaponClass.OneHandedAxe || currentUsageItem.WeaponClass == WeaponClass.TwoHandedAxe)
						{
							PerkHelper.AddPerkBonusForCharacter(NavalPerks.Mariner.AxeOfTheNorthwind, attackerBattleEnvironment, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						if (currentUsageItem.WeaponClass == WeaponClass.OneHandedSword || currentUsageItem.WeaponClass == WeaponClass.TwoHandedSword)
						{
							PerkHelper.AddPerkBonusForCharacter(NavalPerks.Mariner.SunnyDisposition, attackerBattleEnvironment, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						if (currentUsageItem.WeaponClass == WeaponClass.TwoHandedAxe || currentUsageItem.WeaponClass == WeaponClass.TwoHandedMace || currentUsageItem.WeaponClass == WeaponClass.TwoHandedPolearm || currentUsageItem.WeaponClass == WeaponClass.TwoHandedSword)
						{
							PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Mariner.MightyBlows, attackerBattleEnvironment, captainCharacter, ref bonuses);
						}
						if (currentUsageItem.IsMeleeWeapon)
						{
							PerkHelper.AddPerkBonusForCharacter(NavalPerks.Mariner.WarriorsMight, attackerBattleEnvironment, characterObject, isPrimaryBonus: true, ref bonuses);
						}
					}
				}
				else if (currentUsageItem.IsConsumable)
				{
					if (currentUsageItem.RelevantSkill == DefaultSkills.Bow && collisionData.CollisionBoneIndex != -1)
					{
						if (isNavalBattle)
						{
							PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Mariner.TheSkysFury, attackerBattleEnvironment, captainCharacter, ref bonuses);
						}
					}
					else if (currentUsageItem.RelevantSkill == DefaultSkills.Crossbow && collisionData.CollisionBoneIndex != -1)
					{
						if (isNavalBattle)
						{
							PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Mariner.TheSkysFury, attackerBattleEnvironment, captainCharacter, ref bonuses);
						}
					}
					else if (currentUsageItem.RelevantSkill == DefaultSkills.Throwing && isNavalBattle)
					{
						PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Mariner.CrewOfSpears, attackerBattleEnvironment, captainCharacter, ref bonuses);
						PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Mariner.WarriorsMight, attackerBattleEnvironment, captainCharacter, ref bonuses);
					}
					if (isNavalBattle && (currentUsageItem.RelevantSkill == DefaultSkills.Bow || currentUsageItem.RelevantSkill == DefaultSkills.Crossbow || currentUsageItem.RelevantSkill == DefaultSkills.Throwing))
					{
						if (flag)
						{
							PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Boatswain.AccuracyTraining, attackerBattleEnvironment, captainCharacter, ref bonuses);
						}
						if (!IsAgentCrewBoarded(agent2))
						{
							PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Shipmaster.SeaborneFortress, victimBattleEnvironment, captainCharacter2, ref bonuses);
						}
						PerkHelper.AddPerkBonusForCharacter(NavalPerks.Mariner.TheSkysFury, attackerBattleEnvironment, characterObject, isPrimaryBonus: true, ref bonuses);
					}
				}
			}
			if ((currentUsageItem == null || currentUsageItem.IsMeleeWeapon) && Mission.Current.IsNavalBattle)
			{
				if (IsAgentOnEnemyShip(agent))
				{
					_ = agent.Name == "Itsul Ironeye";
					PerkHelper.AddPerkBonusForCharacter(NavalPerks.Mariner.BoardingMaster, attackerBattleEnvironment, characterObject, isPrimaryBonus: true, ref bonuses);
					PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Mariner.BoardingMaster, attackerBattleEnvironment, captainCharacter, ref bonuses);
				}
				else if (IsAgentOnOwnShip(agent))
				{
					PerkHelper.AddPerkBonusForCharacter(NavalPerks.Mariner.HomeTurfAdvantage, attackerBattleEnvironment, characterObject, isPrimaryBonus: true, ref bonuses);
					PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Mariner.HomeTurfAdvantage, attackerBattleEnvironment, captainCharacter, ref bonuses);
				}
			}
			if (collisionData.IsAlternativeAttack)
			{
				PerkHelper.AddPerkBonusForCharacter(NavalPerks.Mariner.BruteForce, attackerBattleEnvironment, characterObject, isPrimaryBonus: true, ref bonuses);
			}
			if (flag && isNavalBattle)
			{
				PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Mariner.Forceful, attackerBattleEnvironment, captainCharacter, ref bonuses);
			}
		}
		return bonuses.ResultNumber;
	}

	public override float ApplyDamageScaling(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
	{
		return base.BaseModel.ApplyDamageScaling(in attackInformation, in collisionData, baseDamage);
	}

	public override float ApplyDamageReductions(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
	{
		float baseNumber = base.BaseModel.ApplyDamageReductions(in attackInformation, in collisionData, baseDamage);
		bool isNavalBattle = Mission.Current.IsNavalBattle;
		_ = attackInformation.IsAttackerAgentMount;
		Agent agent = (attackInformation.IsVictimAgentMount ? attackInformation.VictimAgent.RiderAgent : attackInformation.VictimAgent);
		Agent agent2 = (attackInformation.IsAttackerAgentMount ? attackInformation.AttackerAgent.RiderAgent : attackInformation.AttackerAgent);
		CharacterObject characterObject = (attackInformation.IsVictimAgentMount ? attackInformation.VictimRiderAgentCharacter : attackInformation.VictimAgentCharacter) as CharacterObject;
		CharacterObject characterObject2 = attackInformation.VictimCaptainCharacter as CharacterObject;
		BattleEnvironment victimBattleEnvironment = attackInformation.VictimBattleEnvironment;
		ExplainedNumber bonuses = new ExplainedNumber(baseNumber);
		WeaponComponentData currentUsageItem = attackInformation.AttackerWeapon.CurrentUsageItem;
		if (characterObject != null && currentUsageItem != null)
		{
			if (currentUsageItem.IsConsumable)
			{
				if (isNavalBattle)
				{
					if (agent.CurrentlyUsedGameObject != null && agent.CurrentlyUsedGameObject.GetComponent<UserDamageCalculateComponent>() != null)
					{
						UserDamageCalculateComponent component = agent.CurrentlyUsedGameObject.GetComponent<UserDamageCalculateComponent>();
						component.ApplyPerkBonusForCharacter(NavalPerks.Shipmaster.TheHelmsmansShield, isPrimaryBonus: true, characterObject, BattleEnvironment.Naval, ref bonuses);
						if (agent == Agent.Main)
						{
							bonuses.AddFactor(component.DamageReductionFactor);
							if (currentUsageItem.WeaponClass == WeaponClass.BallistaBoulder && NavalStorylineData.GetNavalStorylineSetPieceBattleMissionType() == NavalStorylineData.NavalStorylineSetPieceBattleMissionTypes.Act3Quest4)
							{
								bonuses.AddFactor(-0.9f);
							}
						}
					}
					if (agent2 != null && agent2.IsAIControlled && (currentUsageItem.WeaponClass == WeaponClass.Bolt || currentUsageItem.WeaponClass == WeaponClass.Arrow))
					{
						bonuses.AddFactor(-0.15f);
					}
				}
			}
			else if (currentUsageItem.IsMeleeWeapon)
			{
				float effectValue;
				if (Mission.Current.IsNavalBattle && IsAgentOnEnemyShip(agent))
				{
					PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Mariner.TerrorOfTheSeas, victimBattleEnvironment, characterObject2, ref bonuses);
				}
				else if (Mission.Current.IsNavalBattle && IsAgentOnOwnShip(agent) && characterObject2 != null && characterObject2.GetPerkValue(NavalPerks.Mariner.RallyingCry, victimBattleEnvironment, isPrimaryEffect: false, out effectValue))
				{
					bonuses.AddFactor(NavalPerks.Mariner.RallyingCry.SecondaryBonus);
				}
			}
		}
		return bonuses.ResultNumber;
	}

	public override float ApplyGeneralDamageModifiers(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
	{
		return base.BaseModel.ApplyGeneralDamageModifiers(in attackInformation, in collisionData, baseDamage);
	}

	public override bool DecideCrushedThrough(Agent attackerAgent, Agent defenderAgent, float totalAttackEnergy, Agent.UsageDirection attackDirection, StrikeType strikeType, WeaponComponentData defendItem, bool isPassiveUsage)
	{
		return base.BaseModel.DecideCrushedThrough(attackerAgent, defenderAgent, totalAttackEnergy, attackDirection, strikeType, defendItem, isPassiveUsage);
	}

	public override void DecideMissileWeaponFlags(Agent attackerAgent, in MissionWeapon missileWeapon, ref WeaponFlags missileWeaponFlags)
	{
		base.BaseModel.DecideMissileWeaponFlags(attackerAgent, in missileWeapon, ref missileWeaponFlags);
		if (attackerAgent?.Character is CharacterObject characterObject && missileWeapon.CurrentUsageItem.WeaponClass == WeaponClass.Javelin && Mission.Current.IsNavalBattle && characterObject.GetPerkValue(NavalPerks.Mariner.CrewOfSpears))
		{
			missileWeaponFlags |= WeaponFlags.CanPenetrateShield;
		}
	}

	public override bool CanWeaponIgnoreFriendlyFireChecks(WeaponComponentData weapon)
	{
		return base.BaseModel.CanWeaponIgnoreFriendlyFireChecks(weapon);
	}

	public override bool CanWeaponDealSneakAttack(in AttackInformation attackInformation, WeaponComponentData weapon)
	{
		return base.BaseModel.CanWeaponDealSneakAttack(in attackInformation, weapon);
	}

	public override bool CanWeaponDismount(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
	{
		return base.BaseModel.CanWeaponDismount(attackerAgent, attackerWeapon, in blow, in collisionData);
	}

	public override void CalculateDefendedBlowStunMultipliers(Agent attackerAgent, Agent defenderAgent, CombatCollisionResult collisionResult, WeaponComponentData attackerWeapon, WeaponComponentData defenderWeapon, ref float attackerStunPeriod, ref float defenderStunPeriod)
	{
		base.BaseModel.CalculateDefendedBlowStunMultipliers(attackerAgent, defenderAgent, collisionResult, attackerWeapon, defenderWeapon, ref attackerStunPeriod, ref defenderStunPeriod);
	}

	public override bool CanWeaponKnockback(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
	{
		return base.BaseModel.CanWeaponKnockback(attackerAgent, attackerWeapon, in blow, in collisionData);
	}

	public override bool CanWeaponKnockDown(Agent attackerAgent, Agent victimAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
	{
		return base.BaseModel.CanWeaponKnockDown(attackerAgent, victimAgent, attackerWeapon, in blow, in collisionData);
	}

	public override float GetDismountPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
	{
		return base.BaseModel.GetDismountPenetration(attackerAgent, attackerWeapon, in blow, in collisionData);
	}

	public override float GetKnockBackPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
	{
		return base.BaseModel.GetKnockBackPenetration(attackerAgent, attackerWeapon, in blow, in collisionData);
	}

	public override float GetKnockDownPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
	{
		return base.BaseModel.GetKnockDownPenetration(attackerAgent, attackerWeapon, in blow, in collisionData);
	}

	public override float GetHorseChargePenetration()
	{
		return base.BaseModel.GetHorseChargePenetration();
	}

	public override float CalculateStaggerThresholdDamage(Agent defenderAgent, in Blow blow)
	{
		return base.BaseModel.CalculateStaggerThresholdDamage(defenderAgent, in blow);
	}

	public override float CalculateAlternativeAttackDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, WeaponComponentData weapon)
	{
		return base.BaseModel.CalculateAlternativeAttackDamage(in attackInformation, in collisionData, weapon);
	}

	public override float CalculatePassiveAttackDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
	{
		return base.BaseModel.CalculatePassiveAttackDamage(in attackInformation, in collisionData, baseDamage);
	}

	public override MeleeCollisionReaction DecidePassiveAttackCollisionReaction(Agent attacker, Agent defender, bool isFatalHit)
	{
		return base.BaseModel.DecidePassiveAttackCollisionReaction(attacker, defender, isFatalHit);
	}

	public override float CalculateShieldDamage(in AttackInformation attackInformation, float baseDamage)
	{
		return base.BaseModel.CalculateShieldDamage(in attackInformation, baseDamage);
	}

	public override float CalculateSailFireDamage(Agent agent, IShipOrigin shipOrigin, float baseDamage, bool damageFromShipMachine)
	{
		float baseNumber = base.BaseModel.CalculateSailFireDamage(agent, shipOrigin, baseDamage, damageFromShipMachine);
		ExplainedNumber bonuses = new ExplainedNumber(baseNumber);
		Agent agent2 = agent.Formation?.Captain;
		CharacterObject characterObject = agent2?.Character as CharacterObject;
		BattleEnvironment battleEnvironment = agent2?.CurrentBattleEnvironment ?? BattleEnvironment.None;
		if (characterObject != null)
		{
			PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Mariner.EnemyOfTheWood, battleEnvironment, characterObject, ref bonuses);
			if (!damageFromShipMachine)
			{
				PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Boatswain.SpecialArrows, battleEnvironment, characterObject, ref bonuses);
			}
		}
		Figurehead figurehead = (shipOrigin as Ship).Figurehead;
		if (figurehead != null && figurehead == DefaultFigureheads.SeaSerpent)
		{
			bonuses.AddFactor(0f - figurehead.EffectAmount);
		}
		return bonuses.ResultNumber;
	}

	public override float CalculateHullFireDamage(float baseFireDamage, IShipOrigin shipOrigin)
	{
		base.BaseModel.CalculateHullFireDamage(baseFireDamage, shipOrigin);
		ExplainedNumber explainedNumber = new ExplainedNumber(baseFireDamage);
		Figurehead figurehead = (shipOrigin as Ship).Figurehead;
		if (figurehead != null && figurehead == DefaultFigureheads.SeaSerpent)
		{
			explainedNumber.AddFactor(0f - figurehead.EffectAmount);
		}
		return explainedNumber.ResultNumber;
	}

	public override float GetDamageMultiplierForBodyPart(BoneBodyPartType bodyPart, DamageTypes type, bool isHuman, bool isMissile)
	{
		return base.BaseModel.GetDamageMultiplierForBodyPart(bodyPart, type, isHuman, isMissile);
	}

	public override bool DecideAgentShrugOffBlow(Agent victimAgent, in AttackCollisionData collisionData, in Blow blow)
	{
		return base.BaseModel.DecideAgentShrugOffBlow(victimAgent, in collisionData, in blow);
	}

	public override bool DecideAgentDismountedByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
	{
		return base.BaseModel.DecideAgentDismountedByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
	}

	public override bool DecideAgentKnockedBackByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
	{
		return base.BaseModel.DecideAgentKnockedBackByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
	}

	public override bool DecideAgentKnockedDownByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
	{
		return base.BaseModel.DecideAgentKnockedDownByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
	}

	public override bool DecideMountRearedByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
	{
		return base.BaseModel.DecideMountRearedByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
	}

	public override void DecideWeaponCollisionReaction(in Blow registeredBlow, in AttackCollisionData collisionData, Agent attacker, Agent defender, in MissionWeapon attackerWeapon, bool isFatalHit, bool isShruggedOff, float momentumRemaining, out MeleeCollisionReaction colReaction)
	{
		base.BaseModel.DecideWeaponCollisionReaction(in registeredBlow, in collisionData, attacker, defender, in attackerWeapon, isFatalHit, isShruggedOff, momentumRemaining, out colReaction);
	}

	public override bool ShouldMissilePassThroughAfterShieldBreak(Agent attackerAgent, WeaponComponentData attackerWeapon)
	{
		bool result = base.BaseModel.ShouldMissilePassThroughAfterShieldBreak(attackerAgent, attackerWeapon);
		CharacterObject characterObject = (CharacterObject)attackerAgent.Character;
		if (characterObject != null && Mission.Current.IsNavalBattle && attackerWeapon != null && attackerWeapon.WeaponClass == WeaponClass.ThrowingAxe && characterObject.GetPerkValue(NavalPerks.Mariner.CrewOfSpears))
		{
			return true;
		}
		return result;
	}

	public override float CalculateRemainingMomentum(float originalMomentum, in Blow b, in AttackCollisionData collisionData, Agent attacker, Agent victim, in MissionWeapon attackerWeapon, bool isCrushThrough)
	{
		float num = base.BaseModel.CalculateRemainingMomentum(originalMomentum, in b, in collisionData, attacker, victim, in attackerWeapon, isCrushThrough);
		CharacterObject characterObject = (CharacterObject)attacker.Character;
		BattleEnvironment currentBattleEnvironment = attacker.CurrentBattleEnvironment;
		if (collisionData.IsColliderAgent && !collisionData.IsHorseCharge && (attacker == null || !attacker.IsDoingPassiveAttack) && !MissionCombatMechanicsHelper.HitWithAnotherBone(in collisionData, attacker, in attackerWeapon) && !attackerWeapon.IsEmpty && b.StrikeType != StrikeType.Thrust && !attackerWeapon.IsEmpty && attackerWeapon.CurrentUsageItem.RelevantSkill == DefaultSkills.TwoHanded)
		{
			ExplainedNumber bonuses = new ExplainedNumber(0f, includeDescriptions: false, null);
			bonuses.LimitMin(0f);
			if ((float)b.InflictedDamage > 0f)
			{
				bonuses.Add(b.AbsorbedByArmor / (float)b.InflictedDamage);
				if (characterObject != null)
				{
					PerkHelper.AddPerkBonusForCharacter(NavalPerks.Mariner.MightyBlows, currentBattleEnvironment, characterObject, isPrimaryBonus: true, ref bonuses);
				}
			}
			num = originalMomentum - bonuses.ResultNumber;
			num *= 0.5f;
			if (num < 0.25f)
			{
				num = 0f;
			}
		}
		return num;
	}

	private bool IsAgentOnEnemyShip(Agent agent)
	{
		foreach (MissionShip allShip in GetNavalShipsLogic().AllShips)
		{
			if (allShip.GameEntity != null && allShip.Team != null && allShip.GetIsAgentOnShip(agent) && agent.Team.IsEnemyOf(allShip.Team))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsAgentOnOwnShip(Agent agent)
	{
		foreach (MissionShip allShip in GetNavalShipsLogic().AllShips)
		{
			if (allShip.GameEntity != null && allShip.Team != null && allShip.GetIsAgentOnShip(agent) && agent.Team.IsFriendOf(allShip.Team))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsAgentCrewBoarded(Agent agent)
	{
		NavalShipsLogic navalShipsLogic = GetNavalShipsLogic();
		bool result = false;
		foreach (MissionShip allShip in navalShipsLogic.AllShips)
		{
			if (allShip.GameEntity != null && allShip.GetIsConnectedToEnemy())
			{
				result = true;
			}
		}
		return result;
	}
}
