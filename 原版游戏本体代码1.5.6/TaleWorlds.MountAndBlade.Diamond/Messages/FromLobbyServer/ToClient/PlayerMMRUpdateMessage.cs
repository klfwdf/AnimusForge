using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Ranked;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class PlayerMMRUpdateMessage : Message
{
	[JsonProperty]
	public RankBarInfo OldInfo { get; private set; }

	[JsonProperty]
	public RankBarInfo NewInfo { get; private set; }

	public PlayerMMRUpdateMessage()
	{
	}

	public PlayerMMRUpdateMessage(RankBarInfo oldInfo, RankBarInfo newInfo)
	{
		OldInfo = oldInfo;
		NewInfo = newInfo;
	}
}
