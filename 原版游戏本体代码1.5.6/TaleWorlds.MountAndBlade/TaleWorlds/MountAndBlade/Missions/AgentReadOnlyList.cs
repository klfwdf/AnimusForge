using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Missions;

public class AgentReadOnlyList : MBReadOnlyList<Agent>
{
	public AgentReadOnlyList(int capacity)
		: base(capacity)
	{
	}

	public AgentReadOnlyList(IEnumerable<Agent> collection)
		: base(collection)
	{
	}

	public AgentReadOnlyList(List<Agent> collection)
		: base((IEnumerable<Agent>)collection)
	{
	}
}
