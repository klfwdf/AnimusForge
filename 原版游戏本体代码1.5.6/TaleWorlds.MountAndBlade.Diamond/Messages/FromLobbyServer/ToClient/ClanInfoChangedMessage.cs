using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class ClanInfoChangedMessage : Message
{
	[JsonProperty]
	public ClanHomeInfo ClanHomeInfo { get; private set; }

	public ClanInfoChangedMessage()
	{
	}

	public ClanInfoChangedMessage(ClanHomeInfo clanHomeInfo)
	{
		ClanHomeInfo = clanHomeInfo;
	}
}
