using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Main-thread boundary for the Native ActionPlan. The detached parser keeps
/// the original postprocess text only as a trace; before the host executes it,
/// this adapter parses that trace again with an explicit wildcard allowlist and
/// requires an exact ordered match with the already-authorized ActionPlan.
/// This prevents a caller from smuggling an additional legacy tag through the
/// raw postprocess text. The supplied callback is the channel-owned bridge to
/// the existing game action implementation and must be created and invoked on
/// the game main thread.
/// </summary>
public sealed class LegacyNativeActionPlanExecutor : IActionPlanExecutor
{
    private readonly Func<ActionPlan, GameInteractionSnapshot, InteractionStatus> _execute;
    private readonly int _maxRawActions;
    private readonly IReadOnlyList<string> _allowedTagFamilies;

    public LegacyNativeActionPlanExecutor(
        Func<ActionPlan, GameInteractionSnapshot, InteractionStatus> execute,
        int maxRawActions = 64,
        IEnumerable<string> allowedTagFamilies = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _maxRawActions = Math.Max(1, maxRawActions);
        _allowedTagFamilies = (allowedTagFamilies ?? LegacyActionTagCatalog.DefaultAllowedTagFamilies)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    public InteractionStatus ValidateAndExecute(
        ActionPlan actionPlan,
        GameInteractionSnapshot currentSnapshot)
    {
        if (actionPlan == null || currentSnapshot == null)
        {
            return InteractionStatus.RejectedByValidation;
        }
        if (actionPlan.Actions.Count == 0 || string.IsNullOrWhiteSpace(actionPlan.RawPostprocessId))
        {
            return InteractionStatus.RejectedByValidation;
        }

        try
        {
            LegacyActionTagParser parser = new LegacyActionTagParser(_maxRawActions);
            PostprocessContext rawContext = new PostprocessContext(
                Array.Empty<string>(),
                _allowedTagFamilies,
                new CapabilitySet(new[] { "action.parse" }));
            if (parser.HasDisallowedProtocolTag(actionPlan.RawPostprocessId, rawContext))
            {
                return InteractionStatus.RejectedByValidation;
            }
            ActionPlan rawPlan = parser.Parse(
                actionPlan.RawPostprocessId,
                rawContext);
            if (!PlansMatch(actionPlan, rawPlan))
            {
                return InteractionStatus.RejectedByValidation;
            }

            InteractionStatus status = _execute(actionPlan, currentSnapshot);
            return status == InteractionStatus.Executed
                ? status
                : InteractionStatus.RejectedByValidation;
        }
        catch
        {
            // Action execution is a failure-isolated boundary. The caller can
            // still commit the visible exchange without treating an action
            // exception as confirmed gameplay or AFEF.
            return InteractionStatus.RejectedByValidation;
        }
    }

    private static bool PlansMatch(ActionPlan expected, ActionPlan actual)
    {
        if (expected.Actions.Count != actual.Actions.Count)
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
        if (!string.Equals(expected.Tag, actual.Tag, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(expected.TargetId, actual.TargetId, StringComparison.Ordinal))
        {
            return false;
        }
        if (expected.Parameters.Count != actual.Parameters.Count)
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
