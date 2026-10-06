using System;

namespace TaleWorlds.CampaignSystem;

public interface IMbEvent
{
	void AddNonSerializedListener(object owner, Action action);

	void ClearListeners(object o);
}
public interface IMbEvent<out T> : IMbEventBase
{
	void AddNonSerializedListener(object owner, Action<T> action);
}
public interface IMbEvent<out T1, out T2> : IMbEventBase
{
	void AddNonSerializedListener(object owner, Action<T1, T2> action);
}
public interface IMbEvent<out T1, out T2, out T3> : IMbEventBase
{
	void AddNonSerializedListener(object owner, Action<T1, T2, T3> action);
}
public interface IMbEvent<out T1, out T2, out T3, out T4> : IMbEventBase
{
	void AddNonSerializedListener(object owner, Action<T1, T2, T3, T4> action);
}
public interface IMbEvent<out T1, out T2, out T3, out T4, out T5> : IMbEventBase
{
	void AddNonSerializedListener(object owner, Action<T1, T2, T3, T4, T5> action);
}
public interface IMbEvent<out T1, out T2, out T3, out T4, out T5, out T6> : IMbEventBase
{
	void AddNonSerializedListener(object owner, Action<T1, T2, T3, T4, T5, T6> action);
}
public interface IMbEvent<out T1, out T2, out T3, out T4, out T5, out T6, out T7> : IMbEventBase
{
	void AddNonSerializedListener(object owner, Action<T1, T2, T3, T4, T5, T6, T7> action);
}
