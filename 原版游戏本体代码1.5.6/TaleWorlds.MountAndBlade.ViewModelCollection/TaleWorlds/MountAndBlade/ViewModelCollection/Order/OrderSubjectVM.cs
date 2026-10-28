using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order;

public abstract class OrderSubjectVM : ViewModel
{
	private int _behaviorType;

	private int _underAttackOfType;

	private bool _isSelectable;

	private bool _isSelected;

	private bool _isSelectionHighlightActive;

	private bool _showSelectionInputs;

	private string _selectionText;

	private InputKeyItemVM _applySelectionKey;

	private InputKeyItemVM _toggleSelectionKey;

	private MBBindingList<OrderItemVM> _activeOrders;

	[DataSourceProperty]
	public bool IsSelectable
	{
		get
		{
			return _isSelectable;
		}
		set
		{
			if (value != _isSelectable)
			{
				_isSelectable = value;
				OnPropertyChangedWithValue(value, "IsSelectable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if ((!value || IsSelectable) && value != _isSelected)
			{
				_isSelected = value;
				OnSelectionStateChanged(value);
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelectionHighlightActive
	{
		get
		{
			return _isSelectionHighlightActive;
		}
		set
		{
			if (value != _isSelectionHighlightActive)
			{
				_isSelectionHighlightActive = value;
				OnPropertyChangedWithValue(value, "IsSelectionHighlightActive");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowSelectionInputs
	{
		get
		{
			return _showSelectionInputs;
		}
		set
		{
			if (value != _showSelectionInputs)
			{
				_showSelectionInputs = value;
				OnPropertyChangedWithValue(value, "ShowSelectionInputs");
			}
		}
	}

	[DataSourceProperty]
	public int BehaviorType
	{
		get
		{
			return _behaviorType;
		}
		set
		{
			if (value != _behaviorType)
			{
				_behaviorType = value;
				OnPropertyChangedWithValue(value, "BehaviorType");
			}
		}
	}

	[DataSourceProperty]
	public int UnderAttackOfType
	{
		get
		{
			return _underAttackOfType;
		}
		set
		{
			if (value != _underAttackOfType)
			{
				_underAttackOfType = value;
				OnPropertyChangedWithValue(value, "UnderAttackOfType");
			}
		}
	}

	[DataSourceProperty]
	public string SelectionText
	{
		get
		{
			return _selectionText;
		}
		set
		{
			if (value != _selectionText)
			{
				_selectionText = value;
				OnPropertyChangedWithValue(value, "SelectionText");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM ApplySelectionKey
	{
		get
		{
			return _applySelectionKey;
		}
		set
		{
			if (value != _applySelectionKey)
			{
				_applySelectionKey = value;
				OnPropertyChangedWithValue(value, "ApplySelectionKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM ToggleSelectionKey
	{
		get
		{
			return _toggleSelectionKey;
		}
		set
		{
			if (value != _toggleSelectionKey)
			{
				_toggleSelectionKey = value;
				OnPropertyChangedWithValue(value, "ToggleSelectionKey");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<OrderItemVM> ActiveOrders
	{
		get
		{
			return _activeOrders;
		}
		set
		{
			if (value != _activeOrders)
			{
				_activeOrders = value;
				OnPropertyChangedWithValue(value, "ActiveOrders");
			}
		}
	}

	public OrderSubjectVM()
	{
		ActiveOrders = new MBBindingList<OrderItemVM>();
		RefreshValues();
	}

	public void AddActiveOrder(OrderItemVM order)
	{
		ActiveOrders.Add(order);
	}

	public void RemoveActiveOrder(OrderItemVM order)
	{
		ActiveOrders.Remove(order);
	}

	public void ClearActiveOrders()
	{
		ActiveOrders.Clear();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		SelectionText = new TextObject("{=xbk1WAt6}Select").ToString();
	}

	protected abstract void OnSelectionStateChanged(bool isSelected);
}
