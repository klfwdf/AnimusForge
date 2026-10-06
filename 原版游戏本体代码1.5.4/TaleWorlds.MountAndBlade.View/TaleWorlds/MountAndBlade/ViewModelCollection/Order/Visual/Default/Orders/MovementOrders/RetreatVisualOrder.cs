using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.MovementOrders;

public class RetreatVisualOrder : VisualOrder
{
	public RetreatVisualOrder(string iconId)
		: base(iconId)
	{
	}

	public override TextObject GetName(OrderController orderController)
	{
		return new TextObject("{=VbeHEAsa}Retreat");
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		orderController.SetOrder(OrderType.Retreat);
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		return OrderController.GetActiveMovementOrderOf(formation) == OrderType.Retreat;
	}

	public override bool IsTargeted()
	{
		return false;
	}
}
