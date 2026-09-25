using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Domain;

public delegate bool CanonicalHistoryAppender(
    string kind,
    string sourceKey,
    string sourceId,
    int day,
    string gameDate,
    string authorKingdomId,
    IEnumerable<string> targetKingdomIds,
    string intent,
    string commitment,
    string content,
    bool verified,
    string respondingToOfferDocumentId = null,
    string respondingToThreatDocumentId = null,
    IEnumerable<string> actionFacts = null);

public static class WorldDiplomacyCanonicalHistoryRules
{
    public static bool AppendCanonicalHistoryEntry(
        WorldDiplomacyStorage storage,
        HashSet<string> sourceKeys,
        long compressionTriggerTokens,
        Action ensureInitialized,
        Func<string, string> newId,
        Func<int, string> formatCampaignDate,
        Func<string, int> estimateTokens,
        Action invalidateRenderCache,
        string kind,
        string sourceKey,
        string sourceId,
        int day,
        string gameDate,
        string authorKingdomId,
        IEnumerable<string> targetKingdomIds,
        string intent,
        string commitment,
        string content,
        bool verified,
        string respondingToOfferDocumentId = null,
        string respondingToThreatDocumentId = null,
        IEnumerable<string> actionFacts = null)
{
    ensureInitialized();
    string normalizedKind = (kind ?? "").Trim().ToLowerInvariant();
    string normalizedSourceKey = (sourceKey ?? "").Trim();
    string normalizedContent = WorldDiplomacyTextRules.NormalizeCanonicalHistoryText(content);
    if (string.IsNullOrWhiteSpace(normalizedKind) || string.IsNullOrWhiteSpace(normalizedSourceKey) || string.IsNullOrWhiteSpace(normalizedContent)) return false;
    WorldDiplomacyCanonicalHistoryState history = storage.CanonicalHistory;
    if (sourceKeys.Contains(normalizedSourceKey)) return false;
    WorldDiplomacyCanonicalHistoryEntry entry = new WorldDiplomacyCanonicalHistoryEntry
    {
        EntryId = newId("diplomacy_history"),
        SourceKey = normalizedSourceKey,
        Sequence = history.NextSequence++,
        Day = Math.Max(0, day),
        GameDate = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(gameDate, formatCampaignDate(Math.Max(0, day))),
        Kind = normalizedKind,
        SourceId = (sourceId ?? "").Trim(),
        RespondingToOfferDocumentId = (respondingToOfferDocumentId ?? "").Trim(),
        RespondingToThreatDocumentId = (respondingToThreatDocumentId ?? "").Trim(),
        AuthorKingdomId = (authorKingdomId ?? "").Trim(),
        TargetKingdomIds = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(targetKingdomIds),
        Intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(intent),
        Commitment = WorldDiplomacyIntentVocabulary.NormalizeCommitment(commitment),
        ActionFacts = actionFacts == null ? null : WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(actionFacts),
        Text = normalizedContent,
        Verified = verified
    };
    entry.EstimatedTokens = WorldDiplomacyRoundLifecycleRules.EstimateHistoryTokens(WorldDiplomacyCanonicalRenderRules.RenderCanonicalHistoryEntry(entry), estimateTokens);
    history.DeltaEntries.Add(entry);
    sourceKeys.Add(normalizedSourceKey);
    history.Revision++;
    history.EstimatedTokens += entry.EstimatedTokens;
    storage.DiplomacyTokensSinceCompression = history.EstimatedTokens;
    storage.DiplomacyCompressionPending = history.EstimatedTokens >= compressionTriggerTokens;
    invalidateRenderCache();
    return true;
}

    public static void AppendCanonicalDocumentEvents(
        WorldDiplomacyStorage storage,
        HashSet<string> sourceKeys,
        long compressionTriggerTokens,
        Action ensureInitialized,
        Func<string, string> newId,
        Func<int, string> formatCampaignDate,
        Func<string, int> estimateTokens,
        Action invalidateRenderCache,
        WorldDiplomacyDocument document)
{
    if (document == null || !document.IsReadyForPublication || string.IsNullOrWhiteSpace(document.DocumentId)) return;
    bool externalResolvedFact = string.Equals(document.AnalysisStatus, "external_fact", StringComparison.OrdinalIgnoreCase);
    if (externalResolvedFact) document.HistoryDeclarationRecorded = true;
    List<string> targets = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(
        (document.AddressedKingdomIds ?? new List<string>())
            .Concat(string.IsNullOrWhiteSpace(document.TargetKingdomId) ? Enumerable.Empty<string>() : new[] { document.TargetKingdomId }));
    List<string> actionFacts = document.Actions?.Where(x => x != null)
        .Select(x => (x.ActionId ?? "") + "@" + (x.TargetKingdomId ?? "") + "=" + WorldDiplomacyIntentVocabulary.NormalizeIntent(x.Intent))
        .ToList();
    string declarationIntent = document.Actions?.Count > 1 ? "multi_action" : document.Intent;
    string declarationCommitment = document.Actions?.Count > 1 ? "mixed" : document.Commitment;
    if (!document.HistoryDeclarationRecorded && !string.IsNullOrWhiteSpace(document.Body))
    {
        string declarationSourceKey = "document:" + document.DocumentId + ":declaration";
        bool appended = AppendCanonicalHistoryEntry(storage, sourceKeys, compressionTriggerTokens, ensureInitialized, newId, formatCampaignDate, estimateTokens, invalidateRenderCache, "declaration", declarationSourceKey, document.DocumentId,
            document.Day, document.GameDate, document.AuthorKingdomId, targets, declarationIntent, declarationCommitment, document.Body,
            verified: true, respondingToOfferDocumentId: document.RespondingToOfferDocumentId,
            respondingToThreatDocumentId: document.RespondingToThreatDocumentId,
            actionFacts: actionFacts);
        if (appended || WorldDiplomacyRoundLifecycleRules.CanonicalDeltaContainsSourceKey(sourceKeys, declarationSourceKey))
        {
            document.HistoryDeclarationRecorded = true;
        }
    }
    if (document.Actions?.Count > 0)
    {
        foreach (WorldDiplomacyDocumentAction action in document.Actions.Where(x => x != null
            && x.ChangedDiplomaticState && !string.IsNullOrWhiteSpace(x.MechanicalResult)))
        {
            string resultSourceKey = "document:" + document.DocumentId + ":action:" + action.ActionId + ":result";
            bool appended = AppendCanonicalHistoryEntry(storage, sourceKeys, compressionTriggerTokens, ensureInitialized, newId, formatCampaignDate, estimateTokens, invalidateRenderCache, "diplomatic_result", resultSourceKey, document.DocumentId,
                document.Day, document.GameDate, document.AuthorKingdomId, new[] { action.TargetKingdomId },
                action.Intent, action.Commitment, "经游戏机制确认：" + action.MechanicalResult,
                verified: true, respondingToOfferDocumentId: action.RespondingToOfferDocumentId,
                respondingToThreatDocumentId: action.RespondingToThreatDocumentId,
                actionFacts: new[] { (action.ActionId ?? "") + "@" + (action.TargetKingdomId ?? "") + "=" + WorldDiplomacyIntentVocabulary.NormalizeIntent(action.Intent) });
            if (appended || WorldDiplomacyRoundLifecycleRules.CanonicalDeltaContainsSourceKey(sourceKeys, resultSourceKey)) action.HistoryResultRecorded = true;
        }
        document.HistoryResultRecorded = document.Actions.Where(x => x != null && x.ChangedDiplomaticState
            && !string.IsNullOrWhiteSpace(x.MechanicalResult)).All(x => x.HistoryResultRecorded);
        return;
    }
    if (!document.HistoryResultRecorded && (document.ChangedDiplomaticState || externalResolvedFact) && !string.IsNullOrWhiteSpace(document.MechanicalResult))
    {
        string resultText = "经游戏机制确认：" + document.MechanicalResult;
        string resultSourceKey = "document:" + document.DocumentId + ":result";
        bool appended = AppendCanonicalHistoryEntry(storage, sourceKeys, compressionTriggerTokens, ensureInitialized, newId, formatCampaignDate, estimateTokens, invalidateRenderCache, "diplomatic_result", resultSourceKey, document.DocumentId,
            document.Day, document.GameDate, document.AuthorKingdomId, targets, document.Intent, document.Commitment, resultText,
            verified: true, respondingToOfferDocumentId: document.RespondingToOfferDocumentId,
            respondingToThreatDocumentId: document.RespondingToThreatDocumentId);
        if (appended || WorldDiplomacyRoundLifecycleRules.CanonicalDeltaContainsSourceKey(sourceKeys, resultSourceKey)
            || (storage.CanonicalHistory.Snapshot.PreservedResultSourceIds ?? new List<string>()).Contains(document.DocumentId, StringComparer.OrdinalIgnoreCase))
        {
            document.HistoryResultRecorded = true;
        }
    }
}

    public static void AppendPublishedWorldWeeklyArtifact(
        WorldDiplomacyStorage storage,
        HashSet<string> sourceKeys,
        long compressionTriggerTokens,
        Action ensureInitialized,
        Func<string, string> newId,
        Func<int, string> formatCampaignDate,
        Func<string, int> estimateTokens,
        Action invalidateRenderCache,
        string sourceId,
        string publishedTitle,
        string publishedReportText,
        int createdDay,
        string createdDate)
{
    if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(publishedReportText)) return;
    WorldDiplomacyCanonicalHistoryState history = storage.CanonicalHistory;
    publishedTitle = (publishedTitle ?? "").Trim();
    string publishedBody = publishedReportText.Trim();
    string hash = WorldDiplomacyPromptContractRules.StablePromptHash(publishedTitle + "\n" + publishedBody);
    history.WorldWeeklySourceHashes.TryGetValue(sourceId, out string previousHash);
    if (string.Equals(previousHash, hash, StringComparison.Ordinal)) return;
    history.WorldWeeklySourceRevisions.TryGetValue(sourceId, out long previousRevision);
    long revision = Math.Max(0L, previousRevision) + 1L;
    string sourceKey = "weekly:" + sourceId + ":r" + revision.ToString(CultureInfo.InvariantCulture);
    while (WorldDiplomacyRoundLifecycleRules.CanonicalDeltaContainsSourceKey(sourceKeys, sourceKey))
    {
        revision++;
        sourceKey = "weekly:" + sourceId + ":r" + revision.ToString(CultureInfo.InvariantCulture);
    }
    bool correction = !string.IsNullOrWhiteSpace(previousHash);
    string heading = correction ? "世界周报成品更正版" : "世界周报成品";
    string text = heading + (string.IsNullOrWhiteSpace(publishedTitle) ? "：\n" : "《" + publishedTitle + "》：\n") + publishedBody;
    if (AppendCanonicalHistoryEntry(storage, sourceKeys, compressionTriggerTokens, ensureInitialized, newId, formatCampaignDate, estimateTokens, invalidateRenderCache, "world_weekly", sourceKey, sourceId,
        createdDay, createdDate, "", Enumerable.Empty<string>(), "", "", text, verified: true))
    {
        history.WorldWeeklySourceHashes[sourceId] = hash;
        history.WorldWeeklySourceRevisions[sourceId] = revision;
    }
}

    internal static void SyncPublishedPolicyArtifacts(
        WorldDiplomacyStorage storage,
        HashSet<string> sourceKeys,
        long compressionTriggerTokens,
        Action ensureInitialized,
        Func<string, string> newId,
        Func<int, string> formatCampaignDate,
        Func<string, int> estimateTokens,
        Action invalidateRenderCache,
        Func<int> currentDay,
        Action<string> log,
        int maxBatches,
        int syncBatchSize,
        Func<string> getLedgerId,
        Func<long> getSourceRevision,
        Func<long> getAvailableSequence,
        Func<long, int, IReadOnlyList<PublishedPolicyArtifactLedgerEntry>> fetchArtifacts,
        Func<long, bool> acknowledgeArtifacts)
{
    WorldDiplomacyCanonicalHistoryState history = storage.CanonicalHistory;
    string ledgerId = (getLedgerId() ?? "").Trim();
    if (string.IsNullOrWhiteSpace(ledgerId)) return;
    long sourceRevision = getSourceRevision();
    if (history.LastPolicyArtifactRevision != sourceRevision)
    {
        // Snapshot sequence positions may shift after insertion/removal. Rescan only when
        // its semantic revision changes; durable per-policy fingerprints prevent replay.
        history.LastPolicyArtifactSequence = 0L;
        history.LastPolicyArtifactRevision = sourceRevision;
    }
    if (string.IsNullOrWhiteSpace(history.LastPolicyArtifactLedgerId))
    {
        history.LastPolicyArtifactLedgerId = ledgerId;
        if (history.LastPolicyArtifactSequence > 0L)
        {
            RebuildPublishedPolicySignaturesThrough(history, history.LastPolicyArtifactSequence, fetchArtifacts);
        }
    }
    else if (!string.Equals(history.LastPolicyArtifactLedgerId, ledgerId, StringComparison.Ordinal))
    {
        log("published policy ledger epoch changed old=" + history.LastPolicyArtifactLedgerId
            + " new=" + ledgerId + "; resynchronizing immutable artifacts");
        history.LastPolicyArtifactLedgerId = ledgerId;
        history.LastPolicyArtifactSequence = 0L;
    }
    long availableSequence = getAvailableSequence();
    long cursor = Math.Max(0L, history.LastPolicyArtifactSequence);
    if (cursor > availableSequence)
    {
        log("published policy cursor exceeds current ledger sequence cursor="
            + cursor.ToString(CultureInfo.InvariantCulture)
            + " available=" + availableSequence.ToString(CultureInfo.InvariantCulture)
            + "; resynchronizing immutable artifacts");
        cursor = 0L;
        history.LastPolicyArtifactSequence = 0L;
    }
    int batchLimit = Math.Max(1, maxBatches);
    for (int batch = 0; batch < batchLimit && cursor < availableSequence; batch++)
    {
        IReadOnlyList<PublishedPolicyArtifactLedgerEntry> entries = fetchArtifacts(cursor, syncBatchSize);
        if (entries == null || entries.Count == 0) break;
        long previousCursor = cursor;
        foreach (PublishedPolicyArtifactLedgerEntry policy in entries.OrderBy(x => x?.Sequence ?? long.MaxValue))
        {
            if (policy == null || policy.Sequence <= cursor) continue;
            if (!AppendPublishedPolicyArtifact(storage, sourceKeys, compressionTriggerTokens, ensureInitialized, newId, formatCampaignDate, estimateTokens, invalidateRenderCache, currentDay, policy)) break;
            cursor = policy.Sequence;
            history.LastPolicyArtifactSequence = cursor;
        }
        if (cursor <= previousCursor) break;
        acknowledgeArtifacts(cursor);
    }
}

    internal static void RebuildPublishedPolicySignaturesThrough(
        WorldDiplomacyCanonicalHistoryState history,
        long throughSequence,
        Func<long, int, IReadOnlyList<PublishedPolicyArtifactLedgerEntry>> fetchArtifacts)
{
    // v2 cursors are durable observations, not an event-log prefix. Rebuilding them
    // from today's snapshot would silently acknowledge changes made since the save.
    if (history.PolicyHistorySchemaVersion >= 1) return;
    long cutoff = Math.Max(0L, throughSequence);
    long cursor = 0L;
    while (cursor < cutoff)
    {
        IReadOnlyList<PublishedPolicyArtifactLedgerEntry> entries =
            fetchArtifacts(cursor, 1024);
        if (entries == null || entries.Count == 0) break;
        long previousCursor = cursor;
        foreach (PublishedPolicyArtifactLedgerEntry policy in entries.OrderBy(x => x?.Sequence ?? long.MaxValue))
        {
            if (policy == null || policy.Sequence <= cursor) continue;
            if (policy.Sequence > cutoff) return;
            if (WorldDiplomacyPolicyHistoryRules.TryBuildPublishedPolicySignature(policy, out string signatureKey, out string fingerprint))
            {
                history.PolicyRevisionSignatures[signatureKey] = fingerprint;
            }
            cursor = policy.Sequence;
        }
        if (cursor <= previousCursor) break;
    }
}

    internal static bool AppendPublishedPolicyArtifact(
        WorldDiplomacyStorage storage,
        HashSet<string> sourceKeys,
        long compressionTriggerTokens,
        Action ensureInitialized,
        Func<string, string> newId,
        Func<int, string> formatCampaignDate,
        Func<string, int> estimateTokens,
        Action invalidateRenderCache,
        Func<int> currentDay,
        PublishedPolicyArtifactLedgerEntry policy)
{
    string eventKind = (policy?.EventKind ?? "").Trim().ToLowerInvariant();
    if (policy == null
        || policy.Sequence <= 0L
        || policy.Revision <= 0L
        || string.IsNullOrWhiteSpace(policy.PolicyId)
        || string.IsNullOrWhiteSpace(policy.PublishedText)
        || (eventKind != "policy_published" && eventKind != "policy_snapshot")) return false;
    if (!WorldDiplomacyPolicyHistoryRules.TryBuildPublishedPolicySignature(policy, out string signatureKey, out string fingerprint)) return false;
    WorldDiplomacyCanonicalHistoryState history = storage.CanonicalHistory;
    long nextRevision = WorldDiplomacyPolicyHistoryRules.NextEventRevision(
        history.PolicyRevisionSignatures, history.PolicyEventRevisions, signatureKey, fingerprint);
    if (nextRevision == 0L) return true;
    history.PolicyEventRevisions.TryGetValue(signatureKey, out long previousRevision);
    string sourceKey = "policy:event-v2:" + signatureKey + ":r" + nextRevision.ToString(CultureInfo.InvariantCulture);
    if (WorldDiplomacyRoundLifecycleRules.CanonicalDeltaContainsSourceKey(sourceKeys, sourceKey))
    {
        history.PolicyRevisionSignatures[signatureKey] = fingerprint;
        history.PolicyEventRevisions[signatureKey] = nextRevision;
        return true;
    }
    StringBuilder text = new StringBuilder();
    text.Append("政策《").Append(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(policy.PolicyName, "未命名政策")).Append("》");
    if (!string.IsNullOrWhiteSpace(policy.KingdomName) || !string.IsNullOrWhiteSpace(policy.KingdomId))
    {
        text.Append("；发布国=").Append(WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(policy.KingdomName, policy.KingdomId));
    }
    if (!string.IsNullOrWhiteSpace(policy.ScopeKind)) text.Append("；范围=").Append(policy.ScopeKind.Trim());
    text.AppendLine().Append(policy.PublishedText.Trim());
    bool isChange = previousRevision > 0L;
    bool appended = AppendCanonicalHistoryEntry(storage, sourceKeys, compressionTriggerTokens, ensureInitialized, newId, formatCampaignDate, estimateTokens, invalidateRenderCache, isChange ? "policy_snapshot" : eventKind, sourceKey, policy.PolicyId,
        isChange ? currentDay() : policy.OccurredDay, isChange ? formatCampaignDate(currentDay()) : policy.GameDate, policy.KingdomId, Enumerable.Empty<string>(), "", "",
        text.ToString(), verified: true);
    if (appended)
    {
        history.PolicyRevisionSignatures[signatureKey] = fingerprint;
        history.PolicyEventRevisions[signatureKey] = nextRevision;
    }
    return appended;
}

    public static void TryAppendDiplomaticThreatDomesticPenaltyHistoryResult(
        WorldDiplomacyStorage storage,
        HashSet<string> sourceKeys,
        CanonicalHistoryAppender appendEntry,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Func<int, string> formatCampaignDate,
        Func<string, string> kingdomNameIncludingEliminated,
        Action<string> log,
        WorldDiplomacyThreat threat)
{
    if (!WorldDiplomacyRoundLifecycleRules.CanAppendDomesticPenaltyHistory(threat)) return;
    WorldDiplomacyDocument compliance = resolveDocument(threat.ComplianceDocumentId);
    if (compliance?.HistoryDeclarationRecorded != true) return;
    try
    {
        int appliedCount = WorldDiplomacyRoundLifecycleRules.CountThreatConsequenceAppliedClans(
            threat.DomesticPenaltyAppliedClanIds);
        int skippedCount = WorldDiplomacyRoundLifecycleRules.CountThreatConsequenceAppliedClans(
            threat.DomesticPenaltySkippedClanIds);

        string sourceKey = "threat:" + threat.ThreatId + ":domestic_penalty";
        string result = "经游戏机制确认：" + kingdomNameIncludingEliminated(threat.TargetKingdomId) + "明确退让后，已按每个正式封臣家族与退让时王族关系降低20点（最低为-100）的规则完成国内关系结算；已结算"
            + appliedCount.ToString(CultureInfo.InvariantCulture) + "个家族"
            + (skippedCount > 0 ? "，另有" + skippedCount.ToString(CultureInfo.InvariantCulture) + "个已无有效关系对象的家族未执行" : "") + "。";
        bool appended = appendEntry("diplomatic_result", sourceKey,
            WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(threat.ComplianceDocumentId, threat.ThreatId), threat.UpdatedDay,
            formatCampaignDate(threat.UpdatedDay), threat.TargetKingdomId, new[] { threat.IssuerKingdomId },
            "comply_ultimatum", "binding", result, verified: true,
            respondingToThreatDocumentId: threat.StageDocumentId);
        if (appended || WorldDiplomacyRoundLifecycleRules.CanonicalDeltaContainsSourceKey(sourceKeys, sourceKey)
            || (storage.CanonicalHistory?.Snapshot?.ProtectedFacts ?? new List<WorldDiplomacyCanonicalProtectedFact>())
                .Any(x => x != null && string.Equals(x.SourceKey, sourceKey, StringComparison.OrdinalIgnoreCase)))
        {
            threat.DomesticPenaltyHistoryRecorded = true;
        }
    }
    catch (Exception ex)
    {
        log("domestic penalty history append deferred threat=" + threat.ThreatId + " error=" + ex.Message);
    }
}

    public static void TryAppendDiplomaticThreatNonComplianceHistoryResult(
        WorldDiplomacyStorage storage,
        HashSet<string> sourceKeys,
        CanonicalHistoryAppender appendEntry,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Func<int, string> formatCampaignDate,
        Func<string, string> kingdomName,
        Action<string> log,
        WorldDiplomacyThreat threat,
        WorldDiplomacyThreatNonComplianceEvent decision)
{
    if (threat == null
        || !WorldDiplomacyRoundLifecycleRules.CanAppendNonComplianceEventHistory(decision)) return;
    WorldDiplomacyDocument response = resolveDocument(decision.DecisionDocumentId);
    if (response?.HistoryDeclarationRecorded != true) return;
    try
    {


        string stageLabel = WorldDiplomacyRoundLifecycleRules.DescribeThreatStageFormal(decision.Stage);
        string sourceKey = "threat:" + threat.ThreatId + ":target_noncompliance:" + decision.StageDocumentId;
        string result = "经游戏机制确认：" + kingdomName(threat.TargetKingdomId) + "在收到" + kingdomName(threat.IssuerKingdomId) + "的" + stageLabel
            + "后，其第一份已发布公文没有使用comply_ultimatum明确退让，因此已作出不退让决定。";
        bool appended = appendEntry("diplomatic_result", sourceKey,
            decision.DecisionDocumentId, decision.DecisionDay,
            formatCampaignDate(decision.DecisionDay), threat.TargetKingdomId,
            new[] { threat.IssuerKingdomId }, WorldDiplomacyIntentVocabulary.NormalizeIntent(response?.Intent),
            WorldDiplomacyIntentVocabulary.NormalizeCommitment(response?.Commitment), result, verified: true,
            respondingToThreatDocumentId: decision.StageDocumentId);
        if (appended || WorldDiplomacyRoundLifecycleRules.CanonicalDeltaContainsSourceKey(sourceKeys, sourceKey)
            || (storage.CanonicalHistory?.Snapshot?.ProtectedFacts ?? new List<WorldDiplomacyCanonicalProtectedFact>())
                .Any(x => x != null && string.Equals(x.SourceKey, sourceKey, StringComparison.OrdinalIgnoreCase)))
        {
            decision.HistoryRecorded = true;
            if (WorldDiplomacyRoundLifecycleRules.IsNonComplianceEventForCurrentStage(threat, decision))
            {
                threat.NonComplianceHistoryRecorded = true;
            }
        }
    }
    catch (Exception ex)
    {
        log("threat noncompliance history append deferred threat=" + threat.ThreatId + " error=" + ex.Message);
    }
}

    public static void TryAppendDiplomaticThreatHistoryResult(
        WorldDiplomacyStorage storage,
        HashSet<string> sourceKeys,
        CanonicalHistoryAppender appendEntry,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Func<int, string> formatCampaignDate,
        Func<string, string> kingdomName,
        Action<string> log,
        WorldDiplomacyThreat threat)
{
    if (!WorldDiplomacyRoundLifecycleRules.CanAppendThreatResultHistory(threat)) return;
    WorldDiplomacyDocument resolution = resolveDocument(threat.ResolutionDocumentId);
    if (resolution?.HistoryDeclarationRecorded != true) return;
    try
    {


        string stageLabel = WorldDiplomacyRoundLifecycleRules.DescribeThreatStage(
            WorldDiplomacyRoundLifecycleRules.NormalizeThreatEventStage(threat.Stage));
        string expected = WorldDiplomacyRoundLifecycleRules.DescribeThreatExpectedFollowThrough(threat.Stage);
        string sourceKey = "threat:" + threat.ThreatId + ":reputation_penalty";
        string result = "经游戏机制确认：" + kingdomName(threat.IssuerKingdomId) + "未在其下一份已发布公文中对"
            + kingdomName(threat.TargetKingdomId) + expected + "，此前" + stageLabel + "未获兑现，国家威望降低"
            + threat.ReputationPenaltyAmount.ToString(CultureInfo.InvariantCulture) + "点。";
        bool appended = appendEntry("diplomatic_result", sourceKey,
            WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(threat.ResolutionDocumentId, threat.StageDocumentId, threat.ThreatId),
            threat.UpdatedDay, formatCampaignDate(threat.UpdatedDay), threat.IssuerKingdomId,
            new[] { threat.TargetKingdomId }, threat.Stage,
            WorldDiplomacyRoundLifecycleRules.ResolveThreatCommitmentLevel(threat.Stage),
            result, verified: true, respondingToThreatDocumentId: threat.StageDocumentId);
        if (appended || WorldDiplomacyRoundLifecycleRules.CanonicalDeltaContainsSourceKey(sourceKeys, sourceKey)
            || (storage.CanonicalHistory?.Snapshot?.ProtectedFacts ?? new List<WorldDiplomacyCanonicalProtectedFact>())
                .Any(x => x != null && string.Equals(x.SourceKey, sourceKey, StringComparison.OrdinalIgnoreCase)))
        {
            threat.HistoryResultRecorded = true;
        }
    }
    catch (Exception ex)
    {
        log("national prestige penalty history append deferred threat=" + threat.ThreatId + " error=" + ex.Message);
    }
}

    public static void TryAppendDiplomaticThreatIssuerRewardHistoryResult(
        WorldDiplomacyStorage storage,
        HashSet<string> sourceKeys,
        CanonicalHistoryAppender appendEntry,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Func<int, string> formatCampaignDate,
        Func<string, string> kingdomNameIncludingEliminated,
        Action<string> log,
        WorldDiplomacyThreat threat)
{
    if (!WorldDiplomacyRoundLifecycleRules.CanAppendIssuerRewardHistory(threat)) return;
    WorldDiplomacyDocument compliance = resolveDocument(threat.ComplianceDocumentId);
    if (compliance?.HistoryDeclarationRecorded != true) return;
    int rewardAmount = Math.Max(0, threat.IssuerRewardAmount);
    int appliedCount = WorldDiplomacyRoundLifecycleRules.CountThreatConsequenceAppliedClans(
        threat.IssuerRewardAppliedClanIds);
    if (WorldDiplomacyRoundLifecycleRules.IsIssuerRewardHistoryEmpty(rewardAmount, appliedCount))
    {
        threat.IssuerRewardHistoryRecorded = true;
        return;
    }
    try
    {
        int skippedCount = WorldDiplomacyRoundLifecycleRules.CountThreatConsequenceAppliedClans(
            threat.IssuerRewardSkippedClanIds);

        string sourceKey = "threat:" + threat.ThreatId + ":issuer_relation_reward";
        string result = "经游戏机制确认：" + kingdomNameIncludingEliminated(threat.IssuerKingdomId) + "迫使对方明确退让后，已按每个正式封臣家族与退让时王族关系增加"
            + rewardAmount.ToString(CultureInfo.InvariantCulture) + "点（最高为100）的规则完成国内关系奖励；已结算"
            + appliedCount.ToString(CultureInfo.InvariantCulture) + "个家族"
            + (skippedCount > 0 ? "，另有" + skippedCount.ToString(CultureInfo.InvariantCulture) + "个已无有效关系对象的家族未执行" : "") + "。";
        bool appended = appendEntry("diplomatic_result", sourceKey,
            WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(threat.ComplianceDocumentId, threat.ThreatId), threat.UpdatedDay,
            formatCampaignDate(threat.UpdatedDay), threat.IssuerKingdomId, new[] { threat.TargetKingdomId },
            "comply_ultimatum", "binding", result, verified: true,
            respondingToThreatDocumentId: threat.StageDocumentId);
        if (appended || WorldDiplomacyRoundLifecycleRules.CanonicalDeltaContainsSourceKey(sourceKeys, sourceKey)
            || (storage.CanonicalHistory?.Snapshot?.ProtectedFacts ?? new List<WorldDiplomacyCanonicalProtectedFact>())
                .Any(x => x != null && string.Equals(x.SourceKey, sourceKey, StringComparison.OrdinalIgnoreCase)))
        {
            threat.IssuerRewardHistoryRecorded = true;
        }
    }
    catch (Exception ex)
    {
        log("issuer relation reward history append deferred threat=" + threat.ThreatId + " error=" + ex.Message);
    }
}
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

    public static string RenderCanonicalHistoryBlock(
        WorldDiplomacyCanonicalHistoryState history,
        long cutoff)
{
StringBuilder sb = new StringBuilder();
    sb.AppendLine("【全局长期外交历史】");
    sb.AppendLine("本档案对所有王国可见；动态当前状态与本档案冲突时，以动态当前状态为准。提议或宣言不等于已执行结果，只有 verified=true 的 diplomatic_result 表示游戏机制已确认改变现实状态。");
    string snapshotPayload = WorldDiplomacyCanonicalRenderRules.RenderCanonicalSnapshotPayload(history.Snapshot);
    if (!string.IsNullOrWhiteSpace(snapshotPayload) && history.Snapshot.CoveredThroughSequence <= cutoff)
    {
        sb.AppendLine("【已压缩历史；覆盖至seq=" + history.Snapshot.CoveredThroughSequence.ToString(CultureInfo.InvariantCulture) + "】");
        sb.AppendLine(snapshotPayload);
    }
    foreach (WorldDiplomacyCanonicalHistoryEntry entry in WorldDiplomacyRoundLifecycleRules.SelectDeltaEntriesThrough(history.DeltaEntries, cutoff))
    {
        sb.AppendLine(WorldDiplomacyCanonicalRenderRules.RenderCanonicalHistoryEntry(entry));
    }
    if (string.IsNullOrWhiteSpace(snapshotPayload) && !history.DeltaEntries.Any(x => x != null && x.Sequence <= cutoff)) sb.AppendLine("（暂无历史记录）");
    return sb.ToString().TrimEnd();
    }

    public static long ClampCanonicalHistoryThroughSequence(
        WorldDiplomacyCanonicalHistoryState history,
        long throughSequence)
    {
        return Math.Min(throughSequence,
            Math.Max(history.Snapshot.CoveredThroughSequence, history.NextSequence - 1L));
    }

    public static void StampCanonicalHistoryOnJob(
        WorldDiplomacyJob job,
        WorldDiplomacyCanonicalHistoryState history,
        string historyBlock)
    {
        job.HistoryRevision = history.Revision;
        job.HistoryEstimatedTokens = history.EstimatedTokens;
        job.HistorySnapshotThroughSequence = history.Snapshot.CoveredThroughSequence;
        job.HistorySnapshotHash = history.Snapshot.ContentHash ?? "";
        job.HistoryPrefixHash = WorldDiplomacyPromptContractRules.StablePromptHashPair(job.SystemPrompt, historyBlock);
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
