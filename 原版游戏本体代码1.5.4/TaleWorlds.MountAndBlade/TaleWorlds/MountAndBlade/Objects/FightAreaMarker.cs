using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.Objects;

public class FightAreaMarker : AreaMarker
{
	public int SubAreaIndex = 1;

	public IEnumerable<Agent> GetAgentsInRange(Team team, bool humanOnly = true)
	{
		foreach (Agent activeAgent in team.ActiveAgents)
		{
			if ((!humanOnly || activeAgent.IsHuman) && IsPositionInRange(activeAgent.Position))
			{
				yield return activeAgent;
			}
		}
	}

	public IEnumerable<Agent> GetAgentsInRange(BattleSideEnum side, bool humanOnly = true)
	{
		foreach (Team team in Mission.Current.Teams)
		{
			if (team.Side != side)
			{
				continue;
			}
			foreach (Agent item in GetAgentsInRange(team, humanOnly))
			{
				yield return item;
			}
		}
	}
}
