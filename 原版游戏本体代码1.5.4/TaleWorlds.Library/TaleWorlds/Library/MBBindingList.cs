using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TaleWorlds.Library;

public class MBBindingList<T> : Collection<T>, IMBBindingList, IList, ICollection, IEnumerable
{
	private readonly List<T> _list;

	private List<ListChangedEventHandler> _eventHandlers;

	public event ListChangedEventHandler ListChanged
	{
		add
		{
			if (_eventHandlers == null)
			{
				_eventHandlers = new List<ListChangedEventHandler>();
			}
			_eventHandlers.Add(value);
		}
		remove
		{
			if (_eventHandlers != null)
			{
				_eventHandlers.Remove(value);
			}
		}
	}

	public MBBindingList()
		: base((IList<T>)new List<T>(64))
	{
		_list = (List<T>)base.Items;
	}

	protected override void ClearItems()
	{
		base.ClearItems();
		FireListChanged(ListChangedType.Reset, -1);
	}

	protected override void InsertItem(int index, T item)
	{
		base.InsertItem(index, item);
		FireListChanged(ListChangedType.ItemAdded, index);
	}

	protected override void RemoveItem(int index)
	{
		FireListChanged(ListChangedType.ItemBeforeDeleted, index);
		base.RemoveItem(index);
		FireListChanged(ListChangedType.ItemDeleted, index);
	}

	protected override void SetItem(int index, T item)
	{
		base.SetItem(index, item);
		FireListChanged(ListChangedType.ItemChanged, index);
	}

	private void FireListChanged(ListChangedType type, int index)
	{
		OnListChanged(new ListChangedEventArgs(type, index));
	}

	protected virtual void OnListChanged(ListChangedEventArgs e)
	{
		if (_eventHandlers == null)
		{
			return;
		}
		foreach (ListChangedEventHandler eventHandler in _eventHandlers)
		{
			eventHandler(this, e);
		}
	}

	public void Sort()
	{
		_list.Sort();
		FireListChanged(ListChangedType.Sorted, -1);
	}

	public void Sort(IComparer<T> comparer)
	{
		if (!IsOrdered(comparer))
		{
			_list.Sort(comparer);
			FireListChanged(ListChangedType.Sorted, -1);
		}
	}

	public bool IsOrdered(IComparer<T> comparer)
	{
		for (int i = 1; i < _list.Count; i++)
		{
			if (comparer.Compare(_list[i - 1], _list[i]) == 1)
			{
				return false;
			}
		}
		return true;
	}

	public void ApplyActionOnAllItems(Action<T> action)
	{
		for (int i = 0; i < _list.Count; i++)
		{
			T obj = _list[i];
			action(obj);
		}
	}
}
