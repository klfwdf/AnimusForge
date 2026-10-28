using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class ClientQuitFromCustomGameMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	public ClientQuitFromCustomGameMessage()
	{
	}

	public ClientQuitFromCustomGameMessage(PlayerId playerId)
	{
		PlayerId = playerId;
	}
}
