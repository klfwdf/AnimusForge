using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;

public class MatchHistoryDataContainer : MultiplayerLocalDataContainer<MatchHistoryData>
{
	private const int MaxMatchCountPerMatchType = 10;

	private List<MatchHistoryData> _matchesToRemove;

	public MatchHistoryDataContainer()
	{
		_matchesToRemove = new List<MatchHistoryData>();
	}

	protected override string GetSaveDirectoryName()
	{
		return "Data";
	}

	protected override string GetSaveFileName()
	{
		return "History.json";
	}

	protected override void OnBeforeRemoveEntry(MatchHistoryData item, out bool canRemoveEntry)
	{
		base.OnBeforeRemoveEntry(item, out canRemoveEntry);
		_matchesToRemove.Remove(item);
	}

	protected override void OnBeforeAddEntry(MatchHistoryData item, out bool canAddEntry)
	{
		base.OnBeforeAddEntry(item, out var _);
		MBReadOnlyList<MatchHistoryData> entries = GetEntries();
		bool flag = false;
		for (int i = 0; i < entries.Count; i++)
		{
			if (entries[i].MatchId == item.MatchId)
			{
				PrintDebugLog("Found existing match with id trying to replace: " + entries[i].MatchId);
				RemoveEntry(entries[i]);
				_matchesToRemove.Add(entries[i]);
				InsertEntry(item, i);
				PrintDebugLog("Replaced existing match: (" + entries[i].MatchId + ") with: " + item.MatchId);
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			int num = GetEntryCountOfMatchType(item.MatchType) + 1 - 10;
			if (num > 0)
			{
				PrintDebugLog($"Max match count is reached, removing ({num}) matches with type: {item.MatchType}");
				List<MatchHistoryData> oldestMatches = GetOldestMatches(item.MatchType, num);
				for (int j = 0; j < oldestMatches.Count; j++)
				{
					if (!_matchesToRemove.Contains(oldestMatches[j]))
					{
						RemoveEntry(oldestMatches[j]);
						_matchesToRemove.Add(oldestMatches[j]);
					}
				}
			}
		}
		canAddEntry = !flag;
	}

	protected override List<MatchHistoryData> DeserializeInCompatibilityMode(string serializedJson)
	{
		List<MatchHistoryData> list = new List<MatchHistoryData>();
		try
		{
			MBList<MatchHistoryData> mBList = JsonConvert.DeserializeObject<MBList<MatchHistoryData>>(serializedJson);
			for (int i = 0; i < mBList.Count; i++)
			{
				list.Add(mBList[i]);
			}
		}
		catch
		{
			Debug.FailedAssert("Failed to resolve match history in compatibility mode. Resetting the file.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Lobby\\LocalData\\MatchHistoryDataContainer.cs", "DeserializeInCompatibilityMode", 228);
		}
		return list;
	}

	public bool TryGetHistoryData(string matchId, out MatchHistoryData historyData)
	{
		historyData = null;
		MBReadOnlyList<MatchHistoryData> entries = GetEntries();
		for (int i = 0; i < entries.Count; i++)
		{
			if (entries[i].MatchId == matchId)
			{
				historyData = entries[i];
				return true;
			}
		}
		return false;
	}

	private List<MatchHistoryData> GetOldestMatches(string matchType, int count = 1)
	{
		_ = DateTime.MaxValue;
		List<MatchHistoryData> list = new List<MatchHistoryData>();
		MBReadOnlyList<MatchHistoryData> entries = GetEntries();
		entries.OrderBy((MatchHistoryData e) => e.MatchDate);
		int num = 0;
		foreach (MatchHistoryData item in entries)
		{
			if (item == null)
			{
				Debug.FailedAssert("Trying to remove null match history data", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Lobby\\LocalData\\MatchHistoryDataContainer.cs", "GetOldestMatches", 267);
				continue;
			}
			if (item.MatchType == matchType)
			{
				list.Add(item);
				num++;
			}
			if (num != count)
			{
				continue;
			}
			break;
		}
		return list;
	}

	private int GetEntryCountOfMatchType(string matchType)
	{
		int num = 0;
		foreach (MatchHistoryData entry in GetEntries())
		{
			if (entry.MatchType == matchType)
			{
				num++;
			}
		}
		return num;
	}

	private static void PrintDebugLog(string text)
	{
		Debug.Print("[MATCH_HISTORY]: " + text, 0, Debug.DebugColor.Yellow);
	}
}
