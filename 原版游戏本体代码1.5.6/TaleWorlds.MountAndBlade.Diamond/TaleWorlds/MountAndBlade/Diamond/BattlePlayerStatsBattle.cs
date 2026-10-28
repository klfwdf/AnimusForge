using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class BattlePlayerStatsBattle : BattlePlayerStatsBase
{
	public int RoundsWon { get; set; }

	public int RoundsLost { get; set; }

	public BattlePlayerStatsBattle()
	{
		base.GameType = "Battle";
	}
}
