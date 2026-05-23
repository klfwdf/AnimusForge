using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace SandBox.GameComponents;

public class SandboxBattleMoraleModel : BattleMoraleModel
{
	public override (float affectedSideMaxMoraleLoss, float affectorSideMaxMoraleGain) CalculateMaxMoraleChangeDueToAgentIncapacitated(Agent affectedAgent, AgentState affectedAgentState, Agent affectorAgent, in KillingBlow killingBlow)
	{
		float battleImportance = affectedAgent.GetBattleImportance();
		BattleSideEnum battleSide = affectedAgent.Team?.Side ?? BattleSideEnum.None;
		float num = CalculateCasualtiesFactor(battleSide);
		CharacterObject characterObject = affectorAgent?.Character as CharacterObject;
		CharacterObject obj = affectedAgent?.Character as CharacterObject;
		SkillObject relevantSkillFromWeaponClass = WeaponComponentData.GetRelevantSkillFromWeaponClass((WeaponClass)killingBlow.WeaponClass);
		bool flag = relevantSkillFromWeaponClass == DefaultSkills.OneHanded || relevantSkillFromWeaponClass == DefaultSkills.TwoHanded || relevantSkillFromWeaponClass == DefaultSkills.Polearm;
		bool flag2 = relevantSkillFromWeaponClass == DefaultSkills.Bow || relevantSkillFromWeaponClass == DefaultSkills.Crossbow || relevantSkillFromWeaponClass == DefaultSkills.Throwing;
		bool num2 = killingBlow.WeaponRecordWeaponFlags.HasAnyFlag(WeaponFlags.AffectsArea | WeaponFlags.AffectsAreaBig | WeaponFlags.MultiplePenetration);
		float num3 = 0.75f;
		if (num2)
		{
			num3 = 0.25f;
			if (killingBlow.WeaponRecordWeaponFlags.HasAllFlags(WeaponFlags.Burning | WeaponFlags.MultiplePenetration))
			{
				num3 += num3 * 0.25f;
			}
		}
		else if (flag2)
		{
			num3 = 0.5f;
		}
		num3 = Math.Max(0f, num3);
		ExplainedNumber bonuses = new ExplainedNumber(battleImportance * 3f * num3);
		ExplainedNumber bonuses2 = new ExplainedNumber(battleImportance * 4f * num3 * num);
		if (characterObject != null)
		{
			CharacterObject captainCharacter = affectorAgent?.Formation?.Captain?.Character as CharacterObject;
			PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Leadership.MakeADifference, characterObject, isPrimaryBonus: true, ref bonuses);
			if (flag)
			{
				if (relevantSkillFromWeaponClass == DefaultSkills.TwoHanded)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.TwoHanded.Hope, characterObject, isPrimaryBonus: true, ref bonuses);
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.TwoHanded.Terror, characterObject, isPrimaryBonus: true, ref bonuses2);
				}
				if (affectorAgent != null && affectorAgent.HasMount)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Riding.ThunderousCharge, characterObject, isPrimaryBonus: true, ref bonuses2);
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Riding.ThunderousCharge, captainCharacter, ref bonuses2);
				}
			}
			else if (flag2)
			{
				if (relevantSkillFromWeaponClass == DefaultSkills.Crossbow)
				{
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Crossbow.Terror, captainCharacter, ref bonuses2);
				}
				if (affectorAgent != null && affectorAgent.HasMount)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Riding.AnnoyingBuzz, characterObject, isPrimaryBonus: true, ref bonuses2);
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Riding.AnnoyingBuzz, captainCharacter, ref bonuses2);
				}
			}
			PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Leadership.HeroicLeader, captainCharacter, ref bonuses2);
		}
		if (obj != null)
		{
			MobileParty mobileParty = (affectedAgent?.Origin?.BattleCombatant as PartyBase)?.MobileParty;
			if (affectedAgentState == AgentState.Unconscious && mobileParty != null && mobileParty.HasPerk(DefaultPerks.Medicine.HealthAdvise, checkSecondaryRole: true))
			{
				bonuses2 = default(ExplainedNumber);
			}
			else
			{
				if (affectedAgent.Formation?.Captain?.Character is CharacterObject captainCharacter2)
				{
					ArrangementOrder arrangementOrder = affectedAgent.Formation.ArrangementOrder;
					if (arrangementOrder == ArrangementOrder.ArrangementOrderShieldWall || arrangementOrder == ArrangementOrder.ArrangementOrderSquare || arrangementOrder == ArrangementOrder.ArrangementOrderSkein || arrangementOrder == ArrangementOrder.ArrangementOrderColumn)
					{
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Tactics.TightFormations, captainCharacter2, ref bonuses2);
					}
					if (arrangementOrder == ArrangementOrder.ArrangementOrderLine || arrangementOrder == ArrangementOrder.ArrangementOrderLoose || arrangementOrder == ArrangementOrder.ArrangementOrderCircle || arrangementOrder == ArrangementOrder.ArrangementOrderScatter)
					{
						PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Tactics.LooseFormations, captainCharacter2, ref bonuses2);
					}
					PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Polearm.StandardBearer, captainCharacter2, ref bonuses2);
				}
				Hero hero = mobileParty?.EffectiveQuartermaster;
				if (hero != null)
				{
					PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Steward.PriceOfLoyalty, hero.CharacterObject, DefaultSkills.Steward, applyPrimaryBonus: true, ref bonuses2, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus);
				}
			}
		}
		Formation formation = affectedAgent.Formation;
		BannerComponent activeBanner = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(formation);
		if (activeBanner != null)
		{
			BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.DecreasedMoraleShock, activeBanner, ref bonuses2);
		}
		Formation formation2 = affectorAgent.Formation;
		BannerComponent activeBanner2 = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(formation2);
		if (activeBanner2 != null && affectorAgent.Character.DefaultFormationClass == FormationClass.Infantry && flag)
		{
			BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.IncreasedMoraleShockByMeleeTroops, activeBanner2, ref bonuses);
		}
		return (affectedSideMaxMoraleLoss: TaleWorlds.Library.MathF.Max(bonuses2.ResultNumber, 0f), affectorSideMaxMoraleGain: TaleWorlds.Library.MathF.Max(bonuses.ResultNumber, 0f));
	}

	public override (float affectedSideMaxMoraleLoss, float affectorSideMaxMoraleGain) CalculateMaxMoraleChangeDueToAgentPanicked(Agent agent)
	{
		float battleImportance = agent.GetBattleImportance();
		BattleSideEnum battleSide = agent.Team?.Side ?? BattleSideEnum.None;
		float num = CalculateCasualtiesFactor(battleSide);
		float a = battleImportance * 2f;
		float num2 = battleImportance * num * 1.1f;
		if (agent?.Character is CharacterObject)
		{
			ExplainedNumber bonuses = new ExplainedNumber(num2);
			Formation formation = agent.Formation;
			CharacterObject characterObject = formation?.Captain?.Character as CharacterObject;
			BannerComponent activeBanner = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(formation);
			if (characterObject != null)
			{
				PerkHelper.AddPerkBonusFromCaptain(DefaultPerks.Polearm.StandardBearer, characterObject, ref bonuses);
			}
			Hero hero = ((agent?.Origin?.BattleCombatant as PartyBase)?.MobileParty)?.EffectiveQuartermaster;
			if (hero != null)
			{
				PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Steward.PriceOfLoyalty, hero.CharacterObject, DefaultSkills.Steward, applyPrimaryBonus: true, ref bonuses, Campaign.Current.Models.CharacterDevelopmentModel.MaxSkillRequiredForEpicPerkBonus);
			}
			if (activeBanner != null)
			{
				BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.DecreasedMoraleShock, activeBanner, ref bonuses);
			}
			num2 = bonuses.ResultNumber;
		}
		return (affectedSideMaxMoraleLoss: TaleWorlds.Library.MathF.Max(num2, 0f), affectorSideMaxMoraleGain: TaleWorlds.Library.MathF.Max(a, 0f));
	}

	public override float CalculateMoraleChangeToCharacter(Agent agent, float maxMoraleChange)
	{
		return maxMoraleChange / TaleWorlds.Library.MathF.Max(1f, agent.Character.GetMoraleResistance());
	}

	public override float GetEffectiveInitialMorale(Agent agent, float baseMorale)
	{
		ExplainedNumber stat = new ExplainedNumber(baseMorale);
		PartyBase partyBase = (PartyBase)(agent?.Origin?.BattleCombatant);
		MobileParty mobileParty = ((partyBase != null && partyBase.IsMobile) ? partyBase.MobileParty : null);
		CharacterObject characterObject = agent?.Character as CharacterObject;
		if (mobileParty != null && characterObject != null)
		{
			CharacterObject characterObject2 = mobileParty.Army?.LeaderParty?.LeaderHero?.CharacterObject;
			CharacterObject characterObject3 = mobileParty.LeaderHero?.CharacterObject;
			characterObject2 = ((characterObject2 != characterObject) ? characterObject2 : null);
			characterObject3 = ((characterObject3 != characterObject) ? characterObject3 : null);
			if (characterObject3 != null)
			{
				if (partyBase.Side == BattleSideEnum.Attacker)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.FerventAttacker, mobileParty, isPrimaryBonus: true, ref stat);
				}
				else if (partyBase.Side == BattleSideEnum.Defender)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.StoutDefender, mobileParty, isPrimaryBonus: true, ref stat);
				}
				if (characterObject3.Culture == characterObject.Culture)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.GreatLeader, mobileParty, isPrimaryBonus: false, ref stat);
				}
				if (characterObject3.GetPerkValue(DefaultPerks.Leadership.WePledgeOurSwords))
				{
					int num = TaleWorlds.Library.MathF.Min(partyBase.GetNumberOfHealthyMenOfTier(6), 10);
					stat.Add(num);
				}
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Throwing.LastHit, mobileParty, isPrimaryBonus: false, ref stat);
				PartyBase partyBase2 = partyBase?.MapEventSide?.LeaderParty;
				if (partyBase2 != null && partyBase != partyBase2)
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Riding.ReliefForce, mobileParty, isPrimaryBonus: true, ref stat);
				}
				if (partyBase.MapEvent != null)
				{
					partyBase.MapEvent.GetStrengthsRelativeToParty(partyBase.Side, out var partySideStrength, out var opposingSideStrength);
					if (partySideStrength < opposingSideStrength)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.OneHanded.StandUnited, mobileParty, isPrimaryBonus: true, ref stat);
					}
					if (partyBase.MapEvent.IsSiegeAssault || partyBase.MapEvent.IsSiegeOutside)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.UpliftingSpirit, mobileParty, isPrimaryBonus: true, ref stat);
					}
					bool flag = false;
					foreach (PartyBase involvedParty in partyBase.MapEvent.InvolvedParties)
					{
						if (involvedParty.Side != partyBase.Side && involvedParty.MapFaction != null && involvedParty.Culture.IsBandit)
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						PerkHelper.AddPerkBonusForParty(DefaultPerks.Scouting.Patrols, mobileParty, isPrimaryBonus: true, ref stat);
					}
				}
				PerkHelper.AddPerkBonusForParty(DefaultPerks.OneHanded.LeadByExample, mobileParty, isPrimaryBonus: false, ref stat, mobileParty.IsCurrentlyAtSea);
			}
			if (characterObject2 != null && characterObject2.GetPerkValue(DefaultPerks.Leadership.GreatLeader))
			{
				stat.Add(DefaultPerks.Leadership.GreatLeader.PrimaryBonus);
			}
			if (characterObject.IsRanged)
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Bow.RenownedArcher, partyBase.MobileParty, isPrimaryBonus: true, ref stat);
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Crossbow.Marksmen, partyBase.MobileParty, isPrimaryBonus: false, ref stat);
			}
			if (mobileParty.IsDisorganized && (mobileParty.MapEvent == null || mobileParty.SiegeEvent == null || mobileParty.MapEventSide.MissionSide != BattleSideEnum.Attacker) && (characterObject3 == null || !characterObject3.GetPerkValue(DefaultPerks.Tactics.Improviser)))
			{
				stat.AddFactor(-0.2f);
			}
		}
		return stat.ResultNumber;
	}

	public override bool CanPanicDueToMorale(Agent agent)
	{
		bool result = true;
		if (agent.IsHuman)
		{
			CharacterObject characterObject = agent.Character as CharacterObject;
			Hero hero = ((PartyBase)(agent.Origin?.BattleCombatant))?.LeaderHero;
			if (characterObject != null && hero != null && characterObject.Tier >= (int)DefaultPerks.Leadership.LoyaltyAndHonor.PrimaryBonus && hero.GetPerkValue(DefaultPerks.Leadership.LoyaltyAndHonor))
			{
				result = false;
			}
		}
		return result;
	}

	public override float CalculateCasualtiesFactor(BattleSideEnum battleSide)
	{
		float num = 1f;
		if (Mission.Current != null && battleSide != BattleSideEnum.None)
		{
			float removedAgentRatioForSide = Mission.Current.GetRemovedAgentRatioForSide(battleSide);
			num += removedAgentRatioForSide * 2f;
			num = TaleWorlds.Library.MathF.Max(0f, num);
		}
		return num;
	}

	public override float GetAverageMorale(Formation formation)
	{
		float num = 0f;
		int num2 = 0;
		if (formation != null)
		{
			foreach (IFormationUnit allUnit in formation.Arrangement.GetAllUnits())
			{
				if (allUnit is Agent { IsHuman: not false, IsAIControlled: not false } agent)
				{
					num2++;
					num += agent.GetMorale();
				}
			}
		}
		if (num2 > 0)
		{
			return MBMath.ClampFloat(num / (float)num2, 0f, 100f);
		}
		return 0f;
	}

	public override float CalculateMoraleChangeOnShipSunk(IShipOrigin shipOrigin)
	{
		return 0f;
	}

	public override float CalculateMoraleOnRamming(Agent agent, IShipOrigin rammingShip, IShipOrigin rammedShip)
	{
		return agent.GetMorale();
	}

	public override float CalculateMoraleOnShipsConnected(Agent agent, IShipOrigin ownerShip, IShipOrigin targetShip)
	{
		return agent.GetMorale();
	}
}
