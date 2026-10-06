using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class RequestJoinPlayerPartyMessageResult : FunctionResult
{
	[JsonProperty]
	public bool Success { get; private set; }

	public RequestJoinPlayerPartyMessageResult()
	{
	}

	public RequestJoinPlayerPartyMessageResult(bool success)
	{
		Success = success;
	}
}
