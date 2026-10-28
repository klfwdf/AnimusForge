using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TacticCharge : TacticComponent
{
	public TacticCharge(Team team)
		: base(team)
	{
	}

	public override void TickOccasionally()
	{
		foreach (Formation item in base.FormationsIncludingEmpty)
		{
			if (item.CountOfUnits > 0)
			{
				item.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(item);
				item.AI.SetBehaviorWeight<BehaviorCharge>(10000f);
			}
		}
		base.TickOccasionally();
	}

	protected internal override void OnApply()
	{
		base.OnApply();
		if (!base.Team.IsPlayerTeam || base.Team.IsPlayerGeneral || !base.Team.IsPlayerSergeant)
		{
			return;
		}
		foreach (Formation item in base.Team.FormationsIncludingSpecialAndEmpty)
		{
			if (item.CountOfUnits <= 0)
			{
				continue;
			}
			foreach (Team team in base.Team.Mission.Teams)
			{
				if (!team.IsEnemyOf(base.Team))
				{
					continue;
				}
				foreach (Formation item2 in team.FormationsIncludingSpecialAndEmpty)
				{
					if (item2.CountOfUnits > 0)
					{
						SoundTacticalHorn(TacticComponent.AttackHornSoundIndex);
						return;
					}
				}
			}
		}
	}

	protected internal override float GetTacticWeight()
	{
		float num = base.Team.QuerySystem.RemainingPowerRatio / base.Team.QuerySystem.TotalPowerRatio;
		float num2 = TaleWorlds.Library.MathF.Max(base.Team.QuerySystem.InfantryRatio, TaleWorlds.Library.MathF.Max(base.Team.QuerySystem.RangedRatio, base.Team.QuerySystem.CavalryRatio)) + ((base.Team.Side == BattleSideEnum.Defender) ? 0.33f : 0f);
		float num3 = ((base.Team.Side == BattleSideEnum.Defender) ? 0.33f : 0.5f);
		float num4 = 0f;
		float num5 = 0f;
		CasualtyHandler missionBehavior = Mission.Current.GetMissionBehavior<CasualtyHandler>();
		foreach (Team team in base.Team.Mission.Teams)
		{
			if (team != base.Team && !team.IsEnemyOf(base.Team))
			{
				continue;
			}
			for (int i = 0; i < Math.Min(team.FormationsIncludingSpecialAndEmpty.Count, 8); i++)
			{
				Formation formation = team.FormationsIncludingSpecialAndEmpty[i];
				if (formation.CountOfUnits > 0)
				{
					num4 += formation.QuerySystem.FormationPower;
					num5 += missionBehavior.GetCasualtyPowerLossOfFormation(formation);
				}
			}
		}
		float num6 = num4 + num5;
		float num7 = num4 / num6;
		num7 = ((base.Team.Side == BattleSideEnum.Attacker && num < 0.5f) ? 0f : MBMath.LinearExtrapolation(0f, 1.6f * num2, (1f - num7) / (1f - num3)));
		float b = MBMath.LinearExtrapolation(0f, 1.6f * num2, base.Team.QuerySystem.RemainingPowerRatio * num3 * 0.5f);
		return TaleWorlds.Library.MathF.Max(num7, b);
	}
}
