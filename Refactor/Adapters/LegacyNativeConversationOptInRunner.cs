using System;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Executes the Native detached sidecar without taking ownership of game
/// thread state. Capture must already have happened at the interaction
/// boundary. Generate is detached; commit is supplied as a main-thread
/// callback. Any infrastructure failure before a successful commit can fall
/// back to the unchanged Native entry supplied by the caller.
/// </summary>
public sealed class LegacyNativeConversationOptInRunner
{
    private readonly LegacyNativeConversationFacade _facade;

    public LegacyNativeConversationOptInRunner(LegacyNativeConversationFacade facade)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
    }

    public async Task<LegacyNativeConversationOptInResult> ExecuteAsync(
        InteractionEnvelope envelope,
        RuntimeConfigSnapshot configuration,
        string moduleId,
        string providerId,
        Func<InteractionResult, InteractionCommitResult> commitOnMainThread,
        Func<Task<string>> fallbackToLegacyNative,
        CancellationToken cancellationToken)
    {
        if (envelope == null)
        {
            return await FallbackAsync("missing_envelope", fallbackToLegacyNative).ConfigureAwait(false);
        }
        if (commitOnMainThread == null)
        {
            return await FallbackAsync("missing_main_thread_commit", fallbackToLegacyNative).ConfigureAwait(false);
        }

        InteractionResult result;
        try
        {
            result = await _facade.GenerateAsync(
                envelope,
                configuration,
                moduleId,
                providerId,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return new LegacyNativeConversationOptInResult(
                string.Empty,
                false,
                InteractionStatus.CancelledAsStale,
                "cancelled",
                null,
                null);
        }
        catch (Exception exception)
        {
            return await FallbackAsync(
                "detached_generate_" + exception.GetType().Name,
                fallbackToLegacyNative).ConfigureAwait(false);
        }

        if (result == null)
        {
            return await FallbackAsync("detached_null_result", fallbackToLegacyNative).ConfigureAwait(false);
        }
        if (result.Status == InteractionStatus.CancelledAsStale)
        {
            // Never restart a stale request through the old path: the save or
            // conversation generation has already changed.
            return new LegacyNativeConversationOptInResult(
                string.Empty,
                false,
                result.Status,
                string.IsNullOrWhiteSpace(result.ErrorCode) ? "stale" : result.ErrorCode,
                result,
                null);
        }
        if (result.Status != InteractionStatus.Succeeded)
        {
            return await FallbackAsync(
                string.IsNullOrWhiteSpace(result.ErrorCode) ? "detached_generation_failed" : result.ErrorCode,
                fallbackToLegacyNative,
                result).ConfigureAwait(false);
        }

        InteractionCommitResult commit;
        try
        {
            // The runner never invokes this callback from a worker-created
            // game object closure. The channel host owns dispatch to its main
            // thread and performs target/action/memory validation there.
            commit = commitOnMainThread(result);
        }
        catch (Exception exception)
        {
            return await FallbackAsync(
                "main_thread_commit_" + exception.GetType().Name,
                fallbackToLegacyNative,
                result).ConfigureAwait(false);
        }

        if (commit == null)
        {
            return await FallbackAsync("missing_commit_result", fallbackToLegacyNative, result).ConfigureAwait(false);
        }

        // A stale generation or a failed main-thread validation is a terminal
        // decision for this captured interaction. Retrying the old entry here
        // could resolve a different target and duplicate player input; only
        // infrastructure failures are allowed to use the legacy fallback.
        if (commit.Status == InteractionStatus.CancelledAsStale
            || commit.Status == InteractionStatus.RejectedByValidation)
        {
            return new LegacyNativeConversationOptInResult(
                result.VisibleReply,
                false,
                commit.Status,
                string.IsNullOrWhiteSpace(commit.ErrorCode) ? "commit_rejected" : commit.ErrorCode,
                result,
                commit);
        }

        bool commitAccepted = commit.Status == InteractionStatus.Succeeded
            || commit.Status == InteractionStatus.Executed;
        if (!commitAccepted && !commit.HistoryWritten)
        {
            return await FallbackAsync(
                string.IsNullOrWhiteSpace(commit.ErrorCode) ? "commit_rejected" : commit.ErrorCode,
                fallbackToLegacyNative,
                result,
                commit).ConfigureAwait(false);
        }

        return new LegacyNativeConversationOptInResult(
            result.VisibleReply,
            false,
            commit.Status,
            commit.ErrorCode,
            result,
            commit);
    }

    private static async Task<LegacyNativeConversationOptInResult> FallbackAsync(
        string errorCode,
        Func<Task<string>> fallback,
        InteractionResult detachedResult = null,
        InteractionCommitResult commit = null)
    {
        if (fallback == null)
        {
            return new LegacyNativeConversationOptInResult(
                string.Empty,
                true,
                InteractionStatus.NonRetryableFailure,
                errorCode,
                detachedResult,
                commit);
        }

        try
        {
            string text = await fallback().ConfigureAwait(false);
            return new LegacyNativeConversationOptInResult(
                text,
                true,
                InteractionStatus.Succeeded,
                errorCode,
                detachedResult,
                commit);
        }
        catch (Exception exception)
        {
            return new LegacyNativeConversationOptInResult(
                string.Empty,
                true,
                InteractionStatus.NonRetryableFailure,
                errorCode + ";legacy_" + exception.GetType().Name,
                detachedResult,
                commit);
        }
    }
}

public sealed class LegacyNativeConversationOptInResult
{
    internal LegacyNativeConversationOptInResult(
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
