using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimusForge.DiplomacyDialogue;
using Newtonsoft.Json;

using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyIntentVocabulary;
using static AnimusForge.Refactor.Domain.WorldDiplomacyTextRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyStructureRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyEnvelopeJsonRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyDocumentFactRules;

namespace AnimusForge;

// The existing WorldDiplomacy save key remains the sole owner. Queries run only
// for diplomacy requests; deferred publication is bounded to four items per day.
internal sealed partial class WorldDiplomacyOrchestration
{
    internal string SubmitOralDiplomaticCommitment(string rulerId, string actorId, string payload, DialogueInteractionOrigin capturedOrigin)
    {
        var owner = this;
        if (owner == null || !_host.WorldDiplomacyEnabled()) return "外交约定暂未提交：世界外交系统未启用。";
        if (!DialogueTagPayload.TryParse(payload, out var parsed)) return "外交约定暂未提交：条款格式无效。";
        string actor = ResolveDialogueParty(actorId);
        if (!DialogueRulerIsCurrent(actor, rulerId) || _host.IsPlayerParty(actor))
            return "外交约定未成立：对方没有代表国家发文的权限。";
        if (parsed.Move == DialogueDiplomaticMove.Discussion) return "";
        var terms = parsed.Terms;
        if (!DialogueTermsValidation.Validate(parsed.Action, terms, actor, parsed.TargetKingdomId, out string termsError)
            && parsed.Move == DialogueDiplomaticMove.NewMatter) return "外交约定未提交：" + termsError + "。";
        DialogueProposalReference source = string.IsNullOrEmpty(parsed.SourceDocumentId) ? null
            : new DialogueProposalReference("", parsed.SourceDocumentId, parsed.SourceActionId);
        // A response adopts the exact source terms only when the tag omits all
        // clauses. Explicit different clauses are a counter-offer, never acceptance.
        if (parsed.Move == DialogueDiplomaticMove.AcceptProposal || parsed.Move == DialogueDiplomaticMove.RejectProposal)
        {
            var batch = owner.QueryAnswerableProposals(rulerId, actor, parsed.TargetKingdomId, parsed.Action, source);
            var usable = batch.Proposals.Where(x => x.IsOpen && x.IsKnownByRuler && x.IsCurrentlyExecutable).ToList();
            if (usable.Count != 1) return usable.Count > 1 ? "有多份相关提案，请先指明要回应哪一份宣言及动作。" : "原提案目前不可回应，请先确认提案来源及其有效性。";
            source = usable[0].Source;
            if (terms.Equals(new DialogueDiplomaticTerms())) terms = usable[0].Terms;
        }
        string fingerprint = rulerId + "|" + actor + "|" + parsed.TargetKingdomId + "|" + parsed.Action + "|" + parsed.Move
            + "|" + JsonConvert.SerializeObject(WorldDiplomacyDialogueTerms.From(terms)) + "|" + source?.DocumentId + "|" + source?.ActionId;
        if (!string.IsNullOrEmpty(parsed.SupersedesArrangementId)) fingerprint += "|" + parsed.SupersedesArrangementId + "|" + parsed.SupersedesVersion;
        var duplicate = owner.Storage.DialogueArrangements?.FirstOrDefault(x => x.Fingerprint == fingerprint
            && (x.Status == "accepted" || x.Status == "deferred"
                || (x.Status == "published" && owner.IsLiveRound(owner.ResolveRound(x.RoundId)))
                || (x.Status == "executed" && x.CreatedDay == _host.CurrentDay())));
        if (duplicate != null) return "该约定已提交正式外交，宣言编号：" + duplicate.DocumentId + "。";
        string id = _host.NewId("dialogue_arrangement");
        var origin = capturedOrigin ?? new DialogueInteractionOrigin("legacy_domain");
        var request = new DialogueCommitmentRequest(id, parsed.SupersedesVersion > 0 ? parsed.SupersedesVersion + 1 : 1,
            origin.InteractionId, rulerId, actor, parsed.TargetKingdomId,
            parsed.Action, parsed.Move, terms, source, origin, parsed.SupersedesArrangementId, parsed.SupersedesVersion, parsed.RevisionReason);
        var result = DialogueSubmission.Submit(request, owner.DialogueAuthority(request), owner);
        var saved = owner.Storage.DialogueArrangements?.FirstOrDefault(x => x.ArrangementId == id);
        if (saved != null) saved.Fingerprint = fingerprint;
        _host.Log("oral commitment ruler=" + rulerId + " action=" + parsed.Action
            + " move=" + parsed.Move + " route=" + result.Decision.Route + " reason=" + result.Decision.Reason);
        if (!result.Submitted) return "外交约定暂未提交：" + result.Decision.Reason + "。";
        if (saved?.Status == "cancelled") return "约定已登记，但正式发文因局势变化取消：" + saved.Reason + "。";
        return saved?.Status == "executed" ? "正式宣言已发布，外交行动已生效。"
            : saved?.Status == "published" ? "正式宣言已发布；双边条约须由对方正式回应后生效。" : "约定已登记，正式发文暂缓，原因：" + saved?.Reason + "。";
    }

    internal string BuildOralArrangementContext(string rulerId, string actor)
    {
        if (!DialogueRulerIsCurrent(actor, rulerId) || _host.IsPlayerParty(actor)) return "";
        var body = new StringBuilder("【口头外交与正式宣言】\n明确答应才构成发文约定；口头同意先转为你的正式宣言。新事项或修改条款开独立回合；回应原案必须绑定宣言及动作。玩家宣言只能由玩家亲自发布。\n");
        body.AppendLine("你代表的王国ID=" + actor + "；玩家所属王国ID=" + _host.PlayerKingdomId());
        var offers = GetLiveRounds().Where(IsLiveRound).SelectMany(x => x.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
            .Where(x => x != null && x.Status == "open" && x.TargetKingdomId == actor && DialogueDocumentKnown(actor, x.SourceDocumentId)).ToList();
        foreach (var offer in offers.Take(12)) body.AppendLine("原案 document=" + offer.SourceDocumentId + ";action=" + offer.SourceActionId
            + ";proposer=" + offer.ProposerKingdomId + ";intent=" + offer.Intent + ";terms=" + JsonConvert.SerializeObject(WorldDiplomacyDialogueTerms.From(ResolveDialogueTerms(ResolveDocument(offer.SourceDocumentId), offer.SourceActionId))));
        if (offers.Count > 12) body.AppendLine("还有未列出提案；不能推定唯一，必须指明来源。");
        foreach (var item in (Storage.DialogueArrangements ?? new List<WorldDiplomacyDialogueArrangement>()).Where(x => x.RulerId == rulerId).OrderByDescending(x => x.CreatedDay).Take(6))
            body.AppendLine("此前约定ID=" + item.ArrangementId + ";版本=" + item.Version + ";事项=" + item.Action + ";对象=" + item.TargetKingdomId + ";状态=" + item.Status + ";正式宣言=" + item.DocumentId + ";结果=" + item.Reason);
        return body.ToString();
    }
    private IEnumerable<WorldDiplomacyRound> GetLiveRounds()
    {
        if (Storage?.ActiveRound != null) yield return Storage.ActiveRound;
        foreach (var round in Storage?.ConcurrentRounds ?? Enumerable.Empty<WorldDiplomacyRound>())
            if (round != null && !ReferenceEquals(round, Storage.ActiveRound)) yield return round;
    }

    private bool IsLiveRound(WorldDiplomacyRound round) => round != null
        && string.Equals(round.State, "active", StringComparison.OrdinalIgnoreCase)
        && (ReferenceEquals(Storage?.ActiveRound, round) || Storage?.ConcurrentRounds?.Contains(round) == true);

    private WorldDiplomacyRound CreateIndependentDialogueRound(string author, string target, string arrangementId)
    {
        int day = _host.CurrentDay(), duration = _host.RoundTargetDurationDays();
        var round = new WorldDiplomacyRound { SchemaVersion = _host.RelaySchemaVersion(),
            RoundId = _host.NewId("diplomacy_round"), InitiatorKingdomId = author,
            State = "active", StartedDay = day, LastActivityDay = day,
            SoftEndDay = day + duration, HardEndDay = day + _host.RoundHardDurationDays(duration),
            RelayPassDurationDays = _host.CourtMaxDeliveryDays(), DialogueArrangementId = arrangementId,
            RoundTopic = arrangementId == "player_manual_declaration" ? "玩家外交宣言" : "口头外交约定",
            IsPlayerInsertion = arrangementId == "player_manual_declaration",
            EventSourceType = arrangementId == "player_manual_declaration" ? "player" : "dialogue_commitment" };
        Storage.ConcurrentRounds ??= new List<WorldDiplomacyRound>();
        Storage.ConcurrentRounds.Add(round);
        InvalidateDialogueIndex();
        EnsureRoundParticipant(round, author, "active", false);
        EnsureRoundParticipant(round, target, "observer", false);
        return round;
    }

    private static string DialogueIntent(DialogueDiplomaticAction action) => action switch
    {
        DialogueDiplomaticAction.Peace => "propose_peace", DialogueDiplomaticAction.Alliance => "propose_alliance",
        DialogueDiplomaticAction.Trade => "propose_trade", DialogueDiplomaticAction.DeclareWar => "declare_war",
        DialogueDiplomaticAction.BreakAlliance => "break_alliance", DialogueDiplomaticAction.CancelTrade => "cancel_trade",
        DialogueDiplomaticAction.Annexation => "propose_annexation", DialogueDiplomaticAction.Tributary => "propose_tributary",
        DialogueDiplomaticAction.Garrison => "propose_garrison", _ => "propose_vassal"
    };

    private DialogueAuthoritySnapshot DialogueAuthority(DialogueCommitmentRequest request)
    {
        string actor = ResolveDialogueParty(request.ActorKingdomId), target = ResolveDialogueParty(request.TargetKingdomId);
        bool legal = actor != null && target != null && actor != target && !_host.IsEliminatedParty(actor) && !_host.IsEliminatedParty(target)
            && _host.HasIndependentAuthority(target) && !TryGetDiplomaticStateViolation(DialogueIntent(request.Action), actor, target, out _);
        return new DialogueAuthoritySnapshot(actor ?? "", _host.PartyRulerId(actor),
            DialogueRulerIsCurrent(actor, request.RulerId) && _host.HasIndependentAuthority(actor), _host.IsPlayerParty(actor), legal);
    }

    public DialogueProposalBatch QueryAnswerableProposals(string rulerId, string responderKingdomId,
        string proposerKingdomId, DialogueDiplomaticAction action, DialogueProposalReference exactSource = null)
    {
        string responder = ResolveDialogueParty(responderKingdomId);
        if (!DialogueRulerIsCurrent(responder, rulerId)) return new DialogueProposalBatch(null, true);

        var result = new List<DialogueProposalSnapshot>();
        var documents = Storage.Documents.ToDictionary(x => x.DocumentId, StringComparer.OrdinalIgnoreCase);
        foreach (var indexed in FindIndexedDialogueOffers(responderKingdomId, proposerKingdomId, DialogueIntent(action)))
        {
            var round = indexed.Key; var offer = indexed.Value;
            if (offer == null || offer.TargetKingdomId != responderKingdomId || offer.ProposerKingdomId != proposerKingdomId
                || NormalizeIntent(offer.Intent) != DialogueIntent(action)) continue;
            if (exactSource != null && (offer.SourceDocumentId != exactSource.DocumentId
                || (offer.SourceActionId ?? "") != exactSource.ActionId
                || (!string.IsNullOrEmpty(exactSource.RoundId) && round.RoundId != exactSource.RoundId))) continue;
            documents.TryGetValue(offer.SourceDocumentId, out var document);
            var terms = ResolveDialogueTerms(document, offer.SourceActionId);
            bool executable = document?.IsReadyForPublication == true
                && !TryGetDiplomaticStateViolation(offer.Intent, ResolveDialogueParty(proposerKingdomId), responder, out _)
                && (action != DialogueDiplomaticAction.Peace || AreOfferedPeaceTermsCurrentlyExecutable(
                    offer, document))
                && ValidateFormalTreatyTerms(offer.Intent, terms, ResolveDialogueParty(proposerKingdomId), responder, out _);
            result.Add(new DialogueProposalSnapshot(new DialogueProposalReference(round.RoundId, offer.SourceDocumentId,
                offer.SourceActionId), proposerKingdomId, responderKingdomId, action, terms,
                offer.Status == "open", DialogueDocumentKnown(responder, offer.SourceDocumentId), executable));
        }
        return new DialogueProposalBatch(result, true);
    }

    public DialogueWorkReceipt StartNewRoundFromCommitment(DialogueCommitmentRequest request) => SubmitDialogueWork(request, null);
    public DialogueWorkReceipt SubmitFormalResponseToProposal(DialogueCommitmentRequest request, DialogueProposalReference source) => SubmitDialogueWork(request, source);

    private DialogueWorkReceipt SubmitDialogueWork(DialogueCommitmentRequest request, DialogueProposalReference source)
    {
        if (request == null) return new DialogueWorkReceipt(false, "", 0, "", "missing_request");
        Storage.DialogueArrangements ??= new List<WorldDiplomacyDialogueArrangement>();
        var existing = Storage.DialogueArrangements.FirstOrDefault(x => x.ArrangementId == request.ArrangementId);
        if (existing != null)
            return new DialogueWorkReceipt(existing.Version == request.Version && existing.Status != "cancelled" && existing.Status != "superseded"
                && existing.RulerId == request.RulerId && existing.ActorKingdomId == request.ActorKingdomId
                && existing.TargetKingdomId == request.TargetKingdomId && existing.Action == request.Action
                && existing.Move == request.Move && existing.Terms.ToTerms().Equals(request.Terms)
                && existing.SourceDocumentId == (source?.DocumentId ?? "") && existing.SourceActionId == (source?.ActionId ?? "")
                && existing.SupersedesArrangementId == request.SupersedesArrangementId && existing.SupersedesVersion == request.SupersedesVersion,
                request.ArrangementId, request.Version, existing.RoundId, "existing_arrangement");
        var batch = source == null ? null : QueryAnswerableProposals(request.RulerId, request.ActorKingdomId,
            request.TargetKingdomId, request.Action, source);
        var decision = DialogueRoutingRules.Resolve(request, DialogueAuthority(request), batch);
        if ((source == null && decision.Route != DialogueRoute.NewRound)
            || (source != null && decision.Route != DialogueRoute.OriginalProposalRound))
            return new DialogueWorkReceipt(false, request.ArrangementId, request.Version, "", decision.Reason);
        if (!ValidateFormalTreatyTerms(DialogueIntent(request.Action), request.Terms,
            ResolveDialogueParty(request.ActorKingdomId), ResolveDialogueParty(request.TargetKingdomId), out string reason))
            return new DialogueWorkReceipt(false, request.ArrangementId, request.Version, "", reason);
        if (!TryValidateDialogueRevision(request, out var previous, out reason))
            return new DialogueWorkReceipt(false, request.ArrangementId, request.Version, "", reason);
        var round = source == null ? CreateIndependentDialogueRound(ResolveDialogueParty(request.ActorKingdomId),
            ResolveDialogueParty(request.TargetKingdomId), request.ArrangementId) : ResolveRound(source.RoundId);
        if (!IsLiveRound(round)) return new DialogueWorkReceipt(false, request.ArrangementId, request.Version, "", "source_round_closed");
        var arrangement = new WorldDiplomacyDialogueArrangement {
            ArrangementId = request.ArrangementId, Version = request.Version, SourceInteractionId = request.SourceInteractionId,
            SourceChannel = request.Origin?.Channel ?? "legacy_domain", SourceSessionId = request.Origin?.SessionId ?? "",
            SourcePlayerText = request.Origin?.PlayerText ?? "", SourceNpcText = request.Origin?.NpcText ?? "",
            SupersedesArrangementId = request.SupersedesArrangementId, SupersedesVersion = request.SupersedesVersion,
            RulerId = request.RulerId, ActorKingdomId = request.ActorKingdomId, TargetKingdomId = request.TargetKingdomId,
            Action = request.Action, Move = request.Move, Terms = WorldDiplomacyDialogueTerms.From(request.Terms),
            RoundId = round.RoundId, SourceDocumentId = source?.DocumentId ?? "", SourceActionId = source?.ActionId ?? "",
            CreatedDay = _host.CurrentDay(), Status = "accepted" };
        Storage.DialogueArrangements.Add(arrangement);
        if (previous != null) SupersedeDialogueArrangement(previous, arrangement, request.RevisionReason);
        RecordDialogueArrangementFact(arrangement, "consent", "我已明确答应就" + _host.PartyNameOrEmpty(ResolveDialogueParty(request.TargetKingdomId))
            + "的外交事项发出正式宣言；口头约定尚未代表条约生效。");
        PublishDialogueArrangement(arrangement);
        return new DialogueWorkReceipt(true, request.ArrangementId, request.Version, round.RoundId, arrangement.Reason);
    }

    public void CancelUnpublishedCommitmentWork(string arrangementId, int version, string reason)
    {
        var item = Storage.DialogueArrangements?.FirstOrDefault(x => x.ArrangementId == arrangementId && x.Version == version);
        if (!CanInvalidateDialogueArrangement(item)) return;
        item.Status = "cancelled"; item.Reason = reason ?? "cancelled";
        var cancelledDocument = ResolveDocument(item.DocumentId);
        if (cancelledDocument != null)
        {
            // Bind older saves' pre-marker documents too, so a late callback
            // cannot bypass cancellation because optional identity was absent.
            cancelledDocument.DialogueArrangementId = item.ArrangementId;
            cancelledDocument.DialogueArrangementVersion = item.Version;
        }
        var round = ResolveRound(item.RoundId);
        if (IsLiveRound(round) && round.DialogueArrangementId == item.ArrangementId
            && ResolveDocument(round.RootDocumentId)?.IsReadyForPublication != true)
            CloseActiveRound("dialogue_commitment_cancelled", round);
        RecordDialogueArrangementFact(item, "cancel", "此前答应的外交发文已取消，原因：" + item.Reason);
    }

    private void MaintainDialogueArrangements()
    {
        foreach (var item in (Storage.DialogueArrangements ?? Enumerable.Empty<WorldDiplomacyDialogueArrangement>())
            .Where(x => x.Status == "accepted" || x.Status == "deferred")) RecoverDialoguePublicationReceipt(item);
        foreach (var item in (Storage.DialogueArrangements ?? new List<WorldDiplomacyDialogueArrangement>())
            .Where(x => x.Status == "accepted" || x.Status == "deferred").ToList())
            if (!HasCurrentDiplomaticRulerMemorySnapshot(ResolveDialogueParty(item.ActorKingdomId), item.RulerId))
                CancelUnpublishedCommitmentWork(item.ArrangementId, item.Version, "original_ruler_no_longer_authorized");
        foreach (var item in (Storage.DialogueArrangements ?? new List<WorldDiplomacyDialogueArrangement>())
            .Where(x => x.Status == "accepted" || (x.Status == "deferred" && !x.ExplicitlyDeferred)).Take(4).ToList()) PublishDialogueArrangement(item);
        foreach (var item in Storage.DialogueArrangements ?? Enumerable.Empty<WorldDiplomacyDialogueArrangement>())
        {
            if (item.Status != "published") continue;
            var document = ResolveDocument(item.DocumentId);
            var round = ResolveRound(item.RoundId);
            var offer = round?.PendingOffers?.FirstOrDefault(x => x.SourceDocumentId == item.DocumentId);
            if (document?.ChangedDiplomaticState == true || offer?.Status == "accepted") item.Status = "executed";
            else if (offer != null && offer.Status != "open") { item.Status = offer.Status; item.Reason = document?.MechanicalResult ?? offer.Status; }
            else if (!IsLiveRound(round)) { item.Status = "expired"; item.Reason = round?.CloseReason ?? "round_unavailable"; }
        }
        // Terminal receipts outlive the document archive. Only old terminal items
        // are pruned; pending work never disappears because of a display limit.
        Storage.DialogueArrangements?.RemoveAll(x => x.CreatedDay < _host.CurrentDay() - 365
            && x.Status != "accepted" && x.Status != "deferred" && x.Status != "published"
            && !(x.MemoryReceipts?.Any(m => !m.Delivered) ?? false));
    }

    private void PublishDialogueArrangement(WorldDiplomacyDialogueArrangement item)
    {
        if (RecoverDialoguePublicationReceipt(item)) return;
        if (!IsCurrentDialoguePublicationWork(item)) return;
        try { PublishDialogueArrangementCore(item); }
        catch (Exception ex)
        {
            item.Status = ResolveDocument(item.DocumentId)?.IsReadyForPublication == true ? "published" : "deferred";
            item.Reason = Limit(ex.Message, 180);
            if (item.Status == "deferred") RecordDialogueArrangementFact(item, "technical_delay", "此前外交发文暂因技术故障延期，尚未发布。原因：" + item.Reason);
            _host.Log("dialogue publication boundary " + item.Status + " id=" + item.ArrangementId);
        }
    }

    private bool RecoverDialoguePublicationReceipt(WorldDiplomacyDialogueArrangement item)
    {
        if (item == null || (item.Status != "accepted" && item.Status != "deferred")) return false;
        var document = ResolveDocument(item.DocumentId);
        if (document?.IsReadyForPublication != true) return false;
        item.Status = document.ChangedDiplomaticState ? "executed" : "published";
        item.Reason = document.MechanicalResult ?? "publication_receipt_recovered";
        return true;
    }

    private void PublishDialogueArrangementCore(WorldDiplomacyDialogueArrangement item)
    {
        if (!IsCurrentDialoguePublicationWork(item)) return;
        var actor = ResolveDialogueParty(item.ActorKingdomId); var target = ResolveDialogueParty(item.TargetKingdomId);
        if (!HasCurrentDiplomaticRulerMemorySnapshot(actor, item.RulerId))
        { CancelUnpublishedCommitmentWork(item.ArrangementId, item.Version, "original_ruler_no_longer_authorized"); return; }
        var round = ResolveRound(item.RoundId);
        if (!IsLiveRound(round)) { CancelUnpublishedCommitmentWork(item.ArrangementId, item.Version, "round_closed_before_publication"); return; }
        string intent = DialogueIntent(item.Action);
        bool response = item.Move == DialogueDiplomaticMove.AcceptProposal || item.Move == DialogueDiplomaticMove.RejectProposal;
        if (response)
        {
            var batch = QueryAnswerableProposals(item.RulerId, item.ActorKingdomId, item.TargetKingdomId, item.Action,
                new DialogueProposalReference(item.RoundId, item.SourceDocumentId, item.SourceActionId));
            if (batch.Proposals.Count != 1 || !batch.Proposals[0].IsOpen || !batch.Proposals[0].IsKnownByRuler
                || !batch.Proposals[0].IsCurrentlyExecutable)
            { CancelUnpublishedCommitmentWork(item.ArrangementId, item.Version, "original_proposal_no_longer_executable"); return; }
            intent = ProposalIntentToResponseIntent(intent, item.Move == DialogueDiplomaticMove.AcceptProposal);
        }
        if (TryGetDiplomaticStateViolation(intent, actor, target, out string block)
            || !ValidateFormalTreatyTerms(intent, item.Terms.ToTerms(), actor, target, out block))
        { CancelUnpublishedCommitmentWork(item.ArrangementId, item.Version, block); return; }
        // Terms are already consented to. Rendering a formal declaration is
        // deterministic: no second model can silently rewrite the agreement.
        var document = ResolveDocument(item.DocumentId);
        if (document == null)
        {
            document = CreateDocument(actor, target, "外交约定正式宣言", BuildDialogueDeclarationBody(item, intent),
                "dialogue_commitment", false, response, round.RoundId);
            item.DocumentId = document.DocumentId;
            document.DialogueArrangementId = item.ArrangementId; document.DialogueArrangementVersion = item.Version;
            document.Intent = intent; document.Commitment = DefaultCommitmentForIntent(intent);
            document.TreatyTerms = item.Terms; document.PeaceTerms = item.Terms.ToPeaceTerms();
            document.RequiresResponse = IsProposalIntent(intent); document.AnalysisStatus = "success";
            document.RespondingToOfferDocumentId = item.SourceDocumentId;
            document.RespondingToOfferActionId = item.SourceActionId; document.SourceDocumentId = item.SourceDocumentId;
            AddDocument(document); round.RootDocumentId = FirstNonEmpty(round.RootDocumentId, document.DocumentId);
        }
        if (document.IsReadyForPublication) { item.Status = "published"; return; }
        try
        {
            ProcessAnalyzedDocument(document, document.Intent, document.Commitment, document.RequiresResponse, "firm", 1f);
            item.Status = document.IsReadyForPublication ? (document.ChangedDiplomaticState ? "executed" : "published") : "cancelled";
            item.Reason = document.MechanicalResult ?? document.AnalysisStatus;
            if (item.Status == "cancelled") RecordDialogueArrangementFact(item, "publication_failed", "此前外交发文未能完成，原因：" + item.Reason);
            else DeliverDialogueDeclarationToCounterparty(document, target);
        }
        catch (Exception ex)
        {
            // A published artifact is never replayed to retry a memory or
            // propagation write. Those have their existing recoverable owners.
            item.Status = document.IsReadyForPublication ? "published" : "deferred";
            item.Reason = Limit(ex.Message, 180); _host.Log("dialogue publication " + item.Status + " id=" + item.ArrangementId);
            if (item.Status == "deferred") RecordDialogueArrangementFact(item, "technical_delay", "此前外交发文暂因技术故障延期，尚未发布。原因：" + item.Reason);
        }
    }

    // The counterparty negotiated this face to face, so its court knows the declaration at once instead of
    // waiting for distance propagation; otherwise the player's immediate formal acceptance finds no known offer.
    // Runs once per published oral declaration; other courts keep the normal propagation schedule.
    private void DeliverDialogueDeclarationToCounterparty(WorldDiplomacyDocument document, string target)
    {
        if (document?.IsReadyForPublication != true || target == null
            || string.Equals(target, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)) return;
        int day = _host.CurrentDay();
        Storage.PropagationArrivals?.RemoveAll(x => x != null && IsCourtArrival(x)
            && MatchesDocumentId(x.DocumentId, document.DocumentId)
            && string.Equals(_host.ResolvePropagationReceiverId(x.KingdomId, x.SettlementId), target, StringComparison.OrdinalIgnoreCase));
        WorldDiplomacyPropagationApplication.ReceiveCourt(Storage, document, target, day,
            () => _host.IsPlayerAffiliatedParty(target), () => ProcessCourtArrival(target, document));
        InvalidateDialogueIndex();
        _host.Log("dialogue declaration delivered to counterparty document=" + document.DocumentId + " receiver=" + target);
    }

    private string BuildDialogueDeclarationBody(WorldDiplomacyDialogueArrangement item, string intent)
    {
        string actor = _host.PartyNameOrEmpty(ResolveDialogueParty(item.ActorKingdomId)), target = _host.PartyNameOrEmpty(ResolveDialogueParty(item.TargetKingdomId));
        string act = item.Action switch { DialogueDiplomaticAction.Peace => "停战与和平", DialogueDiplomaticAction.Alliance => "缔结同盟",
            DialogueDiplomaticAction.Trade => "缔结贸易协定", DialogueDiplomaticAction.DeclareWar => "宣战",
            DialogueDiplomaticAction.BreakAlliance => "解除同盟", DialogueDiplomaticAction.CancelTrade => "终止贸易协定",
            DialogueDiplomaticAction.Annexation => "王国并入", DialogueDiplomaticAction.Tributary => "朝贡臣属条约",
            DialogueDiplomaticAction.Garrison => "驻军臣属条约", _ => "完全臣属条约" };
        var body = new StringBuilder(actor + "统治者正式向" + target + "宣布：");
        body.Append(intent.StartsWith("accept_") ? "我方接受" : intent.StartsWith("reject_") ? "我方拒绝" : IsProposalIntent(intent) ? "我方提出" : "我方决定");
        body.Append(act + "。");
        if (item.Move != DialogueDiplomaticMove.NewMatter) body.Append("贵国原案所列全部条款，本次答复依该原案作出。");
        body.Append(DescribeDialogueTerms(item.Terms.ToTerms()));
        if (item.Action == DialogueDiplomaticAction.Annexation)
            body.Append("并入国放弃独立王权，全部家族及其原有领地归入接收国，原王国解散。");
        else if (item.Action == DialogueDiplomaticAction.Tributary)
            body.Append("臣属国保留外交和军事自主，按既有朝贡规则纳贡，宗主国承担庇护义务。");
        else if (item.Action == DialogueDiplomaticAction.Garrison)
            body.Append("臣属国接受宗主军事号令并履行共同战争义务，保留条约允许的外交表达与独立度规则。");
        else if (item.Action == DialogueDiplomaticAction.Vassal)
            body.Append("臣属国外交和军事由宗主控制，并按既有完全臣属规则履行贡赋义务。");
        if (IsProposalIntent(intent)) body.Append("以上为正式提案，须由对方通过正式宣言接受后生效。");
        return body.ToString();
    }

    private string DescribeDialogueTerms(DialogueDiplomaticTerms terms)
    {
        var body = new StringBuilder();
        if (!string.IsNullOrEmpty(terms.JoiningKingdomId)) body.Append(_host.PartyNameOrEmpty(ResolveDialogueParty(terms.JoiningKingdomId))
            + "向" + _host.PartyNameOrEmpty(ResolveDialogueParty(terms.ReceivingKingdomId)) + "并入或承认宗主权；");
        if (terms.DailyTribute > 0) body.Append(_host.PartyNameOrEmpty(ResolveDialogueParty(terms.TributePayerKingdomId)) + "向"
            + _host.PartyNameOrEmpty(ResolveDialogueParty(terms.TributeReceiverKingdomId)) + "每日支付" + terms.DailyTribute + "第纳尔，期限" + terms.DurationDays + "日；");
        if (!string.IsNullOrEmpty(terms.CessionSettlementId)) body.Append(_host.PartyNameOrEmpty(ResolveDialogueParty(terms.CessionFromKingdomId))
            + "向" + _host.PartyNameOrEmpty(ResolveDialogueParty(terms.CessionToKingdomId)) + "割让"
            + (_host.SettlementValidationName(terms.CessionSettlementId) ?? terms.CessionSettlementId) + "；");
        if (terms.DurationDays > 0 && terms.DailyTribute == 0) body.Append("期限" + terms.DurationDays + "日；");
        return body.ToString();
    }

    private DialogueDiplomaticTerms ResolveDialogueTerms(WorldDiplomacyDocument document, string actionId)
    {
        var action = document?.Actions?.FirstOrDefault(x => x.ActionId == actionId);
        var treaty = action?.TreatyTerms ?? document?.TreatyTerms;
        if (treaty != null) return treaty.ToTerms();
        var peace = ResolveOfferedPeaceTerms(document, actionId);
        return new DialogueDiplomaticTerms(tributePayerKingdomId: peace?.TributePayerKingdomId,
            tributeReceiverKingdomId: peace?.TributeReceiverKingdomId, dailyTribute: peace?.DailyTribute ?? 0,
            durationDays: peace?.DurationDays ?? 0, cessionFromKingdomId: peace?.CessionFromKingdomId,
            cessionToKingdomId: peace?.CessionToKingdomId, cessionSettlementId: peace?.CessionSettlementId);
    }
}
