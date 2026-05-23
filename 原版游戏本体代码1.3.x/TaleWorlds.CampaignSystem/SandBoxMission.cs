using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x020000A3 RID: 163
	public class SandBoxMission
	{
		// Token: 0x06001330 RID: 4912 RVA: 0x00057042 File Offset: 0x00055242
		public static IMission OpenTournamentArcheryMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
		{
			return SandBoxManager.Instance.SandBoxMissionManager.OpenTournamentArcheryMission(scene, tournamentGame, settlement, culture, isPlayerParticipating);
		}

		// Token: 0x06001331 RID: 4913 RVA: 0x00057059 File Offset: 0x00055259
		public static IMission OpenTournamentFightMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
		{
			return SandBoxManager.Instance.SandBoxMissionManager.OpenTournamentFightMission(scene, tournamentGame, settlement, culture, isPlayerParticipating);
		}

		// Token: 0x06001332 RID: 4914 RVA: 0x00057070 File Offset: 0x00055270
		public static IMission OpenTournamentHorseRaceMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
		{
			return SandBoxManager.Instance.SandBoxMissionManager.OpenTournamentHorseRaceMission(scene, tournamentGame, settlement, culture, isPlayerParticipating);
		}

		// Token: 0x06001333 RID: 4915 RVA: 0x00057087 File Offset: 0x00055287
		public static IMission OpenTournamentJoustingMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
		{
			return SandBoxManager.Instance.SandBoxMissionManager.OpenTournamentJoustingMission(scene, tournamentGame, settlement, culture, isPlayerParticipating);
		}

		// Token: 0x06001334 RID: 4916 RVA: 0x0005709E File Offset: 0x0005529E
		public static IMission OpenBattleChallengeMission(string scene, IList<Hero> priorityCharsAttacker, IList<Hero> priorityCharsDefender)
		{
			return SandBoxManager.Instance.SandBoxMissionManager.OpenBattleChallengeMission(scene, priorityCharsAttacker, priorityCharsDefender);
		}
	}
}
