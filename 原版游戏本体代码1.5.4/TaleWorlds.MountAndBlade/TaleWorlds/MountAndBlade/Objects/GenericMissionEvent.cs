using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.MountAndBlade.Objects;

public class GenericMissionEvent : EventBase
{
	public readonly string EventId;

	public readonly string Parameter;

	public GenericMissionEvent(string eventId, string parameter)
	{
		EventId = eventId;
		Parameter = parameter;
	}
}
