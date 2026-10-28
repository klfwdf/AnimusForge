using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class GetPlayerStatsMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	public GetPlayerStatsMessage()
	{
	}

	public GetPlayerStatsMessage(PlayerId playerId)
	{
		PlayerId = playerId;
	}
}
