using System;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class RejoinBattleRequestAnswerMessage : Message
{
	public bool IsRejoinAccepted { get; set; }

	public bool IsSuccessful { get; set; }

	public RejoinBattleRequestAnswerMessage()
	{
	}

	public RejoinBattleRequestAnswerMessage(bool isRejoinAccepted, bool isSuccessful)
	{
		IsRejoinAccepted = isRejoinAccepted;
		IsSuccessful = isSuccessful;
	}
}
