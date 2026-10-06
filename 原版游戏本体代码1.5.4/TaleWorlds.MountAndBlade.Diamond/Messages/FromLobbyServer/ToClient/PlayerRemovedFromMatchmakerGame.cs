using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class PlayerRemovedFromMatchmakerGame : Message
{
	[JsonProperty]
	public DisconnectType DisconnectType { get; private set; }

	public PlayerRemovedFromMatchmakerGame()
	{
	}

	public PlayerRemovedFromMatchmakerGame(DisconnectType disconnectType)
	{
		DisconnectType = disconnectType;
	}
}
