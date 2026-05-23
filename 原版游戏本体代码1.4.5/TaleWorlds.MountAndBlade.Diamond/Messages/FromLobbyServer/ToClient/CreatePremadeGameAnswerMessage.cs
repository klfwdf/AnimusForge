using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class CreatePremadeGameAnswerMessage : Message
{
	[JsonProperty]
	public bool Successful { get; private set; }

	public CreatePremadeGameAnswerMessage()
	{
	}

	public CreatePremadeGameAnswerMessage(bool successful)
	{
		Successful = successful;
	}
}
