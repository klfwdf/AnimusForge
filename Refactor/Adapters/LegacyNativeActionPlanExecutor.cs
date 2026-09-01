using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

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
public sealed class LegacyNativeActionPlanExecutor : IActionPlanExecutor, IRequestBoundActionPlanExecutor,
    IActionPlanExecutionEffectReceipt, IDetachedDuelDispatchExecutionReceipt,
    IWeeklyMemoryMaterialCandidateSource, IWeeklyMemoryMaterialExecutionReceipt
{
    private static readonly LegacyEconomyRewardDebtAdapter WeeklyMaterialCanonicalPlanner =
        new LegacyEconomyRewardDebtAdapter();

    private readonly Func<ActionPlan, GameInteractionSnapshot, InteractionStatus> _execute;
    private readonly Func<ActionPlan, GameInteractionSnapshot, DetachedDuelDispatchContext, InteractionStatus>
        _requestBoundExecute;
    private readonly IDetachedDuelDispatchOwner _duelDispatchOwner;
    private readonly int _maxRawActions;
    private readonly IReadOnlyList<string> _allowedTagFamilies;
    private readonly IEconomyRewardDebtReplayPlanner _economyPlanner;
    private readonly IEconomyRewardDebtMainThreadPort _economyPort;
    private readonly CapabilitySet _economyCapabilities;
    private readonly Func<ActionPlan, GameInteractionSnapshot, bool, InteractionStatus> _economyExecutionGate;
    private IReadOnlyList<FactRecord> _confirmedFacts = Array.Empty<FactRecord>();
    private int _appliedActionCount;
    private string _executionErrorCode = string.Empty;
    private string _confirmedWeeklyMaterialActionFingerprint = string.Empty;
    private ActionExecutionEffectState _effectState;
    private DetachedDuelDispatchReceipt _duelDispatchReceipt;
    private bool _duelCompanionEffectUncertain;

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
        : this(
            execute,
            maxRawActions,
            allowedTagFamilies,
            economyPlanner,
            economyPort,
            economyCapabilities,
            economyExecutionGate,
            null,
            null)
    {
    }

    private LegacyNativeActionPlanExecutor(
        Func<ActionPlan, GameInteractionSnapshot, InteractionStatus> execute,
        int maxRawActions,
        IEnumerable<string> allowedTagFamilies,
        IEconomyRewardDebtReplayPlanner economyPlanner,
        IEconomyRewardDebtMainThreadPort economyPort,
        CapabilitySet economyCapabilities,
        Func<ActionPlan, GameInteractionSnapshot, bool, InteractionStatus> economyExecutionGate,
        Func<ActionPlan, GameInteractionSnapshot, DetachedDuelDispatchContext, InteractionStatus> requestBoundExecute,
        IDetachedDuelDispatchOwner duelDispatchOwner)
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
        _requestBoundExecute = requestBoundExecute;
        _duelDispatchOwner = duelDispatchOwner;
    }

    internal static LegacyNativeActionPlanExecutor CreateRequestBoundDuelExecutor(
        Func<ActionPlan, GameInteractionSnapshot, DetachedDuelDispatchContext, InteractionStatus> execute,
        IDetachedDuelDispatchOwner duelDispatchOwner,
        int maxRawActions = 64,
        IEnumerable<string> allowedTagFamilies = null,
        IEconomyRewardDebtReplayPlanner economyPlanner = null,
        IEconomyRewardDebtMainThreadPort economyPort = null,
        CapabilitySet economyCapabilities = null,
        Func<ActionPlan, GameInteractionSnapshot, bool, InteractionStatus> economyExecutionGate = null)
    {
        if (execute == null)
        {
            throw new ArgumentNullException(nameof(execute));
        }
        if (duelDispatchOwner == null)
        {
            throw new ArgumentNullException(nameof(duelDispatchOwner));
        }
        return new LegacyNativeActionPlanExecutor(
            (plan, snapshot) => execute(plan, snapshot, null),
            maxRawActions,
            allowedTagFamilies,
            economyPlanner,
            economyPort,
            economyCapabilities,
            economyExecutionGate,
            execute,
            duelDispatchOwner);
    }

    public IReadOnlyList<FactRecord> ConfirmedFacts => _confirmedFacts;
    public int AppliedActionCount => _appliedActionCount;
    public string ExecutionErrorCode => _executionErrorCode;
    public ActionExecutionEffectState EffectState => _effectState;
    internal DetachedDuelDispatchReceipt DuelDispatchReceipt => _duelDispatchReceipt?.Clone();
    DetachedDuelDispatchReceipt IDetachedDuelDispatchExecutionReceipt.DuelDispatchReceipt
        => _duelDispatchReceipt?.Clone();
    string IWeeklyMemoryMaterialExecutionReceipt.ConfirmedWeeklyMaterialActionFingerprint
        => _confirmedWeeklyMaterialActionFingerprint;

    bool IWeeklyMemoryMaterialCandidateSource.TryCreateWeeklyMaterialCandidate(
        ActionPlan actionPlan,
        GameInteractionSnapshot snapshot,
        string requestId,
        out WeeklyMemoryMaterialOutcomeCandidate candidate)
    {
        candidate = null;
        if (actionPlan == null
            || snapshot == null
            || string.IsNullOrWhiteSpace(requestId)
            || actionPlan.Actions.Count == 0
            || actionPlan.Actions.Count > _maxRawActions
            || _economyPlanner == null
            || _economyPort == null
            || actionPlan.Actions.Any(action => !LegacyEconomyRewardDebtAdapter.IsEconomyAction(action)))
        {
            return false;
        }

        try
        {
            // Candidate projection must not consume the injected gameplay
            // planner. The actual planner is invoked exactly once by
            // ValidateAndExecute; its execution fingerprint is compared with
            // this canonical, data-only projection before weekly publication.
            EconomyRewardDebtReplayPlan economyPlan = WeeklyMaterialCanonicalPlanner.Plan(
                actionPlan,
                _economyCapabilities);
            if (economyPlan == null
                || economyPlan.Actions.Count != actionPlan.Actions.Count
                || economyPlan.ExclusionReasons.Count != 0)
            {
                return false;
            }
            return WeeklyMemoryMaterialFingerprintHelper.TryCreateCandidate(
                requestId,
                snapshot,
                economyPlan,
                out candidate,
                out _);
        }
        catch
        {
            // Weekly material is an optional, data-only sidecar. Candidate
            // projection must never block or execute the gameplay ActionPlan.
            candidate = null;
            return false;
        }
    }

    public InteractionStatus ValidateAndExecute(
        ActionPlan actionPlan,
        GameInteractionSnapshot currentSnapshot)
        => ValidateAndExecuteCore(actionPlan, currentSnapshot, string.Empty, string.Empty);

    InteractionStatus IRequestBoundActionPlanExecutor.ValidateAndExecute(
        ActionPlan actionPlan,
        GameInteractionSnapshot currentSnapshot,
        string requestId,
        string actionFingerprint)
        => ValidateAndExecuteCore(actionPlan, currentSnapshot, requestId, actionFingerprint);

    private InteractionStatus ValidateAndExecuteCore(
        ActionPlan actionPlan,
        GameInteractionSnapshot currentSnapshot,
        string requestId,
        string actionFingerprint)
    {
        ResetExecutionOutcome();
        bool ownerCallbackInFlight = false;
        string ownerCallbackErrorCode = string.Empty;
        DetachedDuelDispatchContext exactDuelContext = null;
        if (actionPlan == null || currentSnapshot == null)
        {
            return InteractionStatus.RejectedByValidation;
        }
        if (actionPlan.Actions.Count == 0 || string.IsNullOrWhiteSpace(actionPlan.RawPostprocessId))
        {
            return InteractionStatus.RejectedByValidation;
        }

        bool hasRequestBinding = !string.IsNullOrWhiteSpace(requestId)
            || !string.IsNullOrWhiteSpace(actionFingerprint);
        if (hasRequestBinding)
        {
            string canonicalRequestId;
            string canonicalActionFingerprint;
            try
            {
                canonicalRequestId = InteractionResultCommitter.BuildCanonicalRequestId(currentSnapshot);
                canonicalActionFingerprint =
                    InteractionResultCommitter.BuildCanonicalActionPlanFingerprint(actionPlan);
            }
            catch
            {
                _executionErrorCode = "duel.dispatch_binding_invalid";
                return InteractionStatus.RejectedByValidation;
            }
            if (!string.Equals(requestId, canonicalRequestId, StringComparison.Ordinal))
            {
                _executionErrorCode = "duel.dispatch_request_mismatch";
                return InteractionStatus.RejectedByValidation;
            }
            if (!string.Equals(
                actionFingerprint,
                canonicalActionFingerprint,
                StringComparison.Ordinal))
            {
                _executionErrorCode = "duel.dispatch_action_fingerprint_mismatch";
                return InteractionStatus.RejectedByValidation;
            }
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
            string economyOnlyActionFingerprint = string.Empty;
            EconomyRewardDebtReplayPlan economyPlan = null;
            List<ActionRequest> remainingActions = actionPlan.Actions.ToList();
            if (_economyPlanner != null)
            {
                economyPlan = _economyPlanner.Plan(actionPlan, _economyCapabilities);
                if (economyPlan == null || HasBlockingEconomyExclusion(economyPlan))
                {
                    return InteractionStatus.RejectedByValidation;
                }

                int expectedEconomyActionCount = actionPlan.Actions.Count(LegacyEconomyRewardDebtAdapter.IsEconomyAction);
                if (expectedEconomyActionCount != economyPlan.Actions.Count)
                {
                    return InteractionStatus.RejectedByValidation;
                }

                remainingActions = actionPlan.Actions
                    .Where(request => !LegacyEconomyRewardDebtAdapter.IsEconomyAction(request))
                    .ToList();
                if (remainingActions.Count > 0)
                {
                    string filteredRaw = LegacyActionTagParser.RemoveProtocolTags(
                        actionPlan.RawPostprocessId,
                        LegacyEconomyRewardDebtAdapter.IsEconomyActionTag);
                    delegatedPlan = new ActionPlan(remainingActions, filteredRaw);
                }
            }

            int duelActionCount = delegatedPlan.Actions.Count(IsDeferredDuelAction);
            bool exactDispatchEnabled = duelActionCount > 0
                && _requestBoundExecute != null
                && _duelDispatchOwner != null
                && !string.IsNullOrWhiteSpace(requestId);
            if (exactDispatchEnabled)
            {
                string contextError = "duel.dispatch_channel_invalid";
                if (!TryMapDuelChannel(currentSnapshot.Identity?.Channel, out DuelOutcomeChannel duelChannel)
                    || !DetachedDuelDispatchContext.TryCreate(
                        requestId,
                        currentSnapshot.Trace?.TraceId,
                        duelChannel,
                        currentSnapshot.Identity?.SessionId,
                        currentSnapshot.Identity?.SubjectId,
                        currentSnapshot.Trace?.RuntimeGeneration ?? -1L,
                        currentSnapshot.Trace?.SaveGeneration ?? -1L,
                        actionFingerprint,
                        out exactDuelContext,
                        out contextError))
                {
                    _executionErrorCode = string.IsNullOrWhiteSpace(contextError)
                        ? "duel.dispatch_identity_invalid"
                        : contextError;
                    return InteractionStatus.RejectedByValidation;
                }
                if (duelActionCount != 1)
                {
                    _duelDispatchOwner.Reject(exactDuelContext, "multiple_duel_actions");
                    CaptureDuelDispatchReceipt(exactDuelContext);
                    _executionErrorCode = "duel.multiple_duel_actions";
                    return InteractionStatus.RejectedByValidation;
                }
                if (delegatedPlan.Actions.Any(action => !IsDuelProtocolAction(action)))
                {
                    _duelDispatchOwner.Reject(exactDuelContext, "mixed_legacy_unsupported");
                    CaptureDuelDispatchReceipt(exactDuelContext);
                    _executionErrorCode = "duel.mixed_legacy_unsupported";
                    return InteractionStatus.RejectedByValidation;
                }
				_duelCompanionEffectUncertain = delegatedPlan.Actions.Any(IsDuelCompanionAction);
                if (currentSnapshot.Identity?.Channel == InteractionChannel.Courier)
                {
                    exactDuelContext.MarkRejected("duel.unsupported_channel");
                    _duelDispatchOwner.Reject(exactDuelContext, "unsupported_channel");
                    CaptureDuelDispatchReceipt(exactDuelContext);
                    _executionErrorCode = "duel.unsupported_channel";
                    return InteractionStatus.RejectedByValidation;
                }
                if (!_duelDispatchOwner.TryQueue(
                    exactDuelContext,
                    out bool shouldDispatch,
                    out string queueError))
                {
                    CaptureDuelDispatchReceipt(exactDuelContext);
                    _executionErrorCode = string.IsNullOrWhiteSpace(queueError)
                        ? _duelDispatchReceipt?.ErrorCode ?? "duel.dispatch_queue_rejected"
                        : queueError;
                    return InteractionStatus.RejectedByValidation;
                }
                CaptureDuelDispatchReceipt(exactDuelContext);
                if (!shouldDispatch)
                {
                    return ResolveDuplicateDuelDispatchStatus();
                }
            }

            if (economyPlan != null && economyPlan.Actions.Count > 0)
            {
                bool isEconomyOnly = remainingActions.Count == 0;
                if (isEconomyOnly)
                {
                    WeeklyMemoryMaterialFingerprintHelper.TryBuildActionFingerprint(
                        economyPlan,
                        out economyOnlyActionFingerprint,
                        out _);
                }
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
                        CancelUnstartedDuelDispatch(exactDuelContext, "economy_gate_rejected");
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
                    CancelUnstartedDuelDispatch(exactDuelContext, "economy_replay_missing");
                    _effectState = ActionExecutionEffectState.UnknownAfterStart;
                    _executionErrorCode = "economy.replay_null_result";
                    return InteractionStatus.NonRetryableFailure;
                }
                bool hasKnownEffects = (replay.Status == EconomyRewardDebtReplayStatus.Applied
                    || replay.Status == EconomyRewardDebtReplayStatus.PartiallyApplied)
                    && replay.AppliedCount > 0;
                if (replay.Status == EconomyRewardDebtReplayStatus.UnknownAfterStart)
                {
                    CancelUnstartedDuelDispatch(exactDuelContext, "economy_unknown_before_duel");
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
                    CancelUnstartedDuelDispatch(exactDuelContext, "economy_incomplete_before_duel");
                    if (_appliedActionCount > 0)
                    {
                        _executionErrorCode = string.IsNullOrWhiteSpace(replay.ErrorCode)
                            ? "economy.partial_replay"
                            : replay.ErrorCode;
                        return InteractionStatus.NonRetryableFailure;
                    }
                    ResetExecutionOutcome(keepDuelDispatchReceipt: true);
                    return InteractionStatus.RejectedByValidation;
                }
            }

            if (remainingActions.Count == 0)
            {
                if (economyPlan == null || economyPlan.Actions.Count <= 0)
                {
                    return InteractionStatus.RejectedByValidation;
                }
                _confirmedWeeklyMaterialActionFingerprint = economyOnlyActionFingerprint;
                return InteractionStatus.Executed;
            }

            ownerCallbackInFlight = true;
            ownerCallbackErrorCode = "legacy.action_executor_exception";
            InteractionStatus status = exactDuelContext == null
                ? _execute(delegatedPlan, currentSnapshot)
                : _requestBoundExecute(delegatedPlan, currentSnapshot, exactDuelContext);
            ownerCallbackInFlight = false;
            ownerCallbackErrorCode = string.Empty;
            if (exactDuelContext != null)
            {
                DetachedDuelDispatchReceipt exactReceipt = exactDuelContext.Snapshot();
                if (exactReceipt?.State == DetachedDuelDispatchState.Queued
                    && !exactReceipt.HostAccepted)
                {
                    _duelDispatchOwner.Reject(exactDuelContext, "legacy_not_dispatched");
                }
                CaptureDuelDispatchReceipt(exactDuelContext);
				return ResolveExactDuelDispatchStatus(status, exactDuelContext);
            }
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
            if (delegatedPlan.Actions.Any(IsDeferredDuelAction))
            {
                // The legacy Duel callback can reject, queue, start or later
                // abort after it consumes the tag. Its synchronous return is
                // not a terminal gameplay receipt. Keep the commit terminal
                // and non-retryable without inventing Duel facts or allowing
                // the host to replay a Mission-start side effect.
                _effectState = ActionExecutionEffectState.UnknownAfterStart;
                _executionErrorCode = "duel.outcome_pending";
                return InteractionStatus.NonRetryableFailure;
            }
            return status;
        }
        catch
        {
            if (exactDuelContext != null
                && ownerCallbackInFlight
                && ownerCallbackErrorCode.StartsWith("economy.", StringComparison.Ordinal))
            {
                CancelUnstartedDuelDispatch(exactDuelContext, "economy_exception_before_duel");
                _effectState = ActionExecutionEffectState.UnknownAfterStart;
                _executionErrorCode = ownerCallbackErrorCode;
                return InteractionStatus.NonRetryableFailure;
            }
            if (exactDuelContext != null)
            {
                DetachedDuelDispatchReceipt exactReceipt = exactDuelContext.Snapshot();
				if (exactDuelContext.SideEffectBoundaryCrossed
					|| exactReceipt?.State == DetachedDuelDispatchState.Started
                    || exactReceipt?.State == DetachedDuelDispatchState.UnknownAfterStart)
                {
                    _duelDispatchOwner.MarkUnknownAfterStart(
                        exactDuelContext,
                        "dispatch_callback_exception");
                    CaptureDuelDispatchReceipt(exactDuelContext);
                    _effectState = ActionExecutionEffectState.UnknownAfterStart;
                    _executionErrorCode = "duel.dispatch_exception_after_start";
                    return InteractionStatus.NonRetryableFailure;
                }
                if (ownerCallbackInFlight && _duelCompanionEffectUncertain)
                {
                    _duelDispatchOwner.Reject(
                        exactDuelContext,
                        "companion_effect_exception");
                    CaptureDuelDispatchReceipt(exactDuelContext);
                    _effectState = ActionExecutionEffectState.UnknownAfterStart;
                    _executionErrorCode = "duel.companion_effect_unknown";
                    return InteractionStatus.NonRetryableFailure;
                }
                if (exactReceipt?.State == DetachedDuelDispatchState.Queued
                    && exactReceipt.HostAccepted)
                {
                    CaptureDuelDispatchReceipt(exactDuelContext);
                    _effectState = _appliedActionCount > 0
                        ? ActionExecutionEffectState.ConfirmedEffect
                        : ActionExecutionEffectState.NoConfirmedEffect;
                    _executionErrorCode = "duel.dispatch_queued";
                    return InteractionStatus.NonRetryableFailure;
                }
                _duelDispatchOwner.Reject(exactDuelContext, "dispatch_callback_exception");
                CaptureDuelDispatchReceipt(exactDuelContext);
                if (_appliedActionCount > 0)
                {
                    _executionErrorCode = "duel.dispatch_rejected_after_economy";
                    return InteractionStatus.NonRetryableFailure;
                }
                _executionErrorCode = _duelDispatchReceipt?.ErrorCode
                    ?? "duel.dispatch_callback_exception";
                return InteractionStatus.RejectedByValidation;
            }
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

	private InteractionStatus ResolveExactDuelDispatchStatus(
		InteractionStatus callbackStatus,
		DetachedDuelDispatchContext context)
    {
        DetachedDuelDispatchReceipt receipt = _duelDispatchReceipt;
        if (receipt == null)
        {
            _executionErrorCode = "duel.dispatch_receipt_missing";
            return InteractionStatus.RejectedByValidation;
        }
        if (receipt.State == DetachedDuelDispatchState.Started)
        {
            _effectState = ActionExecutionEffectState.UnknownAfterStart;
            _executionErrorCode = "duel.dispatch_started";
            return InteractionStatus.NonRetryableFailure;
        }
        if (receipt.State == DetachedDuelDispatchState.UnknownAfterStart)
        {
            _effectState = ActionExecutionEffectState.UnknownAfterStart;
            _executionErrorCode = string.IsNullOrWhiteSpace(receipt.ErrorCode)
                ? "duel.dispatch_unknown_after_start"
                : receipt.ErrorCode;
            return InteractionStatus.NonRetryableFailure;
        }
        if (receipt.State == DetachedDuelDispatchState.Queued && receipt.HostAccepted)
        {
			if (context?.SideEffectBoundaryCrossed == true)
			{
				_effectState = ActionExecutionEffectState.UnknownAfterStart;
				_executionErrorCode = "duel.dispatch_host_side_effect_pending";
				return InteractionStatus.NonRetryableFailure;
			}
            if (_duelCompanionEffectUncertain)
            {
                _effectState = ActionExecutionEffectState.UnknownAfterStart;
                _executionErrorCode = "duel.dispatch_queued_companion_effect_unknown";
                return InteractionStatus.NonRetryableFailure;
            }
            _effectState = _appliedActionCount > 0
                ? ActionExecutionEffectState.ConfirmedEffect
                : ActionExecutionEffectState.NoConfirmedEffect;
            _executionErrorCode = "duel.dispatch_queued";
            return InteractionStatus.NonRetryableFailure;
        }

        if (_duelCompanionEffectUncertain
			&& callbackStatus == InteractionStatus.Executed)
		{
			_effectState = ActionExecutionEffectState.UnknownAfterStart;
			_executionErrorCode = "duel.companion_effect_unknown";
			return InteractionStatus.NonRetryableFailure;
		}

        _executionErrorCode = string.IsNullOrWhiteSpace(receipt.ErrorCode)
            ? "duel.dispatch_rejected"
            : receipt.ErrorCode;
        if (_appliedActionCount > 0)
        {
            return InteractionStatus.NonRetryableFailure;
        }
        _effectState = ActionExecutionEffectState.NoConfirmedEffect;
        _duelCompanionEffectUncertain = false;
        return callbackStatus == InteractionStatus.CancelledAsStale
            ? InteractionStatus.CancelledAsStale
            : InteractionStatus.RejectedByValidation;
    }

    private InteractionStatus ResolveDuplicateDuelDispatchStatus()
    {
        DetachedDuelDispatchReceipt receipt = _duelDispatchReceipt;
        if (receipt?.State == DetachedDuelDispatchState.UnknownAfterStart)
        {
            _effectState = ActionExecutionEffectState.UnknownAfterStart;
            _executionErrorCode = string.IsNullOrWhiteSpace(receipt.ErrorCode)
                ? "duel.dispatch_duplicate_unknown_after_start"
                : receipt.ErrorCode;
            return InteractionStatus.NonRetryableFailure;
        }
        if (receipt?.State == DetachedDuelDispatchState.Started)
        {
            _effectState = ActionExecutionEffectState.UnknownAfterStart;
            _executionErrorCode = "duel.dispatch_duplicate_started";
            return InteractionStatus.NonRetryableFailure;
        }
        if (receipt?.State == DetachedDuelDispatchState.Queued)
        {
            _effectState = ActionExecutionEffectState.NoConfirmedEffect;
            _executionErrorCode = "duel.dispatch_duplicate_queued";
            return InteractionStatus.NonRetryableFailure;
        }
        _effectState = ActionExecutionEffectState.NoConfirmedEffect;
        _executionErrorCode = !string.IsNullOrWhiteSpace(receipt?.ErrorCode)
            ? receipt.ErrorCode
            : "duel.dispatch_duplicate_rejected";
        return InteractionStatus.RejectedByValidation;
    }

    private void CancelUnstartedDuelDispatch(
        DetachedDuelDispatchContext context,
        string reasonCode)
    {
        if (context == null || _duelDispatchOwner == null)
        {
            return;
        }
        DetachedDuelDispatchReceipt receipt = context.Snapshot();
        if (receipt?.State == DetachedDuelDispatchState.Queued)
        {
            _duelDispatchOwner.Cancel(context, reasonCode);
            CaptureDuelDispatchReceipt(context);
        }
    }

    private void CaptureDuelDispatchReceipt(DetachedDuelDispatchContext context)
    {
        _duelDispatchReceipt = context?.Snapshot()?.Clone();
    }

    private void ResetExecutionOutcome(bool keepDuelDispatchReceipt = false)
    {
        _confirmedFacts = Array.Empty<FactRecord>();
        _appliedActionCount = 0;
        _executionErrorCode = string.Empty;
        _confirmedWeeklyMaterialActionFingerprint = string.Empty;
        _effectState = ActionExecutionEffectState.NoConfirmedEffect;
        _duelCompanionEffectUncertain = false;
        if (!keepDuelDispatchReceipt)
        {
            _duelDispatchReceipt = null;
        }
    }

    private static bool HasBlockingEconomyExclusion(EconomyRewardDebtReplayPlan plan)
    {
        return (plan.ExclusionReasons ?? Array.Empty<string>()).Any(reason =>
            !string.IsNullOrWhiteSpace(reason)
            && !reason.StartsWith("economy.action_not_applicable:", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDeferredDuelAction(ActionRequest request)
        => request != null
            && string.Equals(request.Tag, "ACTION:DUEL", StringComparison.OrdinalIgnoreCase);

    private static bool IsDuelProtocolAction(ActionRequest request)
        => request != null
            && (string.Equals(request.Tag, "ACTION:DUEL", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.Tag, "ACTION:MOOD", StringComparison.OrdinalIgnoreCase)
                || (request.Tag ?? string.Empty).StartsWith(
                    "ACTION:DUEL_",
                    StringComparison.OrdinalIgnoreCase));

    private static bool IsDuelCompanionAction(ActionRequest request)
        => request != null
            && string.Equals(request.Tag, "ACTION:MOOD", StringComparison.OrdinalIgnoreCase);

    private static bool TryMapDuelChannel(
        InteractionChannel? channel,
        out DuelOutcomeChannel duelChannel)
    {
        switch (channel)
        {
            case InteractionChannel.SceneShout:
                duelChannel = DuelOutcomeChannel.SceneShout;
                return true;
            case InteractionChannel.NativeConversation:
                duelChannel = DuelOutcomeChannel.NativeConversation;
                return true;
            case InteractionChannel.Courier:
                duelChannel = DuelOutcomeChannel.Courier;
                return true;
            case InteractionChannel.ProactiveNpc:
                duelChannel = DuelOutcomeChannel.ProactiveNpc;
                return true;
            case InteractionChannel.Domain:
                duelChannel = DuelOutcomeChannel.Domain;
                return true;
            default:
                duelChannel = DuelOutcomeChannel.Domain;
                return false;
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
