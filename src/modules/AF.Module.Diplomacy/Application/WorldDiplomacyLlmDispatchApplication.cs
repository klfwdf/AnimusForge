using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IWorldDiplomacyLlmDispatchSource
{
    bool IsEnabled { get; }
    bool IsRequestRunning { get; }
    WorldDiplomacyStorage Storage { get; }
    int CurrentHour { get; }
    string LastCacheAffinityKey { get; }
    void SetLastCacheAffinityKey(string value);
    bool HasStaleThreatPresentation(WorldDiplomacyJob job);
    bool RefreshThreatPresentation(WorldDiplomacyJob job);
    bool HasStaleActionPresentation(WorldDiplomacyJob job);
    bool RefreshActionPresentation(WorldDiplomacyJob job);
    bool RebuildPendingJob(WorldDiplomacyJob job);
    string GetAuthorBlockReason(WorldDiplomacyJob job);
    void AbandonGeneration(WorldDiplomacyJob job, string reason);
    bool EnsureStrategicProfile(WorldDiplomacyJob job);
    string GetLlmConfigError();
    bool TryConsumeRequestBudget(bool consume);
    void CaptureCanonicalHistory(WorldDiplomacyJob job);
    JArray BuildMessageArray(WorldDiplomacyJob job);
    long InputTokenLimit { get; }
    int HistoryCompressionTargetTokens { get; }
    int EstimateTokens(string text);
    string BuildHistoryBlock(long throughSequence);
    void ScheduleTokenCompression();
    void CommitFailedJob(WorldDiplomacyJob job, string error);
    void RemoveJob(string jobId);
    void Log(string message);
    bool TryClaim(string jobId, long generation, int maxTokens, int timeoutMilliseconds, out WorldDiplomacyRequestSnapshot request);
    void MarkRunning(WorldDiplomacyJob job, string cacheAffinityKey);
    void LogPromptCacheShape(WorldDiplomacyJob job);
    void StartRequest(WorldDiplomacyRequestSnapshot request, JArray messages);
    long RuntimeGeneration { get; }
    int DefaultApiTimeoutMilliseconds { get; }
    int CompressionTimeoutMilliseconds { get; }
}

// Owns runnable-job qualification, prompt preparation, budget admission and
// request claiming. The Behavior remains an identity/main-thread/transport adapter.
internal static class WorldDiplomacyLlmDispatchApplication
{
    internal static void Run<TSource>(ref TSource source)
        where TSource : struct, IWorldDiplomacyLlmDispatchSource
    {
        if (!source.IsEnabled || source.IsRequestRunning || source.Storage?.Jobs?.Count == 0) return;
        WorldDiplomacyJob job = SelectAndPrepareLlmJob(
            source.Storage,
            source.CurrentHour,
            source.LastCacheAffinityKey,
            source.HasStaleThreatPresentation,
            source.RefreshThreatPresentation,
            source.HasStaleActionPresentation,
            source.RefreshActionPresentation,
            source.RebuildPendingJob,
            source.GetAuthorBlockReason,
            source.AbandonGeneration,
            source.EnsureStrategicProfile,
            source.GetLlmConfigError,
            source.TryConsumeRequestBudget,
            source.CaptureCanonicalHistory,
            source.BuildMessageArray,
            out JArray requestMessages,
            source.InputTokenLimit,
            source.HistoryCompressionTargetTokens,
            source.EstimateTokens,
            source.BuildHistoryBlock,
            source.ScheduleTokenCompression,
            source.CommitFailedJob,
            source.RemoveJob,
            source.Log);
        if (job == null) return;

        int timeout = WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "compress")
            ? source.CompressionTimeoutMilliseconds
            : source.DefaultApiTimeoutMilliseconds;
        if (!source.TryClaim(job.JobId, source.RuntimeGeneration, job.MaxTokens, timeout, out WorldDiplomacyRequestSnapshot request))
        {
            job.IsRunning = false;
            source.Log("world diplomacy request claim rejected job=" + (job.JobId ?? "")
                + " generation=" + source.RuntimeGeneration);
            return;
        }
        string cacheAffinityKey = WorldDiplomacyPromptContractRules.ResolveCacheAffinityKey(job);
        source.MarkRunning(job, cacheAffinityKey);
        source.SetLastCacheAffinityKey(cacheAffinityKey);
        source.LogPromptCacheShape(job);
        source.StartRequest(request, requestMessages);
    }

    internal static bool EnsureCurrentCanonicalPromptContractBeforeSend(
        WorldDiplomacyJob job,
        WorldDiplomacyStorage storage,
        Func<WorldDiplomacyJob, bool> rebuildPendingJob,
        Action<WorldDiplomacyJob, string> commitFailedJob,
        Action<string> removeJob,
        Action<string> log)
    {
        if (WorldDiplomacyPromptContractRules.HasCurrentCanonicalPromptContract(job)) return true;
        if (!WorldDiplomacyRoundLifecycleRules.UsesCanonicalHistory(job)) return true;
        log?.Invoke("retired stale canonical prompt contract before send job=" + (job.JobId ?? "")
            + " kind=" + (job.Kind ?? "")
            + " affinity=" + (job.CacheAffinityKey ?? ""));
        job.LlmMessages?.Clear();
        job.SemanticRepairAttempts = 0;
        job.HistoryPrefixHash = "";
        if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate"))
        {
            if (rebuildPendingJob?.Invoke(job) == true) return true;
            commitFailedJob?.Invoke(job, "stale canonical prompt contract could not be rebuilt");
            return false;
        }
        storage.DiplomacyCompressionPending = true;
        storage.CompressionRetryAfterHour = 0;
        storage.CompressionRetryAttempts = 0;
        removeJob?.Invoke(job.JobId);
        return false;
    }

    internal static WorldDiplomacyJob SelectAndPrepareLlmJob(
        WorldDiplomacyStorage storage,
        int currentHour,
        string lastCacheAffinityKey,
        Func<WorldDiplomacyJob, bool> hasStaleThreatPresentation,
        Func<WorldDiplomacyJob, bool> refreshThreatPresentation,
        Func<WorldDiplomacyJob, bool> hasStaleActionPresentation,
        Func<WorldDiplomacyJob, bool> refreshActionPresentation,
        Func<WorldDiplomacyJob, bool> rebuildPendingJob,
        Func<WorldDiplomacyJob, string> authorBlockReason,
        Action<WorldDiplomacyJob, string> abandonGeneration,
        Func<WorldDiplomacyJob, bool> ensureStrategicProfile,
        Func<string> llmConfigError,
        Func<bool, bool> tryConsumeRequestBudget,
        Action<WorldDiplomacyJob> captureCanonicalHistory,
        Func<WorldDiplomacyJob, JArray> buildMessageArray,
        out JArray preparedMessages,
        long inputTokenLimit,
        int historyCompressionTargetTokens,
        Func<string, int> estimateTokens,
        Func<long, string> buildHistoryBlock,
        Action scheduleTokenCompression,
        Action<WorldDiplomacyJob, string> commitFailedJob,
        Action<string> removeJob,
        Action<string> log)
    {
        preparedMessages = null;
        if (storage == null || (storage.Jobs?.Count ?? 0) == 0) return null;
        if (storage.ServiceCooldownUntilHour > currentHour) return null;
        List<WorldDiplomacyJob> runnable = (storage.Jobs ?? new List<WorldDiplomacyJob>())
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.JobId) && !x.IsRunning
                && (!x.AwaitingHistoryCompression || currentHour >= storage.CompressionRetryAfterHour))
            .ToList();
        int highestPriority = runnable.Count == 0 ? int.MinValue : runnable.Max(x => x.Priority);
        WorldDiplomacyJob job = runnable
            .Where(x => x.Priority == highestPriority)
            .OrderByDescending(x => string.Equals(
                (WorldDiplomacyPromptContractRules.ResolveCacheAffinityKey(x) ?? "").Trim(),
                (lastCacheAffinityKey ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => x.CreatedDay)
            .ThenBy(x => x.JobId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (job == null) return null;
        if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")
            && hasStaleThreatPresentation?.Invoke(job) == true)
        {
            if (refreshThreatPresentation?.Invoke(job) != true)
            {
                commitFailedJob?.Invoke(job, "stale diplomatic threat presentation could not be rebuilt");
                return null;
            }
            log?.Invoke("refreshed queued generation for current diplomatic threat stage job=" + job.JobId
                + " author=" + job.AuthorKingdomId);
        }
        if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")
            && hasStaleActionPresentation?.Invoke(job) == true)
        {
            if (refreshActionPresentation?.Invoke(job) != true)
            {
                commitFailedJob?.Invoke(job, "stale diplomatic action list could not be rebuilt");
                return null;
            }
            log?.Invoke("refreshed queued generation for current legal diplomatic actions job=" + job.JobId
                + " author=" + job.AuthorKingdomId);
        }
        if (!EnsureCurrentCanonicalPromptContractBeforeSend(
            job, storage, rebuildPendingJob, commitFailedJob, removeJob, log))
        {
            return null;
        }
        if (job.LlmMessages?.Count > 0 && !WorldDiplomacyPromptContractRules.IsValidSemanticRepairMessageChain(job))
        {
            log?.Invoke("retired invalid persisted LLM message chain job=" + (job.JobId ?? "") + " kind=" + (job.Kind ?? ""));
            job.LlmMessages.Clear();
            job.SemanticRepairAttempts = 0;
            if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")
                && rebuildPendingJob?.Invoke(job) != true)
            {
                commitFailedJob?.Invoke(job, "invalid persisted LLM message chain could not be rebuilt");
                return null;
            }
        }
        if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate"))
        {
            string blockReason = authorBlockReason?.Invoke(job);
            if (!string.IsNullOrWhiteSpace(blockReason))
            {
                log?.Invoke("queued generation cancelled before request job=" + job.JobId
                    + " author=" + (job.AuthorKingdomId ?? "") + " reason=" + blockReason);
                abandonGeneration?.Invoke(job, blockReason);
                removeJob?.Invoke(job.JobId);
                return null;
            }
        }
        if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate")
            && ensureStrategicProfile?.Invoke(job) != true)
        {
            abandonGeneration?.Invoke(job, "missing_kingdom_strategic_profile");
            removeJob?.Invoke(job.JobId);
            return null;
        }
        if (string.IsNullOrWhiteSpace(job.SystemPrompt))
        {
            commitFailedJob?.Invoke(job, "empty prompt");
            return null;
        }
        string configError = llmConfigError?.Invoke();
        if (!string.IsNullOrWhiteSpace(configError))
        {
            commitFailedJob?.Invoke(job, "api not configured: " + configError);
            return null;
        }
        if (tryConsumeRequestBudget?.Invoke(false) != true) return null;
        if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate"))
        {
            if (ensureStrategicProfile?.Invoke(job) != true)
            {
                abandonGeneration?.Invoke(job, "missing_kingdom_strategic_profile");
                removeJob?.Invoke(job.JobId);
                return null;
            }
            // Ordinary queued generations consume the newest committed archive at actual send time.
            // Semantic repairs carry explicit messages and intentionally retain their rejected
            // request's frozen prefix.
            if (job.LlmMessages == null || job.LlmMessages.Count == 0)
            {
                captureCanonicalHistory?.Invoke(job);
            }
        }
        JArray requestMessages = buildMessageArray?.Invoke(job);
        if (!EnsureRequestFitsInputBudget(job, requestMessages, inputTokenLimit,
            historyCompressionTargetTokens, estimateTokens, buildHistoryBlock,
            rebuildPendingJob, commitFailedJob, scheduleTokenCompression, log)) return null;
        if (tryConsumeRequestBudget?.Invoke(true) != true) return null;
        job.IsRunning = true;
        job.CacheAffinityKey = WorldDiplomacyPromptContractRules.ResolveCacheAffinityKey(job);
        preparedMessages = requestMessages;
        return job;
    }

    internal static bool EnsureRequestFitsInputBudget(
        WorldDiplomacyJob job,
        JArray messages,
        long inputTokenLimit,
        int historyCompressionTargetTokens,
        Func<string, int> estimateTokens,
        Func<long, string> buildHistoryBlock,
        Func<WorldDiplomacyJob, bool> rebuildPendingJob,
        Action<WorldDiplomacyJob, string> commitFailedJob,
        Action scheduleTokenCompression,
        Action<string> log)
    {
        long inputTokens = 0L;
        foreach (JToken message in messages)
            inputTokens += WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens((string)message["content"], estimateTokens) + WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens((string)message["role"], estimateTokens) + 4L;
        long limit = inputTokenLimit;
        if (inputTokens <= limit)
        {
            job.AwaitingHistoryCompression = false;
            return true;
        }
        if (!WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate"))
        {
            commitFailedJob?.Invoke(job, "input budget exceeded before send: " + inputTokens + "/" + limit
                + "; single archive entry/snapshot or non-history prompt requires reduction");
            return false;
        }
        if (WorldDiplomacyPromptContractRules.IsValidSemanticRepairMessageChain(job))
        {
            // A repair owns a frozen rejected prompt. Rebuild the declaration from current
            // authoritative state before compressing, rather than silently editing that chain.
            job.LlmMessages.Clear();
            job.SemanticRepairAttempts = 0;
            if (rebuildPendingJob?.Invoke(job) != true) commitFailedJob?.Invoke(job, "oversized repair could not be rebuilt");
            return false;
        }
        long historyTokens = WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens(buildHistoryBlock?.Invoke(job.HistoryThroughSequence) ?? "", estimateTokens);
        long availableHistoryTokens = limit - (inputTokens - historyTokens) - 1024L;
        if (availableHistoryTokens < 512L)
        {
            commitFailedJob?.Invoke(job, "non-history prompt alone exceeds input budget; history was retained");
            return false;
        }
        job.AwaitingHistoryCompression = true;
        job.InputBudgetHistoryTargetTokens = (int)Math.Min(historyCompressionTargetTokens, availableHistoryTokens / 2L);
        scheduleTokenCompression?.Invoke();
        log?.Invoke("generation deferred for history compression job=" + job.JobId + " input_tokens=" + inputTokens
            + " input_limit=" + limit + " history_target=" + job.InputBudgetHistoryTargetTokens);
        return false;
    }

}
