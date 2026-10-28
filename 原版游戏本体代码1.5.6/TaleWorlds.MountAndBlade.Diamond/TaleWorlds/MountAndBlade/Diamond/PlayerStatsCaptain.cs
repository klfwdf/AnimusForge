using System;
using Newtonsoft.Json;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class PlayerStatsCaptain : PlayerStatsRanked
{
	public int CaptainsKilled { get; set; }

	public int MVPs { get; set; }

	public int Score { get; set; }

	[JsonIgnore]
	public int AverageScore
	{
		get
		{
			if (Score / (base.WinCount + base.LoseCount) == 0)
			{
				return 1;
			}
			return base.WinCount + base.LoseCount;
		}
	}

	public PlayerStatsCaptain()
	{
		base.GameType = "Captain";
	}

	public void FillWith(PlayerId playerId, int killCount, int deathCount, int assistCount, int winCount, int loseCount, int forfeitCount, int rating, int ratingDeviation, string rank, bool evaluating, int evaluationMatchesPlayedCount, int captainsKilled, int mvps, int score)
	{
		FillWith(playerId, killCount, deathCount, assistCount, winCount, loseCount, forfeitCount, rating, ratingDeviation, rank, evaluating, evaluationMatchesPlayedCount);
		CaptainsKilled = captainsKilled;
		MVPs = mvps;
		Score = score;
	}

	public void FillWithNewPlayer(PlayerId playerId, int defaultRating, int defaultRatingDeviation)
	{
		FillWith(playerId, 0, 0, 0, 0, 0, 0, defaultRating, defaultRatingDeviation, "", evaluating: true, 0, 0, 0, 0);
	}

	public void Update(BattlePlayerStatsCaptain stats, bool won)
	{
		base.Update(stats, won);
		CaptainsKilled += stats.CaptainsKilled;
		MVPs += stats.MVPs;
		Score += stats.Score;
	}
}
