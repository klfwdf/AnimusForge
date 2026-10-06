using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class UpdatePlayerDataMessage : Message
{
	[JsonProperty]
	public PlayerData PlayerData { get; private set; }

	public UpdatePlayerDataMessage()
	{
	}

	public UpdatePlayerDataMessage(PlayerData playerData)
	{
		PlayerData = playerData;
	}
}
