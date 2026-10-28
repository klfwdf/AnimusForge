using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;

public class MissionSingleplayerKillNotificationUIHandler : MissionView
{
	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
		if (affectedAgent.IsHuman)
		{
			string variable = ((affectorAgent == null) ? string.Empty : affectorAgent.Name);
			string variable2 = ((affectedAgent == null) ? string.Empty : affectedAgent.Name);
			uint color = 4291306250u;
			Agent main = Agent.Main;
			if (main != null && ((main.Team != base.Mission.SpectatorTeam && main.Team != affectedAgent.Team) || (affectorAgent != null && affectorAgent == main)))
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
