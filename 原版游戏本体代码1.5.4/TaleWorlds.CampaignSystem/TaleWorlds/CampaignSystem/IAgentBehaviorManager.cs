using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem;

public interface IAgentBehaviorManager
{
	void AddQuestCharacterBehaviors(IAgent agent);

	void AddWandererBehaviors(IAgent agent);

	void AddOutdoorWandererBehaviors(IAgent agent);

	void AddIndoorWandererBehaviors(IAgent agent);

	void AddFixedCharacterBehaviors(IAgent agent);

	void AddPatrollingThugBehaviors(IAgent agent);

	void AddStandGuardBehaviors(IAgent agent);

	void AddFixedGuardBehaviors(IAgent agent);

	void AddStealthAgentBehaviors(IAgent agent);

	void AddPatrollingGuardBehaviors(IAgent agent);

	void AddCompanionBehaviors(IAgent agent);

	void AddBodyguardBehaviors(IAgent agent);

	void AddFirstCompanionBehavior(IAgent agent);
}
