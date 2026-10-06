using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromCustomBattleServer.ToCustomBattleServerManager;

[Serializable]
[MessageDescription("CustomBattleServer", "CustomBattleServerManager", false)]
public class AddGameLogsMessage : Message
{
	[JsonProperty]
	public GameLog[] GameLogs { get; private set; }

	public AddGameLogsMessage()
	{
	}

	public AddGameLogsMessage(GameLog[] gameLogs)
	{
		GameLogs = gameLogs;
	}
}
