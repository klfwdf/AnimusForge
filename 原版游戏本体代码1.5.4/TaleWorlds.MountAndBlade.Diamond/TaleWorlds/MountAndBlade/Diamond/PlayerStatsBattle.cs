using System;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class PlayerStatsBattle : PlayerStatsBase
{
	public int RoundsWon { get; private set; }

	public int RoundsLost { get; private set; }

	public PlayerStatsBattle()
	{
		base.GameType = "Battle";
	}

	public void FillWith(PlayerId playerId, int killCount, int deathCount, int assistCount, int winCount, int loseCount, int forfeitCount, int roundsWon, int roundsLost)
	{
		FillWith(playerId, killCount, deathCount, assistCount, winCount, loseCount, forfeitCount);
		RoundsWon = roundsWon;
		RoundsLost = roundsLost;
	}

	public void FillWithNewPlayer(PlayerId playerId)
	{
		FillWith(playerId, 0, 0, 0, 0, 0, 0, 0, 0);
	}

	public void Update(BattlePlayerStatsBattle stats, bool won)
	{
		base.Update(stats, won);
		RoundsWon += stats.RoundsWon;
		RoundsLost += stats.RoundsLost;
	}
}
