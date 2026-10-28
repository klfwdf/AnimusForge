using System;
using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class MatchmakingWaitTimeStats
{
	private List<MatchmakingWaitTimeRegionStats> _regionStats;

	public static MatchmakingWaitTimeStats Empty { get; private set; }

	static MatchmakingWaitTimeStats()
	{
		Empty = new MatchmakingWaitTimeStats();
	}

	public MatchmakingWaitTimeStats()
	{
		_regionStats = new List<MatchmakingWaitTimeRegionStats>();
	}

	public void AddRegionStats(MatchmakingWaitTimeRegionStats regionStats)
	{
		_regionStats.Add(regionStats);
	}

	public MatchmakingWaitTimeRegionStats GetRegionStats(string region)
	{
		foreach (MatchmakingWaitTimeRegionStats regionStat in _regionStats)
		{
			if (regionStat.Region.ToLower() == region.ToLower())
			{
				return regionStat;
			}
		}
		return null;
	}

	public int GetWaitTime(string region, string gameType, WaitTimeStatType statType)
	{
		int result = 0;
		if (!string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(gameType))
		{
			MatchmakingWaitTimeRegionStats regionStats = GetRegionStats(region);
			if (regionStats != null)
			{
				result = regionStats.GetWaitTime(gameType, statType);
			}
		}
		return result;
	}
}
