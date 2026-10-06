using System;
using System.Collections.Generic;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class MultipleBattleResult
{
	private int _currentBattleIndex;

	public List<BattleResult> BattleResults { get; set; }

	public MultipleBattleResult()
	{
		BattleResults = new List<BattleResult>();
		_currentBattleIndex = -1;
	}

	public void CreateNewBattleResult(string gameType)
	{
		BattleResult battleResult = new BattleResult();
		BattleResults.Add(battleResult);
		_currentBattleIndex++;
		if (_currentBattleIndex <= 0)
		{
			return;
		}
		foreach (KeyValuePair<string, BattlePlayerEntry> playerEntry in BattleResults[_currentBattleIndex - 1].PlayerEntries)
		{
			battleResult.AddOrUpdatePlayerEntry(PlayerId.FromString(playerEntry.Key), playerEntry.Value.TeamNo, gameType, Guid.Empty);
		}
	}

	public BattleResult GetCurrentBattleResult()
	{
		return BattleResults[_currentBattleIndex];
	}
}
