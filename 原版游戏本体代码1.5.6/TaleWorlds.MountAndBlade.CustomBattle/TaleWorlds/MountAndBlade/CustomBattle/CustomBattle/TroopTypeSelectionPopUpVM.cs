using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;

public class TroopTypeSelectionPopUpVM : ViewModel
{
	public Action OnPopUpClosed;

	private List<bool> _itemSelectionsBackUp;

	private int _selectedItemCount;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _resetInputKey;

	private MBBindingList<CustomBattleTroopTypeVM> _items;

	private string _title;

	private string _doneLbl;

	private string _cancelLbl;

	private string _selectAllLbl;

	private string _backToDefaultLbl;

	private bool _isOpen;

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM ResetInputKey
	{
		get
		{
			return _resetInputKey;
		}
		set
		{
			if (value != _resetInputKey)
			{
				_resetInputKey = value;
				OnPropertyChangedWithValue(value, "ResetInputKey");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CustomBattleTroopTypeVM> Items
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
	public string SelectAllLbl
	{
		get
		{
			return _selectAllLbl;
		}
		set
		{
			if (value != _selectAllLbl)
			{
				_selectAllLbl = value;
				OnPropertyChangedWithValue(value, "SelectAllLbl");
			}
		}
	}

	[DataSourceProperty]
	public string BackToDefaultLbl
	{
		get
		{
			return _backToDefaultLbl;
		}
		set
		{
			if (value != _backToDefaultLbl)
			{
				_backToDefaultLbl = value;
				OnPropertyChangedWithValue(value, "BackToDefaultLbl");
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

	public override void RefreshValues()
	{
		base.RefreshValues();
		DoneLbl = GameTexts.FindText("str_done").ToString();
		CancelLbl = GameTexts.FindText("str_cancel").ToString();
		SelectAllLbl = GameTexts.FindText("str_custom_battle_select_all").ToString();
		BackToDefaultLbl = GameTexts.FindText("str_custom_battle_back_to_default").ToString();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey.OnFinalize();
		CancelInputKey.OnFinalize();
		ResetInputKey.OnFinalize();
	}

	public void OpenPopUp(string title, MBBindingList<CustomBattleTroopTypeVM> troops)
	{
		_itemSelectionsBackUp = new List<bool>();
		foreach (CustomBattleTroopTypeVM troop in troops)
		{
			_itemSelectionsBackUp.Add(troop.IsSelected);
		}
		_selectedItemCount = troops.Count((CustomBattleTroopTypeVM x) => x.IsSelected);
		Title = title;
		Items = troops;
		IsOpen = true;
	}

	public void OnItemSelectionToggled(CustomBattleTroopTypeVM item)
	{
		if (_selectedItemCount > 1 || !item.IsSelected)
		{
			item.IsSelected = !item.IsSelected;
			_selectedItemCount += (item.IsSelected ? 1 : (-1));
		}
	}

	public void ExecuteSelectAll()
	{
		Items.ApplyActionOnAllItems(delegate(CustomBattleTroopTypeVM x)
		{
			x.IsSelected = true;
		});
		_selectedItemCount = Items.Count;
	}

	public void ExecuteBackToDefault()
	{
		Items.ApplyActionOnAllItems(delegate(CustomBattleTroopTypeVM x)
		{
			x.IsSelected = x.IsDefault;
		});
		_selectedItemCount = Items.Count((CustomBattleTroopTypeVM x) => x.IsSelected);
	}

	public void ExecuteCancel()
	{
		ExecuteReset();
		OnPopUpClosed?.Invoke();
		IsOpen = false;
	}

	public void ExecuteDone()
	{
		IsOpen = false;
	}

	public void ExecuteReset()
	{
		int count = _itemSelectionsBackUp.Count;
		if (count != Items.Count)
		{
			Debug.FailedAssert("Backup troop count does not match with the actual troop count.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.CustomBattle\\CustomBattle\\TroopTypeSelectionPopUpVM.cs", "ExecuteReset", 100);
			return;
		}
		for (int i = 0; i < count; i++)
		{
			Items[i].IsSelected = _itemSelectionsBackUp[i];
		}
		_selectedItemCount = Items.Count((CustomBattleTroopTypeVM x) => x.IsSelected);
	}

	public void SetCancelInputKey(HotKey hotkey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetDoneInputKey(HotKey hotkey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetResetInputKey(HotKey hotkey)
	{
		ResetInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
