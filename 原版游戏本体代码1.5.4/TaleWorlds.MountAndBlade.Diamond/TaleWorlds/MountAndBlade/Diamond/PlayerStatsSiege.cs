using System;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class PlayerStatsSiege : PlayerStatsBase
{
	public int WallsBreached { get; set; }

	public int SiegeEngineKills { get; set; }

	public int SiegeEnginesDestroyed { get; set; }

	public int ObjectiveGoldGained { get; set; }

	public int Score { get; set; }

	public int AverageScore => Score / ((base.WinCount + base.LoseCount == 0) ? 1 : (base.WinCount + base.LoseCount));

	public int AverageKillCount => base.KillCount / ((base.WinCount + base.LoseCount == 0) ? 1 : (base.WinCount + base.LoseCount));

	public PlayerStatsSiege()
	{
		base.GameType = "Siege";
	}

	public void FillWith(PlayerId playerId, int killCount, int deathCount, int assistCount, int winCount, int loseCount, int forfeitCount, int wallsBreached, int siegeEngineKills, int siegeEnginesDestroyed, int objectiveGoldGained, int score)
	{
		FillWith(playerId, killCount, deathCount, assistCount, winCount, loseCount, forfeitCount);
		WallsBreached = wallsBreached;
		SiegeEngineKills = siegeEngineKills;
		SiegeEnginesDestroyed = siegeEnginesDestroyed;
		ObjectiveGoldGained = objectiveGoldGained;
		Score = score;
	}

	public void FillWithNewPlayer(PlayerId playerId)
	{
		FillWith(playerId, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
	}

	public void Update(BattlePlayerStatsSiege stats, bool won)
	{
		base.Update(stats, won);
		WallsBreached += stats.WallsBreached;
		SiegeEngineKills += stats.SiegeEngineKills;
		SiegeEnginesDestroyed += stats.SiegeEnginesDestroyed;
		ObjectiveGoldGained += stats.ObjectiveGoldGained;
		Score += stats.Score;
	}
}
