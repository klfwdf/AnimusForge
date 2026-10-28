using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class GetAnotherPlayerStateMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	public GetAnotherPlayerStateMessage()
	{
	}

	public GetAnotherPlayerStateMessage(PlayerId playerId)
	{
		PlayerId = playerId;
	}
}
