using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest3;

internal class AgentObjectiveTarget : MissionObjectiveTarget
{
	private readonly Agent _agent;

	internal AgentObjectiveTarget(Agent agent)
	{
		_agent = agent;
	}

	public override Vec3 GetGlobalPosition()
	{
		return _agent.Position;
	}

	public override TextObject GetName()
	{
		return _agent.NameTextObject;
	}

	public override bool IsActive()
	{
		if (_agent != null)
		{
			return _agent.IsActive();
		}
		return false;
	}
}
