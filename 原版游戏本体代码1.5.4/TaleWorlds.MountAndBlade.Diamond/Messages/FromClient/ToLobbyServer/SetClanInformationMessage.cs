using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class SetClanInformationMessage : Message
{
	[JsonProperty]
	public string Information { get; private set; }

	public SetClanInformationMessage()
	{
	}

	public SetClanInformationMessage(string information)
	{
		Information = information;
	}
}
