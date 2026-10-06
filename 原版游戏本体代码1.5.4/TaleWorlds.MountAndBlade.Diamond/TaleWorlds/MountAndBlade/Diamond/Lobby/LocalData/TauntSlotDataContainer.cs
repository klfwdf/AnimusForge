using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Diamond.Lobby.LocalData;

public class TauntSlotDataContainer : MultiplayerLocalDataContainer<TauntSlotData>
{
	protected override string GetSaveDirectoryName()
	{
		return "Data";
	}

	protected override string GetSaveFileName()
	{
		return "TauntSlots.json";
	}

	protected override PlatformFilePath GetCompatibilityFilePath()
	{
		return new PlatformFilePath(new PlatformDirectoryPath(PlatformFileType.User, "Data"), "Taunts.json");
	}

	protected override List<TauntSlotData> DeserializeInCompatibilityMode(string serializedJson)
	{
		List<TauntSlotData> list = new List<TauntSlotData>();
		try
		{
			Dictionary<string, List<(string, int)>> dictionary = JsonConvert.DeserializeObject<Dictionary<string, List<(string, int)>>>(serializedJson);
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, List<(string, int)>> item in dictionary)
				{
					string key = item.Key;
					List<TauntIndexData> list2 = new List<TauntIndexData>();
					if (item.Value != null)
					{
						foreach (var item2 in item.Value)
						{
							if (string.IsNullOrEmpty(item2.Item1))
							{
								Debug.FailedAssert("Taunt id is null when trying to load in compatibility mode", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Lobby\\LocalData\\TauntSlotDataContainer.cs", "DeserializeInCompatibilityMode", 120);
								continue;
							}
							for (int i = 0; i < list2.Count; i++)
							{
								if (list2[i].TauntIndex == item2.Item2)
								{
									Debug.FailedAssert("Taunt index used for multiple taunts", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Lobby\\LocalData\\TauntSlotDataContainer.cs", "DeserializeInCompatibilityMode", 128);
								}
							}
							list2.Add(new TauntIndexData(item2.Item1, item2.Item2));
						}
					}
					TauntSlotData tauntSlotData = new TauntSlotData(key);
					tauntSlotData.TauntIndices = list2;
					list.Add(tauntSlotData);
				}
			}
		}
		catch
		{
			Debug.FailedAssert("Failed to resolve taunt slot data in compatibility mode. Resetting local data.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Diamond\\Lobby\\LocalData\\TauntSlotDataContainer.cs", "DeserializeInCompatibilityMode", 145);
		}
		return list;
	}

	public MBReadOnlyList<TauntIndexData> GetTauntIndicesForPlayer(string playerId)
	{
		MBReadOnlyList<TauntSlotData> entries = GetEntries();
		for (int i = 0; i < entries.Count; i++)
		{
			if (entries[i].PlayerId == playerId)
			{
				return new MBReadOnlyList<TauntIndexData>(entries[i].TauntIndices);
			}
		}
		return null;
	}

	public void SetTauntIndicesForPlayer(string playerId, List<TauntIndexData> tauntIndices)
	{
		MBReadOnlyList<TauntSlotData> entries = GetEntries();
		TauntSlotData tauntSlotData = null;
		int index = -1;
		for (int i = 0; i < entries.Count; i++)
		{
			TauntSlotData tauntSlotData2 = entries[i];
			if (tauntSlotData2.PlayerId == playerId)
			{
				tauntSlotData = tauntSlotData2;
				index = i;
				break;
			}
		}
		TauntSlotData tauntSlotData3 = new TauntSlotData(playerId);
		tauntSlotData3.TauntIndices = tauntIndices.ToList();
		if (tauntSlotData != null)
		{
			RemoveEntry(tauntSlotData);
			InsertEntry(tauntSlotData3, index);
		}
		else
		{
			AddEntry(tauntSlotData3);
		}
	}
}
