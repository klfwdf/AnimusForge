using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

public enum TownRecruitmentSlowdownStorageVersion
{
    LegacyV1 = 1,
    LegacyV2 = 2,
    CurrentV3 = 3,
}

/// <summary>
/// Selects one versioned recruitment-slowdown store and repairs legacy repopulation entries.
/// The AF adapter remains responsible only for reading and writing primitive save fields.
/// </summary>
public static class TownRecruitmentSlowdownSaveMigration
{
    public static TownRecruitmentSlowdownMigrationResult Resolve(
        bool hasCurrentStorage,
        IReadOnlyDictionary<string, int> currentEntries,
        bool hasLegacyV2Storage,
        IReadOnlyDictionary<string, int> legacyV2Entries,
        IReadOnlyDictionary<string, int> legacyV1Entries,
        IReadOnlyDictionary<string, int> repopulationEntries)
    {
        if (hasCurrentStorage)
        {
            return new TownRecruitmentSlowdownMigrationResult(
                TownRecruitmentSlowdownStorageVersion.CurrentV3,
                CopyEntries(currentEntries),
                0);
        }

        Dictionary<string, int> migrated = hasLegacyV2Storage
            ? CopyEntries(legacyV2Entries)
            : CopyEntries(legacyV1Entries);
        int restoredRepopulationEntries = 0;
        foreach (KeyValuePair<string, int> entry in repopulationEntries
            ?? EmptyEntries.Instance)
        {
            if (entry.Key == null
                || (migrated.TryGetValue(entry.Key, out int existingUntilDay)
                    && existingUntilDay >= entry.Value))
            {
                continue;
            }

            migrated[entry.Key] = entry.Value;
            restoredRepopulationEntries++;
        }
        return new TownRecruitmentSlowdownMigrationResult(
            hasLegacyV2Storage
                ? TownRecruitmentSlowdownStorageVersion.LegacyV2
                : TownRecruitmentSlowdownStorageVersion.LegacyV1,
            migrated,
            restoredRepopulationEntries);
    }

    private static Dictionary<string, int> CopyEntries(
        IReadOnlyDictionary<string, int> entries)
    {
        var copy = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, int> entry in entries ?? EmptyEntries.Instance)
        {
            if (entry.Key != null)
            {
                copy[entry.Key] = entry.Value;
            }
        }
        return copy;
    }

    private static class EmptyEntries
    {
        internal static readonly IReadOnlyDictionary<string, int> Instance =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }
}

public sealed class TownRecruitmentSlowdownMigrationResult
{
    private readonly Dictionary<string, int> _entries;

    public TownRecruitmentSlowdownMigrationResult(
        TownRecruitmentSlowdownStorageVersion sourceVersion,
        IReadOnlyDictionary<string, int> entries,
        int restoredRepopulationEntryCount)
    {
        SourceVersion = sourceVersion;
        _entries = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, int> entry in entries ?? EmptyEntries.Instance)
        {
            if (entry.Key != null)
            {
                _entries[entry.Key] = entry.Value;
            }
        }
        RestoredRepopulationEntryCount = Math.Max(0, restoredRepopulationEntryCount);
    }

    public TownRecruitmentSlowdownStorageVersion SourceVersion { get; }

    public int EntryCount => _entries.Count;

    public int RestoredRepopulationEntryCount { get; }

    public Dictionary<string, int> CopyEntries()
    {
        return new Dictionary<string, int>(_entries, StringComparer.OrdinalIgnoreCase);
    }

    private static class EmptyEntries
    {
        internal static readonly IReadOnlyDictionary<string, int> Instance =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }
}
