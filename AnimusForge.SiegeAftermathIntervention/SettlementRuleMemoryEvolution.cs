using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>Bounded, immutable confirmed facts and revision/cadence state for one tenure.</summary>
public sealed class SettlementRuleMemoryEvolution
{
    public const int MaximumEvents = 12;
    public const int MinimumRefreshDays = 7;
    public const int AccumulationDays = 1;
    public const int AccumulatedChanges = 3;
    private readonly IReadOnlyList<SettlementRuleMemoryFact> _facts;

    public SettlementRuleMemoryEvolution(long revision = 1, long generatedRevision = 1,
        int lastGeneratedDay = -1, int pendingSinceDay = -1,
        IEnumerable<SettlementRuleMemoryFact> facts = null)
    {
        Revision = Math.Max(1, revision);
        GeneratedRevision = Math.Max(0, Math.Min(Revision, generatedRevision));
        LastGeneratedDay = Math.Max(-1, lastGeneratedDay);
        PendingSinceDay = Math.Max(-1, pendingSinceDay);
        _facts = Array.AsReadOnly((facts ?? Array.Empty<SettlementRuleMemoryFact>())
            .Where(f => f != null && f.Id.Length > 0 && f.Text.Length > 0)
            .Reverse().Take(MaximumEvents).Reverse().ToArray());
    }

    public long Revision { get; }
    public long GeneratedRevision { get; }
    public int LastGeneratedDay { get; }
    public int PendingSinceDay { get; }
    public IReadOnlyList<SettlementRuleMemoryFact> Facts => _facts;

    public SettlementRuleMemoryEvolution Changed(int day, IEnumerable<SettlementRuleMemoryFact> facts = null)
        => new SettlementRuleMemoryEvolution(Revision + 1, GeneratedRevision, LastGeneratedDay,
            PendingSinceDay < 0 ? Math.Max(0, day) : PendingSinceDay, facts ?? Facts);

    public SettlementRuleMemoryEvolution Generated(int day)
        => new SettlementRuleMemoryEvolution(Revision, Revision, Math.Max(0, day), -1, Facts);

    public static bool ShouldGenerate(SettlementRuleMemoryRecord record, int day, bool force = false)
    {
        var rule = record?.CurrentRule;
        if (rule == null || rule.NarrativeIsManual) return false;
        if (force || string.IsNullOrWhiteSpace(rule.Narrative)) return true;
        var state = rule.Evolution;
        return state.Revision > state.GeneratedRevision
            && (state.LastGeneratedDay < 0 || day - state.LastGeneratedDay >= MinimumRefreshDays)
            && (state.Revision - state.GeneratedRevision >= AccumulatedChanges
                || (state.PendingSinceDay >= 0 && day - state.PendingSinceDay >= AccumulationDays));
    }
}

public sealed class SettlementRuleMemoryFact
{
    public SettlementRuleMemoryFact(string id, string text, int day)
    {
        Id = Clean(id, 180);
        Text = Clean(text, 480);
        Day = Math.Max(0, day);
    }
    public string Id { get; }
    public string Text { get; }
    public int Day { get; }
    private static string Clean(string text, int length)
    {
        string value = (text ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
        return value.Length <= length ? value : value.Substring(0, length);
    }
}
