using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace TaleWorlds.MountAndBlade.View.VisualOrders.OrderSets;

public class SingleVisualOrderSet : VisualOrderSet
{
	public readonly VisualOrder Order;

	private bool _isInitializing;

	public override bool IsSoloOrder => !_isInitializing;

	public override string StringId => Order.StringId;

	public override string IconId => Order.IconId;

	public override TextObject GetName(OrderController orderController)
	{
		return Order.GetName(orderController);
	}

	public SingleVisualOrderSet(VisualOrder order)
	{
		_isInitializing = true;
		Order = order;
		AddOrder(Order);
		_isInitializing = false;
	}
}
