using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class ChangeRegionMessage : Message
{
	[JsonProperty]
	public string Region { get; private set; }

	public ChangeRegionMessage()
	{
	}

	public ChangeRegionMessage(string region)
	{
		Region = region;
	}
}
