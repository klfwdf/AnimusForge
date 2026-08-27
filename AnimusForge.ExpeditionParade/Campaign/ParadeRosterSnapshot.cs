using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.ExpeditionParade.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;

namespace AnimusForge.ExpeditionParade.Campaign;

internal sealed class ParadeRosterSnapshot
{
	internal sealed class Entry
	{
		internal Entry(CharacterObject character, ParadeTroopCategory category, int healthyCount, int displayCount)
		{
			Character = character ?? throw new ArgumentNullException(nameof(character));
			Category = category;
			HealthyCount = healthyCount;
			DisplayCount = displayCount;
		}

		internal CharacterObject Character { get; }

		internal ParadeTroopCategory Category { get; }

		internal int HealthyCount { get; }

		internal int DisplayCount { get; }
	}

	private ParadeRosterSnapshot(DateTime capturedUtc, IReadOnlyList<Entry> entries)
	{
		CapturedUtc = capturedUtc;
		Entries = entries;
		TotalHealthyCount = entries.Sum(entry => entry.HealthyCount);
		TotalDisplayCount = entries.Sum(entry => entry.DisplayCount);
	}

	internal DateTime CapturedUtc { get; }

	internal IReadOnlyList<Entry> Entries { get; }

	internal int TotalHealthyCount { get; }

	internal int TotalDisplayCount { get; }

	internal static ParadeRosterSnapshot Capture(TroopRoster roster, int displayLimit, bool includeHeroes)
	{
		if (roster == null)
		{
			return new ParadeRosterSnapshot(DateTime.UtcNow, Array.Empty<Entry>());
		}

		List<(CharacterObject Character, int HealthyCount)> available = new();
		for (int index = 0; index < roster.Count; index++)
		{
			TroopRosterElement element = roster.GetElementCopyAtIndex(index);
			CharacterObject character = element.Character;
			if (character == null || (!includeHeroes && character.IsHero))
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
			.OrderBy(item => GetCategory(item.Character))
			.ThenByDescending(item => item.Character.Tier)
			.ThenBy(item => item.Character.StringId ?? string.Empty, StringComparer.Ordinal)
			.ToList();
		int[] allocations = ParadeSampleAllocator.Allocate(available.Select(item => item.HealthyCount).ToArray(), displayLimit);
		List<Entry> entries = new(available.Count);
		for (int index = 0; index < available.Count; index++)
		{
			entries.Add(new Entry(available[index].Character, GetCategory(available[index].Character), available[index].HealthyCount, allocations[index]));
		}
		return new ParadeRosterSnapshot(DateTime.UtcNow, entries);
	}

	internal ParadeRosterSnapshot WithDisplayLimit(int displayLimit)
	{
		int[] allocations = ParadeSampleAllocator.Allocate(Entries.Select(entry => entry.HealthyCount).ToArray(), displayLimit);
		List<Entry> entries = new(Entries.Count);
		for (int index = 0; index < Entries.Count; index++)
		{
			Entry source = Entries[index];
			entries.Add(new Entry(source.Character, source.Category, source.HealthyCount, allocations[index]));
		}
		return new ParadeRosterSnapshot(CapturedUtc, entries);
	}

	private static ParadeTroopCategory GetCategory(CharacterObject character)
	{
		return (int)character.DefaultFormationClass switch
		{
			1 => ParadeTroopCategory.Ranged,
			2 => ParadeTroopCategory.Cavalry,
			3 => ParadeTroopCategory.HorseArcher,
			_ => ParadeTroopCategory.Infantry
		};
	}
}
