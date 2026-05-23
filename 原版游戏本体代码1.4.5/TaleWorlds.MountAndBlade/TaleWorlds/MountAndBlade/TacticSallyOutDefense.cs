using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class TacticSallyOutDefense : TacticComponent
{
	private enum WeaponsToBeDefended
	{
		Unset,
		NoWeapons,
		OnlyRangedWeapons,
		OnePrimary,
		TwoPrimary,
		ThreePrimary
	}

	private bool _hasBattleBeenJoined;

	private WorldPosition SallyOutDefensePosition;

	private Formation _cavalryFormation;

	private readonly TeamAISallyOutDefender _teamAISallyOutDefender;

	private List<SiegeWeapon> _destructableSiegeWeapons;

	private WeaponsToBeDefended _weaponsToBeDefendedState;

	protected override void ManageFormationCounts()
	{
		if (_weaponsToBeDefendedState == WeaponsToBeDefended.TwoPrimary)
		{
			ManageFormationCounts(1, 1, 1, 1);
			_mainInfantry = TacticComponent.ChooseAndSortByPriority(base.FormationsIncludingEmpty, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation, (Formation f) => f.IsAIControlled, (Formation f) => f.QuerySystem.FormationPower).FirstOrDefault();
			if (_mainInfantry != null)
			{
				_mainInfantry.AI.IsMainFormation = true;
			}
			_archers = TacticComponent.ChooseAndSortByPriority(base.FormationsIncludingEmpty, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsRangedFormation, (Formation f) => f.IsAIControlled, (Formation f) => f.QuerySystem.FormationPower).FirstOrDefault();
			_cavalryFormation = TacticComponent.ChooseAndSortByPriority(base.FormationsIncludingEmpty, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsCavalryFormation, (Formation f) => f.IsAIControlled, (Formation f) => f.QuerySystem.FormationPower).FirstOrDefault();
			_rangedCavalry = TacticComponent.ChooseAndSortByPriority(base.FormationsIncludingEmpty, (Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsRangedCavalryFormation, (Formation f) => f.IsAIControlled, (Formation f) => f.QuerySystem.FormationPower).FirstOrDefault();
		}
		else
		{
			AssignTacticFormations1121();
		}
	}

	private void Engage()
	{
		if (_leftCavalry != null)
		{
			_leftCavalry.AI.SetBehaviorWeight<BehaviorFlank>(1f);
			_leftCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
		}
		if (_rightCavalry != null)
		{
			_rightCavalry.AI.SetBehaviorWeight<BehaviorFlank>(1f);
			_rightCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
		}
	}

	private void DetermineState()
	{
		if (_destructableSiegeWeapons.Count == 0)
		{
			_weaponsToBeDefendedState = WeaponsToBeDefended.NoWeapons;
			return;
		}
		switch (_destructableSiegeWeapons.Count((SiegeWeapon dsw) => dsw is IPrimarySiegeWeapon && !dsw.IsDisabled && !dsw.IsDestroyed))
		{
		case 0:
			_weaponsToBeDefendedState = WeaponsToBeDefended.OnlyRangedWeapons;
			break;
		case 1:
			_weaponsToBeDefendedState = WeaponsToBeDefended.OnePrimary;
			break;
		case 2:
			_weaponsToBeDefendedState = WeaponsToBeDefended.TwoPrimary;
			break;
		case 3:
			_weaponsToBeDefendedState = WeaponsToBeDefended.ThreePrimary;
			break;
		}
	}

	public TacticSallyOutDefense(Team team)
		: base(team)
	{
		_teamAISallyOutDefender = team.TeamAI as TeamAISallyOutDefender;
		_destructableSiegeWeapons = (from sw in Mission.Current.ActiveMissionObjects.FindAllWithType<SiegeWeapon>()
			where sw.Side == team.Side && sw.IsDestructible
			select sw).ToList();
		SallyOutDefensePosition = ((team.TeamAI is TeamAISallyOutDefender) ? (team.TeamAI as TeamAISallyOutDefender).DefensePosition() : WorldPosition.Invalid);
	}

	private bool CalculateHasBattleBeenJoined()
	{
		if (_mainInfantry?.CachedClosestEnemyFormation != null && !(_mainInfantry.AI.ActiveBehavior is BehaviorCharge) && !(_mainInfantry.AI.ActiveBehavior is BehaviorTacticalCharge))
		{
			return _mainInfantry.CachedMedianPosition.AsVec2.Distance(_mainInfantry.CachedClosestEnemyFormation.Formation.CachedMedianPosition.AsVec2) / _mainInfantry.CachedClosestEnemyFormation.MovementSpeedMaximum <= 3f + (_hasBattleBeenJoined ? 2f : 0f);
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

	private void DefendCenterLocation()
	{
		if (_mainInfantry != null)
		{
			_mainInfantry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_mainInfantry);
			_mainInfantry.AI.SetBehaviorWeight<BehaviorDefendSiegeWeapon>(1f);
			BehaviorDefendSiegeWeapon behavior = _mainInfantry.AI.GetBehavior<BehaviorDefendSiegeWeapon>();
			behavior.SetDefensePositionFromTactic(_teamAISallyOutDefender.CalculateSallyOutReferencePosition(FormationAI.BehaviorSide.Middle).ToWorldPosition());
			behavior.SetDefendedSiegeWeaponFromTactic(_teamAISallyOutDefender.PrimarySiegeWeapons.FirstOrDefault((IPrimarySiegeWeapon psw) => psw.WeaponSide == FormationAI.BehaviorSide.Middle && psw is SiegeWeapon) as SiegeWeapon);
		}
		if (_archers != null)
		{
			_archers.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_archers);
			_archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(1f);
			_archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
			_archers.AI.SetBehaviorWeight<BehaviorSkirmish>(1f);
		}
		if (_leftCavalry != null)
		{
			_leftCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_leftCavalry);
			_leftCavalry.AI.SetBehaviorWeight<BehaviorDefendSiegeWeapon>(1.5f);
			BehaviorDefendSiegeWeapon behavior2 = _leftCavalry.AI.GetBehavior<BehaviorDefendSiegeWeapon>();
			behavior2.SetDefensePositionFromTactic(_teamAISallyOutDefender.CalculateSallyOutReferencePosition(FormationAI.BehaviorSide.Left).ToWorldPosition());
			behavior2.SetDefendedSiegeWeaponFromTactic(_teamAISallyOutDefender.PrimarySiegeWeapons.FirstOrDefault((IPrimarySiegeWeapon psw) => psw.WeaponSide == FormationAI.BehaviorSide.Left && psw is SiegeWeapon) as SiegeWeapon);
			BehaviorProtectFlank behaviorProtectFlank = _leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f);
			behaviorProtectFlank.FlankSide = FormationAI.BehaviorSide.Left;
			_leftCavalry.AI.AddSpecialBehavior(behaviorProtectFlank, purgePreviousSpecialBehaviors: true);
		}
		if (_rightCavalry != null)
		{
			_rightCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_rightCavalry);
			_rightCavalry.AI.SetBehaviorWeight<BehaviorDefendSiegeWeapon>(1.5f);
			BehaviorDefendSiegeWeapon behavior3 = _rightCavalry.AI.GetBehavior<BehaviorDefendSiegeWeapon>();
			behavior3.SetDefensePositionFromTactic(_teamAISallyOutDefender.CalculateSallyOutReferencePosition(FormationAI.BehaviorSide.Right).ToWorldPosition());
			behavior3.SetDefendedSiegeWeaponFromTactic(_teamAISallyOutDefender.PrimarySiegeWeapons.FirstOrDefault((IPrimarySiegeWeapon psw) => psw.WeaponSide == FormationAI.BehaviorSide.Right && psw is SiegeWeapon) as SiegeWeapon);
			_rightCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
			BehaviorProtectFlank behaviorProtectFlank2 = _leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f);
			behaviorProtectFlank2.FlankSide = FormationAI.BehaviorSide.Right;
			_rightCavalry.AI.AddSpecialBehavior(behaviorProtectFlank2, purgePreviousSpecialBehaviors: true);
		}
		if (_rangedCavalry != null)
		{
			_rangedCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_rangedCavalry);
			_rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
			_rangedCavalry.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(0.3f);
		}
	}

	private void DefendTwoMainPositions()
	{
		FormationAI.BehaviorSide infantrySide = FormationAI.BehaviorSide.BehaviorSideNotSet;
		if (_mainInfantry != null)
		{
			_mainInfantry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_mainInfantry);
			_mainInfantry.AI.SetBehaviorWeight<BehaviorDefendSiegeWeapon>(1f);
			BehaviorDefendSiegeWeapon behavior = _mainInfantry.AI.GetBehavior<BehaviorDefendSiegeWeapon>();
			SiegeWeapon siegeWeapon = _destructableSiegeWeapons.FirstOrDefault((SiegeWeapon dsw) => dsw is IPrimarySiegeWeapon && dsw is IMoveableSiegeWeapon && (dsw as IPrimarySiegeWeapon).WeaponSide == FormationAI.BehaviorSide.Middle);
			if (siegeWeapon != null)
			{
				infantrySide = FormationAI.BehaviorSide.Middle;
			}
			else
			{
				siegeWeapon = Extensions.MinBy(_destructableSiegeWeapons.Where((SiegeWeapon dsw) => dsw is IPrimarySiegeWeapon && dsw is IMoveableSiegeWeapon), (SiegeWeapon dsw) => dsw.GameEntity.GlobalPosition.AsVec2.DistanceSquared(_mainInfantry.CachedAveragePosition));
				infantrySide = (siegeWeapon as IPrimarySiegeWeapon).WeaponSide;
			}
			behavior.SetDefensePositionFromTactic(_teamAISallyOutDefender.CalculateSallyOutReferencePosition(infantrySide).ToWorldPosition());
			behavior.SetDefendedSiegeWeaponFromTactic(siegeWeapon);
			_mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
			_mainInfantry.AI.Side = infantrySide;
		}
		if (_archers != null)
		{
			_archers.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_archers);
			_archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(1f);
			_archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
			_archers.AI.SetBehaviorWeight<BehaviorSkirmish>(1f);
		}
		if (_cavalryFormation != null)
		{
			if (infantrySide != FormationAI.BehaviorSide.BehaviorSideNotSet)
			{
				_cavalryFormation.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(_cavalryFormation);
				_cavalryFormation.AI.SetBehaviorWeight<BehaviorDefendSiegeWeapon>(1f);
				BehaviorDefendSiegeWeapon behavior2 = _cavalryFormation.AI.GetBehavior<BehaviorDefendSiegeWeapon>();
				FormationAI.BehaviorSide behaviorSide = FormationAI.BehaviorSide.BehaviorSideNotSet;
				SiegeWeapon siegeWeapon2 = _destructableSiegeWeapons.FirstOrDefault((SiegeWeapon dsw) => dsw is IPrimarySiegeWeapon && (dsw as IPrimarySiegeWeapon).WeaponSide != infantrySide);
				behaviorSide = ((siegeWeapon2 == null) ? (Extensions.MinBy(_destructableSiegeWeapons.Where((SiegeWeapon dsw) => dsw is IPrimarySiegeWeapon && dsw is IMoveableSiegeWeapon), (SiegeWeapon dsw) => dsw.GameEntity.GlobalPosition.AsVec2.DistanceSquared(_cavalryFormation.CachedAveragePosition)) as IPrimarySiegeWeapon).WeaponSide : (siegeWeapon2 as IPrimarySiegeWeapon).WeaponSide);
				behavior2.SetDefensePositionFromTactic(_teamAISallyOutDefender.CalculateSallyOutReferencePosition(behaviorSide).ToWorldPosition());
				behavior2.SetDefendedSiegeWeaponFromTactic(siegeWeapon2);
				_cavalryFormation.AI.Side = behaviorSide;
			}
			else
			{
				_cavalryFormation.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(_cavalryFormation);
				_cavalryFormation.AI.SetBehaviorWeight<BehaviorDefendSiegeWeapon>(1f);
				BehaviorDefendSiegeWeapon behavior3 = _cavalryFormation.AI.GetBehavior<BehaviorDefendSiegeWeapon>();
				FormationAI.BehaviorSide behaviorSide2 = FormationAI.BehaviorSide.BehaviorSideNotSet;
				SiegeWeapon siegeWeapon3 = _destructableSiegeWeapons.FirstOrDefault((SiegeWeapon dsw) => dsw is IPrimarySiegeWeapon && (dsw as IPrimarySiegeWeapon).WeaponSide == FormationAI.BehaviorSide.Middle);
				if (siegeWeapon3 != null)
				{
					behaviorSide2 = FormationAI.BehaviorSide.Middle;
				}
				else
				{
					siegeWeapon3 = Extensions.MinBy(_destructableSiegeWeapons.Where((SiegeWeapon dsw) => dsw is IPrimarySiegeWeapon && dsw is IMoveableSiegeWeapon), (SiegeWeapon dsw) => dsw.GameEntity.GlobalPosition.AsVec2.DistanceSquared(_cavalryFormation.CachedAveragePosition));
					behaviorSide2 = (siegeWeapon3 as IPrimarySiegeWeapon).WeaponSide;
				}
				behavior3.SetDefensePositionFromTactic(_teamAISallyOutDefender.CalculateSallyOutReferencePosition(behaviorSide2).ToWorldPosition());
				behavior3.SetDefendedSiegeWeaponFromTactic(siegeWeapon3);
				_cavalryFormation.AI.Side = behaviorSide2;
			}
		}
		if (_rangedCavalry != null)
		{
			_rangedCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_rangedCavalry);
			_rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
			_rangedCavalry.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(0.3f);
		}
	}

	private void DefendSingleMainPosition()
	{
		if (_mainInfantry != null)
		{
			_mainInfantry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_mainInfantry);
			if (_destructableSiegeWeapons.FirstOrDefault((SiegeWeapon dsw) => dsw is IPrimarySiegeWeapon && dsw is IMoveableSiegeWeapon) is IPrimarySiegeWeapon primarySiegeWeapon)
			{
				_mainInfantry.AI.SetBehaviorWeight<BehaviorDefendSiegeWeapon>(1f);
				BehaviorDefendSiegeWeapon behavior = _mainInfantry.AI.GetBehavior<BehaviorDefendSiegeWeapon>();
				behavior.SetDefensePositionFromTactic(_teamAISallyOutDefender.CalculateSallyOutReferencePosition(primarySiegeWeapon.WeaponSide).ToWorldPosition());
				behavior.SetDefendedSiegeWeaponFromTactic(primarySiegeWeapon as SiegeWeapon);
			}
			else if (_destructableSiegeWeapons.Any((SiegeWeapon dsw) => !dsw.IsDisabled))
			{
				SiegeWeapon siegeWeapon = Extensions.MinBy(_destructableSiegeWeapons.Where((SiegeWeapon dsw) => !dsw.IsDisabled), (SiegeWeapon dsw) => dsw.GameEntity.GlobalPosition.AsVec2.DistanceSquared(_mainInfantry.CachedAveragePosition));
				_mainInfantry.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(_mainInfantry);
				_mainInfantry.AI.SetBehaviorWeight<BehaviorDefendSiegeWeapon>(1f);
				BehaviorDefendSiegeWeapon behavior2 = _mainInfantry.AI.GetBehavior<BehaviorDefendSiegeWeapon>();
				behavior2.SetDefensePositionFromTactic(siegeWeapon.GameEntity.GlobalPosition.ToWorldPosition());
				behavior2.SetDefendedSiegeWeaponFromTactic(siegeWeapon);
			}
			else
			{
				_mainInfantry.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(_mainInfantry);
				_mainInfantry.AI.SetBehaviorWeight<BehaviorDefend>(1f);
				_mainInfantry.AI.GetBehavior<BehaviorDefend>().DefensePosition = _teamAISallyOutDefender.CalculateSallyOutReferencePosition(FormationAI.BehaviorSide.Middle).ToWorldPosition();
			}
			_mainInfantry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
		}
		if (_archers != null)
		{
			_archers.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_archers);
			_archers.AI.SetBehaviorWeight<BehaviorSkirmishLine>(1f);
			_archers.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(1f);
			_archers.AI.SetBehaviorWeight<BehaviorSkirmish>(1f);
		}
		if (_leftCavalry != null)
		{
			_leftCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_leftCavalry);
			_leftCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Left;
			if (_mainInfantry == null)
			{
				if (_destructableSiegeWeapons.FirstOrDefault((SiegeWeapon dsw) => dsw is IPrimarySiegeWeapon && dsw is IMoveableSiegeWeapon) is IPrimarySiegeWeapon primarySiegeWeapon2)
				{
					_leftCavalry.AI.SetBehaviorWeight<BehaviorDefendSiegeWeapon>(1f);
					BehaviorDefendSiegeWeapon behavior3 = _leftCavalry.AI.GetBehavior<BehaviorDefendSiegeWeapon>();
					behavior3.SetDefensePositionFromTactic(_teamAISallyOutDefender.CalculateSallyOutReferencePosition(primarySiegeWeapon2.WeaponSide).ToWorldPosition());
					behavior3.SetDefendedSiegeWeaponFromTactic(primarySiegeWeapon2 as SiegeWeapon);
				}
				else if (_destructableSiegeWeapons.Any((SiegeWeapon dsw) => !dsw.IsDisabled))
				{
					SiegeWeapon siegeWeapon2 = Extensions.MinBy(_destructableSiegeWeapons.Where((SiegeWeapon dsw) => !dsw.IsDisabled), (SiegeWeapon dsw) => dsw.GameEntity.GlobalPosition.AsVec2.DistanceSquared(_leftCavalry.CachedAveragePosition));
					_leftCavalry.AI.ResetBehaviorWeights();
					TacticComponent.SetDefaultBehaviorWeights(_leftCavalry);
					_leftCavalry.AI.SetBehaviorWeight<BehaviorDefendSiegeWeapon>(1f);
					BehaviorDefendSiegeWeapon behavior4 = _leftCavalry.AI.GetBehavior<BehaviorDefendSiegeWeapon>();
					behavior4.SetDefensePositionFromTactic(siegeWeapon2.GameEntity.GlobalPosition.ToWorldPosition());
					behavior4.SetDefendedSiegeWeaponFromTactic(siegeWeapon2);
				}
				else
				{
					_leftCavalry.AI.ResetBehaviorWeights();
					TacticComponent.SetDefaultBehaviorWeights(_leftCavalry);
					_leftCavalry.AI.SetBehaviorWeight<BehaviorDefend>(1f);
					_leftCavalry.AI.GetBehavior<BehaviorDefend>().DefensePosition = _teamAISallyOutDefender.CalculateSallyOutReferencePosition(FormationAI.BehaviorSide.Middle).ToWorldPosition();
				}
				_leftCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
			}
		}
		if (_rightCavalry != null)
		{
			_rightCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_rightCavalry);
			_rightCavalry.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f).FlankSide = FormationAI.BehaviorSide.Right;
			if (_mainInfantry == null)
			{
				if (_destructableSiegeWeapons.FirstOrDefault((SiegeWeapon dsw) => dsw is IPrimarySiegeWeapon && dsw is IMoveableSiegeWeapon) is IPrimarySiegeWeapon primarySiegeWeapon3)
				{
					_rightCavalry.AI.SetBehaviorWeight<BehaviorDefendSiegeWeapon>(1f);
					BehaviorDefendSiegeWeapon behavior5 = _rightCavalry.AI.GetBehavior<BehaviorDefendSiegeWeapon>();
					behavior5.SetDefensePositionFromTactic(_teamAISallyOutDefender.CalculateSallyOutReferencePosition(primarySiegeWeapon3.WeaponSide).ToWorldPosition());
					behavior5.SetDefendedSiegeWeaponFromTactic(primarySiegeWeapon3 as SiegeWeapon);
				}
				else if (_destructableSiegeWeapons.Any((SiegeWeapon dsw) => !dsw.IsDisabled))
				{
					SiegeWeapon siegeWeapon3 = Extensions.MinBy(_destructableSiegeWeapons.Where((SiegeWeapon dsw) => !dsw.IsDisabled), (SiegeWeapon dsw) => dsw.GameEntity.GlobalPosition.AsVec2.DistanceSquared(_rightCavalry.CachedAveragePosition));
					_rightCavalry.AI.ResetBehaviorWeights();
					TacticComponent.SetDefaultBehaviorWeights(_rightCavalry);
					_rightCavalry.AI.SetBehaviorWeight<BehaviorDefendSiegeWeapon>(1f);
					BehaviorDefendSiegeWeapon behavior6 = _rightCavalry.AI.GetBehavior<BehaviorDefendSiegeWeapon>();
					behavior6.SetDefensePositionFromTactic(siegeWeapon3.GameEntity.GlobalPosition.ToWorldPosition());
					behavior6.SetDefendedSiegeWeaponFromTactic(siegeWeapon3);
				}
				else
				{
					_rightCavalry.AI.ResetBehaviorWeights();
					TacticComponent.SetDefaultBehaviorWeights(_rightCavalry);
					_rightCavalry.AI.SetBehaviorWeight<BehaviorDefend>(1f);
					_rightCavalry.AI.GetBehavior<BehaviorDefend>().DefensePosition = _teamAISallyOutDefender.CalculateSallyOutReferencePosition(FormationAI.BehaviorSide.Middle).ToWorldPosition();
				}
				_rightCavalry.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
			}
		}
		if (_rangedCavalry != null)
		{
			_rangedCavalry.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_rangedCavalry);
			_rangedCavalry.AI.SetBehaviorWeight<BehaviorHorseArcherSkirmish>(1f);
			_rangedCavalry.AI.SetBehaviorWeight<BehaviorScreenedSkirmish>(0.3f);
		}
	}

	private void ApplyDefenseBasedOnState()
	{
		_destructableSiegeWeapons.RemoveAll((SiegeWeapon dsw) => dsw.IsDisabled || dsw.IsDestroyed);
		switch (_weaponsToBeDefendedState)
		{
		case WeaponsToBeDefended.ThreePrimary:
			DefendCenterLocation();
			break;
		case WeaponsToBeDefended.TwoPrimary:
			DefendTwoMainPositions();
			break;
		default:
			DefendSingleMainPosition();
			break;
		}
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
			ApplyDefenseBasedOnState();
			if (_hasBattleBeenJoined)
			{
				Engage();
			}
			IsTacticReapplyNeeded = false;
		}
		WeaponsToBeDefended weaponsToBeDefendedState = _weaponsToBeDefendedState;
		DetermineState();
		if (weaponsToBeDefendedState != _weaponsToBeDefendedState)
		{
			ApplyDefenseBasedOnState();
			IsTacticReapplyNeeded = false;
		}
		bool flag = CalculateHasBattleBeenJoined();
		if (flag != _hasBattleBeenJoined || IsTacticReapplyNeeded)
		{
			_hasBattleBeenJoined = flag;
			if (_hasBattleBeenJoined)
			{
				Engage();
			}
			else
			{
				ApplyDefenseBasedOnState();
			}
			IsTacticReapplyNeeded = false;
		}
		base.TickOccasionally();
	}

	protected internal override float GetTacticWeight()
	{
		return 10f;
	}
}
