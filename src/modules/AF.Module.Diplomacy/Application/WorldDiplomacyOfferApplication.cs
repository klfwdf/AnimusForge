using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Owns the canonical relay offer transition. The synchronous host ports resolve
// and revalidate game objects on the campaign thread before any mechanical effect.
internal static class WorldDiplomacyOfferApplication
{
    internal static void Settle(
        WorldDiplomacyRound round,
        WorldDiplomacyDocument document,
        Action<WorldDiplomacyRound> pruneInvalidOffers,
        Func<string, WorldDiplomacyDocument, (bool Blocked, string Reason)> proposalViolation,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Func<WorldDiplomacyRoundOffer, bool> resolveParties,
        Func<string, WorldDiplomacyRoundOffer, WorldDiplomacyDocument, WorldDiplomacyDocument, bool> executeAcceptance,
        Func<string, WorldDiplomacyRoundOffer, bool> hasProposalTakenEffect,
        Action<string> log)
    {
        if (round == null || document == null) return;
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
                return;
            }
            WorldDiplomacyRoundLifecycleRules.RegisterRelayProposalOffer(round, document, intent);
            return;
        }
        string proposalIntent = intent switch
        {
            "accept_peace" or "reject_peace" => "propose_peace",
            "accept_alliance" or "reject_alliance" => "propose_alliance",
            "accept_trade" or "reject_trade" => "propose_trade",
            _ => ""
        };
        if (string.IsNullOrWhiteSpace(proposalIntent)) return;
        if (string.IsNullOrWhiteSpace(document.RespondingToOfferDocumentId))
        {
            document.MechanicalResult = "答复未执行：缺少唯一来源提议";
            return;
        }
        List<WorldDiplomacyRoundOffer> matchingOffers =
            WorldDiplomacyRoundLifecycleRules.SelectMatchingRelayResponseOffers(round, document, proposalIntent);
        if (matchingOffers.Count != 1)
        {
            document.MechanicalResult = "答复未执行：来源提议已关闭、失效或不唯一";
            return;
        }
        WorldDiplomacyRoundOffer resolvedOffer = matchingOffers[0];
        if (intent.StartsWith("reject_", StringComparison.OrdinalIgnoreCase))
        {
            resolvedOffer.Status = "rejected";
            return;
        }
        WorldDiplomacyDocument source = resolveDocument(resolvedOffer.SourceDocumentId);
        if (source == null || !resolveParties(resolvedOffer))
        {
            resolvedOffer.Status = "invalidated";
            document.MechanicalResult = "接受未执行：原提议或当事国已失效";
            return;
        }
        try
        {
            if (!executeAcceptance(proposalIntent, resolvedOffer, source, document))
            {
                resolvedOffer.Status = "invalidated";
                document.MechanicalResult = "接受未执行：和平原案条款已无法原样履行";
                return;
            }
        }
        catch (Exception ex)
        {
            if (hasProposalTakenEffect(proposalIntent, resolvedOffer))
            {
                document.ChangedDiplomaticState = true;
                document.MechanicalResult = WorldDiplomacyOfferContractRules.ProposalSuccessResult(proposalIntent);
                resolvedOffer.Status = "accepted";
            }
            else
            {
                document.MechanicalResult = "接受未执行：" + WorldDiplomacyTextRules.Limit(ex.Message, 180);
                resolvedOffer.Status = "execution_failed";
            }
            log("offer acceptance execution failed document=" + document.DocumentId
                + " offer=" + resolvedOffer.SourceDocumentId + " error=" + ex.Message);
            return;
        }
        resolvedOffer.Status = document.ChangedDiplomaticState
            ? ((document.MechanicalResult ?? "").IndexOf("交割失败", StringComparison.OrdinalIgnoreCase) >= 0
                ? "partially_executed" : "accepted")
            : "execution_failed";
    }
}
