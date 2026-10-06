using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class DeclinePartyJoinRequestMessage : Message
{
	[JsonProperty]
	public PlayerId RequesterPlayerId { get; private set; }

	[JsonProperty]
	public PartyJoinDeclineReason Reason { get; private set; }

	public DeclinePartyJoinRequestMessage()
	{
	}

	public DeclinePartyJoinRequestMessage(PlayerId requesterPlayerId, PartyJoinDeclineReason reason)
	{
		RequesterPlayerId = requesterPlayerId;
		Reason = reason;
	}
}
