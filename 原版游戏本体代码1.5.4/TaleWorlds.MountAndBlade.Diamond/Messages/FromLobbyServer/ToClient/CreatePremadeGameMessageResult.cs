using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class CreatePremadeGameMessageResult : FunctionResult
{
	[JsonProperty]
	public bool Successful { get; private set; }

	public CreatePremadeGameMessageResult()
	{
	}

	public CreatePremadeGameMessageResult(bool successful)
	{
		Successful = successful;
	}
}
