using System;
using Newtonsoft.Json;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
[JsonConverter(typeof(BattlePlayerStatsBaseJsonConverter))]
public class BattlePlayerStatsBase
{
	public string GameType { get; set; }

	public int Kills { get; set; }

	public int Assists { get; set; }

	public int Deaths { get; set; }

	public int PlayTime { get; set; }
}
