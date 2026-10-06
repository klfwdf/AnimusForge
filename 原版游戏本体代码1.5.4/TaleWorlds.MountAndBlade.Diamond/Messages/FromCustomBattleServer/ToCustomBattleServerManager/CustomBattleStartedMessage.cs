using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromCustomBattleServer.ToCustomBattleServerManager;

[Serializable]
[MessageDescription("CustomBattleServer", "CustomBattleServerManager", false)]
public class CustomBattleStartedMessage : Message
{
	[JsonProperty]
	public string GameType { get; set; }

	[JsonProperty]
	public Dictionary<string, int> PlayerTeams { get; set; }

	[JsonProperty]
	public List<string> FactionNames { get; set; }

	public CustomBattleStartedMessage()
	{
	}

	public CustomBattleStartedMessage(string gameType, Dictionary<PlayerId, int> playerTeams, List<string> factionNames)
	{
		GameType = gameType;
		PlayerTeams = playerTeams.ToDictionary((KeyValuePair<PlayerId, int> kvp) => kvp.Key.ToString(), (KeyValuePair<PlayerId, int> kvp) => kvp.Value);
		FactionNames = factionNames;
	}
}
