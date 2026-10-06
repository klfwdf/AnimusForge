namespace TaleWorlds.CampaignSystem;

public interface ReferenceIMBEvent<T1> : IMbEventBase
{
	void AddNonSerializedListener(object owner, ReferenceAction<T1> action);
}
public interface ReferenceIMBEvent<T1, T2> : IMbEventBase
{
	void AddNonSerializedListener(object owner, ReferenceAction<T1, T2> action);
}
public interface ReferenceIMBEvent<T1, T2, T3> : IMbEventBase
{
	void AddNonSerializedListener(object owner, ReferenceAction<T1, T2, T3> action);
}
