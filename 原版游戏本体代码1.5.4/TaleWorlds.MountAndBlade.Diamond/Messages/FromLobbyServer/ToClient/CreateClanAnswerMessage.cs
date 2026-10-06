using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class CreateClanAnswerMessage : Message
{
	[JsonProperty]
	public bool Successful { get; private set; }

	public CreateClanAnswerMessage()
	{
	}

	public CreateClanAnswerMessage(bool successful)
	{
		Successful = successful;
	}
}
