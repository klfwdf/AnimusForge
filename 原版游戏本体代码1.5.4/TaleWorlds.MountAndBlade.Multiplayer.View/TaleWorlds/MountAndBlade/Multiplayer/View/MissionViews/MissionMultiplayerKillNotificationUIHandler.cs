using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;

public class MissionMultiplayerKillNotificationUIHandler : MissionView
{
	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
		if (!GameNetwork.IsDedicatedServer && affectedAgent.IsHuman)
		{
			string variable = ((affectorAgent == null) ? string.Empty : ((affectorAgent.MissionPeer != null) ? affectorAgent.MissionPeer.DisplayedName : affectorAgent.Name));
			string variable2 = ((affectedAgent.MissionPeer != null) ? affectedAgent.MissionPeer.DisplayedName : affectedAgent.Name);
			uint color = 4291306250u;
			MissionPeer missionPeer = null;
			if (GameNetwork.MyPeer != null)
			{
				missionPeer = GameNetwork.MyPeer.GetComponent<MissionPeer>();
			}
			if (missionPeer != null && ((missionPeer.Team != base.Mission.SpectatorTeam && missionPeer.Team != affectedAgent.Team) || (affectorAgent != null && affectorAgent.MissionPeer == missionPeer)))
			{
				color = 4281589009u;
			}
			TextObject textObject;
			if (affectorAgent != null)
			{
				textObject = new TextObject("{=2ZarUUbw}{KILLERPLAYERNAME} has killed {KILLEDPLAYERNAME}!");
				textObject.SetTextVariable("KILLERPLAYERNAME", variable);
			}
			else
			{
				textObject = new TextObject("{=9CnRKZOb}{KILLEDPLAYERNAME} has died!");
			}
			textObject.SetTextVariable("KILLEDPLAYERNAME", variable2);
			MessageManager.DisplayMessage(textObject.ToString(), color);
		}
	}
}
