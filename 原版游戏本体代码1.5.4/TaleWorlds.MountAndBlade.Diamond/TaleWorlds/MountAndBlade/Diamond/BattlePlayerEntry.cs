using System;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class BattlePlayerEntry
{
	public PlayerId PlayerId { get; set; }

	public int TeamNo { get; set; }

	public Guid Party { get; set; }

	public BattlePlayerStatsBase PlayerStats { get; set; }

	public int PlayTime { get; set; }

	public DateTime LastJoinTime { get; set; }

	public bool Disconnected { get; set; }

	public string GameType { get; set; }

	public bool Won { get; set; }

	public BattleJoinType BattleJoinType { get; set; }
}
