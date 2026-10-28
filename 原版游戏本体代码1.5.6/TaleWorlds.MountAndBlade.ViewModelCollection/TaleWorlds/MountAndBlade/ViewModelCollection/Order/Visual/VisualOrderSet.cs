using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

public abstract class VisualOrderSet
{
	private MBList<VisualOrder> _orders;

	public MBReadOnlyList<VisualOrder> Orders => _orders;

	public VisualOrder SoloOrder
	{
		get
		{
			if (!IsSoloOrder)
			{
				return null;
			}
			return _orders[0];
		}
	}

	public abstract bool IsSoloOrder { get; }

	public abstract string StringId { get; }

	public abstract string IconId { get; }

	public abstract TextObject GetName(OrderController orderController);

	public VisualOrderSet()
	{
		_orders = new MBList<VisualOrder>();
	}

	public void AddOrder(VisualOrder order)
	{
		if (IsSoloOrder)
		{
			Debug.FailedAssert("Can't add additional orders to solo orders", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Order\\Visual\\VisualOrderSet.cs", "AddOrder", 32);
		}
		else if (_orders.Contains(order))
		{
			Debug.FailedAssert("Order:" + order.StringId + " is already in collection: " + StringId, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Order\\Visual\\VisualOrderSet.cs", "AddOrder", 38);
		}
		else
		{
			_orders.Add(order);
		}
	}

	public void RemoveOrder(VisualOrder order)
	{
		if (IsSoloOrder)
		{
			Debug.FailedAssert("Can't remove orders from solo orders", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Order\\Visual\\VisualOrderSet.cs", "RemoveOrder", 49);
		}
		else if (!_orders.Contains(order))
		{
			Debug.FailedAssert("Order:" + order.StringId + " is not in collection: " + StringId, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Order\\Visual\\VisualOrderSet.cs", "RemoveOrder", 55);
		}
		else
		{
			_orders.Remove(order);
		}
	}

	public void ClearOrders()
	{
		if (IsSoloOrder)
		{
			Debug.FailedAssert("Can't remove orders from solo orders", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Order\\Visual\\VisualOrderSet.cs", "ClearOrders", 66);
		}
		else
		{
			_orders.Clear();
		}
	}
}
