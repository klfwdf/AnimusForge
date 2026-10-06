using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace NavalDLC.View.VisualOrders.Orders;

public class NavalToggleVisualOrder : VisualOrder
{
	private OrderType _positiveOrder;

	private OrderType _negativeOrder;

	private TextObject _positiveOrderName;

	private TextObject _negativeOrderName;

	public NavalToggleVisualOrder(string stringId, OrderType positiveOrder, OrderType negativeOrder, TextObject positiveOrderName, TextObject negativeOrderName)
		: base(stringId)
	{
		_positiveOrder = positiveOrder;
		_negativeOrder = negativeOrder;
		_positiveOrderName = positiveOrderName;
		_negativeOrderName = negativeOrderName;
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		if (GetActiveState(orderController) == OrderState.Active)
		{
			orderController.SetOrder(_negativeOrder);
		}
		else
		{
			orderController.SetOrder(_positiveOrder);
		}
	}

	public override TextObject GetName(OrderController orderController)
	{
		OrderState activeState = GetActiveState(orderController);
		if (activeState == OrderState.Active || activeState == OrderState.PartiallyActive)
		{
			return _positiveOrderName;
		}
		return _negativeOrderName;
	}

	public override bool IsTargeted()
	{
		return false;
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		NavalShipsLogic missionBehavior = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		if (missionBehavior == null)
		{
			return false;
		}
		missionBehavior.GetShip(formation.Team.TeamSide, formation.FormationIndex, out var ship);
		switch ((int)_positiveOrder)
		{
		case 14:
			return ship.ShipOrder.BoardAtWill;
		case 35:
			if (formation.GetReadonlyMovementOrderReference().OrderEnum != MovementOrder.MovementOrderEnum.Charge)
			{
				return false;
			}
			return true;
		default:
			return VisualOrderHelper.DoesFormationHaveOrderType(formation, _positiveOrder);
		}
	}

	protected override string GetIconId()
	{
		string iconId = base.GetIconId();
		if (_lastActiveState == OrderState.Active)
		{
			return iconId + "_active";
		}
		return iconId;
	}
}
