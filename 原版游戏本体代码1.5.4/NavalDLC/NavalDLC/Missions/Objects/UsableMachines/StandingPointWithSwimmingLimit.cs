using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class StandingPointWithSwimmingLimit : StandingPoint
{
	public override bool IsDisabledForAgent(Agent agent)
	{
		if (agent.IsInWater())
		{
			return base.IsDisabledForAgent(agent);
		}
		return true;
	}
}
