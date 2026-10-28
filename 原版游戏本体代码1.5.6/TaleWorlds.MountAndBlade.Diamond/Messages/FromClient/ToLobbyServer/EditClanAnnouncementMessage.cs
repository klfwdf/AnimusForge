using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class EditClanAnnouncementMessage : Message
{
	[JsonProperty]
	public int AnnouncementId { get; private set; }

	[JsonProperty]
	public string Text { get; private set; }

	public EditClanAnnouncementMessage()
	{
	}

	public EditClanAnnouncementMessage(int announcementId, string text)
	{
		AnnouncementId = announcementId;
		Text = text;
	}
}
