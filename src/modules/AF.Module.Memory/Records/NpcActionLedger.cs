using System;
using System.Collections.Generic;

namespace AnimusForge;

/// <summary>
/// Pure record-keeping rules for NPC action entries (recent window and major list). The entry
/// type stays the host's persisted class; this owner sees it through the small accessor set
/// below so save identity is untouched. One call per recorded action; no game reads.
/// </summary>
internal static class NpcActionLedger
{
	internal const int RecentWindowDays = 10;
	internal const int MaxRecentEntriesPerHero = 96;
	internal const int MaxMajorEntriesPerHero = 160;

	internal static string NormalizeText(string text) => (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();

	internal static string NormalizeStableKey(string stableKey, string fallbackText)
	{
		string text = (stableKey ?? fallbackText ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		return string.IsNullOrWhiteSpace(text) ? "" : text.ToLowerInvariant();
	}

	internal static int RecentWindowMinimumDay(int currentDay) => currentDay - RecentWindowDays + 1;

	/// <summary>Drop null/blank entries and, for the recent window, entries older than <paramref name="minimumDay"/>.</summary>
	internal static bool RemoveInvalid<T>(List<T> entries, int minimumDay, bool keepOnlyRecentWindow, Func<T, string> text, Func<T, int> day) where T : class
	{
		if (entries == null || entries.Count == 0)
		{
			return false;
		}
		bool removedAny = false;
		for (int index = entries.Count - 1; index >= 0; index--)
		{
			T entry = entries[index];
			if (entry == null || string.IsNullOrWhiteSpace(text(entry)) || (keepOnlyRecentWindow && day(entry) < minimumDay))
			{
				entries.RemoveAt(index);
				removedAny = true;
			}
		}
		return removedAny;
	}

	internal static bool ContainsStableKey<T>(List<T> entries, string stableKey, Func<T, string> key) where T : class
	{
		if (entries == null)
		{
			return false;
		}
		for (int index = 0; index < entries.Count; index++)
		{
			T entry = entries[index];
			if (entry != null && string.Equals(key(entry) ?? "", stableKey, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool ContainsForDay<T>(List<T> entries, int dayIndex, string stableKey, string text, Func<T, int> day, Func<T, string> key, Func<T, string> entryText) where T : class
	{
		if (entries == null)
		{
			return false;
		}
		for (int index = 0; index < entries.Count; index++)
		{
			T entry = entries[index];
			if (entry != null && day(entry) == dayIndex
				&& (string.Equals(key(entry) ?? "", stableKey, StringComparison.OrdinalIgnoreCase) || string.Equals((entryText(entry) ?? "").Trim(), text, StringComparison.Ordinal)))
			{
				return true;
			}
		}
		return false;
	}

	internal static int NextOrder<T>(List<T> entries, int dayIndex, Func<T, int> day, Func<T, int> order) where T : class
	{
		int highest = 0;
		if (entries != null)
		{
			for (int index = 0; index < entries.Count; index++)
			{
				T entry = entries[index];
				if (entry != null && day(entry) == dayIndex && order(entry) > highest)
				{
					highest = order(entry);
				}
			}
		}
		return highest + 1;
	}

	/// <summary>Timeline order: day, then sequence (0 = unknown sorts last), then order, then date text.</summary>
	internal static int CompareTimeline(int leftDay, int leftSequence, int leftOrder, string leftDate, int rightDay, int rightSequence, int rightOrder, string rightDate)
	{
		int result = leftDay.CompareTo(rightDay);
		if (result != 0) return result;
		result = (leftSequence > 0 ? leftSequence : int.MaxValue).CompareTo(rightSequence > 0 ? rightSequence : int.MaxValue);
		if (result != 0) return result;
		result = leftOrder.CompareTo(rightOrder);
		return result != 0 ? result : string.Compare(leftDate ?? "", rightDate ?? "", StringComparison.Ordinal);
	}

	/// <summary>Append, keep timeline sorted only when the tail is out of order, then cap the list from the front.</summary>
	internal static void Append<T>(List<T> entries, T entry, int maxEntries, Comparison<T> compare) where T : class
	{
		entries.Add(entry);
		if (entries.Count > 1 && compare(entries[entries.Count - 2], entry) > 0)
		{
			entries.Sort(compare);
		}
		if (maxEntries > 0 && entries.Count > maxEntries)
		{
			entries.RemoveRange(0, entries.Count - maxEntries);
		}
	}
}
