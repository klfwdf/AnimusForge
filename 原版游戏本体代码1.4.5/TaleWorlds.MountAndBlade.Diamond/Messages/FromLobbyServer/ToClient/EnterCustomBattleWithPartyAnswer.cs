using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class EnterCustomBattleWithPartyAnswer : Message
{
	[JsonProperty]
	public bool Successful { get; private set; }

	public EnterCustomBattleWithPartyAnswer()
	{
	}

	public EnterCustomBattleWithPartyAnswer(bool successful)
	{
		Successful = successful;
	}
}
