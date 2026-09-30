using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
    int CurrentHour();
    long WeeklyRevision();
    IEnumerable<WorldDiplomacyWeeklyArtifact> WeeklyArtifacts();
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

    internal static void SyncSources(IWorldDiplomacyHistoryCapturePort port, IWorldDiplomacyOrchestration orchestration,
        bool force, ref int lastSyncHour, ref long observedWeeklyRevision, int forcePolicyBatches)
    {
        orchestration.EnsureCanonicalHistoryInitialized();
        int hour = port.CurrentHour();
        if (!force && lastSyncHour == hour) return;
        long revision = port.WeeklyRevision();
        if (observedWeeklyRevision != revision)
        {
            foreach (WorldDiplomacyWeeklyArtifact artifact in port.WeeklyArtifacts())
            {
                orchestration.AppendCanonicalHistoryWeeklyArtifact(artifact);
            }
            observedWeeklyRevision = revision;
        }
        orchestration.SyncPublishedPolicyArtifacts(force ? forcePolicyBatches : 1);
        lastSyncHour = hour;
    }

    internal static void Capture(WorldDiplomacyStorage storage, WorldDiplomacyJob job, bool syncSources,
        long throughSequence, IWorldDiplomacyHistoryCapturePort port, IWorldDiplomacyOrchestration orchestration)
    {
        if (job == null) return;
        if (syncSources)
        {
            orchestration.RetryDeferredCanonicalHistoryEntries();
            orchestration.SyncCanonicalHistorySources(true);
        }
        orchestration.EnsureCanonicalHistoryInitialized();
        WorldDiplomacyCanonicalHistoryState history = storage.CanonicalHistory;
        job.HistoryThroughSequence = WorldDiplomacyCanonicalHistoryRules.ClampCanonicalHistoryThroughSequence(history, throughSequence);
        WorldDiplomacyCanonicalHistoryRules.StampCanonicalHistoryOnJob(job, history,
            orchestration.BuildCanonicalHistoryBlock(job.HistoryThroughSequence));
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

    internal static void RecordDiplomacyWeeklyMaterial(
        WorldDiplomacyDocument document,
        List<WorldDiplomacyDocument> documents,
        Action<string, string, string, string, string, string, bool, int, string> recordMaterial)
    {
        if (document == null || string.IsNullOrWhiteSpace(document.DocumentId))
        {
            return;
        }
        int day = Math.Max(0, document.Day);
        string roundKey = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.RoundId, document.DocumentId);
        List<WorldDiplomacyDocument> sameDay = documents
            .Where(item => item != null && item.IsReadyForPublication && item.Day == day
                && string.Equals(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(item.RoundId, item.DocumentId), roundKey, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.CreatedUtcTicks)
            .Take(6)
            .ToList();
        if (!sameDay.Any(item => WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(item.DocumentId, document.DocumentId)))
        {
            sameDay.Add(document);
        }
        StringBuilder snapshot = new StringBuilder();
        snapshot.Append("外交回合").Append(roundKey).Append("在本日出现以下公开进展：");
        foreach (WorldDiplomacyDocument item in sameDay.Take(6))
        {
            snapshot.Append(" ").Append(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(item.AuthorRulerName, item.AuthorKingdomName)).Append("发布《")
                .Append(WorldDiplomacyTextRules.Limit(item.Title, 80)).Append("》");
            if (!string.IsNullOrWhiteSpace(item.Body))
            {
                snapshot.Append("，核心主张：").Append(WorldDiplomacyTextRules.Limit(WorldDiplomacyTextRules.NormalizeBody(item.Body), 180));
            }
            if (item.ChangedDiplomaticState && !string.IsNullOrWhiteSpace(item.MechanicalResult))
            {
                snapshot.Append("；[游戏已执行] ").Append(WorldDiplomacyTextRules.Limit(item.MechanicalResult, 120));
            }
            snapshot.Append("。");
        }
        snapshot.Append("尚未标注[游戏已执行]的内容只是公开主张、提案、接受或拒绝，不得写成已经完成的外交结果。");

        List<string> relatedKingdomIds = WorldDiplomacyRoundLifecycleRules.NormalizeTrimmedIdListPreserveOrder(sameDay
            .SelectMany(item => new[] { item.AuthorKingdomId, item.TargetKingdomId }
                .Concat(item.AddressedKingdomIds ?? new List<string>())));
        string stableBase = "world_diplomacy:" + roundKey + ":day:" + day.ToString(CultureInfo.InvariantCulture);
        string authorKingdomId = (document.AuthorKingdomId ?? "").Trim();
        recordMaterial(
            stableBase + ":world",
            "外交宣言进展 - " + WorldDiplomacyTextRules.Limit(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(document.Title, document.AuthorKingdomName), 80),
            snapshot.ToString(),
            authorKingdomId,
            document.AuthorRulerId ?? "",
            authorKingdomId,
            true,
            day,
            document.GameDate ?? "");
        foreach (string kingdomId in relatedKingdomIds.Where(id => !string.Equals(id, authorKingdomId, StringComparison.OrdinalIgnoreCase)))
        {
            recordMaterial(
                stableBase + ":kingdom:" + kingdomId,
                "与本国有关的外交宣言进展",
                snapshot.ToString(),
                kingdomId,
                document.AuthorRulerId ?? "",
                authorKingdomId,
                false,
                day,
                document.GameDate ?? "");
        }
    }
}
