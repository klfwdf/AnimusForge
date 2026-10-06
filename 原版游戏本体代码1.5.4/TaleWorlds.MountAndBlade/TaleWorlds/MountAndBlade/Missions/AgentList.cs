using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade.Missions;

public class AgentList : AgentReadOnlyList
{
	public AgentList(int capacity)
		: base(capacity)
	{
	}

	public AgentList(IEnumerable<Agent> collection)
		: base(collection)
	{
	}

	public AgentList(List<Agent> collection)
		: base(collection)
	{
	}
}
