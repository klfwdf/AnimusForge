using SandBox.Tournaments.MissionLogics;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Arena;

public class ArenaDuelMissionBehavior : MissionLogic
{
	public override void AfterStart()
	{
		TournamentBehavior.DeleteTournamentSetsExcept(base.Mission.Scene.FindEntityWithTag("tournament_fight"));
	}
}
