using TaleWorlds.Engine;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.MovementOrders;

public class MoveVisualOrder : VisualOrder
{
	public MoveVisualOrder(string iconId)
		: base(iconId)
	{
	}

	public override TextObject GetName(OrderController orderController)
	{
		return new TextObject("{=vbAZwibd}Move to Position");
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		if (executionParameters.HasWorldPosition)
		{
			WorldPosition worldPosition = executionParameters.WorldPosition;
			orderController.SetOrderWithTwoPositions(OrderType.MoveToLineSegment, worldPosition, worldPosition);
		}
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		OrderType activeMovementOrderOf = OrderController.GetActiveMovementOrderOf(formation);
		return activeMovementOrderOf == OrderType.Move || activeMovementOrderOf == OrderType.MoveToLineSegment || activeMovementOrderOf == OrderType.MoveToLineSegmentWithHorizontalLayout;
	}

	public override bool IsTargeted()
	{
		return false;
	}
}
