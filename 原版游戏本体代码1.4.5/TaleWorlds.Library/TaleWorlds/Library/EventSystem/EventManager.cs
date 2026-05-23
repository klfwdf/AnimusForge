using System;
using System.Collections.Generic;

namespace TaleWorlds.Library.EventSystem;

public class EventManager
{
	private readonly DictionaryByType _eventsByType;

	public EventManager()
	{
		_eventsByType = new DictionaryByType();
	}

	public void RegisterEvent<T>(Action<T> eventObjType)
	{
		if (typeof(T).IsSubclassOf(typeof(EventBase)))
		{
			_eventsByType.Add(eventObjType);
		}
		else
		{
			Debug.FailedAssert("Events have to derived from EventSystemBase", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\EventSystem\\EventManager.cs", "RegisterEvent", 31);
		}
	}

	public void UnregisterEvent<T>(Action<T> eventObjType)
	{
		if (typeof(T).IsSubclassOf(typeof(EventBase)))
		{
			_eventsByType.Remove(eventObjType);
		}
		else
		{
			Debug.FailedAssert("Events have to derived from EventSystemBase", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\EventSystem\\EventManager.cs", "UnregisterEvent", 48);
		}
	}

	public void TriggerEvent<T>(T eventObj)
	{
		_eventsByType.InvokeActions(eventObj);
	}

	public void Clear()
	{
		_eventsByType.Clear();
	}

	public IDictionary<Type, object> GetCloneOfEventDictionary()
	{
		return _eventsByType.GetClone();
	}
}
