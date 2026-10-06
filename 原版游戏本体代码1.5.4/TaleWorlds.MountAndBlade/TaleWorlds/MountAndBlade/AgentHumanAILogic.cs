using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class AgentHumanAILogic : MissionLogic
{
	public override void OnAgentCreated(Agent agent)
	{
		base.OnAgentCreated(agent);
		if (agent.IsAIControlled && agent.IsHuman)
		{
			agent.AddComponent(new HumanAIComponent(agent));
		}
	}

	protected internal override void OnAgentControllerChanged(Agent agent, AgentControllerType oldController)
	{
		base.OnAgentControllerChanged(agent, oldController);
		if (agent.IsHuman)
		{
			if (agent.Controller == AgentControllerType.AI)
			{
				agent.AddComponent(new HumanAIComponent(agent));
			}
			else if (oldController == AgentControllerType.AI && agent.HumanAIComponent != null)
			{
				agent.RemoveComponent(agent.HumanAIComponent);
			}
		}
	}

	public override void OnAgentMount(Agent agent)
	{
		base.OnAgentMount(agent);
		Mission.Current.UpdateMountReservationsAfterRiderMounts(agent, agent.MountAgent);
	}
}
