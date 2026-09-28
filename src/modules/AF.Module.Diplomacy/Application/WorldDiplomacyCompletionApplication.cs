using System;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IWorldDiplomacyCompletionEffects
{
    bool HasStaleThreatPresentation(WorldDiplomacyJob job);
    bool RefreshThreatPresentation(WorldDiplomacyJob job);
    bool HasStaleActionPresentation(WorldDiplomacyJob job);
    bool RefreshActionPresentation(WorldDiplomacyJob job);
    void HandleTruncatedDraft(WorldDiplomacyJob job, string content);
    void CommitGeneratedDocument(WorldDiplomacyJob job, string content);
    void CommitAnalysis(WorldDiplomacyJob job, string content);
    void CommitCompression(WorldDiplomacyJob job, string content);
    void CommitRoundPlan(WorldDiplomacyJob job, string content);
    void CommitRoundCompression(WorldDiplomacyJob job, string content);
    void CommitFailedJob(WorldDiplomacyJob job, string content);
    void RemoveJob(string jobId);
    void Log(string message);
}

internal interface IWorldDiplomacyCompletionSource : IWorldDiplomacyCompletionEffects
{
    bool TryDequeue(out LlmJobResult result);
    void ReleaseLease(string jobId, long generation);
    long RuntimeGeneration { get; }
    bool IsSaveRuntimeStale(long generation);
    WorldDiplomacyStorage Storage { get; }
    int CurrentHour { get; }
    int FailedServiceCooldownHours { get; }
    void LogUsage(WorldDiplomacyJob job, LlmJobResult result);
}

// Main-thread completion admission and dispatch. Document/round execution and
// prompt rebuilding remain separate workflows behind the effects port.
internal static class WorldDiplomacyCompletionApplication
{
    internal static void Run<TSource>(ref TSource source)
        where TSource : struct, IWorldDiplomacyCompletionSource
    {
        while (source.TryDequeue(out LlmJobResult result))
        {
            source.ReleaseLease(result?.JobId, result?.RuntimeGeneration ?? 0L);
            // Release only the matching lease; never inspect a reloaded job for a late result.
            bool runtimeIsStale = result != null
                && result.RuntimeGeneration == source.RuntimeGeneration
                && source.IsSaveRuntimeStale(result.RuntimeGeneration);
            if (!WorldDiplomacyJobRuntimeCoordinator.IsCurrentCompletion(
                result?.JobId, result?.RuntimeGeneration ?? 0L,
                source.RuntimeGeneration, runtimeIsStale)) continue;
            WorldDiplomacyJob job = source.Storage.Jobs.FirstOrDefault(
                x => WorldDiplomacyRoundLifecycleRules.HasJobId(x, result.JobId));
            if (job == null) continue;
            job.IsRunning = false;
            source.LogUsage(job, result);
            Complete(job, result.Content, result.Success, result.IsServiceFailure,
                result.IsOutputTruncated, result.Error, source.Storage,
                source.CurrentHour, source.FailedServiceCooldownHours, ref source);
        }
    }

    internal static void Complete<TEffects>(
        WorldDiplomacyJob job, string resultContent, bool resultSuccess,
        bool resultIsServiceFailure, bool resultIsOutputTruncated, string resultError,
        WorldDiplomacyStorage storage, int currentHour, int failedServiceCooldownHours,
        ref TEffects effects)
        where TEffects : struct, IWorldDiplomacyCompletionEffects

    {
        if (job == null) return;
        // Preserve runtime route classification without rewriting the persisted Kind.
        string completionKind = (job.Kind ?? "").Trim();
        bool IsCompletionKind(string kind) => string.Equals(completionKind, kind, StringComparison.OrdinalIgnoreCase);
        if (resultSuccess
            && IsCompletionKind("generate")
            && effects.HasStaleThreatPresentation(job) == true)
        {
            if (effects.RefreshThreatPresentation(job) != true)
            {
                effects.CommitFailedJob(job, "completed generation used a stale diplomatic threat stage and could not be rebuilt");
            }
            else
            {
                effects.Log("discarded completed generation from stale diplomatic threat stage and rebuilt job=" + job.JobId
                    + " author=" + job.AuthorKingdomId);
            }
            return;
        }
        if (resultSuccess
            && IsCompletionKind("generate")
            && effects.HasStaleActionPresentation(job) == true)
        {
            if (effects.RefreshActionPresentation(job) != true)
            {
                effects.CommitFailedJob(job, "completed generation used a stale diplomatic action list and could not be rebuilt");
            }
            else
            {
                effects.Log("discarded completed generation from stale diplomatic action list and rebuilt job=" + job.JobId
                    + " author=" + job.AuthorKingdomId);
            }
            return;
        }
        if (!resultSuccess)
        {
            if (IsCompletionKind("generate")
                && resultIsOutputTruncated
                && !string.IsNullOrWhiteSpace(resultContent))
            {
                if (storage != null) storage.ConsecutiveServiceFailures = 0;
                try
                {
                    effects.HandleTruncatedDraft(job, resultContent);
                    effects.RemoveJob(job.JobId);
                }
                catch (Exception ex)
                {
                    effects.CommitFailedJob(job, "truncated generated draft handling failed: " + ex.Message);
                }
                return;
            }
            if (resultIsServiceFailure && storage != null)
            {
                storage.ConsecutiveServiceFailures++;
                if (storage.ConsecutiveServiceFailures >= 2)
                {
                    storage.ServiceCooldownUntilHour = currentHour + failedServiceCooldownHours;
                    storage.ConsecutiveServiceFailures = 0;
                }
            }
            effects.CommitFailedJob(job, resultError);
            return;
        }
        if (storage != null) storage.ConsecutiveServiceFailures = 0;
        try
        {
            if (IsCompletionKind("generate"))
            {
                effects.CommitGeneratedDocument(job, resultContent);
            }
            else if (IsCompletionKind("analyze"))
            {
                effects.CommitAnalysis(job, resultContent);
            }
            else if (IsCompletionKind("compress"))
            {
                effects.CommitCompression(job, resultContent);
            }
            else if (IsCompletionKind("round_plan"))
            {
                effects.CommitRoundPlan(job, resultContent);
            }
            else if (IsCompletionKind("round_compress"))
            {
                effects.CommitRoundCompression(job, resultContent);
            }
            else
            {
                effects.CommitFailedJob(job, "unknown job kind");
                return;
            }
            effects.RemoveJob(job.JobId);
        }
        catch (Exception ex)
        {
            effects.CommitFailedJob(job, ex.Message);
        }
    }
}
