using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class StandingPointWithTeamLimit : StandingPoint
{
	public Team UsableTeam { get; set; }

	public override bool IsDisabledForAgent(Agent agent)
	{
		if (agent.Team == UsableTeam)
		{
			return base.IsDisabledForAgent(agent);
		}
		return true;
	}

	protected internal override bool IsUsableBySide(BattleSideEnum side)
	{
		if (side == UsableTeam.Side)
		{
			return base.IsUsableBySide(side);
		}
		return false;
	}
}
