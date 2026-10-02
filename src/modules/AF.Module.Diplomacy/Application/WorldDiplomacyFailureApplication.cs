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
        Action<WorldDiplomacyJob, string> commitRoundPlan,
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
            commitAnalysis?.Invoke(job, buildFallbackAnalysisJson?.Invoke(job));
            logThreatFallbackPublished?.Invoke(job);
        }
        else if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "compress"))
        {
            storage.DiplomacyCompressionPending = true;
            storage.CompressionRetryAttempts = Math.Min(31, Math.Max(0, storage.CompressionRetryAttempts) + 1);
            int retryHours = storage.CompressionRetryAttempts >= 6
                ? compressionRetryMaximumHours
                : Math.Min(compressionRetryMaximumHours,
                    compressionRetryInitialHours << Math.Max(0, storage.CompressionRetryAttempts - 1));
            storage.CompressionRetryAfterHour = currentHour() + retryHours;
            log("token compression retained for retry batch=" + (job.CompressionBatchId ?? "")
                + " attempt=" + storage.CompressionRetryAttempts.ToString(CultureInfo.InvariantCulture)
                + " retry_hours=" + retryHours.ToString(CultureInfo.InvariantCulture)
                + " retry_after_hour=" + storage.CompressionRetryAfterHour.ToString(CultureInfo.InvariantCulture));
        }
        else if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "round_plan"))
        {
            commitRoundPlan?.Invoke(job, "{\"topic\":\"外交交涉\",\"selected_kingdom_ids\":[]}");
        }
        else if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "round_compress"))
        {
            commitRoundCompression?.Invoke(job, buildFallbackRoundCompressionJson?.Invoke(job));
        }
        removeJob?.Invoke(job.JobId);
    }
}
