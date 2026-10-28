using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class GetOtherPlayersStateMessage : Message
{
	[JsonProperty]
	public List<PlayerId> Players { get; private set; }

	public GetOtherPlayersStateMessage()
	{
	}

	public GetOtherPlayersStateMessage(List<PlayerId> players)
	{
		Players = players;
	}
}
