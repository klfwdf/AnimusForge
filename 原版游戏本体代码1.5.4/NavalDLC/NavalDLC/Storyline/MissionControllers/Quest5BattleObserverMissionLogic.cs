using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Storyline.MissionControllers;

internal class Quest5BattleObserverMissionLogic : BattleObserverMissionLogic
{
	private bool _isGunnarAddedBefore;

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (affectedAgent.Character != NavalStorylineData.Gunnar.CharacterObject)
		{
			base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		}
	}

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		if (agent.Character == NavalStorylineData.Gunnar.CharacterObject)
		{
			if (!_isGunnarAddedBefore)
			{
				_isGunnarAddedBefore = true;
				base.OnAgentBuild(agent, banner);
			}
		}
		else
		{
			base.OnAgentBuild(agent, banner);
		}
	}
}
