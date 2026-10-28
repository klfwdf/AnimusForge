using System;
using Newtonsoft.Json;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class PlayerStatsSkirmish : PlayerStatsRanked
{
	public int MVPs { get; set; }

	public int Score { get; set; }

	[JsonIgnore]
	public int AverageScore => Score / ((base.WinCount + base.LoseCount == 0) ? 1 : (base.WinCount + base.LoseCount));

	public PlayerStatsSkirmish()
	{
		base.GameType = "Skirmish";
	}

	public void FillWith(PlayerId playerId, int killCount, int deathCount, int assistCount, int winCount, int loseCount, int forfeitCount, int rating, int ratingDeviation, string rank, bool evaluating, int evaluationMatchesPlayedCount, int mvps, int score)
	{
		FillWith(playerId, killCount, deathCount, assistCount, winCount, loseCount, forfeitCount, rating, ratingDeviation, rank, evaluating, evaluationMatchesPlayedCount);
		MVPs = mvps;
		Score = score;
	}

	public void FillWithNewPlayer(PlayerId playerId, int defaultRating, int defaultRatingDeviation)
	{
		FillWith(playerId, 0, 0, 0, 0, 0, 0, defaultRating, defaultRatingDeviation, "", evaluating: true, 0, 0, 0);
	}

	public void Update(BattlePlayerStatsSkirmish stats, bool won)
	{
		base.Update(stats, won);
		MVPs += stats.MVPs;
		Score += stats.Score;
	}
}
