using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
public class GetPlayerStatsMessageResult : FunctionResult
{
	[JsonProperty]
	public PlayerStatsBase[] PlayerStats { get; private set; }

	public GetPlayerStatsMessageResult()
	{
	}

	public GetPlayerStatsMessageResult(PlayerStatsBase[] playerStats)
	{
		PlayerStats = playerStats;
	}
}
