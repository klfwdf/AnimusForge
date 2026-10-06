using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class RegisterCustomGameResult : FunctionResult
{
	[JsonProperty]
	public bool Success { get; private set; }

	public RegisterCustomGameResult()
	{
	}

	public RegisterCustomGameResult(bool success)
	{
		Success = success;
	}
}
