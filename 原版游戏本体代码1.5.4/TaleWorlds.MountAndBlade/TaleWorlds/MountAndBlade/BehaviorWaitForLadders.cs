using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class BehaviorWaitForLadders : BehaviorComponent
{
	private enum BehaviorState
	{
		Unset,
		Stop,
		Follow
	}

	private const string WallWaitPositionTag = "attacker_wait_pos";

	private List<SiegeLadder> _ladders;

	private WallSegment _breachedWallSegment;

	private TeamAISiegeComponent _teamAISiegeComponent;

	private MovementOrder _stopOrder;

	private MovementOrder _followOrder;

	private BehaviorState _behaviorState;

	private GameEntity _followedEntity;

	private TacticalPosition _followTacticalPosition;

	public override float NavmeshlessTargetPositionPenalty => 1f;

	public BehaviorWaitForLadders(Formation formation)
		: base(formation)
	{
		_behaviorSide = formation.AI.Side;
		_ladders = Mission.Current.ActiveMissionObjects.OfType<SiegeLadder>().ToList();
		_ladders.RemoveAll((SiegeLadder l) => l.IsDeactivated || l.WeaponSide != _behaviorSide);
		_teamAISiegeComponent = (TeamAISiegeComponent)formation.Team.TeamAI;
		_breachedWallSegment = TeamAISiegeComponent.SiegeLanes.FirstOrDefault((SiegeLane sl) => sl.LaneSide == _behaviorSide)?.DefensePoints.FirstOrDefault((ICastleKeyPosition dp) => dp is WallSegment && (dp as WallSegment).IsBreachedWall) as WallSegment;
		ResetFollowOrder();
		_stopOrder = MovementOrder.MovementOrderStop;
		if (_followOrder.OrderEnum != MovementOrder.MovementOrderEnum.Invalid)
		{
			base.CurrentOrder = _followOrder;
			_behaviorState = BehaviorState.Follow;
		}
		else
		{
			base.CurrentOrder = _stopOrder;
			_behaviorState = BehaviorState.Stop;
		}
	}

	private void ResetFollowOrder()
	{
		_followedEntity = null;
		_followTacticalPosition = null;
		if (_ladders.Count > 0)
		{
			_followedEntity = (_ladders.FirstOrDefault((SiegeLadder l) => !l.IsDeactivated && l.InitialWaitPosition.HasScriptOfType<TacticalPosition>()) ?? _ladders.FirstOrDefault((SiegeLadder l) => !l.IsDeactivated)).InitialWaitPosition;
			if (_followedEntity == null)
			{
				_followedEntity = _ladders.FirstOrDefault((SiegeLadder l) => !l.IsDeactivated).InitialWaitPosition;
			}
			_followOrder = MovementOrder.MovementOrderFollowEntity(_followedEntity);
		}
		else if (_breachedWallSegment != null)
		{
			WeakGameEntity firstChildEntityWithTagRecursive = _breachedWallSegment.GameEntity.GetFirstChildEntityWithTagRecursive("attacker_wait_pos");
			_followedEntity = GameEntity.CreateFromWeakEntity(firstChildEntityWithTagRecursive);
			_followOrder = MovementOrder.MovementOrderFollowEntity(_followedEntity);
		}
		else
		{
			_followOrder = MovementOrder.MovementOrderNull;
		}
		if (_followedEntity != null)
		{
			_followTacticalPosition = _followedEntity.GetFirstScriptOfType<TacticalPosition>();
		}
	}

	public override void OnValidBehaviorSideChanged()
	{
		base.OnValidBehaviorSideChanged();
		_ladders = Mission.Current.ActiveMissionObjects.OfType<SiegeLadder>().ToList();
		_ladders.RemoveAll((SiegeLadder l) => l.IsDeactivated || l.WeaponSide != _behaviorSide);
		_breachedWallSegment = TeamAISiegeComponent.SiegeLanes.FirstOrDefault((SiegeLane sl) => sl.LaneSide == _behaviorSide)?.DefensePoints.FirstOrDefault((ICastleKeyPosition dp) => dp is WallSegment && (dp as WallSegment).IsBreachedWall) as WallSegment;
		ResetFollowOrder();
		_behaviorState = BehaviorState.Unset;
	}

	protected override void CalculateCurrentOrder()
	{
		BehaviorState behaviorState = ((_followOrder.OrderEnum == MovementOrder.MovementOrderEnum.Invalid) ? BehaviorState.Stop : BehaviorState.Follow);
		if (behaviorState == _behaviorState)
		{
			return;
		}
		if (behaviorState == BehaviorState.Follow)
		{
			base.CurrentOrder = _followOrder;
			if (_followTacticalPosition != null)
			{
				CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(_followTacticalPosition.Direction);
			}
			else
			{
				CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
			}
		}
		else
		{
			base.CurrentOrder = _stopOrder;
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
		}
		_behaviorState = behaviorState;
	}

	public override void TickOccasionally()
	{
		base.TickOccasionally();
		if (_ladders.RemoveAll((SiegeLadder l) => l.IsDeactivated) > 0)
		{
			ResetFollowOrder();
			CalculateCurrentOrder();
		}
		base.Formation.SetMovementOrder(base.CurrentOrder);
		base.Formation.SetFacingOrder(CurrentFacingOrder);
		if (_behaviorState == BehaviorState.Follow && _followTacticalPosition != null)
		{
			base.Formation.SetFormOrder(FormOrder.FormOrderCustom(_followTacticalPosition.Width));
		}
		foreach (SiegeLadder ladder in _ladders)
		{
			if (ladder.IsUsedByFormation(base.Formation))
			{
				base.Formation.StopUsingMachine(ladder);
			}
		}
	}

	protected override void OnBehaviorActivatedAux()
	{
		base.Formation.SetArrangementOrder(base.Formation.QuerySystem.HasShield ? ArrangementOrder.ArrangementOrderShieldWall : ArrangementOrder.ArrangementOrderLine);
		base.Formation.SetFacingOrder(FacingOrder.FacingOrderLookAtEnemy);
		base.Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
		base.Formation.SetFormOrder(FormOrder.FormOrderWide);
	}

	protected override float GetAiWeight()
	{
		float result = 0f;
		if (_followOrder.OrderEnum != MovementOrder.MovementOrderEnum.Invalid && !_teamAISiegeComponent.AreLaddersReady)
		{
			result = ((!_teamAISiegeComponent.IsCastleBreached()) ? 1f : 0.5f);
		}
		return result;
	}
}
