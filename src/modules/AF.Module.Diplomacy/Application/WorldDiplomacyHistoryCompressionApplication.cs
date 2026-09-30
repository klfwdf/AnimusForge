using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Owns ordered canonical-state updates through existing synchronous dependency ports.
internal static class WorldDiplomacyHistoryCompressionApplication
{
public static void CommitCompression(
        WorldDiplomacyStorage storage,
        WorldDiplomacyJob job,
        string raw,
        Action ensureInitialized,
        Func<string, int> estimateTokens,
        Func<int> currentDay,
        int compressionTargetTokens,
        long compressionTriggerTokens,
        Action invalidateRenderCache,
        Action<string> log)
{
    if (job == null) throw new InvalidOperationException("missing compression job");
    ensureInitialized();
    WorldDiplomacyCanonicalHistoryState history = storage.CanonicalHistory;
    long cutoff = Math.Max(0L, job.CompressionThroughSequence);
    if (cutoff < history.Snapshot.CoveredThroughSequence) throw new InvalidOperationException("compression cutoff predates current snapshot");
    JObject json = WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(raw);
    string summaryText = WorldDiplomacyTextRules.NormalizeCanonicalHistoryText(WorldDiplomacyEnvelopeJsonRules.ReadString(json, "summary"));
    if (string.IsNullOrWhiteSpace(summaryText)) throw new InvalidOperationException("compression output has empty summary");
    long covered = json.Value<long?>("covered_through_sequence") ?? -1L;
    if (covered != cutoff) throw new InvalidOperationException("compression output covered_through_sequence mismatch");
    int targetTokens = Math.Max(1, job.CompressionTargetTokens > 0 ? job.CompressionTargetTokens : compressionTargetTokens);
    long summaryTokens = WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens(summaryText, estimateTokens);
    if (summaryTokens > targetTokens) throw new InvalidOperationException("compression output exceeds target token budget");
    int overallTargetTokens = Math.Max(1, job.CompressionOverallTargetTokens > 0
        ? job.CompressionOverallTargetTokens
        : compressionTargetTokens);
    int protectedBudgetTokens = Math.Max(0, Math.Min(overallTargetTokens - 256, overallTargetTokens / 4));
    List<WorldDiplomacyCanonicalProtectedFact> protectedFacts = WorldDiplomacyCanonicalRenderRules.SelectCanonicalProtectedFactsWithinTokenBudget(
        WorldDiplomacyRoundLifecycleRules.BuildCanonicalProtectedFactsThrough(history, cutoff), protectedBudgetTokens,
            text => WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens(text, estimateTokens));
    List<string> preservedResultIds = WorldDiplomacyRoundLifecycleRules.NormalizeOrderedIdList(protectedFacts
        .Where(x => string.Equals(x.Kind, "diplomatic_result", StringComparison.OrdinalIgnoreCase))
        .Select(x => x.SourceId));
    List<WorldDiplomacyCanonicalHistoryEntry> compressedEntries = WorldDiplomacyRoundLifecycleRules
        .SelectDeltaEntriesThrough(history.DeltaEntries, cutoff).ToList();
    List<string> sourceIds = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(compressedEntries.Select(x => x.SourceId));
    WorldDiplomacyCompressionSummary summary = new WorldDiplomacyCompressionSummary
    {
        BatchId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(job.CompressionBatchId, "diplomacy_compaction_" + (storage.CompressionSequence + 1).ToString(CultureInfo.InvariantCulture)),
        Summary = summaryText,
        CreatedDay = currentDay(),
        StartDay = compressedEntries.Count == 0 ? currentDay() : compressedEntries.Min(x => x.Day),
        EndDay = compressedEntries.Count == 0 ? currentDay() : compressedEntries.Max(x => x.Day),
        TokenCount = Math.Max(0L, job.CompressionTokenCount),
        SourceRoundIds = sourceIds,
        KingdomIds = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(compressedEntries.SelectMany(x => (x.TargetKingdomIds ?? new List<string>()).Concat(new[] { x.AuthorKingdomId }))),
        ConfirmedResults = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(compressedEntries.Where(x => string.Equals(x.Kind, "diplomatic_result", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Text)).Take(48).ToList()
    };
    WorldDiplomacyCanonicalHistorySnapshot replacement = new WorldDiplomacyCanonicalHistorySnapshot
    {
        Content = summaryText,
        CoveredThroughSequence = cutoff,
        CreatedDay = currentDay(),
        PreservedResultSourceIds = preservedResultIds,
        ProtectedFacts = protectedFacts
    };
    string replacementPayload = WorldDiplomacyCanonicalRenderRules.RenderCanonicalSnapshotPayload(replacement);
    replacement.ContentHash = WorldDiplomacyPromptContractRules.StablePromptHash(replacementPayload);
    replacement.EstimatedTokens = WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens(replacementPayload, estimateTokens);
    if (replacement.EstimatedTokens > overallTargetTokens)
    {
        throw new InvalidOperationException("compressed history exceeds overall target token budget");
    }
    // Commit snapshot and delete only the frozen prefix. Entries appended while the request
    // was running have greater sequence numbers and remain as delta.
    history.Snapshot = replacement;
    history.DeltaEntries.RemoveAll(x => x != null && x.Sequence <= cutoff);
    history.Revision++;
    foreach (WorldDiplomacyJob pending in storage.Jobs.Where(x => x != null && x.AwaitingHistoryCompression))
    {
        // Let the waiting declaration remeasure its complete request after each commit.
        // Keeping this flag set would enqueue another compaction before it could resume.
        pending.AwaitingHistoryCompression = false;
    }
    storage.CompressionSummaries.RemoveAll(x => x != null && string.Equals(x.BatchId, summary.BatchId, StringComparison.OrdinalIgnoreCase));
    storage.CompressionSummaries.Add(summary);
    storage.CompressionSequence = Math.Max(storage.CompressionSequence + 1, WorldDiplomacyRoundLifecycleRules.ParseCompressionSequence(summary.BatchId));
    storage.LastDiplomacyCompressionDay = currentDay();
    storage.CompressionRetryAfterHour = 0;
    storage.CompressionRetryAttempts = 0;
    invalidateRenderCache();
    WorldDiplomacyRoundLifecycleRules.RecalculateCanonicalHistoryTokens(storage, compressionTriggerTokens);
    log("token compression committed batch=" + summary.BatchId
        + " through_sequence=" + cutoff.ToString(CultureInfo.InvariantCulture)
        + " retained_delta=" + history.DeltaEntries.Count.ToString(CultureInfo.InvariantCulture)
        + " protected_facts=" + protectedFacts.Count.ToString(CultureInfo.InvariantCulture)
        + " remaining_tokens=" + history.EstimatedTokens.ToString(CultureInfo.InvariantCulture));
}

public static void TryScheduleTokenCompression(
        WorldDiplomacyStorage storage,
        Func<bool> diplomacyEnabled,
        Action ensureInitialized,
        Action syncSources,
        Func<int> currentHour,
        long compressionTriggerTokens,
        int compressionTargetTokens,
        Action<long, long, int> enqueueCompressionJob)
{
    if (!diplomacyEnabled()) return;
    ensureInitialized();
    syncSources();
    long threshold = compressionTriggerTokens;
    storage.DiplomacyCompressionPending = storage.CanonicalHistory.EstimatedTokens >= threshold
        || storage.Jobs.Any(x => x != null && x.AwaitingHistoryCompression);
    if (!storage.DiplomacyCompressionPending || currentHour() < storage.CompressionRetryAfterHour) return;
    if (storage.Jobs.Any(x => WorldDiplomacyRoundLifecycleRules.IsJobOfKind(x, "compress"))) return;
    long throughSequence = Math.Max(storage.CanonicalHistory.Snapshot.CoveredThroughSequence, storage.CanonicalHistory.NextSequence - 1L);
    int targetTokens = storage.Jobs.Where(x => x != null && x.AwaitingHistoryCompression && x.InputBudgetHistoryTargetTokens > 0)
        .Select(x => x.InputBudgetHistoryTargetTokens).DefaultIfEmpty(compressionTargetTokens).Min();
    enqueueCompressionJob(throughSequence, storage.CanonicalHistory.EstimatedTokens, Math.Min(targetTokens, compressionTargetTokens));
}

public static void EnqueueCompressionJob(
        WorldDiplomacyStorage storage,
        long throughSequence,
        long tokenCount,
        int targetTokens,
        Action ensureInitialized,
        Func<(int Minimum, int Maximum)> declarationCharacterRange,
        Func<string> commonSystemPrefix,
        Func<string, int> estimateTokens,
        long compressionTriggerTokens,
        int compressionJobPriority,
        int compressionOutputTokenReserve,
        int compressionRetryMaximumHours,
        int maxPendingJobs,
        Func<int> currentHour,
        Func<int> currentDay,
        Func<int> resolveOutputTokenLimit,
        Func<string, string> newId,
        Action<WorldDiplomacyJob, long> captureHistory,
        Action<string> log)
{
    ensureInitialized();
    WorldDiplomacyCanonicalHistoryState history = storage.CanonicalHistory;
    (int minimumCharacters, int maximumCharacters) = declarationCharacterRange();
    string systemPrompt = WorldDiplomacyPromptContractRules.BuildCanonicalHistorySystemPrompt(commonSystemPrefix(), minimumCharacters, maximumCharacters);
    // Reserve room for the request contract, mode parameters, archive headings and message framing.
    long inputBudget = compressionTriggerTokens - WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens(systemPrompt, estimateTokens) - 2048L;
    throughSequence = WorldDiplomacyPolicyHistoryRules.SelectCompressionPrefix(
        history.Snapshot.CoveredThroughSequence, history.Snapshot.EstimatedTokens,
        WorldDiplomacyRoundLifecycleRules.SelectDeltaEntriesThrough(history.DeltaEntries, throughSequence),
        inputBudget, x => x.Sequence, x => x.EstimatedTokens);
    tokenCount = history.Snapshot.EstimatedTokens + history.DeltaEntries
        .Where(x => x != null && x.Sequence <= throughSequence).Sum(x => x.EstimatedTokens);
    int batchSequence = Math.Max(0, storage.CompressionSequence) + 1;
    string batchId = "diplomacy_compaction_" + batchSequence.ToString(CultureInfo.InvariantCulture);
    int overallTargetTokens = Math.Max(1, targetTokens);
    overallTargetTokens = (int)Math.Min(overallTargetTokens, Math.Max(256L, inputBudget / 2L));
    if (!WorldDiplomacyPolicyHistoryRules.CanAdvanceCompression(throughSequence,
        history.Snapshot.CoveredThroughSequence, history.Snapshot.EstimatedTokens, overallTargetTokens))
    {
        // Do not repeatedly pay to compress an already-small snapshot while the next
        // indivisible entry still cannot fit. Keep the archive intact and back off locally.
        storage.CompressionRetryAfterHour = currentHour() + compressionRetryMaximumHours;
        log("compression input budget cannot fit the next history entry; archive retained, retry deferred");
        return;
    }
    int protectedBudgetTokens = Math.Max(0, Math.Min(overallTargetTokens - 256, overallTargetTokens / 4));
    List<WorldDiplomacyCanonicalProtectedFact> protectedFacts = WorldDiplomacyCanonicalRenderRules.SelectCanonicalProtectedFactsWithinTokenBudget(
        WorldDiplomacyRoundLifecycleRules.BuildCanonicalProtectedFactsThrough(history, throughSequence), protectedBudgetTokens,
            text => WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens(text, estimateTokens));
    List<string> preservedResultIds = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(protectedFacts
        .Where(x => string.Equals(x.Kind, "diplomatic_result", StringComparison.OrdinalIgnoreCase))
        .Select(x => x.SourceId));
    long protectedTokens = WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens(WorldDiplomacyCanonicalRenderRules.RenderCanonicalProtectedFacts(protectedFacts, preservedResultIds), estimateTokens);
    int configuredOutputTokenLimit = resolveOutputTokenLimit();
    int outputTokenReserve = Math.Min(compressionOutputTokenReserve, Math.Max(128, configuredOutputTokenLimit / 8));
    int outputSummaryCapacity = Math.Max(256, configuredOutputTokenLimit - outputTokenReserve);
    long desiredSummaryTokens = Math.Max(256L, overallTargetTokens - protectedTokens - 32L);
    int summaryTargetTokens = (int)Math.Min(desiredSummaryTokens, outputSummaryCapacity);
    WorldDiplomacyJob job = new WorldDiplomacyJob
    {
        JobId = newId("diplomacy_compress"),
        Kind = "compress",
        Priority = compressionJobPriority,
        CreatedDay = currentDay(),
        CompressionBatchId = batchId,
        CompressionTokenCount = Math.Max(0L, tokenCount),
        CompressionThroughSequence = Math.Max(0L, throughSequence),
        CompressionOverallTargetTokens = overallTargetTokens,
        CompressionTargetTokens = summaryTargetTokens,
        SystemPrompt = systemPrompt,
        UserPrompt = WorldDiplomacyPromptContractRules.BuildTokenCompressionPrompt(batchId, throughSequence, tokenCount, summaryTargetTokens, protectedTokens),
        CacheAffinityKey = WorldDiplomacyPromptContractRules.CanonicalHistoryCacheAffinityKey,
        MaxTokens = Math.Min(configuredOutputTokenLimit, summaryTargetTokens + outputTokenReserve)
    };
    captureHistory(job, throughSequence);
    WorldDiplomacyRoundLifecycleRules.EnqueueJob(storage, job, maxPendingJobs);
    log("token compression queued batch=" + batchId
        + " through_sequence=" + throughSequence.ToString(CultureInfo.InvariantCulture)
        + " estimated_tokens=" + tokenCount.ToString(CultureInfo.InvariantCulture)
        + " overall_target_tokens=" + overallTargetTokens.ToString(CultureInfo.InvariantCulture)
        + " protected_tokens=" + protectedTokens.ToString(CultureInfo.InvariantCulture)
        + " summary_target_tokens=" + summaryTargetTokens.ToString(CultureInfo.InvariantCulture)
        + " configured_output_token_limit=" + configuredOutputTokenLimit.ToString(CultureInfo.InvariantCulture)
        + " request_max_tokens=" + job.MaxTokens.ToString(CultureInfo.InvariantCulture));
}
}
