using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Persistable, Bannerlord-independent governance memory for one settlement.
/// </summary>
public sealed class SettlementRuleMemoryRecord
{
    private readonly SettlementRuleMemoryEntry[] _rulerMemories;

    public SettlementRuleMemoryRecord(
        int schemaVersion,
        string settlementId,
        string settlementName,
        int cultureStartDay,
        IEnumerable<SettlementRuleMemoryEntry> rulerMemories)
    {
        SchemaVersion = schemaVersion;
        SettlementId = settlementId ?? string.Empty;
        SettlementName = settlementName ?? string.Empty;
        CultureStartDay = Math.Max(0, cultureStartDay);
        _rulerMemories = (rulerMemories ?? Array.Empty<SettlementRuleMemoryEntry>())
            .Where(entry => entry != null && entry.HasIdentity)
            .Take(SettlementRuleMemoryStore.MaximumRulerMemories)
            .ToArray();
    }

    /// <summary>
    /// Retained for source compatibility with the v1 flat record shape.
    /// </summary>
    public SettlementRuleMemoryRecord(
        int schemaVersion,
        string settlementId,
        string settlementName,
        string rulerId,
        string rulerName,
        string cultureId,
        string cultureName,
        string rulerPersonality,
        int ruleStartDay,
        int cultureStartDay,
        int minimumRuleDurationDays,
        string previousRulerId,
        string previousRulerName,
        string previousCultureId,
        string previousCultureName,
        string previousRulerPersonality,
        int previousRuleDurationDays,
        bool previousDurationWasMinimum)
    {
        SchemaVersion = schemaVersion;
        SettlementId = settlementId ?? string.Empty;
        SettlementName = settlementName ?? string.Empty;
        CultureStartDay = Math.Max(0, cultureStartDay);
        var entries = new List<SettlementRuleMemoryEntry>
        {
            new SettlementRuleMemoryEntry(
                rulerId,
                rulerName,
                cultureId,
                cultureName,
                rulerPersonality,
                ruleStartDay,
                minimumRuleDurationDays,
                0,
                false,
                string.Empty,
                false),
        };
        if (!string.IsNullOrWhiteSpace(previousRulerId)
            || !string.IsNullOrWhiteSpace(previousRulerName)
            || !string.IsNullOrWhiteSpace(previousCultureId)
            || !string.IsNullOrWhiteSpace(previousCultureName))
        {
            entries.Add(new SettlementRuleMemoryEntry(
                previousRulerId,
                previousRulerName,
                previousCultureId,
                previousCultureName,
                previousRulerPersonality,
                0,
                0,
                previousRuleDurationDays,
                previousDurationWasMinimum,
                string.Empty,
                false));
        }
        _rulerMemories = entries.Where(entry => entry.HasIdentity).ToArray();
    }

    public int SchemaVersion { get; }

    public string SettlementId { get; }

    public string SettlementName { get; }

    public IReadOnlyList<SettlementRuleMemoryEntry> RulerMemories => _rulerMemories;

    public SettlementRuleMemoryEntry CurrentRule => _rulerMemories.Length > 0 ? _rulerMemories[0] : null;

    public string RulerId => CurrentRule?.RulerId ?? string.Empty;

    public string RulerName => CurrentRule?.RulerName ?? string.Empty;

    public string CultureId => CurrentRule?.CultureId ?? string.Empty;

    public string CultureName => CurrentRule?.CultureName ?? string.Empty;

    public string RulerPersonality => CurrentRule?.RulerPersonality ?? string.Empty;

    public int RuleStartDay => CurrentRule?.RuleStartDay ?? 0;

    public int CultureStartDay { get; }

    public int MinimumRuleDurationDays => CurrentRule?.MinimumRuleDurationDays ?? 0;

    public string PreviousRulerId => GetPreviousValue(entry => entry.RulerId, string.Empty);

    public string PreviousRulerName => GetPreviousValue(entry => entry.RulerName, string.Empty);

    public string PreviousCultureId => GetPreviousValue(entry => entry.CultureId, string.Empty);

    public string PreviousCultureName => GetPreviousValue(entry => entry.CultureName, string.Empty);

    public string PreviousRulerPersonality => GetPreviousValue(entry => entry.RulerPersonality, string.Empty);

    public int PreviousRuleDurationDays => GetPreviousValue(entry => entry.RecordedRuleDurationDays, 0);

    public bool PreviousDurationWasMinimum => GetPreviousValue(entry => entry.DurationWasMinimum, false);

    public bool HasPreviousRule => _rulerMemories.Length > 1;

    private T GetPreviousValue<T>(Func<SettlementRuleMemoryEntry, T> selector, T fallback)
    {
        return _rulerMemories.Length > 1 ? selector(_rulerMemories[1]) : fallback;
    }
}

/// <summary>
/// One live governance observation supplied by the AF host adapter.
/// </summary>
public sealed class SettlementRuleMemoryObservation
{
    public SettlementRuleMemoryObservation(
        string settlementId,
        string settlementName,
        string rulerId,
        string rulerName,
        string cultureId,
        string cultureName,
        string rulerPersonality,
        int currentDay,
        bool useMinimumDurationFallback)
    {
        SettlementId = settlementId ?? string.Empty;
        SettlementName = settlementName ?? string.Empty;
        RulerId = rulerId ?? string.Empty;
        RulerName = rulerName ?? string.Empty;
        CultureId = cultureId ?? string.Empty;
        CultureName = cultureName ?? string.Empty;
        RulerPersonality = rulerPersonality ?? string.Empty;
        CurrentDay = currentDay;
        UseMinimumDurationFallback = useMinimumDurationFallback;
    }

    public string SettlementId { get; }

    public string SettlementName { get; }

    public string RulerId { get; }

    public string RulerName { get; }

    public string CultureId { get; }

    public string CultureName { get; }

    public string RulerPersonality { get; }

    public int CurrentDay { get; }

    public bool UseMinimumDurationFallback { get; }
}

/// <summary>
/// Result of applying one live observation to the settlement memory store.
/// </summary>
public sealed class SettlementRuleMemoryUpdate
{
    public SettlementRuleMemoryUpdate(
        bool accepted,
        SettlementRuleMemoryRecord record,
        bool initialized,
        bool rulerChanged,
        bool cultureChanged)
    {
        Accepted = accepted;
        Record = record;
        Initialized = initialized;
        RulerChanged = rulerChanged;
        CultureChanged = cultureChanged;
    }

    public bool Accepted { get; }

    public SettlementRuleMemoryRecord Record { get; }

    public bool Initialized { get; }

    public bool RulerChanged { get; }

    public bool CultureChanged { get; }

    public bool CapturedPreviousRule => Record?.HasPreviousRule == true && (RulerChanged || CultureChanged);
}
