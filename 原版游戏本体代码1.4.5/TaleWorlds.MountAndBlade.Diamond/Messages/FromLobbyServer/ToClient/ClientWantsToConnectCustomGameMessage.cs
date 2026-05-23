using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
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
