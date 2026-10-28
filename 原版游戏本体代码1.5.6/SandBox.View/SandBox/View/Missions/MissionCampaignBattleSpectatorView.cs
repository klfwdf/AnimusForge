using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace SandBox.View.Missions;

public class MissionCampaignBattleSpectatorView : MissionView
{
	public override void AfterStart()
	{
		base.MissionScreen.SetCustomAgentListToSpectateGatherer(SpectateListGatherer);
	}

	private int CalculateAgentScore(Agent agent)
	{
		Mission mission = agent.Mission;
		CharacterObject characterObject = (CharacterObject)agent.Character;
		int num = (agent.IsPlayerControlled ? 2000000 : 0);
		if (agent.Team != null && agent.Team.IsValid)
		{
			num += ((mission.PlayerTeam == null || !mission.PlayerTeam.IsValid || !agent.Team.IsEnemyOf(mission.PlayerTeam)) ? 1000000 : 0);
			if (agent.Team.GeneralAgent == agent)
			{
				num += 500000;
			}
			else if (characterObject.IsHero)
			{
				num = ((!characterObject.HeroObject.IsLord) ? (num + 250000) : (num + 125000));
				foreach (Formation item in agent.Team.FormationsIncludingEmpty)
				{
					if (item.Captain == agent)
					{
						num += 100000;
					}
				}
			}
			if (characterObject.IsMounted)
			{
				num += 50000;
			}
			if (!characterObject.IsRanged)
			{
				num += 25000;
			}
			num += (int)agent.Health;
		}
		return num;
	}

	private List<Agent> SpectateListGatherer(Agent forcedAgentToInclude)
	{
		return base.Mission.AllAgents.WhereQ((Agent x) => x.IsCameraAttachable() || x == forcedAgentToInclude).OrderByDescending(CalculateAgentScore).ToList();
	}
}
