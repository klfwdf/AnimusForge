using System;

namespace TaleWorlds.MountAndBlade.Diamond;

public class RecentPlayerInfo
{
	public string PlayerId { get; set; }

	public string PlayerName { get; set; }

	public int ImportanceScore { get; set; }

	public DateTime InteractionTime { get; set; }
}
