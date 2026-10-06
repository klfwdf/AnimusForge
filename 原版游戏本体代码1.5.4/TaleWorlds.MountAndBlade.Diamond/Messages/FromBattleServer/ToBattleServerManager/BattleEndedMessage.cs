using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromBattleServer.ToBattleServerManager;

[Serializable]
[MessageDescription("BattleServer", "BattleServerManager", true)]
public class BattleEndedMessage : Message
{
	[JsonProperty]
	public BattleResult BattleResult { get; set; }

	[JsonProperty]
	public GameLog[] GameLogs { get; set; }

	[JsonProperty]
	public List<BadgeDataEntry> BadgeDataEntries { get; set; }

	[JsonProperty]
	public Dictionary<int, int> TeamScores { get; set; }

	[JsonProperty]
	public Dictionary<string, int> PlayerScores { get; set; }

	[JsonProperty]
	public int GameTime { get; set; }

	public BattleEndedMessage()
	{
	}

	public BattleEndedMessage(BattleResult battleResult, GameLog[] gameLogs, Dictionary<(PlayerId, string, string), int> badgeDataDictionary, int gameTime, Dictionary<int, int> teamScores, Dictionary<PlayerId, int> playerScores)
	{
		BattleResult = battleResult;
		GameLogs = gameLogs;
		BadgeDataEntries = BadgeDataEntry.ToList(badgeDataDictionary);
		TeamScores = teamScores;
		PlayerScores = playerScores.ToDictionary((KeyValuePair<PlayerId, int> kvp) => kvp.Key.ToString(), (KeyValuePair<PlayerId, int> kvp) => kvp.Value);
		GameTime = gameTime;
	}
}
