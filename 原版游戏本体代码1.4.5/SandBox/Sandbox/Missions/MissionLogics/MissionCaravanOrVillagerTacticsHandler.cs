using System.Linq;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class MissionCaravanOrVillagerTacticsHandler : MissionLogic
{
	public override void EarlyStart()
	{
		foreach (Team team in Mission.Current.Teams)
		{
			if (team.HasTeamAi && (MapEvent.PlayerMapEvent.PartiesOnSide(team.Side).Any((MapEventParty p) => p.Party.IsMobile && p.Party.MobileParty.IsCaravan) || (MapEvent.PlayerMapEvent.MapEventSettlement == null && MapEvent.PlayerMapEvent.PartiesOnSide(team.Side).Any((MapEventParty p) => p.Party.IsMobile && p.Party.MobileParty.IsVillager))))
			{
				team.AddTacticOption(new TacticDefensiveLine(team));
			}
		}
	}
}
