using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;

public class BattleMissionAgentInteractionLogic : MissionLogic
{
	public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
	{
		if (otherAgent.IsMount)
		{
			if (otherAgent.IsActive())
			{
				if (otherAgent.RiderAgent != userAgent)
				{
					if (otherAgent.RiderAgent == null)
					{
						return (userAgent.GetAgentFlags() & AgentFlag.CanRide) == AgentFlag.CanRide;
					}
					return false;
				}
				return true;
			}
			return false;
		}
		return false;
	}
}
