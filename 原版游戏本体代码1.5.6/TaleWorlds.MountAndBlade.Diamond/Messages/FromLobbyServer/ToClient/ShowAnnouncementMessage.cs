using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class ShowAnnouncementMessage : Message
{
	[JsonProperty]
	public Announcement Announcement { get; private set; }

	public ShowAnnouncementMessage()
	{
	}

	public ShowAnnouncementMessage(Announcement announcement)
	{
		Announcement = announcement;
	}
}
