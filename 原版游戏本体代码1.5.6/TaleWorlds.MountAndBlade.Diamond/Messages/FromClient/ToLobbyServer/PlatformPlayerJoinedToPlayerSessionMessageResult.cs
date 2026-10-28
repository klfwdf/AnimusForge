using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
public class PlatformPlayerJoinedToPlayerSessionMessageResult : FunctionResult
{
	[JsonProperty]
	public bool Successful { get; private set; }

	public PlatformPlayerJoinedToPlayerSessionMessageResult()
	{
	}

	public PlatformPlayerJoinedToPlayerSessionMessageResult(bool successful)
	{
		Successful = successful;
	}
}
