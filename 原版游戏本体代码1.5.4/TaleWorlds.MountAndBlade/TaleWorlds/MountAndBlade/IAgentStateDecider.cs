using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public interface IAgentStateDecider : IMissionBehavior
{
	AgentState GetAgentState(Agent affectedAgent, float deathProbability, out bool usedSurgery);
}
