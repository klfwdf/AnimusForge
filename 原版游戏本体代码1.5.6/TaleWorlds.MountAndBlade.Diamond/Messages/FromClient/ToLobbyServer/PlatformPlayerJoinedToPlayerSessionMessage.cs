using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class PlatformPlayerJoinedToPlayerSessionMessage : Message
{
	[JsonProperty]
	public PlayerId InviterPlayerId { get; private set; }

	public PlatformPlayerJoinedToPlayerSessionMessage()
	{
	}

	public PlatformPlayerJoinedToPlayerSessionMessage(PlayerId inviterPlayerId)
	{
		InviterPlayerId = inviterPlayerId;
	}
}
