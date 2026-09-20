using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Request-bound compatibility adapter for a channel that still owns its
/// Bannerlord-side action routine. The shared Actions module validates the
/// immutable identity and raw/plan parity; the supplied callback remains the
/// only place allowed to resolve or mutate live game objects.
/// </summary>
internal sealed class LegacyChannelActionPlanExecutor :
    IRequestBoundActionPlanExecutor,
    IActionPlanExecutionEffectReceipt
{
    private readonly InteractionChannel _channel;
    private readonly string _sessionId;
    private readonly string _subjectId;
    private readonly Func<ActionPlan, GameInteractionSnapshot, InteractionStatus> _execute;
    private readonly ActionPlanIntegrityPolicy _integrityPolicy;
    private int _appliedActionCount;
    private string _executionErrorCode = string.Empty;
    private ActionExecutionEffectState _effectState;

    internal LegacyChannelActionPlanExecutor(
        InteractionChannel channel,
        string sessionId,
        string subjectId,
        Func<ActionPlan, GameInteractionSnapshot, InteractionStatus> execute,
        int maxActions = 64,
        IEnumerable<string> allowedTagFamilies = null)
    {
        _channel = channel;
        _sessionId = Required(sessionId, nameof(sessionId));
        _subjectId = Required(subjectId, nameof(subjectId));
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _integrityPolicy = new ActionPlanIntegrityPolicy(maxActions, allowedTagFamilies);
    }

    public IReadOnlyList<FactRecord> ConfirmedFacts => Array.Empty<FactRecord>();
    public int AppliedActionCount => _appliedActionCount;
    public string ExecutionErrorCode => _executionErrorCode;
    public ActionExecutionEffectState EffectState => _effectState;

    public InteractionStatus ValidateAndExecute(
        ActionPlan actionPlan,
        GameInteractionSnapshot currentSnapshot)
    {
        Reset();
        _executionErrorCode = "channel.request_binding_required";
        return InteractionStatus.RejectedByValidation;
    }

    InteractionStatus IRequestBoundActionPlanExecutor.ValidateAndExecute(
        ActionPlan actionPlan,
        GameInteractionSnapshot currentSnapshot,
        string requestId,
        string actionFingerprint)
    {
        Reset();
        if (actionPlan == null
            || actionPlan.Actions.Count == 0
            || currentSnapshot?.Identity == null
            || currentSnapshot.Identity.Channel != _channel
            || !string.Equals(currentSnapshot.Identity.SessionId, _sessionId, StringComparison.Ordinal)
            || !string.Equals(currentSnapshot.Identity.SubjectId, _subjectId, StringComparison.Ordinal))
        {
            _executionErrorCode = "channel.identity_mismatch";
            return InteractionStatus.RejectedByValidation;
        }

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
            _executionErrorCode = "channel.binding_invalid";
            return InteractionStatus.RejectedByValidation;
        }
        if (!string.Equals(requestId, canonicalRequestId, StringComparison.Ordinal))
        {
            _executionErrorCode = "channel.request_mismatch";
            return InteractionStatus.RejectedByValidation;
        }
        if (!string.Equals(actionFingerprint, canonicalActionFingerprint, StringComparison.Ordinal))
        {
            _executionErrorCode = "channel.action_fingerprint_mismatch";
            return InteractionStatus.RejectedByValidation;
        }
        if (!_integrityPolicy.MatchesAuthorizedPlan(actionPlan))
        {
            _executionErrorCode = "channel.action_plan_mismatch";
            return InteractionStatus.RejectedByValidation;
        }

        try
        {
            InteractionStatus status = _execute(actionPlan, currentSnapshot);
            if (status == InteractionStatus.Executed)
            {
                _appliedActionCount = actionPlan.Actions.Count;
                _effectState = ActionExecutionEffectState.ConfirmedEffect;
                return status;
            }
            _executionErrorCode = "channel.action_not_executed";
            return status == InteractionStatus.NonRetryableFailure
                ? status
                : InteractionStatus.RejectedByValidation;
        }
        catch
        {
            _effectState = ActionExecutionEffectState.UnknownAfterStart;
            _executionErrorCode = "channel.action_unknown_after_start";
            throw;
        }
    }

    private void Reset()
    {
        _appliedActionCount = 0;
        _executionErrorCode = string.Empty;
        _effectState = ActionExecutionEffectState.NoConfirmedEffect;
    }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
        return value.Trim();
    }
}
