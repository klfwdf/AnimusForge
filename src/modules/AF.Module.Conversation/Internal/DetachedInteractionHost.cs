using System;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Channel-neutral host lifecycle for a detached interaction. The channel
/// supplies capture, memory/action facades and a dispatcher that owns the game
/// main thread. This type owns only lifecycle policy: generate once, dispatch
/// one commit, never retry stale/validation decisions, and use the legacy
/// fallback only for failures before the commit callback starts. Once a
/// callback starts, even a failed/missing receipt can hide partial effects.
/// </summary>
public sealed class DetachedInteractionHost
{
    private readonly Func<string, InteractionEnvelope> _capture;
    private readonly Func<InteractionEnvelope, RuntimeConfigSnapshot, string, string, CancellationToken, Task<InteractionResult>> _generate;
    private readonly Func<InteractionEnvelope, InteractionResult, IActionPlanExecutor, IInteractionMemory, bool, InteractionCommitResult> _commit;

    public DetachedInteractionHost(
        Func<string, InteractionEnvelope> capture,
        Func<InteractionEnvelope, RuntimeConfigSnapshot, string, string, CancellationToken, Task<InteractionResult>> generate,
        Func<InteractionEnvelope, InteractionResult, IActionPlanExecutor, IInteractionMemory, bool, InteractionCommitResult> commit)
    {
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _generate = generate ?? throw new ArgumentNullException(nameof(generate));
        _commit = commit ?? throw new ArgumentNullException(nameof(commit));
    }

    public async Task<DetachedInteractionHostResult> ExecuteAsync(
        string playerText,
        RuntimeConfigSnapshot configuration,
        string moduleId,
        string providerId,
        Func<InteractionEnvelope, IActionPlanExecutor> actionExecutorFactory,
        Func<InteractionEnvelope, IInteractionMemory> memoryFactory,
        Func<InteractionEnvelope, Func<InteractionCommitResult>, Task<InteractionCommitResult>> dispatchCommitAsync,
        Func<Task<string>> fallbackToLegacy,
        CancellationToken cancellationToken,
        Action<InteractionEnvelope, InteractionResult, InteractionCommitResult> afterCommit = null,
        bool appendPlayerInput = true)
    {
        InteractionEnvelope envelope;
        try
        {
            // Capture is intentionally synchronous: the facade's channel
            // owner must call this host at its interaction boundary.
            envelope = _capture(playerText);
        }
        catch (Exception exception)
        {
            return await FallbackAsync("capture_" + exception.GetType().Name, fallbackToLegacy).ConfigureAwait(false);
        }

        if (envelope == null)
        {
            return await FallbackAsync("missing_envelope", fallbackToLegacy).ConfigureAwait(false);
        }

        // Both factories are part of the channel's capture boundary. In
        // particular, an action executor may close over a currently valid
        // Agent/target, so creating it after an async LLM request would allow
        // target drift between capture and commit.
        if (memoryFactory == null)
        {
            return await FallbackAsync("missing_memory_factory", fallbackToLegacy).ConfigureAwait(false);
        }
        IActionPlanExecutor actionExecutor;
        IInteractionMemory memory;
        try
        {
            actionExecutor = actionExecutorFactory?.Invoke(envelope);
            memory = memoryFactory(envelope);
        }
        catch (Exception exception)
        {
            return await FallbackAsync("commit_factory_" + exception.GetType().Name, fallbackToLegacy).ConfigureAwait(false);
        }
        if (memory == null)
        {
            return await FallbackAsync("missing_memory", fallbackToLegacy).ConfigureAwait(false);
        }

        InteractionResult result;
        try
        {
            result = await _generate(
                envelope,
                configuration,
                moduleId,
                providerId,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return Terminal(
                InteractionStatus.CancelledAsStale,
                "cancelled",
                null,
                null);
        }
        catch (Exception exception)
        {
            return await FallbackAsync("detached_generate_" + exception.GetType().Name, fallbackToLegacy).ConfigureAwait(false);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Terminal(InteractionStatus.CancelledAsStale, "cancelled_before_commit", result, null);
        }
        if (result == null)
        {
            return await FallbackAsync("detached_null_result", fallbackToLegacy).ConfigureAwait(false);
        }
        if (result.Status == InteractionStatus.CancelledAsStale)
        {
            return Terminal(result.Status, result.ErrorCode, result, null);
        }
        if (result.Status != InteractionStatus.Succeeded)
        {
            return await FallbackAsync(
                string.IsNullOrWhiteSpace(result.ErrorCode) ? "detached_generation_failed" : result.ErrorCode,
                fallbackToLegacy,
                result).ConfigureAwait(false);
        }
        if (dispatchCommitAsync == null)
        {
            return await FallbackAsync("missing_commit_dependencies", fallbackToLegacy, result).ConfigureAwait(false);
        }

        // 0 = pending, 1 = consumed, 2 = closed without starting. Closing the
        // gate also prevents a broken dispatcher from running a queued callback
        // after its task failed/returned and the host selected legacy fallback.
        int commitState = 0;
        InteractionCommitResult observedCommit = null;
        object commitPublicationSync = new object();
        bool acceptCommitPublication = true;
        InteractionCommitResult commit;
        try
        {
            commit = await dispatchCommitAsync(
                envelope,
                () =>
                {
                    int previousState = Interlocked.CompareExchange(ref commitState, 1, 0);
                    if (previousState != 0)
                    {
                        InteractionCommitResult receipt = Volatile.Read(ref observedCommit);
                        return new InteractionCommitResult(
                            InteractionStatus.NonRetryableFailure,
                            receipt?.HistoryWritten ?? false,
                            receipt?.ActionsExecuted ?? false,
                            "commit_callback_closed",
                            receipt?.EffectState ?? (previousState == 1
                                ? ActionExecutionEffectState.UnknownAfterStart
                                : ActionExecutionEffectState.NoConfirmedEffect));
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        var cancelled = new InteractionCommitResult(
                            InteractionStatus.CancelledAsStale, false, false, "cancelled_before_commit");
                        Volatile.Write(ref observedCommit, cancelled);
                        return cancelled;
                    }
                    InteractionCommitResult committed = _commit(
                        envelope,
                        result,
                        actionExecutor,
                        memory,
                        appendPlayerInput);
                    bool published;
                    lock (commitPublicationSync)
                    {
                        published = acceptCommitPublication;
                        if (published)
                        {
                            Volatile.Write(ref observedCommit, committed);
                        }
                    }
                    if (published
                        && committed != null
                        && !committed.IsDuplicate
                        && committed.HistoryWritten
                        && (committed.Status == InteractionStatus.Succeeded
                            || committed.Status == InteractionStatus.Executed))
                    {
                        afterCommit?.Invoke(envelope, result, committed);
                    }
                    return committed;
                })
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (Interlocked.CompareExchange(ref commitState, 2, 0) == 1)
            {
                string errorCode = "main_thread_commit_" + exception.GetType().Name;
                return Terminal(InteractionStatus.NonRetryableFailure,
                    errorCode,
                    result,
                    ObservedOrUnknownCommit(Volatile.Read(ref observedCommit), errorCode));
            }
            if (cancellationToken.IsCancellationRequested || exception is OperationCanceledException)
            {
                return Terminal(InteractionStatus.CancelledAsStale, "cancelled_before_commit", result, null);
            }
            return await FallbackAsync("main_thread_commit_" + exception.GetType().Name, fallbackToLegacy, result).ConfigureAwait(false);
        }
        finally
        {
            lock (commitPublicationSync)
            {
                acceptCommitPublication = false;
            }
            Interlocked.CompareExchange(ref commitState, 2, 0);
        }
        if (Volatile.Read(ref commitState) == 1 && Volatile.Read(ref observedCommit) == null)
        {
            string errorCode = commit == null || string.IsNullOrWhiteSpace(commit.ErrorCode)
                ? "missing_commit_result"
                : commit.ErrorCode;
            return Terminal(
                InteractionStatus.NonRetryableFailure,
                errorCode,
                result,
                ObservedOrUnknownCommit(null, errorCode));
        }
        if (Volatile.Read(ref commitState) == 1)
        {
            InteractionCommitResult observed = Volatile.Read(ref observedCommit);
            bool observedAccepted = IsAccepted(observed);
            bool dispatcherAccepted = IsAccepted(commit);
            if (observedAccepted && !dispatcherAccepted)
            {
                return Terminal(
                    InteractionStatus.NonRetryableFailure,
                    commit == null
                        ? "missing_commit_result"
                        : string.IsNullOrWhiteSpace(commit.ErrorCode) ? "commit_failed" : commit.ErrorCode,
                    result,
                    observed);
            }
            if (!observedAccepted)
            {
                InteractionStatus terminalStatus = observed.Status == InteractionStatus.CancelledAsStale
                    || observed.Status == InteractionStatus.RejectedByValidation
                        ? observed.Status
                        : InteractionStatus.NonRetryableFailure;
                return Terminal(
                    terminalStatus,
                    string.IsNullOrWhiteSpace(observed.ErrorCode) ? "commit_failed" : observed.ErrorCode,
                    result,
                    observed);
            }
            return Terminal(
                observed.Status,
                observed.ErrorCode,
                result,
                observed,
                result.VisibleReply,
                false);
        }
        if (cancellationToken.IsCancellationRequested
            || commit?.Status == InteractionStatus.CancelledAsStale)
        {
            return Terminal(
                InteractionStatus.CancelledAsStale,
                "cancelled_before_commit",
                result,
                commit);
        }
        string noCallbackError = commit == null
            ? "missing_commit_result"
            : IsAccepted(commit)
                ? "commit_callback_not_started"
                : string.IsNullOrWhiteSpace(commit.ErrorCode)
                    ? "commit_failed"
                    : commit.ErrorCode;
        InteractionCommitResult noCallbackReceipt = commit == null
            ? null
            : new InteractionCommitResult(
                InteractionStatus.NonRetryableFailure,
                false,
                false,
                noCallbackError,
                ActionExecutionEffectState.NoConfirmedEffect);
        return await FallbackAsync(
            noCallbackError,
            fallbackToLegacy,
            result,
            noCallbackReceipt).ConfigureAwait(false);
    }

    private static DetachedInteractionHostResult Terminal(
        InteractionStatus status,
        string errorCode,
        InteractionResult result,
        InteractionCommitResult commit,
        string visibleReply = null,
        bool usedLegacyFallback = false)
    {
        return new DetachedInteractionHostResult(
            visibleReply ?? result?.VisibleReply ?? string.Empty,
            usedLegacyFallback,
            status,
            errorCode,
            result,
            commit);
    }

    private static InteractionCommitResult ObservedOrUnknownCommit(
        InteractionCommitResult observed,
        string errorCode)
    {
        return observed ?? new InteractionCommitResult(
            InteractionStatus.NonRetryableFailure,
            false,
            false,
            errorCode,
            ActionExecutionEffectState.UnknownAfterStart);
    }

    private static bool IsAccepted(InteractionCommitResult commit)
    {
        return commit != null
            && (commit.Status == InteractionStatus.Succeeded
                || commit.Status == InteractionStatus.Executed);
    }

    private static async Task<DetachedInteractionHostResult> FallbackAsync(
        string errorCode,
        Func<Task<string>> fallback,
        InteractionResult result = null,
        InteractionCommitResult commit = null)
    {
        if (fallback == null)
        {
            return Terminal(InteractionStatus.NonRetryableFailure, errorCode, result, commit, string.Empty, true);
        }
        try
        {
            return Terminal(
                InteractionStatus.Succeeded,
                errorCode,
                result,
                commit,
                await fallback().ConfigureAwait(false),
                true);
        }
        catch (Exception exception)
        {
            return Terminal(
                InteractionStatus.NonRetryableFailure,
                errorCode + ";legacy_" + exception.GetType().Name,
                result,
                commit,
                string.Empty,
                true);
        }
    }
}

public sealed class DetachedInteractionHostResult
{
    internal DetachedInteractionHostResult(
        string visibleReply,
        bool usedLegacyFallback,
        InteractionStatus status,
        string errorCode,
        InteractionResult detachedResult,
        InteractionCommitResult commit)
    {
        VisibleReply = visibleReply ?? string.Empty;
        UsedLegacyFallback = usedLegacyFallback;
        Status = status;
        ErrorCode = errorCode ?? string.Empty;
        DetachedResult = detachedResult;
        Commit = commit;
    }

    public string VisibleReply { get; }
    public bool UsedLegacyFallback { get; }
    public InteractionStatus Status { get; }
    public string ErrorCode { get; }
    public InteractionResult DetachedResult { get; }
    public InteractionCommitResult Commit { get; }
}
