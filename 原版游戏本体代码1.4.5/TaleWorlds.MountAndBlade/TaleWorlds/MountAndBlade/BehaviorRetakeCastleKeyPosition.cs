using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class BehaviorRetakeCastleKeyPosition : BehaviorComponent
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

	private FormationAI.BehaviorSide _gatheringSide;

	public override float NavmeshlessTargetPositionPenalty => 1f;

	public BehaviorRetakeCastleKeyPosition(Formation formation)
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

	private FormationAI.BehaviorSide DetermineGatheringSide()
	{
		IEnumerable<SiegeLane> source = TeamAISiegeComponent.SiegeLanes.Where((SiegeLane sl) => sl.LaneSide != _behaviorSide && sl.LaneState != SiegeLane.LaneStateEnum.Conceited && sl.DefenderOrigin.IsValid);
		if (source.Any())
		{
			int nearestSafeSideDistance = source.Min((SiegeLane pgl) => SiegeQuerySystem.SideDistance(1 << (int)_behaviorSide, 1 << (int)pgl.LaneSide));
			return Extensions.MinBy(source.Where((SiegeLane pgl) => SiegeQuerySystem.SideDistance(1 << (int)_behaviorSide, 1 << (int)pgl.LaneSide) == nearestSafeSideDistance), (SiegeLane pgl) => pgl.DefenderOrigin.GetGroundVec3().DistanceSquared(base.Formation.CachedMedianPosition.GetGroundVec3())).LaneSide;
		}
		return _behaviorSide;
	}

	private void ConfirmGatheringSide()
	{
		SiegeLane siegeLane = TeamAISiegeComponent.SiegeLanes.FirstOrDefault((SiegeLane sl) => sl.LaneSide == _gatheringSide);
		if (siegeLane == null || siegeLane.LaneState >= SiegeLane.LaneStateEnum.Conceited)
		{
			ResetOrderPositions();
		}
	}

	private void ResetOrderPositions()
	{
		_behaviorState = BehaviorState.UnSet;
		_gatheringSide = DetermineGatheringSide();
		SiegeLane siegeLane = TeamAISiegeComponent.SiegeLanes.FirstOrDefault((SiegeLane sl) => sl.LaneSide == _gatheringSide);
		WorldFrame worldFrame = siegeLane.DefensePoints.FirstOrDefault((ICastleKeyPosition dp) => dp.AttackerSiegeWeapon is UsableMachine && !(dp.AttackerSiegeWeapon as UsableMachine).IsDisabled)?.DefenseWaitFrame ?? siegeLane.DefensePoints.FirstOrDefault().DefenseWaitFrame;
		_gatheringTacticalPos = siegeLane.DefensePoints.FirstOrDefault((ICastleKeyPosition dp) => dp.AttackerSiegeWeapon is UsableMachine && !(dp.AttackerSiegeWeapon as UsableMachine).IsDisabled)?.WaitPosition;
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
		SiegeLane siegeLane2 = TeamAISiegeComponent.SiegeLanes.FirstOrDefault((SiegeLane sl) => sl.LaneSide == _behaviorSide);
		_attackOrder = MovementOrder.MovementOrderMove(siegeLane2.DefensePoints.FirstOrDefault((ICastleKeyPosition dp) => dp.AttackerSiegeWeapon is UsableMachine && !(dp.AttackerSiegeWeapon as UsableMachine).IsDisabled)?.MiddleFrame.Origin ?? siegeLane2.DefensePoints.FirstOrDefault().MiddleFrame.Origin);
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
		bool flag = true;
		if (_behaviorState != BehaviorState.Attacking)
		{
			flag = base.Formation.CachedMedianPosition.GetNavMeshVec3().DistanceSquared(_gatherOrder.CreateNewOrderWorldPositionMT(base.Formation, WorldPosition.WorldPositionEnforcedCache.NavMeshVec3).GetNavMeshVec3()) < 100f || base.Formation.CachedFormationIntegrityData.DeviationOfPositionsExcludeFarAgents / ((base.Formation.QuerySystem.IdealAverageDisplacement != 0f) ? base.Formation.QuerySystem.IdealAverageDisplacement : 1f) <= 3f;
		}
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
