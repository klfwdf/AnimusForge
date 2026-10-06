using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TacticCoordinatedRetreat : TacticComponent
{
	private bool _canWeSafelyRunAway;

	private Vec2 _retreatPosition = Vec2.Invalid;

	private const float RetreatThresholdValue = 2f;

	public TacticCoordinatedRetreat(Team team)
		: base(team)
	{
	}

	protected override void ManageFormationCounts()
	{
		if (!_canWeSafelyRunAway)
		{
			AssignTacticFormations1121();
		}
	}

	private void OrganizedRetreat()
	{
		if (base.Team.IsPlayerTeam && !base.Team.IsPlayerGeneral && base.Team.IsPlayerSergeant)
		{
			SoundTacticalHorn(TacticComponent.RetreatHornSoundIndex);
		}
		if (_mainInfantry != null)
		{
			_mainInfantry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_mainInfantry);
			BehaviorDefend behaviorDefend = _mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1f);
			WorldPosition closestFleePositionForFormation = Mission.Current.GetClosestFleePositionForFormation(_mainInfantry);
			closestFleePositionForFormation.SetVec2(Mission.Current.GetClosestBoundaryPosition(closestFleePositionForFormation.AsVec2));
			_retreatPosition = closestFleePositionForFormation.AsVec2;
			behaviorDefend.DefensePosition = closestFleePositionForFormation;
			_mainInfantry.AI.SetBehaviorWeight<BehaviorPullBack>(1f);
		}
		if (_archers != null)
		{
			_archers.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_archers);
			_archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
			_archers.AI.SetBehaviorWeight<BehaviorPullBack>(1.5f);
		}
		if (_leftCavalry != null)
		{
			_leftCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_leftCavalry);
			_leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Left;
			_leftCavalry.AI.SetBehaviorWeight<BehaviorCavalryScreen>(1f);
			_leftCavalry.AI.SetBehaviorWeight<BehaviorPullBack>(1.5f);
		}
		if (_rightCavalry != null)
		{
			_rightCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_rightCavalry);
			_rightCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Right;
			_rightCavalry.AI.SetBehaviorWeight<BehaviorCavalryScreen>(1f);
			_rightCavalry.AI.SetBehaviorWeight<BehaviorPullBack>(1.5f);
		}
		if (_rangedCavalry != null)
		{
			_rangedCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_rangedCavalry);
			_rangedCavalry.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
			_rangedCavalry.AI.SetBehaviorWeight<BehaviorPullBack>(1.5f);
			_rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
		}
	}

	private void RunForTheBorder()
	{
		if (base.Team.IsPlayerTeam && !base.Team.IsPlayerGeneral && base.Team.IsPlayerSergeant)
		{
			SoundTacticalHorn(TacticComponent.RetreatHornSoundIndex);
		}
		foreach (Formation item in base.FormationsIncludingSpecialAndEmpty)
		{
			if (item.CountOfUnits > 0)
			{
				item.AI.ResetBehaviorWeights();
				item.AI.SetBehaviorWeight<BehaviorRetreat>(1f);
			}
		}
	}

	private bool HasRetreatDestinationBeenReached()
	{
		return base.FormationsIncludingEmpty.All((Formation f) => f.CountOfUnits == 0 || !f.QuerySystem.IsInfantryFormation || f.CachedAveragePosition.DistanceSquared(_retreatPosition) < 5625f);
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
		bool flag = HasRetreatDestinationBeenReached();
		if (CheckAndSetAvailableFormationsChanged())
		{
			_canWeSafelyRunAway = flag;
			ManageFormationCounts();
			if (_canWeSafelyRunAway)
			{
				RunForTheBorder();
			}
			else
			{
				OrganizedRetreat();
			}
			IsTacticReapplyNeeded = false;
		}
		if (flag != _canWeSafelyRunAway || IsTacticReapplyNeeded)
		{
			_canWeSafelyRunAway = flag;
			if (_canWeSafelyRunAway)
			{
				RunForTheBorder();
			}
			else
			{
				OrganizedRetreat();
			}
			IsTacticReapplyNeeded = false;
		}
		base.TickOccasionally();
	}

	protected internal override float GetTacticWeight()
	{
		float value = base.Team.QuerySystem.TotalPowerRatio / base.Team.QuerySystem.RemainingPowerRatio;
		float valueTo = MathF.Max(base.Team.QuerySystem.InfantryRatio, MathF.Max(base.Team.QuerySystem.RangedRatio, base.Team.QuerySystem.CavalryRatio));
		float num = MBMath.LinearExtrapolation(0f, valueTo, MBMath.ClampFloat(value, 0f, 4f) / 2f);
		float num2 = 0f;
		int num3 = 0;
		foreach (Team team in base.Team.Mission.Teams)
		{
			if (team.IsEnemyOf(base.Team))
			{
				num3++;
				num2 += team.QuerySystem.CavalryRatio + team.QuerySystem.RangedCavalryRatio;
			}
		}
		if (num3 > 0)
		{
			num2 /= (float)num3;
		}
		float num4 = ((num2 == 0f) ? 1.2f : MBMath.Lerp(0.8f, 1.2f, MBMath.ClampFloat((base.Team.QuerySystem.CavalryRatio + base.Team.QuerySystem.RangedCavalryRatio) / num2, 0f, 2f) / 2f));
		return num * num4 * MathF.Min(1f, MathF.Sqrt(base.Team.QuerySystem.RemainingPowerRatio));
	}
}
