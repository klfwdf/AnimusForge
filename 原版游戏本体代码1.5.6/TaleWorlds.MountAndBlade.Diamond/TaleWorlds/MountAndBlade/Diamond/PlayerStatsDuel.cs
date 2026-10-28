using System;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class PlayerStatsDuel : PlayerStatsBase
{
	public int DuelsWon { get; set; }

	public int InfantryWins { get; set; }

	public int ArcherWins { get; set; }

	public int CavalryWins { get; set; }

	public PlayerStatsDuel()
	{
		base.GameType = "Duel";
	}

	public void FillWith(PlayerId playerId, int killCount, int deathCount, int assistCount, int winCount, int loseCount, int forfeitCount, int duelsWon, int infantryWins, int archerWins, int cavalryWins)
	{
		FillWith(playerId, killCount, deathCount, assistCount, winCount, loseCount, forfeitCount);
		DuelsWon = duelsWon;
		InfantryWins = infantryWins;
		ArcherWins = archerWins;
		CavalryWins = cavalryWins;
	}

	public void FillWithNewPlayer(PlayerId playerId)
	{
		FillWith(playerId, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
	}

	public void Update(BattlePlayerStatsDuel stats, bool won)
	{
		base.Update(stats, won);
		DuelsWon += stats.DuelsWon;
		InfantryWins += stats.InfantryWins;
		ArcherWins += stats.ArcherWins;
		CavalryWins += stats.CavalryWins;
	}
}
