using System;
using System.Collections.Generic;

namespace TaleWorlds.Library.EventSystem;

public class DictionaryByType
{
	private readonly IDictionary<Type, object> _eventsByType = new Dictionary<Type, object>();

	public void Add<T>(Action<T> value)
	{
		if (!_eventsByType.TryGetValue(typeof(T), out var value2))
		{
			value2 = new List<Action<T>>();
			_eventsByType[typeof(T)] = value2;
		}
		((List<Action<T>>)value2).Add(value);
	}

	public void Remove<T>(Action<T> value)
	{
		if (_eventsByType.TryGetValue(typeof(T), out var value2))
		{
			List<Action<T>> list = (List<Action<T>>)value2;
			list.Remove(value);
			_eventsByType[typeof(T)] = list;
		}
		else
		{
			Debug.FailedAssert("Event: " + typeof(T).Name + " were not registered in the first place", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\EventSystem\\EventManager.cs", "Remove", 106);
		}
	}

	public void InvokeActions<T>(T item)
	{
		if (!_eventsByType.TryGetValue(typeof(T), out var value))
		{
			return;
		}
		foreach (Action<T> item2 in (List<Action<T>>)value)
		{
			item2(item);
		}
	}

	public List<Action<T>> Get<T>()
	{
		return (List<Action<T>>)_eventsByType[typeof(T)];
	}

	public bool TryGet<T>(out List<Action<T>> value)
	{
		if (_eventsByType.TryGetValue(typeof(T), out var value2))
		{
			value = (List<Action<T>>)value2;
			return true;
		}
		value = null;
		return false;
	}

	public IDictionary<Type, object> GetClone()
	{
		return new Dictionary<Type, object>(_eventsByType);
	}

	public void Clear()
	{
		_eventsByType.Clear();
	}
}
