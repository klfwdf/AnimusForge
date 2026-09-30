using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge.Refactor.Persistence;

public static class WorldDiplomacyStorageMigration
{
    public static void MigrateDiplomacyPromptContractIfNeeded(
        WorldDiplomacyStorage storage,
        int targetVersion,
        bool worldReady,
        Func<WorldDiplomacyJob, bool> tryRebuildPendingJob,
        Action<string, string> completeExchange,
        Action clearLlmCacheAffinity,
        Action<string> log)
{
    if (storage == null || storage.PromptContractVersion >= targetVersion) return;
    if (!worldReady) return;
    List<WorldDiplomacyJob> retiredJobs = new List<WorldDiplomacyJob>();
    bool retiredCompression = false;
    foreach (WorldDiplomacyJob job in (storage.Jobs ?? new List<WorldDiplomacyJob>()).Where(x => x != null).ToList())
    {
        job.IsRunning = false;
        bool isAnalysis = WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "analyze");
        if (!WorldDiplomacyRoundLifecycleRules.UsesCanonicalHistory(job) && !isAnalysis) continue;
        job.LlmMessages?.Clear();
        job.SemanticRepairAttempts = 0;
        job.HistoryPrefixHash = "";
        if (WorldDiplomacyRoundLifecycleRules.IsJobOfKind(job, "compress"))
        {
            retiredCompression = true;
            retiredJobs.Add(job);
            continue;
        }
        if (tryRebuildPendingJob?.Invoke(job) != true) retiredJobs.Add(job);
    }
    if (retiredJobs.Count > 0)
    {
        HashSet<string> retiredIds = new HashSet<string>(retiredJobs.Select(x => x.JobId), StringComparer.OrdinalIgnoreCase);
        storage.Jobs.RemoveAll(x => WorldDiplomacyRoundLifecycleRules.HasJobIdInSet(x, retiredIds));
        foreach (WorldDiplomacyJob retired in retiredJobs.Where(x => !WorldDiplomacyRoundLifecycleRules.IsJobOfKind(x, "compress")))
        {
            if (WorldDiplomacyRoundLifecycleRules.ResolveExchange(storage?.ActiveExchange, storage?.SuspendedExchanges, retired.ExchangeId) != null) completeExchange?.Invoke(retired.ExchangeId, "prompt_contract_migration_retired_invalid_job");
        }
    }
    if (retiredCompression)
    {
        storage.DiplomacyCompressionPending = true;
        storage.CompressionRetryAfterHour = 0;
        storage.CompressionRetryAttempts = 0;
    }
    foreach (WorldDiplomacyRound round in (storage.CompletedRounds ?? new List<WorldDiplomacyRound>())
        .Concat(storage.ActiveRound == null ? Enumerable.Empty<WorldDiplomacyRound>() : new[] { storage.ActiveRound })
        .Where(x => x != null))
    {
        round.LlmTranscript?.Clear();
        round.LlmProfiledKingdomIds?.Clear();
        round.LlmLastStateSignatureByKingdom?.Clear();
        round.CachePrefix = "";
        round.CommonContractSnapshot = "";
        round.CommonContractSnapshotInitialized = false;
    }
    clearLlmCacheAffinity?.Invoke();
    storage.PromptContractVersion = targetVersion;
    log?.Invoke("diplomacy prompt contract migration completed version=" + targetVersion.ToString(CultureInfo.InvariantCulture)
        + " rebuilt_jobs=" + ((storage.Jobs ?? new List<WorldDiplomacyJob>()).Count(WorldDiplomacyRoundLifecycleRules.UsesCanonicalHistory)).ToString(CultureInfo.InvariantCulture)
        + " retired_jobs=" + retiredJobs.Count.ToString(CultureInfo.InvariantCulture)
        + " compression_requeued=" + retiredCompression.ToString());
}

    public static void MigrateAutonomousDecisionArchitectureIfNeeded(
        WorldDiplomacyStorage storage,
        int targetVersion,
        int relaySchemaVersion,
        int currentDay,
        bool worldReady,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Func<string, string> resolveEligibleKingdomId,
        Func<string, string, bool> isAtWarByKingdomId,
        Action<string> closeActiveRound,
        Action<string> log)
{
    if (storage == null || storage.DecisionArchitectureVersion >= targetVersion) return;
    if (storage.ActiveRound != null && !worldReady) return;
    int day = currentDay;
    List<WorldDiplomacyJob> retiredJobs = WorldDiplomacyRoundLifecycleRules.SelectRetiredArchitectureJobs(
        storage.Jobs);
    Dictionary<string, int> retiredByRound = retiredJobs
        .Where(x => WorldDiplomacyRoundLifecycleRules.IsJobOfKind(x, "generate")
            && !string.IsNullOrWhiteSpace(x.RoundId))
        .GroupBy(x => x.RoundId, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
    HashSet<string> retiredJobIds = new HashSet<string>(retiredJobs.Select(x => x.JobId), StringComparer.OrdinalIgnoreCase);
    storage.Jobs.RemoveAll(x => WorldDiplomacyRoundLifecycleRules.HasJobIdInSet(x, retiredJobIds));

    if (storage.ActiveExchange?.IsForced == true || !string.IsNullOrWhiteSpace(storage.ActiveExchange?.PendingAction))
    {
        storage.ActiveExchange.State = "closed_architecture_migration";
        storage.ActiveExchange.CompletedDay = day;
        storage.ActiveExchange = null;
    }
    storage.SuspendedExchanges.RemoveAll(x => x == null || x.IsForced || !string.IsNullOrWhiteSpace(x.PendingAction));
    storage.RecentTopicUses.Clear();
    foreach (WarPressureEntry entry in storage.WarPressure.Where(x => x != null))
    {
        entry.IsEscalationArmed = false;
        entry.ArmedDay = 0;
        entry.NeedsFreshEscalation = false;
    }
    storage.ForcedWarToggleWasEnabled = false;

    WorldDiplomacyRound active = storage.ActiveRound;
    if (active != null)
    {
        active.LlmTranscript ??= new List<WorldDiplomacyLlmMessage>();
        active.LlmProfiledKingdomIds ??= new List<string>();
        active.LlmLastStateSignatureByKingdom ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        active.LlmTranscript.Clear();
        active.LlmProfiledKingdomIds.Clear();
        active.LlmLastStateSignatureByKingdom.Clear();
        active.CachePrefix = "";
        active.RelayWaiting = false;
        active.RequiresSharedBorder = false;
        active.TopicSeedContext = "";
        active.TopicFingerprint = "";
        active.EventSourceType = "";
        active.EventMotif = "";
        active.EventLocation = "";
        active.AllowedFiction = "";
        active.ForbiddenFiction = "";
        active.PotentialActionIntents ??= new List<string>();
        active.PotentialActionIntents.Clear();
        foreach (WorldDiplomacyRoundParticipant participant in (active.Participants ?? new List<WorldDiplomacyRoundParticipant>()).Where(x => x != null))
        {
            participant.Role = "";
            participant.Agenda = "";
            participant.PrimaryTargetKingdomId = "";
            participant.PreferredOutcome = "";
            participant.RedLine = "";
            participant.Leverage = "";
            participant.RequiredContribution = "";
        }
        if (retiredByRound.TryGetValue(active.RoundId ?? "", out int retiredCount))
        {
            int publishedAutomatic = storage.Documents.Count(x => x != null && !x.IsPlayerAuthored
                && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, active.RoundId));
            active.AutomaticDocumentsStarted = Math.Max(publishedAutomatic, active.AutomaticDocumentsStarted - retiredCount);
        }
        storage.RelayArrivals.RemoveAll(x => x != null && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, active.RoundId));
        WorldDiplomacyDocument root = resolveDocument?.Invoke(active.RootDocumentId);
        string rootAuthorId = root == null ? null : resolveEligibleKingdomId(root.AuthorKingdomId);
        if (root?.IsReadyForPublication != true)
        {
            closeActiveRound?.Invoke("technical_architecture_migration_unpublished_round");
            storage.NextNormalRoundDay = day + 1;
        }
        else if (rootAuthorId == null)
        {
            closeActiveRound?.Invoke("technical_architecture_migration_invalid_root_author");
            storage.NextNormalRoundDay = day + 1;
        }
        else
        {
            active.RoundTopic = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(root.PlannedRoundTopic, root.Title, "外交交涉");
            active.TopicCategory = WorldDiplomacyIntentVocabulary.NormalizeIntent(root.Intent) is "warning" or "ultimatum"
                ? "war_escalation"
                : WorldDiplomacyIntentVocabulary.InferTopicCategory(active.RoundTopic, isAtWarByKingdomId(rootAuthorId, root.TargetKingdomId));
            active.SchemaVersion = relaySchemaVersion;
            if (active.RelayPlanned)
            {
                List<string> previousRoute = active.RelayRouteKingdomIds ?? new List<string>();
                string cursorKingdomId = active.RelayCursor >= 0 && active.RelayCursor < previousRoute.Count
                    ? previousRoute[active.RelayCursor]
                    : rootAuthorId;
                active.RelayRouteKingdomIds = (active.RelayRouteKingdomIds ?? new List<string>())
                    .Where(id => resolveEligibleKingdomId(id) != null)
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (!active.RelayRouteKingdomIds.Contains(rootAuthorId, StringComparer.OrdinalIgnoreCase))
                {
                    active.RelayRouteKingdomIds.Insert(0, rootAuthorId);
                }
                active.RelayDirection = active.RelayDirection < 0 ? -1 : 1;
                active.RelayCursor = active.RelayRouteKingdomIds.FindIndex(id => string.Equals(id, cursorKingdomId, StringComparison.OrdinalIgnoreCase));
                if (active.RelayCursor < 0) active.RelayCursor = active.RelayRouteKingdomIds.FindIndex(id => string.Equals(id, rootAuthorId, StringComparison.OrdinalIgnoreCase));
                if (active.RelayCursor < 0) active.RelayCursor = 0;
                HashSet<string> migratedRouteIds = new HashSet<string>(active.RelayRouteKingdomIds, StringComparer.OrdinalIgnoreCase);
                foreach (WorldDiplomacyRoundParticipant participant in (active.Participants ?? new List<WorldDiplomacyRoundParticipant>()).Where(x => x != null))
                {
                    participant.SelectedForRelay = migratedRouteIds.Contains(participant.KingdomId ?? "");
                }
                if (active.RelayRouteKingdomIds.Count < 2)
                {
                    active.RelayPlanned = false;
                    active.RelayCursor = 0;
                    active.RelayDirection = 1;
                }
            }
            active.CachePrefix = "";
        }
    }
    storage.DecisionArchitectureVersion = targetVersion;
    log?.Invoke("autonomous diplomacy architecture migration completed retiredJobs=" + retiredJobs.Count.ToString(CultureInfo.InvariantCulture)
        + " activeRound=" + (storage.ActiveRound?.RoundId ?? "none"));
}

    public static void MigratePolicyCountdownHistory(
        WorldDiplomacyCanonicalHistoryState history,
        List<WorldDiplomacyJob> jobs,
        Action invalidateRenderCache,
        Action<string> log)
{

    if (history.PolicyHistorySchemaVersion >= 1) return;
    // One cold pass, preserving sequences and non-policy facts. An increase in any
    // countdown, changed duration, status, body or effect remains a real observation.
    history.DeltaEntries = WorldDiplomacyPolicyHistoryRules.CollapseCountdownCopies(
        history.DeltaEntries,
        entry => entry.Kind == "policy_published" || entry.Kind == "policy_snapshot"
            ? entry.SourceId : null,
        entry => entry.Text, out int removed,
        (before, after) => before.Kind == after.Kind && before.GameDate == after.GameDate
            && before.AuthorKingdomId == after.AuthorKingdomId && before.Verified == after.Verified
            && before.Intent == after.Intent && before.Commitment == after.Commitment
            && before.RespondingToOfferDocumentId == after.RespondingToOfferDocumentId
            && before.RespondingToThreatDocumentId == after.RespondingToThreatDocumentId
            && (before.TargetKingdomIds ?? new List<string>()).SequenceEqual(after.TargetKingdomIds ?? new List<string>())
            && (before.ActionFacts ?? new List<string>()).SequenceEqual(after.ActionFacts ?? new List<string>()));
    foreach (WorldDiplomacyCanonicalHistoryEntry entry in history.DeltaEntries)
    {
        if ((entry.Kind == "policy_published" || entry.Kind == "policy_snapshot") && !string.IsNullOrWhiteSpace(entry.SourceId))
        {
            history.PolicyEventRevisions.TryGetValue(entry.SourceId, out long revision);
            history.PolicyEventRevisions[entry.SourceId] = revision + 1L;
        }
    }
    history.PolicyRevisionSignatures.Clear();
    history.LastPolicyArtifactSequence = 0L;
    history.LastPolicyArtifactRevision = 0L;
    history.LastPolicyArtifactLedgerId = "";
    history.PolicyHistorySchemaVersion = 1;
    history.Revision++;
    // Persisted repair chains contain their own copy of the old oversized history.
    foreach (WorldDiplomacyJob job in jobs ?? new List<WorldDiplomacyJob>())
    {
        if (job == null || job.IsRunning || !WorldDiplomacyRoundLifecycleRules.UsesCanonicalHistory(job)) continue;
        job.LlmMessages?.Clear();
        job.SemanticRepairAttempts = 0;
        job.HistoryPrefixHash = "";
    }
    invalidateRenderCache?.Invoke();
    log?.Invoke("policy countdown history migration removed=" + removed.ToString(CultureInfo.InvariantCulture)
        + " retained=" + history.DeltaEntries.Count.ToString(CultureInfo.InvariantCulture));
}

    public static void BackfillCanonicalResponseLinksV2(
        WorldDiplomacyStorage storage,
        Func<WorldDiplomacyDocument, List<string>, bool> appendResponseLink)
{

    if (storage == null || storage.HistoryMemorySchemaVersion >= 2) return;
    foreach (WorldDiplomacyDocument document in WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically((storage.Documents ?? new List<WorldDiplomacyDocument>())
            .Where(x => x != null && x.IsReadyForPublication
                && !string.IsNullOrWhiteSpace(x.DocumentId)
                && !string.IsNullOrWhiteSpace(x.RespondingToOfferDocumentId)
                && !string.IsNullOrWhiteSpace(x.Body))))
    {
        bool alreadyLinked = (storage.CanonicalHistory.DeltaEntries ?? new List<WorldDiplomacyCanonicalHistoryEntry>())
            .Any(x => x != null
                && WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.SourceId, document.DocumentId)
                && WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.RespondingToOfferDocumentId, document.RespondingToOfferDocumentId));
        if (alreadyLinked) continue;
        List<string> targets = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder((document.AddressedKingdomIds ?? new List<string>())
            .Concat(string.IsNullOrWhiteSpace(document.TargetKingdomId) ? Enumerable.Empty<string>() : new[] { document.TargetKingdomId }));
        appendResponseLink?.Invoke(document, targets);
    }
}

    public static void MigrateResultSettlementStateIfNeeded(
        WorldDiplomacyStorage storage,
        int targetVersion,
        Action<WorldDiplomacyRound, WorldDiplomacyDocument, string, string> beginResultSettlement,
        Action<string> log)
{

    if (storage == null
        || storage.ResultSettlementStateSchemaVersion >= targetVersion) return;
    WorldDiplomacyRound round = storage.ActiveRound;
    if (round != null && WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State)
        && !round.ResultSettlementPending)
    {
        List<WorldDiplomacyDocument> published = WorldDiplomacyRoundLifecycleRules.SelectPublishedRoundDocuments(
            storage.Documents, round.RoundId);
        foreach (WorldDiplomacyDocument document in published)
        {
            if (WorldDiplomacyRoundLifecycleRules.TryGetConfirmedRoundResult(document, round, storage?.DiplomaticThreats, out string closeReason, out string roundStatus))
            {
                beginResultSettlement?.Invoke(round, document, closeReason, roundStatus);
            }
        }
        WorldDiplomacyRoundLifecycleRules.RemoveAnsweredWarResponseSlots(round, published);
    }
    storage.ResultSettlementStateSchemaVersion = targetVersion;
    log?.Invoke("round result-settlement state migrated version="
        + targetVersion.ToString(CultureInfo.InvariantCulture)
        + " active=" + (round?.ResultSettlementPending == true).ToString());
}

    public static void NormalizeCanonicalHistoryState(
        WorldDiplomacyStorage storage,
        HashSet<string> sourceKeys,
        long compressionTriggerTokens,
        Func<string, int> estimateTokens,
        Action invalidateRenderCache,
        Action<string> log)
{
    storage.CanonicalHistory ??= new WorldDiplomacyCanonicalHistoryState();
    WorldDiplomacyCanonicalHistoryState history = storage.CanonicalHistory;
    history.Snapshot ??= new WorldDiplomacyCanonicalHistorySnapshot();
    history.Snapshot.PreservedResultSourceIds ??= new List<string>();
    history.Snapshot.ProtectedFacts ??= new List<WorldDiplomacyCanonicalProtectedFact>();
    history.Snapshot.ProtectedFacts = WorldDiplomacyRoundLifecycleRules.OrderProtectedFactsBySequence(
            history.Snapshot.ProtectedFacts
                .Select(WorldDiplomacyCanonicalRenderRules.CloneProtectedFact)
                .Where(x => x != null
                    && (x.Kind == "diplomatic_result" || x.Kind == "response_link")
                    && !string.IsNullOrWhiteSpace(x.SourceId)
                    && (x.Kind != "diplomatic_result" || !string.IsNullOrWhiteSpace(x.Text))
                    && (x.Kind != "response_link" || !string.IsNullOrWhiteSpace(x.RelatedSourceId)))
                .GroupBy(WorldDiplomacyCanonicalRenderRules.ProtectedFactStableKey, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.OrderBy(y => y.Sequence).First()))
        .ToList();
    history.Snapshot.PreservedResultSourceIds = WorldDiplomacyRoundLifecycleRules.NormalizeOrderedIdList(
        history.Snapshot.PreservedResultSourceIds
            .Concat(history.Snapshot.ProtectedFacts.Where(x => x.Kind == "diplomatic_result").Select(x => x.SourceId)));
    history.DeltaEntries ??= new List<WorldDiplomacyCanonicalHistoryEntry>();
    history.WorldWeeklySourceHashes ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    history.WorldWeeklySourceRevisions ??= new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
    history.PolicyRevisionSignatures ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    history.PolicyEventRevisions = WorldDiplomacyRoundLifecycleRules.NormalizeRevisionMapLong(history.PolicyEventRevisions);
    history.LastPolicyArtifactSequence = Math.Max(0L, history.LastPolicyArtifactSequence);
    history.LastPolicyArtifactLedgerId = (history.LastPolicyArtifactLedgerId ?? "").Trim();
    history.WorldWeeklySourceHashes = WorldDiplomacyRoundLifecycleRules.NormalizeRevisionMapString(history.WorldWeeklySourceHashes);
    history.WorldWeeklySourceRevisions = WorldDiplomacyRoundLifecycleRules.NormalizeRevisionMapLong(history.WorldWeeklySourceRevisions);
    history.PolicyRevisionSignatures = WorldDiplomacyRoundLifecycleRules.NormalizeRevisionMapString(history.PolicyRevisionSignatures);
    history.DeltaEntries.RemoveAll(x => WorldDiplomacyRoundLifecycleRules.IsDeltaEntryRetired(x, history.Snapshot.CoveredThroughSequence));
    history.DeltaEntries = WorldDiplomacyRoundLifecycleRules.OrderDeltaEntriesBySequence(history.DeltaEntries)
        .GroupBy(x => x.Sequence)
        .Select(x => x.First())
        .ToList();
    MigratePolicyCountdownHistory(history, storage.Jobs, invalidateRenderCache, log);
    sourceKeys.Clear();
    foreach (string sourceKey in history.DeltaEntries.Select(x => x?.SourceKey).Where(x => !string.IsNullOrWhiteSpace(x))) sourceKeys.Add(sourceKey);
    history.NextSequence = WorldDiplomacyRoundLifecycleRules.ResolveNextDeltaSequence(
        history.Snapshot.CoveredThroughSequence, history.NextSequence, history.DeltaEntries);
    foreach (WorldDiplomacyCanonicalHistoryEntry entry in history.DeltaEntries)
    {
        WorldDiplomacyRoundLifecycleRules.NormalizeDeltaEntryFields(entry);
        if (entry.EstimatedTokens <= 0) entry.EstimatedTokens = WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens(WorldDiplomacyCanonicalRenderRules.RenderCanonicalHistoryEntry(entry), estimateTokens);
    }
    string snapshotPayload = WorldDiplomacyCanonicalRenderRules.RenderCanonicalSnapshotPayload(history.Snapshot);
    history.Snapshot.EstimatedTokens = WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens(snapshotPayload, estimateTokens);
    history.Snapshot.ContentHash = WorldDiplomacyPromptContractRules.StablePromptHash(snapshotPayload);
    WorldDiplomacyRoundLifecycleRules.RecalculateCanonicalHistoryTokens(storage, compressionTriggerTokens);
}
    public static void NormalizeStoredDocumentRecord(
        WorldDiplomacyDocument document,
        WorldDiplomacyStorage storage,
        bool migrateLegacyPropagationState,
        int legacyPropagationRecoveryWindow,
        int currentDay,
        int maxActionsPerDocument,
        Func<string, string> resolveKingdomNameOrEmpty,
        Func<IEnumerable<string>, string, List<string>> normalizeKingdomIdList,
        Func<int, string> formatCampaignDate,
        IReadOnlyCollection<string> nonHideoutSettlementIds,
        IReadOnlyCollection<string> nonEliminatedKingdomIds)
{
    if (document.RoundProgressHandled) document.RoundAccountingHandled = true;
    document.NegotiationMove = WorldDiplomacyIntentVocabulary.NormalizeNegotiationMove(document.NegotiationMove);
    document.InternationalReputationEvaluationDelta = Math.Max(-WorldDiplomacyReputationRules.MaximumInternationalReputationChangePerDocument,
        Math.Min(WorldDiplomacyReputationRules.MaximumInternationalReputationChangePerDocument, document.InternationalReputationEvaluationDelta));
    document.InternationalReputationEvaluationReason = WorldDiplomacyTextRules.Limit((document.InternationalReputationEvaluationReason ?? "").Trim(), 240);
    document.InternationalReputationEvaluationSource = WorldDiplomacyTextRules.Limit(
        WorldDiplomacyIntentVocabulary.NormalizeToken(document.InternationalReputationEvaluationSource), 40);
    document.DiplomaticStandingChanges ??= new List<WorldDiplomacyStandingChange>();
    document.DiplomaticStandingChanges = document.DiplomaticStandingChanges
        .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Kind) && !string.IsNullOrWhiteSpace(x.KingdomId))
        .Take(16)
        .ToList();
    document.Title = WorldDiplomacyTextRules.Limit(WorldDiplomacyTextRules.SanitizePublicDiplomacyText(document.Title), 100);
    document.Body = WorldDiplomacyTextRules.NormalizeBody(WorldDiplomacyTextRules.SanitizePublicDiplomacyText(document.Body));
    document.AddressedKingdomIds ??= new List<string>();
    document.MentionedKingdomIds ??= new List<string>();
    document.PlannedKingdomIds ??= new List<string>();
    document.PresentedThreatDocumentIds = WorldDiplomacyRoundLifecycleRules.NormalizeThreatIdList(document.PresentedThreatDocumentIds);
    document.PresentedThreatFollowThroughDocumentIds = WorldDiplomacyRoundLifecycleRules.NormalizeThreatIdList(document.PresentedThreatFollowThroughDocumentIds);
    document.RespondingToOfferActionId = (document.RespondingToOfferActionId ?? "").Trim();
    document.RespondingToThreatDocumentId ??= "";
    document.RespondingToThreatActionId = (document.RespondingToThreatActionId ?? "").Trim();
    document.ResultSettlementSlotId ??= "";
    if (document.Actions != null)
    {
        List<WorldDiplomacyDocumentAction> normalizedActions = new List<WorldDiplomacyDocumentAction>(maxActionsPerDocument);
        HashSet<string> normalizedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (WorldDiplomacyDocumentAction action in document.Actions)
        {
            string targetId = (action?.TargetKingdomId ?? "").Trim();
            if (action == null || string.IsNullOrWhiteSpace(targetId)
                || string.IsNullOrWhiteSpace(action.Intent) || !normalizedTargets.Add(targetId)) continue;
            action.TargetKingdomId = targetId;
            normalizedActions.Add(action);
            if (normalizedActions.Count >= maxActionsPerDocument) break;
        }
        document.Actions = normalizedActions;
        HashSet<string> actionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int actionIndex = 0; actionIndex < document.Actions.Count; actionIndex++)
        {
            WorldDiplomacyDocumentAction action = document.Actions[actionIndex];
            action.ActionId = (action.ActionId ?? "").Trim();
            if (string.IsNullOrWhiteSpace(action.ActionId) || !actionIds.Add(action.ActionId))
            {
                int suffix = actionIndex + 1;
                do
                {
                    action.ActionId = "action_" + suffix.ToString(CultureInfo.InvariantCulture);
                    suffix++;
                }
                while (!actionIds.Add(action.ActionId));
            }
            action.TargetKingdomId = (action.TargetKingdomId ?? "").Trim();
            action.TargetKingdomName = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(resolveKingdomNameOrEmpty(action.TargetKingdomId), action.TargetKingdomName);
            action.Intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(action.Intent);
            action.NegotiationMove = WorldDiplomacyIntentVocabulary.NormalizeNegotiationMove(action.NegotiationMove);
            action.Commitment = WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent(action.Intent);
            action.RespondingToOfferDocumentId = (action.RespondingToOfferDocumentId ?? "").Trim();
            action.RespondingToOfferActionId = (action.RespondingToOfferActionId ?? "").Trim();
            action.RespondingToThreatDocumentId = (action.RespondingToThreatDocumentId ?? "").Trim();
            action.RespondingToThreatActionId = (action.RespondingToThreatActionId ?? "").Trim();
        }
        if (document.Actions.Count == 0) document.Actions = null;
        else
        {
            document.AddressedKingdomIds = normalizeKingdomIdList(document.Actions.Select(x => x.TargetKingdomId), document.AuthorKingdomId);
            WorldDiplomacyDocumentFactRules.MirrorPrimaryActionToDocument(document, document.Actions[0]);
            document.ChangedDiplomaticState = document.Actions.Any(x => x.ChangedDiplomaticState);
            document.MechanicalResult = WorldDiplomacyDocumentFactRules.BuildMultiActionMechanicalResult(document.Actions);
            document.RequiresResponse = document.Actions.Any(x => x.RequiresResponse);
        }
    }
    if (string.IsNullOrWhiteSpace(document.RoundId) && !string.IsNullOrWhiteSpace(document.ExchangeId)) document.RoundId = document.ExchangeId;
    if (document.AddressedKingdomIds.Count == 0 && !string.IsNullOrWhiteSpace(document.TargetKingdomId)) document.AddressedKingdomIds.Add(document.TargetKingdomId);
    if (string.IsNullOrWhiteSpace(document.GameDate))
    {
        document.GameDate = formatCampaignDate(document.Day);
    }
    if (string.Equals(document.TargetKingdomName, "未知王国", StringComparison.Ordinal))
    {
        document.TargetKingdomName = "";
    }
    if (!document.IsReadyForPublication
        && (!string.IsNullOrWhiteSpace(document.AnalysisStatus) || document.IsCompressed))
    {
        document.IsReadyForPublication = true;
    }
    if (migrateLegacyPropagationState && document.IsReadyForPublication)
    {
        bool belongsToActiveRound = storage.ActiveRound != null
            && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(storage.ActiveRound.RoundId, document.RoundId)
            && WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(storage.ActiveRound.State);
        bool stillRelevant = belongsToActiveRound || document.Day >= currentDay - legacyPropagationRecoveryWindow;
        if (!document.PropagationStarted && string.IsNullOrWhiteSpace(document.OriginSettlementId))
        {
            // Pre-propagation-format declarations were globally visible already.
            document.PropagationCompleted = true;
        }
        else
        {
            document.PropagationCompleted = !stillRelevant
                || WorldDiplomacyRoundLifecycleRules.HasCompleteLegacyPropagationCoverage(
                    document, storage.PropagationArrivals, storage.SettlementKnowledge,
                    storage.KingdomKnowledge, nonHideoutSettlementIds, nonEliminatedKingdomIds);
        }
    }
    if (document.IsReadyForPublication && !document.PropagationStarted && string.IsNullOrWhiteSpace(document.OriginSettlementId))
    {
        // Documents from the pre-propagation save format were globally visible already.
        document.HasReachedPlayerCourt = document.HasReachedPlayerCourt || !document.IsPlayerAuthored;
    }
}

    public static void NormalizeStoredRoundRecord(
        WorldDiplomacyRound round,
        WorldDiplomacyStorage storage,
        Dictionary<string, WorldDiplomacyDocument> documentsById,
        bool allowWorldValidation,
        int decisionArchitectureVersion,
        int relaySchemaVersion,
        int relayTargetDurationDays,
        int courtMaxDeliveryDays,
        int maxPendingPolicySignals,
        int maxConsecutiveTechnicalGenerationFailuresPerRound,
        Func<int, int> getRoundHardDurationDays,
        Action<WorldDiplomacyRound> pruneInvalidOffers,
        Action<string> log)
{
    round.ActionAttemptCountAtPassStart = WorldDiplomacyRoundLifecycleRules.ClampPassStartAttemptCount(
        round.DiplomaticActionAttemptCount, round.ActionAttemptCountAtPassStart);
    round.ConsecutiveNoActionPasses = WorldDiplomacyRoundLifecycleRules.ClampNoActionPassCount(
        round.ConsecutiveNoActionPasses);
    round.LastAccountedRelayPassNumber = WorldDiplomacyRoundLifecycleRules.ClampLastAccountedPassNumber(
        round.LastAccountedRelayPassNumber);
    WorldDiplomacyRoundLifecycleRules.NormalizeRoundRecordCollections(
        round, maxPendingPolicySignals, maxConsecutiveTechnicalGenerationFailuresPerRound);
    int normalizedOfferSourceBindings = 0;
    foreach (WorldDiplomacyRoundOffer offer in round.PendingOffers)
    {
        if (WorldDiplomacyRoundLifecycleRules.NormalizePendingOfferSourceActionBinding(offer, documentsById))
        {
            normalizedOfferSourceBindings++;
        }
    }
    if (normalizedOfferSourceBindings > 0)
    {
        log?.Invoke("diplomacy offer source-action bindings normalized round=" + round.RoundId
            + " count=" + normalizedOfferSourceBindings.ToString(CultureInfo.InvariantCulture));
    }
    if (allowWorldValidation
        && ReferenceEquals(round, storage.ActiveRound)
        && storage.DecisionArchitectureVersion >= decisionArchitectureVersion) pruneInvalidOffers?.Invoke(round);
    WorldDiplomacyRoundLifecycleRules.NormalizeRoundAttemptCount(round);
    int storedTargetDurationDays = WorldDiplomacyRoundLifecycleRules.ResolveStoredRoundDurationDays(
        round.SoftEndDay, round.StartedDay, relayTargetDurationDays);
    WorldDiplomacyRoundLifecycleRules.NormalizeRoundScheduleDays(
        round, storedTargetDurationDays, courtMaxDeliveryDays,
        getRoundHardDurationDays(storedTargetDurationDays));
    if (WorldDiplomacyRoundLifecycleRules.IsClosedRoundWithoutFinalDocument(round))
    {
        round.FinalDocumentId = WorldDiplomacyRoundLifecycleRules.SelectFinalRoundDocumentId(
            storage.Documents, round.RoundId);
    }
    WorldDiplomacyRoundLifecycleRules.ReleaseNonWaitingSettlementReplies(round);
    if (round.AutomaticDocumentsStarted <= 0)
    {
        round.AutomaticDocumentsStarted = WorldDiplomacyRoundLifecycleRules.CountAutomaticRoundDocuments(
            storage.Documents, round.RoundId);
    }
    if (allowWorldValidation
        && ReferenceEquals(round, storage.ActiveRound)
        && storage.DecisionArchitectureVersion >= decisionArchitectureVersion
        && round.SchemaVersion < relaySchemaVersion)
    {
        List<WorldDiplomacyJob> retiredRoundJobs = WorldDiplomacyRoundLifecycleRules.SelectRoundJobsOfKinds(
            storage.Jobs, round.RoundId, "generate", "round_plan");
        int retiredGenerateCount = retiredRoundJobs.Count(x => WorldDiplomacyRoundLifecycleRules.IsJobOfKind(x, "generate"));
        HashSet<string> retiredRoundJobIds = new HashSet<string>(retiredRoundJobs.Select(x => x.JobId), StringComparer.OrdinalIgnoreCase);
        storage.Jobs.RemoveAll(x => WorldDiplomacyRoundLifecycleRules.HasJobIdInSet(x, retiredRoundJobIds));
        int publishedAutomatic = WorldDiplomacyRoundLifecycleRules.CountAutomaticRoundDocuments(
            storage.Documents, round.RoundId);
        round.AutomaticDocumentsStarted = Math.Max(publishedAutomatic, round.AutomaticDocumentsStarted - retiredGenerateCount);
        storage.RelayArrivals.RemoveAll(x => x != null && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, round.RoundId));
        round.CachePrefix = "";
        round.LlmTranscript.Clear();
        round.LlmProfiledKingdomIds.Clear();
        round.LlmLastStateSignatureByKingdom.Clear();
        round.RelayWaiting = false;
        round.SchemaVersion = relaySchemaVersion;
    }
}

}
