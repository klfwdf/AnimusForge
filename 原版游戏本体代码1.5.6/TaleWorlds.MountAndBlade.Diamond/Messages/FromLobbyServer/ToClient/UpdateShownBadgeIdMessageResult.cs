using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class UpdateShownBadgeIdMessageResult : FunctionResult
{
	[JsonProperty]
	public bool Successful { get; private set; }

	public UpdateShownBadgeIdMessageResult()
	{
	}

	public UpdateShownBadgeIdMessageResult(bool successful)
	{
		Successful = successful;
	}
}
