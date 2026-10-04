using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal interface IWorldDiplomacyCanonicalHistoryMigrationSource
{
    bool HasCampaignWorld { get; }
    int CurrentDay { get; }
    long HistoryCompressionTriggerTokens { get; }
    int TargetHistoryMemorySchemaVersion { get; }
    int RelaySchemaVersion { get; }
    string PublishedPolicyLedgerId();
    void AcknowledgePolicyArtifactsThrough(long throughSequence);
    void ClearSourceKeys();
    IEnumerable<WorldDiplomacyWeeklyArtifact> WeeklyArtifacts();
    IEnumerable<PublishedPolicyArtifactLedgerEntry> PolicyArtifacts();
    long WeeklyRevision();
    void SetObservedWeeklyRevision(long revision);
    WorldDiplomacyDocument ResolveDocument(string documentId);
    int EstimateTokens(string text);
    void Log(string message);
}

internal interface IWorldDiplomacyRoundClosure
{
    void CloseRound(string reason, WorldDiplomacyRound round);
}

internal static class WorldDiplomacyCanonicalHistoryMigrationApplication
{
    private sealed class WorkItem
    {
        internal int Day;
        internal long CreatedUtcTicks;
        internal string StableKey;
        internal WorldDiplomacyDocument Document;
        internal WorldDiplomacyWeeklyArtifact WeeklyArtifact;
        internal PublishedPolicyArtifactLedgerEntry Policy;
    }

    internal static void MigrateIfNeeded(WorldDiplomacyStorage storage, IWorldDiplomacyCanonicalHistoryMigrationSource source,
        IWorldDiplomacyOrchestration orchestration)
    {
        if (storage == null || storage.HistoryMemorySchemaVersion >= source.TargetHistoryMemorySchemaVersion) return;
        if (!source.HasCampaignWorld) return;
        orchestration.EnsureCanonicalHistoryInitialized();
        WorldDiplomacyCanonicalHistoryState history = storage.CanonicalHistory;
        if (storage.HistoryMemorySchemaVersion == 3)
        {
            // Schema v3 temporarily kept an unbounded, exact hard-fact appendix beside the
            // summary. Fold it once into the compressible snapshot so the configured
            // summary target and independent trigger apply to the complete history again.
            string legacyProtectedFacts = WorldDiplomacyCanonicalRenderRules.RenderCanonicalProtectedFacts(
                history.Snapshot.ProtectedFacts,
                history.Snapshot.PreservedResultSourceIds);
            if (!string.IsNullOrWhiteSpace(legacyProtectedFacts))
            {
                history.Snapshot.Content = WorldDiplomacyTextRules.NormalizeCanonicalHistoryText(
                    string.Join("\n", new[] { history.Snapshot.Content, legacyProtectedFacts }
                        .Where(x => !string.IsNullOrWhiteSpace(x))));
                history.Revision++;
            }
            history.Snapshot.ProtectedFacts.Clear();
            history.Snapshot.PreservedResultSourceIds.Clear();
            string upgradedSnapshotPayload = WorldDiplomacyCanonicalRenderRules.RenderCanonicalSnapshotPayload(history.Snapshot);
            history.Snapshot.ContentHash = WorldDiplomacyPromptContractRules.StablePromptHash(upgradedSnapshotPayload);
            history.Snapshot.EstimatedTokens = WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens(upgradedSnapshotPayload, source.EstimateTokens);
        }
        if (storage.HistoryMemorySchemaVersion >= 3)
        {
            string currentPolicyLedgerId = (source.PublishedPolicyLedgerId() ?? "").Trim();
            if (!string.Equals(history.LastPolicyArtifactLedgerId, currentPolicyLedgerId, StringComparison.Ordinal))
            {
                history.LastPolicyArtifactLedgerId = currentPolicyLedgerId;
                history.LastPolicyArtifactSequence = 0L;
            }
            else if (history.LastPolicyArtifactSequence > 0L)
            {
                orchestration.RebuildPublishedPolicySignaturesThrough(history.LastPolicyArtifactSequence);
            }
            WorldDiplomacyRoundLifecycleRules.RecalculateCanonicalHistoryTokens(storage, source.HistoryCompressionTriggerTokens);
            storage.HistoryMemorySchemaVersion = source.TargetHistoryMemorySchemaVersion;
            orchestration.InvalidateCanonicalHistoryRenderCache();
            source.Log("canonical diplomacy history schema upgraded version="
                + source.TargetHistoryMemorySchemaVersion.ToString(CultureInfo.InvariantCulture)
                + " entries=" + history.DeltaEntries.Count.ToString(CultureInfo.InvariantCulture)
                + " snapshot_tokens=" + history.Snapshot.EstimatedTokens.ToString(CultureInfo.InvariantCulture));
            return;
        }
        if (storage.HistoryMemorySchemaVersion < 3)
        {
            // Early canonical-history schemas could contain pre-final policy material whose
            // provenance cannot be proven after it was compressed. Rebuild this cold migration
            // exclusively from published documents/results, final world reports, the immutable
            // policy artifact ledger and legacy summary products; never carry the old request body.
            history.Snapshot = new WorldDiplomacyCanonicalHistorySnapshot();
            history.DeltaEntries.Clear();
            history.NextSequence = 1L;
            history.EstimatedTokens = 0L;
            history.WorldWeeklySourceHashes.Clear();
            history.WorldWeeklySourceRevisions.Clear();
            history.PolicyRevisionSignatures.Clear();
            history.LastPolicyArtifactSequence = 0L;
            history.LastPolicyArtifactLedgerId = "";
            history.Revision++;
            source.ClearSourceKeys();
            foreach (WorldDiplomacyDocument document in storage.Documents ?? new List<WorldDiplomacyDocument>())
            {
                if (document == null) continue;
                document.HistoryDeclarationRecorded = false;
                document.HistoryResultRecorded = false;
            }
        }
        history.LastPolicyArtifactLedgerId = (source.PublishedPolicyLedgerId() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(history.Snapshot.Content) && history.DeltaEntries.Count == 0)
        {
            List<string> legacy = new List<string>();
            foreach (WorldDiplomacyAnnualSummary summary in (storage.AnnualSummaries ?? new List<WorldDiplomacyAnnualSummary>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Summary)).OrderBy(x => x.Year).ThenBy(x => x.CreatedDay))
            {
                legacy.Add("[旧年度档案 " + summary.Year.ToString(CultureInfo.InvariantCulture) + "]\n" + summary.Summary.Trim());
            }
            foreach (WorldDiplomacyCompressionSummary summary in (storage.CompressionSummaries ?? new List<WorldDiplomacyCompressionSummary>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Summary)).OrderBy(x => x.CreatedDay).ThenBy(x => x.BatchId, StringComparer.OrdinalIgnoreCase))
            {
                legacy.Add("[旧压缩档案 " + (summary.BatchId ?? "") + "]\n" + summary.Summary.Trim());
            }
            foreach (WorldDiplomacyRoundSummary summary in (storage.RoundSummaries ?? new List<WorldDiplomacyRoundSummary>())
                .Where(x => x != null && !x.IsTokenCompressed && !string.IsNullOrWhiteSpace(x.Summary)).OrderBy(x => x.CreatedDay).ThenBy(x => x.RoundId, StringComparer.OrdinalIgnoreCase))
            {
                legacy.Add("[旧回合档案 " + (summary.RoundId ?? "") + "]\n" + summary.Summary.Trim());
            }
            if (legacy.Count > 0)
            {
                history.Snapshot.Content = "【从旧存档恢复的外交摘要；仅作历史背景】\n" + string.Join("\n", legacy.Distinct(StringComparer.Ordinal));
                history.Snapshot.CoveredThroughSequence = 0L;
                history.Snapshot.CreatedDay = source.CurrentDay;
                history.Snapshot.ContentHash = WorldDiplomacyPromptContractRules.StablePromptHash(history.Snapshot.Content);
                history.Snapshot.EstimatedTokens = WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens(history.Snapshot.Content, source.EstimateTokens);
            }
        }
        List<WorkItem> migrationItems = new List<WorkItem>();
        foreach (WorldDiplomacyDocument document in (storage.Documents ?? new List<WorldDiplomacyDocument>())
            .Where(x => x != null && x.IsReadyForPublication
                && (!string.IsNullOrWhiteSpace(x.Body)
                    || ((x.ChangedDiplomaticState || string.Equals(x.AnalysisStatus, "external_fact", StringComparison.OrdinalIgnoreCase))
                        && !string.IsNullOrWhiteSpace(x.MechanicalResult)))))
        {
            migrationItems.Add(new WorkItem
            {
                Day = Math.Max(0, document.Day),
                CreatedUtcTicks = Math.Max(0L, document.CreatedUtcTicks),
                StableKey = "document:" + (document.DocumentId ?? ""),
                Document = document
            });
        }
        foreach (WorldDiplomacyWeeklyArtifact report in source.WeeklyArtifacts())
        {
            if (report == null || string.IsNullOrWhiteSpace(report.SourceId) || string.IsNullOrWhiteSpace(report.Text)) continue;
            migrationItems.Add(new WorkItem
            {
                Day = Math.Max(0, report.Day),
                StableKey = "weekly:" + report.SourceId,
                WeeklyArtifact = report
            });
        }
        List<PublishedPolicyArtifactLedgerEntry> policyArtifacts = (source.PolicyArtifacts() ?? Enumerable.Empty<PublishedPolicyArtifactLedgerEntry>()).ToList();
        foreach (PublishedPolicyArtifactLedgerEntry policy in policyArtifacts)
        {
            if (policy == null || string.IsNullOrWhiteSpace(policy.PolicyId) || string.IsNullOrWhiteSpace(policy.PublishedText)) continue;
            migrationItems.Add(new WorkItem
            {
                Day = Math.Max(0, policy.OccurredDay),
                CreatedUtcTicks = Math.Max(0L, policy.CreatedUtcTicks),
                StableKey = "policy:" + policy.Sequence.ToString("D20", CultureInfo.InvariantCulture),
                Policy = policy
            });
        }
        foreach (WorkItem item in migrationItems
            .OrderBy(x => x.Day)
            .ThenBy(x => x.CreatedUtcTicks)
            .ThenBy(x => x.StableKey, StringComparer.OrdinalIgnoreCase))
        {
            if (item.Document != null) orchestration.AppendCanonicalDocumentEvents(item.Document);
            else if (item.WeeklyArtifact != null) orchestration.AppendCanonicalHistoryWeeklyArtifact(item.WeeklyArtifact);
            else if (item.Policy != null) orchestration.AppendPublishedPolicyArtifact(item.Policy);
        }
        orchestration.BackfillCanonicalResponseLinksV2();
        if (policyArtifacts.Count > 0)
        {
            history.LastPolicyArtifactSequence = Math.Max(history.LastPolicyArtifactSequence, policyArtifacts.Max(x => x.Sequence));
            source.AcknowledgePolicyArtifactsThrough(history.LastPolicyArtifactSequence);
        }
        source.SetObservedWeeklyRevision(source.WeeklyRevision());
        foreach (WorldDiplomacyRound round in (storage.CompletedRounds ?? new List<WorldDiplomacyRound>())
            .Concat(WorldDiplomacyLiveRoundRules.Live(storage)).Where(x => x != null))
        {
            round.LlmTranscript?.Clear();
            round.LlmProfiledKingdomIds?.Clear();
            round.LlmLastStateSignatureByKingdom?.Clear();
            round.CachePrefix = "";
            round.CommonContractSnapshot = "";
            round.CommonContractSnapshotInitialized = false;
            round.SchemaVersion = Math.Max(round.SchemaVersion, source.RelaySchemaVersion);
        }
        List<WorldDiplomacyJob> invalidJobs = new List<WorldDiplomacyJob>();
        foreach (WorldDiplomacyJob job in (storage.Jobs ?? new List<WorldDiplomacyJob>()).Where(x => x != null))
        {
            job.IsRunning = false;
            job.LlmMessages?.Clear();
            if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "compress"))
            {
                invalidJobs.Add(job);
                continue;
            }
            if (!orchestration.TryRebuildPendingJob(job)) invalidJobs.Add(job);
        }
        if (invalidJobs.Count > 0)
        {
            HashSet<string> invalidIds = new HashSet<string>(invalidJobs.Select(x => x.JobId), StringComparer.OrdinalIgnoreCase);
            storage.Jobs.RemoveAll(x => WorldDiplomacyRoundLifecycleRules.HasJobIdInSet(x, invalidIds));
            foreach (WorldDiplomacyJob invalidJob in invalidJobs)
            {
                if (WorldDiplomacyRoundLifecycleRules.ResolveExchange(storage?.ActiveExchange, storage?.SuspendedExchanges, invalidJob.ExchangeId) != null) orchestration.CompleteExchange(invalidJob.ExchangeId, "canonical_history_migration_retired_invalid_job");
            }
            foreach (WorldDiplomacyRound activeRound in WorldDiplomacyLiveRoundRules.Live(storage).ToList())
            {
                activeRound.RelayWaiting = false;
                bool hasRoundJob = storage.Jobs.Any(x => x != null && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, activeRound.RoundId));
                bool hasPublishedRoot = source.ResolveDocument(activeRound.RootDocumentId)?.IsReadyForPublication == true;
                if (!hasRoundJob && !hasPublishedRoot)
                {
                    if (orchestration is IWorldDiplomacyRoundClosure closure)
                        closure.CloseRound("canonical_history_migration_missing_root", activeRound);
                    else if (activeRound == storage.ActiveRound)
                        orchestration.CloseActiveRound("canonical_history_migration_missing_root");
                }
            }
        }
        WorldDiplomacyRoundLifecycleRules.RecalculateCanonicalHistoryTokens(storage, source.HistoryCompressionTriggerTokens);
        storage.HistoryMemorySchemaVersion = source.TargetHistoryMemorySchemaVersion;
        orchestration.InvalidateCanonicalHistoryRenderCache();
        source.Log("canonical diplomacy history migration completed entries=" + history.DeltaEntries.Count.ToString(CultureInfo.InvariantCulture)
            + " snapshot_tokens=" + history.Snapshot.EstimatedTokens.ToString(CultureInfo.InvariantCulture)
            + " retired_jobs=" + invalidJobs.Count.ToString(CultureInfo.InvariantCulture));
    }
}
