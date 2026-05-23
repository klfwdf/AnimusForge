using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class PlayerRemovedFromCustomGame : Message
{
	[JsonProperty]
	public DisconnectType DisconnectType { get; private set; }

	public PlayerRemovedFromCustomGame()
	{
	}

	public PlayerRemovedFromCustomGame(DisconnectType disconnectType)
	{
		DisconnectType = disconnectType;
	}
}
