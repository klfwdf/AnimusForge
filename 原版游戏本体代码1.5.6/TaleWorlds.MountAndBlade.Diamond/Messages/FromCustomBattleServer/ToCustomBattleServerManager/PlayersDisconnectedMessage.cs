using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromCustomBattleServer.ToCustomBattleServerManager;

[Serializable]
[MessageDescription("CustomBattleServer", "CustomBattleServerManager", false)]
public class PlayersDisconnectedMessage : Message
{
	[JsonProperty]
	public PlayerDisconnectData[] Players { get; private set; }

	public PlayersDisconnectedMessage()
	{
	}

	public PlayersDisconnectedMessage(PlayerDisconnectData[] players)
	{
		Players = players;
	}
}
