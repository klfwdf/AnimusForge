using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class BadgeDataEntry
{
	[JsonProperty]
	public PlayerId PlayerId { get; set; }

	[JsonProperty]
	public string BadgeId { get; set; }

	[JsonProperty]
	public string ConditionId { get; set; }

	[JsonProperty]
	public int Count { get; set; }

	public static Dictionary<(PlayerId, string, string), int> ToDictionary(List<BadgeDataEntry> entries)
	{
		Dictionary<(PlayerId, string, string), int> dictionary = new Dictionary<(PlayerId, string, string), int>();
		if (entries != null)
		{
			foreach (BadgeDataEntry entry in entries)
			{
				dictionary.Add((entry.PlayerId, entry.BadgeId, entry.ConditionId), entry.Count);
			}
		}
		return dictionary;
	}

	public static List<BadgeDataEntry> ToList(Dictionary<(PlayerId, string, string), int> dictionary)
	{
		List<BadgeDataEntry> list = new List<BadgeDataEntry>();
		if (dictionary != null)
		{
			foreach (KeyValuePair<(PlayerId, string, string), int> item in dictionary)
			{
				BadgeDataEntry badgeDataEntry = new BadgeDataEntry();
				badgeDataEntry.PlayerId = item.Key.Item1;
				badgeDataEntry.BadgeId = item.Key.Item2;
				badgeDataEntry.ConditionId = item.Key.Item3;
				badgeDataEntry.Count = item.Value;
				list.Add(badgeDataEntry);
			}
		}
		return list;
	}
}
