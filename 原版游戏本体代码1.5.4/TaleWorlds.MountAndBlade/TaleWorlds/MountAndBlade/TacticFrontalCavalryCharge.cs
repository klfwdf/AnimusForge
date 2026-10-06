using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TacticFrontalCavalryCharge : TacticComponent
{
	private Formation _cavalry;

	private bool _hasBattleBeenJoined;

	public TacticFrontalCavalryCharge(Team team)
		: base(team)
	{
	}

	protected override void ManageFormationCounts()
	{
		ManageFormationCounts(1, 1, 1, 1);
		_mainInfantry = TacticComponent.ChooseAndSortByPriority(base.FormationsIncludingEmpty, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation, (Formation f) => f.IsAIControlled, (Formation f) => f.QuerySystem.FormationPower).FirstOrDefault();
		if (_mainInfantry != null)
		{
			_mainInfantry.AI.IsMainFormation = true;
		}
		_archers = TacticComponent.ChooseAndSortByPriority(base.FormationsIncludingEmpty, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsRangedFormation, (Formation f) => f.IsAIControlled, (Formation f) => f.QuerySystem.FormationPower).FirstOrDefault();
		_cavalry = TacticComponent.ChooseAndSortByPriority(base.FormationsIncludingEmpty, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsCavalryFormation, (Formation f) => f.IsAIControlled, (Formation f) => f.QuerySystem.FormationPower).FirstOrDefault();
		_rangedCavalry = TacticComponent.ChooseAndSortByPriority(base.FormationsIncludingEmpty, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsRangedCavalryFormation, (Formation f) => f.IsAIControlled, (Formation f) => f.QuerySystem.FormationPower).FirstOrDefault();
	}

	private void Advance()
	{
		if (base.Team.IsPlayerTeam && !base.Team.IsPlayerGeneral && base.Team.IsPlayerSergeant)
		{
			SoundTacticalHorn(TacticComponent.MoveHornSoundIndex);
		}
		if (_mainInfantry != null)
		{
			_mainInfantry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_mainInfantry);
			_mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
		}
		if (_archers != null)
		{
			_archers.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_archers);
			_archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(1f);
			_archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
			_archers.AI.SetBehaviorWeight<BehaviorSkirmish>(1f);
		}
		if (_cavalry != null)
		{
			_cavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_cavalry);
			_cavalry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
			_cavalry.AI.SetBehaviorWeight<BehaviorVanguard>(1f);
		}
		if (_rangedCavalry != null)
		{
			_rangedCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_rangedCavalry);
			_rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
			_rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
		}
	}

	private void Attack()
	{
		if (base.Team.IsPlayerTeam && !base.Team.IsPlayerGeneral && base.Team.IsPlayerSergeant)
		{
			SoundTacticalHorn(TacticComponent.AttackHornSoundIndex);
		}
		if (_mainInfantry != null)
		{
			_mainInfantry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_mainInfantry);
			_mainInfantry.AI.SetBehaviorWeight<BehaviorAdvance>(1f);
			_mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
		}
		if (_archers != null)
		{
			_archers.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_archers);
			_archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
			_archers.AI.SetBehaviorWeight<BehaviorSkirmish>(1f);
		}
		if (_cavalry != null)
		{
			_cavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_cavalry);
			_cavalry.AI.SetBehaviorWeight<BehaviorFlank>(1f);
			_cavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
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
		if (_cavalry?.CachedClosestEnemyFormation != null && !(_cavalry.AI.ActiveBehavior is BehaviorCharge) && !(_cavalry.AI.ActiveBehavior is BehaviorTacticalCharge))
		{
			return _cavalry.CachedMedianPosition.AsVec2.Distance(_cavalry.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2) / _cavalry.CachedClosestEnemyFormation.MovementSpeedMaximum <= 7f + (_hasBattleBeenJoined ? 7f : 0f);
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
			if ((_mainInfantry == null || (_mainInfantry.CountOfUnits != 0 && _mainInfantry.QuerySystem.IsInfantryFormation)) && (_archers == null || (_archers.CountOfUnits != 0 && _archers.QuerySystem.IsRangedFormation)) && (_cavalry == null || (_cavalry.CountOfUnits != 0 && _cavalry.QuerySystem.IsCavalryFormation)))
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
		if (CheckAndSetAvailableFormationsChanged())
		{
			ManageFormationCounts();
			if (_hasBattleBeenJoined)
			{
				Attack();
			}
			else
			{
				Advance();
			}
			IsTacticReapplyNeeded = false;
		}
		bool flag = HasBattleBeenJoined();
		if (flag != _hasBattleBeenJoined || IsTacticReapplyNeeded)
		{
			_hasBattleBeenJoined = flag;
			if (_hasBattleBeenJoined)
			{
				Attack();
			}
			else
			{
				Advance();
			}
			IsTacticReapplyNeeded = false;
		}
		base.TickOccasionally();
	}

	protected internal override float GetTacticWeight()
	{
		float num = base.Team.QuerySystem.RangedCavalryRatio * (float)base.Team.QuerySystem.MemberCount;
		return base.Team.QuerySystem.CavalryRatio * (float)base.Team.QuerySystem.MemberCount / ((float)base.Team.QuerySystem.MemberCount - num) * MathF.Sqrt(base.Team.QuerySystem.RemainingPowerRatio);
	}
}
