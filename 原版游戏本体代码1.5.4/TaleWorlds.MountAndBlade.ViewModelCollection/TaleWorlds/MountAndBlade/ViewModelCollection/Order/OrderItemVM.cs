using System;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order;

public class OrderItemVM : OrderItemBaseVM
{
	public readonly VisualOrder Order;

	public static event Action<OrderItemVM> OnExecuteOrder;

	public OrderItemVM(OrderController orderController, VisualOrder order)
		: base(orderController)
	{
		Order = order;
		base.OrderIconId = order.IconId;
		base.IsActive = true;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.Name = Order.GetName(_orderController)?.ToString();
	}

	protected override void OnRefreshState()
	{
		OrderState activeState = Order.GetActiveState(_orderController);
		base.IsActive = activeState == OrderState.Active;
		base.SelectionState = activeState.ToString();
		base.Name = Order.GetName(_orderController).ToString();
		base.OrderIconId = Order.IconId;
	}

	protected override void OnExecuteAction(VisualOrderExecutionParameters executionParameters)
	{
		Order.BeforeExecuteOrder(_orderController, executionParameters);
		Order.ExecuteOrder(_orderController, executionParameters);
		OrderItemVM.OnExecuteOrder?.Invoke(this);
	}
}
