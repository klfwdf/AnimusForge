using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class SandBoxMissionHandler : MissionLogic
{
	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if ((affectedAgent == null || affectedAgent.Character == CharacterObject.PlayerCharacter) && (affectedAgent == affectorAgent || affectorAgent == null))
		{
			Campaign.Current.GameMenuManager.SetNextMenu("settlement_player_unconscious");
		}
	}
}
