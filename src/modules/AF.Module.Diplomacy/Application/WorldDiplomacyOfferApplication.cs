using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal enum WorldDiplomacyOfferOutcome { Invalidated, Failed, Partial, Applied, Unknown, None }

internal static class WorldDiplomacyOfferActionApplication
{
    internal static WorldDiplomacyOfferOutcome Execute(string intent, WorldDiplomacyRoundOffer offer, WorldDiplomacyDocument source,
        WorldDiplomacyDocument response, IWorldDiplomacyOfferActionPort port, IWorldDiplomacyOrchestration orchestration)
    {
        string proposerId = offer.ProposerKingdomId;
        string targetId = offer.TargetKingdomId;
        WorldDiplomacyOfferActionReceipt receipt;
        if (intent == "propose_peace")
        {
            if (!orchestration.AreOfferedPeaceTermsCurrentlyExecutable(offer, source)) return WorldDiplomacyOfferOutcome.Invalidated;
            response.PeaceTerms = WorldDiplomacyOfferContractRules.ClonePeaceTerms(
                WorldDiplomacyDocumentFactRules.ResolveOfferedPeaceTerms(source, offer.SourceActionId));
            try { receipt = port.ExecutePeace(proposerId, targetId, response.PeaceTerms); }
            catch (Exception ex)
            {
                receipt = port.ReadPeace(proposerId, targetId, response.PeaceTerms);
                port.Log("peace effect exception; confirmed receipt retained: " + ex.Message);
            }
            if (receipt.Applied)
            {
                port.Storage.LastPeaceDayByPair[WorldDiplomacyRoundLifecycleRules.PairKey(proposerId, targetId)] = port.CurrentDay;
                WorldDiplomacyWarPressureRules.ClearWarPressure(port.Storage?.WarPressure, proposerId, targetId, port.CurrentDay);
                WorldDiplomacyWarPressureRules.ClearWarPressure(port.Storage?.WarPressure, targetId, proposerId, port.CurrentDay);
                WorldDiplomacyCessionReceipt cession = port.ApplyCession(proposerId, targetId, response.PeaceTerms);
                receipt = new WorldDiplomacyOfferActionReceipt(true, receipt.Message + cession.Message,
                    receipt.Complete && cession.Complete);
            }
        }
        else if (intent == "propose_alliance") receipt = port.ExecuteAlliance(proposerId, targetId);
        else if (intent == "propose_trade") receipt = port.ExecuteTrade(proposerId, targetId);
        else return WorldDiplomacyOfferOutcome.Failed;
        response.MechanicalResult = receipt.Message;
        if (receipt.Applied) response.ChangedDiplomaticState = true;
        return !receipt.Known ? WorldDiplomacyOfferOutcome.Unknown
            : !receipt.Applied ? WorldDiplomacyOfferOutcome.Failed
            : receipt.Complete ? WorldDiplomacyOfferOutcome.Applied : WorldDiplomacyOfferOutcome.Partial;
    }
}

// Owns the canonical relay offer transition. The synchronous host ports resolve
// and revalidate game objects on the campaign thread before any mechanical effect.
internal static class WorldDiplomacyOfferApplication
{
    internal static WorldDiplomacyOfferOutcome Settle(WorldDiplomacyDocument document, IWorldDiplomacyOfferActionPort port,
        IWorldDiplomacyOrchestration orchestration)
    {
        return Settle(port.ResolveRound(document?.RoundId), document, orchestration.PruneInvalidOffers,
            (intent, doc) =>
            {
                bool blocked = orchestration.TryGetDiplomaticStateViolation(
                    intent, doc?.AuthorKingdomId, doc?.TargetKingdomId, out string reason);
                return (blocked, reason);
            },
            port.ResolveDocument, port.ResolveParties,
            (intent, offer, source, response) => WorldDiplomacyOfferActionApplication.Execute(
                intent, offer, source, response, port, orchestration),
            (intent, offer) => port.HasTakenEffect(intent, offer.ProposerKingdomId, offer.TargetKingdomId), port.Log);
    }

    internal static WorldDiplomacyOfferOutcome Settle(
        WorldDiplomacyRound round,
        WorldDiplomacyDocument document,
        Action<WorldDiplomacyRound> pruneInvalidOffers,
        Func<string, WorldDiplomacyDocument, (bool Blocked, string Reason)> proposalViolation,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Func<WorldDiplomacyRoundOffer, bool> resolveParties,
        Func<string, WorldDiplomacyRoundOffer, WorldDiplomacyDocument, WorldDiplomacyDocument, WorldDiplomacyOfferOutcome> executeAcceptance,
        Func<string, WorldDiplomacyRoundOffer, bool> hasProposalTakenEffect,
        Action<string> log)
    {
        if (round == null || document == null) return WorldDiplomacyOfferOutcome.None;
        round.PendingOffers ??= new List<WorldDiplomacyRoundOffer>();
        pruneInvalidOffers(round);
        string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent(document.Intent);
        if (WorldDiplomacyIntentVocabulary.IsProposalIntent(intent)
            && !string.IsNullOrWhiteSpace(document.TargetKingdomId))
        {
            (bool blocked, string blockReason) = proposalViolation(intent, document);
            if (blocked)
            {
                document.MechanicalResult = "提议未登记：" + blockReason;
                return WorldDiplomacyOfferOutcome.Failed;
            }
            WorldDiplomacyRoundLifecycleRules.RegisterRelayProposalOffer(round, document, intent);
            return WorldDiplomacyOfferOutcome.None;
        }
        string proposalIntent = intent switch
        {
            "accept_peace" or "reject_peace" => "propose_peace",
            "accept_alliance" or "reject_alliance" => "propose_alliance",
            "accept_trade" or "reject_trade" => "propose_trade",
            _ => ""
        };
        if (string.IsNullOrWhiteSpace(proposalIntent)) return WorldDiplomacyOfferOutcome.None;
        if (string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId))
        {
            document.MechanicalResult = "答复未执行：缺少唯一来源提议";
            return WorldDiplomacyOfferOutcome.Failed;
        }
        List<WorldDiplomacyRoundOffer> matchingOffers =
            WorldDiplomacyRoundLifecycleRules.SelectMatchingRelayResponseOffers(round, document, proposalIntent);
        if (matchingOffers.Count != 1)
        {
            document.MechanicalResult = "答复未执行：来源提议已关闭、失效或不唯一";
            return WorldDiplomacyOfferOutcome.Failed;
        }
        WorldDiplomacyRoundOffer resolvedOffer = matchingOffers[0];
        if (intent.StartsWith("reject_", StringComparison.OrdinalIgnoreCase))
        {
            resolvedOffer.Status = "rejected";
            return WorldDiplomacyOfferOutcome.None;
        }
        WorldDiplomacyDocument source = resolveDocument(resolvedOffer.SourceDocumentId);
        if (source == null || !resolveParties(resolvedOffer))
        {
            resolvedOffer.Status = "invalidated";
            document.MechanicalResult = "接受未执行：原提议或当事国已失效";
            return WorldDiplomacyOfferOutcome.Failed;
        }
        WorldDiplomacyOfferOutcome outcome;
        try
        {
            outcome = executeAcceptance(proposalIntent, resolvedOffer, source, document);
            if (outcome == WorldDiplomacyOfferOutcome.Invalidated)
            {
                resolvedOffer.Status = "invalidated";
                document.MechanicalResult = "接受未执行：和平原案条款已无法原样履行";
                return WorldDiplomacyOfferOutcome.Invalidated;
            }
        }
        catch (Exception ex)
        {
            if (hasProposalTakenEffect(proposalIntent, resolvedOffer))
            {
                document.ChangedDiplomaticState = true;
                document.MechanicalResult = "部分外交变化已确认；其余执行结果无法确认";
                resolvedOffer.Status = "partially_executed";
            }
            else
            {
                document.MechanicalResult = "接受未执行：" + WorldDiplomacyTextRules.Limit(ex.Message, 180);
                resolvedOffer.Status = "execution_failed";
            }
            log("offer acceptance execution failed document=" + document.DocumentId
                + " offer=" + resolvedOffer.SourceDocumentId + " error=" + ex.Message);
            return WorldDiplomacyOfferOutcome.Unknown;
        }
        resolvedOffer.Status = outcome == WorldDiplomacyOfferOutcome.Applied ? "accepted"
            : outcome == WorldDiplomacyOfferOutcome.Partial ? "partially_executed" : "execution_failed";
        return outcome;
    }
}
