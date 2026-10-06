using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class TacticSallyOutHitAndRun : TacticComponent
{
	private enum TacticState
	{
		HeadingOutFromCastle,
		DestroyingSiegeWeapons,
		CavalryRetreating,
		InfantryRetreating
	}

	private TacticState _state;

	private Formation _mainInfantryFormation;

	private MBList<Formation> _archerFormations;

	private MBList<Formation> _cavalryFormations;

	private readonly TeamAISallyOutAttacker _teamAISallyOutAttacker;

	private readonly List<SiegeWeapon> _destructibleEnemySiegeWeapons;

	protected override void ManageFormationCounts()
	{
		List<IPrimarySiegeWeapon> list = _teamAISallyOutAttacker.PrimarySiegeWeapons.Where((IPrimarySiegeWeapon psw) => psw is SiegeWeapon { IsDisabled: false } siegeWeapon && siegeWeapon.IsDestructible).ToList();
		int count = list.Count;
		bool flag = false;
		foreach (UsableMachine besiegerRangedSiegeWeapon in _teamAISallyOutAttacker.BesiegerRangedSiegeWeapons)
		{
			if (!besiegerRangedSiegeWeapon.IsDisabled && !besiegerRangedSiegeWeapon.IsDestroyed)
			{
				flag = true;
				break;
			}
		}
		count = TaleWorlds.Library.MathF.Max(count, 1 + ((list.Count > 0 && flag) ? 1 : 0));
		int count2 = TaleWorlds.Library.MathF.Min(_teamAISallyOutAttacker.ArcherPositions.Count, 7 - count);
		bool num = base.FormationsIncludingEmpty.Count((Formation f) => f.CountOfUnits > 0 && (f.QuerySystem.IsCavalryFormation || f.QuerySystem.IsRangedCavalryFormation)) > 0 && base.Team.QuerySystem.CavalryRatio + base.Team.QuerySystem.RangedCavalryRatio > 0.1f;
		bool flag2 = true;
		if (!num)
		{
			count = 1;
			flag2 = base.FormationsIncludingEmpty.Count((Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation) > 0 && base.Team.QuerySystem.CavalryRatio + base.Team.QuerySystem.RangedCavalryRatio + base.Team.QuerySystem.InfantryRatio > 0.15f;
			if (!flag2)
			{
				count2 = 1;
			}
		}
		SplitFormationClassIntoGivenNumber((Formation f) => f.QuerySystem.IsInfantryFormation, 1);
		SplitFormationClassIntoGivenNumber((Formation f) => f.QuerySystem.IsRangedFormation, count2);
		SplitFormationClassIntoGivenNumber((Formation f) => f.QuerySystem.IsCavalryFormation || f.QuerySystem.IsRangedCavalryFormation, count);
		_mainInfantryFormation = base.FormationsIncludingEmpty.FirstOrDefault((Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsInfantryFormation);
		if (_mainInfantryFormation != null)
		{
			_mainInfantryFormation.AI.IsMainFormation = true;
		}
		_archerFormations = base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && f.QuerySystem.IsRangedFormation).ToMBList();
		_cavalryFormations.Clear();
		_cavalryFormations = base.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && (f.QuerySystem.IsCavalryFormation || f.QuerySystem.IsRangedCavalryFormation)).ToMBList();
		if (!num)
		{
			if (_mainInfantryFormation != null)
			{
				_cavalryFormations.Add(_mainInfantryFormation);
				_mainInfantryFormation.AI.IsMainFormation = false;
				_mainInfantryFormation = null;
			}
			if (!flag2)
			{
				_cavalryFormations.AddRange(_archerFormations);
				_archerFormations.Clear();
			}
		}
		bool flag3 = list.Count == 0 || (list.Count == 1 && flag && _cavalryFormations.Count + ((_mainInfantryFormation != null) ? 1 : 0) > 1);
		for (int num2 = 0; num2 < _cavalryFormations.Count - (flag3 ? 1 : 0); num2++)
		{
			if (list.Count > 0)
			{
				_cavalryFormations[num2].AI.Side = list[num2 % list.Count].WeaponSide;
			}
			else
			{
				_cavalryFormations[num2].AI.Side = FormationAI.BehaviorSide.Middle;
			}
		}
		for (int num3 = 0; num3 < _archerFormations.Count - (flag3 ? 1 : 0); num3++)
		{
			if (list.Count > 0)
			{
				_archerFormations[num3].AI.Side = list[num3 % list.Count].WeaponSide;
			}
			else
			{
				_archerFormations[num3].AI.Side = FormationAI.BehaviorSide.Middle;
			}
		}
		if (_cavalryFormations.Count > 0 && flag3)
		{
			if (list.Any((IPrimarySiegeWeapon psw) => psw != null && psw.WeaponSide == FormationAI.BehaviorSide.Middle))
			{
				_cavalryFormations[0].AI.Side = FormationAI.BehaviorSide.Left;
			}
			else
			{
				_cavalryFormations[_cavalryFormations.Count - 1].AI.Side = FormationAI.BehaviorSide.Middle;
			}
		}
		if (_archerFormations.Count > 0 && flag3)
		{
			if (list.Any((IPrimarySiegeWeapon psw) => psw != null && psw.WeaponSide == FormationAI.BehaviorSide.Middle))
			{
				_archerFormations[0].AI.Side = FormationAI.BehaviorSide.Left;
			}
			else
			{
				_archerFormations[_archerFormations.Count - 1].AI.Side = FormationAI.BehaviorSide.Middle;
			}
		}
		_AIControlledFormationCount = base.Team.GetAIControlledFormationCount();
	}

	private void DestroySiegeWeapons()
	{
		if (_mainInfantryFormation != null)
		{
			_mainInfantryFormation.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_mainInfantryFormation);
			BehaviorDefend behaviorDefend = _mainInfantryFormation.AI.SetBehaviorWeight<BehaviorDefend>(1f);
			Vec2 vec = (_teamAISallyOutAttacker.OuterGate.GameEntity.GlobalPosition.AsVec2 - _teamAISallyOutAttacker.InnerGate.GameEntity.GlobalPosition.AsVec2).Normalized();
			WorldPosition defensePosition = new WorldPosition(_mainInfantryFormation.Team.Mission.Scene, UIntPtr.Zero, _teamAISallyOutAttacker.OuterGate.GameEntity.GlobalPosition, hasValidZ: false);
			defensePosition.SetVec2(defensePosition.AsVec2 + (3f + _mainInfantryFormation.Depth) * vec);
			behaviorDefend.DefensePosition = defensePosition;
			_mainInfantryFormation.AI.SetBehaviorWeight<BehaviorDestroySiegeWeapons>(1f);
			_mainInfantryFormation.AI.SetBehaviorWeight<BehaviorCharge>(0.1f);
		}
		if (_teamAISallyOutAttacker.ArcherPositions.Count > 0)
		{
			for (int i = 0; i < _archerFormations.Count; i++)
			{
				Formation formation = _archerFormations[i];
				formation.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(formation);
				formation.AI.SetBehaviorWeight<BehaviorShootFromCastleWalls>(0.1f);
				formation.AI.GetBehavior<BehaviorShootFromCastleWalls>().ArcherPosition = _teamAISallyOutAttacker.ArcherPositions[i % _teamAISallyOutAttacker.ArcherPositions.Count];
				formation.AI.SetBehaviorWeight<BehaviorDestroySiegeWeapons>(1f);
				formation.AI.SetBehaviorWeight<BehaviorCharge>(0.1f);
			}
		}
		foreach (Formation cavalryFormation in _cavalryFormations)
		{
			cavalryFormation.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(cavalryFormation);
			cavalryFormation.AI.SetBehaviorWeight<BehaviorDestroySiegeWeapons>(1f);
			cavalryFormation.AI.SetBehaviorWeight<BehaviorCharge>(0.1f);
		}
	}

	private void CavalryRetreat()
	{
		if (_mainInfantryFormation != null)
		{
			_mainInfantryFormation.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_mainInfantryFormation);
			BehaviorDefend behaviorDefend = _mainInfantryFormation.AI.SetBehaviorWeight<BehaviorDefend>(1f);
			Vec2 vec = (_teamAISallyOutAttacker.OuterGate.GameEntity.GlobalPosition.AsVec2 - _teamAISallyOutAttacker.InnerGate.GameEntity.GlobalPosition.AsVec2).Normalized();
			WorldPosition defensePosition = new WorldPosition(_mainInfantryFormation.Team.Mission.Scene, UIntPtr.Zero, _teamAISallyOutAttacker.OuterGate.GameEntity.GlobalPosition, hasValidZ: false);
			defensePosition.SetVec2(defensePosition.AsVec2 + (3f + _mainInfantryFormation.Depth) * vec);
			behaviorDefend.DefensePosition = defensePosition;
		}
		if (_teamAISallyOutAttacker.ArcherPositions.Count > 0)
		{
			for (int i = 0; i < _archerFormations.Count; i++)
			{
				Formation formation = _archerFormations[i];
				formation.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(formation);
				formation.AI.SetBehaviorWeight<BehaviorShootFromCastleWalls>(1f);
				formation.AI.GetBehavior<BehaviorShootFromCastleWalls>().ArcherPosition = _teamAISallyOutAttacker.ArcherPositions[i % _teamAISallyOutAttacker.ArcherPositions.Count];
			}
		}
		foreach (Formation cavalryFormation in _cavalryFormations)
		{
			cavalryFormation.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(cavalryFormation);
			cavalryFormation.AI.SetBehaviorWeight<BehaviorRetreatToCastle>(3f);
			cavalryFormation.AI.SetBehaviorWeight<BehaviorCharge>(0.1f);
		}
	}

	private void InfantryRetreat()
	{
		if (_mainInfantryFormation == null)
		{
			return;
		}
		for (int i = 0; i < TeamAISiegeComponent.SiegeLanes.Count; i++)
		{
			SiegeLane siegeLane = TeamAISiegeComponent.SiegeLanes[i];
			if (siegeLane.HasGate)
			{
				_mainInfantryFormation.AI.Side = siegeLane.LaneSide;
				break;
			}
		}
		_mainInfantryFormation.AI.ResetBehaviorWeights();
		TacticComponent.SetDefaultBehaviorWeights(_mainInfantryFormation);
		_mainInfantryFormation.AI.SetBehaviorWeight<BehaviorDefendCastleKeyPosition>(1f);
	}

	private void HeadOutFromTheCastle()
	{
		if (_mainInfantryFormation != null)
		{
			_mainInfantryFormation.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(_mainInfantryFormation);
			_mainInfantryFormation.AI.SetBehaviorWeight<BehaviorStop>(1000f);
		}
		if (_teamAISallyOutAttacker.ArcherPositions.Count > 0)
		{
			for (int i = 0; i < _archerFormations.Count; i++)
			{
				Formation formation = _archerFormations[i];
				formation.AI.ResetBehaviorWeights();
				TacticComponent.SetDefaultBehaviorWeights(formation);
				formation.AI.SetBehaviorWeight<BehaviorShootFromCastleWalls>(1f);
				formation.AI.GetBehavior<BehaviorShootFromCastleWalls>().ArcherPosition = _teamAISallyOutAttacker.ArcherPositions[i % _teamAISallyOutAttacker.ArcherPositions.Count];
			}
		}
		foreach (Formation cavalryFormation in _cavalryFormations)
		{
			cavalryFormation.AI.ResetBehaviorWeights();
			TacticComponent.SetDefaultBehaviorWeights(cavalryFormation);
			cavalryFormation.AI.SetBehaviorWeight<BehaviorDestroySiegeWeapons>(1f);
			cavalryFormation.AI.SetBehaviorWeight<BehaviorCharge>(0.1f);
		}
	}

	public TacticSallyOutHitAndRun(Team team)
		: base(team)
	{
		_archerFormations = new MBList<Formation>();
		_cavalryFormations = new MBList<Formation>();
		_teamAISallyOutAttacker = team.TeamAI as TeamAISallyOutAttacker;
		_state = TacticState.HeadingOutFromCastle;
		_destructibleEnemySiegeWeapons = (from sw in Mission.Current.ActiveMissionObjects.FindAllWithType<SiegeWeapon>()
			where sw.Side != team.Side && sw.IsDestructible
			select sw).ToList();
		ManageFormationCounts();
		HeadOutFromTheCastle();
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
		if (!num && (_mainInfantryFormation == null || (_mainInfantryFormation.CountOfUnits != 0 && _mainInfantryFormation.QuerySystem.IsInfantryFormation)) && (_archerFormations.Count <= 0 || !_archerFormations.Any((Formation af) => af.CountOfUnits == 0 || !af.QuerySystem.IsRangedFormation)))
		{
			if (_cavalryFormations.Count > 0)
			{
				return _cavalryFormations.Any((Formation cf) => cf.CountOfUnits == 0);
			}
			return false;
		}
		return true;
	}

	private void CheckAndChangeState()
	{
		switch (_state)
		{
		case TacticState.HeadingOutFromCastle:
			if (_cavalryFormations.All((Formation cf) => !TeamAISiegeComponent.IsFormationInsideCastle(cf, includeOnlyPositionedUnits: false)))
			{
				_state = TacticState.DestroyingSiegeWeapons;
				DestroySiegeWeapons();
			}
			break;
		case TacticState.DestroyingSiegeWeapons:
		{
			bool flag = true;
			foreach (SiegeWeapon destructibleEnemySiegeWeapon in _destructibleEnemySiegeWeapons)
			{
				if (!destructibleEnemySiegeWeapon.IsDestroyed)
				{
					flag = false;
					break;
				}
			}
			bool flag2 = true;
			if (!flag)
			{
				foreach (Formation cavalryFormation in _cavalryFormations)
				{
					if (cavalryFormation.AI.ActiveBehavior == null)
					{
						flag2 = false;
						break;
					}
					if (!(cavalryFormation.GetReadonlyMovementOrderReference() == MovementOrder.MovementOrderRetreat) && cavalryFormation.AI.ActiveBehavior is BehaviorDestroySiegeWeapons behaviorDestroySiegeWeapons)
					{
						if (behaviorDestroySiegeWeapons.LastTargetWeapon == null)
						{
							flag2 = false;
							break;
						}
						Vec2 asVec = behaviorDestroySiegeWeapons.LastTargetWeapon.GameEntity.GlobalPosition.AsVec2;
						if (!(base.Team.QuerySystem.GetLocalEnemyPower(asVec) > base.Team.QuerySystem.GetLocalAllyPower(asVec) + cavalryFormation.QuerySystem.FormationPower))
						{
							flag2 = false;
							break;
						}
					}
				}
			}
			if (flag || flag2)
			{
				_state = TacticState.CavalryRetreating;
				CavalryRetreat();
				base.Team.TeamAI.NotifyTacticalDecision(new TacticalDecision(this, 31));
			}
			break;
		}
		case TacticState.CavalryRetreating:
			if (_cavalryFormations.IsEmpty() || TeamAISiegeComponent.IsFormationGroupInsideCastle(_cavalryFormations, includeOnlyPositionedUnits: false))
			{
				_state = TacticState.InfantryRetreating;
				InfantryRetreat();
			}
			break;
		}
	}

	public override void TickOccasionally()
	{
		if (!base.AreFormationsCreated)
		{
			return;
		}
		if (CheckAndSetAvailableFormationsChanged() || IsTacticReapplyNeeded)
		{
			ManageFormationCounts();
			switch (_state)
			{
			case TacticState.HeadingOutFromCastle:
				HeadOutFromTheCastle();
				break;
			case TacticState.DestroyingSiegeWeapons:
				DestroySiegeWeapons();
				break;
			case TacticState.CavalryRetreating:
				CavalryRetreat();
				break;
			case TacticState.InfantryRetreating:
				InfantryRetreat();
				break;
			}
			IsTacticReapplyNeeded = false;
		}
		CheckAndChangeState();
		base.TickOccasionally();
	}

	protected internal override float GetTacticWeight()
	{
		return 10f;
	}
}
