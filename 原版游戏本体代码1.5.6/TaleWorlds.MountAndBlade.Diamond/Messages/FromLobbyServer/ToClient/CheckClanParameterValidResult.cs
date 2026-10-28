using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class CheckClanParameterValidResult : FunctionResult
{
	[JsonProperty]
	public bool IsValid { get; private set; }

	[JsonProperty]
	public StringValidationError Error { get; private set; }

	public CheckClanParameterValidResult()
	{
	}

	public CheckClanParameterValidResult(bool isValid, StringValidationError error)
	{
		IsValid = isValid;
		Error = error;
	}
}
