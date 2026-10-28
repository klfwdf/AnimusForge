using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.MovementOrders;

public class StopVisualOrder : VisualOrder
{
	public StopVisualOrder(string iconId)
		: base(iconId)
	{
	}

	public override TextObject GetName(OrderController orderController)
	{
		return new TextObject("{=QTr6UDAa}Stop");
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		orderController.SetOrder(OrderType.StandYourGround);
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		return OrderController.GetActiveMovementOrderOf(formation) == OrderType.StandYourGround;
	}

	public override bool IsTargeted()
	{
		return false;
	}
}
