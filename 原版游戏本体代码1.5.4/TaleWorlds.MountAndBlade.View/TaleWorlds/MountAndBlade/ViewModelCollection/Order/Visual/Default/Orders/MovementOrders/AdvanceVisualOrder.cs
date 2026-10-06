using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.MovementOrders;

public class AdvanceVisualOrder : VisualOrder
{
	public AdvanceVisualOrder(string iconId)
		: base(iconId)
	{
	}

	public override TextObject GetName(OrderController orderController)
	{
		return new TextObject("{=A38xbjqm}Engage");
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		if (executionParameters.HasFormation)
		{
			orderController.SetOrderWithFormation(OrderType.Advance, executionParameters.Formation);
		}
		else
		{
			orderController.SetOrder(OrderType.Advance);
		}
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		return OrderController.GetActiveMovementOrderOf(formation) == OrderType.Advance;
	}

	public override bool IsTargeted()
	{
		return true;
	}
}
