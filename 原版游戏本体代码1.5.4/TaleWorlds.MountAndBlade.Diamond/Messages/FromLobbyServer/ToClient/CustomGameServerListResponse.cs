using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class CustomGameServerListResponse : FunctionResult
{
	[JsonProperty]
	public AvailableCustomGames AvailableCustomGames { get; private set; }

	public CustomGameServerListResponse()
	{
	}

	public CustomGameServerListResponse(AvailableCustomGames availableCustomGames)
	{
		AvailableCustomGames = availableCustomGames;
	}
}
