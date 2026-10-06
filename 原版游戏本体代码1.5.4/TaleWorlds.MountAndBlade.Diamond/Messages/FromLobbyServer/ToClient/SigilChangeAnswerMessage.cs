using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class SigilChangeAnswerMessage : Message
{
	[JsonProperty]
	public bool Successful { get; private set; }

	public SigilChangeAnswerMessage()
	{
	}

	public SigilChangeAnswerMessage(bool answer)
	{
		Successful = answer;
	}
}
