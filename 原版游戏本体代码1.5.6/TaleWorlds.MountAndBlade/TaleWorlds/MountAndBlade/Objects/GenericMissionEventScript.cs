using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.Objects;

public class GenericMissionEventScript : ScriptComponentBehavior
{
	public string EventId;

	public string Parameter;

	[EditableScriptComponentVariable(false, "")]
	public bool IsDisabled;

	public GenericMissionEventScript()
	{
		EventId = string.Empty;
		Parameter = string.Empty;
		IsDisabled = false;
	}

	public GenericMissionEventScript(string eventId, string parameter)
	{
		EventId = eventId;
		Parameter = parameter;
		IsDisabled = false;
	}
}
