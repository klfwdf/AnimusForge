using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class PartyMessage : Message
{
	[JsonProperty]
	public string Message { get; private set; }

	public PartyMessage()
	{
	}

	public PartyMessage(string message)
	{
		Message = message;
	}
}
