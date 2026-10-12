using System;
using System.Linq;
using AnimusForge.DiplomacyDialogue;

using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyIntentVocabulary;
using static AnimusForge.Refactor.Domain.WorldDiplomacyTextRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyStructureRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyEnvelopeJsonRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyDocumentFactRules;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    internal string ControlOralDiplomaticCommitment(string rulerId, string actorId, string payload, DialogueInteractionOrigin capturedOrigin = null)
    {
        var owner = this;
        if (owner == null || !_host.WorldDiplomacyEnabled() || !DialogueRulerIsCurrent(actorId, rulerId) || _host.IsPlayerParty(actorId)) return "";
        if (string.IsNullOrEmpty(payload) || payload.Length > 1800) return DiplomacyMiscPromptTexts.Get("DialogueControl_InvalidFormat", "Commitment disposition not submitted: invalid format.");
        var parts = payload.Split(';').Select(x => x.Split(new[] { '=' }, 2)).ToList();
        if (parts.Any(x => x.Length != 2 || (x[0] != "arrangement" && x[0] != "state" && x[0] != "reason"))
            || parts.GroupBy(x => x[0]).Any(x => x.Count() > 1)) return DiplomacyMiscPromptTexts.Get("DialogueControl_InvalidFormat", "Commitment disposition not submitted: invalid format.");
        string Field(string key) => parts.FirstOrDefault(x => x[0] == key)?[1]?.Trim() ?? "";
        string id = Field("arrangement"), state = Field("state"), reason = Limit(Field("reason"), 180);
        var item = owner.Storage.DialogueArrangements?.FirstOrDefault(x => x.ArrangementId == id && x.RulerId == rulerId
            && x.ActorKingdomId == actorId);
        if (item == null || string.IsNullOrWhiteSpace(reason)) return DiplomacyMiscPromptTexts.Get("DialogueControl_AmbiguousSourceReason", "Commitment disposition not submitted: source or reason unclear.");
        owner.RecoverDialoguePublicationReceipt(item);
        if (item.LastControlState == state && item.LastControlReason == reason)
            return DiplomacyMiscPromptTexts.Get("DialogueControl_AlreadyUpdatedPrefix", "Diplomatic commitment was previously updated: ") + item.Status + "。";
        if (item.Status == "published" && state == "cancelled")
        {
            var source = owner.ResolveDocument(item.DocumentId);
            var round = owner.ResolveRound(item.RoundId);
            var offer = round?.PendingOffers?.FirstOrDefault(x => x.SourceDocumentId == item.DocumentId && x.Status == "open");
            if (source == null || offer == null) return DiplomacyMiscPromptTexts.Get("DialogueControl_CannotWithdraw", "The original proposal has taken effect, is closed, or cannot be withdrawn; completed diplomatic actions cannot be undone.");
            QueueOralWithdrawal(item, source, offer, reason, capturedOrigin);
            item.Reason = reason;
        }
        else if (item.Status == "accepted" || item.Status == "deferred")
        {
            if (state == "cancelled") owner.CancelUnpublishedCommitmentWork(id, item.Version, reason);
            else if (state == "deferred")
            {
                item.Status = "deferred"; item.Reason = reason; item.ExplicitlyDeferred = true;
                foreach (var job in Storage.Jobs.Where(x => x.DialogueArrangementId == item.ArrangementId).ToList()) RemoveJob(job.JobId);
            }
            else if (state == "accepted")
            {
                item.Status = "accepted"; item.Reason = reason; item.ExplicitlyDeferred = false;
                item.GenerationRetryAfterDay = -1; owner.PublishDialogueArrangement(item);
            }
            else return DiplomacyMiscPromptTexts.Get("DialogueControl_InvalidStatus", "Commitment disposition not submitted: invalid status.");
        }
        else return DiplomacyMiscPromptTexts.Get("DialogueControl_HasFormalResult", "This commitment already has a formal outcome and cannot be rolled back through private discussion.");
        owner.RecordDialogueArrangementFact(item, AdvanceDialogueControlReceipt(item, state, reason),
            DiplomacyCompatibilityTexts.Get("DialogueControl_HistoricalCommitmentPrefix") + item.Status + DiplomacyCompatibilityTexts.Get("DialogueControl_ReasonPrefix") + reason);
        return DiplomacyMiscPromptTexts.Get("DialogueControl_UpdatedPrefix", "Diplomatic commitment updated: ") + item.Status + "。";
    }

    private static string AdvanceDialogueControlReceipt(WorldDiplomacyDialogueArrangement item, string state, string reason)
    {
        if (item.LastControlState == state && item.LastControlReason == reason) return "";
        item.LastControlState = state; item.LastControlReason = reason;
        return "control:" + ++item.ControlSequence;
    }

    private bool TryProcessOfferWithdrawal(WorldDiplomacyDocument document)
    {
        if (document?.Actions?.Count > 1 && document.Actions.Any(x => NormalizeIntent(x?.Intent) == "withdraw_offer"))
        {
            document.MechanicalResult = DiplomacyMiscPromptTexts.Get("DialogueControl_WithdrawStandalone", "Withdrawal not executed: proposal withdrawal must be declared separately and cannot be mixed with other diplomatic actions.");
            if (!document.IsPlayerAuthored) SuppressInvalidDocumentBeforePropagation(document, "withdrawal_must_be_standalone");
            else FinalizePublishedDocumentAfterAnalysis(document, ResolveDialogueParty(document.AuthorKingdomId),
                ResolveDialogueParty(document.TargetKingdomId), "none", true);
            return true;
        }
        if (NormalizeIntent(document?.Intent) != "withdraw_offer") return false;
        var actor = ResolveDialogueParty(document.AuthorKingdomId); var target = ResolveDialogueParty(document.TargetKingdomId);
        var source = ResolveDocument(document.RespondingToOfferDocumentId);
        var round = ResolveRound(source?.RoundId);
        var matches = round?.PendingOffers?.Where(x => x != null && x.Status == "open"
            && x.SourceDocumentId == document.RespondingToOfferDocumentId && x.SourceActionId == document.RespondingToOfferActionId
            && x.ProposerKingdomId == document.AuthorKingdomId && x.TargetKingdomId == document.TargetKingdomId).Take(2).ToList();
        bool authority = actor != null && !_host.IsEliminatedParty(actor) && _host.HasIndependentAuthority(actor)
            && (document.IsPlayerAuthored ? _host.IsPlayerParty(actor) : DialogueRulerIsCurrent(actor, document.AuthorRulerId));
        if (!authority || target == null || !IsLiveRound(round) || source?.IsReadyForPublication != true || matches?.Count != 1
            || !DialogueDocumentKnown(actor, source.DocumentId))
        {
            document.MechanicalResult = DiplomacyMiscPromptTexts.Get("DialogueControl_WithdrawUnauthorized", "Withdrawal not executed: original proposal is closed or publisher lacks authority.");
            if (!document.IsPlayerAuthored) SuppressInvalidDocumentBeforePropagation(document, "invalid_offer_withdrawal");
            else FinalizePublishedDocumentAfterAnalysis(document, actor, target, "none", true);
            return true;
        }
        var provisional = ResolveRound(document.RoundId);
        document.RoundId = round.RoundId; document.ExchangeId = round.RoundId;
        if (provisional != round && IsLiveRound(provisional) && provisional.DialogueArrangementId == "player_manual_declaration")
            CloseActiveRound("manual_withdrawal_bound_to_original_proposal", provisional);
        document.IsReadyForPublication = true;
        matches[0].Status = "withdrawn";
        InvalidateDialogueIndex();
        document.MechanicalResult = DiplomacyMiscPromptTexts.Get("DialogueControl_WithdrawSuccess", "Original diplomatic proposal withdrawn via formal declaration; effective diplomatic actions retain actual results.");
        FinalizePublishedDocumentAfterAnalysis(document, actor, target, "withdraw_offer", true);
        return true;
    }
}
