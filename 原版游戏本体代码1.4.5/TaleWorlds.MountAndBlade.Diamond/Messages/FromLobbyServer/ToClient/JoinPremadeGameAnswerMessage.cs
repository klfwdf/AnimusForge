using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class JoinPremadeGameAnswerMessage : Message
{
	[JsonProperty]
	public bool Successful { get; private set; }

	public JoinPremadeGameAnswerMessage()
	{
	}

	public JoinPremadeGameAnswerMessage(bool successful)
	{
		Successful = successful;
	}
}
