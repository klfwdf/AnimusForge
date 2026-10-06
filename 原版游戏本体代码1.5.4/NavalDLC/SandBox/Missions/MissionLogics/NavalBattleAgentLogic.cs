using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class NavalBattleAgentLogic : BattleAgentLogic
{
	private NavalAgentsLogic _agentsLogic;

	public override void OnBehaviorInitialize()
	{
		_agentsLogic = base.Mission.GetMissionBehavior<NavalAgentsLogic>();
		base.OnBehaviorInitialize();
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (!_agentsLogic.IsDeploymentMode && !_agentsLogic.IsMissionEnding)
		{
			base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
		}
	}
}
