using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem;

public interface ISandBoxMissionManager
{
	IMission OpenTournamentFightMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating);

	IMission OpenTournamentHorseRaceMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating);

	IMission OpenTournamentJoustingMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating);

	IMission OpenTournamentArcheryMission(string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating);

	IMission OpenBattleChallengeMission(string scene, IList<Hero> priorityCharsAttacker, IList<Hero> priorityCharsDefender);
}
