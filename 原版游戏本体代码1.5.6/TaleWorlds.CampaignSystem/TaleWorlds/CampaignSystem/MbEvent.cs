using System;

namespace TaleWorlds.CampaignSystem;

public class MbEvent : IMbEvent
{
	internal class EventHandlerRec
	{
		public EventHandlerRec Next;

		internal Action Action { get; private set; }

		internal object Owner { get; private set; }

		public EventHandlerRec(object owner, Action action)
		{
			Action = action;
			Owner = owner;
		}
	}

	private EventHandlerRec _nonSerializedListenerList;

	public void AddNonSerializedListener(object owner, Action action)
	{
		EventHandlerRec eventHandlerRec = new EventHandlerRec(owner, action);
		EventHandlerRec nonSerializedListenerList = _nonSerializedListenerList;
		_nonSerializedListenerList = eventHandlerRec;
		eventHandlerRec.Next = nonSerializedListenerList;
	}

	public void Invoke()
	{
		InvokeList(_nonSerializedListenerList);
	}

	private void InvokeList(EventHandlerRec list)
	{
		while (list != null)
		{
			list.Action();
			list = list.Next;
		}
	}

	public void ClearListeners(object o)
	{
		ClearListenerOfList(ref _nonSerializedListenerList, o);
	}

	private void ClearListenerOfList(ref EventHandlerRec list, object o)
	{
		EventHandlerRec eventHandlerRec = list;
		while (eventHandlerRec != null && eventHandlerRec.Owner != o)
		{
			eventHandlerRec = eventHandlerRec.Next;
		}
		if (eventHandlerRec == null)
		{
			return;
		}
		EventHandlerRec eventHandlerRec2 = list;
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
public class MbEvent<T> : IMbEvent<T>, IMbEventBase
{
	internal class EventHandlerRec<TS>
	{
		public EventHandlerRec<TS> Next;

		internal Action<TS> Action { get; private set; }

		internal object Owner { get; private set; }

		public EventHandlerRec(object owner, Action<TS> action)
		{
			Action = action;
			Owner = owner;
		}
	}

	private EventHandlerRec<T> _nonSerializedListenerList;

	public void AddNonSerializedListener(object owner, Action<T> action)
	{
		EventHandlerRec<T> eventHandlerRec = new EventHandlerRec<T>(owner, action);
		EventHandlerRec<T> nonSerializedListenerList = _nonSerializedListenerList;
		_nonSerializedListenerList = eventHandlerRec;
		eventHandlerRec.Next = nonSerializedListenerList;
	}

	public void Invoke(T t)
	{
		InvokeList(_nonSerializedListenerList, t);
	}

	private void InvokeList(EventHandlerRec<T> list, T t)
	{
		while (list != null)
		{
			list.Action(t);
			list = list.Next;
		}
	}

	public void ClearListeners(object o)
	{
		ClearListenerOfList(ref _nonSerializedListenerList, o);
	}

	private void ClearListenerOfList(ref EventHandlerRec<T> list, object o)
	{
		EventHandlerRec<T> eventHandlerRec = list;
		while (eventHandlerRec != null && eventHandlerRec.Owner != o)
		{
			eventHandlerRec = eventHandlerRec.Next;
		}
		if (eventHandlerRec == null)
		{
			return;
		}
		EventHandlerRec<T> eventHandlerRec2 = list;
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
public class MbEvent<T1, T2> : IMbEvent<T1, T2>, IMbEventBase
{
	internal class EventHandlerRec<TS, TQ>
	{
		public EventHandlerRec<TS, TQ> Next;

		internal Action<TS, TQ> Action { get; private set; }

		internal object Owner { get; private set; }

		public EventHandlerRec(object owner, Action<TS, TQ> action)
		{
			Action = action;
			Owner = owner;
		}
	}

	private EventHandlerRec<T1, T2> _nonSerializedListenerList;

	public void AddNonSerializedListener(object owner, Action<T1, T2> action)
	{
		EventHandlerRec<T1, T2> eventHandlerRec = new EventHandlerRec<T1, T2>(owner, action);
		EventHandlerRec<T1, T2> nonSerializedListenerList = _nonSerializedListenerList;
		_nonSerializedListenerList = eventHandlerRec;
		eventHandlerRec.Next = nonSerializedListenerList;
	}

	public void Invoke(T1 t1, T2 t2)
	{
		InvokeList(_nonSerializedListenerList, t1, t2);
	}

	private void InvokeList(EventHandlerRec<T1, T2> list, T1 t1, T2 t2)
	{
		while (list != null)
		{
			list.Action(t1, t2);
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
public class MbEvent<T1, T2, T3> : IMbEvent<T1, T2, T3>, IMbEventBase
{
	internal class EventHandlerRec<TS, TQ, TR>
	{
		public EventHandlerRec<TS, TQ, TR> Next;

		internal Action<TS, TQ, TR> Action { get; private set; }

		internal object Owner { get; private set; }

		public EventHandlerRec(object owner, Action<TS, TQ, TR> action)
		{
			Action = action;
			Owner = owner;
		}
	}

	private EventHandlerRec<T1, T2, T3> _nonSerializedListenerList;

	public void AddNonSerializedListener(object owner, Action<T1, T2, T3> action)
	{
		EventHandlerRec<T1, T2, T3> eventHandlerRec = new EventHandlerRec<T1, T2, T3>(owner, action);
		EventHandlerRec<T1, T2, T3> nonSerializedListenerList = _nonSerializedListenerList;
		_nonSerializedListenerList = eventHandlerRec;
		eventHandlerRec.Next = nonSerializedListenerList;
	}

	public void Invoke(T1 t1, T2 t2, T3 t3)
	{
		InvokeList(_nonSerializedListenerList, t1, t2, t3);
	}

	private void InvokeList(EventHandlerRec<T1, T2, T3> list, T1 t1, T2 t2, T3 t3)
	{
		while (list != null)
		{
			list.Action(t1, t2, t3);
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
public class MbEvent<T1, T2, T3, T4> : IMbEvent<T1, T2, T3, T4>, IMbEventBase
{
	internal class EventHandlerRec<TA, TB, TC, TD>
	{
		public EventHandlerRec<TA, TB, TC, TD> Next;

		internal Action<TA, TB, TC, TD> Action { get; private set; }

		internal object Owner { get; private set; }

		public EventHandlerRec(object owner, Action<TA, TB, TC, TD> action)
		{
			Action = action;
			Owner = owner;
		}
	}

	private EventHandlerRec<T1, T2, T3, T4> _nonSerializedListenerList;

	public void AddNonSerializedListener(object owner, Action<T1, T2, T3, T4> action)
	{
		EventHandlerRec<T1, T2, T3, T4> eventHandlerRec = new EventHandlerRec<T1, T2, T3, T4>(owner, action);
		EventHandlerRec<T1, T2, T3, T4> nonSerializedListenerList = _nonSerializedListenerList;
		_nonSerializedListenerList = eventHandlerRec;
		eventHandlerRec.Next = nonSerializedListenerList;
	}

	public void Invoke(T1 t1, T2 t2, T3 t3, T4 t4)
	{
		InvokeList(_nonSerializedListenerList, t1, t2, t3, t4);
	}

	private void InvokeList(EventHandlerRec<T1, T2, T3, T4> list, T1 t1, T2 t2, T3 t3, T4 t4)
	{
		while (list != null)
		{
			list.Action(t1, t2, t3, t4);
			list = list.Next;
		}
	}

	public void ClearListeners(object o)
	{
		ClearListenerOfList(ref _nonSerializedListenerList, o);
	}

	private void ClearListenerOfList(ref EventHandlerRec<T1, T2, T3, T4> list, object o)
	{
		EventHandlerRec<T1, T2, T3, T4> eventHandlerRec = list;
		while (eventHandlerRec != null && eventHandlerRec.Owner != o)
		{
			eventHandlerRec = eventHandlerRec.Next;
		}
		if (eventHandlerRec == null)
		{
			return;
		}
		EventHandlerRec<T1, T2, T3, T4> eventHandlerRec2 = list;
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
public class MbEvent<T1, T2, T3, T4, T5> : IMbEvent<T1, T2, T3, T4, T5>, IMbEventBase
{
	internal class EventHandlerRec<TA, TB, TC, TD, TE>
	{
		public EventHandlerRec<TA, TB, TC, TD, TE> Next;

		internal Action<TA, TB, TC, TD, TE> Action { get; private set; }

		internal object Owner { get; private set; }

		public EventHandlerRec(object owner, Action<TA, TB, TC, TD, TE> action)
		{
			Action = action;
			Owner = owner;
		}
	}

	private EventHandlerRec<T1, T2, T3, T4, T5> _nonSerializedListenerList;

	public void AddNonSerializedListener(object owner, Action<T1, T2, T3, T4, T5> action)
	{
		EventHandlerRec<T1, T2, T3, T4, T5> eventHandlerRec = new EventHandlerRec<T1, T2, T3, T4, T5>(owner, action);
		EventHandlerRec<T1, T2, T3, T4, T5> nonSerializedListenerList = _nonSerializedListenerList;
		_nonSerializedListenerList = eventHandlerRec;
		eventHandlerRec.Next = nonSerializedListenerList;
	}

	public void Invoke(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
	{
		InvokeList(_nonSerializedListenerList, t1, t2, t3, t4, t5);
	}

	private void InvokeList(EventHandlerRec<T1, T2, T3, T4, T5> list, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
	{
		while (list != null)
		{
			list.Action(t1, t2, t3, t4, t5);
			list = list.Next;
		}
	}

	public void ClearListeners(object o)
	{
		ClearListenerOfList(ref _nonSerializedListenerList, o);
	}

	private void ClearListenerOfList(ref EventHandlerRec<T1, T2, T3, T4, T5> list, object o)
	{
		EventHandlerRec<T1, T2, T3, T4, T5> eventHandlerRec = list;
		while (eventHandlerRec != null && eventHandlerRec.Owner != o)
		{
			eventHandlerRec = eventHandlerRec.Next;
		}
		if (eventHandlerRec == null)
		{
			return;
		}
		EventHandlerRec<T1, T2, T3, T4, T5> eventHandlerRec2 = list;
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
public class MbEvent<T1, T2, T3, T4, T5, T6> : IMbEvent<T1, T2, T3, T4, T5, T6>, IMbEventBase
{
	internal class EventHandlerRec<TA, TB, TC, TD, TE, TF>
	{
		public EventHandlerRec<TA, TB, TC, TD, TE, TF> Next;

		internal Action<TA, TB, TC, TD, TE, TF> Action { get; private set; }

		internal object Owner { get; private set; }

		public EventHandlerRec(object owner, Action<TA, TB, TC, TD, TE, TF> action)
		{
			Action = action;
			Owner = owner;
		}
	}

	private EventHandlerRec<T1, T2, T3, T4, T5, T6> _nonSerializedListenerList;

	public void AddNonSerializedListener(object owner, Action<T1, T2, T3, T4, T5, T6> action)
	{
		EventHandlerRec<T1, T2, T3, T4, T5, T6> eventHandlerRec = new EventHandlerRec<T1, T2, T3, T4, T5, T6>(owner, action);
		EventHandlerRec<T1, T2, T3, T4, T5, T6> nonSerializedListenerList = _nonSerializedListenerList;
		_nonSerializedListenerList = eventHandlerRec;
		eventHandlerRec.Next = nonSerializedListenerList;
	}

	public void Invoke(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
	{
		InvokeList(_nonSerializedListenerList, t1, t2, t3, t4, t5, t6);
	}

	private void InvokeList(EventHandlerRec<T1, T2, T3, T4, T5, T6> list, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
	{
		while (list != null)
		{
			list.Action(t1, t2, t3, t4, t5, t6);
			list = list.Next;
		}
	}

	public void ClearListeners(object o)
	{
		ClearListenerOfList(ref _nonSerializedListenerList, o);
	}

	private void ClearListenerOfList(ref EventHandlerRec<T1, T2, T3, T4, T5, T6> list, object o)
	{
		EventHandlerRec<T1, T2, T3, T4, T5, T6> eventHandlerRec = list;
		while (eventHandlerRec != null && eventHandlerRec.Owner != o)
		{
			eventHandlerRec = eventHandlerRec.Next;
		}
		if (eventHandlerRec == null)
		{
			return;
		}
		EventHandlerRec<T1, T2, T3, T4, T5, T6> eventHandlerRec2 = list;
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
public class MbEvent<T1, T2, T3, T4, T5, T6, T7> : IMbEvent<T1, T2, T3, T4, T5, T6, T7>, IMbEventBase
{
	internal class EventHandlerRec<TA, TB, TC, TD, TE, TF, TG>
	{
		public EventHandlerRec<TA, TB, TC, TD, TE, TF, TG> Next;

		internal Action<TA, TB, TC, TD, TE, TF, TG> Action { get; private set; }

		internal object Owner { get; private set; }

		public EventHandlerRec(object owner, Action<TA, TB, TC, TD, TE, TF, TG> action)
		{
			Action = action;
			Owner = owner;
		}
	}

	private EventHandlerRec<T1, T2, T3, T4, T5, T6, T7> _nonSerializedListenerList;

	public void AddNonSerializedListener(object owner, Action<T1, T2, T3, T4, T5, T6, T7> action)
	{
		EventHandlerRec<T1, T2, T3, T4, T5, T6, T7> eventHandlerRec = new EventHandlerRec<T1, T2, T3, T4, T5, T6, T7>(owner, action);
		EventHandlerRec<T1, T2, T3, T4, T5, T6, T7> nonSerializedListenerList = _nonSerializedListenerList;
		_nonSerializedListenerList = eventHandlerRec;
		eventHandlerRec.Next = nonSerializedListenerList;
	}

	public void Invoke(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
	{
		InvokeList(_nonSerializedListenerList, t1, t2, t3, t4, t5, t6, t7);
	}

	private void InvokeList(EventHandlerRec<T1, T2, T3, T4, T5, T6, T7> list, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
	{
		while (list != null)
		{
			list.Action(t1, t2, t3, t4, t5, t6, t7);
			list = list.Next;
		}
	}

	public void ClearListeners(object o)
	{
		ClearListenerOfList(ref _nonSerializedListenerList, o);
	}

	private void ClearListenerOfList(ref EventHandlerRec<T1, T2, T3, T4, T5, T6, T7> list, object o)
	{
		EventHandlerRec<T1, T2, T3, T4, T5, T6, T7> eventHandlerRec = list;
		while (eventHandlerRec != null && eventHandlerRec.Owner != o)
		{
			eventHandlerRec = eventHandlerRec.Next;
		}
		if (eventHandlerRec == null)
		{
			return;
		}
		EventHandlerRec<T1, T2, T3, T4, T5, T6, T7> eventHandlerRec2 = list;
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
