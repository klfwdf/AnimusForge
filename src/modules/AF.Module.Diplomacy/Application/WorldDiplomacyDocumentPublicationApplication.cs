using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;

namespace AnimusForge;

// Publication ordering and retry policy, shared by compatibility and production callers.
internal static class WorldDiplomacyDocumentPublicationApplication
{
    internal static void FinalizePublishedDocumentAfterAnalysis(
        WorldDiplomacyDocument document,
        string authorKingdomId,
        string targetKingdomId,
        string normalizedIntent,
        bool recordNoActionDecision,
        List<WorldDiplomacyThreat> threats,
        Action<WorldDiplomacyDocument, string, string, string> recordThreatDecisions,
        Func<WorldDiplomacyDocument, string, string, string, bool> deferUnresolvedThreatAction,
        Action<WorldDiplomacyThreat, WorldDiplomacyDocument> applyThreatReputationPenalty,
        Action<WorldDiplomacyDocument> settleReputation,
        Action<WorldDiplomacyDocument, string> startPropagation,
        Action<WorldDiplomacyDocument> recordWeeklyMaterial,
        Action<WorldDiplomacyDocument> reconcilePlayerDeclaration,
        Action<WorldDiplomacyDocument> appendCanonicalEvents,
        Action<WorldDiplomacyThreat> appendThreatHistory,
        Action<WorldDiplomacyThreat> appendThreatDomesticPenaltyHistory,
        Action<WorldDiplomacyThreat> appendThreatIssuerRewardHistory,
        Action<WorldDiplomacyThreat, WorldDiplomacyThreatNonComplianceEvent> appendThreatNonComplianceHistory,
        Action<string> scheduleDeferredHistoryRetry,
        Action<WorldDiplomacyDocument> handleRoundDocumentProcessed,
        Action<string> log)
    {
        if (document == null || string.IsNullOrWhiteSpace(authorKingdomId)) return;
        document.IsReadyForPublication = true;
        if (recordNoActionDecision)
        {
            recordThreatDecisions?.Invoke(document, authorKingdomId, targetKingdomId, normalizedIntent);
        }
        bool requiredThreatActionDeferred = deferUnresolvedThreatAction?.Invoke(document, authorKingdomId, targetKingdomId, normalizedIntent) == true;
        if (!requiredThreatActionDeferred)
        {
            SettleDiplomaticThreatFollowThroughAfterDeclaration(
            document, threats, authorKingdomId, applyThreatReputationPenalty);
        }
        settleReputation?.Invoke(document);
        try
        {
            startPropagation?.Invoke(document, authorKingdomId);
        }
        catch (Exception ex)
        {
            document.PropagationCompleted = false;
            log?.Invoke("valid declaration propagation deferred document=" + document.DocumentId + " error=" + ex.Message);
        }
        try
        {
            recordWeeklyMaterial?.Invoke(document);
            reconcilePlayerDeclaration?.Invoke(document);
        }
        catch (Exception ex)
        {
            log?.Invoke("analyzed player declaration routing refresh deferred document=" + document.DocumentId + " error=" + ex.Message);
        }
        try
        {
            appendCanonicalEvents?.Invoke(document);
            FinalizeDiplomaticThreatHistoryAfterDocument(document,
            threats, appendThreatHistory,
            appendThreatDomesticPenaltyHistory,
            appendThreatIssuerRewardHistory);
            FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument(document,
            threats, appendThreatNonComplianceHistory);
        }
        catch (Exception ex)
        {
            scheduleDeferredHistoryRetry?.Invoke(document.DocumentId);
            log?.Invoke("canonical history append deferred document=" + document.DocumentId + " error=" + ex.Message);
        }
        try
        {
            handleRoundDocumentProcessed?.Invoke(document);
        }
        catch (Exception ex)
        {
            log?.Invoke("valid declaration round progress deferred document=" + document.DocumentId + " error=" + ex.Message);
        }
    }
}
