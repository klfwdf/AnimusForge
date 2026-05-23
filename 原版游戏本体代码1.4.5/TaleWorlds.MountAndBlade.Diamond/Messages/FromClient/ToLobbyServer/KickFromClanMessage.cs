using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class KickFromClanMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	public KickFromClanMessage()
	{
	}

	public KickFromClanMessage(PlayerId playerId)
	{
		PlayerId = playerId;
	}
}
