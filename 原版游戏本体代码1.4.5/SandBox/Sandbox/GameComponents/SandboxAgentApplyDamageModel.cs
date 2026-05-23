using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace SandBox.GameComponents;

public class SandboxAgentApplyDamageModel : AgentApplyDamageModel
{
	private const float SallyOutSiegeEngineDamageMultiplier = 4.5f;

	public override bool IsDamageIgnored(in AttackInformation attackInformation, in AttackCollisionData collisionData)
	{
		CharacterObject characterObject = (attackInformation.IsVictimAgentMount ? attackInformation.VictimRiderAgentCharacter : attackInformation.VictimAgentCharacter) as CharacterObject;
		WeaponComponentData currentUsageItem = attackInformation.AttackerWeapon.CurrentUsageItem;
		bool result = false;
		if (currentUsageItem != null && currentUsageItem.IsConsumable && collisionData.CollidedWithShieldOnBack && characterObject != null && characterObject.GetPerkValue(DefaultPerks.Crossbow.Pavise))
		{
			float num = MBMath.ClampFloat(DefaultPerks.Crossbow.Pavise.PrimaryBonus, 0f, 1f);
			result = MBRandom.RandomFloat <= num;
		}
		return result;
	}

	public override float ApplyDamageAmplifications(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
	{
		Formation attackerFormation = attackInformation.AttackerFormation;
		BannerComponent activeBanner = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(attackerFormation);
		Agent agent = (attackInformation.IsAttackerAgentMount ? attackInformation.AttackerAgent.RiderAgent : attackInformation.AttackerAgent);
		CharacterObject characterObject = (attackInformation.IsAttackerAgentMount ? attackInformation.AttackerRiderAgentCharacter : attackInformation.AttackerAgentCharacter) as CharacterObject;
		CharacterObject captainCharacter = attackInformation.AttackerCaptainCharacter as CharacterObject;
		bool flag = attackInformation.IsAttackerAgentHuman && !attackInformation.DoesAttackerHaveMountAgent;
		bool flag2 = attackInformation.DoesAttackerHaveMountAgent || attackInformation.DoesAttackerHaveRiderAgent;
		CharacterObject characterObject2 = (attackInformation.IsVictimAgentMount ? attackInformation.VictimRiderAgentCharacter : attackInformation.VictimAgentCharacter) as CharacterObject;
		bool flag3 = attackInformation.IsVictimAgentHuman && !attackInformation.DoesVictimHaveMountAgent;
		bool flag4 = attackInformation.DoesVictimHaveMountAgent || attackInformation.DoesVictimHaveRiderAgent;
		Formation victimFormation = attackInformation.VictimFormation;
		BannerComponent activeBanner2 = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(victimFormation);
		bool flag5 = collisionData.AttackBlockedWithShield || collisionData.CollidedWithShieldOnBack;
		ExplainedNumber bonuses = new ExplainedNumber(baseDamage);
		MissionWeapon attackerWeapon = attackInformation.AttackerWeapon;
		WeaponComponentData currentUsageItem = attackerWeapon.CurrentUsageItem;
		if (characterObject != null)
		{
			if (currentUsageItem != null)
			{
				if (currentUsageItem.IsMeleeWeapon)
				{
					if (currentUsageItem.RelevantSkill == DefaultSkills.OneHanded)
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.DeadlyPurpose, characterObject, isPrimaryBonus: true, ref bonuses);
						if (flag2)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.Cavalry, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						if (attackInformation.OffHandItem.IsEmpty)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.Duelist, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						if (currentUsageItem.WeaponClass == WeaponClass.Mace || currentUsageItem.WeaponClass == WeaponClass.OneHandedAxe)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.ToBeBlunt, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						if (flag5)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.Prestige, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Roguery.Carver, captainCharacter, ref bonuses);
						PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.OneHanded.WayOfTheSword, characterObject, DefaultSkills.OneHanded, applyPrimaryBonus: false, ref bonuses, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus);
					}
					else if (currentUsageItem.RelevantSkill == DefaultSkills.TwoHanded)
					{
						if (flag5)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.TwoHanded.WoodChopper, characterObject, isPrimaryBonus: true, ref bonuses);
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.TwoHanded.WoodChopper, captainCharacter, ref bonuses);
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.TwoHanded.ShieldBreaker, characterObject, isPrimaryBonus: true, ref bonuses);
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.TwoHanded.ShieldBreaker, captainCharacter, ref bonuses);
						}
						if (currentUsageItem.WeaponClass == WeaponClass.TwoHandedAxe || currentUsageItem.WeaponClass == WeaponClass.TwoHandedMace)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.TwoHanded.HeadBasher, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						if (attackInformation.IsVictimAgentMount)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.TwoHanded.BeastSlayer, characterObject, isPrimaryBonus: true, ref bonuses);
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.TwoHanded.BeastSlayer, captainCharacter, ref bonuses);
						}
						if (attackInformation.AttackerHitPointRate < 0.5f)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.TwoHanded.Berserker, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						else if (attackInformation.AttackerHitPointRate > 0.9f)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.TwoHanded.Confidence, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.TwoHanded.BladeMaster, characterObject, isPrimaryBonus: true, ref bonuses);
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Roguery.DashAndSlash, captainCharacter, ref bonuses);
						PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.TwoHanded.WayOfTheGreatAxe, characterObject, DefaultSkills.TwoHanded, applyPrimaryBonus: false, ref bonuses, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus);
					}
					else if (currentUsageItem.RelevantSkill == DefaultSkills.Polearm)
					{
						if (flag2)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Polearm.Cavalry, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						else
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Polearm.Pikeman, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						if (collisionData.StrikeType == 1)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Polearm.CleanThrust, characterObject, isPrimaryBonus: true, ref bonuses);
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Polearm.SharpenTheTip, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						if (attackInformation.IsVictimAgentMount)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Polearm.SteedKiller, characterObject, isPrimaryBonus: true, ref bonuses);
							if (flag)
							{
								PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Polearm.SteedKiller, captainCharacter, ref bonuses);
							}
						}
						if (attackInformation.IsHeadShot)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Polearm.Guards, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Polearm.Phalanx, captainCharacter, ref bonuses);
						PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Polearm.WayOfTheSpear, characterObject, DefaultSkills.Polearm, applyPrimaryBonus: false, ref bonuses, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus);
					}
					else if (currentUsageItem.IsShield)
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.Basher, characterObject, isPrimaryBonus: true, ref bonuses);
					}
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Athletics.Powerful, characterObject, isPrimaryBonus: true, ref bonuses);
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Athletics.Powerful, captainCharacter, ref bonuses);
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Engineering.ImprovedTools, captainCharacter, ref bonuses);
					if (attackerWeapon.Item != null && attackerWeapon.Item.ItemType == ItemObject.ItemTypeEnum.Thrown)
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.FlexibleFighter, characterObject, isPrimaryBonus: true, ref bonuses);
					}
					if (flag2)
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Riding.MountedWarrior, characterObject, isPrimaryBonus: true, ref bonuses);
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Riding.MountedWarrior, captainCharacter, ref bonuses);
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.OneHanded.Cavalry, captainCharacter, ref bonuses);
					}
					else
					{
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.OneHanded.DeadlyPurpose, captainCharacter, ref bonuses);
						if (collisionData.StrikeType == 1)
						{
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Polearm.SharpenTheTip, captainCharacter, ref bonuses);
						}
					}
					if (activeBanner != null)
					{
						BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.IncreasedMeleeDamage, activeBanner, ref bonuses);
						if (attackInformation.DoesVictimHaveMountAgent)
						{
							BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.IncreasedMeleeDamageAgainstMountedTroops, activeBanner, ref bonuses);
						}
					}
				}
				else if (currentUsageItem.IsConsumable)
				{
					if (currentUsageItem.RelevantSkill == DefaultSkills.Bow && collisionData.CollisionBoneIndex != -1)
					{
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Bow.BowControl, captainCharacter, ref bonuses);
						if (attackInformation.IsHeadShot)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.DeadAim, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.StrongBows, characterObject, isPrimaryBonus: true, ref bonuses);
						if (characterObject.Tier >= 3)
						{
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Bow.StrongBows, captainCharacter, ref bonuses);
						}
						if (attackInformation.IsVictimAgentMount)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.HunterClan, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Bow.Deadshot, characterObject, DefaultSkills.Bow, applyPrimaryBonus: false, ref bonuses, Campaign.Current.Models.CharacterDevelopmentModel.MinSkillRequiredForEpicPerkBonus);
					}
					else if (currentUsageItem.RelevantSkill == DefaultSkills.Crossbow && collisionData.CollisionBoneIndex != -1)
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Engineering.TorsionEngines, characterObject, isPrimaryBonus: false, ref bonuses);
						if (attackInformation.IsVictimAgentMount)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Crossbow.Unhorser, characterObject, isPrimaryBonus: true, ref bonuses);
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Crossbow.Unhorser, captainCharacter, ref bonuses);
						}
						if (attackInformation.IsHeadShot)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Crossbow.Sheriff, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						if (flag3)
						{
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Crossbow.Sheriff, captainCharacter, ref bonuses);
						}
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Crossbow.HammerBolts, captainCharacter, ref bonuses);
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Engineering.DreadfulSieger, captainCharacter, ref bonuses);
						PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Crossbow.MightyPull, characterObject, DefaultSkills.Crossbow, applyPrimaryBonus: false, ref bonuses, Campaign.Current.Models.CharacterDevelopmentModel.MinSkillRequiredForEpicPerkBonus);
					}
					else if (currentUsageItem.RelevantSkill == DefaultSkills.Throwing)
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Athletics.StrongArms, characterObject, isPrimaryBonus: true, ref bonuses);
						if (flag5)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.ShieldBreaker, characterObject, isPrimaryBonus: true, ref bonuses);
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Throwing.ShieldBreaker, captainCharacter, ref bonuses);
							if (currentUsageItem.WeaponClass == WeaponClass.ThrowingAxe)
							{
								PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.Splinters, characterObject, isPrimaryBonus: true, ref bonuses);
							}
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Throwing.Splinters, captainCharacter, ref bonuses);
						}
						if (attackInformation.IsVictimAgentMount)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.Hunter, characterObject, isPrimaryBonus: true, ref bonuses);
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Throwing.Hunter, captainCharacter, ref bonuses);
						}
						if (flag2)
						{
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Throwing.MountedSkirmisher, captainCharacter, ref bonuses);
						}
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Throwing.Impale, captainCharacter, ref bonuses);
						if (flag4)
						{
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Throwing.KnockOff, captainCharacter, ref bonuses);
						}
						if (attackInformation.VictimAgentHealth <= attackInformation.VictimAgentMaxHealth * 0.5f)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.LastHit, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						if (attackInformation.IsHeadShot)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.HeadHunter, characterObject, isPrimaryBonus: true, ref bonuses);
						}
						PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Throwing.UnstoppableForce, characterObject, DefaultSkills.Throwing, applyPrimaryBonus: false, ref bonuses, Campaign.Current.Models.CharacterDevelopmentModel.MinSkillRequiredForEpicPerkBonus);
					}
					if (flag2)
					{
						PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Riding.HorseArcher, characterObject, isPrimaryBonus: true, ref bonuses);
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Riding.HorseArcher, captainCharacter, ref bonuses);
					}
					if (activeBanner != null)
					{
						BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.IncreasedRangedDamage, activeBanner, ref bonuses);
					}
				}
				if (attackerWeapon.Item != null && attackerWeapon.Item.IsCivilian)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Roguery.Carver, characterObject, isPrimaryBonus: true, ref bonuses);
				}
			}
			if (collisionData.IsHorseCharge)
			{
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Riding.FullSpeed, characterObject, isPrimaryBonus: true, ref bonuses);
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Riding.FullSpeed, captainCharacter, ref bonuses);
				if (characterObject.GetPerkValue(DefaultPerks.Riding.TheWayOfTheSaddle))
				{
					float value = (float)MathF.Max(MissionGameModels.Current.AgentStatCalculateModel.GetEffectiveSkill(agent, DefaultSkills.Riding) - Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus, 0) * DefaultPerks.Riding.TheWayOfTheSaddle.PrimaryBonus;
					bonuses.Add(value);
				}
				if (activeBanner != null)
				{
					BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.IncreasedChargeDamage, activeBanner, ref bonuses);
				}
				if (activeBanner2 != null)
				{
					BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.DecreasedChargeDamage, activeBanner2, ref bonuses);
				}
			}
			if (flag)
			{
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.TwoHanded.HeadBasher, captainCharacter, ref bonuses);
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.TwoHanded.RecklessCharge, captainCharacter, ref bonuses);
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Polearm.Pikeman, captainCharacter, ref bonuses);
				if (flag4)
				{
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Polearm.Braced, captainCharacter, ref bonuses);
				}
			}
			if (flag2)
			{
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Polearm.Cavalry, captainCharacter, ref bonuses);
			}
			if (currentUsageItem == null && collisionData.IsAlternativeAttack && characterObject.GetPerkValue(DefaultPerks.Athletics.StrongLegs))
			{
				bonuses.AddFactor(1f);
			}
			if (flag5)
			{
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Engineering.WallBreaker, captainCharacter, ref bonuses);
			}
			if (collisionData.EntityExists)
			{
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.TwoHanded.Vandal, captainCharacter, ref bonuses);
			}
			if (characterObject2 != null)
			{
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Tactics.Coaching, captainCharacter, ref bonuses);
				if (characterObject2.Culture.IsBandit)
				{
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Tactics.LawKeeper, captainCharacter, ref bonuses);
				}
				if (flag2 && flag3)
				{
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Tactics.Gensdarmes, captainCharacter, ref bonuses);
				}
			}
			if (characterObject.Culture.IsBandit)
			{
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Roguery.PartnersInCrime, captainCharacter, ref bonuses);
			}
		}
		return bonuses.ResultNumber;
	}

	public override float ApplyDamageScaling(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
	{
		float num = 1f;
		if (Mission.Current.IsSallyOutBattle)
		{
			DestructableComponent hitObjectDestructibleComponent = attackInformation.HitObjectDestructibleComponent;
			if (hitObjectDestructibleComponent != null && hitObjectDestructibleComponent.GameEntity.GetFirstScriptOfType<SiegeWeapon>() != null)
			{
				num *= 4.5f;
			}
		}
		return baseDamage * num;
	}

	public override float ApplyDamageReductions(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
	{
		Formation attackerFormation = attackInformation.AttackerFormation;
		MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(attackerFormation);
		Agent agent = (attackInformation.IsAttackerAgentMount ? attackInformation.AttackerAgent.RiderAgent : attackInformation.AttackerAgent);
		_ = attackInformation.IsAttackerAgentMount;
		if (attackInformation.IsAttackerAgentHuman)
		{
			_ = !attackInformation.DoesAttackerHaveMountAgent;
		}
		else
			_ = 0;
		if (attackInformation.DoesAttackerHaveMountAgent)
		{
			_ = 1;
		}
		else
			_ = attackInformation.DoesAttackerHaveRiderAgent;
		CharacterObject characterObject = (attackInformation.IsVictimAgentMount ? attackInformation.VictimRiderAgentCharacter : attackInformation.VictimAgentCharacter) as CharacterObject;
		CharacterObject characterObject2 = attackInformation.VictimCaptainCharacter as CharacterObject;
		bool flag = attackInformation.IsVictimAgentHuman && !attackInformation.DoesVictimHaveMountAgent;
		if (attackInformation.DoesVictimHaveMountAgent)
		{
			_ = 1;
		}
		else
			_ = attackInformation.DoesVictimHaveRiderAgent;
		Formation victimFormation = attackInformation.VictimFormation;
		BannerComponent activeBanner = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(victimFormation);
		_ = attackInformation.VictimMainHandWeapon.CurrentUsageItem;
		WeaponComponentData currentUsageItem = attackInformation.VictimMainHandWeapon.CurrentUsageItem;
		bool flag2 = collisionData.AttackBlockedWithShield || collisionData.CollidedWithShieldOnBack;
		ExplainedNumber explainedNumber = new ExplainedNumber(baseDamage);
		WeaponComponentData currentUsageItem2 = attackInformation.AttackerWeapon.CurrentUsageItem;
		if (attackInformation.DoesAttackerHaveMountAgent && (currentUsageItem2 == null || currentUsageItem2.RelevantSkill != DefaultSkills.Crossbow))
		{
			int effectiveSkill = MissionGameModels.Current.AgentStatCalculateModel.GetEffectiveSkill(agent, DefaultSkills.Riding);
			SkillHelper.AddSkillBonusForSkillLevel(DefaultSkillEffects.MountedWeaponDamagePenalty, ref explainedNumber, effectiveSkill);
		}
		if (characterObject != null)
		{
			if (currentUsageItem2 != null)
			{
				if (currentUsageItem2.IsConsumable)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Bow.SkirmishPhaseMaster, characterObject, isPrimaryBonus: true, ref explainedNumber);
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Throwing.Skirmisher, characterObject2, ref explainedNumber);
					if (characterObject.IsRanged)
					{
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Bow.SkirmishPhaseMaster, characterObject2, ref explainedNumber);
					}
					if (currentUsageItem != null)
					{
						if (currentUsageItem.WeaponClass == WeaponClass.Crossbow)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Crossbow.CounterFire, characterObject, isPrimaryBonus: true, ref explainedNumber);
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Crossbow.CounterFire, characterObject2, ref explainedNumber);
						}
						else if (currentUsageItem.RelevantSkill == DefaultSkills.Throwing)
						{
							PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Throwing.Skirmisher, characterObject, isPrimaryBonus: true, ref explainedNumber);
						}
					}
					if (activeBanner != null)
					{
						BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.DecreasedRangedAttackDamage, activeBanner, ref explainedNumber);
					}
				}
				else if (currentUsageItem2.IsMeleeWeapon)
				{
					if (characterObject2 != null)
					{
						Formation victimFormation2 = attackInformation.VictimFormation;
						if (victimFormation2 != null && victimFormation2.ArrangementOrder.OrderEnum == ArrangementOrder.ArrangementOrderEnum.ShieldWall)
						{
							PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.OneHanded.Basher, characterObject2, ref explainedNumber);
						}
					}
					if (activeBanner != null)
					{
						BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.DecreasedMeleeAttackDamage, activeBanner, ref explainedNumber);
					}
				}
			}
			if (flag2)
			{
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.SteelCoreShields, characterObject, isPrimaryBonus: true, ref explainedNumber);
				if (flag)
				{
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.OneHanded.SteelCoreShields, characterObject2, ref explainedNumber);
				}
				if (collisionData.AttackBlockedWithShield && !collisionData.CorrectSideShieldBlock)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.OneHanded.ShieldWall, characterObject, isPrimaryBonus: true, ref explainedNumber);
				}
			}
			if (collisionData.IsHorseCharge)
			{
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Polearm.SureFooted, characterObject, isPrimaryBonus: true, ref explainedNumber);
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Athletics.Braced, characterObject, isPrimaryBonus: true, ref explainedNumber);
				if (characterObject2 != null)
				{
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Polearm.SureFooted, characterObject2, ref explainedNumber);
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Athletics.Braced, characterObject2, ref explainedNumber);
				}
			}
			if (collisionData.IsFallDamage)
			{
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Athletics.StrongLegs, characterObject, isPrimaryBonus: true, ref explainedNumber);
			}
			PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Tactics.EliteReserves, characterObject2, ref explainedNumber);
		}
		return explainedNumber.ResultNumber;
	}

	public override float ApplyGeneralDamageModifiers(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseDamage)
	{
		_ = attackInformation.IsAttackerAgentMount;
		_ = attackInformation.IsVictimAgentMount;
		WeaponComponentData currentUsageItem = attackInformation.AttackerWeapon.CurrentUsageItem;
		ExplainedNumber explainedNumber = new ExplainedNumber(baseDamage);
		if (currentUsageItem != null)
		{
			if (currentUsageItem.RelevantSkill == DefaultSkills.Throwing)
			{
				explainedNumber = new ExplainedNumber(explainedNumber.ResultNumber * (1f + attackInformation.AttackerAgent.AgentDrivenProperties.ThrowingWeaponDamageMultiplierBonus));
			}
			else if (currentUsageItem.IsMeleeWeapon)
			{
				explainedNumber = new ExplainedNumber(explainedNumber.ResultNumber * (1f + attackInformation.AttackerAgent.AgentDrivenProperties.MeleeWeaponDamageMultiplierBonus));
			}
		}
		Agent attackerAgent = attackInformation.AttackerAgent;
		if (attackerAgent != null)
		{
			explainedNumber = new ExplainedNumber(explainedNumber.ResultNumber * (1f + attackerAgent.AgentDrivenProperties.DamageMultiplierBonus));
		}
		return explainedNumber.ResultNumber;
	}

	public override bool DecideCrushedThrough(Agent attackerAgent, Agent defenderAgent, float totalAttackEnergy, Agent.UsageDirection attackDirection, StrikeType strikeType, WeaponComponentData defendItem, bool isPassiveUsage)
	{
		EquipmentIndex equipmentIndex = attackerAgent.GetOffhandWieldedItemIndex();
		if (equipmentIndex == EquipmentIndex.None)
		{
			equipmentIndex = attackerAgent.GetPrimaryWieldedItemIndex();
		}
		if (((equipmentIndex != EquipmentIndex.None) ? attackerAgent.Equipment[equipmentIndex].CurrentUsageItem : null) == null || isPassiveUsage || strikeType != StrikeType.Swing || attackDirection != Agent.UsageDirection.AttackUp)
		{
			return false;
		}
		float num = 58f;
		if (defendItem != null && defendItem.IsShield)
		{
			num *= 1.2f;
		}
		return totalAttackEnergy > num;
	}

	public override void DecideMissileWeaponFlags(Agent attackerAgent, in MissionWeapon missileWeapon, ref WeaponFlags missileWeaponFlags)
	{
		if (attackerAgent?.Character is CharacterObject characterObject && missileWeapon.CurrentUsageItem.WeaponClass == WeaponClass.Javelin && characterObject.GetPerkValue(DefaultPerks.Throwing.Impale))
		{
			missileWeaponFlags |= WeaponFlags.CanPenetrateShield;
		}
	}

	public override bool CanWeaponIgnoreFriendlyFireChecks(WeaponComponentData weapon)
	{
		if (weapon != null && weapon.IsConsumable && weapon.WeaponFlags.HasAnyFlag(WeaponFlags.CanPenetrateShield) && weapon.WeaponFlags.HasAnyFlag(WeaponFlags.MultiplePenetration))
		{
			return true;
		}
		return false;
	}

	public override bool CanWeaponDealSneakAttack(in AttackInformation attackInformation, WeaponComponentData weapon)
	{
		if (weapon != null && (weapon.IsMeleeWeapon || weapon.WeaponClass == WeaponClass.ThrowingKnife) && attackInformation.IsVictimAgentHuman && !attackInformation.IsVictimPlayer)
		{
			if ((attackInformation.VictimAgentAIStateFlags & Agent.AIStateFlag.Alarmed) == 0 && attackInformation.VictimAgentFlags.HasAnyFlag(AgentFlag.CanGetAlarmed))
			{
				return true;
			}
			if (!attackInformation.VictimAgentAIStateFlags.HasAllFlags(Agent.AIStateFlag.Alarmed) && !attackInformation.IsAttackerAgentNull && Vec2.DotProduct((attackInformation.AttackerAgentPosition - attackInformation.VictimAgentPosition).AsVec2.Normalized(), attackInformation.VictimAgentMovementDirection) < 0.174f)
			{
				return true;
			}
		}
		return false;
	}

	public override bool CanWeaponDismount(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
	{
		if (!MBMath.IsBetween((int)blow.VictimBodyPart, 0, 6))
		{
			return false;
		}
		if (!attackerAgent.HasMount && blow.StrikeType == StrikeType.Swing && blow.WeaponRecord.WeaponFlags.HasAnyFlag(WeaponFlags.CanHook))
		{
			return true;
		}
		if (blow.StrikeType == StrikeType.Thrust && blow.WeaponRecord.WeaponFlags.HasAnyFlag(WeaponFlags.CanDismount))
		{
			return true;
		}
		if (attackerAgent.Character is CharacterObject characterObject)
		{
			if (attackerWeapon.RelevantSkill != DefaultSkills.Crossbow || !attackerWeapon.IsConsumable || !characterObject.GetPerkValue(DefaultPerks.Crossbow.HammerBolts))
			{
				if (attackerWeapon.RelevantSkill == DefaultSkills.Throwing && attackerWeapon.IsConsumable)
				{
					return characterObject.GetPerkValue(DefaultPerks.Throwing.KnockOff);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public override void CalculateDefendedBlowStunMultipliers(Agent attackerAgent, Agent defenderAgent, CombatCollisionResult collisionResult, WeaponComponentData attackerWeapon, WeaponComponentData defenderWeapon, ref float attackerStunPeriod, ref float defenderStunPeriod)
	{
		float num = 1f;
		float b = 1f;
		if (attackerAgent.Character is CharacterObject characterObject && (collisionResult == CombatCollisionResult.Blocked || collisionResult == CombatCollisionResult.Parried) && characterObject.GetPerkValue(DefaultPerks.Athletics.MightyBlow))
		{
			num += num * DefaultPerks.Athletics.MightyBlow.PrimaryBonus;
		}
		num = MathF.Max(0f, num);
		b = MathF.Max(0f, b);
		attackerStunPeriod *= num;
		defenderStunPeriod *= b;
	}

	public override bool CanWeaponKnockback(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
	{
		if (MBMath.IsBetween((int)collisionData.VictimHitBodyPart, 0, 6) && !attackerWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.CanKnockDown))
		{
			if (!attackerWeapon.IsConsumable && (blow.BlowFlag & BlowFlags.CrushThrough) == 0)
			{
				if (blow.StrikeType == StrikeType.Thrust)
				{
					return blow.WeaponRecord.WeaponFlags.HasAnyFlag(WeaponFlags.WideGrip);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public override bool CanWeaponKnockDown(Agent attackerAgent, Agent victimAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
	{
		if (attackerWeapon.WeaponClass == WeaponClass.Boulder || attackerWeapon.WeaponClass == WeaponClass.BallistaBoulder)
		{
			return true;
		}
		BoneBodyPartType victimHitBodyPart = collisionData.VictimHitBodyPart;
		bool flag = MBMath.IsBetween((int)victimHitBodyPart, 0, 6);
		if (!victimAgent.HasMount && victimHitBodyPart == BoneBodyPartType.Legs)
		{
			flag = true;
		}
		if (flag && blow.WeaponRecord.WeaponFlags.HasAnyFlag(WeaponFlags.CanKnockDown))
		{
			if (!attackerWeapon.IsPolearm || blow.StrikeType != StrikeType.Thrust)
			{
				if (attackerWeapon.IsMeleeWeapon && blow.StrikeType == StrikeType.Swing)
				{
					return MissionCombatMechanicsHelper.DecideSweetSpotCollision(in collisionData);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public override float GetDismountPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
	{
		float num = 0f;
		if (blow.StrikeType == StrikeType.Swing && blow.WeaponRecord.WeaponFlags.HasAnyFlag(WeaponFlags.CanHook))
		{
			num += 0.25f;
		}
		if (attackerWeapon != null && attackerAgent.Character is CharacterObject characterObject)
		{
			if (attackerWeapon.RelevantSkill == DefaultSkills.Polearm && characterObject.GetPerkValue(DefaultPerks.Polearm.Braced))
			{
				num += DefaultPerks.Polearm.Braced.PrimaryBonus;
			}
			else if (attackerWeapon.RelevantSkill == DefaultSkills.Crossbow && attackerWeapon.IsConsumable && characterObject.GetPerkValue(DefaultPerks.Crossbow.HammerBolts))
			{
				num += DefaultPerks.Crossbow.HammerBolts.PrimaryBonus;
			}
			else if (attackerWeapon.RelevantSkill == DefaultSkills.Throwing && attackerWeapon.IsConsumable && characterObject.GetPerkValue(DefaultPerks.Throwing.KnockOff))
			{
				num += DefaultPerks.Throwing.KnockOff.PrimaryBonus;
			}
		}
		return MathF.Max(0f, num);
	}

	public override float GetKnockBackPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
	{
		float num = 0f;
		if (attackerWeapon != null && attackerWeapon.RelevantSkill == DefaultSkills.Polearm && attackerAgent?.Character is CharacterObject characterObject && blow.StrikeType == StrikeType.Thrust && characterObject.GetPerkValue(DefaultPerks.Polearm.KeepAtBay))
		{
			num += DefaultPerks.Polearm.KeepAtBay.PrimaryBonus;
		}
		return num;
	}

	public override float GetKnockDownPenetration(Agent attackerAgent, WeaponComponentData attackerWeapon, in Blow blow, in AttackCollisionData collisionData)
	{
		float num = 0f;
		if (attackerWeapon.WeaponClass == WeaponClass.Boulder || attackerWeapon.WeaponClass == WeaponClass.BallistaBoulder)
		{
			num += 0.25f;
		}
		else if (attackerWeapon.IsMeleeWeapon)
		{
			CharacterObject characterObject = attackerAgent?.Character as CharacterObject;
			if (blow.StrikeType == StrikeType.Swing)
			{
				if (collisionData.VictimHitBodyPart == BoneBodyPartType.Legs)
				{
					num += 0.1f;
				}
				if (characterObject != null && attackerWeapon.RelevantSkill == DefaultSkills.TwoHanded && characterObject.GetPerkValue(DefaultPerks.TwoHanded.ShowOfStrength))
				{
					num += DefaultPerks.TwoHanded.ShowOfStrength.PrimaryBonus;
				}
			}
			if (collisionData.VictimHitBodyPart == BoneBodyPartType.Head)
			{
				num += 0.15f;
			}
			if (characterObject != null && attackerWeapon.RelevantSkill == DefaultSkills.Polearm && characterObject.GetPerkValue(DefaultPerks.Polearm.HardKnock))
			{
				num += DefaultPerks.Polearm.HardKnock.PrimaryBonus;
			}
		}
		return num;
	}

	public override float GetHorseChargePenetration()
	{
		return 0.4f;
	}

	public override float CalculateStaggerThresholdDamage(Agent defenderAgent, in Blow blow)
	{
		float num = 1f;
		CharacterObject characterObject = defenderAgent.Character as CharacterObject;
		CharacterObject characterObject2 = defenderAgent.Formation?.Captain?.Character as CharacterObject;
		if (characterObject != null)
		{
			if (characterObject2 == characterObject)
			{
				characterObject2 = null;
			}
			ExplainedNumber bonuses = new ExplainedNumber(1f);
			if (defenderAgent.HasMount)
			{
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Riding.DauntlessSteed, characterObject, isPrimaryBonus: true, ref bonuses);
			}
			else
			{
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Athletics.Spartan, characterObject, isPrimaryBonus: true, ref bonuses);
			}
			WeaponComponentData currentUsageItem = defenderAgent.WieldedWeapon.CurrentUsageItem;
			if (currentUsageItem != null && currentUsageItem.WeaponClass == WeaponClass.Crossbow && defenderAgent.WieldedWeapon.IsReloading)
			{
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Crossbow.DeftHands, characterObject, isPrimaryBonus: true, ref bonuses);
				if (characterObject2 != null)
				{
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Crossbow.DeftHands, characterObject2, ref bonuses);
				}
			}
			num = bonuses.ResultNumber;
		}
		TaleWorlds.Core.ManagedParametersEnum managedParameterEnum = ((blow.DamageType == DamageTypes.Cut) ? TaleWorlds.Core.ManagedParametersEnum.DamageInterruptAttackThresholdCut : ((blow.DamageType != DamageTypes.Pierce) ? TaleWorlds.Core.ManagedParametersEnum.DamageInterruptAttackThresholdBlunt : TaleWorlds.Core.ManagedParametersEnum.DamageInterruptAttackThresholdPierce));
		return TaleWorlds.Core.ManagedParameters.Instance.GetManagedParameter(managedParameterEnum) * num;
	}

	public override float CalculateAlternativeAttackDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, WeaponComponentData weapon)
	{
		if (weapon == null)
		{
			return 2f;
		}
		if (weapon.WeaponClass == WeaponClass.LargeShield)
		{
			return 2f;
		}
		if (weapon.WeaponClass == WeaponClass.SmallShield)
		{
			return 1f;
		}
		if (weapon.IsTwoHanded)
		{
			return 2f;
		}
		return 1f;
	}

	public override float CalculatePassiveAttackDamage(BasicCharacterObject attackerCharacter, in AttackCollisionData collisionData, float baseDamage)
	{
		if (attackerCharacter is CharacterObject characterObject && collisionData.AttackBlockedWithShield && characterObject.GetPerkValue(DefaultPerks.Polearm.UnstoppableForce))
		{
			baseDamage *= DefaultPerks.Polearm.UnstoppableForce.PrimaryBonus;
		}
		return baseDamage;
	}

	public override MeleeCollisionReaction DecidePassiveAttackCollisionReaction(Agent attacker, Agent defender, bool isFatalHit)
	{
		MeleeCollisionReaction result = MeleeCollisionReaction.Bounced;
		if (isFatalHit && attacker.HasMount)
		{
			float num = 0.05f;
			if (attacker.Character is CharacterObject characterObject && characterObject.GetPerkValue(DefaultPerks.Polearm.Skewer))
			{
				num += DefaultPerks.Polearm.Skewer.PrimaryBonus;
			}
			if (MBRandom.RandomFloat < num)
			{
				result = MeleeCollisionReaction.SlicedThrough;
			}
		}
		return result;
	}

	public override float CalculateShieldDamage(in AttackInformation attackInformation, float baseDamage)
	{
		Formation victimFormation = attackInformation.VictimFormation;
		ExplainedNumber bonuses = new ExplainedNumber(baseDamage);
		BannerComponent activeBanner = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(victimFormation);
		if (activeBanner != null)
		{
			BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.DecreasedShieldDamage, activeBanner, ref bonuses);
		}
		return bonuses.ResultNumber;
	}

	public override float CalculateSailFireDamage(Agent attackerAgent, IShipOrigin shipOrigin, float baseDamage, bool damageFromShipMachine)
	{
		return baseDamage;
	}

	public override float CalculateHullFireDamage(float baseFireDamage, IShipOrigin shipOrigin)
	{
		return new ExplainedNumber(baseFireDamage).ResultNumber;
	}

	public override float GetDamageMultiplierForBodyPart(BoneBodyPartType bodyPart, DamageTypes type, bool isHuman, bool isMissile)
	{
		float result = 1f;
		switch (bodyPart)
		{
		case BoneBodyPartType.None:
			result = 1f;
			break;
		case BoneBodyPartType.Head:
			switch (type)
			{
			case DamageTypes.Invalid:
				result = 1.5f;
				break;
			case DamageTypes.Cut:
				result = 1.2f;
				break;
			case DamageTypes.Pierce:
				result = ((!isHuman) ? 1.2f : (isMissile ? 2f : 1.25f));
				break;
			case DamageTypes.Blunt:
				result = 1.2f;
				break;
			}
			break;
		case BoneBodyPartType.Neck:
			switch (type)
			{
			case DamageTypes.Invalid:
				result = 1.5f;
				break;
			case DamageTypes.Cut:
				result = 1.2f;
				break;
			case DamageTypes.Pierce:
				result = ((!isHuman) ? 1.2f : (isMissile ? 2f : 1.25f));
				break;
			case DamageTypes.Blunt:
				result = 1.2f;
				break;
			}
			break;
		case BoneBodyPartType.Chest:
		case BoneBodyPartType.Abdomen:
		case BoneBodyPartType.ShoulderLeft:
		case BoneBodyPartType.ShoulderRight:
		case BoneBodyPartType.ArmLeft:
		case BoneBodyPartType.ArmRight:
			result = (isHuman ? 1f : 0.8f);
			break;
		case BoneBodyPartType.Legs:
			result = 0.8f;
			break;
		}
		return result;
	}

	public override bool DecideAgentShrugOffBlow(Agent victimAgent, in AttackCollisionData collisionData, in Blow blow)
	{
		return MissionCombatMechanicsHelper.DecideAgentShrugOffBlow(victimAgent, in collisionData, in blow);
	}

	public override bool DecideAgentDismountedByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
	{
		return MissionCombatMechanicsHelper.DecideAgentDismountedByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
	}

	public override bool DecideAgentKnockedBackByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
	{
		return MissionCombatMechanicsHelper.DecideAgentKnockedBackByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
	}

	public override bool DecideAgentKnockedDownByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
	{
		return MissionCombatMechanicsHelper.DecideAgentKnockedDownByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
	}

	public override bool DecideMountRearedByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
	{
		return MissionCombatMechanicsHelper.DecideMountRearedByBlow(attackerAgent, victimAgent, in collisionData, attackerWeapon, in blow);
	}

	public override void DecideWeaponCollisionReaction(in Blow registeredBlow, in AttackCollisionData collisionData, Agent attacker, Agent defender, in MissionWeapon attackerWeapon, bool isFatalHit, bool isShruggedOff, float momentumRemaining, out MeleeCollisionReaction colReaction)
	{
		MissionCombatMechanicsHelper.DecideWeaponCollisionReaction(in registeredBlow, in collisionData, attacker, defender, in attackerWeapon, isFatalHit, isShruggedOff, momentumRemaining, out colReaction);
	}

	public override bool ShouldMissilePassThroughAfterShieldBreak(Agent attackerAgent, WeaponComponentData attackerWeapon)
	{
		return false;
	}

	public override float CalculateRemainingMomentum(float originalMomentum, in Blow b, in AttackCollisionData collisionData, Agent attacker, Agent victim, in MissionWeapon attackerWeapon, bool isCrushThrough)
	{
		return CalculateDefaultRemainingMomentum(originalMomentum, in b, in collisionData, attacker, victim, in attackerWeapon, isCrushThrough);
	}
}
