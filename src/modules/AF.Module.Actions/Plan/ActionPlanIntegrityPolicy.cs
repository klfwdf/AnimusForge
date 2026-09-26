using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Detached raw-to-plan integrity boundary shared by every channel executor.
/// It never resolves or mutates game objects: the channel/domain owner still
/// revalidates live state and performs the actual effect on the main thread.
/// </summary>
internal sealed class ActionPlanIntegrityPolicy
{
    private readonly LegacyActionTagParser _parser;
    private readonly PostprocessContext _rawContext;

    internal ActionPlanIntegrityPolicy(int maxRawActions, IEnumerable<string> allowedTagFamilies)
    {
        IReadOnlyList<string> allowed = (allowedTagFamilies ?? LegacyActionTagCatalog.DefaultAllowedTagFamilies)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
        _parser = new LegacyActionTagParser(Math.Max(1, maxRawActions));
        _rawContext = new PostprocessContext(
            Array.Empty<string>(),
            allowed,
            new CapabilitySet(new[] { "action.parse" }));
    }

    internal bool MatchesAuthorizedPlan(ActionPlan authorizedPlan)
    {
        if (authorizedPlan == null
            || authorizedPlan.Actions.Count == 0
            || string.IsNullOrWhiteSpace(authorizedPlan.RawPostprocessId)
            || _parser.ExceedsActionLimit(authorizedPlan.RawPostprocessId)
            || _parser.HasDisallowedProtocolTag(authorizedPlan.RawPostprocessId, _rawContext))
        {
            return false;
        }

        return PlansMatch(authorizedPlan, _parser.Parse(authorizedPlan.RawPostprocessId, _rawContext));
    }

    internal static bool PlansMatch(ActionPlan expected, ActionPlan actual)
    {
        if (expected == null || actual == null || expected.Actions.Count != actual.Actions.Count)
        {
            return false;
        }
        for (int i = 0; i < expected.Actions.Count; i++)
        {
            if (!RequestsMatch(expected.Actions[i], actual.Actions[i]))
            {
                return false;
            }
        }
        return true;
    }

    private static bool RequestsMatch(ActionRequest expected, ActionRequest actual)
    {
        if (expected == null || actual == null
            || !string.Equals(expected.Tag, actual.Tag, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(expected.TargetId, actual.TargetId, StringComparison.Ordinal)
            || expected.Parameters.Count != actual.Parameters.Count)
        {
            return false;
        }
        foreach (KeyValuePair<string, string> pair in expected.Parameters)
        {
            if (!actual.Parameters.TryGetValue(pair.Key, out string actualValue)
                || !string.Equals(pair.Value, actualValue, StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }
}
