using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromCustomBattleServer.ToCustomBattleServerManager;

[Serializable]
[MessageDescription("CustomBattleServer", "CustomBattleServerManager", true)]
public class CustomBattleServerFinishingMessage : Message
{
	[JsonProperty]
	public GameLog[] GameLogs { get; private set; }

	[JsonProperty]
	public List<BadgeDataEntry> BadgeDataEntries { get; private set; }

	[JsonProperty]
	public MultipleBattleResult BattleResult { get; private set; }

	public CustomBattleServerFinishingMessage()
	{
	}

	public CustomBattleServerFinishingMessage(GameLog[] gameLogs, Dictionary<(PlayerId, string, string), int> badgeDataDictionary, MultipleBattleResult battleResult)
	{
		GameLogs = gameLogs;
		BadgeDataEntries = BadgeDataEntry.ToList(badgeDataDictionary);
		BattleResult = battleResult;
	}
}
