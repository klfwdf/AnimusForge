using System;
using System.Collections.Generic;

namespace AnimusForge;

// Keyed record authority. Resolving a live Hero or a scene target belongs to the caller.
internal sealed class ShownResourceRecordOwner
{
 internal Dictionary<string, MyBehavior.HeroShownRecord> Records = new Dictionary<string, MyBehavior.HeroShownRecord>();
 internal static string NormalizeKey(string key) => (key ?? "").Trim().ToLowerInvariant();
	internal MyBehavior.HeroShownRecord Get(string key, bool createIfMissing)
	{
		string text = NormalizeKey(key);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		if (!Records.TryGetValue(text, out var value))
		{
			if (!createIfMissing)
			{
				return null;
			}
			value = new MyBehavior.HeroShownRecord();
			Records[text] = value;
		}
		if (value.ShownItems == null)
		{
			value.ShownItems = new Dictionary<string, int>();
		}
		return value;
	}

	internal void Record(string key, int shownGold, Dictionary<string, int> shownItems)
	{
		if (shownGold <= 0 && (shownItems == null || shownItems.Count == 0))
		{
			return;
		}
		MyBehavior.HeroShownRecord shownRecord = Get(key, createIfMissing: true);
		if (shownRecord == null)
		{
			return;
		}
		if (shownGold > 0)
		{
			shownRecord.ShownGold += shownGold;
		}
		if (shownItems == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> shownItem in shownItems)
		{
			string text = (shownItem.Key ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || shownItem.Value <= 0)
			{
				continue;
			}
			if (!shownRecord.ShownItems.ContainsKey(text))
			{
				shownRecord.ShownItems[text] = 0;
			}
			shownRecord.ShownItems[text] += shownItem.Value;
		}
	}

	internal int RemainingGold(string key, int currentGold)
	{
		MyBehavior.HeroShownRecord shownRecord = Get(key, createIfMissing: false);
		if (shownRecord == null)
		{
			return Math.Max(0, currentGold);
		}
		return Math.Max(0, currentGold - Math.Max(0, shownRecord.ShownGold));
	}

	internal int RemainingItemCount(string key, string itemId, int currentAmount)
	{
		if (string.IsNullOrWhiteSpace(itemId))
		{
			return Math.Max(0, currentAmount);
		}
		MyBehavior.HeroShownRecord shownRecord = Get(key, createIfMissing: false);
		if (shownRecord == null || shownRecord.ShownItems == null || !shownRecord.ShownItems.TryGetValue(itemId, out var value))
		{
			return Math.Max(0, currentAmount);
		}
		return Math.Max(0, currentAmount - Math.Max(0, value));
	}

internal void ResetForCurrentSave() { Records = new Dictionary<string,MyBehavior.HeroShownRecord>(); }
}
