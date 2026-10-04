using AnimusForge.DiplomacyDialogue;

int checks = 0;
void Check(bool passed, string name) { if (!passed) throw new Exception(name); checks++; }
var authority = new DialogueAuthoritySnapshot("B", "king-B", true, false, true);
var terms = new DialogueDiplomaticTerms(tributePayerKingdomId: "B", tributeReceiverKingdomId: "A", dailyTribute: 50, durationDays: 20);
DialogueCommitmentRequest Request(DialogueDiplomaticMove move = DialogueDiplomaticMove.AcceptProposal,
    DialogueProposalReference source = null, DialogueDiplomaticTerms customTerms = null,
    DialogueDiplomaticAction action = DialogueDiplomaticAction.Peace) =>
    new("arrangement", 1, "courier-delivery-1", "king-B", "B", "A", action, move, customTerms ?? terms, source);
DialogueProposalSnapshot Offer(string round, string document, string actionId = "peace", bool open = true,
    bool known = true, bool executable = true, DialogueDiplomaticAction action = DialogueDiplomaticAction.Peace,
    DialogueDiplomaticTerms customTerms = null) =>
    new(new(round, document, actionId), "A", "B", action, customTerms ?? terms, open, known, executable);
DialogueRouteDecision Route(DialogueCommitmentRequest request, params DialogueProposalSnapshot[] offers) =>
    DialogueRoutingRules.Resolve(request, authority, new(offers, true));

var peaceful = Offer("peace-round", "A-paper");
var trade = Offer("trade-round", "trade-paper", action: DialogueDiplomaticAction.Trade);
var exact = Route(Request(), peaceful, trade);
Check(exact.Route == DialogueRoute.OriginalProposalRound && exact.Proposal.Source.RoundId == "peace-round",
    "mediator has no role in the treaty; B responds to A's peace round");
Check(Route(Request(), peaceful, Offer("other-peace-round", "other-paper")).Route == DialogueRoute.Clarification,
    "same pair and topic cannot select latest or earliest");
Check(Route(Request(source: new("peace-round", "A-paper", "peace")), peaceful,
    Offer("other-peace-round", "other-paper")).Proposal.Source.DocumentId == "A-paper", "exact source resolves ambiguity");
Check(Route(Request(source: new("trade-round", "A-paper", "peace")), peaceful).Route == DialogueRoute.Rejected,
    "forged round identity rejected");
Check(Route(Request(source: new("peace-round", "A-paper", "wrong-action")), peaceful).Route == DialogueRoute.Rejected,
    "source action required for multi-action paper");
Check(Route(Request(), Offer("r", "d", known: false)).Route == DialogueRoute.Rejected,
    "player hearsay cannot replace ruler knowledge");
Check(Route(Request(), Offer("r", "d", open: false)).Route == DialogueRoute.Rejected, "closed offer cannot become new event");
Check(Route(Request(), Offer("r", "d", executable: false)).Route == DialogueRoute.Rejected, "stale world state cannot execute");
Check(DialogueRoutingRules.Resolve(Request(), authority, new(new[] { peaceful }, false)).Route == DialogueRoute.Clarification,
    "truncation cannot prove uniqueness");
Check(Route(Request(), peaceful, peaceful).Route == DialogueRoute.OriginalProposalRound, "duplicate propagation does not create ambiguity");
Check(Route(Request(), peaceful, Offer("conflicting-round", "A-paper")).Reason == "conflicting_proposal_snapshots",
    "same source with contradictory round rejected");
Check(Route(Request(customTerms: new()), peaceful).Reason == "acceptance_cannot_change_original_terms", "acceptance preserves tribute");
Check(Route(Request(move: DialogueDiplomaticMove.RejectProposal, customTerms: new()), peaceful).Route == DialogueRoute.OriginalProposalRound,
    "rejection is bound to source and cannot execute modified terms");
Check(Route(Request(move: DialogueDiplomaticMove.NewMatter)).Route == DialogueRoute.NewRound, "new matter always needs independent round");
Check(Route(Request(move: DialogueDiplomaticMove.NewMatter, source: peaceful.Source)).Route == DialogueRoute.Rejected,
    "new proposal cannot rewrite original");
Check(DialogueRoutingRules.Resolve(Request(), new("B", "king-B", true, true, true), new(new[] { peaceful }, true)).Route == DialogueRoute.Rejected,
    "player publication cannot be automated");
Check(DialogueRoutingRules.Resolve(Request(), new("B", "successor", true, false, true), new(new[] { peaceful }, true)).Route == DialogueRoute.Rejected,
    "successor does not acquire private promise");
Check(DialogueRoutingRules.Resolve(Request(), authority, null, isDuplicate: true).Route == DialogueRoute.Duplicate, "duplicate does not create work");
Check(Route(Request(action: DialogueDiplomaticAction.DeclareWar), peaceful).Route == DialogueRoute.Rejected, "unilateral war has no fake acceptance");
foreach (var action in new[] { DialogueDiplomaticAction.Annexation, DialogueDiplomaticAction.Tributary,
    DialogueDiplomaticAction.Garrison, DialogueDiplomaticAction.Vassal })
{
    Check(Route(Request(DialogueDiplomaticMove.NewMatter, customTerms: new("A", "B"), action: action)).Route == DialogueRoute.NewRound,
        "voluntary submission has explicit correct roles " + action);
    Check(Route(Request(DialogueDiplomaticMove.NewMatter, customTerms: new("A", "third-party"), action: action)).Route == DialogueRoute.Rejected,
        "cannot commit a third nation's sovereignty " + action);
}

var progress = new DialogueArrangementProgress("arrangement", 1, "king-B");
Check(!progress.ApplySubmission(new(true, "arrangement", 2, "r")), "late revision submission rejected");
Check(progress.ApplySubmission(new(true, "arrangement", 1, "r")), "actual round receipt advances submitted");
Check(progress.ApplySubmission(new(true, "arrangement", 1, "r")), "repeat receipt harmless");
Check(!progress.ApplySubmission(new(true, "arrangement", 1, "another-r")), "duplicate cannot change bound round");
Check(!progress.ApplyConfirmedEffect(1, "paper", true), "submission is not treaty execution");
Check(!progress.ApplyPublication(2, "r", "paper", "king-B", true), "stale draft cannot publish");
Check(progress.ApplyPublication(1, "r", "paper", "king-B", true), "publication receipt recorded");
Check(progress.Status == DialogueArrangementStatus.Published, "publication is not execution");
Check(progress.RevalidateUnpublishedRuler("successor", true), "published national paper remains public history");
Check(!progress.ApplyConfirmedEffect(1, "paper", false), "unconfirmed effect cannot succeed");
Check(progress.ApplyConfirmedEffect(1, "paper", true) && progress.ApplyConfirmedEffect(1, "paper", true), "confirmed effect idempotent");
var unpublished = new DialogueArrangementProgress("other", 1, "old-king");
Check(!unpublished.RevalidateUnpublishedRuler("new-king", true)
    && unpublished.Status == DialogueArrangementStatus.Cancelled, "unpublished promise ends on succession");
Check(!unpublished.ApplySubmission(new(true, "other", 1, "r")), "late work cannot resurrect cancelled promise");
var port = new ReplayRoundPort(new(new[] { peaceful, trade }, true));
var newRequest = Request(DialogueDiplomaticMove.NewMatter);
Check(DialogueSubmission.Submit(newRequest, authority, null).Decision.Reason == "independent_round_port_unavailable",
    "missing engine is explicit, with no single-round fallback");
Check(DialogueSubmission.Submit(newRequest, authority, port).Submitted
    && DialogueSubmission.Submit(newRequest, authority, port).Submitted && port.EventsCreated == 1,
    "port replays same arrangement without duplicate event creation");
Check(DialogueSubmission.Submit(Request(), authority, port).Receipt.RoundId == "peace-round" && port.ResponsesSubmitted == 1,
    "submission uses original offer's round while unrelated events coexist");
var ambiguousPort = new ReplayRoundPort(new(new[] { peaceful, Offer("r2", "d2") }, true));
Check(DialogueSubmission.Submit(Request(), authority, ambiguousPort).Decision.Route == DialogueRoute.Clarification
    && ambiguousPort.ResponsesSubmitted == 0 && ambiguousPort.EventsCreated == 0,
    "ambiguity never escapes by creating a new event");
Check(!DialogueSubmission.Submit(newRequest, new("B", "king-B", true, true, true), port).Submitted
    && port.EventsCreated == 1, "no player-authored job enters engine");
Check(DialogueTagPayload.TryParse("action=Peace;move=NewMatter;target=A;payer=B;receiver=A;tribute=500;days=21", out var parsed)
    && parsed.Terms.DailyTribute == 500 && parsed.Terms.DurationDays == 21, "agreed terms parsed without repricing");
Check(!DialogueTagPayload.TryParse("action=Peace;move=NewMatter;target=A;tribute=-5", out _), "negative tribute is rejected");
Check(!DialogueTagPayload.TryParse("action=Peace;move=NewMatter;target=A;tribute=auto", out _), "unknown tribute cannot replace agreed amount");
Check(!DialogueTagPayload.TryParse("action=999;move=NewMatter;target=A", out _), "unknown numeric action cannot default to peace");
Check(!DialogueTagPayload.TryParse("action=Peace;move=NewMatter;target=A;target=C", out _), "duplicate participant fields rejected");
Check(!DialogueTagPayload.TryParse("action=Peace;move=NewMatter;target=A;source_document=old", out _), "new event cannot masquerade as original response");
Check(!DialogueTagPayload.TryParse("action=Peace;move=NewMatter;target=A;days=253", out _), "unsupported duration cannot be clamped silently");
Check(DialogueTagPayload.TryParse("action=Peace;move=AcceptProposal;target=A;source_document=paper;source_action=peace", out parsed)
    && parsed.SourceDocumentId == "paper" && parsed.SourceActionId == "peace", "shared channel tag retains exact action source");
Check(!DialogueTermsValidation.Validate(DialogueDiplomaticAction.Peace,
    new(tributePayerKingdomId: "third", tributeReceiverKingdomId: "A", dailyTribute: 1), "A", "B", out _), "cannot promise third nation's tribute");
Check(!DialogueTermsValidation.Validate(DialogueDiplomaticAction.Peace,
    new(cessionFromKingdomId: "C", cessionToKingdomId: "A", cessionSettlementId: "town"), "A", "B", out _), "cannot cede a third nation's town");
Check(!DialogueTermsValidation.Validate(DialogueDiplomaticAction.Alliance, new(durationDays: 10), "A", "B", out _), "permanent alliance cannot promise ignored duration");
Check(DialogueTermsValidation.Validate(DialogueDiplomaticAction.Trade, new(durationDays: 21), "A", "B", out _), "trade supports exact duration");
Check(!DialogueTermsValidation.Validate(DialogueDiplomaticAction.Peace, new(tributePayerKingdomId: "A", tributeReceiverKingdomId: "B", dailyTribute: 50), "A", "B", out _), "tribute period cannot silently fall back to engine default");
Check(!DialogueTermsValidation.Validate(DialogueDiplomaticAction.Peace, new(durationDays: 21), "A", "B", out _), "tribute-free peace cannot promise an unsupported duration");
foreach (var suffix in new[] { "annexation", "tributary", "garrison", "vassal" })
{
    var kind = AnimusForge.WorldDiplomacyResultSettlementRules.EvaluateConfirmedResult(new("accept_" + suffix, true, true, "accepted", ""));
    Check(kind == AnimusForge.WorldDiplomacyConfirmedResultKind.OfferAccepted, "new treaty acceptance has confirmed result " + suffix);
    kind = AnimusForge.WorldDiplomacyResultSettlementRules.EvaluateConfirmedResult(new("accept_" + suffix, false, true, "open", ""));
    Check(kind == AnimusForge.WorldDiplomacyConfirmedResultKind.None, "publication alone does not execute " + suffix);
    kind = AnimusForge.WorldDiplomacyResultSettlementRules.EvaluateConfirmedResult(new("reject_" + suffix, false, true, "rejected", ""));
    Check(kind == AnimusForge.WorldDiplomacyConfirmedResultKind.OfferRejected, "formal rejection closes source proposal " + suffix);
}
Check(DialogueTagPayload.TryParse("action=Peace;move=NewMatter;target=A;supersedes=promise;version=3;reason=明确改约", out var revised)
    && revised.SupersedesArrangementId == "promise" && revised.SupersedesVersion == 3, "revision carries an exact private source/version");
foreach (string suffix in new[] { "supersedes=promise", "version=3", "supersedes=promise;version=0;reason=x",
    "supersedes=promise;version=2147483647;reason=x", "supersedes=promise;version=3;reason=" })
    Check(!DialogueTagPayload.TryParse("action=Peace;move=NewMatter;target=A;" + suffix, out _), "incomplete revision rejected " + suffix);
Check(!DialogueTagPayload.TryParse("action=Peace;move=AcceptProposal;target=A;supersedes=promise;version=3;reason=x", out _),
    "revision cannot masquerade as accepting original public proposal");
foreach (string channel in new[] { "courier", "native", "scene" })
{
    var provenance = new DialogueInteractionOrigin(channel, "actual-turn", "actual-session", new string('p', 900), new string('n', 900));
    Check(provenance.Channel == channel && provenance.InteractionId == "actual-turn" && provenance.SessionId == "actual-session"
        && provenance.PlayerText.Length == 600 && provenance.NpcText.Length == 600, "private provenance bounded " + channel);
}
Check(new DialogueInteractionOrigin("native").InteractionId != new DialogueInteractionOrigin("native").InteractionId,
    "different legacy turns do not reuse a content fingerprint as interaction identity");
Console.WriteLine("PASS " + checks + " diplomacy routing, terms, tags and result checks (contract replay)");

sealed class ReplayRoundPort : IDiplomacyDialogueRoundPort
{
    readonly DialogueProposalBatch proposals;
    readonly Dictionary<string, DialogueWorkReceipt> events = new();
    public int EventsCreated { get; private set; }
    public int ResponsesSubmitted { get; private set; }
    public ReplayRoundPort(DialogueProposalBatch proposals) { this.proposals = proposals; }
    public DialogueProposalBatch QueryAnswerableProposals(string rulerId, string responderKingdomId, string proposerKingdomId,
        DialogueDiplomaticAction action, DialogueProposalReference exactSource = null) => proposals;
    public DialogueWorkReceipt StartNewRoundFromCommitment(DialogueCommitmentRequest request)
    {
        string key = request.ArrangementId + ":" + request.Version;
        if (!events.TryGetValue(key, out var receipt))
        {
            EventsCreated++;
            receipt = new(true, request.ArrangementId, request.Version, "new-independent-round-" + EventsCreated);
            events.Add(key, receipt);
        }
        return receipt;
    }
    public DialogueWorkReceipt SubmitFormalResponseToProposal(DialogueCommitmentRequest request, DialogueProposalReference source)
    {
        ResponsesSubmitted++;
        return new(true, request.ArrangementId, request.Version, source.RoundId);
    }
    public void CancelUnpublishedCommitmentWork(string arrangementId, int version, string reason) { }
}
