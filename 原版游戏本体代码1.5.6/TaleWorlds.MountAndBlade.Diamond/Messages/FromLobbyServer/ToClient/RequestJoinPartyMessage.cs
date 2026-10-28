using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class RequestJoinPartyMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	[JsonProperty]
	public string PlayerName { get; private set; }

	[JsonProperty]
	public PlayerId ViaPlayerId { get; private set; }

	[JsonProperty]
	public string ViaPlayerName { get; private set; }

	public RequestJoinPartyMessage()
	{
	}

	public RequestJoinPartyMessage(PlayerId playerId, string playerName, PlayerId viaPlayerId, string viaPlayerName)
	{
		PlayerId = playerId;
		PlayerName = playerName;
		ViaPlayerId = viaPlayerId;
		ViaPlayerName = viaPlayerName;
	}
}
