using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.MissionLogics;

internal struct NavalTroopAssignment
{
	public readonly IAgentOriginBase Origin;

	public readonly Agent Agent;

	public bool HasAgent => Agent != null;

	public bool IsValid => Origin != null;

	public int Priority => GetPriority(Origin, Agent);

	private NavalTroopAssignment(IAgentOriginBase origin, Agent agent = null)
	{
		Origin = origin;
		Agent = agent;
	}

	public bool Equals(in NavalTroopAssignment other)
	{
		if (Origin == other.Origin)
		{
			return Agent == other.Agent;
		}
		return false;
	}

	public static NavalTroopAssignment Invalid()
	{
		return new NavalTroopAssignment(null);
	}

	public static NavalTroopAssignment Create(IAgentOriginBase origin, Agent agent = null)
	{
		return new NavalTroopAssignment(origin, agent);
	}

	public static int GetPriority(IAgentOriginBase origin, Agent agent = null)
	{
		bool flag = agent != null;
		if ((flag && agent.IsMainAgent) || origin.Troop.IsPlayerCharacter)
		{
			return 4;
		}
		bool isHero = origin.Troop.IsHero;
		if (flag)
		{
			if (!isHero)
			{
				return 2;
			}
			return 3;
		}
		if (!isHero)
		{
			return 0;
		}
		return 1;
	}
}
