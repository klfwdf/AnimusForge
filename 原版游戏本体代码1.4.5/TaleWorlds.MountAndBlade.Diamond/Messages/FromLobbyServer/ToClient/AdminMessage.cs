using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class AdminMessage : Message
{
	[JsonProperty]
	public string Message { get; private set; }

	public AdminMessage()
	{
	}

	public AdminMessage(string message)
	{
		Message = message;
	}
}
