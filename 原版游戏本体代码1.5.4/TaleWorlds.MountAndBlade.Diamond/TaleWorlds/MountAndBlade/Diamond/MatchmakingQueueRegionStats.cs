using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class MatchmakingQueueRegionStats
{
	[JsonProperty]
	public List<MatchmakingQueueGameTypeStats> GameTypeStats;

	[JsonProperty]
	public string Region { get; set; }

	[JsonIgnore]
	public int TotalCount
	{
		get
		{
			int num = 0;
			foreach (MatchmakingQueueGameTypeStats gameTypeStat in GameTypeStats)
			{
				num += gameTypeStat.Count;
			}
			return num;
		}
	}

	[JsonProperty]
	public int MaxWaitTime { get; set; }

	[JsonProperty]
	public int MinWaitTime { get; set; }

	[JsonProperty]
	public int MedianWaitTime { get; set; }

	[JsonProperty]
	public int AverageWaitTime { get; set; }

	public MatchmakingQueueRegionStats(string region)
	{
		Region = region;
		GameTypeStats = new List<MatchmakingQueueGameTypeStats>();
	}

	public MatchmakingQueueGameTypeStats GetQueueCountObjectOf(string[] gameTypes)
	{
		if (gameTypes != null)
		{
			foreach (MatchmakingQueueGameTypeStats gameTypeStat in GameTypeStats)
			{
				if (gameTypeStat.EqualWith(gameTypes))
				{
					return gameTypeStat;
				}
			}
		}
		return null;
	}

	public void AddStats(MatchmakingQueueGameTypeStats matchmakingQueueGameTypeStats)
	{
		GameTypeStats.Add(matchmakingQueueGameTypeStats);
	}

	public int GetQueueCountOf(string[] gameTypes)
	{
		int num = 0;
		if (gameTypes != null)
		{
			foreach (MatchmakingQueueGameTypeStats gameTypeStat in GameTypeStats)
			{
				if (gameTypeStat.HasAnyGameType(gameTypes))
				{
					num += gameTypeStat.Count;
				}
			}
		}
		return num;
	}

	public void SetWaitTimeStats(int averageWaitTime, int maxWaitTime, int minWaitTime, int medianWaitTime)
	{
		AverageWaitTime = averageWaitTime;
		MaxWaitTime = maxWaitTime;
		MinWaitTime = minWaitTime;
		MedianWaitTime = medianWaitTime;
	}
}
