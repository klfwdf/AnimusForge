using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class JoinPremadeGameRequestResultMessage : Message
{
	[JsonProperty]
	public bool Successful { get; private set; }

	public JoinPremadeGameRequestResultMessage()
	{
	}

	public JoinPremadeGameRequestResultMessage(bool successful)
	{
		Successful = successful;
	}
}
