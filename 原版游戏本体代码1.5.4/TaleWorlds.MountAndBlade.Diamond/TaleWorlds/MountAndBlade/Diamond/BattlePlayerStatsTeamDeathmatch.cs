using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class BattlePlayerStatsTeamDeathmatch : BattlePlayerStatsBase
{
	public int Score { get; set; }

	public BattlePlayerStatsTeamDeathmatch()
	{
		base.GameType = "TeamDeathmatch";
	}
}
