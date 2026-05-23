using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class GetBannerlordIDMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	public GetBannerlordIDMessage()
	{
	}

	public GetBannerlordIDMessage(PlayerId playerId)
	{
		PlayerId = playerId;
	}
}
