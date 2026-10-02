using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge.Refactor.Persistence;

public static class WorldDiplomacyThreatStorageMigration
    {
    private const int DaysPerYear = 84;
    public const int DiplomaticThreatStateSchemaVersion = 3;
    public const int MaxStoredDiplomaticThreats = 96;
    public const int DiplomaticThreatRetentionDays = DaysPerYear * 2;

    public static void NormalizeDiplomaticThreats(
        WorldDiplomacyStorage storage,
        Func<WorldDiplomacyThreat, string> validateOpenThreatWorld,
        int currentDay,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Func<string> newId,
        int issuerRelationRewardMax,
        Action<string> log)
    {
    if (storage.DiplomaticThreatStateSchemaVersion < DiplomaticThreatStateSchemaVersion)
    {
        int previousThreatSchemaVersion = storage.DiplomaticThreatStateSchemaVersion;
        if (storage.DiplomaticThreatStateSchemaVersion <= 0)
        {
            // Older releases did not persist enforceable threat obligations. Never infer them
            // from old prose, because that would create a retroactive prestige penalty.
            storage.DiplomaticThreats.Clear();
            storage.NationalPrestigeByKingdom.Clear();
        }
        else
        {
            if (storage.DiplomaticThreatStateSchemaVersion < 2)
            {
                MigrateDiplomaticThreatsToNextDeclarationRules(storage, currentDay, resolveDocument, log);
            }
            if (storage.DiplomaticThreatStateSchemaVersion < 3)
            {
                MigrateThreatComplianceConsequencesV3(storage);
            }
        }
        storage.DiplomaticThreatStateSchemaVersion = DiplomaticThreatStateSchemaVersion;
        log?.Invoke("diplomatic threat state migrated previous=" + previousThreatSchemaVersion.ToString(CultureInfo.InvariantCulture)
            + " current=" + DiplomaticThreatStateSchemaVersion.ToString(CultureInfo.InvariantCulture));
    }

    storage.NationalPrestigeByKingdom = WorldDiplomacyRoundLifecycleRules.NormalizeKingdomIntDictionary(
        storage.NationalPrestigeByKingdom, 0, WorldDiplomacyReputationRules.DefaultNationalPrestige);
    storage.InternationalReputationByKingdom = WorldDiplomacyRoundLifecycleRules.NormalizeKingdomIntDictionary(
        storage.InternationalReputationByKingdom, 0, 100);
    storage.InternationalReputationNaturalChangeLastDayByKingdom =
        WorldDiplomacyRoundLifecycleRules.NormalizeKingdomIntDictionary(
            storage.InternationalReputationNaturalChangeLastDayByKingdom, 0, int.MaxValue);
    storage.NationalPrestigeRelationModifiers =
        WorldDiplomacyRoundLifecycleRules.SelectRetainedPrestigeRelationModifiers(
            storage.NationalPrestigeRelationModifiers);

    storage.DiplomaticThreats = storage.DiplomaticThreats
        .Where(x => x != null)
        .ToList();
    foreach (WorldDiplomacyThreat threat in storage.DiplomaticThreats)
    {
        WorldDiplomacyRoundLifecycleRules.NormalizeThreatRecord(threat, newId, issuerRelationRewardMax);
    }
    storage.DiplomaticThreats.RemoveAll(x => !WorldDiplomacyRoundLifecycleRules.HasValidThreatParties(x));

    foreach (IGrouping<string, WorldDiplomacyThreat> issuerGroup in storage.DiplomaticThreats
        .Where(x => x != null && WorldDiplomacyRoundLifecycleRules.IsOpenDiplomaticThreatStatus(x.Status))
        .GroupBy(x => x.IssuerKingdomId, StringComparer.OrdinalIgnoreCase))
    {
        WorldDiplomacyThreat retained =
            WorldDiplomacyRoundLifecycleRules.SelectRetainedOpenThreatForIssuer(issuerGroup);
        foreach (WorldDiplomacyThreat duplicate in issuerGroup.Where(x => !ReferenceEquals(x, retained)))
        {
            WorldDiplomacyRoundLifecycleRules.InvalidateThreatForNormalization(duplicate, "duplicate_open_threat_for_issuer", currentDay);
        }
    }

    if (validateOpenThreatWorld != null)
    {
        foreach (WorldDiplomacyThreat threat in storage.DiplomaticThreats.Where(x => x != null && WorldDiplomacyRoundLifecycleRules.IsOpenDiplomaticThreatStatus(x.Status)))
        {
            string worldVerdict = validateOpenThreatWorld(threat);
            if (string.Equals(worldVerdict, "war_already_started_outside_pending_declaration", StringComparison.Ordinal))
            {
                threat.Status = "invalidated";
                threat.ResolutionReason = "war_already_started_outside_pending_declaration";
                threat.UpdatedDay = Math.Max(threat.UpdatedDay, currentDay);
                threat.ObligationRoundId = "";
                threat.ObligationClaimedDay = 0;
                // The war itself is already recorded by the game event/history path.
                threat.HistoryResultRecorded = true;
                continue;
            }
            if (worldVerdict != null)
            {
                WorldDiplomacyRoundLifecycleRules.InvalidateThreatForNormalization(threat, worldVerdict, currentDay);
            }
        }
    }

    int cutoffDay = currentDay - DiplomaticThreatRetentionDays;
    storage.DiplomaticThreats = WorldDiplomacyRoundLifecycleRules.SelectThreatRetentionSet(
        storage.DiplomaticThreats, cutoffDay, MaxStoredDiplomaticThreats);
    }

    public static void MigrateDiplomaticThreatsToNextDeclarationRules(
        WorldDiplomacyStorage storage,
        int currentDay,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Action<string> log)
    {
    List<WorldDiplomacyDocument> orderedDocuments = WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically((storage?.Documents ?? new List<WorldDiplomacyDocument>())
            .Where(x => x != null && x.IsReadyForPublication && !string.IsNullOrWhiteSpace(x.DocumentId)))
        .ThenBy(x => x.DocumentId, StringComparer.OrdinalIgnoreCase)
        .ToList();
    Dictionary<string, int> documentIndex = orderedDocuments
        .Select((document, index) => new { document.DocumentId, Index = index })
        .GroupBy(x => x.DocumentId, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(x => x.Key, x => x.First().Index, StringComparer.OrdinalIgnoreCase);
    foreach (WorldDiplomacyThreat threat in (storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>()).Where(x => x != null && WorldDiplomacyRoundLifecycleRules.IsOpenDiplomaticThreatStatus(x.Status)).ToList())
    {
        threat.Stage = WorldDiplomacyRoundLifecycleRules.ResolveCanonicalThreatStage(
            threat.Stage, threat.UltimatumDocumentId);
        threat.StageDocumentId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(threat.StageDocumentId,
            WorldDiplomacyRoundLifecycleRules.SelectThreatStageDocumentId(threat));
        threat.ObligationRoundId = "";
        threat.ObligationClaimedDay = 0;
        for (int stagePass = 0; stagePass < 2 && WorldDiplomacyRoundLifecycleRules.IsOpenDiplomaticThreatStatus(threat?.Status); stagePass++)
        {
            WorldDiplomacyDocument source = resolveDocument?.Invoke(threat.StageDocumentId);
            bool sourceMatchesThreat = WorldDiplomacyRoundLifecycleRules.IsThreatStageSourceDocument(
                source, threat, documentIndex.ContainsKey(threat.StageDocumentId));
            if (!sourceMatchesThreat)
            {
                WorldDiplomacyRoundLifecycleRules.InvalidateThreatForNormalization(threat, "source_document_structure_mismatch", currentDay);
                log?.Invoke("legacy open diplomatic threat invalidated without prestige penalty threat=" + threat.ThreatId
                    + " source=" + threat.StageDocumentId + " reason=source_document_structure_mismatch");
                break;
            }
            threat.TargetDecision = "pending";
            threat.TargetDecisionDocumentId = "";
            threat.TargetDecisionRoundId = "";
            threat.TargetDecisionDay = 0;
            threat.NonComplianceHistoryRecorded = false;
            if (!documentIndex.TryGetValue(threat.StageDocumentId, out int sourceIndex)) break;
            WorldDiplomacyDocument firstTargetDeclaration = orderedDocuments.Skip(sourceIndex + 1)
                .FirstOrDefault(x => string.Equals(x.AuthorKingdomId, threat.TargetKingdomId, StringComparison.OrdinalIgnoreCase));
            if (firstTargetDeclaration == null) break;
            bool wasValidCompliance = WorldDiplomacyIntentVocabulary.NormalizeIntent(firstTargetDeclaration.Intent) == "comply_ultimatum"
                && string.Equals(firstTargetDeclaration.TargetKingdomId, threat.IssuerKingdomId, StringComparison.OrdinalIgnoreCase)
                && WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(firstTargetDeclaration.RespondingToThreatDocumentId, threat.StageDocumentId);
            if (wasValidCompliance)
            {
                threat.Status = "complied";
                threat.TargetDecision = "complied";
                threat.TargetDecisionDocumentId = firstTargetDeclaration.DocumentId ?? "";
                threat.TargetDecisionRoundId = firstTargetDeclaration.RoundId ?? "";
                threat.TargetDecisionDay = Math.Max(0, firstTargetDeclaration.Day);
                threat.ComplianceDocumentId = firstTargetDeclaration.DocumentId ?? "";
                threat.ResolutionDocumentId = firstTargetDeclaration.DocumentId ?? "";
                threat.ResolutionRoundId = firstTargetDeclaration.RoundId ?? "";
                threat.ResolutionReason = "migrated_valid_compliance_first_declaration";
                threat.IssuerResolutionNoticePending = true;
                threat.UpdatedDay = Math.Max(threat.UpdatedDay, threat.TargetDecisionDay);
                break;
            }
            threat.TargetDecision = "noncomplied";
            threat.TargetDecisionDocumentId = firstTargetDeclaration.DocumentId ?? "";
            threat.TargetDecisionRoundId = firstTargetDeclaration.RoundId ?? "";
            threat.TargetDecisionDay = Math.Max(0, firstTargetDeclaration.Day);
            threat.ResolutionReason = "migrated_target_noncompliance_first_declaration";
            threat.UpdatedDay = Math.Max(threat.UpdatedDay, threat.TargetDecisionDay);
            WorldDiplomacyRoundLifecycleRules.CaptureThreatNonComplianceEvent(threat);

            if (!documentIndex.TryGetValue(firstTargetDeclaration.DocumentId, out int targetDecisionIndex)) break;
            WorldDiplomacyDocument firstIssuerFollowThrough = orderedDocuments.Skip(targetDecisionIndex + 1)
                .FirstOrDefault(x => string.Equals(x.AuthorKingdomId, threat.IssuerKingdomId, StringComparison.OrdinalIgnoreCase));
            if (firstIssuerFollowThrough == null) break;
            string followThroughIntent = WorldDiplomacyIntentVocabulary.NormalizeIntent(firstIssuerFollowThrough.Intent);
            bool targetsThreatTarget = string.Equals(firstIssuerFollowThrough.TargetKingdomId, threat.TargetKingdomId, StringComparison.OrdinalIgnoreCase);
            if (WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughEscalation(
                threat.Stage, followThroughIntent, targetsThreatTarget))
            {
                threat.Stage = "ultimatum";
                threat.UltimatumDocumentId = firstIssuerFollowThrough.DocumentId ?? "";
                threat.StageDocumentId = firstIssuerFollowThrough.DocumentId ?? "";
                threat.StageRoundId = firstIssuerFollowThrough.RoundId ?? "";
                threat.StageIssuedDay = Math.Max(0, firstIssuerFollowThrough.Day);
                threat.UpdatedDay = Math.Max(threat.UpdatedDay, threat.StageIssuedDay);
                continue;
            }
            if (WorldDiplomacyRoundLifecycleRules.IsThreatFollowThroughEnforcement(
                threat.Stage, followThroughIntent, targetsThreatTarget,
                firstIssuerFollowThrough.ChangedDiplomaticState))
            {
                threat.Status = "enforced";
                threat.ResolutionDocumentId = firstIssuerFollowThrough.DocumentId ?? "";
                threat.ResolutionRoundId = firstIssuerFollowThrough.RoundId ?? "";
                threat.ResolutionReason = "migrated_issuer_declared_war_in_next_declaration";
                threat.HistoryResultRecorded = true;
                threat.UpdatedDay = Math.Max(threat.UpdatedDay, Math.Max(0, firstIssuerFollowThrough.Day));
                break;
            }

            WorldDiplomacyRoundLifecycleRules.InvalidateThreatForNormalization(threat, "legacy_next_declaration_already_consumed_without_retroactive_penalty", currentDay);
            log?.Invoke("legacy threat follow-through closed without retroactive prestige penalty threat=" + threat.ThreatId
                + " declaration=" + firstIssuerFollowThrough.DocumentId + " intent=" + followThroughIntent);
            break;
        }
    }
    }

    public static void MigrateThreatComplianceConsequencesV3(WorldDiplomacyStorage storage)
    {
    foreach (WorldDiplomacyThreat threat in storage?.DiplomaticThreats ?? new List<WorldDiplomacyThreat>())
    {
        WorldDiplomacyRoundLifecycleRules.MigrateThreatComplianceConsequencesV3(threat);
    }
    }
}
