using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem;

public class SandBoxMission
{
	public static IMission OpenTournamentArcheryMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
	{
		return SandBoxManager.Instance.SandBoxMissionManager.OpenTournamentArcheryMission(scene, tournamentGame, settlement, culture, isPlayerParticipating);
	}

	public static IMission OpenTournamentFightMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
	{
		return SandBoxManager.Instance.SandBoxMissionManager.OpenTournamentFightMission(scene, tournamentGame, settlement, culture, isPlayerParticipating);
	}

	public static IMission OpenTournamentHorseRaceMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
	{
		return SandBoxManager.Instance.SandBoxMissionManager.OpenTournamentHorseRaceMission(scene, tournamentGame, settlement, culture, isPlayerParticipating);
	}

	public static IMission OpenTournamentJoustingMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
	{
		return SandBoxManager.Instance.SandBoxMissionManager.OpenTournamentJoustingMission(scene, tournamentGame, settlement, culture, isPlayerParticipating);
	}

	public static IMission OpenBattleChallengeMission(string scene, IList<Hero> priorityCharsAttacker, IList<Hero> priorityCharsDefender)
	{
		return SandBoxManager.Instance.SandBoxMissionManager.OpenBattleChallengeMission(scene, priorityCharsAttacker, priorityCharsDefender);
	}
}
