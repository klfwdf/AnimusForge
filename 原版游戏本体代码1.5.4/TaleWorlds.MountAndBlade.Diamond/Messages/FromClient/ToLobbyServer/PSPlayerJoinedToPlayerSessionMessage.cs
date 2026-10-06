using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class PSPlayerJoinedToPlayerSessionMessage : Message
{
	[JsonProperty]
	public ulong InviterPlayerAccountId { get; private set; }

	public PSPlayerJoinedToPlayerSessionMessage()
	{
	}

	public PSPlayerJoinedToPlayerSessionMessage(ulong inviterPlayerAccountId)
	{
		InviterPlayerAccountId = inviterPlayerAccountId;
	}
}
