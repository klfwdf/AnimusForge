namespace TaleWorlds.CampaignSystem.TournamentGames;

public class TournamentRound
{
	public TournamentMatch[] Matches { get; private set; }

	public int CurrentMatchIndex { get; private set; }

	public TournamentMatch CurrentMatch
	{
		get
		{
			if (CurrentMatchIndex >= Matches.Length)
			{
				return null;
			}
			return Matches[CurrentMatchIndex];
		}
	}

	public TournamentRound(int participantCount, int numberOfMatches, int numberOfTeamsPerMatch, int numberOfWinnerParticipants, TournamentGame.QualificationMode qualificationMode)
	{
		Matches = new TournamentMatch[numberOfMatches];
		CurrentMatchIndex = 0;
		int participantCount2 = participantCount / numberOfMatches;
		for (int i = 0; i < numberOfMatches; i++)
		{
			Matches[i] = new TournamentMatch(participantCount2, numberOfTeamsPerMatch, numberOfWinnerParticipants / numberOfMatches, qualificationMode);
		}
	}

	public void OnMatchEnded()
	{
		CurrentMatchIndex++;
	}

	public void EndMatch()
	{
		CurrentMatch.End();
		CurrentMatchIndex++;
	}

	public void AddParticipant(TournamentParticipant participant, bool firstTime = false)
	{
		TournamentMatch[] matches = Matches;
		foreach (TournamentMatch tournamentMatch in matches)
		{
			if (tournamentMatch.IsParticipantRequired())
			{
				tournamentMatch.AddParticipant(participant, firstTime);
				break;
			}
		}
	}
}
