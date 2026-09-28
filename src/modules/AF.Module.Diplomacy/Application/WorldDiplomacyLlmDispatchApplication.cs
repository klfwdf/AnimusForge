using System;
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
    bool EnsureFitsInputBudget(WorldDiplomacyJob job, JArray messages);
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
        WorldDiplomacyJob job = WorldDiplomacyRoundLifecycleRules.SelectAndPrepareLlmJob(
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
            source.EnsureFitsInputBudget,
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
}
