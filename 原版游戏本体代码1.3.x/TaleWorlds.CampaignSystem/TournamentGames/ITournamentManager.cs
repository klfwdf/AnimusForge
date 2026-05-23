using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.TournamentGames
{
	// Token: 0x020002CF RID: 719
	public interface ITournamentManager
	{
		// Token: 0x060026E9 RID: 9961
		void AddTournament(TournamentGame game);

		// Token: 0x060026EA RID: 9962
		TournamentGame GetTournamentGame(Town town);

		// Token: 0x060026EB RID: 9963
		void OnPlayerJoinMatch(Type gameType);

		// Token: 0x060026EC RID: 9964
		void OnPlayerJoinTournament(Type gameType, Settlement settlement);

		// Token: 0x060026ED RID: 9965
		void OnPlayerWatchTournament(Type gameType, Settlement settlement);

		// Token: 0x060026EE RID: 9966
		void OnPlayerWinMatch(Type gameType);

		// Token: 0x060026EF RID: 9967
		void OnPlayerWinTournament(Type gameType);

		// Token: 0x060026F0 RID: 9968
		void InitializeLeaderboardEntry(Hero hero, int initialVictories = 0);

		// Token: 0x060026F1 RID: 9969
		void AddLeaderboardEntry(Hero hero);

		// Token: 0x060026F2 RID: 9970
		void GivePrizeToWinner(TournamentGame tournament, Hero winner, bool isPlayerParticipated);

		// Token: 0x060026F3 RID: 9971
		void DeleteLeaderboardEntry(Hero hero);

		// Token: 0x060026F4 RID: 9972
		List<KeyValuePair<Hero, int>> GetLeaderboard();

		// Token: 0x060026F5 RID: 9973
		int GetLeaderBoardRank(Hero hero);

		// Token: 0x060026F6 RID: 9974
		Hero GetLeaderBoardLeader();

		// Token: 0x060026F7 RID: 9975
		void ResolveTournament(TournamentGame tournament, Town town);
	}
}
