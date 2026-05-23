using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TacticDefensiveLine : TacticComponent
{
	private bool _hasBattleBeenJoined;

	private const float DefendersAdvantage = 1.2f;

	private TacticalPosition _mainDefensiveLineObject;

	private TacticalPosition _linkedRangedDefensivePosition;

	public TacticDefensiveLine(Team team)
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
			_mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1f).TacticalDefendPosition = _mainDefensiveLineObject;
			_mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
		}
		if (_archers != null)
		{
			_archers.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_archers);
			_archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(1f);
			_archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
			if (_linkedRangedDefensivePosition != null)
			{
				_archers.AI.SetBehaviorWeight<BehaviorDefend>(10f).TacticalDefendPosition = _linkedRangedDefensivePosition;
			}
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

	private void Engage()
	{
		if (base.Team.IsPlayerTeam && !base.Team.IsPlayerGeneral && base.Team.IsPlayerSergeant)
		{
			SoundTacticalHorn(TacticComponent.AttackHornSoundIndex);
		}
		if (_mainInfantry != null)
		{
			_mainInfantry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_mainInfantry);
			_mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1f).TacticalDefendPosition = _mainDefensiveLineObject;
			_mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
		}
		if (_archers != null)
		{
			_archers.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_archers);
			_archers.AI.SetBehaviorWeight<BehaviorSkirmish>(1f);
			_archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
			if (_linkedRangedDefensivePosition != null)
			{
				_archers.AI.SetBehaviorWeight<BehaviorShootFromCliff>(1f).SetTacticalDefendPosition(_linkedRangedDefensivePosition);
			}
		}
		if (_leftCavalry != null)
		{
			_leftCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_leftCavalry);
			_leftCavalry.AI.SetBehaviorWeight<BehaviorFlank>(1f);
			_leftCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
		}
		if (_rightCavalry != null)
		{
			_rightCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_rightCavalry);
			_rightCavalry.AI.SetBehaviorWeight<BehaviorFlank>(1f);
			_rightCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
		}
		if (_rangedCavalry != null)
		{
			_rangedCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_rangedCavalry);
			_rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
			_rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
		}
	}

	private bool HasBattleBeenJoined()
	{
		if (_mainInfantry?.CachedClosestEnemyFormation != null && !(_mainInfantry.AI.ActiveBehavior is BehaviorCharge) && !(_mainInfantry.AI.ActiveBehavior is BehaviorTacticalCharge))
		{
			return _mainInfantry.CachedMedianPosition.AsVec2.Distance(_mainInfantry.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2) / _mainInfantry.CachedClosestEnemyFormation.MovementSpeedMaximum <= 5f + (_hasBattleBeenJoined ? 5f : 0f);
		}
		return true;
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
		if (!base.AreFormationsCreated)
		{
			return;
		}
		bool flag = HasBattleBeenJoined();
		if (CheckAndSetAvailableFormationsChanged())
		{
			_hasBattleBeenJoined = flag;
			ManageFormationCounts();
			if (_hasBattleBeenJoined)
			{
				Engage();
			}
			else
			{
				Defend();
			}
			IsTacticReapplyNeeded = false;
		}
		if (flag != _hasBattleBeenJoined || IsTacticReapplyNeeded)
		{
			_hasBattleBeenJoined = flag;
			if (_hasBattleBeenJoined)
			{
				Engage();
			}
			else
			{
				Defend();
			}
			IsTacticReapplyNeeded = false;
		}
		base.TickOccasionally();
	}

	protected internal override bool ResetTacticalPositions()
	{
		DetermineMainDefensiveLine();
		return true;
	}

	protected internal override float GetTacticWeight()
	{
		if (!base.Team.TeamAI.IsDefenseApplicable || !CheckAndDetermineFormation(ref _mainInfantry, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation))
		{
			return 0f;
		}
		if (!base.Team.TeamAI.IsCurrentTactic(this) || _mainDefensiveLineObject == null || !IsTacticalPositionEligible(_mainDefensiveLineObject))
		{
			DetermineMainDefensiveLine();
		}
		if (_mainDefensiveLineObject == null)
		{
			return 0f;
		}
		return (base.Team.QuerySystem.InfantryRatio + base.Team.QuerySystem.RangedRatio) * 1.2f * GetTacticalPositionScore(_mainDefensiveLineObject) * TacticComponent.CalculateNotEngagingTacticalAdvantage(base.Team.QuerySystem) / TaleWorlds.Library.MathF.Sqrt(base.Team.QuerySystem.RemainingPowerRatio);
	}

	private bool IsTacticalPositionEligible(TacticalPosition tacticalPosition)
	{
		if (tacticalPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.SpecialMissionPosition)
		{
			return true;
		}
		float num = _mainInfantry?.CachedAveragePosition.Distance(tacticalPosition.Position.AsVec2) ?? base.Team.QuerySystem.AveragePosition.Distance(tacticalPosition.Position.AsVec2);
		float num2 = base.Team.QuerySystem.AverageEnemyPosition.Distance(_mainInfantry?.CachedAveragePosition ?? base.Team.QuerySystem.AveragePosition);
		if (num > 20f && num > num2 * 0.5f)
		{
			return false;
		}
		if (!tacticalPosition.IsInsurmountable)
		{
			return (base.Team.QuerySystem.AverageEnemyPosition - tacticalPosition.Position.AsVec2).Normalized().DotProduct(tacticalPosition.Direction) > 0.5f;
		}
		return true;
	}

	private float GetTacticalPositionScore(TacticalPosition tacticalPosition)
	{
		if (tacticalPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.SpecialMissionPosition)
		{
			return 100f;
		}
		if (CheckAndDetermineFormation(ref _mainInfantry, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation))
		{
			float num = MBMath.Lerp(1f, 1.5f, MBMath.ClampFloat(tacticalPosition.Slope, 0f, 60f) / 60f);
			float maximumWidth = _mainInfantry.MaximumWidth;
			float num2 = MBMath.Lerp(0.67f, 1f, (6f - MBMath.ClampFloat(maximumWidth / tacticalPosition.Width, 3f, 6f)) / 3f);
			float num3 = (tacticalPosition.IsInsurmountable ? 1.3f : 1f);
			float num4 = 1f;
			if (_archers != null && tacticalPosition.LinkedTacticalPositions.Where((TacticalPosition lcp) => lcp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.Cliff).ToList().Count > 0)
			{
				num4 = MBMath.Lerp(1f, 1.5f, (MBMath.ClampFloat(base.Team.QuerySystem.RangedRatio, 0.05f, 0.25f) - 0.05f) * 5f);
			}
			float rangedFactor = GetRangedFactor(tacticalPosition);
			float cavalryFactor = GetCavalryFactor(tacticalPosition);
			float value = _mainInfantry.CachedAveragePosition.Distance(tacticalPosition.Position.AsVec2);
			float num5 = MBMath.Lerp(0.7f, 1f, (150f - MBMath.ClampFloat(value, 50f, 150f)) / 100f);
			return num * num2 * num4 * rangedFactor * cavalryFactor * num5 * num3;
		}
		return 0f;
	}

	private List<TacticalPosition> ExtractPossibleTacticalPositionsFromTacticalRegion(TacticalRegion tacticalRegion)
	{
		List<TacticalPosition> list = new List<TacticalPosition>();
		if (tacticalRegion.tacticalRegionType == TacticalRegion.TacticalRegionTypeEnum.Forest)
		{
			Vec2 vec = (base.Team.QuerySystem.AverageEnemyPosition - tacticalRegion.Position.AsVec2).Normalized();
			TacticalPosition item = new TacticalPosition(tacticalRegion.Position, vec, tacticalRegion.radius, 0f, isInsurmountable: false, TacticalPosition.TacticalPositionTypeEnum.Regional, TacticalRegion.TacticalRegionTypeEnum.Forest);
			list.Add(item);
			float num = tacticalRegion.radius * 0.87f;
			TacticalPosition item2 = new TacticalPosition(new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, tacticalRegion.Position.GetNavMeshVec3() + new Vec3(num * vec), hasValidZ: false), vec, tacticalRegion.radius, 0f, isInsurmountable: false, TacticalPosition.TacticalPositionTypeEnum.Regional, TacticalRegion.TacticalRegionTypeEnum.Forest);
			list.Add(item2);
			TacticalPosition item3 = new TacticalPosition(new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, tacticalRegion.Position.GetNavMeshVec3() - new Vec3(num * vec), hasValidZ: false), vec, tacticalRegion.radius, 0f, isInsurmountable: false, TacticalPosition.TacticalPositionTypeEnum.Regional, TacticalRegion.TacticalRegionTypeEnum.Forest);
			list.Add(item3);
		}
		return list;
	}

	private float GetCavalryFactor(TacticalPosition tacticalPosition)
	{
		if (tacticalPosition.TacticalRegionMembership != TacticalRegion.TacticalRegionTypeEnum.Forest)
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
		if (num <= 0f)
		{
			num = 0.01f;
		}
		return teamPower / num / base.Team.QuerySystem.RemainingPowerRatio;
	}

	private float GetRangedFactor(TacticalPosition tacticalPosition)
	{
		bool isOuterEdge = tacticalPosition.IsOuterEdge;
		if (tacticalPosition.TacticalRegionMembership != TacticalRegion.TacticalRegionTypeEnum.Forest)
		{
			return 1f;
		}
		float num = base.Team.QuerySystem.TeamPower;
		float num2 = 0f;
		foreach (Team team in base.Team.Mission.Teams)
		{
			if (team.IsEnemyOf(base.Team))
			{
				num2 += team.QuerySystem.TeamPower;
			}
		}
		num2 -= num2 * (base.Team.QuerySystem.EnemyRangedRatio + base.Team.QuerySystem.EnemyRangedCavalryRatio) * 0.5f;
		if (num2 <= 0f)
		{
			num2 = 0.01f;
		}
		if (!isOuterEdge)
		{
			num -= num * (base.Team.QuerySystem.RangedRatio + base.Team.QuerySystem.RangedCavalryRatio) * 0.5f;
		}
		return num / num2 / base.Team.QuerySystem.RemainingPowerRatio;
	}

	private void DetermineMainDefensiveLine()
	{
		IEnumerable<(TacticalPosition tp, float)> first = from tp in base.Team.TeamAI.TacticalPositions
			where (tp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.SpecialMissionPosition || tp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.HighGround) && IsTacticalPositionEligible(tp)
			select (tp: tp, GetTacticalPositionScore(tp));
		IEnumerable<(TacticalPosition, float)> second = from tp in base.Team.TeamAI.TacticalRegions.SelectMany((TacticalRegion r) => ExtractPossibleTacticalPositionsFromTacticalRegion(r))
			where (tp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.Regional || tp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.HighGround) && IsTacticalPositionEligible(tp)
			select (tp: tp, GetTacticalPositionScore(tp));
		List<(TacticalPosition, float)> list = first.Concat<(TacticalPosition, float)>(second).ToList();
		if (list.Count > 0)
		{
			TacticalPosition item = TaleWorlds.Core.Extensions.MaxBy<(TacticalPosition, float), float>(list, ((TacticalPosition tp, float) pst) => pst.Item2).Item1;
			if (item != _mainDefensiveLineObject)
			{
				_mainDefensiveLineObject = item;
				IsTacticReapplyNeeded = true;
			}
			if (_mainDefensiveLineObject.LinkedTacticalPositions.Count > 0)
			{
				TacticalPosition tacticalPosition = _mainDefensiveLineObject.LinkedTacticalPositions.FirstOrDefault();
				if (tacticalPosition != _linkedRangedDefensivePosition)
				{
					_linkedRangedDefensivePosition = tacticalPosition;
					IsTacticReapplyNeeded = true;
				}
			}
			else
			{
				_linkedRangedDefensivePosition = null;
			}
		}
		else
		{
			_mainDefensiveLineObject = null;
			_linkedRangedDefensivePosition = null;
		}
	}
}
