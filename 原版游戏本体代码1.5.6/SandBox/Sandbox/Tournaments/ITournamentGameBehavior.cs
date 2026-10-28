using TaleWorlds.CampaignSystem.TournamentGames;

namespace SandBox.Tournaments;

public interface ITournamentGameBehavior
{
	void StartMatch(TournamentMatch match, bool isLastRound);

	void SkipMatch(TournamentMatch match);

	bool IsMatchEnded();

	void OnMatchEnded();
}
