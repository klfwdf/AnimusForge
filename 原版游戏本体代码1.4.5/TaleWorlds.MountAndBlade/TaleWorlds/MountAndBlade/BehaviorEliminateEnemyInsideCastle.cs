using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class BehaviorEliminateEnemyInsideCastle : BehaviorComponent
{
	private enum BehaviorState
	{
		UnSet,
		Gathering,
		Attacking
	}

	private BehaviorState _behaviorState;

	private MovementOrder _gatherOrder;

	private MovementOrder _attackOrder;

	private FacingOrder _gatheringFacingOrder;

	private FacingOrder _attackFacingOrder;

	private TacticalPosition _gatheringTacticalPos;

	private Formation _targetEnemyFormation;

	public override float NavmeshlessTargetPositionPenalty => 1f;

	public BehaviorEliminateEnemyInsideCastle(Formation formation)
		: base(formation)
	{
		_behaviorState = BehaviorState.UnSet;
		_behaviorSide = formation.AI.Side;
		ResetOrderPositions();
	}

	protected override void CalculateCurrentOrder()
	{
		base.CalculateCurrentOrder();
		base.CurrentOrder = ((_behaviorState == BehaviorState.Attacking) ? _attackOrder : _gatherOrder);
	}

	private void DetermineMostImportantInvadingEnemyFormation()
	{
		float num = float.MinValue;
		_targetEnemyFormation = null;
		foreach (Team team in base.Formation.Team.Mission.Teams)
		{
			if (!team.IsEnemyOf(base.Formation.Team))
			{
				continue;
			}
			for (int i = 0; i < Math.Min(team.FormationsIncludingSpecialAndEmpty.Count, 8); i++)
			{
				Formation formation = team.FormationsIncludingSpecialAndEmpty[i];
				if (formation.CountOfUnits > 0 && TeamAISiegeComponent.IsFormationInsideCastle(formation, includeOnlyPositionedUnits: true))
				{
					float formationPower = formation.QuerySystem.FormationPower;
					if (formationPower > num)
					{
						num = formationPower;
						_targetEnemyFormation = formation;
					}
				}
			}
		}
	}

	private void ConfirmGatheringSide()
	{
		SiegeLane siegeLane = TeamAISiegeComponent.SiegeLanes.FirstOrDefault((SiegeLane sl) => sl.LaneSide == _behaviorSide);
		if (siegeLane == null || siegeLane.LaneState >= SiegeLane.LaneStateEnum.Conceited)
		{
			ResetOrderPositions();
		}
	}

	private FormationAI.BehaviorSide DetermineGatheringSide()
	{
		DetermineMostImportantInvadingEnemyFormation();
		if (_targetEnemyFormation == null)
		{
			if (_behaviorState == BehaviorState.Attacking)
			{
				_behaviorState = BehaviorState.UnSet;
			}
			return _behaviorSide;
		}
		int connectedSides = TeamAISiegeComponent.QuerySystem.DeterminePositionAssociatedSide(_targetEnemyFormation.CachedMedianPosition.GetNavMeshVec3());
		IEnumerable<SiegeLane> source = TeamAISiegeComponent.SiegeLanes.Where((SiegeLane sl) => sl.LaneState != SiegeLane.LaneStateEnum.Conceited && !SiegeQuerySystem.AreSidesRelated(sl.LaneSide, connectedSides));
		FormationAI.BehaviorSide result = _behaviorSide;
		if (source.Any())
		{
			if (source.Count() > 1)
			{
				int leastDangerousLaneState = source.Min((SiegeLane pgl) => (int)pgl.LaneState);
				IEnumerable<SiegeLane> source2 = source.Where((SiegeLane pgl) => pgl.LaneState == (SiegeLane.LaneStateEnum)leastDangerousLaneState);
				result = ((source2.Count() > 1) ? Extensions.MinBy(source2, (SiegeLane ldl) => SiegeQuerySystem.SideDistance(1 << connectedSides, 1 << (int)ldl.LaneSide)).LaneSide : source2.First().LaneSide);
			}
			else
			{
				result = source.First().LaneSide;
			}
		}
		return result;
	}

	private void ResetOrderPositions()
	{
		_behaviorSide = DetermineGatheringSide();
		SiegeLane siegeLane = TeamAISiegeComponent.SiegeLanes.FirstOrDefault((SiegeLane sl) => sl.LaneSide == _behaviorSide);
		WorldFrame worldFrame = siegeLane?.DefensePoints?.FirstOrDefault((ICastleKeyPosition dp) => dp.AttackerSiegeWeapon is UsableMachine && !(dp.AttackerSiegeWeapon as UsableMachine).IsDisabled)?.DefenseWaitFrame ?? WorldFrame.Invalid;
		_gatheringTacticalPos = siegeLane?.DefensePoints?.FirstOrDefault((ICastleKeyPosition dp) => dp.AttackerSiegeWeapon is UsableMachine && !(dp.AttackerSiegeWeapon as UsableMachine).IsDisabled)?.WaitPosition ?? siegeLane?.DefensePoints?.FirstOrDefault().WaitPosition;
		if (_gatheringTacticalPos != null)
		{
			_gatherOrder = MovementOrder.MovementOrderMove(_gatheringTacticalPos.Position);
			_gatheringFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
		}
		else if (worldFrame.Origin.IsValid)
		{
			worldFrame.Rotation.f.Normalize();
			_gatherOrder = MovementOrder.MovementOrderMove(worldFrame.Origin);
			_gatheringFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
		}
		else
		{
			_gatherOrder = MovementOrder.MovementOrderMove(base.Formation.CachedMedianPosition);
			_gatheringFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
		}
		_attackOrder = MovementOrder.MovementOrderChargeToTarget(_targetEnemyFormation);
		_attackFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
		base.CurrentOrder = ((_behaviorState == BehaviorState.Attacking) ? _attackOrder : _gatherOrder);
		CurrentFacingOrder = ((_behaviorState == BehaviorState.Attacking) ? _attackFacingOrder : _gatheringFacingOrder);
	}

	public override void OnValidBehaviorSideChanged()
	{
		base.OnValidBehaviorSideChanged();
		ResetOrderPositions();
	}

	public override void TickOccasionally()
	{
		base.TickOccasionally();
		if (_behaviorState != BehaviorState.Attacking)
		{
			ConfirmGatheringSide();
		}
		bool flag = ((_behaviorState != BehaviorState.Attacking) ? (_targetEnemyFormation != null && (base.Formation.CachedMedianPosition.GetNavMeshVec3().DistanceSquared(_gatherOrder.CreateNewOrderWorldPositionMT(base.Formation, WorldPosition.WorldPositionEnforcedCache.NavMeshVec3).GetNavMeshVec3()) < 100f || base.Formation.CachedFormationIntegrityData.DeviationOfPositionsExcludeFarAgents / ((base.Formation.QuerySystem.IdealAverageDisplacement != 0f) ? base.Formation.QuerySystem.IdealAverageDisplacement : 1f) <= 3f)) : (_targetEnemyFormation != null));
		BehaviorState behaviorState = ((!flag) ? BehaviorState.Gathering : BehaviorState.Attacking);
		if (behaviorState != _behaviorState)
		{
			_behaviorState = behaviorState;
			base.CurrentOrder = ((_behaviorState == BehaviorState.Attacking) ? _attackOrder : _gatherOrder);
			CurrentFacingOrder = ((_behaviorState == BehaviorState.Attacking) ? _attackFacingOrder : _gatheringFacingOrder);
		}
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		if (_behaviorState == BehaviorState.Gathering && _gatheringTacticalPos != null)
		{
			base.Formation.SetFormOrder(FormOrder.FormOrderCustom(_gatheringTacticalPos.Width));
		}
	}

	protected override void OnBehaviorActivatedAux()
	{
		_behaviorState = BehaviorState.UnSet;
		_behaviorSide = base.Formation.AI.Side;
		ResetOrderPositions();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	protected override float GetAiWeight()
	{
		return 1f;
	}
}
