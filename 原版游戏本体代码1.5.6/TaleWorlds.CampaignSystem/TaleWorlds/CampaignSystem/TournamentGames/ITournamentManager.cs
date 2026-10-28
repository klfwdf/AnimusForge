using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.TournamentGames;

public interface ITournamentManager
{
	void AddTournament(TournamentGame game);

	TournamentGame GetTournamentGame(Town town);

	void OnPlayerJoinMatch(Type gameType);

	void OnPlayerJoinTournament(Type gameType, Settlement settlement);

	void OnPlayerWatchTournament(Type gameType, Settlement settlement);

	void OnPlayerWinMatch(Type gameType);

	void OnPlayerWinTournament(Type gameType);

	void InitializeLeaderboardEntry(Hero hero, int initialVictories = 0);

	void AddLeaderboardEntry(Hero hero);

	void GivePrizeToWinner(TournamentGame tournament, Hero winner, bool isPlayerParticipated);

	void DeleteLeaderboardEntry(Hero hero);

	List<KeyValuePair<Hero, int>> GetLeaderboard();

	int GetLeaderBoardRank(Hero hero);

	Hero GetLeaderBoardLeader();

	void ResolveTournament(TournamentGame tournament, Town town);
}
