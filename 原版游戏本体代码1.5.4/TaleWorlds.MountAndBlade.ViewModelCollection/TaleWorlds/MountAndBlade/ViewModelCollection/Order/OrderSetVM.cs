using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order;

public class OrderSetVM : OrderItemBaseVM
{
	public delegate void OnOrderSetSelectionStateChangedDelegate(OrderSetVM orderSet, bool isSelected);

	private string _selectedOrderText;

	private OrderItemVM _soloOrder;

	private MBBindingList<OrderItemVM> _orders;

	public bool HasSingleOrder => SoloOrder != null;

	public VisualOrderSet OrderSet { get; }

	[DataSourceProperty]
	public string SelectedOrderText
	{
		get
		{
			return _selectedOrderText;
		}
		set
		{
			if (value != _selectedOrderText)
			{
				_selectedOrderText = value;
				OnPropertyChangedWithValue(value, "SelectedOrderText");
			}
		}
	}

	[DataSourceProperty]
	public OrderItemVM SoloOrder
	{
		get
		{
			return _soloOrder;
		}
		set
		{
			if (value != _soloOrder)
			{
				_soloOrder = value;
				OnPropertyChangedWithValue(value, "SoloOrder");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<OrderItemVM> Orders
	{
		get
		{
			return _orders;
		}
		set
		{
			if (value != _orders)
			{
				_orders = value;
				OnPropertyChangedWithValue(value, "Orders");
			}
		}
	}

	public static event OnOrderSetSelectionStateChangedDelegate OnSelectionStateChanged;

	public OrderSetVM(OrderController orderController, VisualOrderSet collection)
		: base(orderController)
	{
		OrderSet = collection;
		Orders = new MBBindingList<OrderItemVM>();
		RefreshOrders();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.Name = OrderSet.GetName(_orderController).ToString();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		if (SoloOrder != null)
		{
			SoloOrder = null;
		}
		for (int i = 0; i < Orders.Count; i++)
		{
			Orders[i].OnFinalize();
		}
		base.ShortcutKey?.OnFinalize();
	}

	protected override void OnExecuteAction(VisualOrderExecutionParameters executionParameters)
	{
		if (OrderSet.IsSoloOrder)
		{
			SoloOrder.ExecuteAction(executionParameters);
		}
		else
		{
			OrderSetVM.OnSelectionStateChanged?.Invoke(this, isSelected: true);
		}
		RefreshOrderStates();
	}

	protected override void OnRefreshState()
	{
		base.Name = OrderSet.GetName(_orderController).ToString();
		if (OrderSet.IsSoloOrder)
		{
			OrderState activeState = OrderSet.SoloOrder.GetActiveState(_orderController);
			base.SelectionState = activeState.ToString();
			base.IsActive = activeState == OrderState.Active;
		}
		else
		{
			base.IsActive = false;
			base.SelectionState = OrderState.Default.ToString();
		}
	}

	public void ExecuteSelect()
	{
		OrderSetVM.OnSelectionStateChanged?.Invoke(this, isSelected: true);
	}

	public void ExecuteDeSelect()
	{
		OrderSetVM.OnSelectionStateChanged?.Invoke(this, isSelected: false);
	}

	public void OnOrderExecuted(OrderItemVM order)
	{
		RefreshOrderStates();
		RefreshValues();
	}

	public void RefreshOrders()
	{
		Orders.Clear();
		if (SoloOrder != null)
		{
			SoloOrder = null;
		}
		if (OrderSet != null)
		{
			MBReadOnlyList<VisualOrder> orders = OrderSet.Orders;
			for (int i = 0; i < orders.Count; i++)
			{
				Orders.Add(new OrderItemVM(_orderController, orders[i]));
			}
			if (OrderSet.IsSoloOrder)
			{
				SoloOrder = Orders[0];
			}
		}
	}

	protected override void OnSelectedStateChanged(bool isSelected)
	{
		base.OnSelectedStateChanged(isSelected);
		OrderSetVM.OnSelectionStateChanged?.Invoke(this, isSelected);
		if (SoloOrder != null)
		{
			SoloOrder.IsSelected = isSelected;
		}
	}

	public void RefreshOrderStates()
	{
		base.OrderIconId = OrderSet.IconId;
		RefreshState();
		for (int i = 0; i < Orders.Count; i++)
		{
			Orders[i].RefreshState();
		}
	}

	public void UpdateCanUseShortcuts(bool value)
	{
		base.CanUseShortcuts = value;
		for (int i = 0; i < Orders.Count; i++)
		{
			Orders[i].CanUseShortcuts = value;
		}
	}
}
