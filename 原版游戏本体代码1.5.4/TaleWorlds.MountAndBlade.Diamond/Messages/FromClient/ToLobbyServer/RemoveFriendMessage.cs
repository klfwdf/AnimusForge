using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class RemoveFriendMessage : Message
{
	[JsonProperty]
	public PlayerId FriendId { get; private set; }

	public RemoveFriendMessage()
	{
	}

	public RemoveFriendMessage(PlayerId friendId)
	{
		FriendId = friendId;
	}
}
