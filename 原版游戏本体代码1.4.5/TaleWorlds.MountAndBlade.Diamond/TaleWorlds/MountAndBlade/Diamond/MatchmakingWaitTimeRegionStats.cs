using System;
using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class MatchmakingWaitTimeRegionStats
{
	private Dictionary<string, Dictionary<WaitTimeStatType, int>> _gameTypeAverageWaitTimes;

	public string Region { get; private set; }

	public MatchmakingWaitTimeRegionStats(string region)
	{
		Region = region;
		_gameTypeAverageWaitTimes = new Dictionary<string, Dictionary<WaitTimeStatType, int>>();
	}

	public void SetGameTypeAverage(string gameType, WaitTimeStatType statType, int average)
	{
		if (!_gameTypeAverageWaitTimes.TryGetValue(gameType, out var value))
		{
			value = new Dictionary<WaitTimeStatType, int>();
			_gameTypeAverageWaitTimes.Add(gameType, value);
		}
		_gameTypeAverageWaitTimes[gameType][statType] = average;
	}

	public bool HasStatsForGameType(string gameType)
	{
		if (gameType != null)
		{
			return _gameTypeAverageWaitTimes.ContainsKey(gameType);
		}
		return false;
	}

	public int GetWaitTime(string gameType, WaitTimeStatType statType)
	{
		if (_gameTypeAverageWaitTimes.TryGetValue(gameType, out var value) && value.TryGetValue(statType, out var value2))
		{
			return value2;
		}
		return int.MaxValue;
	}
}
