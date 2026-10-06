using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.MovementOrders;

public class FollowMeVisualOrder : VisualOrder
{
	public FollowMeVisualOrder(string iconId)
		: base(iconId)
	{
	}

	public override TextObject GetName(OrderController orderController)
	{
		return new TextObject("{=5LpufKs7}Follow Me");
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		orderController.SetOrderWithAgent(OrderType.FollowMe, executionParameters.Agent);
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		return OrderController.GetActiveMovementOrderOf(formation) == OrderType.FollowMe;
	}

	public override bool IsTargeted()
	{
		return false;
	}
}
