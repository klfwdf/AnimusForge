using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class ClanMessage : Message
{
	[JsonProperty]
	public string Message { get; private set; }

	public ClanMessage()
	{
	}

	public ClanMessage(string message)
	{
		Message = message;
	}
}
