using System;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class PlayerStatsRanked : PlayerStatsBase
{
	public int Rating { get; set; }

	public string Rank { get; set; }

	public bool Evaluating { get; set; }

	public int EvaluationMatchesPlayedCount { get; set; }

	public void FillWith(PlayerId playerId, int killCount, int deathCount, int assistCount, int winCount, int loseCount, int forfeitCount, int rating, int ratingDeviation, string rank, bool evaluating, int evaluationMatchesPlayedCount)
	{
		FillWith(playerId, killCount, deathCount, assistCount, winCount, loseCount, forfeitCount);
		Rating = rating;
		Rank = rank;
		Evaluating = evaluating;
		EvaluationMatchesPlayedCount = evaluationMatchesPlayedCount;
	}

	public virtual void FillWithNewPlayer(PlayerId playerId, string gameType, int defaultRating, int defaultRatingDeviation)
	{
		FillWith(playerId, 0, 0, 0, 0, 0, 0, defaultRating, defaultRatingDeviation, "", evaluating: true, 0);
	}
}
