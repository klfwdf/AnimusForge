using System;
using TaleWorlds.CampaignSystem.TournamentGames;

namespace SandBox.Tournaments
{
	// Token: 0x0200002A RID: 42
	public interface ITournamentGameBehavior
	{
		// Token: 0x0600013F RID: 319
		void StartMatch(TournamentMatch match, bool isLastRound);

		// Token: 0x06000140 RID: 320
		void SkipMatch(TournamentMatch match);

		// Token: 0x06000141 RID: 321
		bool IsMatchEnded();

		// Token: 0x06000142 RID: 322
		void OnMatchEnded();
	}
}
