using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class InviteToClanMessage : Message
{
	[JsonProperty]
	public PlayerId InvitedPlayerId { get; private set; }

	[JsonProperty]
	public bool DontUseNameForUnknownPlayer { get; private set; }

	public InviteToClanMessage()
	{
	}

	public InviteToClanMessage(PlayerId invitedPlayerId, bool dontUseNameForUnknownPlayer)
	{
		InvitedPlayerId = invitedPlayerId;
		DontUseNameForUnknownPlayer = dontUseNameForUnknownPlayer;
	}
}
