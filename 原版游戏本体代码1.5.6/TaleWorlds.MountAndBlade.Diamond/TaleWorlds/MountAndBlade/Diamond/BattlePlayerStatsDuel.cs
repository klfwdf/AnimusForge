using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class BattlePlayerStatsDuel : BattlePlayerStatsBase
{
	public int DuelsWon { get; set; }

	public int InfantryWins { get; set; }

	public int ArcherWins { get; set; }

	public int CavalryWins { get; set; }

	public BattlePlayerStatsDuel()
	{
		base.GameType = "Duel";
	}
}
