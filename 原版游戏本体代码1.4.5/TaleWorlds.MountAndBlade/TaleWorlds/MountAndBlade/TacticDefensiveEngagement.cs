using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TacticDefensiveEngagement : TacticComponent
{
	private const float DefendersAdvantage = 1.1f;

	private bool _hasBattleBeenJoined;

	public TacticDefensiveEngagement(Team team)
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
			_mainInfantry.AI.SetBehaviorWeight<BehaviorHoldHighGround>(1f).RangedAllyFormation = _archers;
			_mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
		}
		if (_archers != null)
		{
			_archers.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_archers);
			_archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(1f);
			_archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
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
			_mainInfantry.AI.SetBehaviorWeight<BehaviorHoldHighGround>(1f).RangedAllyFormation = _archers;
			_mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
		}
		if (_archers != null)
		{
			_archers.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_archers);
			_archers.AI.SetBehaviorWeight<BehaviorSkirmish>(1f);
			_archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
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
		if (base.AreFormationsCreated)
		{
			bool flag = HasBattleBeenJoined();
			bool flag2 = CheckAndSetAvailableFormationsChanged();
			if (flag2 || flag != _hasBattleBeenJoined || IsTacticReapplyNeeded)
			{
				_hasBattleBeenJoined = flag;
				if (flag2)
				{
					ManageFormationCounts();
				}
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
		}
		base.TickOccasionally();
	}

	protected internal override float GetTacticWeight()
	{
		if (!base.Team.TeamAI.IsDefenseApplicable || base.FormationsIncludingEmpty.All((Formation f) => f.CountOfUnits == 0 || !f.QuerySystem.IsInfantryFormation))
		{
			return 0f;
		}
		Formation formation = _mainInfantry ?? TaleWorlds.Core.Extensions.MaxBy(base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation), (Formation f) => f.CountOfUnits);
		if (formation == null)
		{
			return 0f;
		}
		if (_mainInfantry == null)
		{
			_mainInfantry = formation;
		}
		float num = base.Team.QuerySystem.InfantryRatio + base.Team.QuerySystem.RangedRatio;
		float value = _mainInfantry.CachedAveragePosition.Distance(_mainInfantry.QuerySystem.HighGroundCloseToForeseenBattleGround);
		float num2 = MBMath.Lerp(0.7f, 1f, (150f - MBMath.ClampFloat(value, 50f, 150f)) / 100f);
		return num * 1.1f * TacticComponent.CalculateNotEngagingTacticalAdvantage(base.Team.QuerySystem) * num2 / MathF.Sqrt(base.Team.QuerySystem.RemainingPowerRatio);
	}
}
