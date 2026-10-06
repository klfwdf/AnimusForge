using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromBattleServerManager.ToBattleServer;

[Serializable]
[MessageDescription("BattleServerManager", "BattleServer", true)]
public class RequestMaxAllowedPriorityResponse : FunctionResult
{
	[JsonProperty]
	public sbyte Priority { get; private set; }

	public RequestMaxAllowedPriorityResponse()
	{
	}

	public RequestMaxAllowedPriorityResponse(sbyte priority)
	{
		Priority = priority;
	}
}
