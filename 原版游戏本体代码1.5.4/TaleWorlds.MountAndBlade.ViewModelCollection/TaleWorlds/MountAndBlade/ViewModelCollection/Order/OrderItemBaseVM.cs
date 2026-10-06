using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order;

public abstract class OrderItemBaseVM : ViewModel
{
	protected OrderController _orderController;

	private InputKeyItemVM _shortcutKey;

	private bool _isActive;

	private bool _isSelected;

	private bool _canUseShortcuts;

	private string _orderIconId;

	private string _selectionState;

	private string _name;

	[DataSourceProperty]
	public InputKeyItemVM ShortcutKey
	{
		get
		{
			return _shortcutKey;
		}
		set
		{
			if (value != _shortcutKey)
			{
				_shortcutKey = value;
				OnPropertyChangedWithValue(value, "ShortcutKey");
			}
		}
	}

	[DataSourceProperty]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			_isActive = value;
			OnPropertyChangedWithValue(value, "IsActive");
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
			_isSelected = value;
			OnPropertyChangedWithValue(value, "IsSelected");
			OnSelectedStateChanged(value);
		}
	}

	[DataSourceProperty]
	public bool CanUseShortcuts
	{
		get
		{
			return _canUseShortcuts;
		}
		set
		{
			if (value != _canUseShortcuts)
			{
				_canUseShortcuts = value;
				OnPropertyChangedWithValue(value, "CanUseShortcuts");
			}
		}
	}

	[DataSourceProperty]
	public string OrderIconId
	{
		get
		{
			return _orderIconId;
		}
		set
		{
			if (value != _orderIconId)
			{
				_orderIconId = value;
				OnPropertyChangedWithValue(value, "OrderIconId");
			}
		}
	}

	[DataSourceProperty]
	public string SelectionState
	{
		get
		{
			return _selectionState;
		}
		set
		{
			if (value != _selectionState)
			{
				_selectionState = value;
				OnPropertyChangedWithValue(value, "SelectionState");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	public OrderItemBaseVM(OrderController orderController)
	{
		_orderController = orderController;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		ShortcutKey?.OnFinalize();
	}

	public void RefreshState()
	{
		OnRefreshState();
	}

	public void ExecuteAction(VisualOrderExecutionParameters executionParameters)
	{
		OnExecuteAction(executionParameters);
	}

	protected virtual void OnSelectedStateChanged(bool isSelected)
	{
	}

	protected abstract void OnRefreshState();

	protected abstract void OnExecuteAction(VisualOrderExecutionParameters executionParameters);

	public void SetShortcutKey(InputKeyItemVM inputKeyItem)
	{
		ShortcutKey = inputKeyItem;
		ShortcutKey?.RefreshValues();
	}
}
