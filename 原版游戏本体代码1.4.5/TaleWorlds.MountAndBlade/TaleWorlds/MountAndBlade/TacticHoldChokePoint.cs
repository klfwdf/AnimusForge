using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TacticHoldChokePoint : TacticComponent
{
	private const float DefendersAdvantage = 1.3f;

	private TacticalPosition _chokePointTacticalPosition;

	private TacticalPosition _linkedRangedDefensivePosition;

	public TacticHoldChokePoint(Team team)
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
			_mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1f).TacticalDefendPosition = _chokePointTacticalPosition;
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

	protected internal override float GetTacticWeight()
	{
		if (!base.Team.TeamAI.IsDefenseApplicable || !CheckAndDetermineFormation(ref _mainInfantry, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation))
		{
			return 0f;
		}
		if (!base.Team.TeamAI.IsCurrentTactic(this) || _chokePointTacticalPosition == null || !IsTacticalPositionEligible(_chokePointTacticalPosition))
		{
			DetermineChokePoints();
		}
		if (_chokePointTacticalPosition == null)
		{
			return 0f;
		}
		float infantryRatio = base.Team.QuerySystem.InfantryRatio;
		float num = MathF.Min(infantryRatio, base.Team.QuerySystem.RangedRatio);
		float num2 = infantryRatio + num;
		float num3 = MBMath.ClampFloat((float)base.Team.QuerySystem.EnemyUnitCount / (float)base.Team.QuerySystem.MemberCount, 0.33f, 3f);
		return num2 * num3 * GetTacticalPositionScore(_chokePointTacticalPosition) * TacticComponent.CalculateNotEngagingTacticalAdvantage(base.Team.QuerySystem) * 1.3f / MathF.Sqrt(base.Team.QuerySystem.RemainingPowerRatio);
	}

	private bool IsTacticalPositionEligible(TacticalPosition tacticalPosition)
	{
		if (CheckAndDetermineFormation(ref _mainInfantry, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation))
		{
			float num = base.Team.QuerySystem.AveragePosition.Distance(tacticalPosition.Position.AsVec2);
			float num2 = base.Team.QuerySystem.AverageEnemyPosition.Distance(_mainInfantry.CachedAveragePosition);
			if (num > 20f && num > num2 * 0.5f)
			{
				return false;
			}
			if (_mainInfantry.MaximumWidth < tacticalPosition.Width)
			{
				return false;
			}
			float num3 = (base.Team.QuerySystem.AverageEnemyPosition - tacticalPosition.Position.AsVec2).Normalized().DotProduct(tacticalPosition.Direction);
			if (tacticalPosition.IsInsurmountable)
			{
				return MathF.Abs(num3) >= 0.5f;
			}
			return num3 >= 0.5f;
		}
		return false;
	}

	private float GetTacticalPositionScore(TacticalPosition tacticalPosition)
	{
		if (CheckAndDetermineFormation(ref _mainInfantry, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation))
		{
			float num = MBMath.Lerp(1f, 1.5f, MBMath.ClampFloat(tacticalPosition.Slope, 0f, 60f) / 60f);
			int countOfUnits = _mainInfantry.CountOfUnits;
			float num2 = _mainInfantry.Interval * (float)(countOfUnits - 1) + _mainInfantry.UnitDiameter * (float)countOfUnits;
			float num3 = MBMath.Lerp(0.67f, 1.5f, (MBMath.ClampFloat(num2 / tacticalPosition.Width, 0.5f, 3f) - 0.5f) / 2.5f);
			float num4 = 1f;
			if (CheckAndDetermineFormation(ref _archers, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsRangedFormation) && tacticalPosition.LinkedTacticalPositions.Where((TacticalPosition lcp) => lcp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.Cliff).ToList().Count > 0)
			{
				num4 = MBMath.Lerp(1f, 1.5f, (MBMath.ClampFloat(base.Team.QuerySystem.RangedRatio, 0.05f, 0.25f) - 0.05f) * 5f);
			}
			float value = _mainInfantry.CachedAveragePosition.Distance(tacticalPosition.Position.AsVec2);
			float num5 = MBMath.Lerp(0.7f, 1f, (150f - MBMath.ClampFloat(value, 50f, 150f)) / 100f);
			return num * num3 * num4 * num5;
		}
		return 0f;
	}

	protected internal override bool ResetTacticalPositions()
	{
		DetermineChokePoints();
		return true;
	}

	private void DetermineChokePoints()
	{
		IEnumerable<(TacticalPosition tp, float)> first = from tp in base.Team.TeamAI.TacticalPositions
			where tp.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.ChokePoint && IsTacticalPositionEligible(tp)
			select (tp: tp, GetTacticalPositionScore(tp));
		IEnumerable<(TacticalPosition, float)> second = from tp in base.Team.TeamAI.TacticalRegions.SelectMany((TacticalRegion r) => r.LinkedTacticalPositions.Where((TacticalPosition tpftr) => tpftr.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.ChokePoint && IsTacticalPositionEligible(tpftr)))
			select (tp: tp, GetTacticalPositionScore(tp));
		IEnumerable<(TacticalPosition, float)> source = first.Concat<(TacticalPosition, float)>(second);
		if (source.Any())
		{
			TacticalPosition item = TaleWorlds.Core.Extensions.MaxBy<(TacticalPosition, float), float>(source, ((TacticalPosition tp, float) pst) => pst.Item2).Item1;
			if (item != _chokePointTacticalPosition)
			{
				_chokePointTacticalPosition = item;
				IsTacticReapplyNeeded = true;
			}
			if (_chokePointTacticalPosition.LinkedTacticalPositions.Count > 0)
			{
				TacticalPosition tacticalPosition = _chokePointTacticalPosition.LinkedTacticalPositions.FirstOrDefault();
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
			_chokePointTacticalPosition = null;
		}
	}
}
