using System;
using MBHelpers;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace TaleWorlds.MountAndBlade;

public class CustomBattleMoraleModel : BattleMoraleModel
{
	public override (float affectedSideMaxMoraleLoss, float affectorSideMaxMoraleGain) CalculateMaxMoraleChangeDueToAgentIncapacitated(Agent affectedAgent, AgentState affectedAgentState, Agent affectorAgent, in KillingBlow killingBlow)
	{
		float battleImportance = affectedAgent.GetBattleImportance();
		BattleSideEnum battleSide = affectedAgent.Team?.Side ?? BattleSideEnum.None;
		float num = CalculateCasualtiesFactor(battleSide);
		SkillObject relevantSkillFromWeaponClass = WeaponComponentData.GetRelevantSkillFromWeaponClass((WeaponClass)killingBlow.WeaponClass);
		bool flag = relevantSkillFromWeaponClass == DefaultSkills.Bow || relevantSkillFromWeaponClass == DefaultSkills.Crossbow || relevantSkillFromWeaponClass == DefaultSkills.Throwing;
		bool flag2 = relevantSkillFromWeaponClass == DefaultSkills.OneHanded || relevantSkillFromWeaponClass == DefaultSkills.TwoHanded || relevantSkillFromWeaponClass == DefaultSkills.Polearm;
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
		else if (flag)
		{
			num3 = 0.5f;
		}
		num3 = Math.Max(0f, num3);
		FactoredNumber bonuses = new FactoredNumber(battleImportance * 3f * num3);
		FactoredNumber bonuses2 = new FactoredNumber(battleImportance * 4f * num3 * num);
		Formation formation = affectedAgent.Formation;
		BannerComponent activeBanner = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(formation);
		if (activeBanner != null)
		{
			BannerHelper.AddBannerBonusForBanner(DefaultBannerEffects.DecreasedMoraleShock, activeBanner, ref bonuses2);
		}
		Formation formation2 = affectorAgent.Formation;
		BannerComponent activeBanner2 = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(formation2);
		if (activeBanner2 != null && affectorAgent.Character.DefaultFormationClass == FormationClass.Infantry && flag2)
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
		if (agent.Character != null)
		{
			FactoredNumber bonuses = new FactoredNumber(num2);
			Formation formation = agent.Formation;
			BannerComponent activeBanner = MissionGameModels.Current.BattleBannerBearersModel.GetActiveBanner(formation);
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
		return baseMorale;
	}

	public override bool CanPanicDueToMorale(Agent agent)
	{
		return true;
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
				if (allUnit is Agent agent && agent.IsActive() && agent.IsHuman && agent.IsAIControlled)
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
