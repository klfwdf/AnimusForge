using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class MatchmakingQueueStats
{
	[JsonProperty]
	public List<MatchmakingQueueRegionStats> RegionStats;

	public static MatchmakingQueueStats Empty { get; private set; }

	[JsonIgnore]
	public int TotalCount
	{
		get
		{
			int num = 0;
			foreach (MatchmakingQueueRegionStats regionStat in RegionStats)
			{
				num += regionStat.TotalCount;
			}
			return num;
		}
	}

	[JsonIgnore]
	public int AverageWaitTime
	{
		get
		{
			int result = 0;
			int num = 0;
			if (RegionStats.Count > 0)
			{
				foreach (MatchmakingQueueRegionStats regionStat in RegionStats)
				{
					num += regionStat.AverageWaitTime;
				}
				result = num / RegionStats.Count;
			}
			return result;
		}
	}

	static MatchmakingQueueStats()
	{
		Empty = new MatchmakingQueueStats();
	}

	public MatchmakingQueueStats()
	{
		RegionStats = new List<MatchmakingQueueRegionStats>();
	}

	public void AddRegionStats(MatchmakingQueueRegionStats matchmakingQueueRegionStats)
	{
		RegionStats.Add(matchmakingQueueRegionStats);
	}

	public MatchmakingQueueRegionStats GetRegionStats(string region)
	{
		foreach (MatchmakingQueueRegionStats regionStat in RegionStats)
		{
			if (regionStat.Region.ToLower() == region.ToLower())
			{
				return regionStat;
			}
		}
		return null;
	}

	public int GetQueueCountOf(string region, string[] gameTypes)
	{
		int result = 0;
		if (!string.IsNullOrEmpty(region) && gameTypes != null)
		{
			MatchmakingQueueRegionStats regionStats = GetRegionStats(region);
			if (regionStats != null)
			{
				result = regionStats.GetQueueCountOf(gameTypes);
			}
		}
		return result;
	}

	public string[] GetRegionNames()
	{
		string[] array = new string[RegionStats.Count];
		for (int i = 0; i < RegionStats.Count; i++)
		{
			array[i] = RegionStats[i].Region;
		}
		return array;
	}
}
