using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace NavalDLC.View.VisualOrders.Orders;

public class NavalSkirmishOrder : VisualOrder
{
	private NavalShipsLogic _shipsLogic;

	public NavalSkirmishOrder(string stringId)
		: base(stringId)
	{
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		if (!executionParameters.HasFormation)
		{
			orderController.SetOrder(OrderType.Dismount);
		}
		else
		{
			orderController.SetOrderWithFormation(OrderType.Dismount, executionParameters.Formation);
		}
	}

	public override TextObject GetName(OrderController orderController)
	{
		return new TextObject("{=skirmishOrder}Skirmish");
	}

	public override bool IsTargeted()
	{
		return true;
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		if (_shipsLogic == null)
		{
			_shipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		}
		if (_shipsLogic != null)
		{
			_shipsLogic.GetShip(formation.Team.TeamSide, formation.FormationIndex, out var ship);
			if (ship != null)
			{
				return ship.ShipOrder.MovementOrderEnum == ShipOrder.ShipMovementOrderEnum.Skirmish;
			}
		}
		return VisualOrderHelper.DoesFormationHaveOrderType(formation, OrderType.Dismount);
	}
}
