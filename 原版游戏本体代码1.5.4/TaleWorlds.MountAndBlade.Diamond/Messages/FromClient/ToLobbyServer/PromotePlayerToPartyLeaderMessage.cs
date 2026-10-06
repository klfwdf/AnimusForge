using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class PromotePlayerToPartyLeaderMessage : Message
{
	[JsonProperty]
	public PlayerId PromotedPlayerId { get; private set; }

	public PromotePlayerToPartyLeaderMessage()
	{
	}

	public PromotePlayerToPartyLeaderMessage(PlayerId promotedPlayerId)
	{
		PromotedPlayerId = promotedPlayerId;
	}
}
