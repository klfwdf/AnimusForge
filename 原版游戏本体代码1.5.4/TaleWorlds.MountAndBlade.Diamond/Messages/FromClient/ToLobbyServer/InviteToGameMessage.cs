using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class InviteToGameMessage : Message
{
	[JsonProperty]
	public PlayerId InvitedPlayerId { get; private set; }

	public InviteToGameMessage()
	{
	}

	public InviteToGameMessage(PlayerId invitedPlayerId)
	{
		InvitedPlayerId = invitedPlayerId;
	}
}
