using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class GetAnotherPlayerDataMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	public GetAnotherPlayerDataMessage()
	{
	}

	public GetAnotherPlayerDataMessage(PlayerId playerId)
	{
		PlayerId = playerId;
	}
}
