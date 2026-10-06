using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetPlayerCountInQueueResult : FunctionResult
{
	[JsonProperty]
	public MatchmakingQueueStats MatchmakingQueueStats { get; private set; }

	public GetPlayerCountInQueueResult()
	{
	}

	public GetPlayerCountInQueueResult(MatchmakingQueueStats matchmakingQueueStats)
	{
		MatchmakingQueueStats = matchmakingQueueStats;
	}
}
