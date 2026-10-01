using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Owns the single in-memory governance history for each town.
/// </summary>
public sealed class SettlementRuleMemoryStore
{
    public const int CurrentSchemaVersion = 3;
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
        if (!rulerChanged && !cultureChanged
            && PickObserved(observation.RulerId, current.RulerId) == current.RulerId
            && PickObserved(observation.RulerName, current.RulerName) == current.RulerName
            && PickObserved(observation.CultureId, current.CultureId) == current.CultureId
            && PickObserved(observation.CultureName, current.CultureName) == current.CultureName
            && PickObserved(observation.RulerPersonality, current.RulerPersonality) == current.RulerPersonality
            && PickObserved(observation.SettlementName, existing.SettlementName) == existing.SettlementName)
            return new SettlementRuleMemoryUpdate(true, existing, false, false, false);
        var entries = new List<SettlementRuleMemoryEntry>(MaximumRulerMemories);

        if (rulerChanged)
        {
            entries.Add(CreateObservedEntry(observation, currentDay, current.Evolution));
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

    public bool TryRecordConfirmedEvent(string settlementId, SettlementRuleMemoryFact fact)
    {
        if (fact == null || fact.Id.Length == 0 || fact.Text.Length == 0
            || !TryGet(settlementId, out var record) || record.CurrentRule == null) return false;
        var entry = record.CurrentRule;
        if (entry.Evolution.Facts.Any(f => string.Equals(f.Id, fact.Id, StringComparison.Ordinal))) return false;
        ReplaceCurrent(record, CopyEntry(entry, entry.Narrative, entry.NarrativeIsManual,
            entry.Evolution.Changed(fact.Day, entry.Evolution.Facts.Concat(new[] { fact }))));
        return true;
    }

    public bool TryStoreGeneratedNarrative(SettlementRuleMemoryRecord expected, int day, string narrative)
    {
        if (expected?.CurrentRule == null || !TryGet(expected.SettlementId, out var record)) return false;
        var entry = record.CurrentRule;
        if (entry == null || entry.NarrativeIsManual
            || entry.RulerId != expected.RulerId || entry.RuleStartDay != expected.RuleStartDay
            || entry.Evolution.Revision != expected.CurrentRule.Evolution.Revision) return false;
        string normalized = SettlementRuleMemoryNarrativePolicy.NormalizeForStorage(narrative);
        if (normalized.Length < SettlementRuleMemoryNarrativePolicy.MinimumGeneratedLength) return false;
        ReplaceCurrent(record, CopyEntry(entry, normalized, false, entry.Evolution.Generated(day)));
        return true;
    }

    private void ReplaceCurrent(SettlementRuleMemoryRecord record, SettlementRuleMemoryEntry entry)
    {
        _records[record.SettlementId] = new SettlementRuleMemoryRecord(CurrentSchemaVersion,
            record.SettlementId, record.SettlementName, record.CultureStartDay,
            new[] { entry }.Concat(record.RulerMemories.Skip(1)));
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

    private static SettlementRuleMemoryEntry CreateObservedEntry(SettlementRuleMemoryObservation observation, int currentDay, SettlementRuleMemoryEvolution previous = null)
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
            false,
            previous == null ? null : new SettlementRuleMemoryEvolution(previous.Revision + 1, 0, -1, currentDay, previous.Facts));
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
            entry.NarrativeIsManual,
            entry.Evolution);
    }

    private static SettlementRuleMemoryEntry UpdateCurrentEntry(
        SettlementRuleMemoryEntry entry,
        SettlementRuleMemoryObservation observation,
        bool cultureChanged)
    {
        bool sourceChanged = cultureChanged
            || !string.Equals(PickObserved(observation.RulerPersonality, entry.RulerPersonality), entry.RulerPersonality, StringComparison.Ordinal)
            || !string.Equals(PickObserved(observation.RulerName, entry.RulerName), entry.RulerName, StringComparison.Ordinal);
        string narrative = entry.Narrative; // Keep the last good text until its replacement commits.
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
            entry.NarrativeIsManual,
            sourceChanged ? entry.Evolution.Changed(observation.CurrentDay) : entry.Evolution);
    }

    private static SettlementRuleMemoryEntry CopyEntry(
        SettlementRuleMemoryEntry entry,
        string narrative,
        bool narrativeIsManual,
        SettlementRuleMemoryEvolution evolution = null)
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
            narrativeIsManual,
            evolution ?? entry.Evolution.Changed(entry.RuleStartDay));
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
                entry.NarrativeIsManual,
                entry.Evolution))
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
