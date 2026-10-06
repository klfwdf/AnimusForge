using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class WhisperReceivedMessage : Message
{
	[JsonProperty]
	public string FromPlayer { get; private set; }

	[JsonProperty]
	public string ToPlayer { get; private set; }

	[JsonProperty]
	public string Message { get; private set; }

	public WhisperReceivedMessage()
	{
	}

	public WhisperReceivedMessage(string fromPlayer, string toPlayer, string message)
	{
		FromPlayer = fromPlayer;
		ToPlayer = toPlayer;
		Message = message;
	}
}
