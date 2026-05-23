using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class UpdateShownBadgeIdMessage : Message
{
	[JsonProperty]
	public string ShownBadgeId { get; private set; }

	public UpdateShownBadgeIdMessage()
	{
	}

	public UpdateShownBadgeIdMessage(string shownBadgeId)
	{
		ShownBadgeId = shownBadgeId;
	}
}
