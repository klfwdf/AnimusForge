using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

/// <summary>
/// Pure record-keeping rules for NPC action entries (recent window and major list). The entry
/// record and its JSON normalization live here. The host keeps only save keys, game-day
/// capture and engine-facing facts. One call per recorded action; no game reads.
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

	private static string RepairLegacyVillageRaidDefenseActionText(NpcActionEntry entry, string sourceText)
	{
		string text = (sourceText ?? "").Trim();
		if (!IsLegacyVillageRaidDefenseAction(entry, text, out bool isAftermath))
		{
			return text;
		}
		string place = (entry?.LocationText ?? "").Trim();
		if (string.IsNullOrWhiteSpace(place))
		{
			place = (entry?.SettlementName ?? "").Trim();
		}
		if (string.IsNullOrWhiteSpace(place))
		{
			place = "当地村庄";
		}
		bool? won = entry?.Won;
		string corrected = isAftermath
			? (won == true
				? (place + "的袭掠已经结束，你协助守军击退了袭掠者，正在整顿部队。")
				: (won == false
					? (place + "的袭掠已经结束，袭掠者已经得手；你正在收拢部队并处理残局。")
					: (place + "的袭掠已经结束，你正在协助守军整顿部队。")))
			: (won == true
				? ("你在" + place + "参与村庄保卫战，击退了袭掠者。")
				: (won == false
					? ("你在" + place + "参与村庄保卫战时失利，未能阻止袭掠者。")
					: ("你在" + place + "参与了村庄保卫战。")));
		int sentenceEnd = text.IndexOf('。');
		if (sentenceEnd < 0 || sentenceEnd >= text.Length - 1)
		{
			return corrected;
		}
		string detail = text.Substring(sentenceEnd + 1).Trim();
		return string.IsNullOrWhiteSpace(detail) ? corrected : (corrected + " " + detail);
	}

	private static bool IsLegacyVillageRaidDefenseAction(NpcActionEntry entry, string text, out bool isAftermath)
	{
		isAftermath = false;
		string stableKey = (entry?.StableKey ?? "").Trim();
		if (stableKey.IndexOf(":side:defender:hero:", StringComparison.OrdinalIgnoreCase) < 0)
		{
			return false;
		}
		string actionKind = (entry?.ActionKind ?? "").Trim();
		isAftermath = string.Equals(actionKind, "map_event_aftermath", StringComparison.OrdinalIgnoreCase)
			|| stableKey.StartsWith("mapevent_aftermath:", StringComparison.OrdinalIgnoreCase);
		bool isMapEvent = string.Equals(actionKind, "map_event", StringComparison.OrdinalIgnoreCase)
			|| stableKey.StartsWith("mapevent:", StringComparison.OrdinalIgnoreCase);
		if (!isMapEvent && !isAftermath)
		{
			return false;
		}
		if (isAftermath)
		{
			return text.IndexOf("的袭掠已经结束", StringComparison.OrdinalIgnoreCase) >= 0
				&& (text.IndexOf("清点缴获", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("收拢部队并处理残局", StringComparison.OrdinalIgnoreCase) >= 0);
		}
		return text.IndexOf("发动的袭掠中", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	internal static List<NpcActionEntry> SanitizeNpcActionEntries(List<NpcActionEntry> source, bool keepOnlyRecentWindow, int currentGameDayIndex)
	{
		List<NpcActionEntry> list = new List<NpcActionEntry>();
		if (source == null || source.Count <= 0)
		{
			return list;
		}
		int num = currentGameDayIndex;
		int num2 = num - RecentWindowDays + 1;
		int num3 = 0;
		int num4 = 0;
		foreach (NpcActionEntry item in source)
		{
			if (item == null)
			{
				continue;
			}
			string text = RepairLegacyVillageRaidDefenseActionText(item, (item.Text ?? "").Trim());
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (keepOnlyRecentWindow && item.Day < num2)
			{
				continue;
			}
			if (!keepOnlyRecentWindow && ShouldSuppressNpcMajorAction(item.ActionKind, item.StableKey, text))
			{
				continue;
			}
			list.Add(new NpcActionEntry
			{
				Day = Math.Max(0, item.Day),
				Order = ((item.Order > 0) ? item.Order : (++num3)),
				Sequence = ((item.Sequence > 0) ? item.Sequence : (++num4)),
				GameDate = (item.GameDate ?? "").Trim(),
				Text = text,
				StableKey = NpcActionLedger.NormalizeStableKey(item.StableKey, text),
				ActionKind = (item.ActionKind ?? "").Trim(),
				ActorHeroId = (item.ActorHeroId ?? "").Trim(),
				ActorClanId = (item.ActorClanId ?? "").Trim(),
				ActorKingdomId = (item.ActorKingdomId ?? "").Trim(),
				TargetHeroId = (item.TargetHeroId ?? "").Trim(),
				TargetClanId = (item.TargetClanId ?? "").Trim(),
				TargetKingdomId = (item.TargetKingdomId ?? "").Trim(),
				SettlementId = (item.SettlementId ?? "").Trim(),
				SettlementName = (item.SettlementName ?? "").Trim(),
				SettlementOwnerHeroId = (item.SettlementOwnerHeroId ?? "").Trim(),
				SettlementOwnerClanId = (item.SettlementOwnerClanId ?? "").Trim(),
				SettlementOwnerKingdomId = (item.SettlementOwnerKingdomId ?? "").Trim(),
				PreviousSettlementOwnerHeroId = (item.PreviousSettlementOwnerHeroId ?? "").Trim(),
				PreviousSettlementOwnerClanId = (item.PreviousSettlementOwnerClanId ?? "").Trim(),
				PreviousSettlementOwnerKingdomId = (item.PreviousSettlementOwnerKingdomId ?? "").Trim(),
				LocationText = (item.LocationText ?? "").Trim(),
				Won = item.Won,
				IsMajor = item.IsMajor
			});
			CopyFactIds(item.RelatedHeroIds, list[list.Count - 1].RelatedHeroIds);
			CopyFactIds(item.RelatedClanIds, list[list.Count - 1].RelatedClanIds);
			CopyFactIds(item.RelatedKingdomIds, list[list.Count - 1].RelatedKingdomIds);
		}
		return list.OrderBy((NpcActionEntry x) => x.Day).ThenBy((NpcActionEntry x) => (x.Sequence > 0) ? x.Sequence : int.MaxValue).ThenBy((NpcActionEntry x) => x.Order).ThenBy((NpcActionEntry x) => x.GameDate ?? "", StringComparer.Ordinal).ToList();
	}

	internal static bool ShouldSuppressNpcMajorAction(string actionKind, string stableKey, string text)
	{
		string text2 = (actionKind ?? "").Trim().ToLowerInvariant();
		switch (text2)
		{
		case "prisoner_taken_captor":
		case "prisoner_taken_prisoner":
		case "prisoner_released_captor":
		case "prisoner_released_prisoner":
		case "army_create":
		case "army_join":
			return true;
		}
		string text3 = (stableKey ?? "").Trim().ToLowerInvariant();
		if (text3.StartsWith("prisoner_taken:", StringComparison.OrdinalIgnoreCase) || text3.StartsWith("prisoner_released:", StringComparison.OrdinalIgnoreCase) || text3.StartsWith("army_create:", StringComparison.OrdinalIgnoreCase) || text3.StartsWith("army_join:", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		string text4 = (text ?? "").Trim();
		return (text4.IndexOf("俘虏了", StringComparison.OrdinalIgnoreCase) >= 0 || text4.IndexOf("被", StringComparison.OrdinalIgnoreCase) >= 0 && text4.IndexOf("俘虏", StringComparison.OrdinalIgnoreCase) >= 0 || text4.IndexOf("获释", StringComparison.OrdinalIgnoreCase) >= 0 || text4.IndexOf("成功逃脱", StringComparison.OrdinalIgnoreCase) >= 0 || text4.IndexOf("不再是你的囚犯", StringComparison.OrdinalIgnoreCase) >= 0 || text4.IndexOf("组建并统领了", StringComparison.OrdinalIgnoreCase) >= 0 || text4.IndexOf("加入了", StringComparison.OrdinalIgnoreCase) >= 0 && text4.IndexOf("军团", StringComparison.OrdinalIgnoreCase) >= 0);
	}

	private static void CopyFactIds(List<string> source, List<string> destination)
	{
		if (source == null || destination == null) return;
		foreach (string item in source)
		{
			string text = (item ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !destination.Any(x => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)))
				destination.Add(text);
		}
	}
}
