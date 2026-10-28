using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.Core.ViewModelCollection.Selector;

public class SelectorVM<T> : ViewModel where T : SelectorItemVM
{
	private Action<SelectorVM<T>> _onChange;

	private MBBindingList<T> _itemList;

	private int _selectedIndex = -1;

	private T _selectedItem;

	private bool _hasSingleItem;

	[DataSourceProperty]
	public MBBindingList<T> ItemList
	{
		get
		{
			return _itemList;
		}
		set
		{
			if (value != _itemList)
			{
				_itemList = value;
				OnPropertyChangedWithValue(value, "ItemList");
			}
		}
	}

	[DataSourceProperty]
	public int SelectedIndex
	{
		get
		{
			return _selectedIndex;
		}
		set
		{
			if (value != _selectedIndex)
			{
				_selectedIndex = value;
				OnPropertyChangedWithValue(value, "SelectedIndex");
				if (SelectedItem != null)
				{
					SelectedItem.IsSelected = false;
				}
				SelectedItem = GetCurrentItem();
				if (SelectedItem != null)
				{
					SelectedItem.IsSelected = true;
				}
				_onChange?.Invoke(this);
			}
		}
	}

	[DataSourceProperty]
	public T SelectedItem
	{
		get
		{
			return _selectedItem;
		}
		set
		{
			if (value != _selectedItem)
			{
				_selectedItem = value;
				OnPropertyChangedWithValue(value, "SelectedItem");
			}
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
			if (value != _hasSingleItem)
			{
				_hasSingleItem = value;
				OnPropertyChangedWithValue(value, "HasSingleItem");
			}
		}
	}

	public SelectorVM(int selectedIndex, Action<SelectorVM<T>> onChange)
	{
		ItemList = new MBBindingList<T>();
		HasSingleItem = true;
		_onChange = onChange;
	}

	public SelectorVM(IEnumerable<string> list, int selectedIndex, Action<SelectorVM<T>> onChange)
	{
		ItemList = new MBBindingList<T>();
		Refresh(list, selectedIndex, onChange);
	}

	public SelectorVM(IEnumerable<TextObject> list, int selectedIndex, Action<SelectorVM<T>> onChange)
	{
		ItemList = new MBBindingList<T>();
		Refresh(list, selectedIndex, onChange);
	}

	public void Refresh(IEnumerable<string> list, int selectedIndex, Action<SelectorVM<T>> onChange)
	{
		ItemList.Clear();
		_selectedIndex = -1;
		foreach (string item2 in list)
		{
			T item = (T)Activator.CreateInstance(typeof(T), item2);
			ItemList.Add(item);
		}
		HasSingleItem = ItemList.Count <= 1;
		_onChange = onChange;
		SelectedIndex = selectedIndex;
	}

	public void Refresh(IEnumerable<TextObject> list, int selectedIndex, Action<SelectorVM<T>> onChange)
	{
		ItemList.Clear();
		_selectedIndex = -1;
		foreach (TextObject item2 in list)
		{
			T item = (T)Activator.CreateInstance(typeof(T), item2);
			ItemList.Add(item);
		}
		HasSingleItem = ItemList.Count <= 1;
		_onChange = onChange;
		SelectedIndex = selectedIndex;
	}

	public void Refresh(IEnumerable<T> list, int selectedIndex, Action<SelectorVM<T>> onChange)
	{
		ItemList.Clear();
		_selectedIndex = -1;
		foreach (T item in list)
		{
			ItemList.Add(item);
		}
		HasSingleItem = ItemList.Count <= 1;
		_onChange = onChange;
		SelectedIndex = selectedIndex;
	}

	public void SetOnChangeAction(Action<SelectorVM<T>> onChange)
	{
		_onChange = onChange;
	}

	public void AddItem(T item)
	{
		ItemList.Add(item);
		HasSingleItem = ItemList.Count <= 1;
	}

	public void ExecuteRandomize()
	{
		MBBindingList<T> itemList = ItemList;
		T val = ((itemList != null) ? itemList.GetRandomElementWithPredicate((T i) => i.CanBeSelected) : null);
		if (val != null)
		{
			SelectedIndex = ItemList.IndexOf(val);
		}
	}

	public void ExecuteSelectNextItem()
	{
		MBBindingList<T> itemList = ItemList;
		if (itemList == null || itemList.Count <= 0)
		{
			return;
		}
		for (int num = (SelectedIndex + 1) % ItemList.Count; num != SelectedIndex; num = (num + 1) % ItemList.Count)
		{
			if (ItemList[num].CanBeSelected)
			{
				SelectedIndex = num;
				break;
			}
		}
	}

	public void ExecuteSelectPreviousItem()
	{
		MBBindingList<T> itemList = ItemList;
		if (itemList == null || itemList.Count <= 0)
		{
			return;
		}
		for (int num = ((SelectedIndex - 1 >= 0) ? (SelectedIndex - 1) : (ItemList.Count - 1)); num != SelectedIndex; num = ((num - 1 >= 0) ? (num - 1) : (ItemList.Count - 1)))
		{
			if (ItemList[num].CanBeSelected)
			{
				SelectedIndex = num;
				break;
			}
		}
	}

	public T GetCurrentItem()
	{
		MBBindingList<T> itemList = _itemList;
		if (itemList != null && itemList.Count > 0 && SelectedIndex >= 0 && SelectedIndex < _itemList.Count)
		{
			return _itemList[SelectedIndex];
		}
		return null;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		_itemList.ApplyActionOnAllItems(delegate(T x)
		{
			x.RefreshValues();
		});
	}
}
