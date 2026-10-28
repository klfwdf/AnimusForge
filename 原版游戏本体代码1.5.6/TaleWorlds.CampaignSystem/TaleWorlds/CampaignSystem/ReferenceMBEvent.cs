namespace TaleWorlds.CampaignSystem;

public class ReferenceMBEvent<T1> : ReferenceIMBEvent<T1>, IMbEventBase
{
	internal class EventHandlerRec<TS>
	{
		public EventHandlerRec<TS> Next;

		internal ReferenceAction<TS> Action { get; private set; }

		internal object Owner { get; private set; }

		public EventHandlerRec(object owner, ReferenceAction<TS> action)
		{
			Action = action;
			Owner = owner;
		}
	}

	private EventHandlerRec<T1> _nonSerializedListenerList;

	public void AddNonSerializedListener(object owner, ReferenceAction<T1> action)
	{
		EventHandlerRec<T1> eventHandlerRec = new EventHandlerRec<T1>(owner, action);
		EventHandlerRec<T1> nonSerializedListenerList = _nonSerializedListenerList;
		_nonSerializedListenerList = eventHandlerRec;
		eventHandlerRec.Next = nonSerializedListenerList;
	}

	public void Invoke(ref T1 t1)
	{
		InvokeList(_nonSerializedListenerList, ref t1);
	}

	private void InvokeList(EventHandlerRec<T1> list, ref T1 t1)
	{
		while (list != null)
		{
			list.Action(ref t1);
			list = list.Next;
		}
	}

	public void ClearListeners(object o)
	{
		ClearListenerOfList(ref _nonSerializedListenerList, o);
	}

	private void ClearListenerOfList(ref EventHandlerRec<T1> list, object o)
	{
		EventHandlerRec<T1> eventHandlerRec = list;
		while (eventHandlerRec != null && eventHandlerRec.Owner != o)
		{
			eventHandlerRec = eventHandlerRec.Next;
		}
		if (eventHandlerRec == null)
		{
			return;
		}
		EventHandlerRec<T1> eventHandlerRec2 = list;
		if (eventHandlerRec2 == eventHandlerRec)
		{
			list = eventHandlerRec2.Next;
			return;
		}
		while (eventHandlerRec2 != null)
		{
			if (eventHandlerRec2.Next == eventHandlerRec)
			{
				eventHandlerRec2.Next = eventHandlerRec.Next;
			}
			else
			{
				eventHandlerRec2 = eventHandlerRec2.Next;
			}
		}
	}
}
public class ReferenceMBEvent<T1, T2> : ReferenceIMBEvent<T1, T2>, IMbEventBase
{
	internal class EventHandlerRec<TS, TQ>
	{
		public EventHandlerRec<TS, TQ> Next;

		internal ReferenceAction<TS, TQ> Action { get; private set; }

		internal object Owner { get; private set; }

		public EventHandlerRec(object owner, ReferenceAction<TS, TQ> action)
		{
			Action = action;
			Owner = owner;
		}
	}

	private EventHandlerRec<T1, T2> _nonSerializedListenerList;

	public void AddNonSerializedListener(object owner, ReferenceAction<T1, T2> action)
	{
		EventHandlerRec<T1, T2> eventHandlerRec = new EventHandlerRec<T1, T2>(owner, action);
		EventHandlerRec<T1, T2> nonSerializedListenerList = _nonSerializedListenerList;
		_nonSerializedListenerList = eventHandlerRec;
		eventHandlerRec.Next = nonSerializedListenerList;
	}

	public void Invoke(T1 t1, ref T2 t2)
	{
		InvokeList(_nonSerializedListenerList, t1, ref t2);
	}

	private void InvokeList(EventHandlerRec<T1, T2> list, T1 t1, ref T2 t2)
	{
		while (list != null)
		{
			list.Action(t1, ref t2);
			list = list.Next;
		}
	}

	public void ClearListeners(object o)
	{
		ClearListenerOfList(ref _nonSerializedListenerList, o);
	}

	private void ClearListenerOfList(ref EventHandlerRec<T1, T2> list, object o)
	{
		EventHandlerRec<T1, T2> eventHandlerRec = list;
		while (eventHandlerRec != null && eventHandlerRec.Owner != o)
		{
			eventHandlerRec = eventHandlerRec.Next;
		}
		if (eventHandlerRec == null)
		{
			return;
		}
		EventHandlerRec<T1, T2> eventHandlerRec2 = list;
		if (eventHandlerRec2 == eventHandlerRec)
		{
			list = eventHandlerRec2.Next;
			return;
		}
		while (eventHandlerRec2 != null)
		{
			if (eventHandlerRec2.Next == eventHandlerRec)
			{
				eventHandlerRec2.Next = eventHandlerRec.Next;
			}
			else
			{
				eventHandlerRec2 = eventHandlerRec2.Next;
			}
		}
	}
}
public class ReferenceMBEvent<T1, T2, T3> : ReferenceIMBEvent<T1, T2, T3>, IMbEventBase
{
	internal class EventHandlerRec<TS, TQ, TR>
	{
		public EventHandlerRec<TS, TQ, TR> Next;

		internal ReferenceAction<TS, TQ, TR> Action { get; private set; }

		internal object Owner { get; private set; }

		public EventHandlerRec(object owner, ReferenceAction<TS, TQ, TR> action)
		{
			Action = action;
			Owner = owner;
		}
	}

	private EventHandlerRec<T1, T2, T3> _nonSerializedListenerList;

	public void AddNonSerializedListener(object owner, ReferenceAction<T1, T2, T3> action)
	{
		EventHandlerRec<T1, T2, T3> eventHandlerRec = new EventHandlerRec<T1, T2, T3>(owner, action);
		EventHandlerRec<T1, T2, T3> nonSerializedListenerList = _nonSerializedListenerList;
		_nonSerializedListenerList = eventHandlerRec;
		eventHandlerRec.Next = nonSerializedListenerList;
	}

	public void Invoke(T1 t1, T2 t2, ref T3 t3)
	{
		InvokeList(_nonSerializedListenerList, t1, t2, ref t3);
	}

	private void InvokeList(EventHandlerRec<T1, T2, T3> list, T1 t1, T2 t2, ref T3 t3)
	{
		while (list != null)
		{
			list.Action(t1, t2, ref t3);
			list = list.Next;
		}
	}

	public void ClearListeners(object o)
	{
		ClearListenerOfList(ref _nonSerializedListenerList, o);
	}

	private void ClearListenerOfList(ref EventHandlerRec<T1, T2, T3> list, object o)
	{
		EventHandlerRec<T1, T2, T3> eventHandlerRec = list;
		while (eventHandlerRec != null && eventHandlerRec.Owner != o)
		{
			eventHandlerRec = eventHandlerRec.Next;
		}
		if (eventHandlerRec == null)
		{
			return;
		}
		EventHandlerRec<T1, T2, T3> eventHandlerRec2 = list;
		if (eventHandlerRec2 == eventHandlerRec)
		{
			list = eventHandlerRec2.Next;
			return;
		}
		while (eventHandlerRec2 != null)
		{
			if (eventHandlerRec2.Next == eventHandlerRec)
			{
				eventHandlerRec2.Next = eventHandlerRec.Next;
			}
			else
			{
				eventHandlerRec2 = eventHandlerRec2.Next;
			}
		}
	}
}
