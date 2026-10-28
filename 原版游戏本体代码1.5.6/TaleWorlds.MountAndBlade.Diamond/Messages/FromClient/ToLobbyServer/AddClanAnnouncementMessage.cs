using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class AddClanAnnouncementMessage : Message
{
	[JsonProperty]
	public string Announcement { get; private set; }

	public AddClanAnnouncementMessage()
	{
	}

	public AddClanAnnouncementMessage(string announcement)
	{
		Announcement = announcement;
	}
}
