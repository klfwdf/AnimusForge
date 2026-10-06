using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromCustomBattleServer.ToCustomBattleServerManager;

[Serializable]
[MessageDescription("CustomBattleServer", "CustomBattleServerManager", false)]
public class CustomBattleServerStatsUpdateMessage : Message
{
	[JsonProperty]
	public BattleResult BattleResult { get; set; }

	[JsonProperty]
	public Dictionary<int, int> TeamScores { get; set; }

	[JsonProperty]
	public Dictionary<string, int> PlayerScores { get; set; }

	public CustomBattleServerStatsUpdateMessage()
	{
	}

	public CustomBattleServerStatsUpdateMessage(BattleResult battleResult, Dictionary<int, int> teamScores, Dictionary<PlayerId, int> playerScores)
	{
		BattleResult = battleResult;
		TeamScores = teamScores;
		PlayerScores = playerScores.ToDictionary((KeyValuePair<PlayerId, int> kvp) => kvp.Key.ToString(), (KeyValuePair<PlayerId, int> kvp) => kvp.Value);
	}
}
