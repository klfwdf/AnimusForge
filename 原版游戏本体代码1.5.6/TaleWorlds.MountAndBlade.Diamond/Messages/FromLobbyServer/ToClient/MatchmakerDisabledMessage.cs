using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class MatchmakerDisabledMessage : Message
{
	[JsonProperty]
	public int RemainingTime { get; private set; }

	public MatchmakerDisabledMessage()
	{
	}

	public MatchmakerDisabledMessage(int remainingTime)
	{
		RemainingTime = remainingTime;
	}
}
