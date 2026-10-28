using System;
using System.Collections.Generic;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace MCM.UI.Dropdown;

internal class MCMSelectorVM<TSelectorItemVM> : ViewModel where TSelectorItemVM : ViewModel
{
	private delegate bool CanBeSelectedDelegate(TSelectorItemVM instance);

	private delegate void SetIsSelectedDelegate(object instance, bool value);

	private static readonly CanBeSelectedDelegate _canBeSelectedDelegate = HarmonyLib.BUTR.Extensions.AccessTools2.GetPropertyGetterDelegate<CanBeSelectedDelegate>(typeof(TSelectorItemVM), "CanBeSelected") ?? ((CanBeSelectedDelegate)((TSelectorItemVM _) => false));

	private static readonly SetIsSelectedDelegate _setIsSelectedDelegate = HarmonyLib.BUTR.Extensions.AccessTools2.GetPropertySetterDelegate<SetIsSelectedDelegate>(typeof(TSelectorItemVM), "IsSelected") ?? ((SetIsSelectedDelegate)delegate
	{
	});

	protected int _selectedIndex = -1;

	private TSelectorItemVM? _selectedItem;

	private bool _hasSingleItem;

	public static MCMSelectorVM<TSelectorItemVM> Empty => new MCMSelectorVM<TSelectorItemVM>();

	[DataSourceProperty]
	public MBBindingList<TSelectorItemVM> ItemList { get; } = new MBBindingList<TSelectorItemVM>();

	[DataSourceProperty]
	public int SelectedIndex
	{
		get
		{
			return _selectedIndex;
		}
		set
		{
			if (SetField(ref _selectedIndex, value, "SelectedIndex"))
			{
				if (SelectedItem != null)
				{
					_setIsSelectedDelegate(SelectedItem, value: false);
				}
				SelectedItem = GetCurrentItem();
				if (SelectedItem != null)
				{
					_setIsSelectedDelegate(SelectedItem, value: true);
				}
			}
		}
	}

	[DataSourceProperty]
	public TSelectorItemVM? SelectedItem
	{
		get
		{
			return _selectedItem;
		}
		set
		{
			SetField(ref _selectedItem, value, "SelectedItem");
		}
	}

	[DataSourceProperty]
	public bool HasSingleItem
	{
		get
		{
			return _hasSingleItem;
		}
		set
		{
			SetField(ref _hasSingleItem, value, "HasSingleItem");
		}
	}

	public MCMSelectorVM()
	{
		HasSingleItem = true;
	}

	public MCMSelectorVM(IEnumerable<object> list, int selectedIndex)
	{
		Refresh(list, selectedIndex);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ItemList.ApplyActionOnAllItems(delegate(TSelectorItemVM x)
		{
			x.RefreshValues();
		});
	}

	public void Refresh(IEnumerable<object> list, int selectedIndex)
	{
		ItemList.Clear();
		_selectedIndex = -1;
		foreach (object item2 in list)
		{
			if (Activator.CreateInstance(typeof(TSelectorItemVM), item2) is TSelectorItemVM item)
			{
				ItemList.Add(item);
			}
		}
		HasSingleItem = ItemList.Count <= 1;
		SelectedIndex = selectedIndex;
	}

	public TSelectorItemVM? GetCurrentItem()
	{
		if (ItemList.Count > 0 && SelectedIndex >= 0 && SelectedIndex < ItemList.Count)
		{
			return ItemList[SelectedIndex];
		}
		return null;
	}

	public void AddItem(TSelectorItemVM item)
	{
		ItemList.Add(item);
		HasSingleItem = ItemList.Count <= 1;
	}

	public void ExecuteRandomize()
	{
		TSelectorItemVM randomElementWithPredicate = ItemList.GetRandomElementWithPredicate((TSelectorItemVM i) => _canBeSelectedDelegate(i));
		if (randomElementWithPredicate != null)
		{
			SelectedIndex = ItemList.IndexOf(randomElementWithPredicate);
		}
	}

	public void ExecuteSelectNextItem()
	{
		if (ItemList.Count <= 0)
		{
			return;
		}
		for (int num = (SelectedIndex + 1) % ItemList.Count; num != SelectedIndex; num = (num + 1) % ItemList.Count)
		{
			if (_canBeSelectedDelegate(ItemList[num]))
			{
				SelectedIndex = num;
				break;
			}
		}
	}

	public void ExecuteSelectPreviousItem()
	{
		if (ItemList.Count <= 0)
		{
			return;
		}
		for (int num = ((SelectedIndex - 1 >= 0) ? (SelectedIndex - 1) : (ItemList.Count - 1)); num != SelectedIndex; num = ((num - 1 >= 0) ? (num - 1) : (ItemList.Count - 1)))
		{
			if (_canBeSelectedDelegate(ItemList[num]))
			{
				SelectedIndex = num;
				break;
			}
		}
	}
}
internal class MCMSelectorVM<TSelectorItemVM, TSelectorItemVMValueType> : MCMSelectorVM<TSelectorItemVM> where TSelectorItemVM : MCMSelectorItemVM<TSelectorItemVMValueType> where TSelectorItemVMValueType : class
{
	public MCMSelectorVM(IEnumerable<TSelectorItemVMValueType> list, int selectedIndex)
	{
		Refresh(list, selectedIndex);
	}

	public void Refresh(IEnumerable<TSelectorItemVMValueType> list, int selectedIndex)
	{
		base.ItemList.Clear();
		_selectedIndex = -1;
		foreach (TSelectorItemVMValueType item2 in list)
		{
			if (Activator.CreateInstance(typeof(TSelectorItemVM), item2) is TSelectorItemVM item)
			{
				base.ItemList.Add(item);
			}
		}
		base.HasSingleItem = base.ItemList.Count <= 1;
		base.SelectedIndex = selectedIndex;
	}
}
