using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
public class PSPlayerJoinedToPlayerSessionMessageResult : FunctionResult
{
	[JsonProperty]
	public bool Successful { get; private set; }

	public PSPlayerJoinedToPlayerSessionMessageResult()
	{
	}

	public PSPlayerJoinedToPlayerSessionMessageResult(bool successful)
	{
		Successful = successful;
	}
}
