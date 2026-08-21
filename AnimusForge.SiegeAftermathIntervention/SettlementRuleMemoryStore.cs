using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Owns the single in-memory governance record for each settlement.
/// </summary>
public sealed class SettlementRuleMemoryStore
{
    public const int CurrentSchemaVersion = 1;
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
        if (!_records.TryGetValue(settlementId, out SettlementRuleMemoryRecord existing))
        {
            var initialized = new SettlementRuleMemoryRecord(
                CurrentSchemaVersion,
                settlementId,
                Normalize(observation.SettlementName),
                Normalize(observation.RulerId),
                Normalize(observation.RulerName),
                Normalize(observation.CultureId),
                Normalize(observation.CultureName),
                Normalize(observation.RulerPersonality),
                currentDay,
                currentDay,
                observation.UseMinimumDurationFallback ? MinimumFallbackRuleDays : 0,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                false);
            _records[settlementId] = initialized;
            return new SettlementRuleMemoryUpdate(true, initialized, true, false, false);
        }

        string observedRulerId = Normalize(observation.RulerId);
        string observedCultureId = Normalize(observation.CultureId);
        bool rulerChanged = !string.IsNullOrWhiteSpace(observedRulerId)
            && !string.IsNullOrWhiteSpace(existing.RulerId)
            && !string.Equals(observedRulerId, existing.RulerId, StringComparison.OrdinalIgnoreCase);
        bool cultureChanged = !string.IsNullOrWhiteSpace(observedCultureId)
            && !string.IsNullOrWhiteSpace(existing.CultureId)
            && !string.Equals(observedCultureId, existing.CultureId, StringComparison.OrdinalIgnoreCase);

        string rulerId = PickObserved(observedRulerId, existing.RulerId);
        string rulerName = PickObserved(observation.RulerName, existing.RulerName);
        string cultureId = PickObserved(observedCultureId, existing.CultureId);
        string cultureName = PickObserved(observation.CultureName, existing.CultureName);
        string personality = rulerChanged
            ? Normalize(observation.RulerPersonality)
            : PickObserved(observation.RulerPersonality, existing.RulerPersonality);
        int previousDuration = GetEffectiveRuleDurationDays(existing, currentDay);
        int elapsedBeforeChange = Math.Max(0, currentDay - Math.Max(0, existing.RuleStartDay));
        bool previousDurationWasMinimum = existing.MinimumRuleDurationDays > elapsedBeforeChange;

        var updated = new SettlementRuleMemoryRecord(
            CurrentSchemaVersion,
            settlementId,
            PickObserved(observation.SettlementName, existing.SettlementName),
            rulerId,
            rulerName,
            cultureId,
            cultureName,
            personality,
            rulerChanged ? currentDay : Math.Max(0, existing.RuleStartDay),
            cultureChanged ? currentDay : Math.Max(0, existing.CultureStartDay),
            rulerChanged
                ? (observation.UseMinimumDurationFallback ? MinimumFallbackRuleDays : 0)
                : Math.Max(0, existing.MinimumRuleDurationDays),
            rulerChanged || cultureChanged ? existing.RulerId : existing.PreviousRulerId,
            rulerChanged || cultureChanged ? existing.RulerName : existing.PreviousRulerName,
            rulerChanged || cultureChanged ? existing.CultureId : existing.PreviousCultureId,
            rulerChanged || cultureChanged ? existing.CultureName : existing.PreviousCultureName,
            rulerChanged || cultureChanged ? existing.RulerPersonality : existing.PreviousRulerPersonality,
            rulerChanged || cultureChanged ? previousDuration : Math.Max(0, existing.PreviousRuleDurationDays),
            rulerChanged || cultureChanged ? previousDurationWasMinimum : existing.PreviousDurationWasMinimum);
        _records[settlementId] = updated;
        return new SettlementRuleMemoryUpdate(true, updated, false, rulerChanged, cultureChanged);
    }

    public bool TryGet(string settlementId, out SettlementRuleMemoryRecord record)
    {
        return _records.TryGetValue(Normalize(settlementId), out record);
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
        if (record == null)
        {
            return 0;
        }
        int elapsed = Math.Max(0, Math.Max(0, currentDay) - Math.Max(0, record.RuleStartDay));
        return Math.Max(elapsed, Math.Max(0, record.MinimumRuleDurationDays));
    }

    private static SettlementRuleMemoryRecord NormalizeRecord(SettlementRuleMemoryRecord record)
    {
        if (record == null
            || record.SchemaVersion != CurrentSchemaVersion
            || string.IsNullOrWhiteSpace(record.SettlementId))
        {
            return null;
        }

        return new SettlementRuleMemoryRecord(
            CurrentSchemaVersion,
            Normalize(record.SettlementId),
            Normalize(record.SettlementName),
            Normalize(record.RulerId),
            Normalize(record.RulerName),
            Normalize(record.CultureId),
            Normalize(record.CultureName),
            Normalize(record.RulerPersonality),
            Math.Max(0, record.RuleStartDay),
            Math.Max(0, record.CultureStartDay),
            Math.Max(0, record.MinimumRuleDurationDays),
            Normalize(record.PreviousRulerId),
            Normalize(record.PreviousRulerName),
            Normalize(record.PreviousCultureId),
            Normalize(record.PreviousCultureName),
            Normalize(record.PreviousRulerPersonality),
            Math.Max(0, record.PreviousRuleDurationDays),
            record.PreviousDurationWasMinimum);
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
