using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade;

public class StonePileAI : UsableMachineAIBase
{
	public StonePileAI(StonePile stonePile)
		: base(stonePile)
	{
	}

	public static Agent GetSuitableAgentForStandingPoint(StonePile usableMachine, StandingPoint standingPoint, List<Agent> agents, List<Agent> usedAgents)
	{
		float num = float.MinValue;
		Agent result = null;
		foreach (Agent agent in agents)
		{
			if (IsAgentAssignable(agent) && !standingPoint.IsDisabledForAgent(agent) && standingPoint.GetUsageScoreForAgent(agent) > num)
			{
				num = standingPoint.GetUsageScoreForAgent(agent);
				result = agent;
			}
		}
		return result;
	}

	public static Agent GetSuitableAgentForStandingPoint(StonePile stonePile, StandingPoint standingPoint, List<(Agent, float)> agents, List<Agent> usedAgents, float weight)
	{
		float num = float.MinValue;
		Agent result = null;
		foreach (var agent in agents)
		{
			Agent item = agent.Item1;
			if (IsAgentAssignable(item) && !standingPoint.IsDisabledForAgent(item) && standingPoint.GetUsageScoreForAgent(item) > num)
			{
				num = standingPoint.GetUsageScoreForAgent(item);
				result = item;
			}
		}
		return result;
	}

	public static bool IsAgentAssignable(Agent agent)
	{
		if (agent != null && agent.IsAIControlled && agent.IsActive() && !agent.IsRunningAway && !agent.InteractingWithAnyGameObject())
		{
			if (agent.Formation != null)
			{
				return !agent.IsDetachedFromFormation;
			}
			return true;
		}
		return false;
	}

	protected override void HandleAgentStopUsingStandingPoint(Agent agent, StandingPoint standingPoint)
	{
		agent.DisableScriptedCombatMovement();
		base.HandleAgentStopUsingStandingPoint(agent, standingPoint);
	}
}
