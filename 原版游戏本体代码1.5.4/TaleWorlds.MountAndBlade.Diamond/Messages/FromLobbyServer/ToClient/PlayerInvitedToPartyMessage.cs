using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class PlayerInvitedToPartyMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	[JsonProperty]
	public string PlayerName { get; private set; }

	public PlayerInvitedToPartyMessage()
	{
	}

	public PlayerInvitedToPartyMessage(PlayerId playerId, string playerName)
	{
		PlayerId = playerId;
		PlayerName = playerName;
	}
}
