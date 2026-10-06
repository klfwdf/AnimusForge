using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class BattlePlayerStatsSkirmish : BattlePlayerStatsBase
{
	public int MVPs { get; set; }

	public int Score { get; set; }

	public BattlePlayerStatsSkirmish()
	{
		base.GameType = "Skirmish";
	}
}
