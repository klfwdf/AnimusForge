using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Built on a queue mutation or hour/affinity change, never on each campaign tick.
// The callbacks are runtime-only; persisted job shape and ordering stay unchanged.
internal sealed class WorldDiplomacyJobSelectionView
{
    private static readonly ConditionalWeakTable<WorldDiplomacyStorage, WorldDiplomacyJobSelectionView> Views =
        new ConditionalWeakTable<WorldDiplomacyStorage, WorldDiplomacyJobSelectionView>();
    internal static WorldDiplomacyJobSelectionView For(WorldDiplomacyStorage storage) =>
        Views.GetValue(storage, _ => new WorldDiplomacyJobSelectionView());

    private List<WorldDiplomacyJob> _jobs;
    private int _jobCount = -1;
    private bool _dirty = true;
    private bool _hasAwaiting;
    private bool _hasCompression;
    private int _minimumAwaitingTarget;
    private bool _selectionValid;
    private int _selectedHour;
    private int _selectedRetryHour;
    private string _selectedAffinity;
    private WorldDiplomacyJob _selected;

    internal int RebuildCount { get; private set; }
    internal void Invalidate() => _dirty = true;

    private void EnsureSummary(WorldDiplomacyStorage storage)
    {
        List<WorldDiplomacyJob> jobs = storage.Jobs;
        int count = jobs?.Count ?? 0;
        if (!_dirty && ReferenceEquals(_jobs, jobs) && _jobCount == count) return;
        _jobs = jobs;
        _jobCount = count;
        _dirty = false;
        _selectionValid = false;
        _hasAwaiting = false;
        _hasCompression = false;
        _minimumAwaitingTarget = 0;
        RebuildCount++;
        if (jobs == null) return;
        foreach (WorldDiplomacyJob job in jobs)
        {
            if (job == null) continue;
            job.SelectionChanged = Invalidate;
            if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "compress")) _hasCompression = true;
            if (!job.AwaitingHistoryCompression) continue;
            _hasAwaiting = true;
            if (job.InputBudgetHistoryTargetTokens > 0
                && (_minimumAwaitingTarget == 0 || job.InputBudgetHistoryTargetTokens < _minimumAwaitingTarget))
                _minimumAwaitingTarget = job.InputBudgetHistoryTargetTokens;
        }
    }

    internal bool HasAwaiting(WorldDiplomacyStorage storage) { EnsureSummary(storage); return _hasAwaiting; }
    internal bool HasCompression(WorldDiplomacyStorage storage) { EnsureSummary(storage); return _hasCompression; }
    internal int MinimumAwaitingTarget(WorldDiplomacyStorage storage, int fallback)
    { EnsureSummary(storage); return _minimumAwaitingTarget > 0 ? Math.Min(_minimumAwaitingTarget, fallback) : fallback; }

    internal WorldDiplomacyJob Select(WorldDiplomacyStorage storage, int currentHour, string lastCacheAffinityKey, Func<WorldDiplomacyJob, bool> eligible = null, Func<WorldDiplomacyJob, int> priority = null)
    {
        EnsureSummary(storage);
        if (eligible == null && storage.ServiceCooldownUntilHour > currentHour) return null;
        string affinity = (lastCacheAffinityKey ?? "").Trim();
        if (eligible == null && _selectionValid && _selectedHour == currentHour
            && _selectedRetryHour == storage.CompressionRetryAfterHour
            && string.Equals(_selectedAffinity, affinity, StringComparison.OrdinalIgnoreCase)) return _selected;
        _selectionValid = true;
        _selectedHour = currentHour;
        _selectedRetryHour = storage.CompressionRetryAfterHour;
        _selectedAffinity = affinity;
        _selected = null;
        bool selectedAffinityMatch = false;
        if (_jobs == null) return null;
        foreach (WorldDiplomacyJob candidate in _jobs)
        {
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.JobId) || candidate.IsRunning
                || (candidate.AwaitingHistoryCompression && currentHour < storage.CompressionRetryAfterHour)) continue;
            if (eligible != null && !eligible(candidate)) continue;
            int candidatePriority = priority?.Invoke(candidate) ?? candidate.Priority;
            int selectedPriority = _selected == null ? int.MinValue : priority?.Invoke(_selected) ?? _selected.Priority;
            bool affinityMatch = string.Equals(
                (WorldDiplomacyPromptContractRules.ResolveCacheAffinityKey(candidate) ?? "").Trim(),
                affinity, StringComparison.OrdinalIgnoreCase);
            if (_selected == null || candidatePriority > selectedPriority
                || (candidatePriority == selectedPriority && affinityMatch && !selectedAffinityMatch)
                || (candidatePriority == selectedPriority && affinityMatch == selectedAffinityMatch
                    && (candidate.CreatedDay < _selected.CreatedDay
                        || (candidate.CreatedDay == _selected.CreatedDay
                            && StringComparer.OrdinalIgnoreCase.Compare(candidate.JobId, _selected.JobId) < 0))))
            {
                _selected = candidate;
                selectedAffinityMatch = affinityMatch;
            }
        }
        return _selected;
    }
}

internal interface IWorldDiplomacyLlmDispatchSource
{
    bool IsEnabled { get; }
    bool IsRequestRunning { get; }
    WorldDiplomacyStorage Storage { get; }
    int CurrentHour { get; }
    string LastCacheAffinityKey { get; }
    void SetLastCacheAffinityKey(string value);
    string GetAuthorBlockReason(WorldDiplomacyJob job);
    string GetLlmConfigError();
    bool TryConsumeRequestBudget(bool consume);
    JArray BuildMessageArray(WorldDiplomacyJob job);
    long InputTokenLimit { get; }
    int HistoryCompressionTargetTokens { get; }
    int EstimateTokens(string text);
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
    internal static void Run<TSource>(ref TSource source, IWorldDiplomacyOrchestration orchestration)
        where TSource : IWorldDiplomacyLlmDispatchSource
    {
        var scheduler = orchestration as WorldDiplomacyOrchestration;
        if (!source.IsEnabled || (scheduler != null ? scheduler.RequestLeases.IsFull : source.IsRequestRunning) || source.Storage?.Jobs?.Count == 0) return;
        int selectionDay = source.CurrentHour / 24;
        WorldDiplomacyJob job = SelectAndPrepareLlmJob(
            source.Storage,
            source.CurrentHour,
            source.LastCacheAffinityKey,
            orchestration.BuildGenerationLegalActionSignature,
            orchestration.RefreshDiplomaticThreatPresentationAndPrompt,
            orchestration.RefreshDiplomaticActionPresentationAndPrompt,
            orchestration.TryRebuildPendingJob,
            source.GetAuthorBlockReason,
            (j, reason) => orchestration.AbandonRejectedGeneration(j, j?.AuthorKingdomId, j?.TargetKingdomId, reason),
            orchestration.EnsureGenerationJobHasKingdomStrategicProfile,
            source.GetLlmConfigError,
            scheduler == null ? source.TryConsumeRequestBudget : _ => true,
            j => orchestration.CaptureCanonicalHistoryForJob(j, syncSources: true),
            source.BuildMessageArray,
            out JArray requestMessages,
            source.InputTokenLimit,
            source.HistoryCompressionTargetTokens,
            source.EstimateTokens,
            orchestration.BuildCanonicalHistoryBlock,
            orchestration.TryScheduleTokenCompression,
            orchestration.CommitFailedJob,
            source.RemoveJob,
            source.Log,
            scheduler == null ? null : scheduler.CanDispatchDiplomacyJob,
            scheduler == null ? null : scheduler.PrepareSharedRequest,
            scheduler == null ? null : j => j.Kind == "compress" ? j.Priority : scheduler.IsPlayerSchedulingJobForDispatch(j) ? 95
                : Math.Min(100, j.Priority + Math.Max(0, selectionDay - j.CreatedDay) * 5));
        if (job == null) return;

        int timeout = WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "compress")
            ? source.CompressionTimeoutMilliseconds
            : source.DefaultApiTimeoutMilliseconds;
        WorldDiplomacyRequestSnapshot request;
        bool claimed = scheduler != null
            ? scheduler.RequestLeases.TryClaim(job, source.RuntimeGeneration, job.MaxTokens, timeout, scheduler.IsPlayerSchedulingJobForDispatch(job), out request)
            : source.TryClaim(job.JobId, source.RuntimeGeneration, job.MaxTokens, timeout, out request);
        if (!claimed)
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
        Func<WorldDiplomacyJob, string> buildLegalActionSignature,
        Func<WorldDiplomacyJob, bool> refreshThreatPresentation,
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
        Action<string> log, Func<WorldDiplomacyJob, bool> eligible = null, Action<WorldDiplomacyJob> prepareShared = null, Func<WorldDiplomacyJob, int> priority = null)
    {
        preparedMessages = null;
        if (storage == null || (storage.Jobs?.Count ?? 0) == 0) return null;
        WorldDiplomacyJob job = WorldDiplomacyJobSelectionView.For(storage)
            .Select(storage, currentHour, lastCacheAffinityKey, eligible, priority);
        if (job == null) return null;
        if (WorldDiplomacyRoundLifecycleRules.HasStaleThreatPresentation(job, storage?.DiplomaticThreats))
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
            && WorldDiplomacyJobPreparationApplication.HasStaleDiplomaticActionPresentation(
                job, buildLegalActionSignature))
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
        // Refreshing legal actions or repairing a persisted prompt can replace its
        // tail. Freeze the event/player sources only after those refreshes finish.
        prepareShared?.Invoke(job);
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
