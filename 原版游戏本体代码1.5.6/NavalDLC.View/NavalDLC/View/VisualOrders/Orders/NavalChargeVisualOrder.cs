using NavalDLC.Missions.MissionLogics;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.MovementOrders;

namespace NavalDLC.View.VisualOrders.Orders;

public class NavalChargeVisualOrder : ChargeVisualOrder
{
	public NavalChargeVisualOrder(string iconId)
		: base(iconId)
	{
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		if (base.OnGetFormationHasOrder(formation) == true)
		{
			return true;
		}
		if (OrderController.GetActiveMovementOrderOf(formation) == OrderType.Move)
		{
			NavalShipsLogic navalShipsLogic = Mission.Current?.GetMissionBehavior<NavalShipsLogic>();
			if (navalShipsLogic != null && navalShipsLogic.GetShip(formation, out var ship))
			{
				return ship.ShipOrder?.GetIsChargeOrderOverridden() ?? false;
			}
		}
		return false;
	}
}
