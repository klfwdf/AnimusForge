using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class KickPlayerFromPartyMessage : Message
{
	[JsonProperty]
	public PlayerId KickedPlayerId { get; private set; }

	public KickPlayerFromPartyMessage()
	{
	}

	public KickPlayerFromPartyMessage(PlayerId kickedPlayerId)
	{
		KickedPlayerId = kickedPlayerId;
	}
}
