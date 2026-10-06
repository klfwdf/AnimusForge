using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class RecentPlayerStatusesMessage : Message
{
	[JsonProperty]
	public FriendInfo[] Friends { get; private set; }

	public RecentPlayerStatusesMessage()
	{
	}

	public RecentPlayerStatusesMessage(FriendInfo[] friends)
	{
		Friends = friends;
	}
}
