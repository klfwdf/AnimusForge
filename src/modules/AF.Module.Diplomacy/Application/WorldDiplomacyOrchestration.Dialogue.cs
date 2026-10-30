using AnimusForge;
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
        => SubmitOralDiplomaticCommitment(rulerId, actorId, payload, capturedOrigin, out _);

    internal string SubmitOralDiplomaticCommitment(string rulerId, string actorId, string payload, DialogueInteractionOrigin capturedOrigin, out bool registered)
        => SubmitReviewedOralCommitment(rulerId, actorId, payload, capturedOrigin, out registered, "");

    private DialogueCommitmentRequest _reviewedOralEventRequest;

    private string SubmitReviewedOralCommitment(string rulerId, string actorId, string payload, DialogueInteractionOrigin capturedOrigin,
        out bool registered, string reviewedEventId)
    {
        registered = false;
        var owner = this;
        if (owner == null || !_host.WorldDiplomacyEnabled()) return DiplomacyMiscPromptTexts.Get("OralDiplomacy_Uncommitted", "Diplomatic commitment not submitted: world diplomacy system is not enabled.");
        if (!DialogueTagPayload.TryParse(payload, out var parsed)) return DiplomacyMiscPromptTexts.Get("OralDiplomacy_InvalidFormat", "Diplomatic commitment not submitted: clause format is invalid.");
        string actor = ResolveDialogueParty(actorId);
        if (!DialogueRulerIsCurrent(actor, rulerId) || _host.IsPlayerParty(actor))
            return DiplomacyMiscPromptTexts.Get("OralDiplomacy_Unauthorized", "Diplomatic commitment not established: the other party does not have authority to represent the kingdom for official documentation.");
        if (parsed.Move == DialogueDiplomaticMove.Discussion) return "";
        if (DialogueRoutingRules.UsesLegacyOralVassalage(parsed, actor, CurrentPlayerAuthority()))
        {
            _host.Log("oral commitment routed to original vassalage entry action=" + parsed.Action);
            return DiplomacyMiscPromptTexts.Get("OralVassalage_UseOriginalEntry",
                "This oral submission uses the original vassalage signing entry; no formal publication commitment was created.");
        }
        var terms = parsed.Terms;
        if (!DialogueTermsValidation.Validate(parsed.Action, terms, actor, parsed.TargetKingdomId, out string termsError)
            && parsed.Move == DialogueDiplomaticMove.NewMatter)
        {
            _host.Log("oral commitment validation failed action=" + parsed.Action + " reason=" + termsError);
            return DiplomacyMiscPromptTexts.Get("OralDiplomacy_SubmitFailedPrefix", "Diplomatic commitment not submitted: ") + DialoguePeaceClarificationRules.DescribeValidationFailure(termsError, terms) + "。";
        }
        DialogueProposalReference source = string.IsNullOrEmpty(parsed.SourceDocumentId) ? null
            : new DialogueProposalReference("", parsed.SourceDocumentId, parsed.SourceActionId);
        // A response adopts the exact source terms only when the tag omits all
        // clauses. Explicit different clauses are a counter-offer, never acceptance.
        if (parsed.Move == DialogueDiplomaticMove.AcceptProposal || parsed.Move == DialogueDiplomaticMove.RejectProposal)
        {
            var batch = owner.QueryAnswerableProposals(rulerId, actor, parsed.TargetKingdomId, parsed.Action, source);
            var usable = batch.Proposals.Where(x => x.IsOpen && x.IsKnownByRuler && x.IsCurrentlyExecutable).ToList();
            if (usable.Count != 1) return usable.Count > 1 ? DiplomacyMiscPromptTexts.Get("OralDiplomacy_MultipleProposals", "There are multiple related proposals; please specify which declaration and action to respond to first.") : DiplomacyMiscPromptTexts.Get("OralDiplomacy_UnavailableProposal", "The original proposal is currently not answerable; please verify the proposal source and validity first.");
            source = usable[0].Source;
            if (terms.Equals(new DialogueDiplomaticTerms())) terms = usable[0].Terms;
        }
        // Event hints are not authority. Exact proposal replies use their source;
        // new matters may reuse only a separately reviewed event association.
        string eventId = parsed.Move == DialogueDiplomaticMove.NewMatter ? reviewedEventId ?? "" : "";
        string fingerprint = rulerId + "|" + actor + "|" + parsed.TargetKingdomId + "|" + parsed.Action + "|" + parsed.Move
            + "|" + JsonConvert.SerializeObject(WorldDiplomacyDialogueTerms.From(terms)) + "|" + source?.DocumentId + "|" + source?.ActionId + "|" + eventId;
        if (!string.IsNullOrEmpty(parsed.SupersedesArrangementId)) fingerprint += "|" + parsed.SupersedesArrangementId + "|" + parsed.SupersedesVersion;
        var duplicate = owner.Storage.DialogueArrangements?.FirstOrDefault(x => x.Fingerprint == fingerprint
            && (x.Status == "accepted" || x.Status == "deferred"
                || (x.Status == "published" && owner.IsLiveRound(owner.ResolveRound(x.RoundId)))
                || (x.Status == "executed" && x.CreatedDay == _host.CurrentDay())));
        duplicate ??= owner.Storage.DialogueArrangements?.FirstOrDefault(x => capturedOrigin != null
            && !string.IsNullOrEmpty(capturedOrigin.InteractionId) && x.SourceInteractionId == capturedOrigin.InteractionId
            && x.SourceChannel == capturedOrigin.Channel && x.SourceSessionId == capturedOrigin.SessionId
            && x.RulerId == rulerId && x.ActorKingdomId == actor && x.TargetKingdomId == parsed.TargetKingdomId
            && x.Action == parsed.Action && x.Move == parsed.Move && x.Terms.ToTerms().Equals(terms)
            && x.SourceDocumentId == (source?.DocumentId ?? "") && x.SourceActionId == (source?.ActionId ?? "")
            && IsRegisteredDialogueCommitment(x));
        if (duplicate != null)
        {
            registered = IsRegisteredDialogueCommitment(duplicate);
            return DiplomacyMiscPromptTexts.Get("OralDiplomacy_DuplicatePrefix", "This commitment has already been submitted to formal diplomacy, document ID: ") + duplicate.DocumentId + "。";
        }
        string id = _host.NewId("dialogue_arrangement");
        var origin = capturedOrigin ?? new DialogueInteractionOrigin("legacy_domain");
        var request = new DialogueCommitmentRequest(id, parsed.SupersedesVersion > 0 ? parsed.SupersedesVersion + 1 : 1,
            origin.InteractionId, rulerId, actor, parsed.TargetKingdomId,
            parsed.Action, parsed.Move, terms, source, origin, parsed.SupersedesArrangementId, parsed.SupersedesVersion, parsed.RevisionReason, eventId);
        var previousReviewedRequest = _reviewedOralEventRequest;
        DialogueSubmissionResult result;
        try
        {
            _reviewedOralEventRequest = string.IsNullOrEmpty(eventId) ? null : request;
            result = DialogueSubmission.Submit(request, owner.DialogueAuthority(request), owner);
        }
        finally { _reviewedOralEventRequest = previousReviewedRequest; }
        var saved = owner.Storage.DialogueArrangements?.FirstOrDefault(x => x.ArrangementId == id);
        if (saved != null) saved.Fingerprint = fingerprint;
        _host.Log("oral commitment ruler=" + rulerId + " action=" + parsed.Action
            + " move=" + parsed.Move + " route=" + result.Decision.Route + " reason=" + result.Decision.Reason
            + " event=" + saved?.RoundId + " binding=" + (source != null ? "source_proposal" : saved?.RoundId == eventId ? "reviewed_existing" : "new_independent"));
        if (!result.Submitted) return string.Format(DiplomacyMiscPromptTexts.Get("OralDiplomacy_SubmissionRejected", "Diplomatic commitment not submitted: {0}."), result.Decision.Reason);
        // This flag means accepted for publication, not that the treaty has taken effect.
        registered = IsRegisteredDialogueCommitment(saved);
        if (saved?.Status == "cancelled") return DiplomacyMiscPromptTexts.Get("OralDiplomacy_CancelledPrefix", "Commitment registered, but official publication cancelled due to changing circumstances: ") + saved.Reason + "。";
        return saved?.Status == "executed" ? DiplomacyMiscPromptTexts.Get("OralDiplomacy_Executed", "Official declaration published; diplomatic action has taken effect.")
            : saved?.Status == "published" ? DiplomacyMiscPromptTexts.Get("OralDiplomacy_Published", "Official declaration published; bilateral treaty requires official response from the other party to take effect.") : DiplomacyMiscPromptTexts.Get("OralDiplomacy_DeferredPrefix", "Commitment registered; official publication deferred. Reason: ") + saved?.Reason + "。";
    }

    internal static bool IsRegisteredDialogueCommitment(WorldDiplomacyDialogueArrangement item)
        => item != null && (item.Status == "accepted" || item.Status == "deferred"
            || item.Status == "published" || item.Status == "executed");

    internal string BuildOralArrangementContext(string rulerId, string actor)
    {
        if (!_host.WorldDiplomacyEnabled() || !DialogueRulerIsCurrent(actor, rulerId) || _host.IsPlayerParty(actor)) return "";
        var body = new StringBuilder(DiplomacyPromptTexts.Get("OralArrangementHeader", "[Oral diplomacy and formal declarations]\nOnly explicit final assent creates a binding commitment. The declaration expresses the accepted decision; drafting cannot reconsider it. For a decision about a known existing event, append event=event_ID and continue that case; omit event only for a genuinely independent matter. Never guess attribution from recency. Accepted commitments must be fulfilled and cannot be cancelled, postponed or replaced with new terms; reject or clarify conflicting requests before giving another promise. Bilateral terms expressly agreed with the player ruler may execute after your formal declaration; third-party assent and conditions remain required. Only the player publishes player declarations. Keep internal IDs, technical status and receipts out of speech.\n"));
        body.AppendLine(DiplomacyPromptTexts.Get("CommittedDeclaration_AllianceTerms", "This alliance establishes non-aggression and cooperation. Joining a particular war requires separate agreement; aid expressly promised will be fulfilled."));
        body.AppendLine(DiplomacyPromptTexts.Get("Oral_AllianceBeforeAssent", "Before proposing or accepting an ordinary alliance, explain the preceding scope naturally in your actual reply, not only after a future refusal."));
        body.AppendLine(DiplomacyPromptTexts.Format("OralArrangementParties", "Your represented kingdom ID={ACTOR}; player kingdom ID={PLAYER}", "ACTOR", actor, "PLAYER", _host.PlayerKingdomId()));
        var knownDocumentIds = CollectKnownDocumentIds(null, Storage.NobleKnowledge, Storage.KingdomKnowledge, null, actor, true, true);
        var knownEvents = new HashSet<string>(Storage.Documents.Where(x => x.IsReadyForPublication && (x.AuthorKingdomId == actor || knownDocumentIds.Contains(x.DocumentId)))
            .Select(x => x.RoundId), StringComparer.OrdinalIgnoreCase);
        knownEvents.UnionWith(Storage.EventAnnouncements.Where(x => x.State == "public").Select(x => x.EventId));
        foreach (var round in GetLiveRounds().Where(x => IsLiveRound(x) && knownEvents.Contains(x.RoundId)).Take(12))
            body.AppendLine(DiplomacyPromptTexts.Format("OralArrangementEvent", "Known active event={ID}; topic={TOPIC}. Bind event when this decision concerns that dispute.", "ID", round.RoundId, "TOPIC", round.RoundTopic));
        var offers = GetLiveRounds().Where(IsLiveRound).SelectMany(x => x.PendingOffers ?? new List<WorldDiplomacyRoundOffer>())
            .Where(x => x != null && x.Status == "open" && x.TargetKingdomId == actor && DialogueDocumentKnown(actor, x.SourceDocumentId)).ToList();
        foreach (var offer in offers.Take(12)) body.AppendLine(DiplomacyPromptTexts.Get("OralArrangementOriginalProposal", "Original proposal document=") + offer.SourceDocumentId + ";action=" + offer.SourceActionId
            + ";proposer=" + offer.ProposerKingdomId + ";intent=" + offer.Intent + ";terms=" + JsonConvert.SerializeObject(WorldDiplomacyDialogueTerms.From(ResolveDialogueTerms(ResolveDocument(offer.SourceDocumentId), offer.SourceActionId))));
        if (offers.Count > 12) body.AppendLine(DiplomacyPromptTexts.Get("OralArrangementMoreProposals", "Further proposals are not listed; uniqueness must not be assumed. Identify the source explicitly."));
        foreach (var item in (Storage.DialogueArrangements ?? new List<WorldDiplomacyDialogueArrangement>()).Where(x => x.RulerId == rulerId && x.Status is "accepted" or "deferred" or "execution_uncertain" or "published").Take(12))
            body.AppendLine(DiplomacyPromptTexts.Format("OralArrangementPrevious", "Previous arrangement ID={ID};version={VERSION};action={ACTION};target={TARGET};status={STATUS};formal declaration={DOCUMENT};result={REASON}",
                "ID", item.ArrangementId, "VERSION", item.Version.ToString(), "ACTION", item.Action.ToString(), "TARGET", item.TargetKingdomId, "STATUS", item.Status, "DOCUMENT", item.DocumentId, "REASON", item.Reason));
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
        string source = arrangementId == "player_manual_declaration" ? "player" : arrangementId == "npc_player_agenda" ? "npc_player_agenda" : "dialogue_commitment";
        return CreateIndependentEventRound(author, target, arrangementId, source, source is "player" or "npc_player_agenda");
    }

    private WorldDiplomacyRound CreateIndependentEventRound(string author, string target, string arrangementId, string source, bool playerLane)
    {
        int day = _host.CurrentDay(), duration = _host.RoundTargetDurationDays();
        var round = new WorldDiplomacyRound { SchemaVersion = _host.RelaySchemaVersion(),
            RoundId = _host.NewId("diplomacy_round"), InitiatorKingdomId = author,
            State = "active", StartedDay = day, LastActivityDay = day,
            SoftEndDay = day + duration, HardEndDay = day + _host.RoundHardDurationDays(duration),
            RelayPassDurationDays = _host.CourtMaxDeliveryDays(), DialogueArrangementId = arrangementId,
            RoundTopic = arrangementId == "player_manual_declaration" ? DiplomacyMiscPromptTexts.Get("Dialogue_PlayerDiplomacyDeclaration", "Player Diplomatic Declaration") : DiplomacyMiscPromptTexts.Get("Dialogue_OralDiplomacyAgreement", "Oral Diplomatic Agreement"),
            IsPlayerInsertion = arrangementId == "player_manual_declaration",
            EventSourceType = source, CountryAdmissionVersion = source is "player" or "npc_player_agenda" or "dialogue_commitment" ? DiplomacyCountryAdmission.CurrentVersion : 0 };
        Storage.ConcurrentRounds ??= new List<WorldDiplomacyRound>();
        Storage.ConcurrentRounds.Add(round);
        InvalidateDialogueIndex();
        EnsureRoundParticipant(round, author, "active", false);
        EnsureRoundParticipant(round, target, "observer", false);
        InitializeEventOwner(round, playerLane);
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
        WorldDiplomacyRound existingEvent = null;
        if (source == null && ReferenceEquals(request, _reviewedOralEventRequest) && !string.IsNullOrEmpty(request.EventRoundId))
        {
            existingEvent = ResolveRound(request.EventRoundId);
            if (!IsLiveRound(existingEvent)
                || (!Storage.EventAnnouncements.Any(x => x.EventId == request.EventRoundId && x.State == "public")
                    && !Storage.Documents.Any(x => x.RoundId == request.EventRoundId && x.IsReadyForPublication
                        && DialogueDocumentKnown(request.ActorKingdomId, x.DocumentId))))
                existingEvent = null; // Changed/unknown event falls back without losing the accepted promise.
        }
        var round = existingEvent ?? (source == null ? CreateIndependentDialogueRound(ResolveDialogueParty(request.ActorKingdomId),
            ResolveDialogueParty(request.TargetKingdomId), request.ArrangementId) : ResolveRound(source.RoundId));
        if (!IsLiveRound(round)) return new DialogueWorkReceipt(false, request.ArrangementId, request.Version, "", "source_round_closed");
        EnsureRoundParticipant(round, request.ActorKingdomId, "active", false);
        RefreshEventRequests(round, "oral_commitment");
        var arrangement = new WorldDiplomacyDialogueArrangement {
            ArrangementId = request.ArrangementId, Version = request.Version, SourceInteractionId = request.SourceInteractionId,
            SourceChannel = request.Origin?.Channel ?? "legacy_domain", SourceSessionId = request.Origin?.SessionId ?? "",
            SourcePlayerText = request.Origin?.PlayerText ?? "", SourceNpcText = request.Origin?.NpcText ?? "",
            SupersedesArrangementId = request.SupersedesArrangementId, SupersedesVersion = request.SupersedesVersion,
            RulerId = request.RulerId, ActorKingdomId = request.ActorKingdomId, TargetKingdomId = request.TargetKingdomId,
            Action = request.Action, Move = request.Move, Terms = WorldDiplomacyDialogueTerms.From(request.Terms),
            RoundId = round.RoundId, SourceDocumentId = source?.DocumentId ?? "", SourceActionId = source?.ActionId ?? "",
            CreatedDay = _host.CurrentDay(), Status = "accepted" };
        if (request.Move == DialogueDiplomaticMove.NewMatter && _host.IsPlayerParty(request.TargetKingdomId)
            && DialogueHost?.IsPlayerRuler(_host.PartyRulerId(request.TargetKingdomId)) == true
            && request.Origin != null && request.Origin.PlayerConsentConfirmed != false && !string.IsNullOrWhiteSpace(request.Origin.PlayerText)
            && !string.IsNullOrWhiteSpace(request.Origin.NpcText))
            arrangement.ConfirmedPlayerRulerId = _host.PartyRulerId(request.TargetKingdomId);
        Storage.DialogueArrangements.Add(arrangement);
        if (HasCurrentPlayerConsent(arrangement) && (arrangement.Action == DialogueDiplomaticAction.Alliance
            || arrangement.Action == DialogueDiplomaticAction.Trade)) arrangement.TreatyExecutionState = "pending";
        RecordDialogueArrangementFact(arrangement, "consent", string.Format(DiplomacyMiscPromptTexts.Get("Dialogue_PromisedDeclarationOnTopic", "I have explicitly promised to issue a formal declaration regarding {0}; an oral agreement does not yet mean the treaty has taken effect."), _host.PartyNameOrEmpty(ResolveDialogueParty(request.TargetKingdomId))));
        PublishDialogueArrangement(arrangement);
        return new DialogueWorkReceipt(true, request.ArrangementId, request.Version, round.RoundId, arrangement.Reason);
    }

    public void CancelUnpublishedCommitmentWork(string arrangementId, int version, string reason)
    {
        // Public cancellation requests cannot revoke an accepted promise. The
        // validator may terminate it only after reading objective current facts.
        var item = Storage.DialogueArrangements?.FirstOrDefault(x => x.ArrangementId == arrangementId && x.Version == version);
        if (CanInvalidateDialogueArrangement(item)) TryValidateOralDeclarationWork(item, out _);
    }

    private void InvalidateImpossibleCommitment(string arrangementId, int version, string reason)
    {
        var item = Storage.DialogueArrangements?.FirstOrDefault(x => x.ArrangementId == arrangementId && x.Version == version);
        if (!CanInvalidateDialogueArrangement(item)) return;
        item.Status = "cancelled"; item.Reason = reason ?? "cancelled";
        foreach (var job in Storage.Jobs.Where(x => x.DialogueArrangementId == item.ArrangementId
            && x.DialogueArrangementVersion == item.Version).ToList()) RemoveJob(job.JobId);
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
        RecordDialogueArrangementFact(item, "cancel", DiplomacyMiscPromptTexts.Get("Dialogue_PromisedPublicationCancelledPrefix", "The previously promised diplomatic publication was cancelled. Reason: ") + item.Reason);
    }

    private void MaintainDialogueArrangements()
    {
        if (!_host.WorldDiplomacyEnabled()) return;
        RecoverLegacyOralDrafts();
        int directRecovered = 0;
        foreach (var item in Storage.DialogueArrangements ?? Enumerable.Empty<WorldDiplomacyDialogueArrangement>())
        {
            if (item.DirectExecutionState == "applied" && item.Status is "accepted" or "deferred")
            {
                if (item.GenerationRetryAfterDay > _host.CurrentDay()
                    || ResolveDocument(item.DocumentId)?.EventExecutionPending == true
                    || Storage.Jobs.Any(x => x.DialogueArrangementId == item.ArrangementId
                        && x.DialogueArrangementVersion == item.Version)) continue;
                if (directRecovered >= 4) continue;
                directRecovered++;
                try { RecoverDirectResultPublication(item); }
                catch (Exception ex) { _host.Log("direct result publication recovery failed id=" + item.ArrangementId + " error=" + ex.GetType().Name); }
                continue;
            }
            RecoverSkippedOralTreaty(item);
            if (item.Status == "accepted" || item.Status == "deferred") RecoverDialoguePublicationReceipt(item);
        }
        foreach (var item in (Storage.DialogueArrangements ?? new List<WorldDiplomacyDialogueArrangement>())
            .Where(x => string.IsNullOrEmpty(x.DirectExecutionState) && (x.Status == "accepted" || x.Status == "deferred")).ToList())
            if (!HasCurrentDiplomaticRulerMemorySnapshot(ResolveDialogueParty(item.ActorKingdomId), item.RulerId))
                InvalidateImpossibleCommitment(item.ArrangementId, item.Version, "original_ruler_no_longer_authorized");
        foreach (var item in (Storage.DialogueArrangements ?? new List<WorldDiplomacyDialogueArrangement>())
            .Where(x => string.IsNullOrEmpty(x.DirectExecutionState) && (x.Status == "accepted" || (x.Status == "deferred" && !x.ExplicitlyDeferred))
                && x.GenerationRetryAfterDay <= _host.CurrentDay()).Take(4).ToList()) PublishDialogueArrangement(item);
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
            && string.IsNullOrEmpty(x.DirectExecutionState)
            && x.Status != "accepted" && x.Status != "deferred" && x.Status != "published"
            && !(x.MemoryReceipts?.Any(m => !m.Delivered) ?? false));
    }

    private void PublishDialogueArrangement(WorldDiplomacyDialogueArrangement item)
    {
        if (item.DirectExecutionState == "applied" && string.IsNullOrEmpty(item.RoundId))
            item.RoundId = CreateIndependentDialogueRound(item.ActorKingdomId, item.TargetKingdomId, item.ArrangementId).RoundId;
        if (RecoverDialoguePublicationReceipt(item)) return;
        if (!IsCurrentDialoguePublicationWork(item)) return;
        try { PublishDialogueArrangementCore(item); }
        catch (Exception ex)
        {
            item.Status = ResolveDocument(item.DocumentId)?.IsReadyForPublication == true ? "published" : "deferred";
            item.Reason = Limit(ex.Message, 180);
            if (item.Status == "deferred") RecordDialogueArrangementFact(item, "technical_delay", DiplomacyMiscPromptTexts.Get("Dialogue_PromisedPublicationPostponedPrefix", "The diplomatic publication was temporarily postponed due to technical issues and has not been published yet. Reason: ") + item.Reason);
            _host.Log("dialogue publication boundary " + item.Status + " id=" + item.ArrangementId);
        }
    }

    private bool RecoverDialoguePublicationReceipt(WorldDiplomacyDialogueArrangement item)
    {
        if (item == null || (item.Status != "accepted" && item.Status != "deferred")) return false;
        var document = ResolveDocument(item.DocumentId);
        if (document?.IsReadyForPublication != true || document.EventExecutionPending
            || item.TreatyExecutionState is "pending" or "started" or "uncertain") return false;
        item.Status = document.ChangedDiplomaticState || item.DirectExecutionState == "applied" ? "executed" : "published";
        item.Reason = document.MechanicalResult ?? "publication_receipt_recovered";
        return true;
    }

    private void PublishDialogueArrangementCore(WorldDiplomacyDialogueArrangement item)
    {
        if (!TryValidateOralDeclarationWork(item, out _)) return;
        if (ResolveDocument(item.DocumentId)?.EventExecutionPending == true)
        { CommitPendingEventDocuments(ResolveRound(item.RoundId), true); return; }
        if (Storage.Jobs.Any(x => x.DialogueArrangementId == item.ArrangementId
            && x.DialogueArrangementVersion == item.Version)) return;
        var job = new WorldDiplomacyJob { JobId = _host.NewId("oral_declaration"), Kind = "generate",
            AuthorKingdomId = item.ActorKingdomId, TargetKingdomId = item.TargetKingdomId,
            RoundId = item.RoundId, ExchangeId = item.RoundId, SourceDocumentId = item.SourceDocumentId,
            DialogueArrangementId = item.ArrangementId, DialogueArrangementVersion = item.Version,
            IsResponse = item.IsWithdrawal || item.Move != DialogueDiplomaticMove.NewMatter, Priority = 95, CreatedDay = _host.CurrentDay() };
        if (item.GenerationFailureAttempts >= 3)
        {
            EnqueueJob(job);
            CommitOralDeclaration(job, BuildCommittedDeclaration(item));
        }
        else if (RebuildOralDeclarationJob(job)) EnqueueJob(job);
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

    private string DescribeDialogueTerms(DialogueDiplomaticTerms terms)
    {
        var body = new StringBuilder();
        if (!string.IsNullOrEmpty(terms.JoiningKingdomId)) body.Append(DiplomacyPromptTexts.Format("DialogueDeclarationJoining", "{SUBJECT} enters {RECEIVER} or recognizes its suzerainty; ",
            "SUBJECT", _host.PartyNameOrEmpty(ResolveDialogueParty(terms.JoiningKingdomId)), "RECEIVER", _host.PartyNameOrEmpty(ResolveDialogueParty(terms.ReceivingKingdomId))));
        if (terms.DailyTribute > 0) body.Append(DiplomacyPromptTexts.Format("DialogueDeclarationTribute", "{PAYER} pays {RECEIVER} {AMOUNT} denars daily for {DAYS} days; ",
            "PAYER", _host.PartyNameOrEmpty(ResolveDialogueParty(terms.TributePayerKingdomId)), "RECEIVER", _host.PartyNameOrEmpty(ResolveDialogueParty(terms.TributeReceiverKingdomId)),
            "AMOUNT", terms.DailyTribute.ToString(), "DAYS", terms.DurationDays.ToString()));
        if (!string.IsNullOrEmpty(terms.CessionSettlementId)) body.Append(DiplomacyPromptTexts.Format("DialogueDeclarationCession", "{FROM} cedes {SETTLEMENT} to {TO}; ",
            "FROM", _host.PartyNameOrEmpty(ResolveDialogueParty(terms.CessionFromKingdomId)), "TO", _host.PartyNameOrEmpty(ResolveDialogueParty(terms.CessionToKingdomId)),
            "SETTLEMENT", _host.SettlementValidationName(terms.CessionSettlementId) ?? terms.CessionSettlementId));
        if (terms.DurationDays > 0 && terms.DailyTribute == 0) body.Append(DiplomacyPromptTexts.Get("DialogueDeclaration_031", "Duration: ") + terms.DurationDays + DiplomacyPromptTexts.Get("DialogueDeclaration_028", " days; "));
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
