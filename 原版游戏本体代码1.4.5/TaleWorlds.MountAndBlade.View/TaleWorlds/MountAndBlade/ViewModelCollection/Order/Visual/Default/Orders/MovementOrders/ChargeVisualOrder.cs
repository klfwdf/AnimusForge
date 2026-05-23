using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.MovementOrders;

public class ChargeVisualOrder : VisualOrder
{
	public ChargeVisualOrder(string iconId)
		: base(iconId)
	{
	}

	public override TextObject GetName(OrderController orderController)
	{
		return new TextObject("{=Dxmq32qW}Charge");
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		if (executionParameters.HasFormation)
		{
			orderController.SetOrderWithFormation(OrderType.Charge, executionParameters.Formation);
		}
		else
		{
			orderController.SetOrder(OrderType.Charge);
		}
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		OrderType activeMovementOrderOf = OrderController.GetActiveMovementOrderOf(formation);
		return activeMovementOrderOf == OrderType.Charge || activeMovementOrderOf == OrderType.ChargeWithTarget;
	}

	public override bool IsTargeted()
	{
		return true;
	}
}
