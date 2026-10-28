using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class SystemMessage : Message
{
	[JsonProperty]
	public ServerInfoMessage Message { get; private set; }

	[JsonProperty]
	public List<string> Parameters { get; private set; }

	public SystemMessage()
	{
	}

	public SystemMessage(ServerInfoMessage message, params string[] arguments)
	{
		Message = message;
		Parameters = new List<string>(arguments);
	}
}
