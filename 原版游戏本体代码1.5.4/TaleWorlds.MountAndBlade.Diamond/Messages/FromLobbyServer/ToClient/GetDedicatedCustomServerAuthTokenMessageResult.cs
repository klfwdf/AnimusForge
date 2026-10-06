using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetDedicatedCustomServerAuthTokenMessageResult : FunctionResult
{
	[JsonProperty]
	public string AuthToken { get; private set; }

	public GetDedicatedCustomServerAuthTokenMessageResult()
	{
	}

	public GetDedicatedCustomServerAuthTokenMessageResult(string authToken)
	{
		AuthToken = authToken;
	}
}
