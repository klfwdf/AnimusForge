using System;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// One ruler tenure retained in a town's local governance memory.
/// </summary>
public sealed class SettlementRuleMemoryEntry
{
    public SettlementRuleMemoryEntry(
        string rulerId,
        string rulerName,
        string cultureId,
        string cultureName,
        string rulerPersonality,
        int ruleStartDay,
        int minimumRuleDurationDays,
        int recordedRuleDurationDays,
        bool durationWasMinimum,
        string narrative,
        bool narrativeIsManual)
    {
        RulerId = rulerId ?? string.Empty;
        RulerName = rulerName ?? string.Empty;
        CultureId = cultureId ?? string.Empty;
        CultureName = cultureName ?? string.Empty;
        RulerPersonality = rulerPersonality ?? string.Empty;
        RuleStartDay = Math.Max(0, ruleStartDay);
        MinimumRuleDurationDays = Math.Max(0, minimumRuleDurationDays);
        RecordedRuleDurationDays = Math.Max(0, recordedRuleDurationDays);
        DurationWasMinimum = durationWasMinimum;
        Narrative = narrative ?? string.Empty;
        NarrativeIsManual = narrativeIsManual;
    }

    public string RulerId { get; }

    public string RulerName { get; }

    public string CultureId { get; }

    public string CultureName { get; }

    public string RulerPersonality { get; }

    public int RuleStartDay { get; }

    public int MinimumRuleDurationDays { get; }

    public int RecordedRuleDurationDays { get; }

    public bool DurationWasMinimum { get; }

    public string Narrative { get; }

    public bool NarrativeIsManual { get; }

    public bool HasIdentity => !string.IsNullOrWhiteSpace(RulerId) || !string.IsNullOrWhiteSpace(RulerName);
}
