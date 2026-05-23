using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetOfficialServerProviderNameResult : FunctionResult
{
	[JsonProperty]
	public string Name { get; private set; }

	public GetOfficialServerProviderNameResult()
	{
	}

	public GetOfficialServerProviderNameResult(string name)
	{
		Name = name;
	}
}
