using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromCustomBattleServer.ToCustomBattleServerManager;

[Serializable]
[MessageDescription("CustomBattleServer", "CustomBattleServerManager", false)]
public class ResponseCustomGameClientConnectionMessage : Message
{
	[JsonProperty]
	public PlayerJoinGameResponseDataFromHost[] PlayerJoinData { get; private set; }

	public ResponseCustomGameClientConnectionMessage()
	{
	}

	public ResponseCustomGameClientConnectionMessage(PlayerJoinGameResponseDataFromHost[] playerJoinData)
	{
		PlayerJoinData = playerJoinData;
	}
}
