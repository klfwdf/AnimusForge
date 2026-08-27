using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;

namespace AnimusForge.ExpeditionParade;

internal sealed class ParadeRosterSnapshot
{
	internal sealed class Entry
	{
		internal Entry(CharacterObject character, int healthyCount, int displayCount)
		{
			Character = character;
			HealthyCount = healthyCount;
			DisplayCount = displayCount;
		}

		internal CharacterObject Character { get; }

		internal int HealthyCount { get; }

		internal int DisplayCount { get; }
	}

	private ParadeRosterSnapshot(IReadOnlyList<Entry> entries)
	{
		Entries = entries;
		TotalHealthyCount = entries.Sum(entry => entry.HealthyCount);
		TotalDisplayCount = entries.Sum(entry => entry.DisplayCount);
	}

	internal IReadOnlyList<Entry> Entries { get; }

	internal int TotalHealthyCount { get; }

	internal int TotalDisplayCount { get; }

	internal static ParadeRosterSnapshot Capture(TroopRoster roster, int displayLimit)
	{
		if (roster == null)
		{
			return new ParadeRosterSnapshot(Array.Empty<Entry>());
		}

		List<(CharacterObject Character, int HealthyCount)> available = new();
		for (int index = 0; index < roster.Count; index++)
		{
			TroopRosterElement element = roster.GetElementCopyAtIndex(index);
			CharacterObject character = element.Character;
			if (character == null || character.IsHero)
			{
				continue;
			}

			int healthyCount = Math.Max(0, element.Number - element.WoundedNumber);
			if (healthyCount > 0)
			{
				available.Add((character, healthyCount));
			}
		}

		available = available
			.OrderBy(item => (int)item.Character.DefaultFormationClass)
			.ThenByDescending(item => item.Character.Tier)
			.ThenBy(item => item.Character.StringId ?? string.Empty, StringComparer.Ordinal)
			.ToList();

		int[] displayCounts = ParadeSampleAllocator.Allocate(
			available.Select(item => item.HealthyCount).ToArray(),
			displayLimit);
		List<Entry> entries = new(available.Count);
		for (int index = 0; index < available.Count; index++)
		{
			entries.Add(new Entry(available[index].Character, available[index].HealthyCount, displayCounts[index]));
		}
		return new ParadeRosterSnapshot(entries);
	}

	internal string BuildDiagnosticSummary(int maxEntries = 24)
	{
		IEnumerable<string> parts = Entries
			.Where(entry => entry.DisplayCount > 0)
			.Take(Math.Max(0, maxEntries))
			.Select(entry => (entry.Character?.StringId ?? "N/A") + ":" + entry.DisplayCount + "/" + entry.HealthyCount);
		return "healthy=" + TotalHealthyCount
			+ ",display=" + TotalDisplayCount
			+ ",types=" + Entries.Count
			+ ",sample=[" + string.Join(",", parts) + "]";
	}
}
