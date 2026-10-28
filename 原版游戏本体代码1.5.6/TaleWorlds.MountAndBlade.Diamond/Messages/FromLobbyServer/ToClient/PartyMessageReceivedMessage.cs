using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class PartyMessageReceivedMessage : Message
{
	[JsonProperty]
	public string PlayerName { get; private set; }

	[JsonProperty]
	public string Message { get; private set; }

	public PartyMessageReceivedMessage()
	{
	}

	public PartyMessageReceivedMessage(string playerName, string message)
	{
		PlayerName = playerName;
		Message = message;
	}
}
