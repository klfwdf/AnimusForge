using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class AcceptPartyJoinRequestMessage : Message
{
	[JsonProperty]
	public PlayerId RequesterPlayerId { get; private set; }

	public AcceptPartyJoinRequestMessage()
	{
	}

	public AcceptPartyJoinRequestMessage(PlayerId requesterPlayerId)
	{
		RequesterPlayerId = requesterPlayerId;
	}
}
