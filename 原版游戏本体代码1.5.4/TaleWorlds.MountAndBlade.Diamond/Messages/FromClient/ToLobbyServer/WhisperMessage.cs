using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class WhisperMessage : Message
{
	[JsonProperty]
	public string TargetPlayerName { get; private set; }

	[JsonProperty]
	public string Message { get; private set; }

	public WhisperMessage()
	{
	}

	public WhisperMessage(string targetPlayerName, string message)
	{
		TargetPlayerName = targetPlayerName;
		Message = message;
	}
}
