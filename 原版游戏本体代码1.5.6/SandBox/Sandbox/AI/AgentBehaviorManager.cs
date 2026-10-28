using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace SandBox.AI;

public class AgentBehaviorManager : IAgentBehaviorManager
{
	public void AddQuestCharacterBehaviors(IAgent agent)
	{
		BehaviorSets.AddQuestCharacterBehaviors(agent);
	}

	void IAgentBehaviorManager.AddWandererBehaviors(IAgent agent)
	{
		BehaviorSets.AddWandererBehaviors(agent);
	}

	void IAgentBehaviorManager.AddOutdoorWandererBehaviors(IAgent agent)
	{
		BehaviorSets.AddOutdoorWandererBehaviors(agent);
	}

	void IAgentBehaviorManager.AddIndoorWandererBehaviors(IAgent agent)
	{
		BehaviorSets.AddIndoorWandererBehaviors(agent);
	}

	void IAgentBehaviorManager.AddFixedCharacterBehaviors(IAgent agent)
	{
		BehaviorSets.AddFixedCharacterBehaviors(agent);
	}

	void IAgentBehaviorManager.AddPatrollingThugBehaviors(IAgent agent)
	{
		BehaviorSets.AddPatrollingThugBehaviors(agent);
	}

	void IAgentBehaviorManager.AddStandGuardBehaviors(IAgent agent)
	{
		BehaviorSets.AddStandGuardBehaviors(agent);
	}

	void IAgentBehaviorManager.AddFixedGuardBehaviors(IAgent agent)
	{
		BehaviorSets.AddFixedGuardBehaviors(agent);
	}

	void IAgentBehaviorManager.AddStealthAgentBehaviors(IAgent agent)
	{
		BehaviorSets.StealthAgentBehaviors(agent);
	}

	void IAgentBehaviorManager.AddPatrollingGuardBehaviors(IAgent agent)
	{
		BehaviorSets.AddPatrollingGuardBehaviors(agent);
	}

	void IAgentBehaviorManager.AddCompanionBehaviors(IAgent agent)
	{
		BehaviorSets.AddCompanionBehaviors(agent);
	}

	void IAgentBehaviorManager.AddBodyguardBehaviors(IAgent agent)
	{
		BehaviorSets.AddBodyguardBehaviors(agent);
	}

	public void AddFirstCompanionBehavior(IAgent agent)
	{
		BehaviorSets.AddFirstCompanionBehavior(agent);
	}
}
