using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.MovementOrders;

public class FallbackVisualOrder : VisualOrder
{
	public FallbackVisualOrder(string iconId)
		: base(iconId)
	{
	}

	public override TextObject GetName(OrderController orderController)
	{
		return new TextObject("{=WhUoF9Mw}Fallback");
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		orderController.SetOrder(OrderType.FallBack);
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		return OrderController.GetActiveMovementOrderOf(formation) == OrderType.FallBack;
	}

	public override bool IsTargeted()
	{
		return false;
	}
}
