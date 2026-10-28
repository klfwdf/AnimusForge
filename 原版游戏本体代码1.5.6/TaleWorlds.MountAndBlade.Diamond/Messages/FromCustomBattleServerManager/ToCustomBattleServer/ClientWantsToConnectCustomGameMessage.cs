using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromCustomBattleServerManager.ToCustomBattleServer;

[Serializable]
[MessageDescription("CustomBattleServerManager", "CustomBattleServer", true)]
public class ClientWantsToConnectCustomGameMessage : Message
{
	[JsonProperty]
	public PlayerJoinGameData[] PlayerJoinGameData { get; private set; }

	public ClientWantsToConnectCustomGameMessage()
	{
	}

	public ClientWantsToConnectCustomGameMessage(PlayerJoinGameData[] playerJoinGameData)
	{
		PlayerJoinGameData = playerJoinGameData;
	}
}
