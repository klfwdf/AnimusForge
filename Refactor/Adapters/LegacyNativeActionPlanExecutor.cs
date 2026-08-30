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
///
/// When an Economy owner is supplied, Economy tags are projected and replayed
/// exactly once through that owner. The legacy callback receives a filtered
/// plan containing only non-Economy tags, so existing action authority is
/// retained without double-mutating rewards or debt.
/// </summary>
public sealed class LegacyNativeActionPlanExecutor : IActionPlanExecutor, IActionPlanExecutionReceipt
{
    private readonly Func<ActionPlan, GameInteractionSnapshot, InteractionStatus> _execute;
    private readonly int _maxRawActions;
    private readonly IReadOnlyList<string> _allowedTagFamilies;
    private readonly IEconomyRewardDebtReplayPlanner _economyPlanner;
    private readonly IEconomyRewardDebtMainThreadPort _economyPort;
    private readonly CapabilitySet _economyCapabilities;
    private IReadOnlyList<FactRecord> _confirmedFacts = Array.Empty<FactRecord>();

    public LegacyNativeActionPlanExecutor(
        Func<ActionPlan, GameInteractionSnapshot, InteractionStatus> execute,
        int maxRawActions = 64,
        IEnumerable<string> allowedTagFamilies = null,
        IEconomyRewardDebtReplayPlanner economyPlanner = null,
        IEconomyRewardDebtMainThreadPort economyPort = null,
        CapabilitySet economyCapabilities = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _maxRawActions = Math.Max(1, maxRawActions);
        _allowedTagFamilies = (allowedTagFamilies ?? LegacyActionTagCatalog.DefaultAllowedTagFamilies)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
        if ((economyPlanner == null) != (economyPort == null))
        {
            throw new ArgumentException("Economy planner and main-thread port must be supplied together.");
        }
        _economyPlanner = economyPlanner;
        _economyPort = economyPort;
        _economyCapabilities = economyCapabilities ?? new CapabilitySet(Array.Empty<string>());
    }

    public IReadOnlyList<FactRecord> ConfirmedFacts => _confirmedFacts;

    public InteractionStatus ValidateAndExecute(
        ActionPlan actionPlan,
        GameInteractionSnapshot currentSnapshot)
    {
        _confirmedFacts = Array.Empty<FactRecord>();
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

            ActionPlan delegatedPlan = actionPlan;
            if (_economyPlanner != null)
            {
                EconomyRewardDebtReplayPlan economyPlan = _economyPlanner.Plan(actionPlan, _economyCapabilities);
                if (economyPlan == null || HasBlockingEconomyExclusion(economyPlan))
                {
                    return InteractionStatus.RejectedByValidation;
                }

                int expectedEconomyActionCount = actionPlan.Actions.Count(LegacyEconomyRewardDebtAdapter.IsEconomyAction);
                if (expectedEconomyActionCount != economyPlan.Actions.Count)
                {
                    return InteractionStatus.RejectedByValidation;
                }

                if (economyPlan.Actions.Count > 0)
                {
                    EconomyRewardDebtReplayResult replay = _economyPort.Replay(economyPlan, currentSnapshot);
                    if (replay == null
                        || replay.Status != EconomyRewardDebtReplayStatus.Applied
                        || replay.AppliedCount != economyPlan.Actions.Count)
                    {
                        return InteractionStatus.RejectedByValidation;
                    }
                    _confirmedFacts = replay.ConfirmedFacts ?? Array.Empty<FactRecord>();
                }

                List<ActionRequest> remainingActions = actionPlan.Actions
                    .Where(request => !LegacyEconomyRewardDebtAdapter.IsEconomyAction(request))
                    .ToList();
                if (remainingActions.Count == 0)
                {
                    return economyPlan.Actions.Count > 0
                        ? InteractionStatus.Executed
                        : InteractionStatus.RejectedByValidation;
                }

                string filteredRaw = LegacyActionTagParser.RemoveProtocolTags(
                    actionPlan.RawPostprocessId,
                    LegacyEconomyRewardDebtAdapter.IsEconomyActionTag);
                delegatedPlan = new ActionPlan(remainingActions, filteredRaw);
            }

            InteractionStatus status = _execute(delegatedPlan, currentSnapshot);
            if (status != InteractionStatus.Executed)
            {
                _confirmedFacts = Array.Empty<FactRecord>();
                return InteractionStatus.RejectedByValidation;
            }
            return status;
        }
        catch
        {
            _confirmedFacts = Array.Empty<FactRecord>();
            // Action execution is a failure-isolated boundary. The caller can
            // still commit the visible exchange without treating an action
            // exception as confirmed gameplay or AFEF.
            return InteractionStatus.RejectedByValidation;
        }
    }

    private static bool HasBlockingEconomyExclusion(EconomyRewardDebtReplayPlan plan)
    {
        return (plan.ExclusionReasons ?? Array.Empty<string>()).Any(reason =>
            !string.IsNullOrWhiteSpace(reason)
            && !reason.StartsWith("economy.action_not_applicable:", StringComparison.OrdinalIgnoreCase));
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