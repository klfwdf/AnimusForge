namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Persistable, Bannerlord-independent governance memory for one settlement.
/// </summary>
public sealed class SettlementRuleMemoryRecord
{
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
        RulerId = rulerId ?? string.Empty;
        RulerName = rulerName ?? string.Empty;
        CultureId = cultureId ?? string.Empty;
        CultureName = cultureName ?? string.Empty;
        RulerPersonality = rulerPersonality ?? string.Empty;
        RuleStartDay = ruleStartDay;
        CultureStartDay = cultureStartDay;
        MinimumRuleDurationDays = minimumRuleDurationDays;
        PreviousRulerId = previousRulerId ?? string.Empty;
        PreviousRulerName = previousRulerName ?? string.Empty;
        PreviousCultureId = previousCultureId ?? string.Empty;
        PreviousCultureName = previousCultureName ?? string.Empty;
        PreviousRulerPersonality = previousRulerPersonality ?? string.Empty;
        PreviousRuleDurationDays = previousRuleDurationDays;
        PreviousDurationWasMinimum = previousDurationWasMinimum;
    }

    public int SchemaVersion { get; }

    public string SettlementId { get; }

    public string SettlementName { get; }

    public string RulerId { get; }

    public string RulerName { get; }

    public string CultureId { get; }

    public string CultureName { get; }

    public string RulerPersonality { get; }

    public int RuleStartDay { get; }

    public int CultureStartDay { get; }

    public int MinimumRuleDurationDays { get; }

    public string PreviousRulerId { get; }

    public string PreviousRulerName { get; }

    public string PreviousCultureId { get; }

    public string PreviousCultureName { get; }

    public string PreviousRulerPersonality { get; }

    public int PreviousRuleDurationDays { get; }

    public bool PreviousDurationWasMinimum { get; }

    public bool HasPreviousRule => !string.IsNullOrWhiteSpace(PreviousRulerId)
        || !string.IsNullOrWhiteSpace(PreviousRulerName)
        || !string.IsNullOrWhiteSpace(PreviousCultureId)
        || !string.IsNullOrWhiteSpace(PreviousCultureName);
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
