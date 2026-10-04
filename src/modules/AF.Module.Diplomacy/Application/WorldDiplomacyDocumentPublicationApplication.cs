using System;
using System.Linq;
using System.Collections.Generic;
using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;

namespace AnimusForge;

// Publication ordering and retry policy, shared by compatibility and production callers.
internal static class WorldDiplomacyDocumentPublicationApplication
{
    internal static void PublishPlayerImmediately(WorldDiplomacyDocument document, IWorldDiplomacyPublicationPort port,
        IWorldDiplomacyOrchestration orchestration)
    {
        if (document?.IsPlayerAuthored != true) return;
        document.IsReadyForPublication = true;
        document.AnalysisStatus = "pending_analysis";
        string author = port.ResolveKingdomId(document.AuthorKingdomId);
        if (author == null) return;
        try
        {
            WorldDiplomacyPublicationRoutingApplication.Start(port, orchestration, document, author);
        }
        catch (Exception ex)
        {
            // Visibility survives propagation failure; the bounded retry path repairs delivery.
            document.PropagationCompleted = false;
            port.Log("immediate player declaration propagation deferred document=" + document.DocumentId
                + " error=" + ex.Message);
        }
    }

    internal static void FinalizePublishedDocumentAfterAnalysis(
        WorldDiplomacyDocument document,
        string authorKingdomId,
        string targetKingdomId,
        string normalizedIntent,
        bool recordNoActionDecision,
        IReadOnlyList<WorldDiplomacyThreat> threats,
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
            WorldDiplomacyThreatApplication.SettleDiplomaticThreatFollowThroughAfterDeclaration(
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
            WorldDiplomacyThreatHistoryApplication.FinalizeDiplomaticThreatHistoryAfterDocument(document,
            threats, appendThreatHistory,
            appendThreatDomesticPenaltyHistory,
            appendThreatIssuerRewardHistory);
            WorldDiplomacyThreatHistoryApplication.FinalizeDiplomaticThreatNonComplianceHistoryAfterDocument(document,
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

    internal static void NotifyExternalDiplomacyResolved(
        string action,
        string initiatorId,
        string targetId,
        string reason,
        bool initiatorIsPlayer,
        WorldDiplomacyStorage storage,
        int currentDay,
        Func<string, bool> proposalTakenEffect,
        Action<WorldDiplomacyOfferDomain> clearBilateralCooldowns,
        Func<string, string, string, bool, WorldDiplomacyDocument> createDocument,
        Func<string, string> buildFactBody,
        Func<bool, WorldDiplomacyRound> ensureActiveRound,
        Func<WorldDiplomacyRound, bool> canFactJoinRound,
        Func<WorldDiplomacyRound, string, bool> tryIncludeSettlementTarget,
        Func<string, string> createId,
        Action<WorldDiplomacyDocument> addDocument,
        Action<WorldDiplomacyDocument> startPropagation,
        Action<WorldDiplomacyDocument> appendCanonicalEvents,
        Action<string> scheduleDeferredRetry,
        Action<WorldDiplomacyDocument> handleRoundDocumentProcessed,
        Action<string> log)
    {
        if (string.IsNullOrWhiteSpace(initiatorId) || string.IsNullOrWhiteSpace(targetId)
            || string.Equals(initiatorId, targetId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        string normalizedAction = WorldDiplomacyIntentVocabulary.NormalizeIntent(action);
        if (!WorldDiplomacyIntentVocabulary.IsExternallyResolvedDiplomaticIntent(normalizedAction))
        {
            log?.Invoke("external resolved diplomacy ignored because action is not an executed result action="
                + (normalizedAction ?? "") + " initiator=" + (initiatorId ?? "")
                + " target=" + (targetId ?? ""));
            return;
        }
        if (string.Equals(normalizedAction, "accept_trade", StringComparison.OrdinalIgnoreCase))
        {
            if (proposalTakenEffect?.Invoke("propose_trade") != true)
            {
                log?.Invoke("external trade acceptance ignored because the live trade agreement was not created initiator="
                    + initiatorId + " target=" + targetId);
                return;
            }
            foreach (var live in WorldDiplomacyLiveRoundRules.Live(storage)) MarkOpenBilateralOffersAccepted(live, initiatorId, targetId, WorldDiplomacyOfferDomain.Trade);
            clearBilateralCooldowns?.Invoke(WorldDiplomacyOfferDomain.Trade);
        }
        else if (string.Equals(normalizedAction, "accept_alliance", StringComparison.OrdinalIgnoreCase))
        {
            if (proposalTakenEffect?.Invoke("propose_alliance") != true)
            {
                log?.Invoke("external alliance acceptance ignored because the live alliance was not created initiator="
                    + initiatorId + " target=" + targetId);
                return;
            }
            foreach (var live in WorldDiplomacyLiveRoundRules.Live(storage)) MarkOpenBilateralOffersAccepted(live, initiatorId, targetId, WorldDiplomacyOfferDomain.Alliance);
            clearBilateralCooldowns?.Invoke(WorldDiplomacyOfferDomain.Alliance);
        }
        WorldDiplomacyDocument fact = createDocument?.Invoke(
            "口头外交结果",
            buildFactBody?.Invoke(normalizedAction) ?? "",
            "oral_diplomacy",
            initiatorIsPlayer);
        if (fact == null) return;
        fact.Intent = normalizedAction;
        fact.Commitment = "binding";
        fact.AnalysisStatus = "external_fact";
        fact.MechanicalResult = string.IsNullOrWhiteSpace(fact.Body) ? "已由口头外交执行" : fact.Body;
        fact.ChangedDiplomaticState = true;
        fact.HistoryDeclarationRecorded = true;
        var candidates = WorldDiplomacyLiveRoundRules.Live(storage).Where(x => canFactJoinRound?.Invoke(x) == true).Take(2).ToList();
        WorldDiplomacyRound activeRound = candidates.Count == 1 ? candidates[0] : null;
        WorldDiplomacyRound round = activeRound ?? (WorldDiplomacyLiveRoundRules.Live(storage).Any()
            ? null : ensureActiveRound?.Invoke(initiatorIsPlayer));
        bool appendedExternalSettlementTarget = round?.ResultSettlementPending == true
            && !WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, targetId);
        if (appendedExternalSettlementTarget
            && tryIncludeSettlementTarget?.Invoke(round, targetId) != true) round = null;
        else if (appendedExternalSettlementTarget)
        {
            WorldDiplomacyResultSlotApplication.AddOrMergeResultSettlementSlot(round, targetId, "route",
                fact.DocumentId, initiatorId, prioritize: false, tryIncludeSettlementTarget, createId);
        }
        fact.RoundId = round?.RoundId ?? "";
        fact.ExchangeId = fact.RoundId;
        fact.AddressedKingdomIds = new List<string> { targetId };
        // This document records a diplomacy action that has already resolved elsewhere; it must not start a reply chain.
        fact.RequiresResponse = false;
        addDocument?.Invoke(fact);
        if (normalizedAction == "declare_war")
        {
            WorldDiplomacyWarPressureRules.ClearWarPressure(storage?.WarPressure, initiatorId, targetId, currentDay);
        }
        try
        {
            startPropagation?.Invoke(fact);
        }
        catch (Exception ex)
        {
            log?.Invoke("external diplomacy propagation failed document=" + fact.DocumentId + " error=" + ex.Message);
        }
        try
        {
            appendCanonicalEvents?.Invoke(fact);
        }
        catch (Exception ex)
        {
            scheduleDeferredRetry?.Invoke(fact.DocumentId);
            log?.Invoke("external diplomacy canonical history append deferred document=" + fact.DocumentId + " error=" + ex.Message);
        }
        if (round == null)
        {
            fact.RoundProgressHandled = true;
            log?.Invoke("external diplomacy fact kept outside unrelated active round document=" + fact.DocumentId
                + " activeRound=" + (activeRound?.RoundId ?? ""));
            return;
        }
        try
        {
            handleRoundDocumentProcessed?.Invoke(fact);
        }
        catch (Exception ex)
        {
            log?.Invoke("external diplomacy round progress deferred document=" + fact.DocumentId + " error=" + ex.Message);
        }
    }
}
