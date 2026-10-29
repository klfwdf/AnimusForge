using AnimusForge;
using System;
using System.Globalization;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Main-thread failure transition for every persisted LLM job kind. The callbacks
// enter distinct workflows or invoke narrow effects after this owner chooses the route.
internal static class WorldDiplomacyFailureApplication
{
    internal static void Commit(
        WorldDiplomacyJob job, string error, WorldDiplomacyStorage storage,
        int compressionRetryMaximumHours, int compressionRetryInitialHours,
        Func<int> currentHour,
        Action<WorldDiplomacyJob, string, string, string> abandonRejectedGeneration,
        Func<WorldDiplomacyJob, string> buildFallbackAnalysisJson,
        Action<WorldDiplomacyJob, string> commitAnalysis,
        Action<WorldDiplomacyJob> logThreatFallbackPublished,
        Func<WorldDiplomacyJob, string> buildFallbackRoundCompressionJson,
        Action<WorldDiplomacyJob, string> commitRoundCompression,
        Action<string> removeJob, Action<string> log)
    {
        if (job == null) return;
        log("job failed kind=" + job.Kind + " id=" + job.JobId + " error=" + WorldDiplomacyTextRules.Limit(error, 600));
        if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "generate"))
        {
            abandonRejectedGeneration?.Invoke(job, job.AuthorKingdomId, job.TargetKingdomId,
                WorldDiplomacyRoundLifecycleRules.IsAutonomousOpeningJob(job) ? "autonomous_generation_failed" : "generation_failed");
        }
        else if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "analyze"))
        {
            var document = storage?.Documents?.Find(x => x?.DocumentId == job.DocumentId);
            if (document?.IsPlayerAuthored == true && document.PlayerAnalysisCommitted)
            {
                document.AnalysisStatus = "analysis_execution_failed";
                document.MechanicalResult += DiplomacyMiscPromptTexts.Get("Failure_IncompleteProcessingWarning", "\nDiplomatic processing did not complete fully; to prevent duplicate execution, this item will not retry automatically. Check existing diplomatic outcomes.");
            }
            commitAnalysis?.Invoke(job, document?.IsPlayerAuthored == true
                ? (error == "world_diplomacy_output_truncated"
                    ? "{\"status\":\"analysis_failed\",\"failure_reason\":\"output_truncated\"}"
                    : "{\"status\":\"analysis_failed\"}") : buildFallbackAnalysisJson?.Invoke(job));
            logThreatFallbackPublished?.Invoke(job);
        }
        else if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "compress"))
        {
            DiplomacyCompressionBudget.Fail(storage, job, error, currentHour(), compressionRetryInitialHours, compressionRetryMaximumHours);
            int retryHours = DiplomacyCompressionBudget.Held(storage) ? -1 : Math.Max(0, storage.CompressionRetryAfterHour - currentHour());
            log("token compression retained for retry batch=" + (job.CompressionBatchId ?? "")
                + " attempt=" + storage.CompressionRetryAttempts.ToString(CultureInfo.InvariantCulture)
                + " retry_hours=" + retryHours.ToString(CultureInfo.InvariantCulture)
                + " retry_after_hour=" + storage.CompressionRetryAfterHour.ToString(CultureInfo.InvariantCulture));
        }
        else if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "round_compress"))
        {
            commitRoundCompression?.Invoke(job, buildFallbackRoundCompressionJson?.Invoke(job));
        }
        removeJob?.Invoke(job.JobId);
    }
}
