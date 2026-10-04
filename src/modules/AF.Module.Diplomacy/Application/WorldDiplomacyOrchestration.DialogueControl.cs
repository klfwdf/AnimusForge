using System;
using System.Linq;

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
    internal string ControlOralDiplomaticCommitment(string rulerId, string actorId, string payload)
    {
        var owner = this;
        if (owner == null || !_host.WorldDiplomacyEnabled() || !DialogueRulerIsCurrent(actorId, rulerId) || _host.IsPlayerParty(actorId)) return "";
        if (string.IsNullOrEmpty(payload) || payload.Length > 1800) return "约定处置未提交：格式无效。";
        var parts = payload.Split(';').Select(x => x.Split(new[] { '=' }, 2)).ToList();
        if (parts.Any(x => x.Length != 2 || (x[0] != "arrangement" && x[0] != "state" && x[0] != "reason"))
            || parts.GroupBy(x => x[0]).Any(x => x.Count() > 1)) return "约定处置未提交：格式无效。";
        string Field(string key) => parts.FirstOrDefault(x => x[0] == key)?[1]?.Trim() ?? "";
        string id = Field("arrangement"), state = Field("state"), reason = Limit(Field("reason"), 180);
        var item = owner.Storage.DialogueArrangements?.FirstOrDefault(x => x.ArrangementId == id && x.RulerId == rulerId
            && x.ActorKingdomId == actorId);
        if (item == null || string.IsNullOrWhiteSpace(reason)) return "约定处置未提交：来源或原因不明确。";
        owner.RecoverDialoguePublicationReceipt(item);
        if (item.LastControlState == state && item.LastControlReason == reason)
            return "外交约定此前已更新：" + item.Status + "。";
        if (item.Status == "published" && state == "cancelled")
        {
            var source = owner.ResolveDocument(item.DocumentId);
            var round = owner.ResolveRound(item.RoundId);
            var offer = round?.PendingOffers?.FirstOrDefault(x => x.SourceDocumentId == item.DocumentId && x.Status == "open");
            if (source == null || offer == null) return "原案已生效、关闭或不可撤回，不能撤销已经发生的外交行动。";
            var document = owner.CreateDocument(actorId, ResolveDialogueParty(item.TargetKingdomId), "撤回外交提案",
                source.AuthorKingdomName + "王庭正式宣布撤回此前向" + source.TargetKingdomName + "提出的" + IntentLabel(source.Intent)
                + "。对方尚未正式接受，原提案自此不再开放。", "dialogue_commitment", false, true, round.RoundId);
            document.Intent = "withdraw_offer"; document.Commitment = "binding";
            document.RespondingToOfferDocumentId = source.DocumentId; document.RespondingToOfferActionId = offer.SourceActionId;
            document.SourceDocumentId = source.DocumentId; document.AnalysisStatus = "success";
            owner.AddDocument(document);
            owner.ProcessAnalyzedDocument(document, document.Intent, document.Commitment, false, "firm", 1f);
            if (offer.Status != "withdrawn") return "撤回宣言未完成，原案仍以当前实际状态为准。";
            item.Status = "withdrawn"; item.Reason = reason;
        }
        else if (item.Status == "accepted" || item.Status == "deferred")
        {
            if (state == "cancelled") owner.CancelUnpublishedCommitmentWork(id, item.Version, reason);
            else if (state == "deferred") { item.Status = "deferred"; item.Reason = reason; item.ExplicitlyDeferred = true; }
            else if (state == "accepted") { item.Status = "accepted"; item.Reason = reason; item.ExplicitlyDeferred = false; owner.PublishDialogueArrangement(item); }
            else return "约定处置未提交：状态无效。";
        }
        else return "该约定已有正式结果，不能通过私人谈话回滚。";
        owner.RecordDialogueArrangementFact(item, AdvanceDialogueControlReceipt(item, state, reason),
            "此前外交约定目前为" + item.Status + "，原因：" + reason);
        return "外交约定已更新：" + item.Status + "。";
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
            document.MechanicalResult = "撤回未执行：撤回提案必须单独声明，不能混入其他外交行动。";
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
            document.MechanicalResult = "撤回未执行：原提案已关闭或当前发文者没有权限。";
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
        document.MechanicalResult = "原外交提案已通过正式宣言撤回；已生效的外交行动保持其实际结果。";
        FinalizePublishedDocumentAfterAnalysis(document, actor, target, "withdraw_offer", true);
        return true;
    }
}
