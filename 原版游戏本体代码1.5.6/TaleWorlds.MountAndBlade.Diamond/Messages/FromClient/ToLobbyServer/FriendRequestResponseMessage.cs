using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class FriendRequestResponseMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	[JsonProperty]
	public bool DontUseNameForUnknownPlayer { get; private set; }

	[JsonProperty]
	public bool IsAccepted { get; private set; }

	[JsonProperty]
	public bool IsBlocked { get; private set; }

	public FriendRequestResponseMessage()
	{
	}

	public FriendRequestResponseMessage(PlayerId playerId, bool dontUseNameForUnknownPlayer, bool isAccepted, bool isBlocked)
	{
		PlayerId = playerId;
		DontUseNameForUnknownPlayer = dontUseNameForUnknownPlayer;
		IsAccepted = isAccepted;
		IsBlocked = isBlocked;
	}
}
