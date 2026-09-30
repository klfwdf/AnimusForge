using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Detached adapter for the existing auxiliary rule-code retriever. It only
/// consumes strings from the immutable snapshot; game objects remain owned by
/// the channel capture/commit boundary.
/// </summary>
public sealed class LegacyDetachedRuleSelector : IRuleSelector
{
    private readonly Func<string, string, string, int, IEnumerable<string>, DetachedRuleLookupResult> _lookup;
    private readonly int _topN;

    public LegacyDetachedRuleSelector(
        Func<string, string, string, int, IEnumerable<string>, DetachedRuleLookupResult> lookup,
        int topN = 12)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
        _topN = Math.Max(1, topN);
    }

    public RuleSelection Select(GameInteractionSnapshot snapshot)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        string secondaryInput = ReadFact(snapshot, "secondary_input");
        string runtimeContext = ReadFact(snapshot, "rule_runtime_context");
        IEnumerable<string> excludedRuleIds = SplitCsv(ReadFact(snapshot, "excluded_rule_ids"));
        DetachedRuleLookupResult lookup = _lookup(
            snapshot.PlayerText,
            secondaryInput,
            runtimeContext,
            _topN,
            excludedRuleIds);
        if (lookup == null)
        {
            return new RuleSelection(Array.Empty<string>(), new[] { "rule_lookup_returned_null" });
        }

        List<string> rules = (lookup.RuleIds ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        List<string> exclusions = SplitCsv(ReadFact(snapshot, "exclusion_reasons")).ToList();
        if (!string.IsNullOrWhiteSpace(lookup.ErrorCode))
        {
            exclusions.Add("rule_lookup_" + lookup.ErrorCode.Trim());
        }
        return new RuleSelection(rules, exclusions);
    }

    private static string ReadFact(GameInteractionSnapshot snapshot, string key)
    {
        return snapshot.DetachedFacts.TryGetValue(key, out string value) ? value ?? string.Empty : string.Empty;
    }

    private static IEnumerable<string> SplitCsv(string value)
    {
        return (value ?? string.Empty)
            .Split(new[] { ',', '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}

public sealed class DetachedRuleLookupResult
{
    public DetachedRuleLookupResult(IEnumerable<string> ruleIds, string errorCode = null)
    {
        RuleIds = (ruleIds ?? Enumerable.Empty<string>()).ToList().AsReadOnly();
        ErrorCode = errorCode ?? string.Empty;
    }

    public IReadOnlyList<string> RuleIds { get; }
    public string ErrorCode { get; }
}
