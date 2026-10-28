using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class UpdateUsedCosmeticItemsMessageResult : FunctionResult
{
	[JsonProperty]
	public bool Successful { get; private set; }

	public UpdateUsedCosmeticItemsMessageResult()
	{
	}

	public UpdateUsedCosmeticItemsMessageResult(bool successful)
	{
		Successful = successful;
	}
}
