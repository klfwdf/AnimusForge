using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetRankedLeaderboardMessageResult : FunctionResult
{
	[JsonProperty]
	public PlayerLeaderboardData[] LeaderboardPlayers { get; private set; }

	public GetRankedLeaderboardMessageResult()
	{
	}

	public GetRankedLeaderboardMessageResult(PlayerLeaderboardData[] leaderboardPlayers)
	{
		LeaderboardPlayers = leaderboardPlayers;
	}
}
