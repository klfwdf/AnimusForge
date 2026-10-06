using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace NavalDLC.View.VisualOrders.Orders;

public class NavalMovementOrder : VisualOrder
{
	private OrderType _orderType;

	private bool _useWorldPosition;

	private bool _isTargeted;

	private TextObject _name;

	public NavalMovementOrder(string stringId, OrderType order, TextObject name, bool useWorldPosition = false, bool isTargeted = false)
		: base(stringId)
	{
		_orderType = order;
		_useWorldPosition = useWorldPosition;
		_isTargeted = isTargeted;
		_name = name;
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		if (_useWorldPosition && executionParameters.HasWorldPosition)
		{
			orderController.SetOrderWithPosition(_orderType, executionParameters.WorldPosition);
		}
		else if (_isTargeted && executionParameters.HasFormation)
		{
			orderController.SetOrderWithFormation(_orderType, executionParameters.Formation);
		}
		else
		{
			orderController.SetOrder(_orderType);
		}
	}

	public override TextObject GetName(OrderController orderController)
	{
		return _name;
	}

	public override bool IsTargeted()
	{
		return _isTargeted;
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		NavalShipsLogic missionBehavior = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		if (missionBehavior == null)
		{
			return false;
		}
		ShipOrder.ShipMovementOrderEnum movementOrderEnum = GetMovementOrderEnum();
		missionBehavior.GetShip(formation.Team.TeamSide, formation.FormationIndex, out var ship);
		if (ship == null)
		{
			return false;
		}
		if (ship.IsPlayerShip || ship.IsPlayerControlled)
		{
			return null;
		}
		return ship.ShipOrder.MovementOrderEnum == movementOrderEnum;
	}

	private ShipOrder.ShipMovementOrderEnum GetMovementOrderEnum()
	{
		switch (_orderType)
		{
		case OrderType.Move:
			return ShipOrder.ShipMovementOrderEnum.Move;
		case OrderType.FollowMe:
			return ShipOrder.ShipMovementOrderEnum.StaticOrderCount;
		case OrderType.Advance:
			return ShipOrder.ShipMovementOrderEnum.Engage;
		case OrderType.StandYourGround:
			return ShipOrder.ShipMovementOrderEnum.Stop;
		case OrderType.Retreat:
			return ShipOrder.ShipMovementOrderEnum.Retreat;
		default:
			Debug.FailedAssert("Failed to find corresponding ship order of: " + _orderType, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.View\\VisualOrders\\Orders\\NavalMovementOrder.cs", "GetMovementOrderEnum", 96);
			return ShipOrder.ShipMovementOrderEnum.Move;
		}
	}
}
