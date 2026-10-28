using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToLobbyServer;

[Serializable]
[MessageDescription("LobbyServer", "LobbyServer", true)]
public class RefreshClanInfoMessage : Message
{
	[JsonProperty]
	public Guid ClanId { get; private set; }

	public RefreshClanInfoMessage()
	{
	}

	public RefreshClanInfoMessage(Guid clanId)
	{
		ClanId = clanId;
	}
}
