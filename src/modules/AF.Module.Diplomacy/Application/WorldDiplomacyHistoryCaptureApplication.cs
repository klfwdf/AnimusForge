using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Persistence;

namespace AnimusForge;

internal sealed class WorldDiplomacyWeeklyArtifact
{
    internal string SourceId { get; }
    internal string Title { get; }
    internal string Text { get; }
    internal int Day { get; }
    internal string Date { get; }
    internal WorldDiplomacyWeeklyArtifact(string sourceId, string title, string text, int day, string date)
    { SourceId = sourceId; Title = title; Text = text; Day = day; Date = date; }
}

internal interface IWorldDiplomacyHistoryCapturePort
{
    void EnsureInitialized();
    int CurrentHour();
    long WeeklyRevision();
    IEnumerable<WorldDiplomacyWeeklyArtifact> WeeklyArtifacts();
    void AppendWeekly(WorldDiplomacyWeeklyArtifact artifact);
    void SyncPolicyArtifacts(int maxBatches);
    void RetryDeferredEntries();
    void SyncSources(bool force);
    string Render(long throughSequence);
}

internal static class WorldDiplomacyHistoryCaptureApplication
{
    internal static void EnsureInitialized(WorldDiplomacyStorage storage, ref bool initialized,
        HashSet<string> sourceKeys, long tokenLimit, Func<string, int> estimateTokens, Action invalidate, Action<string> log)
    {
        if (initialized && storage?.CanonicalHistory?.Snapshot != null && storage.CanonicalHistory.DeltaEntries != null) return;
        WorldDiplomacyStorageMigration.NormalizeCanonicalHistoryState(storage, sourceKeys, tokenLimit, estimateTokens, invalidate, log);
        initialized = true;
    }

    internal static void SyncSources<TPort>(ref TPort port, bool force, ref int lastSyncHour, ref long observedWeeklyRevision,
        int forcePolicyBatches) where TPort : IWorldDiplomacyHistoryCapturePort
    {
        port.EnsureInitialized();
        int hour = port.CurrentHour();
        if (!force && lastSyncHour == hour) return;
        long revision = port.WeeklyRevision();
        if (observedWeeklyRevision != revision)
        {
            foreach (WorldDiplomacyWeeklyArtifact artifact in port.WeeklyArtifacts()) port.AppendWeekly(artifact);
            observedWeeklyRevision = revision;
        }
        port.SyncPolicyArtifacts(force ? forcePolicyBatches : 1);
        lastSyncHour = hour;
    }

    internal static void Capture<TPort>(WorldDiplomacyStorage storage, WorldDiplomacyJob job, bool syncSources,
        long throughSequence, ref TPort port) where TPort : IWorldDiplomacyHistoryCapturePort
    {
        if (job == null) return;
        if (syncSources)
        {
            port.RetryDeferredEntries();
            port.SyncSources(true);
        }
        port.EnsureInitialized();
        WorldDiplomacyCanonicalHistoryState history = storage.CanonicalHistory;
        job.HistoryThroughSequence = WorldDiplomacyCanonicalHistoryRules.ClampCanonicalHistoryThroughSequence(history, throughSequence);
        WorldDiplomacyCanonicalHistoryRules.StampCanonicalHistoryOnJob(job, history, port.Render(job.HistoryThroughSequence));
    }

    internal static void RetryDeferredCanonicalHistoryEntries(
        Queue<string> documentIds,
        HashSet<string> documentIdSet,
        Dictionary<string, int> retryAttempts,
        Dictionary<string, int> retryAfterHours,
        List<WorldDiplomacyThreat> threats,
        int currentHour,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Action<WorldDiplomacyDocument> appendDocumentEvents,
        Action<WorldDiplomacyThreat> appendThreatHistoryResult,
        Action<WorldDiplomacyThreat> appendThreatDomesticPenaltyResult,
        Action<WorldDiplomacyThreat> appendThreatIssuerRewardResult,
        Action<WorldDiplomacyThreat, WorldDiplomacyThreatNonComplianceEvent> appendNonComplianceResult,
        Action<string> log,
        int maxAttempts = 16)
    {
        if (documentIds == null || documentIdSet == null) return;
        int attempts = WorldDiplomacyRoundLifecycleRules.ComputeDeferredRetryBatchSize(maxAttempts, documentIds.Count);
        for (int i = 0; i < attempts; i++)
        {
            string documentId = documentIds.Dequeue();
            documentIdSet.Remove(documentId);
            WorldDiplomacyDocument document = resolveDocument?.Invoke(documentId);
            if (!WorldDiplomacyStructureRules.NeedsCanonicalHistoryRetry(document))
            {
                retryAttempts?.Remove(documentId);
                retryAfterHours?.Remove(documentId);
                continue;
            }
            if (retryAfterHours != null
                && retryAfterHours.TryGetValue(documentId, out int retryAfterHour)
                && !WorldDiplomacyRoundLifecycleRules.IsDeferredRetryDue(currentHour, retryAfterHour))
            {
                WorldDiplomacyRoundLifecycleRules.EnqueueDeferredCanonicalHistoryRetry(documentIdSet, documentIds, documentId);
                continue;
            }
            try
            {
                appendDocumentEvents?.Invoke(document);
                WorldDiplomacyRoundLifecycleRules.FinalizeDiplomaticThreatHistoryAfterDocument(document, threats,
                    appendThreatHistoryResult, appendThreatDomesticPenaltyResult, appendThreatIssuerRewardResult);
                WorldDiplomacyRoundLifecycleRules.FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument(document,
                    threats, appendNonComplianceResult);
            }
            catch (Exception ex)
            {
                WorldDiplomacyRoundLifecycleRules.ScheduleDeferredCanonicalHistoryRetry(
                    retryAttempts, retryAfterHours, documentIdSet, documentIds, documentId, currentHour);
                log?.Invoke("deferred canonical history retry failed document=" + documentId + " error=" + ex.Message);
                continue;
            }
            if (WorldDiplomacyStructureRules.NeedsCanonicalHistoryRetry(document))
            {
                WorldDiplomacyRoundLifecycleRules.ScheduleDeferredCanonicalHistoryRetry(
                    retryAttempts, retryAfterHours, documentIdSet, documentIds, documentId, currentHour);
            }
            else
            {
                retryAttempts?.Remove(documentId);
                retryAfterHours?.Remove(documentId);
            }
        }
    }
}
