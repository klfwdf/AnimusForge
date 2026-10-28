using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class CancelBattleResponseMessage : Message
{
	[JsonProperty]
	public bool Successful { get; private set; }

	public CancelBattleResponseMessage()
	{
	}

	public CancelBattleResponseMessage(bool successful)
	{
		Successful = successful;
	}
}
