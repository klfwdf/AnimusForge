using System;
using System.Linq;
using Newtonsoft.Json;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class MatchmakingQueueGameTypeStats
{
	[JsonProperty]
	public string[] GameTypes { get; set; }

	[JsonProperty]
	public int Count { get; set; }

	[JsonProperty]
	public int TotalWaitTime { get; set; }

	public MatchmakingQueueGameTypeStats()
	{
	}

	public MatchmakingQueueGameTypeStats(string[] gameTypes)
	{
		GameTypes = gameTypes;
	}

	public bool HasGameType(string gameType)
	{
		return GameTypes.Contains(gameType);
	}

	public bool EqualWith(string[] gameTypes)
	{
		if (GameTypes.Length == gameTypes.Length)
		{
			foreach (string gameType in gameTypes)
			{
				if (!HasGameType(gameType))
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	internal bool HasAnyGameType(string[] gameTypes)
	{
		foreach (string gameType in gameTypes)
		{
			if (HasGameType(gameType))
			{
				return true;
			}
		}
		return false;
	}
}
