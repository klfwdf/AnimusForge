using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Owns ordered canonical-state updates through existing synchronous dependency ports.
internal static class WorldDiplomacyHistoryPublicationApplication
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
        IEnumerable<string> actionFacts = null,
        IEnumerable<string> answeredPlayerDocumentIds = null)
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
        AnsweredPlayerDocumentIds = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(answeredPlayerDocumentIds),
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
            actionFacts: actionFacts, answeredPlayerDocumentIds: document.AnsweredPlayerDocumentIds);
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
}
