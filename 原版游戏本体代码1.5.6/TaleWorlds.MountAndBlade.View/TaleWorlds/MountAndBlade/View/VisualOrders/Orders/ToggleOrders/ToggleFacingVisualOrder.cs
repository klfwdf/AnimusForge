using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace TaleWorlds.MountAndBlade.View.VisualOrders.Orders.ToggleOrders;

public class ToggleFacingVisualOrder : VisualOrder
{
	public ToggleFacingVisualOrder(string iconId)
		: base(iconId)
	{
	}

	public override TextObject GetName(OrderController orderController)
	{
		OrderState activeState = GetActiveState(orderController);
		if (activeState == OrderState.Active || activeState == OrderState.PartiallyActive)
		{
			return new TextObject("{=qWzBa3KT}Facing Enemy");
		}
		return new TextObject("{=LWVwNcRA}Facing Direction");
	}

	public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
	{
		if (IsFacingEnemy(GetActiveState(orderController)))
		{
			orderController.SetOrderWithPosition(OrderType.LookAtDirection, executionParameters.WorldPosition);
		}
		else
		{
			orderController.SetOrder(OrderType.LookAtEnemy);
		}
	}

	public override bool IsTargeted()
	{
		return false;
	}

	protected override bool? OnGetFormationHasOrder(Formation formation)
	{
		return OrderController.GetActiveFacingOrderOf(formation) == OrderType.LookAtEnemy;
	}

	protected override string GetIconId()
	{
		string iconId = base.GetIconId();
		if (_lastActiveState == OrderState.Active)
		{
			return iconId + "_active";
		}
		return iconId;
	}

	private static bool IsFacingEnemy(OrderState activeState)
	{
		return activeState == OrderState.Active;
	}
}
