using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class PlayerRemovedFromPartyMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	[JsonProperty]
	public PartyRemoveReason Reason { get; private set; }

	public PlayerRemovedFromPartyMessage()
	{
	}

	public PlayerRemovedFromPartyMessage(PlayerId playerId, PartyRemoveReason reason)
	{
		PlayerId = playerId;
		Reason = reason;
	}
}
