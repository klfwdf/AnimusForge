using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

public sealed class SettlementCultureLegacyRecoveryFacts
{
    public SettlementCultureLegacyRecoveryFacts(
        bool isTown,
        bool hasExplicitOverride,
        int cleanedDeadNotableCount,
        string currentCultureId,
        IEnumerable<string> livingNotableCultureIds)
    {
        IsTown = isTown;
        HasExplicitOverride = hasExplicitOverride;
        CleanedDeadNotableCount = Math.Max(0, cleanedDeadNotableCount);
        CurrentCultureId = Normalize(currentCultureId);
        var cultures = new List<string>();
        foreach (string cultureId in livingNotableCultureIds ?? Array.Empty<string>())
        {
            cultures.Add(Normalize(cultureId));
        }
        LivingNotableCultureIds = cultures;
    }

    public bool IsTown { get; }

    public bool HasExplicitOverride { get; }

    public int CleanedDeadNotableCount { get; }

    public string CurrentCultureId { get; }

    public IReadOnlyList<string> LivingNotableCultureIds { get; }

    private static string Normalize(string value)
    {
        return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
    }
}

public sealed class SettlementCultureLegacyRecoveryDecision
{
    public SettlementCultureLegacyRecoveryDecision(bool shouldRecordOverride, string cultureId, string reason)
    {
        ShouldRecordOverride = shouldRecordOverride;
        CultureId = cultureId ?? string.Empty;
        Reason = reason ?? string.Empty;
    }

    public bool ShouldRecordOverride { get; }

    public string CultureId { get; }

    public string Reason { get; }
}

/// <summary>
/// Restricts automatic recovery to the legacy corruption signature: a town with cleaned dead-notable
/// cache entries and a unanimous replacement-notable culture. Ordinary culture differences never qualify.
/// </summary>
public sealed class SettlementCultureLegacyRecoveryPolicy
{
    public const int MinimumUnanimousNotableCount = 3;

    public SettlementCultureLegacyRecoveryDecision Evaluate(SettlementCultureLegacyRecoveryFacts facts)
    {
        if (facts == null)
        {
            return Reject("missing_facts");
        }
        if (!facts.IsTown)
        {
            return Reject("not_town");
        }
        if (facts.HasExplicitOverride)
        {
            return Reject("explicit_override_present");
        }
        if (facts.CleanedDeadNotableCount <= 0)
        {
            return Reject("no_legacy_corruption_evidence");
        }
        if (facts.LivingNotableCultureIds.Count < MinimumUnanimousNotableCount)
        {
            return Reject("insufficient_living_notables");
        }

        string candidateCultureId = facts.LivingNotableCultureIds[0];
        if (string.IsNullOrWhiteSpace(candidateCultureId))
        {
            return Reject("missing_notable_culture");
        }
        for (int index = 1; index < facts.LivingNotableCultureIds.Count; index++)
        {
            string cultureId = facts.LivingNotableCultureIds[index];
            if (string.IsNullOrWhiteSpace(cultureId))
            {
                return Reject("missing_notable_culture");
            }
            if (!string.Equals(candidateCultureId, cultureId, StringComparison.OrdinalIgnoreCase))
            {
                return Reject("mixed_notable_cultures");
            }
        }
        if (string.IsNullOrWhiteSpace(facts.CurrentCultureId))
        {
            return Reject("missing_settlement_culture");
        }
        if (string.Equals(candidateCultureId, facts.CurrentCultureId, StringComparison.OrdinalIgnoreCase))
        {
            return Reject("culture_already_current");
        }
        return new SettlementCultureLegacyRecoveryDecision(true, candidateCultureId, "legacy_split_recovered");
    }

    private static SettlementCultureLegacyRecoveryDecision Reject(string reason)
    {
        return new SettlementCultureLegacyRecoveryDecision(false, string.Empty, reason);
    }
}
