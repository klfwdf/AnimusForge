using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class RejoinBattleRequestMessage : Message
{
	[JsonProperty]
	public bool IsRejoinAccepted { get; private set; }

	public RejoinBattleRequestMessage()
	{
	}

	public RejoinBattleRequestMessage(bool isRejoinAccepted)
	{
		IsRejoinAccepted = isRejoinAccepted;
	}
}
