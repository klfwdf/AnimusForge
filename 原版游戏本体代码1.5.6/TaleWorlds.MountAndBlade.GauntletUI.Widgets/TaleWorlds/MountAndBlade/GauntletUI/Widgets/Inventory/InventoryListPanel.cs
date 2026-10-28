using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

public class InventoryListPanel : NavigatableListPanel
{
	private Action<Widget> _sortByTypeClickHandler;

	private Action<Widget> _sortByNameClickHandler;

	private Action<Widget> _sortByQuantityClickHandler;

	private Action<Widget> _sortByCostClickHandler;

	private ButtonWidget _sortByTypeBtn;

	private ButtonWidget _sortByNameBtn;

	private ButtonWidget _sortByQuantityBtn;

	private ButtonWidget _sortByCostBtn;

	[Editor(false)]
	public ButtonWidget SortByTypeBtn
	{
		get
		{
			return _sortByTypeBtn;
		}
		set
		{
			if (_sortByTypeBtn != value)
			{
				if (_sortByTypeBtn != null)
				{
					_sortByTypeBtn.ClickEventHandlers.Remove(_sortByTypeClickHandler);
				}
				_sortByTypeBtn = value;
				if (_sortByTypeBtn != null)
				{
					_sortByTypeBtn.ClickEventHandlers.Add(_sortByTypeClickHandler);
				}
				OnPropertyChanged(value, "SortByTypeBtn");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget SortByNameBtn
	{
		get
		{
			return _sortByNameBtn;
		}
		set
		{
			if (_sortByNameBtn != value)
			{
				if (_sortByNameBtn != null)
				{
					_sortByNameBtn.ClickEventHandlers.Remove(_sortByNameClickHandler);
				}
				_sortByNameBtn = value;
				if (_sortByNameBtn != null)
				{
					_sortByNameBtn.ClickEventHandlers.Add(_sortByNameClickHandler);
				}
				OnPropertyChanged(value, "SortByNameBtn");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget SortByQuantityBtn
	{
		get
		{
			return _sortByQuantityBtn;
		}
		set
		{
			if (_sortByQuantityBtn != value)
			{
				if (_sortByQuantityBtn != null)
				{
					_sortByQuantityBtn.ClickEventHandlers.Remove(_sortByQuantityClickHandler);
				}
				_sortByQuantityBtn = value;
				if (_sortByQuantityBtn != null)
				{
					_sortByQuantityBtn.ClickEventHandlers.Add(_sortByQuantityClickHandler);
				}
				OnPropertyChanged(value, "SortByQuantityBtn");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget SortByCostBtn
	{
		get
		{
			return _sortByCostBtn;
		}
		set
		{
			if (_sortByCostBtn != value)
			{
				if (_sortByCostBtn != null)
				{
					_sortByCostBtn.ClickEventHandlers.Remove(_sortByCostClickHandler);
				}
				_sortByCostBtn = value;
				if (_sortByCostBtn != null)
				{
					_sortByCostBtn.ClickEventHandlers.Remove(_sortByCostClickHandler);
				}
				OnPropertyChanged(value, "SortByCostBtn");
			}
		}
	}

	public InventoryListPanel(UIContext context)
		: base(context)
	{
		_sortByTypeClickHandler = OnSortByType;
		_sortByNameClickHandler = OnSortByName;
		_sortByQuantityClickHandler = OnSortByQuantity;
		_sortByCostClickHandler = OnSortByCost;
		base.ClearSelectedOnRemoval = true;
	}

	private void OnSortByType(Widget widget)
	{
		RefreshChildNavigationIndices();
	}

	private void OnSortByName(Widget widget)
	{
		RefreshChildNavigationIndices();
	}

	private void OnSortByQuantity(Widget widget)
	{
		RefreshChildNavigationIndices();
	}

	private void OnSortByCost(Widget widget)
	{
		RefreshChildNavigationIndices();
	}
}
