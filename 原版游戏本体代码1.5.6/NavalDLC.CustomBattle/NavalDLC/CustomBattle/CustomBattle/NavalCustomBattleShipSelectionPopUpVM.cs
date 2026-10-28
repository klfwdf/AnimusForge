using System;
using NavalDLC.CustomBattle.CustomBattle.SelectionItem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace NavalDLC.CustomBattle.CustomBattle;

public class NavalCustomBattleShipSelectionPopUpVM : ViewModel
{
	private Action<ShipHull> _onConfirm;

	private InputKeyItemVM _closeInputKey;

	private MBBindingList<NavalCustomBattleShipHullItemVM> _items;

	private string _title;

	private string _doneLbl;

	private string _cancelLbl;

	private bool _isOpen;

	[DataSourceProperty]
	public InputKeyItemVM CloseInputKey
	{
		get
		{
			return _closeInputKey;
		}
		set
		{
			if (value != _closeInputKey)
			{
				_closeInputKey = value;
				OnPropertyChangedWithValue(value, "CloseInputKey");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<NavalCustomBattleShipHullItemVM> Items
	{
		get
		{
			return _items;
		}
		set
		{
			if (value != _items)
			{
				_items = value;
				OnPropertyChangedWithValue(value, "Items");
			}
		}
	}

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	[DataSourceProperty]
	public string DoneLbl
	{
		get
		{
			return _doneLbl;
		}
		set
		{
			if (value != _doneLbl)
			{
				_doneLbl = value;
				OnPropertyChangedWithValue(value, "DoneLbl");
			}
		}
	}

	[DataSourceProperty]
	public string CancelLbl
	{
		get
		{
			return _cancelLbl;
		}
		set
		{
			if (value != _cancelLbl)
			{
				_cancelLbl = value;
				OnPropertyChangedWithValue(value, "CancelLbl");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOpen
	{
		get
		{
			return _isOpen;
		}
		set
		{
			if (value != _isOpen)
			{
				_isOpen = value;
				OnPropertyChangedWithValue(value, "IsOpen");
			}
		}
	}

	public NavalCustomBattleShipSelectionPopUpVM()
	{
		Items = new MBBindingList<NavalCustomBattleShipHullItemVM>
		{
			new NavalCustomBattleShipHullItemVM(new TextObject("{=koX9okuG}None"), new TextObject("{=fNyb979i}Must have at least one ship"), OnShipHullSelected)
		};
		foreach (ShipHull shipHull in NavalCustomBattleData.ShipHulls)
		{
			Items.Add(new NavalCustomBattleShipHullItemVM(shipHull, new TextObject("{=d3WMrFKo}Not usable in selected game mode"), OnShipHullSelected));
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CloseInputKey.OnFinalize();
	}

	public void OpenPopUp(string title, ShipHull selectedItem, bool canSelectEmpty, Func<ShipHull, bool> getIsHullDisabled, Action<ShipHull> onConfirm)
	{
		Title = title;
		IsOpen = true;
		_onConfirm = onConfirm;
		Items.ApplyActionOnAllItems(delegate(NavalCustomBattleShipHullItemVM item)
		{
			item.IsSelected = item.ShipHull == selectedItem;
			item.IsDisabled = (item.ShipHull == null && !canSelectEmpty) || (item.ShipHull != null && (getIsHullDisabled?.Invoke(item.ShipHull) ?? false));
		});
	}

	public void ExecuteClose()
	{
		IsOpen = false;
		_onConfirm = null;
	}

	private void OnShipHullSelected(NavalCustomBattleShipHullItemVM item)
	{
		_onConfirm?.Invoke(item?.ShipHull);
		IsOpen = false;
		_onConfirm = null;
	}

	public void SetCloseInputKey(HotKey hotkey)
	{
		CloseInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
