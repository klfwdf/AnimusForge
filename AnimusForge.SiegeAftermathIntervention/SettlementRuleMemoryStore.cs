using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Owns the single in-memory governance history for each town.
/// </summary>
public sealed class SettlementRuleMemoryStore
{
    public const int CurrentSchemaVersion = 2;
    public const int MaximumRulerMemories = 3;
    public const int MinimumFallbackRuleDays = 168;

    private readonly Dictionary<string, SettlementRuleMemoryRecord> _records =
        new Dictionary<string, SettlementRuleMemoryRecord>(StringComparer.OrdinalIgnoreCase);

    public int Count => _records.Count;

    public SettlementRuleMemoryUpdate Observe(SettlementRuleMemoryObservation observation)
    {
        if (observation == null || string.IsNullOrWhiteSpace(observation.SettlementId))
        {
            return new SettlementRuleMemoryUpdate(false, null, false, false, false);
        }

        string settlementId = Normalize(observation.SettlementId);
        int currentDay = Math.Max(0, observation.CurrentDay);
        if (!_records.TryGetValue(settlementId, out SettlementRuleMemoryRecord existing)
            || existing.CurrentRule == null)
        {
            var initialized = new SettlementRuleMemoryRecord(
                CurrentSchemaVersion,
                settlementId,
                Normalize(observation.SettlementName),
                currentDay,
                new[] { CreateObservedEntry(observation, currentDay) });
            _records[settlementId] = initialized;
            return new SettlementRuleMemoryUpdate(true, initialized, true, false, false);
        }

        SettlementRuleMemoryEntry current = existing.CurrentRule;
        string observedRulerId = Normalize(observation.RulerId);
        string observedCultureId = Normalize(observation.CultureId);
        bool rulerChanged = HasChanged(observedRulerId, current.RulerId);
        bool cultureChanged = HasChanged(observedCultureId, current.CultureId);
        var entries = new List<SettlementRuleMemoryEntry>(MaximumRulerMemories);

        if (rulerChanged)
        {
            entries.Add(CreateObservedEntry(observation, currentDay));
            entries.Add(FreezeCurrentEntry(current, currentDay));
            entries.AddRange(existing.RulerMemories.Skip(1));
        }
        else
        {
            entries.Add(UpdateCurrentEntry(current, observation, cultureChanged));
            entries.AddRange(existing.RulerMemories.Skip(1));
        }

        var updated = new SettlementRuleMemoryRecord(
            CurrentSchemaVersion,
            settlementId,
            PickObserved(observation.SettlementName, existing.SettlementName),
            cultureChanged ? currentDay : existing.CultureStartDay,
            entries.Take(MaximumRulerMemories));
        _records[settlementId] = updated;
        return new SettlementRuleMemoryUpdate(true, updated, false, rulerChanged, cultureChanged);
    }

    public bool TryGet(string settlementId, out SettlementRuleMemoryRecord record)
    {
        return _records.TryGetValue(Normalize(settlementId), out record);
    }

    public bool TrySetNarrative(
        string settlementId,
        string rulerId,
        string narrative,
        bool narrativeIsManual,
        out SettlementRuleMemoryRecord updatedRecord)
    {
        return TrySetNarrative(
            settlementId,
            rulerId,
            null,
            narrative,
            narrativeIsManual,
            out updatedRecord);
    }

    public bool TrySetNarrative(
        string settlementId,
        string rulerId,
        int? ruleStartDay,
        string narrative,
        bool narrativeIsManual,
        out SettlementRuleMemoryRecord updatedRecord)
    {
        updatedRecord = null;
        if (!_records.TryGetValue(Normalize(settlementId), out SettlementRuleMemoryRecord existing))
        {
            return false;
        }

        string targetRulerId = Normalize(rulerId);
        string normalizedNarrative = SettlementRuleMemoryNarrativePolicy.NormalizeForStorage(narrative);
        var entries = new List<SettlementRuleMemoryEntry>(existing.RulerMemories.Count);
        bool replaced = false;
        foreach (SettlementRuleMemoryEntry entry in existing.RulerMemories)
        {
            bool matches = !replaced
                && IsSameRuler(entry, targetRulerId)
                && (!ruleStartDay.HasValue || entry.RuleStartDay == Math.Max(0, ruleStartDay.Value));
            entries.Add(matches
                ? CopyEntry(entry, normalizedNarrative, narrativeIsManual)
                : entry);
            replaced |= matches;
        }
        if (!replaced)
        {
            return false;
        }

        updatedRecord = new SettlementRuleMemoryRecord(
            CurrentSchemaVersion,
            existing.SettlementId,
            existing.SettlementName,
            existing.CultureStartDay,
            entries);
        _records[existing.SettlementId] = updatedRecord;
        return true;
    }

    public int Restore(IEnumerable<SettlementRuleMemoryRecord> records)
    {
        _records.Clear();
        int rejected = 0;
        foreach (SettlementRuleMemoryRecord record in records ?? Array.Empty<SettlementRuleMemoryRecord>())
        {
            SettlementRuleMemoryRecord normalized = NormalizeRecord(record);
            if (normalized == null)
            {
                rejected++;
                continue;
            }
            _records[normalized.SettlementId] = normalized;
        }
        return rejected;
    }

    public IReadOnlyList<SettlementRuleMemoryRecord> Export()
    {
        return _records.Values
            .OrderBy(record => record.SettlementId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public void Clear()
    {
        _records.Clear();
    }

    public static int GetEffectiveRuleDurationDays(SettlementRuleMemoryRecord record, int currentDay)
    {
        return GetEffectiveRuleDurationDays(record?.CurrentRule, currentDay, true);
    }

    public static int GetEffectiveRuleDurationDays(SettlementRuleMemoryEntry entry, int currentDay, bool isCurrent)
    {
        if (entry == null)
        {
            return 0;
        }
        if (!isCurrent)
        {
            return Math.Max(entry.RecordedRuleDurationDays, entry.MinimumRuleDurationDays);
        }
        int elapsed = Math.Max(0, Math.Max(0, currentDay) - entry.RuleStartDay);
        return Math.Max(elapsed, entry.MinimumRuleDurationDays);
    }

    private static SettlementRuleMemoryEntry CreateObservedEntry(SettlementRuleMemoryObservation observation, int currentDay)
    {
        return new SettlementRuleMemoryEntry(
            Normalize(observation.RulerId),
            Normalize(observation.RulerName),
            Normalize(observation.CultureId),
            Normalize(observation.CultureName),
            Normalize(observation.RulerPersonality),
            currentDay,
            observation.UseMinimumDurationFallback ? MinimumFallbackRuleDays : 0,
            0,
            false,
            string.Empty,
            false);
    }

    private static SettlementRuleMemoryEntry FreezeCurrentEntry(SettlementRuleMemoryEntry entry, int currentDay)
    {
        int elapsed = Math.Max(0, currentDay - entry.RuleStartDay);
        int duration = GetEffectiveRuleDurationDays(entry, currentDay, true);
        return new SettlementRuleMemoryEntry(
            entry.RulerId,
            entry.RulerName,
            entry.CultureId,
            entry.CultureName,
            entry.RulerPersonality,
            entry.RuleStartDay,
            0,
            duration,
            entry.MinimumRuleDurationDays > elapsed,
            entry.Narrative,
            entry.NarrativeIsManual);
    }

    private static SettlementRuleMemoryEntry UpdateCurrentEntry(
        SettlementRuleMemoryEntry entry,
        SettlementRuleMemoryObservation observation,
        bool cultureChanged)
    {
        string narrative = cultureChanged && !entry.NarrativeIsManual ? string.Empty : entry.Narrative;
        return new SettlementRuleMemoryEntry(
            PickObserved(observation.RulerId, entry.RulerId),
            PickObserved(observation.RulerName, entry.RulerName),
            PickObserved(observation.CultureId, entry.CultureId),
            PickObserved(observation.CultureName, entry.CultureName),
            PickObserved(observation.RulerPersonality, entry.RulerPersonality),
            entry.RuleStartDay,
            entry.MinimumRuleDurationDays,
            0,
            false,
            narrative,
            entry.NarrativeIsManual);
    }

    private static SettlementRuleMemoryEntry CopyEntry(
        SettlementRuleMemoryEntry entry,
        string narrative,
        bool narrativeIsManual)
    {
        return new SettlementRuleMemoryEntry(
            entry.RulerId,
            entry.RulerName,
            entry.CultureId,
            entry.CultureName,
            entry.RulerPersonality,
            entry.RuleStartDay,
            entry.MinimumRuleDurationDays,
            entry.RecordedRuleDurationDays,
            entry.DurationWasMinimum,
            narrative,
            narrativeIsManual);
    }

    private static SettlementRuleMemoryRecord NormalizeRecord(SettlementRuleMemoryRecord record)
    {
        if (record == null
            || record.SchemaVersion != CurrentSchemaVersion
            || string.IsNullOrWhiteSpace(record.SettlementId))
        {
            return null;
        }

        SettlementRuleMemoryEntry[] entries = record.RulerMemories
            .Where(entry => entry != null && entry.HasIdentity)
            .Select(entry => new SettlementRuleMemoryEntry(
                Normalize(entry.RulerId),
                Normalize(entry.RulerName),
                Normalize(entry.CultureId),
                Normalize(entry.CultureName),
                Normalize(entry.RulerPersonality),
                entry.RuleStartDay,
                entry.MinimumRuleDurationDays,
                entry.RecordedRuleDurationDays,
                entry.DurationWasMinimum,
                SettlementRuleMemoryNarrativePolicy.NormalizeForStorage(entry.Narrative),
                entry.NarrativeIsManual))
            .Take(MaximumRulerMemories)
            .ToArray();
        if (entries.Length == 0)
        {
            return null;
        }
        return new SettlementRuleMemoryRecord(
            CurrentSchemaVersion,
            Normalize(record.SettlementId),
            Normalize(record.SettlementName),
            record.CultureStartDay,
            entries);
    }

    private static bool IsSameRuler(SettlementRuleMemoryEntry entry, string targetRulerId)
    {
        if (entry == null)
        {
            return false;
        }
        return !string.IsNullOrWhiteSpace(targetRulerId)
            ? string.Equals(entry.RulerId, targetRulerId, StringComparison.OrdinalIgnoreCase)
            : string.IsNullOrWhiteSpace(entry.RulerId);
    }

    private static bool HasChanged(string observed, string existing)
    {
        return !string.IsNullOrWhiteSpace(observed)
            && !string.IsNullOrWhiteSpace(existing)
            && !string.Equals(observed, existing, StringComparison.OrdinalIgnoreCase);
    }

    private static string PickObserved(string observed, string existing)
    {
        string normalized = Normalize(observed);
        return string.IsNullOrWhiteSpace(normalized) ? Normalize(existing) : normalized;
    }

    private static string Normalize(string value)
    {
        return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
    }
}
