using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class GetPlayerClanInfo : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	public GetPlayerClanInfo()
	{
	}

	public GetPlayerClanInfo(PlayerId playerId)
	{
		PlayerId = playerId;
	}
}
