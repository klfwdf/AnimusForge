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
/// retained without double-mutating rewards or debt. A channel owner may also
/// supply a gate that runs after pure planning but before the first Economy
/// side effect; Courier uses it for live session validation and economy-only
/// persistent consumption.
/// </summary>
public sealed class LegacyNativeActionPlanExecutor : IActionPlanExecutor, IActionPlanExecutionEffectReceipt
{
    private readonly Func<ActionPlan, GameInteractionSnapshot, InteractionStatus> _execute;
    private readonly int _maxRawActions;
    private readonly IReadOnlyList<string> _allowedTagFamilies;
    private readonly IEconomyRewardDebtReplayPlanner _economyPlanner;
    private readonly IEconomyRewardDebtMainThreadPort _economyPort;
    private readonly CapabilitySet _economyCapabilities;
    private readonly Func<ActionPlan, GameInteractionSnapshot, bool, InteractionStatus> _economyExecutionGate;
    private IReadOnlyList<FactRecord> _confirmedFacts = Array.Empty<FactRecord>();
    private int _appliedActionCount;
    private string _executionErrorCode = string.Empty;
    private ActionExecutionEffectState _effectState;

    public LegacyNativeActionPlanExecutor(
        Func<ActionPlan, GameInteractionSnapshot, InteractionStatus> execute,
        int maxRawActions = 64,
        IEnumerable<string> allowedTagFamilies = null,
        IEconomyRewardDebtReplayPlanner economyPlanner = null,
        IEconomyRewardDebtMainThreadPort economyPort = null,
        CapabilitySet economyCapabilities = null)
        : this(execute, maxRawActions, allowedTagFamilies, economyPlanner, economyPort, economyCapabilities, null)
    {
    }

    public LegacyNativeActionPlanExecutor(
        Func<ActionPlan, GameInteractionSnapshot, InteractionStatus> execute,
        int maxRawActions,
        IEnumerable<string> allowedTagFamilies,
        IEconomyRewardDebtReplayPlanner economyPlanner,
        IEconomyRewardDebtMainThreadPort economyPort,
        CapabilitySet economyCapabilities,
        Func<ActionPlan, GameInteractionSnapshot, bool, InteractionStatus> economyExecutionGate)
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
        _economyExecutionGate = economyExecutionGate;
    }

    public IReadOnlyList<FactRecord> ConfirmedFacts => _confirmedFacts;
    public int AppliedActionCount => _appliedActionCount;
    public string ExecutionErrorCode => _executionErrorCode;
    public ActionExecutionEffectState EffectState => _effectState;

    public InteractionStatus ValidateAndExecute(
        ActionPlan actionPlan,
        GameInteractionSnapshot currentSnapshot)
    {
        ResetExecutionOutcome();
        bool ownerCallbackInFlight = false;
        string ownerCallbackErrorCode = string.Empty;
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

                List<ActionRequest> remainingActions = actionPlan.Actions
                    .Where(request => !LegacyEconomyRewardDebtAdapter.IsEconomyAction(request))
                    .ToList();
                if (economyPlan.Actions.Count > 0)
                {
                    bool isEconomyOnly = remainingActions.Count == 0;
                    if (_economyExecutionGate != null)
                    {
                        ownerCallbackInFlight = true;
                        ownerCallbackErrorCode = "economy.execution_gate_exception";
                        InteractionStatus gateStatus = _economyExecutionGate(
                            actionPlan,
                            currentSnapshot,
                            isEconomyOnly);
                        ownerCallbackInFlight = false;
                        ownerCallbackErrorCode = string.Empty;
                        if (gateStatus != InteractionStatus.Executed)
                        {
                            return InteractionStatus.RejectedByValidation;
                        }
                    }
                    ownerCallbackInFlight = true;
                    ownerCallbackErrorCode = "economy.replay_exception";
                    EconomyRewardDebtReplayResult replay = _economyPort.Replay(economyPlan, currentSnapshot);
                    ownerCallbackInFlight = false;
                    ownerCallbackErrorCode = string.Empty;
                    if (replay == null)
                    {
                        _effectState = ActionExecutionEffectState.UnknownAfterStart;
                        _executionErrorCode = "economy.replay_null_result";
                        return InteractionStatus.NonRetryableFailure;
                    }
                    bool hasKnownEffects = (replay.Status == EconomyRewardDebtReplayStatus.Applied
                        || replay.Status == EconomyRewardDebtReplayStatus.PartiallyApplied)
                        && replay.AppliedCount > 0;
                    if (replay.Status == EconomyRewardDebtReplayStatus.UnknownAfterStart)
                    {
                        if (replay.AppliedCount > 0)
                        {
                            _appliedActionCount = replay.AppliedCount;
                            _confirmedFacts = replay.ConfirmedFacts ?? Array.Empty<FactRecord>();
                        }
                        _effectState = ActionExecutionEffectState.UnknownAfterStart;
                        _executionErrorCode = string.IsNullOrWhiteSpace(replay.ErrorCode)
                            ? "economy.unknown_after_start"
                            : replay.ErrorCode;
                        return InteractionStatus.NonRetryableFailure;
                    }
                    if (hasKnownEffects)
                    {
                        _appliedActionCount = replay.AppliedCount;
                        _confirmedFacts = replay.ConfirmedFacts ?? Array.Empty<FactRecord>();
                        _effectState = ActionExecutionEffectState.ConfirmedEffect;
                    }
                    if (replay.Status != EconomyRewardDebtReplayStatus.Applied
                        || replay.AppliedCount != economyPlan.Actions.Count)
                    {
                        if (_appliedActionCount > 0)
                        {
                            _executionErrorCode = string.IsNullOrWhiteSpace(replay.ErrorCode)
                                ? "economy.partial_replay"
                                : replay.ErrorCode;
                            return InteractionStatus.NonRetryableFailure;
                        }
                        ResetExecutionOutcome();
                        return InteractionStatus.RejectedByValidation;
                    }
                }

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

            ownerCallbackInFlight = true;
            ownerCallbackErrorCode = "legacy.action_executor_exception";
            InteractionStatus status = _execute(delegatedPlan, currentSnapshot);
            ownerCallbackInFlight = false;
            ownerCallbackErrorCode = string.Empty;
            if (status != InteractionStatus.Executed)
            {
                if (_appliedActionCount > 0)
                {
                    _executionErrorCode = "economy.applied_before_legacy_rejection";
                    return InteractionStatus.NonRetryableFailure;
                }
                ResetExecutionOutcome();
                return InteractionStatus.RejectedByValidation;
            }
            return status;
        }
        catch
        {
            if (ownerCallbackInFlight)
            {
                _effectState = ActionExecutionEffectState.UnknownAfterStart;
                _executionErrorCode = _appliedActionCount > 0
                    ? "economy.applied_before_executor_exception"
                    : string.IsNullOrWhiteSpace(ownerCallbackErrorCode)
                        ? "action_owner_exception"
                        : ownerCallbackErrorCode;
                return InteractionStatus.NonRetryableFailure;
            }
            if (_appliedActionCount > 0)
            {
                _executionErrorCode = "economy.applied_before_pipeline_exception";
                return InteractionStatus.NonRetryableFailure;
            }
            ResetExecutionOutcome();
            // Action execution is a failure-isolated boundary. The caller can
            // still commit the visible exchange without treating an action
            // exception as confirmed gameplay or AFEF.
            return InteractionStatus.RejectedByValidation;
        }
    }

    private void ResetExecutionOutcome()
    {
        _confirmedFacts = Array.Empty<FactRecord>();
        _appliedActionCount = 0;
        _executionErrorCode = string.Empty;
        _effectState = ActionExecutionEffectState.NoConfirmedEffect;
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
