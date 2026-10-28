using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class AgentCommonAILogic : MissionLogic
{
	public override void OnAgentCreated(Agent agent)
	{
		base.OnAgentCreated(agent);
		if (agent.IsAIControlled)
		{
			agent.AddComponent(new CommonAIComponent(agent));
		}
	}

	protected internal override void OnAgentControllerChanged(Agent agent, AgentControllerType oldController)
	{
		base.OnAgentControllerChanged(agent, oldController);
		if (agent.IsActive())
		{
			if (agent.Controller == AgentControllerType.AI)
			{
				agent.AddComponent(new CommonAIComponent(agent));
			}
			else if (oldController == AgentControllerType.AI && agent.CommonAIComponent != null)
			{
				agent.RemoveComponent(agent.CommonAIComponent);
			}
		}
	}
}
