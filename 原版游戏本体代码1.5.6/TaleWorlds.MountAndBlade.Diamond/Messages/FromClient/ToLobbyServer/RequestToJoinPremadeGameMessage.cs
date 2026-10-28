using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class RequestToJoinPremadeGameMessage : Message
{
	[JsonProperty]
	public Guid GameId { get; private set; }

	[JsonProperty]
	public string Password { get; private set; }

	public RequestToJoinPremadeGameMessage()
	{
	}

	public RequestToJoinPremadeGameMessage(Guid gameId, string password)
	{
		GameId = gameId;
		Password = password;
	}
}
