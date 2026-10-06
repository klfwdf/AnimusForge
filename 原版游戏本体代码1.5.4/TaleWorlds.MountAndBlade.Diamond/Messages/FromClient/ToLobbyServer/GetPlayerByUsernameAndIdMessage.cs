using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class GetPlayerByUsernameAndIdMessage : Message
{
	[JsonProperty]
	public string Username { get; private set; }

	[JsonProperty]
	public int UserId { get; private set; }

	public GetPlayerByUsernameAndIdMessage()
	{
	}

	public GetPlayerByUsernameAndIdMessage(string username, int userId)
	{
		Username = username;
		UserId = userId;
	}
}
