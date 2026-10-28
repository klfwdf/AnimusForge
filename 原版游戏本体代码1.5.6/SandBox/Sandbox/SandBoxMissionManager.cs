using System.Collections.Generic;
using SandBox.Tournaments;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;

namespace SandBox;

public class SandBoxMissionManager : ISandBoxMissionManager
{
	public IMission OpenTournamentFightMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
	{
		return TournamentMissionStarter.OpenTournamentFightMission(scene, tournamentGame, settlement, culture, isPlayerParticipating);
	}

	public IMission OpenTournamentHorseRaceMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
	{
		return TournamentMissionStarter.OpenTournamentHorseRaceMission(scene, tournamentGame, settlement, culture, isPlayerParticipating);
	}

	public IMission OpenTournamentJoustingMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
	{
		return TournamentMissionStarter.OpenTournamentJoustingMission(scene, tournamentGame, settlement, culture, isPlayerParticipating);
	}

	public IMission OpenTournamentArcheryMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
	{
		return TournamentMissionStarter.OpenTournamentArcheryMission(scene, tournamentGame, settlement, culture, isPlayerParticipating);
	}

	IMission ISandBoxMissionManager.OpenBattleChallengeMission(string scene, IList<Hero> priorityCharsAttacker, IList<Hero> priorityCharsDefender)
	{
		return TournamentMissionStarter.OpenBattleChallengeMission(scene, priorityCharsAttacker, priorityCharsDefender);
	}
}
