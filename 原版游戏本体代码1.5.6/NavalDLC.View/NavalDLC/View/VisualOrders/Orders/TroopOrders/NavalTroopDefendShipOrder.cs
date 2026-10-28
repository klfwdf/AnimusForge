using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace NavalDLC.View.VisualOrders.Orders.TroopOrders;

public class NavalTroopDefendShipOrder : VisualOrder
{
	public NavalTroopDefendShipOrder(string iconId)
		: base(iconId)
	{
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		orderController.SetOrder(OrderType.Mount);
	}

	public override TextObject GetName(OrderController orderController)
	{
		return new TextObject("{=FUeeV5aO}Defend Ship");
	}

	public override bool IsTargeted()
	{
		return false;
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		return VisualOrderHelper.DoesFormationHaveOrderType(formation, OrderType.Mount);
	}
}
