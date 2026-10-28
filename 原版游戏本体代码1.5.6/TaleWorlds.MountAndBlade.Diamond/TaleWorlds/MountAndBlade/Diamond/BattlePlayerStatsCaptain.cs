using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class BattlePlayerStatsCaptain : BattlePlayerStatsBase
{
	public int CaptainsKilled { get; set; }

	public int MVPs { get; set; }

	public int Score { get; set; }

	public BattlePlayerStatsCaptain()
	{
		base.GameType = "Captain";
	}
}
