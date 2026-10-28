using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class BuyCosmeticMessageResult : FunctionResult
{
	[JsonProperty]
	public bool Successful { get; private set; }

	[JsonProperty]
	public int Gold { get; private set; }

	public BuyCosmeticMessageResult()
	{
	}

	public BuyCosmeticMessageResult(bool successful, int gold)
	{
		Successful = successful;
		Gold = gold;
	}
}
