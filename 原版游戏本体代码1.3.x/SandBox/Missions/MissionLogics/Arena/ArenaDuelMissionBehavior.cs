using System;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Arena
{
	// Token: 0x02000096 RID: 150
	public class ArenaDuelMissionBehavior : MissionLogic
	{
		// Token: 0x06000637 RID: 1591 RVA: 0x0002A843 File Offset: 0x00028A43
		public override void AfterStart()
		{
			TournamentBehavior.DeleteTournamentSetsExcept(base.Mission.Scene.FindEntityWithTag("tournament_fight"));
		}
	}
}
