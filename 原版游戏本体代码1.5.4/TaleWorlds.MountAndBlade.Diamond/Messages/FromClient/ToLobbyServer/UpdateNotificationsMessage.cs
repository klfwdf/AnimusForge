using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class UpdateNotificationsMessage : Message
{
	[JsonProperty]
	public int[] SeenNotificationIds { get; private set; }

	public UpdateNotificationsMessage()
	{
	}

	public UpdateNotificationsMessage(int[] seenNotificationIds)
	{
		SeenNotificationIds = seenNotificationIds;
	}
}
