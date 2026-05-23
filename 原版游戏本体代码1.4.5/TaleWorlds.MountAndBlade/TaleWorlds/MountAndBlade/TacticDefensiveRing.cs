using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TacticDefensiveRing : TacticComponent
{
	private const float DefendersAdvantage = 1.5f;

	private TacticalPosition _mainRingPosition;

	public TacticDefensiveRing(Team team)
		: base(team)
	{
	}

	protected override void ManageFormationCounts()
	{
		AssignTacticFormations1121();
	}

	private void Defend()
	{
		if (base.Team.IsPlayerTeam && !base.Team.IsPlayerGeneral && base.Team.IsPlayerSergeant)
		{
			SoundTacticalHorn(TacticComponent.MoveHornSoundIndex);
		}
		if (_mainInfantry != null)
		{
			_mainInfantry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_mainInfantry);
			_mainInfantry.AI.SetBehaviorWeight<BehaviorDefensiveRing>(1f).TacticalDefendPosition = _mainRingPosition;
		}
		if (_archers != null && _mainInfantry != null)
		{
			_archers.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_archers);
			_archers.AI.SetBehaviorWeight<BehaviorFireFromInfantryCover>(1f);
			_archers.AI.SetBehaviorWeight<BehaviorSkirmish>(1f);
		}
		if (_leftCavalry != null)
		{
			_leftCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_leftCavalry);
			_leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Left;
			_leftCavalry.AI.SetBehaviorWeight<BehaviorCavalryScreen>(1f);
		}
		if (_rightCavalry != null)
		{
			_rightCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_rightCavalry);
			_rightCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Right;
			_rightCavalry.AI.SetBehaviorWeight<BehaviorCavalryScreen>(1f);
		}
		if (_rangedCavalry != null)
		{
			_rangedCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_rangedCavalry);
			_rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
			_rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
		}
	}

	protected override bool CheckAndSetAvailableFormationsChanged()
	{
		int aIControlledFormationCount = base.Team.GetAIControlledFormationCount();
		bool num = aIControlledFormationCount != _AIControlledFormationCount;
		if (num)
		{
			_AIControlledFormationCount = aIControlledFormationCount;
			IsTacticReapplyNeeded = true;
		}
		if (!num)
		{
			if ((_mainInfantry == null || (_mainInfantry.CountOfUnits != 0 && _mainInfantry.QuerySystem.IsInfantryFormation)) && (_archers == null || (_archers.CountOfUnits != 0 && _archers.QuerySystem.IsRangedFormation)) && (_leftCavalry == null || (_leftCavalry.CountOfUnits != 0 && _leftCavalry.QuerySystem.IsCavalryFormation)) && (_rightCavalry == null || (_rightCavalry.CountOfUnits != 0 && _rightCavalry.QuerySystem.IsCavalryFormation)))
			{
				if (_rangedCavalry != null)
				{
					if (_rangedCavalry.CountOfUnits != 0)
					{
						return !_rangedCavalry.QuerySystem.IsRangedCavalryFormation;
					}
					return true;
				}
				return false;
			}
			return true;
		}
		return true;
	}

	public override void TickOccasionally()
	{
		if (base.AreFormationsCreated)
		{
			if (CheckAndSetAvailableFormationsChanged() || IsTacticReapplyNeeded)
			{
				ManageFormationCounts();
				Defend();
				IsTacticReapplyNeeded = false;
			}
			base.TickOccasionally();
		}
	}

	protected internal override bool ResetTacticalPositions()
	{
		DetermineRingPosition();
		return true;
	}

	protected internal override float GetTacticWeight()
	{
		if (!base.Team.TeamAI.IsDefenseApplicable || !CheckAndDetermineFormation(ref _mainInfantry, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation) || !CheckAndDetermineFormation(ref _archers, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsRangedFormation))
		{
			return 0f;
		}
		float num = (float)TaleWorlds.Library.MathF.Max(0, _mainInfantry.CountOfUnits) * (_mainInfantry.MaximumInterval + _mainInfantry.UnitDiameter) / (System.MathF.PI * 2f);
		float num2 = TaleWorlds.Library.MathF.Sqrt(_archers.CountOfUnits);
		float num3 = _archers.UnitDiameter * num2 + _archers.Interval * (num2 - 1f);
		if (num < num3)
		{
			return 0f;
		}
		if (!base.Team.TeamAI.IsCurrentTactic(this) || _mainRingPosition == null || !IsTacticalPositionEligible(_mainRingPosition))
		{
			DetermineRingPosition();
		}
		if (_mainRingPosition == null)
		{
			return 0f;
		}
		return TaleWorlds.Library.MathF.Min(base.Team.QuerySystem.InfantryRatio, base.Team.QuerySystem.RangedRatio) * 2f * 1.5f * GetTacticalPositionScore(_mainRingPosition) * TacticComponent.CalculateNotEngagingTacticalAdvantage(base.Team.QuerySystem) / TaleWorlds.Library.MathF.Sqrt(base.Team.QuerySystem.RemainingPowerRatio);
	}

	private bool IsTacticalPositionEligible(TacticalPosition tacticalPosition)
	{
		float num = _mainInfantry?.CachedAveragePosition.Distance(tacticalPosition.Position.AsVec2) ?? base.Team.QuerySystem.AveragePosition.Distance(tacticalPosition.Position.AsVec2);
		float num2 = base.Team.QuerySystem.AverageEnemyPosition.Distance(_mainInfantry?.CachedAveragePosition ?? base.Team.QuerySystem.AveragePosition);
		if ((num > 20f && num > num2 * 0.5f) || !tacticalPosition.IsInsurmountable)
		{
			return false;
		}
		return true;
	}

	private float GetTacticalPositionScore(TacticalPosition tacticalPosition)
	{
		if (CheckAndDetermineFormation(ref _mainInfantry, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation) && CheckAndDetermineFormation(ref _archers, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsRangedFormation))
		{
			float num = MBMath.Lerp(1f, 1.5f, MBMath.ClampFloat(tacticalPosition.Slope, 0f, 60f) / 60f);
			Formation formation = TaleWorlds.Core.Extensions.MaxBy(base.Team.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsRangedFormation), (Formation f) => f.CountOfUnits);
			float num2 = TaleWorlds.Library.MathF.Max(formation.Arrangement.RankDepth, formation.Arrangement.FlankWidth);
			float num3 = MBMath.ClampFloat(tacticalPosition.Width / num2, 0.7f, 1f);
			float num4 = (tacticalPosition.IsInsurmountable ? 1.5f : 1f);
			float cavalryFactor = GetCavalryFactor(tacticalPosition);
			float value = _mainInfantry.CachedAveragePosition.Distance(tacticalPosition.Position.AsVec2);
			float num5 = MBMath.Lerp(0.7f, 1f, (150f - MBMath.ClampFloat(value, 50f, 150f)) / 100f);
			return num * num3 * num4 * cavalryFactor * num5;
		}
		return 0f;
	}

	private List<TacticalPosition> ExtractPossibleTacticalPositionsFromTacticalRegion(TacticalRegion tacticalRegion)
	{
		List<TacticalPosition> list = new List<TacticalPosition>();
		foreach (TacticalPosition linkedTacticalPosition in tacticalRegion.LinkedTacticalPositions)
		{
			if (linkedTacticalPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.HighGround)
			{
				list.Add(linkedTacticalPosition);
			}
		}
		if (tacticalRegion.tacticalRegionType == TacticalRegion.TacticalRegionTypeEnum.DifficultTerrain || tacticalRegion.tacticalRegionType == TacticalRegion.TacticalRegionTypeEnum.Opening)
		{
			Vec2 direction = (base.Team.QuerySystem.AverageEnemyPosition - tacticalRegion.Position.AsVec2).Normalized();
			TacticalPosition item = new TacticalPosition(tacticalRegion.Position, direction, tacticalRegion.radius, 0f, isInsurmountable: true, TacticalPosition.TacticalPositionTypeEnum.Regional, tacticalRegion.tacticalRegionType);
			list.Add(item);
		}
		return list;
	}

	private float GetCavalryFactor(TacticalPosition tacticalPosition)
	{
		if (tacticalPosition.TacticalRegionMembership == TacticalRegion.TacticalRegionTypeEnum.Opening)
		{
			return 1f;
		}
		float teamPower = base.Team.QuerySystem.TeamPower;
		float num = 0f;
		foreach (Team team in base.Team.Mission.Teams)
		{
			if (team.IsEnemyOf(base.Team))
			{
				num += team.QuerySystem.TeamPower;
			}
		}
		teamPower -= teamPower * (base.Team.QuerySystem.CavalryRatio + base.Team.QuerySystem.RangedCavalryRatio) * 0.5f;
		num -= num * (base.Team.QuerySystem.EnemyCavalryRatio + base.Team.QuerySystem.EnemyRangedCavalryRatio) * 0.5f;
		return teamPower / num / base.Team.QuerySystem.RemainingPowerRatio;
	}

	private void DetermineRingPosition()
	{
		IEnumerable<(TacticalPosition tp, float)> first = from tp in base.Team.TeamAI.TacticalPositions
			where tp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.HighGround && IsTacticalPositionEligible(tp)
			select (tp: tp, GetTacticalPositionScore(tp));
		IEnumerable<(TacticalPosition, float)> second = from tp in base.Team.TeamAI.TacticalRegions.SelectMany((TacticalRegion r) => ExtractPossibleTacticalPositionsFromTacticalRegion(r))
			where (tp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.Regional || tp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.HighGround) && IsTacticalPositionEligible(tp)
			select (tp: tp, GetTacticalPositionScore(tp));
		List<(TacticalPosition, float)> list = first.Concat<(TacticalPosition, float)>(second).ToList();
		if (list.Count > 0)
		{
			TacticalPosition item = TaleWorlds.Core.Extensions.MaxBy<(TacticalPosition, float), float>(list, ((TacticalPosition tp, float) pst) => pst.Item2).Item1;
			if (item != _mainRingPosition)
			{
				_mainRingPosition = item;
				IsTacticReapplyNeeded = true;
			}
		}
		else
		{
			_mainRingPosition = null;
		}
	}
}
