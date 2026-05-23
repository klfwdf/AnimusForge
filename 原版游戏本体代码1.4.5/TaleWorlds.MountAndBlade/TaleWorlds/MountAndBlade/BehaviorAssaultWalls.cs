using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class BehaviorAssaultWalls : BehaviorComponent
{
	private enum BehaviorState
	{
		Deciding,
		ClimbWall,
		AttackEntity,
		TakeControl,
		MoveToGate,
		Charging,
		Stop
	}

	private BehaviorState _behaviorState;

	private List<IPrimarySiegeWeapon> _primarySiegeWeapons;

	private WallSegment _wallSegment;

	private CastleGate _innerGate;

	private TeamAISiegeComponent _teamAISiegeComponent;

	private MovementOrder _attackEntityOrderInnerGate;

	private MovementOrder _attackEntityOrderOuterGate;

	private MovementOrder _chargeOrder;

	private MovementOrder _stopOrder;

	private MovementOrder _castleGateMoveOrder;

	private MovementOrder _wallSegmentMoveOrder;

	private FacingOrder _facingOrder;

	protected ArrangementOrder CurrentArrangementOrder;

	private bool _isGateLane;

	public override float NavmeshlessTargetPositionPenalty => 1f;

	private void ResetOrderPositions()
	{
		_primarySiegeWeapons = _teamAISiegeComponent.PrimarySiegeWeapons.ToList();
		_primarySiegeWeapons.RemoveAll((IPrimarySiegeWeapon uM) => uM.WeaponSide != _behaviorSide || !(uM is SiegeWeapon siegeWeapon) || (siegeWeapon.IsDeactivated && !siegeWeapon.IsDestroyed && (!(siegeWeapon is IPrimarySiegeWeapon primarySiegeWeapon2) || !primarySiegeWeapon2.HasCompletedAction())));
		IEnumerable<ICastleKeyPosition> source = TeamAISiegeComponent.SiegeLanes.Where((SiegeLane sl) => sl.LaneSide == _behaviorSide).SelectMany((SiegeLane sila) => sila.DefensePoints);
		_innerGate = _teamAISiegeComponent.InnerGate;
		_isGateLane = _teamAISiegeComponent.OuterGate.DefenseSide == _behaviorSide;
		if (_isGateLane)
		{
			_wallSegment = null;
		}
		else if (source.FirstOrDefault((ICastleKeyPosition dp) => dp is WallSegment && (dp as WallSegment).IsBreachedWall) is WallSegment wallSegment)
		{
			_wallSegment = wallSegment;
		}
		else
		{
			IPrimarySiegeWeapon primarySiegeWeapon = Extensions.MaxBy(_primarySiegeWeapons, (IPrimarySiegeWeapon psw) => psw.SiegeWeaponPriority);
			_wallSegment = primarySiegeWeapon.TargetCastlePosition as WallSegment;
		}
		_stopOrder = MovementOrder.MovementOrderStop;
		_chargeOrder = MovementOrder.MovementOrderCharge;
		bool flag = _teamAISiegeComponent.OuterGate != null && _behaviorSide == _teamAISiegeComponent.OuterGate.DefenseSide;
		_attackEntityOrderOuterGate = ((flag && !_teamAISiegeComponent.OuterGate.IsDeactivated && _teamAISiegeComponent.OuterGate.State != CastleGate.GateState.Open) ? MovementOrder.MovementOrderAttackEntity(GameEntity.CreateFromWeakEntity(_teamAISiegeComponent.OuterGate.GameEntity), surroundEntity: false) : MovementOrder.MovementOrderStop);
		_attackEntityOrderInnerGate = ((flag && _teamAISiegeComponent.InnerGate != null && !_teamAISiegeComponent.InnerGate.IsDeactivated && _teamAISiegeComponent.InnerGate.State != CastleGate.GateState.Open) ? MovementOrder.MovementOrderAttackEntity(GameEntity.CreateFromWeakEntity(_teamAISiegeComponent.InnerGate.GameEntity), surroundEntity: false) : MovementOrder.MovementOrderStop);
		WorldPosition origin = _teamAISiegeComponent.OuterGate.MiddleFrame.Origin;
		_castleGateMoveOrder = MovementOrder.MovementOrderMove(origin);
		if (_isGateLane)
		{
			_wallSegmentMoveOrder = _castleGateMoveOrder;
		}
		else
		{
			WorldPosition origin2 = _wallSegment.MiddleFrame.Origin;
			_wallSegmentMoveOrder = MovementOrder.MovementOrderMove(origin2);
		}
		_facingOrder = FacingOrder.FacingOrderLookAtEnemy;
	}

	public BehaviorAssaultWalls(Formation formation)
		: base(formation)
	{
		base.BehaviorCoherence = 0f;
		_behaviorSide = formation.AI.Side;
		_teamAISiegeComponent = (TeamAISiegeComponent)formation.Team.TeamAI;
		_behaviorState = BehaviorState.Deciding;
		ResetOrderPositions();
		base.CurrentOrder = _stopOrder;
	}

	public override TextObject GetBehaviorString()
	{
		TextObject behaviorString = base.GetBehaviorString();
		TextObject variable = GameTexts.FindText("str_formation_ai_side_strings", base.Formation.AI.Side.ToString());
		behaviorString.SetTextVariable("SIDE_STRING", variable);
		behaviorString.SetTextVariable("IS_GENERAL_SIDE", "0");
		return behaviorString;
	}

	private BehaviorState CheckAndChangeState()
	{
		switch (_behaviorState)
		{
		case BehaviorState.Deciding:
			if (!_isGateLane && _wallSegment == null)
			{
				return BehaviorState.Charging;
			}
			if (_isGateLane)
			{
				if (_teamAISiegeComponent.OuterGate.IsGateOpen && _teamAISiegeComponent.InnerGate.IsGateOpen)
				{
					return BehaviorState.Charging;
				}
				return BehaviorState.AttackEntity;
			}
			return BehaviorState.ClimbWall;
		case BehaviorState.ClimbWall:
		{
			if (_wallSegment == null)
			{
				return BehaviorState.Charging;
			}
			bool flag = false;
			if (_behaviorSide < FormationAI.BehaviorSide.BehaviorSideNotSet)
			{
				SiegeLane siegeLane = TeamAISiegeComponent.SiegeLanes[(int)_behaviorSide];
				flag = siegeLane.IsUnderAttack() && !siegeLane.IsDefended();
			}
			if (flag || base.Formation.CachedMedianPosition.GetNavMeshVec3().DistanceSquared(_wallSegment.MiddleFrame.Origin.GetNavMeshVec3()) < base.Formation.Depth * base.Formation.Depth)
			{
				return BehaviorState.TakeControl;
			}
			return BehaviorState.ClimbWall;
		}
		case BehaviorState.TakeControl:
			if (base.Formation.CachedClosestEnemyFormation != null)
			{
				if (!TeamAISiegeComponent.SiegeLanes.FirstOrDefault((SiegeLane sl) => sl.LaneSide == _behaviorSide).IsDefended())
				{
					if (!_teamAISiegeComponent.OuterGate.IsGateOpen || !_teamAISiegeComponent.InnerGate.IsGateOpen)
					{
						return BehaviorState.MoveToGate;
					}
					return BehaviorState.Charging;
				}
				return BehaviorState.TakeControl;
			}
			return BehaviorState.Stop;
		case BehaviorState.AttackEntity:
			if (_teamAISiegeComponent.OuterGate.IsGateOpen && _teamAISiegeComponent.InnerGate.IsGateOpen)
			{
				return BehaviorState.Charging;
			}
			return BehaviorState.AttackEntity;
		case BehaviorState.MoveToGate:
			if (_teamAISiegeComponent.OuterGate.IsGateOpen && _teamAISiegeComponent.InnerGate.IsGateOpen)
			{
				return BehaviorState.Charging;
			}
			return BehaviorState.MoveToGate;
		case BehaviorState.Charging:
			if ((!_isGateLane || !_teamAISiegeComponent.OuterGate.IsGateOpen || !_teamAISiegeComponent.InnerGate.IsGateOpen) && _behaviorSide < FormationAI.BehaviorSide.BehaviorSideNotSet)
			{
				if (!TeamAISiegeComponent.SiegeLanes[(int)_behaviorSide].IsOpen && !TeamAISiegeComponent.IsFormationInsideCastle(base.Formation, includeOnlyPositionedUnits: true))
				{
					return BehaviorState.Deciding;
				}
				if (base.Formation.CachedClosestEnemyFormation == null)
				{
					return BehaviorState.Stop;
				}
			}
			return BehaviorState.Charging;
		default:
			if (base.Formation.CachedClosestEnemyFormation != null)
			{
				return BehaviorState.Deciding;
			}
			return BehaviorState.Stop;
		}
	}

	protected override void CalculateCurrentOrder()
	{
		switch (_behaviorState)
		{
		case BehaviorState.Deciding:
			base.CurrentOrder = _stopOrder;
			break;
		case BehaviorState.ClimbWall:
			base.CurrentOrder = _wallSegmentMoveOrder;
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(-_wallSegment.MiddleFrame.Rotation.f.AsVec2.Normalized());
			CurrentArrangementOrder = ArrangementOrder.ArrangementOrderLine;
			break;
		case BehaviorState.TakeControl:
			base.CurrentOrder = ((base.Formation.CachedClosestEnemyFormation != null) ? MovementOrder.MovementOrderChargeToTarget(base.Formation.CachedClosestEnemyFormation.Formation) : MovementOrder.MovementOrderCharge);
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(-_wallSegment.MiddleFrame.Rotation.f.AsVec2.Normalized());
			CurrentArrangementOrder = ArrangementOrder.ArrangementOrderLine;
			break;
		case BehaviorState.AttackEntity:
			base.CurrentOrder = ((!_teamAISiegeComponent.OuterGate.IsGateOpen) ? _attackEntityOrderOuterGate : _attackEntityOrderInnerGate);
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
			CurrentArrangementOrder = ArrangementOrder.ArrangementOrderLine;
			break;
		case BehaviorState.MoveToGate:
			base.CurrentOrder = _castleGateMoveOrder;
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(-_innerGate.MiddleFrame.Rotation.f.AsVec2.Normalized());
			CurrentArrangementOrder = ArrangementOrder.ArrangementOrderLine;
			break;
		case BehaviorState.Charging:
			base.CurrentOrder = _chargeOrder;
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
			CurrentArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
			break;
		case BehaviorState.Stop:
			base.CurrentOrder = _chargeOrder;
			break;
		}
	}

	public override void OnValidBehaviorSideChanged()
	{
		base.OnValidBehaviorSideChanged();
		ResetOrderPositions();
		_behaviorState = BehaviorState.Deciding;
	}

	public override void TickOccasionally()
	{
		BehaviorState behaviorState = CheckAndChangeState();
		_behaviorState = behaviorState;
		CalculateCurrentOrder();
		foreach (IPrimarySiegeWeapon primarySiegeWeapon in _primarySiegeWeapons)
		{
			UsableMachine usableMachine = primarySiegeWeapon as UsableMachine;
			if (!usableMachine.IsDeactivated && !primarySiegeWeapon.HasCompletedAction() && !usableMachine.IsUsedByFormation(base.Formation))
			{
				base.Formation.StartUsingMachine(primarySiegeWeapon as UsableMachine);
			}
		}
		if (_behaviorState == BehaviorState.MoveToGate || _behaviorState == BehaviorState.Stop || _behaviorState == BehaviorState.Charging || _behaviorState == BehaviorState.TakeControl)
		{
			CastleGate innerGate = _teamAISiegeComponent.InnerGate;
			if (innerGate != null && !innerGate.IsGateOpen && !innerGate.IsDestroyed)
			{
				if (!innerGate.IsUsedByFormation(base.Formation))
				{
					base.Formation.StartUsingMachine(innerGate);
				}
			}
			else
			{
				innerGate = _teamAISiegeComponent.OuterGate;
				if (innerGate != null && !innerGate.IsGateOpen && !innerGate.IsDestroyed && !innerGate.IsUsedByFormation(base.Formation))
				{
					base.Formation.StartUsingMachine(innerGate);
				}
			}
		}
		else
		{
			if (base.Formation.Detachments.Contains(_teamAISiegeComponent.OuterGate))
			{
				base.Formation.StopUsingMachine(_teamAISiegeComponent.OuterGate);
			}
			if (base.Formation.Detachments.Contains(_teamAISiegeComponent.InnerGate))
			{
				base.Formation.StopUsingMachine(_teamAISiegeComponent.InnerGate);
			}
		}
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetArrangementOrder(CurrentArrangementOrder);
	}

	protected override void OnBehaviorActivatedAux()
	{
		CalculateCurrentOrder();
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
		base.Formation.SetFormOrder(FormOrder.FormOrderDeep);
	}

	protected override float GetAiWeight()
	{
		float result = 0f;
		if (_teamAISiegeComponent != null)
		{
			if (_primarySiegeWeapons.Any((IPrimarySiegeWeapon psw) => psw.HasCompletedAction()) || _wallSegment != null)
			{
				result = ((!_teamAISiegeComponent.IsCastleBreached()) ? 0.25f : 0.75f);
			}
			else if (_teamAISiegeComponent.OuterGate.DefenseSide == _behaviorSide)
			{
				result = 0.1f;
			}
		}
		return result;
	}
}
