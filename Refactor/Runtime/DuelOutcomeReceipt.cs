using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AnimusForge.Refactor.Runtime;

internal enum DuelOutcomeState
{
    Rejected = 0,
    Queued = 1,
    Started = 2,
    OutcomeKnown = 3,
    Completed = 4,
    PartiallyCompleted = 5,
    UnknownAfterStart = 6,
    Cancelled = 7
}

internal enum DuelOutcomeEffectState
{
    NotApplicable = 0,
    Confirmed = 1,
    Partial = 2,
    AttemptedUnconfirmed = 3,
    Unknown = 4
}

internal enum DuelOutcomeChannel
{
    SceneShout = 0,
    NativeConversation = 1,
    Courier = 2,
    ProactiveNpc = 3,
    Domain = 4
}

internal enum DuelSessionKind
{
    Meeting = 0,
    Arena = 1,
    Wilderness = 2
}

internal enum DuelResultKind
{
    PlayerWon = 0,
    OpponentWon = 1,
    Draw = 2
}

internal enum DuelOutcomeOperationStatus
{
    Accepted = 0,
    Duplicate = 1,
    NotFound = 2,
    InvalidTransition = 3,
    IdentityConflict = 4,
    CapacityExceeded = 5,
    InvalidIdentity = 6
}

/// <summary>
/// Immutable request identity for one Duel owner entry. The identity contains
/// bounded identifiers and an action digest only; it never retains prompts,
/// replies, callbacks, or game objects.
/// </summary>
internal sealed class DuelOutcomeRequestIdentity
{
    private DuelOutcomeRequestIdentity(
        string duelId,
        string requestId,
        string traceId,
        DuelOutcomeChannel channel,
        string interactionSessionId,
        string subjectId,
        long runtimeGeneration,
        long saveGeneration,
        string actionFingerprint,
        string identityHash)
    {
        DuelId = duelId;
        RequestId = requestId;
        TraceId = traceId;
        Channel = channel;
        InteractionSessionId = interactionSessionId;
        SubjectId = subjectId;
        RuntimeGeneration = runtimeGeneration;
        SaveGeneration = saveGeneration;
        ActionFingerprint = actionFingerprint;
        IdentityHash = identityHash;
    }

    internal string DuelId { get; }
    internal string RequestId { get; }
    internal string TraceId { get; }
    internal DuelOutcomeChannel Channel { get; }
    internal string InteractionSessionId { get; }
    internal string SubjectId { get; }
    internal long RuntimeGeneration { get; }
    internal long SaveGeneration { get; }
    internal string ActionFingerprint { get; }
    internal string IdentityHash { get; }

    internal static bool TryCreate(
        string duelId,
        string requestId,
        string traceId,
        DuelOutcomeChannel channel,
        string interactionSessionId,
        string subjectId,
        long runtimeGeneration,
        long saveGeneration,
        string actionFingerprint,
        out DuelOutcomeRequestIdentity identity,
        out string errorCode)
    {
        identity = null;
        if (!DuelOutcomeFingerprint.TryNormalizeDigest(duelId, out string normalizedDuelId)
            || !DuelOutcomeFingerprint.TryNormalizeToken(requestId, out string normalizedRequestId)
            || !DuelOutcomeFingerprint.TryNormalizeToken(traceId, out string normalizedTraceId)
            || !Enum.IsDefined(typeof(DuelOutcomeChannel), channel)
            || !DuelOutcomeFingerprint.TryNormalizeToken(interactionSessionId, out string normalizedInteractionSessionId)
            || !DuelOutcomeFingerprint.TryNormalizeToken(subjectId, out string normalizedSubjectId)
            || runtimeGeneration < 0
            || saveGeneration < 0
            || !DuelOutcomeFingerprint.TryNormalizeDigest(actionFingerprint, out string normalizedActionFingerprint))
        {
            errorCode = "duel_request_identity_invalid";
            return false;
        }

        string identityHash = DuelOutcomeFingerprint.Hash(
            "AnimusForge.DuelOutcome.Request.v1",
            normalizedDuelId,
            normalizedRequestId,
            normalizedTraceId,
            ((int)channel).ToString(CultureInfo.InvariantCulture),
            normalizedInteractionSessionId,
            normalizedSubjectId,
            runtimeGeneration.ToString(CultureInfo.InvariantCulture),
            saveGeneration.ToString(CultureInfo.InvariantCulture),
            normalizedActionFingerprint);
        identity = new DuelOutcomeRequestIdentity(
            normalizedDuelId,
            normalizedRequestId,
            normalizedTraceId,
            channel,
            normalizedInteractionSessionId,
            normalizedSubjectId,
            runtimeGeneration,
            saveGeneration,
            normalizedActionFingerprint,
            identityHash);
        errorCode = string.Empty;
        return true;
    }

    internal DuelOutcomeRequestIdentity Clone()
        => new DuelOutcomeRequestIdentity(
            DuelId,
            RequestId,
            TraceId,
            Channel,
            InteractionSessionId,
            SubjectId,
            RuntimeGeneration,
            SaveGeneration,
            ActionFingerprint,
            IdentityHash);
}

/// <summary>
/// Immutable identity issued at the actual Duel start seam. DuelSessionId is
/// an opaque digest owned by the runtime adapter and is bound to the request.
/// </summary>
internal sealed class DuelOutcomeStartIdentity
{
    private DuelOutcomeStartIdentity(
        string duelId,
        string requestIdentityHash,
        string duelSessionId,
        DuelSessionKind sessionKind,
        string identityHash)
    {
        DuelId = duelId;
        RequestIdentityHash = requestIdentityHash;
        DuelSessionId = duelSessionId;
        SessionKind = sessionKind;
        IdentityHash = identityHash;
    }

    internal string DuelId { get; }
    internal string RequestIdentityHash { get; }
    internal string DuelSessionId { get; }
    internal DuelSessionKind SessionKind { get; }
    internal string IdentityHash { get; }

    internal static bool TryCreate(
        DuelOutcomeRequestIdentity request,
        string duelSessionId,
        DuelSessionKind sessionKind,
        out DuelOutcomeStartIdentity identity,
        out string errorCode)
    {
        identity = null;
        if (request == null
            || !DuelOutcomeFingerprint.TryNormalizeDigest(request.DuelId, out string normalizedDuelId)
            || !DuelOutcomeFingerprint.TryNormalizeDigest(request.IdentityHash, out string normalizedRequestHash)
            || !DuelOutcomeFingerprint.TryNormalizeDigest(duelSessionId, out string normalizedSessionId)
            || !Enum.IsDefined(typeof(DuelSessionKind), sessionKind))
        {
            errorCode = "duel_start_identity_invalid";
            return false;
        }

        string identityHash = DuelOutcomeFingerprint.Hash(
            "AnimusForge.DuelOutcome.Start.v1",
            normalizedDuelId,
            normalizedRequestHash,
            normalizedSessionId,
            ((int)sessionKind).ToString(CultureInfo.InvariantCulture));
        identity = new DuelOutcomeStartIdentity(
            normalizedDuelId,
            normalizedRequestHash,
            normalizedSessionId,
            sessionKind,
            identityHash);
        errorCode = string.Empty;
        return true;
    }

    internal DuelOutcomeStartIdentity Clone()
        => new DuelOutcomeStartIdentity(
            DuelId,
            RequestIdentityHash,
            DuelSessionId,
            SessionKind,
            IdentityHash);
}

/// <summary>
/// Immutable decisive result identity. ResultId must be generated and retained
/// when the first real result is locked; retries must reuse that same digest.
/// </summary>
internal sealed class DuelOutcomeResultIdentity
{
    private DuelOutcomeResultIdentity(
        string duelId,
        string startIdentityHash,
        string resultId,
        DuelResultKind resultKind,
        string identityHash)
    {
        DuelId = duelId;
        StartIdentityHash = startIdentityHash;
        ResultId = resultId;
        ResultKind = resultKind;
        IdentityHash = identityHash;
    }

    internal string DuelId { get; }
    internal string StartIdentityHash { get; }
    internal string ResultId { get; }
    internal DuelResultKind ResultKind { get; }
    internal string IdentityHash { get; }

    internal static bool TryCreate(
        DuelOutcomeStartIdentity start,
        string resultId,
        DuelResultKind resultKind,
        out DuelOutcomeResultIdentity identity,
        out string errorCode)
    {
        identity = null;
        if (start == null
            || !DuelOutcomeFingerprint.TryNormalizeDigest(start.DuelId, out string normalizedDuelId)
            || !DuelOutcomeFingerprint.TryNormalizeDigest(start.IdentityHash, out string normalizedStartHash)
            || !DuelOutcomeFingerprint.TryNormalizeDigest(resultId, out string normalizedResultId)
            || !Enum.IsDefined(typeof(DuelResultKind), resultKind))
        {
            errorCode = "duel_result_identity_invalid";
            return false;
        }

        string identityHash = DuelOutcomeFingerprint.Hash(
            "AnimusForge.DuelOutcome.Result.v1",
            normalizedDuelId,
            normalizedStartHash,
            normalizedResultId,
            ((int)resultKind).ToString(CultureInfo.InvariantCulture));
        identity = new DuelOutcomeResultIdentity(
            normalizedDuelId,
            normalizedStartHash,
            normalizedResultId,
            resultKind,
            identityHash);
        errorCode = string.Empty;
        return true;
    }

    internal DuelOutcomeResultIdentity Clone()
        => new DuelOutcomeResultIdentity(
            DuelId,
            StartIdentityHash,
            ResultId,
            ResultKind,
            IdentityHash);
}

/// <summary>
/// Fixed, typed component evidence. Free-form component dictionaries are
/// intentionally excluded so unknown effects cannot be omitted silently.
/// </summary>
internal sealed class DuelOutcomeEffects
{
    private DuelOutcomeEffects(
        DuelOutcomeEffectState memory,
        DuelOutcomeEffectState afef,
        DuelOutcomeEffectState death,
        DuelOutcomeEffectState renown,
        DuelOutcomeEffectState stake,
        string identityHash)
    {
        Memory = memory;
        Afef = afef;
        Death = death;
        Renown = renown;
        Stake = stake;
        IdentityHash = identityHash;
    }

    internal DuelOutcomeEffectState Memory { get; }
    internal DuelOutcomeEffectState Afef { get; }
    internal DuelOutcomeEffectState Death { get; }
    internal DuelOutcomeEffectState Renown { get; }
    internal DuelOutcomeEffectState Stake { get; }
    internal string IdentityHash { get; }

    internal bool IsFullyConfirmed
        => IsConfirmedOrNotApplicable(Memory)
            && IsConfirmedOrNotApplicable(Afef)
            && IsConfirmedOrNotApplicable(Death)
            && IsConfirmedOrNotApplicable(Renown)
            && IsConfirmedOrNotApplicable(Stake);

    internal static bool TryCreate(
        DuelOutcomeEffectState memory,
        DuelOutcomeEffectState afef,
        DuelOutcomeEffectState death,
        DuelOutcomeEffectState renown,
        DuelOutcomeEffectState stake,
        out DuelOutcomeEffects effects,
        out string errorCode)
    {
        effects = null;
        if (!IsDefined(memory)
            || !IsDefined(afef)
            || !IsDefined(death)
            || !IsDefined(renown)
            || !IsDefined(stake))
        {
            errorCode = "duel_effect_state_invalid";
            return false;
        }

        string identityHash = DuelOutcomeFingerprint.Hash(
            "AnimusForge.DuelOutcome.Effects.v1",
            ((int)memory).ToString(CultureInfo.InvariantCulture),
            ((int)afef).ToString(CultureInfo.InvariantCulture),
            ((int)death).ToString(CultureInfo.InvariantCulture),
            ((int)renown).ToString(CultureInfo.InvariantCulture),
            ((int)stake).ToString(CultureInfo.InvariantCulture));
        effects = new DuelOutcomeEffects(memory, afef, death, renown, stake, identityHash);
        errorCode = string.Empty;
        return true;
    }

    internal DuelOutcomeEffects Clone()
        => new DuelOutcomeEffects(Memory, Afef, Death, Renown, Stake, IdentityHash);

    private static bool IsDefined(DuelOutcomeEffectState state)
        => Enum.IsDefined(typeof(DuelOutcomeEffectState), state);

    private static bool IsConfirmedOrNotApplicable(DuelOutcomeEffectState state)
        => state == DuelOutcomeEffectState.NotApplicable
            || state == DuelOutcomeEffectState.Confirmed;
}

/// <summary>
/// Immutable process-local readback. This DTO deliberately has no wire format:
/// a process restart loses it and never authorizes replay of Duel side effects.
/// </summary>
internal sealed class DuelOutcomeReceipt
{
    private DuelOutcomeReceipt(
        DuelOutcomeRequestIdentity requestIdentity,
        DuelOutcomeStartIdentity startIdentity,
        DuelOutcomeResultIdentity resultIdentity,
        DuelOutcomeEffects effects,
        DuelOutcomeState state,
        string reasonCode,
        string finalizationHash)
    {
        RequestIdentity = requestIdentity;
        StartIdentity = startIdentity;
        ResultIdentity = resultIdentity;
        Effects = effects;
        State = state;
        ReasonCode = reasonCode ?? string.Empty;
        FinalizationHash = finalizationHash ?? string.Empty;
    }

    internal string DuelId => RequestIdentity?.DuelId ?? string.Empty;
    internal DuelOutcomeRequestIdentity RequestIdentity { get; }
    internal DuelOutcomeStartIdentity StartIdentity { get; }
    internal DuelOutcomeResultIdentity ResultIdentity { get; }
    internal DuelOutcomeEffects Effects { get; }
    internal DuelOutcomeState State { get; }
    internal string ReasonCode { get; }
    internal string FinalizationHash { get; }
    internal bool IsTerminal => IsTerminalState(State);

    internal static DuelOutcomeReceipt CreateQueued(DuelOutcomeRequestIdentity request)
        => new DuelOutcomeReceipt(
            request.Clone(),
            null,
            null,
            null,
            DuelOutcomeState.Queued,
            string.Empty,
            string.Empty);

    internal static DuelOutcomeReceipt CreateRejected(
        DuelOutcomeRequestIdentity request,
        string reasonCode)
        => new DuelOutcomeReceipt(
            request.Clone(),
            null,
            null,
            null,
            DuelOutcomeState.Rejected,
            reasonCode,
            DuelOutcomeFingerprint.Hash(
                "AnimusForge.DuelOutcome.Rejected.v1",
                request.IdentityHash,
                reasonCode));

    internal DuelOutcomeReceipt WithStart(DuelOutcomeStartIdentity start)
        => new DuelOutcomeReceipt(
            RequestIdentity.Clone(),
            start.Clone(),
            null,
            null,
            DuelOutcomeState.Started,
            string.Empty,
            string.Empty);

    internal DuelOutcomeReceipt WithResult(DuelOutcomeResultIdentity result)
        => new DuelOutcomeReceipt(
            RequestIdentity.Clone(),
            StartIdentity.Clone(),
            result.Clone(),
            null,
            DuelOutcomeState.OutcomeKnown,
            string.Empty,
            string.Empty);

    internal DuelOutcomeReceipt WithFinalEffects(
        DuelOutcomeResultIdentity result,
        DuelOutcomeEffects effects,
        string finalizationHash)
        => new DuelOutcomeReceipt(
            RequestIdentity.Clone(),
            StartIdentity.Clone(),
            result.Clone(),
            effects.Clone(),
            effects.IsFullyConfirmed
                ? DuelOutcomeState.Completed
                : DuelOutcomeState.PartiallyCompleted,
            string.Empty,
            finalizationHash);

    internal DuelOutcomeReceipt WithTerminalState(
        DuelOutcomeState state,
        string reasonCode,
        string finalizationHash)
        => new DuelOutcomeReceipt(
            RequestIdentity.Clone(),
            StartIdentity?.Clone(),
            ResultIdentity?.Clone(),
            Effects?.Clone(),
            state,
            reasonCode,
            finalizationHash);

    internal DuelOutcomeReceipt Clone()
        => new DuelOutcomeReceipt(
            RequestIdentity?.Clone(),
            StartIdentity?.Clone(),
            ResultIdentity?.Clone(),
            Effects?.Clone(),
            State,
            ReasonCode,
            FinalizationHash);

    internal static bool IsTerminalState(DuelOutcomeState state)
        => state == DuelOutcomeState.Rejected
            || state == DuelOutcomeState.Completed
            || state == DuelOutcomeState.PartiallyCompleted
            || state == DuelOutcomeState.UnknownAfterStart
            || state == DuelOutcomeState.Cancelled;
}

/// <summary>
/// Thread-safe, bounded, process-local owner for Duel receipts. Accepted
/// entries are retained for this owner lifetime so one DuelId can reach a
/// final outcome only once. Capacity is reserved before any live side effect;
/// there are no callbacks, persistence hooks, eviction replays, or game types.
/// </summary>
internal sealed class DuelOutcomeOwner
{
    internal const int DefaultActiveCapacity = 64;
    internal const int DefaultTotalCapacity = 512;

    private readonly object _sync = new object();
    private readonly Dictionary<string, DuelOutcomeReceipt> _entries =
        new Dictionary<string, DuelOutcomeReceipt>(StringComparer.Ordinal);
    private readonly int _activeCapacity;
    private readonly int _totalCapacity;
    private int _activeCount;
    private int _terminalCount;

    internal DuelOutcomeOwner(
        int activeCapacity = DefaultActiveCapacity,
        int totalCapacity = DefaultTotalCapacity)
    {
        if (activeCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(activeCapacity));
        }
        if (totalCapacity < activeCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCapacity));
        }

        _activeCapacity = activeCapacity;
        _totalCapacity = totalCapacity;
    }

    internal int ActiveCount
    {
        get
        {
            lock (_sync)
            {
                return _activeCount;
            }
        }
    }

    internal int TerminalCount
    {
        get
        {
            lock (_sync)
            {
                return _terminalCount;
            }
        }
    }

    internal int RetainedCount
    {
        get
        {
            lock (_sync)
            {
                return _entries.Count;
            }
        }
    }

    internal DuelOutcomeOperationStatus Queue(
        DuelOutcomeRequestIdentity request,
        out DuelOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        if (!IsValid(request))
        {
            errorCode = "duel_request_identity_invalid";
            return DuelOutcomeOperationStatus.InvalidIdentity;
        }

        lock (_sync)
        {
            if (_entries.TryGetValue(request.DuelId, out DuelOutcomeReceipt existing))
            {
                return ExistingRequestResult(existing, request, out receipt, out errorCode);
            }
            if (_entries.Count >= _totalCapacity)
            {
                errorCode = "duel_outcome_total_capacity";
                return DuelOutcomeOperationStatus.CapacityExceeded;
            }
            if (_activeCount >= _activeCapacity)
            {
                errorCode = "duel_outcome_active_capacity";
                return DuelOutcomeOperationStatus.CapacityExceeded;
            }

            DuelOutcomeReceipt queued = DuelOutcomeReceipt.CreateQueued(request);
            _entries.Add(request.DuelId, queued);
            _activeCount++;
            receipt = queued.Clone();
            errorCode = string.Empty;
            return DuelOutcomeOperationStatus.Accepted;
        }
    }

    internal DuelOutcomeOperationStatus Reject(
        DuelOutcomeRequestIdentity request,
        string reasonCode,
        out DuelOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        if (!IsValid(request)
            || !DuelOutcomeFingerprint.TryNormalizeReasonCode(reasonCode, out string normalizedReason))
        {
            errorCode = "duel_rejection_identity_invalid";
            return DuelOutcomeOperationStatus.InvalidIdentity;
        }

        lock (_sync)
        {
            if (!_entries.TryGetValue(request.DuelId, out DuelOutcomeReceipt existing))
            {
                if (_entries.Count >= _totalCapacity)
                {
                    errorCode = "duel_outcome_total_capacity";
                    return DuelOutcomeOperationStatus.CapacityExceeded;
                }

                DuelOutcomeReceipt rejected = DuelOutcomeReceipt.CreateRejected(request, normalizedReason);
                _entries.Add(request.DuelId, rejected);
                _terminalCount++;
                receipt = rejected.Clone();
                errorCode = string.Empty;
                return DuelOutcomeOperationStatus.Accepted;
            }
            if (!SameRequest(existing, request))
            {
                return Conflict(existing, out receipt, out errorCode);
            }
            if (existing.State == DuelOutcomeState.Rejected)
            {
                return SameTerminalReason(existing, normalizedReason, out receipt, out errorCode);
            }
            if (existing.State != DuelOutcomeState.Queued)
            {
                return InvalidTransition(existing, out receipt, out errorCode);
            }

            DuelOutcomeReceipt rejectedQueued = DuelOutcomeReceipt.CreateRejected(request, normalizedReason);
            _entries[request.DuelId] = rejectedQueued;
            MoveToTerminal();
            receipt = rejectedQueued.Clone();
            errorCode = string.Empty;
            return DuelOutcomeOperationStatus.Accepted;
        }
    }

    internal DuelOutcomeOperationStatus Start(
        DuelOutcomeStartIdentity start,
        out DuelOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        if (!IsValid(start))
        {
            errorCode = "duel_start_identity_invalid";
            return DuelOutcomeOperationStatus.InvalidIdentity;
        }

        lock (_sync)
        {
            if (!_entries.TryGetValue(start.DuelId, out DuelOutcomeReceipt existing))
            {
                return NotFound(out receipt, out errorCode);
            }
            if (!string.Equals(
                    existing.RequestIdentity.IdentityHash,
                    start.RequestIdentityHash,
                    StringComparison.Ordinal))
            {
                return Conflict(existing, out receipt, out errorCode);
            }
            if (existing.StartIdentity != null)
            {
                if (!string.Equals(existing.StartIdentity.IdentityHash, start.IdentityHash, StringComparison.Ordinal))
                {
                    return Conflict(existing, out receipt, out errorCode);
                }
                receipt = existing.Clone();
                errorCode = "duel_outcome_duplicate";
                return DuelOutcomeOperationStatus.Duplicate;
            }
            if (existing.State != DuelOutcomeState.Queued)
            {
                return InvalidTransition(existing, out receipt, out errorCode);
            }

            DuelOutcomeReceipt started = existing.WithStart(start);
            _entries[start.DuelId] = started;
            receipt = started.Clone();
            errorCode = string.Empty;
            return DuelOutcomeOperationStatus.Accepted;
        }
    }

    internal DuelOutcomeOperationStatus RecordOutcome(
        DuelOutcomeResultIdentity result,
        out DuelOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        if (!IsValid(result))
        {
            errorCode = "duel_result_identity_invalid";
            return DuelOutcomeOperationStatus.InvalidIdentity;
        }

        lock (_sync)
        {
            if (!_entries.TryGetValue(result.DuelId, out DuelOutcomeReceipt existing))
            {
                return NotFound(out receipt, out errorCode);
            }
            if (existing.StartIdentity == null)
            {
                return InvalidTransition(existing, out receipt, out errorCode);
            }
            if (!string.Equals(existing.StartIdentity.IdentityHash, result.StartIdentityHash, StringComparison.Ordinal))
            {
                return Conflict(existing, out receipt, out errorCode);
            }
            if (existing.ResultIdentity != null)
            {
                if (!string.Equals(existing.ResultIdentity.IdentityHash, result.IdentityHash, StringComparison.Ordinal))
                {
                    return Conflict(existing, out receipt, out errorCode);
                }
                receipt = existing.Clone();
                errorCode = "duel_outcome_duplicate";
                return DuelOutcomeOperationStatus.Duplicate;
            }
            if (existing.State != DuelOutcomeState.Started)
            {
                return InvalidTransition(existing, out receipt, out errorCode);
            }

            DuelOutcomeReceipt outcomeKnown = existing.WithResult(result);
            _entries[result.DuelId] = outcomeKnown;
            receipt = outcomeKnown.Clone();
            errorCode = string.Empty;
            return DuelOutcomeOperationStatus.Accepted;
        }
    }

    internal DuelOutcomeOperationStatus Finalize(
        DuelOutcomeResultIdentity result,
        DuelOutcomeEffects effects,
        out DuelOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        if (!IsValid(result) || !IsValid(effects))
        {
            errorCode = "duel_finalization_identity_invalid";
            return DuelOutcomeOperationStatus.InvalidIdentity;
        }

        string finalizationHash = DuelOutcomeFingerprint.Hash(
            "AnimusForge.DuelOutcome.Finalization.v1",
            result.IdentityHash,
            effects.IdentityHash);
        lock (_sync)
        {
            if (!_entries.TryGetValue(result.DuelId, out DuelOutcomeReceipt existing))
            {
                return NotFound(out receipt, out errorCode);
            }
            if (existing.StartIdentity == null || existing.ResultIdentity == null)
            {
                return InvalidTransition(existing, out receipt, out errorCode);
            }
            if (!string.Equals(existing.StartIdentity.IdentityHash, result.StartIdentityHash, StringComparison.Ordinal)
                || !string.Equals(existing.ResultIdentity.IdentityHash, result.IdentityHash, StringComparison.Ordinal))
            {
                return Conflict(existing, out receipt, out errorCode);
            }
            if (existing.State == DuelOutcomeState.Completed
                || existing.State == DuelOutcomeState.PartiallyCompleted)
            {
                if (!string.Equals(existing.FinalizationHash, finalizationHash, StringComparison.Ordinal))
                {
                    return Conflict(existing, out receipt, out errorCode);
                }
                receipt = existing.Clone();
                errorCode = "duel_outcome_duplicate";
                return DuelOutcomeOperationStatus.Duplicate;
            }
            if (existing.State != DuelOutcomeState.OutcomeKnown)
            {
                return InvalidTransition(existing, out receipt, out errorCode);
            }

            DuelOutcomeReceipt finalized = existing.WithFinalEffects(result, effects, finalizationHash);
            _entries[result.DuelId] = finalized;
            MoveToTerminal();
            receipt = finalized.Clone();
            errorCode = string.Empty;
            return DuelOutcomeOperationStatus.Accepted;
        }
    }

    internal DuelOutcomeOperationStatus Cancel(
        DuelOutcomeRequestIdentity request,
        string reasonCode,
        out DuelOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        if (!IsValid(request)
            || !DuelOutcomeFingerprint.TryNormalizeReasonCode(reasonCode, out string normalizedReason))
        {
            errorCode = "duel_cancellation_identity_invalid";
            return DuelOutcomeOperationStatus.InvalidIdentity;
        }

        lock (_sync)
        {
            if (!_entries.TryGetValue(request.DuelId, out DuelOutcomeReceipt existing))
            {
                return NotFound(out receipt, out errorCode);
            }
            if (!SameRequest(existing, request))
            {
                return Conflict(existing, out receipt, out errorCode);
            }
            if (existing.State == DuelOutcomeState.Cancelled)
            {
                return SameTerminalReason(existing, normalizedReason, out receipt, out errorCode);
            }
            if (existing.State != DuelOutcomeState.Queued)
            {
                return InvalidTransition(existing, out receipt, out errorCode);
            }

            string finalizationHash = DuelOutcomeFingerprint.Hash(
                "AnimusForge.DuelOutcome.Cancelled.v1",
                request.IdentityHash,
                normalizedReason);
            DuelOutcomeReceipt cancelled = existing.WithTerminalState(
                DuelOutcomeState.Cancelled,
                normalizedReason,
                finalizationHash);
            _entries[request.DuelId] = cancelled;
            MoveToTerminal();
            receipt = cancelled.Clone();
            errorCode = string.Empty;
            return DuelOutcomeOperationStatus.Accepted;
        }
    }

    internal DuelOutcomeOperationStatus MarkUnknownAfterStart(
        DuelOutcomeStartIdentity start,
        string reasonCode,
        out DuelOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        if (!IsValid(start)
            || !DuelOutcomeFingerprint.TryNormalizeReasonCode(reasonCode, out string normalizedReason))
        {
            errorCode = "duel_unknown_identity_invalid";
            return DuelOutcomeOperationStatus.InvalidIdentity;
        }

        lock (_sync)
        {
            if (!_entries.TryGetValue(start.DuelId, out DuelOutcomeReceipt existing))
            {
                return NotFound(out receipt, out errorCode);
            }
            if (!string.Equals(
                    existing.RequestIdentity.IdentityHash,
                    start.RequestIdentityHash,
                    StringComparison.Ordinal))
            {
                return Conflict(existing, out receipt, out errorCode);
            }
            if (existing.StartIdentity == null)
            {
                return InvalidTransition(existing, out receipt, out errorCode);
            }
            if (!string.Equals(existing.StartIdentity.IdentityHash, start.IdentityHash, StringComparison.Ordinal))
            {
                return Conflict(existing, out receipt, out errorCode);
            }
            if (existing.State == DuelOutcomeState.UnknownAfterStart)
            {
                return SameTerminalReason(existing, normalizedReason, out receipt, out errorCode);
            }
            if (existing.State != DuelOutcomeState.Started
                && existing.State != DuelOutcomeState.OutcomeKnown)
            {
                return InvalidTransition(existing, out receipt, out errorCode);
            }

            string finalizationHash = DuelOutcomeFingerprint.Hash(
                "AnimusForge.DuelOutcome.UnknownAfterStart.v1",
                start.IdentityHash,
                existing.ResultIdentity?.IdentityHash ?? string.Empty,
                normalizedReason);
            DuelOutcomeReceipt unknown = existing.WithTerminalState(
                DuelOutcomeState.UnknownAfterStart,
                normalizedReason,
                finalizationHash);
            _entries[start.DuelId] = unknown;
            MoveToTerminal();
            receipt = unknown.Clone();
            errorCode = string.Empty;
            return DuelOutcomeOperationStatus.Accepted;
        }
    }

    internal bool TryGet(string duelId, out DuelOutcomeReceipt receipt)
    {
        receipt = null;
        if (!DuelOutcomeFingerprint.TryNormalizeDigest(duelId, out string normalizedDuelId))
        {
            return false;
        }

        lock (_sync)
        {
            if (!_entries.TryGetValue(normalizedDuelId, out DuelOutcomeReceipt existing))
            {
                return false;
            }
            receipt = existing.Clone();
            return true;
        }
    }

    private void MoveToTerminal()
    {
        _activeCount--;
        _terminalCount++;
    }

    private static DuelOutcomeOperationStatus ExistingRequestResult(
        DuelOutcomeReceipt existing,
        DuelOutcomeRequestIdentity request,
        out DuelOutcomeReceipt receipt,
        out string errorCode)
    {
        if (!SameRequest(existing, request))
        {
            return Conflict(existing, out receipt, out errorCode);
        }
        receipt = existing.Clone();
        errorCode = "duel_outcome_duplicate";
        return DuelOutcomeOperationStatus.Duplicate;
    }

    private static DuelOutcomeOperationStatus SameTerminalReason(
        DuelOutcomeReceipt existing,
        string reasonCode,
        out DuelOutcomeReceipt receipt,
        out string errorCode)
    {
        if (!string.Equals(existing.ReasonCode, reasonCode, StringComparison.Ordinal))
        {
            return Conflict(existing, out receipt, out errorCode);
        }
        receipt = existing.Clone();
        errorCode = "duel_outcome_duplicate";
        return DuelOutcomeOperationStatus.Duplicate;
    }

    private static DuelOutcomeOperationStatus Conflict(
        DuelOutcomeReceipt existing,
        out DuelOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = existing?.Clone();
        errorCode = "duel_outcome_identity_conflict";
        return DuelOutcomeOperationStatus.IdentityConflict;
    }

    private static DuelOutcomeOperationStatus InvalidTransition(
        DuelOutcomeReceipt existing,
        out DuelOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = existing?.Clone();
        errorCode = "duel_outcome_invalid_transition";
        return DuelOutcomeOperationStatus.InvalidTransition;
    }

    private static DuelOutcomeOperationStatus NotFound(
        out DuelOutcomeReceipt receipt,
        out string errorCode)
    {
        receipt = null;
        errorCode = "duel_outcome_not_found";
        return DuelOutcomeOperationStatus.NotFound;
    }

    private static bool SameRequest(
        DuelOutcomeReceipt receipt,
        DuelOutcomeRequestIdentity request)
        => receipt?.RequestIdentity != null
            && string.Equals(receipt.RequestIdentity.IdentityHash, request.IdentityHash, StringComparison.Ordinal);

    private static bool IsValid(DuelOutcomeRequestIdentity request)
        => request != null
            && DuelOutcomeFingerprint.IsDigest(request.DuelId)
            && DuelOutcomeFingerprint.IsDigest(request.IdentityHash)
            && DuelOutcomeFingerprint.IsDigest(request.ActionFingerprint);

    private static bool IsValid(DuelOutcomeStartIdentity start)
        => start != null
            && DuelOutcomeFingerprint.IsDigest(start.DuelId)
            && DuelOutcomeFingerprint.IsDigest(start.RequestIdentityHash)
            && DuelOutcomeFingerprint.IsDigest(start.DuelSessionId)
            && DuelOutcomeFingerprint.IsDigest(start.IdentityHash)
            && Enum.IsDefined(typeof(DuelSessionKind), start.SessionKind);

    private static bool IsValid(DuelOutcomeResultIdentity result)
        => result != null
            && DuelOutcomeFingerprint.IsDigest(result.DuelId)
            && DuelOutcomeFingerprint.IsDigest(result.StartIdentityHash)
            && DuelOutcomeFingerprint.IsDigest(result.ResultId)
            && DuelOutcomeFingerprint.IsDigest(result.IdentityHash)
            && Enum.IsDefined(typeof(DuelResultKind), result.ResultKind);

    private static bool IsValid(DuelOutcomeEffects effects)
        => effects != null
            && DuelOutcomeFingerprint.IsDigest(effects.IdentityHash)
            && Enum.IsDefined(typeof(DuelOutcomeEffectState), effects.Memory)
            && Enum.IsDefined(typeof(DuelOutcomeEffectState), effects.Afef)
            && Enum.IsDefined(typeof(DuelOutcomeEffectState), effects.Death)
            && Enum.IsDefined(typeof(DuelOutcomeEffectState), effects.Renown)
            && Enum.IsDefined(typeof(DuelOutcomeEffectState), effects.Stake);
}

internal static class DuelOutcomeFingerprint
{
    internal const int DigestLength = 64;
    internal const int MaximumIdentityTokenLength = 256;
    internal const int MaximumTokenLength = MaximumIdentityTokenLength;
    internal const int MaximumReasonCodeLength = 128;

    internal static string Hash(params string[] values)
    {
        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(values?.Length ?? 0);
                if (values != null)
                {
                    for (int index = 0; index < values.Length; index++)
                    {
                        writer.Write(values[index] ?? string.Empty);
                    }
                }
                writer.Flush();
            }

            using (SHA256 sha = SHA256.Create())
            {
                byte[] digest = sha.ComputeHash(stream.ToArray());
                var text = new StringBuilder(DigestLength);
                for (int index = 0; index < digest.Length; index++)
                {
                    text.Append(digest[index].ToString("X2", CultureInfo.InvariantCulture));
                }
                return text.ToString();
            }
        }
    }

    internal static bool TryNormalizeDigest(string value, out string normalized)
    {
        normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        return IsDigest(normalized);
    }

    internal static bool IsDigest(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length != DigestLength)
        {
            return false;
        }
        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];
            bool hexadecimal = (current >= '0' && current <= '9')
                || (current >= 'A' && current <= 'F')
                || (current >= 'a' && current <= 'f');
            if (!hexadecimal)
            {
                return false;
            }
        }
        return true;
    }

    internal static bool TryNormalizeToken(string value, out string normalized)
    {
        normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0 || normalized.Length > MaximumIdentityTokenLength)
        {
            return false;
        }
        for (int index = 0; index < normalized.Length; index++)
        {
            if (char.IsControl(normalized[index]))
            {
                return false;
            }
        }
        return true;
    }

    internal static bool TryNormalizeReasonCode(string value, out string normalized)
    {
        normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length == 0 || normalized.Length > MaximumReasonCodeLength)
        {
            return false;
        }
        for (int index = 0; index < normalized.Length; index++)
        {
            char current = normalized[index];
            bool allowed = (current >= 'a' && current <= 'z')
                || (current >= '0' && current <= '9')
                || current == '_'
                || current == '-'
                || current == '.';
            if (!allowed)
            {
                return false;
            }
        }
        return true;
    }
}
