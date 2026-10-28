using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetAverageMatchmakingWaitTimesResult : FunctionResult
{
	[JsonProperty]
	public MatchmakingWaitTimeStats MatchmakingWaitTimeStats { get; private set; }

	public GetAverageMatchmakingWaitTimesResult()
	{
	}

	public GetAverageMatchmakingWaitTimesResult(MatchmakingWaitTimeStats matchmakingWaitTimeStats)
	{
		MatchmakingWaitTimeStats = matchmakingWaitTimeStats;
	}
}
