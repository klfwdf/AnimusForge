using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade;

public class StandingPointWithAgentLimit : StandingPoint
{
	private readonly List<Agent> _validAgents = new List<Agent>();

	public void AddValidAgent(Agent agent)
	{
		if (agent != null)
		{
			_validAgents.Add(agent);
		}
	}

	public void ClearValidAgents()
	{
		_validAgents.Clear();
	}

	public override bool IsDisabledForAgent(Agent agent)
	{
		if (_validAgents.Contains(agent))
		{
			return base.IsDisabledForAgent(agent);
		}
		return true;
	}
}
