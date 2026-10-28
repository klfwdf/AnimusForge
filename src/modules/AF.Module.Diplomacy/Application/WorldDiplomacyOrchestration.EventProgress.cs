using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private static bool IsEventProgressJob(WorldDiplomacyJob job) => job?.Kind is "event_progress" or "event_progress_audit";

    private void ReconcileEventEligibility(WorldDiplomacyRound round)
    {
        if (!IsLiveRound(round) || !DiplomacyEventRules.IsEvent(round)) return;
        RetireUnsolicitedCourtJoins(round);
        var unavailable = new HashSet<string>(round.Participants.Where(x =>
            _host.IsEliminatedParty(x.KingdomId) || !_host.HasIndependentAuthority(x.KingdomId))
            .Select(x => x.KingdomId), StringComparer.Ordinal);
        if (unavailable.Count == 0) return;
        bool changed = false;
        foreach (var party in round.Participants.Where(x => unavailable.Contains(x.KingdomId)))
        {
            if (party.State != "unavailable" || party.MandatoryReplyPending) changed = true;
            party.State = "unavailable"; party.MandatoryReplyPending = false;
        }
        foreach (var response in round.PlayerResponses.Where(x => x.Status == "pending"
            && (unavailable.Contains(x.KingdomId) || unavailable.Contains(ResolveDocument(x.SourceDocumentId)?.AuthorKingdomId ?? ""))))
        { response.Status = "unavailable"; response.FailureReason = "original_party_lost_diplomatic_authority"; changed = true; }
        foreach (var work in round.PlayerCourtWork.Where(x => x.Status is "pending" or "queued"
            && (unavailable.Contains(x.KingdomId) || unavailable.Contains(ResolveDocument(x.SourceDocumentId)?.AuthorKingdomId ?? ""))))
        { work.Status = "unavailable"; changed = true; }
        foreach (var wait in round.PlayerNegotiationWaits.Where(x => x.RequiresAnswer
            && unavailable.Contains(ResolveDocument(x.SourceDocumentId)?.AuthorKingdomId ?? "")))
        { wait.RequiresAnswer = false; changed = true; }
        // Retire only uncommitted speech. Durable arrangements and accepted aid
        // retain their own objective-impossibility and outcome accounting.
        var retiredJobs = Storage.Jobs.Where(x => x.RoundId == round.RoundId
            && string.IsNullOrEmpty(x.DialogueArrangementId)
            && (unavailable.Contains(x.AuthorKingdomId) || unavailable.Contains(x.TargetKingdomId))
            && (x.Kind is "generate" or "player_decision" || IsPrivacyPipelineJob(x) || IsUnpublishedPlayerLifecycleJob(x))).ToList();
        bool retiredHold = retiredJobs.Any(x => !string.IsNullOrEmpty(x.SuspendedReason) && x.SuspendedReason == round.TechnicalHoldReason);
        foreach (var job in retiredJobs) { RemoveJob(job.JobId); changed = true; }
        // Clear only the exact technical hold owned by retired uncommitted work.
        // Accepted obligations and uncertain effect receipts keep their own hold.
        if (retiredHold && !Storage.Jobs.Any(x => x.RoundId == round.RoundId && !string.IsNullOrEmpty(x.SuspendedReason))
            && !Storage.Documents.Any(x => x.RoundId == round.RoundId && x.EventExecutionPending)
            && !Storage.EventEffects.Any(x => x.EventId == round.RoundId && x.Status is "applying" or "uncertain"))
        {
            AccrueTechnicalPause(round);
            round.TechnicalHoldStartedHour = -1; round.TechnicalRequestHoldReason = "";
            round.TechnicalHoldReason = "";
        }
        if (changed)
        {
            round.ConversationRevision++;
            MarkEventDirty(round);
        }
    }

    private bool IsSideQuestion(WorldDiplomacyRound round, string source, string receiver)
    {
        if (round.EventSourceType == "player_followup") return false;
        var obligations = round.PlayerResponses.Where(x => x.SourceDocumentId == source && x.KingdomId == receiver).ToList();
        if (obligations.Count > 0) return obligations.All(x => x.RequestKind == "side_question");
        var requests = ResolveDocument(source)?.ResponseRequests?.Where(x => x.KingdomId == receiver).ToList();
        return requests?.Count > 0 && requests.All(x => x.Kind == "side_question");
    }

    private bool IsSideQuestionJob(WorldDiplomacyRound round, WorldDiplomacyJob job) =>
        string.IsNullOrEmpty(job.DialogueArrangementId) && string.IsNullOrEmpty(job.SilenceSourceDocumentId)
        && IsSideQuestion(round,job.SourceDocumentId,job.AuthorKingdomId)
        && (job.PlayerResponseSourceIds ?? new()).All(x => IsSideQuestion(round,x,job.AuthorKingdomId));

    private bool EventHasPendingWork(WorldDiplomacyRound round, string exceptJob = null) =>
        Storage.Jobs.Any(x => x.RoundId == round.RoundId && x.JobId != exceptJob && x.Kind != "compress"
            && IsEndingBlockingJob(round, x) && !IsSideQuestionJob(round,x))
        || round.PlayerResponses.Any(x => x.Status == "pending" && (x.RequestKind != "side_question" || round.EventSourceType == "player_followup"))
        || round.PlayerCourtWork.Any(x => x.Status is "pending" or "queued" && !IsSideQuestion(round,x.SourceDocumentId,x.KingdomId))
        || round.PlayerNegotiationWaits.Any(x => x.RequiresAnswer && string.IsNullOrEmpty(x.AnswerDocumentId) && string.IsNullOrEmpty(x.ClosureDocumentId))
        || Storage.Documents.Any(x => x.RoundId == round.RoundId && x.EventExecutionPending)
        || Storage.PropagationArrivals.Any(x => x.RoundId == round.RoundId && x.Scope == "court"
            && IsEndingDelivery(round, x.DocumentId)
            && !IsSideQuestion(round,x.DocumentId,x.KingdomId)
            && round.Participants.Any(p => p.KingdomId == x.KingdomId && p.State == "active"))
        || Storage.DialogueArrangements.Any(x => x.RoundId == round.RoundId && AnimusForge.DiplomacyDialogue.DiplomacyCommitmentRules.IsPending(x))
        || Storage.AidRequests.Any(x => x.RoundId == round.RoundId && x.Status == "accepted");

    private void AdvanceEventProgress(WorldDiplomacyRound round)
    {
        if (!IsLiveRound(round) || !EventBoundaryComplete(round, "started")) return;
        if (AdvanceEventEnding(round)) return;
        if (round.EventSourceType == "player_followup")
        {
            if (!EventHasPendingWork(round)) CloseRound("player_negotiation_finished",round);
            return;
        }
        if (!string.IsNullOrEmpty(round.TechnicalHoldReason))
        {
            var held = Storage.Jobs.Where(x => x.RoundId == round.RoundId && !string.IsNullOrEmpty(x.SuspendedReason)).ToList();
            if (held.Count == 0 || held.Any(x => !IsSideQuestionJob(round,x))
                || Storage.EventEffects.Any(x => x.EventId == round.RoundId && x.Status is "applying" or "uncertain")) return;
        }
        if (!HasEventSpeakerWork(round) && ScheduleUnexpressedPosition(round)) return;
        if (EventHasPendingWork(round)) return;
        if (round.EventPhase == "closing") { EnsureEventBoundary(round, "ended"); return; }
        if (round.EventWait != null)
        {
            var wait = round.EventWait;
            bool changed = wait.WorldFingerprint != MaterialHash(MaterialEvidence(round.Participants.Select(x => x.KingdomId)));
            bool stillOpen = round.PendingOffers.Any(x => x.Status == "open" && x.SourceDocumentId == wait.SourceId
                && x.SourceActionId == wait.SourceActionId);
            if (stillOpen && _host.CurrentDay() < wait.UntilDay && !changed && round.EventProgressRevision == round.ConversationRevision) return;
            round.EventWait = null; round.ConversationRevision++;
        }
        // The public opening is enough to reassess a case whose first speaker
        // became unavailable. Requiring its never-published declaration here
        // strands an otherwise idle event and keeps native peace occupied.
        if (round.EventProgressRevision == round.ConversationRevision) return;
        EnqueueJob(new WorldDiplomacyJob { JobId = _host.NewId("event_progress"), Kind = "event_progress",
            RoundId = round.RoundId, ExchangeId = round.RoundId, AuthorKingdomId = round.InitiatorKingdomId,
            Priority = 80, CreatedDay = _host.CurrentDay(), MaxTokens = _host.AnalysisMaxTokens() });
    }

    private bool ScheduleUnexpressedPosition(WorldDiplomacyRound round)
    {
        foreach (var party in round.Participants.Where(x => !string.IsNullOrEmpty(x.AgendaExpressionDocumentId)
            && x.AgendaAnswerDocumentIds.Count == 0))
        {
            var expression = ResolveDocument(party.AgendaExpressionDocumentId);
            if (expression?.IsReadyForPublication != true) continue;
            string respondent = party.AgendaAddressTo.FirstOrDefault(x => !_host.IsPlayerParty(x)
                && CanAnswerEventAgenda(round, x, expression.DocumentId)
                && DialogueDocumentKnown(x,expression.DocumentId));
            if (respondent == null) continue;
            EnqueueGeneration(respondent,party.KingdomId,null,true,expression,85,roundId:round.RoundId);
            if (HasEventSpeakerWork(round)) return true;
        }
        foreach (var party in round.Participants.Where(x => x.State == "active" && !_host.IsPlayerParty(x.KingdomId)
            && x.AgendaAddressTo.Count > 0 && string.IsNullOrEmpty(x.AgendaExpressionDocumentId)))
        {
            if (IsEnding(round) && !HasUnexpressedEndingAgenda(round, party.KingdomId)) continue;
            if (!_host.HasIndependentAuthority(party.KingdomId)) continue;
            var source = Storage.Documents.Where(x => x.RoundId == round.RoundId && x.IsReadyForPublication
                && x.AuthorKingdomId != party.KingdomId && DialogueDocumentKnown(party.KingdomId, x.DocumentId))
                .OrderByDescending(x => x.CreatedUtcTicks).FirstOrDefault();
            if (source == null) continue; // Normal court propagation supplies the first discussion context.
            string target = party.AgendaAddressTo.FirstOrDefault(x => _host.HasIndependentAuthority(x)
                && !_host.IsEliminatedParty(x) && round.Participants.Any(p => p.KingdomId == x && p.State != "unavailable"));
            if (target == null) continue;
            EnqueueGeneration(party.KingdomId, target, null, true, source, 85, roundId: round.RoundId);
            if (HasEventSpeakerWork(round)) return true;
        }
        return false;
    }

    private bool CanAnswerEventAgenda(WorldDiplomacyRound round, string kingdom, string source) =>
        _host.HasIndependentAuthority(kingdom) && !_host.IsEliminatedParty(kingdom)
        && ResolveDocument(source) is WorldDiplomacyDocument expression
        && _host.HasIndependentAuthority(expression.AuthorKingdomId) && !_host.IsEliminatedParty(expression.AuthorKingdomId)
        && round.Participants.Any(p => p.KingdomId == kingdom && (p.State == "active"
            || p.State == "observer" && round.PlayerResponses.Any(x => x.KingdomId == kingdom
                && x.SourceDocumentId == source && x.Status == "pending")));

    private void PrepareEventProgressRequest(WorldDiplomacyJob job)
    {
        var round = ResolveRound(job.RoundId); if (!IsLiveRound(round)) return;
        if (!EnsureBoundaryWorldBook(job, round, round.Participants.Select(x => x.KingdomId).ToList())) return;
        if (IsPlayerDeclarationOpening(round)) job.EventMaterialSnapshot = ReadEventLore(round).ToString(Formatting.None);
        job.SystemPrompt = DiplomacyPromptTexts.Get(job.Kind == "event_progress_audit" ? "EventProgressAudit" : "EventProgressWriter", "");
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("EventEffectPayloadContract", "");
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("SovereignPositionContract", "");
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("EventProgressRoutes", "");
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("EventEndingContract", "");
        job.EventProgressContractVersion = 1;
        job.PositionOwnerReviewVersion = 1;
        if (job.Kind == "event_progress_audit") job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("PositionOwnerReview", "");
        var evidence = BuildEventRequestEvidence(round);
        job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("EventNarrativeSafety", "");
        if (IsPlayerDeclarationOpening(round)) job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("PlayerWorldBookEvidence", "");
        job.UserPrompt = JsonConvert.SerializeObject(new {
            round.RoundId, round.RoundTopic, round.EventBackground, round.EventNarrative,
            world_book_sources = ReadEventLore(round),
            round.ConversationRevision, day = _host.CurrentDay(), Participants = EventPositionsForModel(round),
            ending_decision = round.EventConvergence,
            closure_required_kingdoms = RequiredEventClosureKingdoms(round),
            player_kingdom = CurrentPlayerAuthority(),
            required_npc_position_kingdoms = round.Participants.Where(x => !_host.IsPlayerParty(x.KingdomId)
                && (x.State == "active" || !string.IsNullOrEmpty(x.Agenda))).Select(x => x.KingdomId),
            narrative_safety_review_required = NarrativeSafetyRequest(job, evidence),
            available_continuations = EventContinuationOptions(round, evidence),
            player_waits = round.PlayerNegotiationWaits.Where(x => x.RequiresAnswer && string.IsNullOrEmpty(x.AnswerDocumentId)
                && string.IsNullOrEmpty(x.ClosureDocumentId)),
            sovereign_speakers = round.Participants.Select(x => new { kingdom = x.KingdomId, ruler_hero_id = _host.PartyRulerId(x.KingdomId) }),
            opening_document_missing = ResolveDocument(round.RootDocumentId)?.IsReadyForPublication != true,
            PendingOffers = evidence["pending_offers"], documents = evidence["documents"],
            public_announcements = Storage.EventAnnouncements.Where(x => x.EventId == round.RoundId && x.State == "public")
                .Select(x => new { x.Id, x.Stage, x.Title, x.Body }),
            commitments = evidence["commitments"], history_selection = evidence["history_selection"],
            world = EventHost?.CaptureEventWorld(round.Participants.Select(x => x.KingdomId).ToList()),
            live_effects = _activeEventEffects.Where(x => x.EventId == round.RoundId),
            narrative_actors = NarrativeActors(round.Participants.Select(x => x.KingdomId)),
            actor_review_required = job.Kind == "event_progress_audit"
                ? BuildNarrativeActorAuditRequest(WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.PrivacyDraft)?["effect_update"] as JObject) : new JArray(),
            position_review_required = job.Kind == "event_progress_audit"
                ? BuildPositionOwnerReview(WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.PrivacyDraft)?["positions"] as JArray) : new JArray(),
            proposed_progress = job.Kind == "event_progress_audit" ? job.PrivacyDraft : "",
            validation_error = job.PrivacySourceMaterial,
            rejected_progress = job.Kind == "event_progress" ? job.PrivacyDraft : ""
        });
        FitOptionalEventWorld(job);
        job.RoundConversationRevision = round.ConversationRevision;
        job.CourtKnowledgeRevision = DiplomacyEventRules.KnowledgeRevision(round, job.AuthorKingdomId);
        job.LlmMessages?.Clear();
    }

    private bool CompleteEventProgress(WorldDiplomacyJob job, JObject json)
    {
        if (!IsEventProgressJob(job)) return false;
        var round = ResolveRound(job.RoundId);
        if (!IsLiveRound(round)) { RemoveJob(job.JobId); return true; }
        if (IsEnding(round)) { RemoveJob(job.JobId); MarkEventDirty(round); return true; }
        if (job.Kind == "event_progress")
        {
            job.PrivacyDraft = FreezeEventDraft(json);
            var misplaced = new[] { "narrative", "confirmed_documents", "progress_evidence", "effects", "remove_effects", "body", "title" }
                .Where(x => json[x] != null).ToArray();
            if (misplaced.Length > 0)
            { RetryEventProgress(job, "event_progress_misplaced_boundary_fields:" + string.Join(",", misplaced)
                + ";return_only_outcome_reason_positions_next_actor_source_document_wait_effect_update_closure;optional_effect_changes_belong_inside_effect_update;do_not_add_a_standalone_announcement"); return true; }
            if (!ValidateUnansweredPlayerNarration(round, json, out string narrationError))
            { RetryEventProgress(job, narrationError); return true; }
            if (!ValidateEventClosureEvidence(round, json, out string invalid))
            { RetryEventProgress(job, invalid); return true; }
            if (!ValidateEventProgressRoute(round, json, out _, out invalid))
            { RetryEventProgress(job, invalid); return true; }
            if (!TryReadProgressPositions(round, json, out _, out invalid))
            { RetryEventProgress(job, invalid); return true; }
            // Reuse the execution boundary in read-only mode before paying for
            // semantic review. Publication still rechecks current game state.
            if (json["effect_update"] != null && json["effect_update"].Type != JTokenType.Null)
            {
                if (json["effect_update"] is not JObject update)
                { RetryEventProgress(job, "effect_update:object_or_null_required"); return true; }
                if (!TryPublishEventBoundaryCore(round, "progress:" + round.ConversationRevision, update, out invalid, true))
                { RetryEventProgress(job, "effect_update:" + invalid); return true; }
            }
            job.Kind = "event_progress_audit";
            job.PrivacySourceMaterial = "";
            job.PrivacyAuditRetries = 0;
            job.IsRunning = false; job.LlmMessages?.Clear(); return true;
        }
        if (json["approved"]?.Type != JTokenType.Boolean)
        { RetryProgressAudit(job, "approved_boolean_missing"); return true; }
        if (!json["approved"].Value<bool>()
            || !WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.PrivacyDraft, out var progress))
        { RetryEventProgress(job, "event_progress_audit_failed:" + EventAuditFeedback(json)); return true; }
        if ((string)progress["outcome"] is not "continue" and not "wait" && json["closure_consistent"]?.Type != JTokenType.Boolean)
        { RetryProgressAudit(job, "event_closure_review_required:review_each_claim_and_remaining_issue"); return true; }
        if ((string)progress["outcome"] is not "continue" and not "wait" && !(bool)json["closure_consistent"])
        { RetryEventProgress(job, "event_closure_review_required:" + EventAuditFeedback(json)); return true; }
        if (job.PositionOwnerReviewVersion >= 1 && !ValidatePositionOwnerReview(progress["positions"] as JArray, json,
            out string ownerError, out bool rejectedOwner))
        {
            if (rejectedOwner) RetryEventProgress(job, ownerError); else RetryProgressAudit(job, ownerError);
            return true;
        }
        if (EventHasPendingWork(round, job.JobId)) { job.Kind = "event_progress"; job.PrivacyDraft = ""; return true; }
        if (!ValidateNarrativeSafety(job, progress, json, out string reason, out bool rejectedSafety))
        {
            if (rejectedSafety) RetryEventProgress(job, reason); else RetryProgressAudit(job, reason);
            return true;
        }
        if (!ValidateNarrativeActorAudit(progress["effect_update"] as JObject, json, out reason))
        {
            if (reason.StartsWith("narrative_actor_audit_mismatch", StringComparison.Ordinal)) RetryEventProgress(job, reason);
            else RetryProgressAudit(job, reason);
            return true;
        }
        if (!ValidateEventClosureEvidence(round, progress, out reason) || !TryApplyEventProgress(round, progress, out reason))
        { RetryEventProgress(job, reason); return true; }
        RemoveJob(job.JobId);
        if (round.EventPhase == "closing")
        { round.EventClosureReviewVersion = 1; CloseRound("event_" + round.EventOutcome, round); }
        return true;
    }

    private void RetryEventProgress(WorldDiplomacyJob job, string reason)
    {
        job.Kind = "event_progress"; job.IsRunning = false; job.LlmMessages?.Clear();
        job.PrivacySourceMaterial = reason;
        job.RetryAfterUtcTicks = 0;
        if (++job.SemanticRepairAttempts >= 3) SuspendDiplomacyJob(job, "event_progress_repair_exhausted:" + reason);
        _host.Log("diplomacy.progress_repair event=" + job.RoundId + " reason=" + reason);
    }

    private void RetryProgressAudit(WorldDiplomacyJob job, string reason)
    {
        job.PrivacySourceMaterial = "event_progress_audit_invalid:" + reason + ";review_same_draft";
        job.IsRunning = false; job.LlmMessages?.Clear();
        if (++job.PrivacyAuditRetries >= 3) HoldTechnicalRequest(job, job.PrivacySourceMaterial);
    }

    // Closure is a semantic decision with per-party evidence, not a document count.
    private IEnumerable<string> RequiredEventClosureKingdoms(WorldDiplomacyRound round) => round.Participants
        .Where(x => x.State == "active" || !string.IsNullOrEmpty(x.Agenda)
            // Declaring war may withdraw the human from talks. Their published
            // initiating act still needs an evidence-based conclusion, even
            // though we deliberately store no invented human agenda.
            || IsPlayerDeclarationOpening(round) && x.KingdomId == ResolveDocument(round.RootDocumentId).AuthorKingdomId)
        .Select(x => x.KingdomId);

    // The independent reviewer checks meaning; this boundary checks complete sources.
    private bool ValidateEventClosureEvidence(WorldDiplomacyRound round, JObject progress, out string reason)
    {
        reason = "event_closure_evidence_required:closure_requires_each_participant_kingdom_status_reason_and_sources";
        if (progress["outcome"]?.Type != JTokenType.String) return false;
        string outcome = (string)progress["outcome"];
        if (outcome is "continue" or "wait") { reason = ""; return true; }
        if (progress["closure"] is not JArray rows || rows.Count > round.Participants.Count) return false;
        var required = new HashSet<string>(RequiredEventClosureKingdoms(round), StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in rows)
        {
            if (token is JObject candidate && candidate["kingdom"]?.Type == JTokenType.String
                && !required.Contains((string)candidate["kingdom"]))
            { reason = "event_closure_unexpected_kingdom:" + (string)candidate["kingdom"]
                + ";required_exactly=" + string.Join(",", required); return false; }
            if (token is not JObject row || row["kingdom"]?.Type != JTokenType.String
                || !required.Contains((string)row["kingdom"]) || !seen.Add((string)row["kingdom"])
                || row["reason"]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)row["reason"])
                || ((string)row["reason"]).Length > 1200 || row["status"]?.Type != JTokenType.String
                || row["sources"] is not JArray sources || sources.Count > 16) return false;
            string status = (string)row["status"], kingdom = (string)row["kingdom"];
            if (status is not "satisfied" and not "unresolved" and not "withdrawn" and not "unavailable") return false;
            if (outcome == "resolved" && status != "satisfied")
            { reason = "event_closure_unresolved:kingdom=" + kingdom + ";do_not_label_replies_or_stalemate_as_resolved"; return false; }
            if (status == "unavailable" && _host.HasIndependentAuthority(kingdom) && !_host.IsEliminatedParty(kingdom)) return false;
            if (status != "unavailable" && sources.Count == 0) return false;
            foreach (var id in sources)
            {
                if (id.Type == JTokenType.String && (ResolveDocument((string)id) is WorldDiplomacyDocument doc
                    && doc.RoundId == round.RoundId && doc.IsReadyForPublication && DialogueDocumentKnown(kingdom, doc.DocumentId)
                    || Storage.EventAnnouncements.Any(x => x.Id == (string)id && x.EventId == round.RoundId && x.State == "public"))) continue;
                reason = "event_closure_source_invalid:kingdom=" + kingdom + ";use_same_event_known_published_documents_or_public_announcements"; return false;
            }
        }
        if (!seen.SetEquals(required))
        { reason = "event_closure_missing_kingdoms:" + string.Join(",", required.Except(seen)); return false; }
        reason = ""; return true;
    }

    private bool TryReadProgressPositions(WorldDiplomacyRound round, JObject progress,
        out Dictionary<string, WorldDiplomacyRoundParticipant> positions, out string reason)
    {
        positions = null;
        reason = "event_progress_positions_invalid:include_each_active_NPC_once_with_agenda_gain_cost_red_line_alternative";
        if (progress["positions"] is not JArray rows) return false;
        positions = new Dictionary<string, WorldDiplomacyRoundParticipant>(StringComparer.Ordinal);
        foreach (var row in rows.OfType<JObject>())
        {
            string id = (string)row["kingdom"];
            if (string.IsNullOrEmpty(id) || !round.Participants.Any(x => x.KingdomId == id) || positions.ContainsKey(id)) return false;
            // Player decisions come from player-authored documents. Model
            // progress may not silently replace them, including legacy agendas.
            if (_host.IsPlayerParty(id))
            {
                if (PositionFields.Any(x => row[x]?.Type != JTokenType.String || !string.IsNullOrEmpty((string)row[x])))
                { reason = "event_progress_player_agency:omit_player_position_or_supply_empty_fields;do_not_author_player_intentions"; return false; }
                positions.Add(id, new WorldDiplomacyRoundParticipant());
                continue;
            }
            if (!TryReadEventPosition(row, out var position))
            { reason = "event_progress_position_invalid:kingdom=" + id + ";state=" + round.Participants.First(x => x.KingdomId == id).State
                + ";NPC_not_human;retain_recorded_position_even_if_withdrawn;use_nonempty_agenda_gain_cost_red_line_alternative_strings_not_preferred_outcome_or_expected_cost"; return false; }
            positions.Add(id, position);
        }
        var parsed = positions;
        if (positions.Count != rows.Count || round.Participants.Any(x => !_host.IsPlayerParty(x.KingdomId) && (x.State == "active" || !string.IsNullOrEmpty(x.Agenda))
            && !parsed.ContainsKey(x.KingdomId))) return false;
        reason = ""; return true;
    }

    private bool TryApplyEventProgress(WorldDiplomacyRound round, JObject progress, out string reason)
    {
        reason = "event_progress_invalid";
        string outcome = (string)progress["outcome"], explanation = (string)progress["reason"];
        if (outcome is not "continue" and not "wait" && round.Participants.Any(x => x.State == "active"
            && !_host.IsPlayerParty(x.KingdomId) && x.AgendaAddressTo.Any(k => _host.HasIndependentAuthority(k)
                && !_host.IsEliminatedParty(k)) && string.IsNullOrEmpty(x.AgendaExpressionDocumentId)))
        { reason = "event_core_position_unexpressed"; return false; }
        if (outcome is not "continue" and not "wait" && round.Participants.Any(x => !string.IsNullOrEmpty(x.AgendaExpressionDocumentId)
            && x.AgendaAnswerDocumentIds.Count == 0 && x.AgendaAddressTo.Any(k => !_host.IsPlayerParty(k)
                && CanAnswerEventAgenda(round, k, x.AgendaExpressionDocumentId))))
        { reason = "event_core_position_unanswered"; return false; }
        if (outcome is not "continue" and not "wait" and not "resolved" and not "deadlocked" and not "deferred" and not "escalated"
            || string.IsNullOrWhiteSpace(explanation) || explanation.Length > 2000 || progress["positions"] is not JArray rows) return false;
        if (!TryReadProgressPositions(round, progress, out var positions, out reason)) return false;
        if (outcome is not "continue" and not "wait" && (round.PendingOffers.Any(x => x.Status == "open")
            || Storage.DiplomaticThreats.Any(x => x.StageRoundId == round.RoundId && x.Status == "open")))
        {
            BeginEventEnding(round, "");
            reason = "";
            return true;
        }
        string actor = (string)progress["next_actor"], sourceId = (string)progress["source_document"];
        var source = ResolveDocument(sourceId);
        if (!ValidateEventProgressRoute(round, progress, out var wait, out reason)) return false;
        if (wait != null) wait.WorldFingerprint = MaterialHash(MaterialEvidence(round.Participants.Select(x => x.KingdomId)));
        // A resolved result must not silently discard an unaccepted proposal.
        if (outcome == "resolved" && round.PendingOffers.Any(x => x.Status == "open")) return false;
        if (outcome == "escalated" && !Storage.Documents.Any(x => x.RoundId == round.RoundId && x.IsReadyForPublication
            && x.ChangedDiplomaticState && (x.Intent == "declare_war" || x.Actions?.Any(a => a.Intent == "declare_war" && a.ChangedDiplomaticState) == true))) return false;
        if (progress["effect_update"] is JObject update)
        {
            string stage = "progress:" + round.ConversationRevision;
            if (!TryPublishEventBoundary(round, stage, update, out reason)) return false;
        }
        foreach (var party in round.Participants)
        {
            if (_host.IsPlayerParty(party.KingdomId)) continue;
            if ((party.State != "active" && string.IsNullOrEmpty(party.Agenda)) || !positions.TryGetValue(party.KingdomId, out var p)) continue;
            party.Agenda = p.Agenda; party.PreferredOutcome = p.PreferredOutcome; party.ExpectedCost = p.ExpectedCost;
            party.RedLine = p.RedLine; party.Alternative = p.Alternative;
        }
        round.EventProgressRevision = round.ConversationRevision; round.EventWait = wait;
        if (outcome == "continue")
        {
            round.EventPhase = "discussing";
            round.TopicSeedContext = explanation;
            EnqueueGeneration(actor, source.AuthorKingdomId == actor ? source.TargetKingdomId : source.AuthorKingdomId,
                null, true, source, 80, roundId: round.RoundId);
        }
        else if (outcome == "wait") round.EventPhase = "waiting";
        else
        {
            round.EventPhase = "closing"; round.EventOutcome = outcome; round.EventOutcomeReason = explanation;
            round.EventCloseReason = "event_" + outcome;
        }
        reason = ""; return true;
    }

    private JArray EventContinuationOptions(WorldDiplomacyRound round, JObject evidence)
    {
        var result = new JArray();
        var documents = (evidence["documents"] as JArray ?? new JArray()).OfType<JObject>().ToList();
        foreach (var party in round.Participants.Where(x => x.State == "active" && !_host.IsPlayerParty(x.KingdomId)
            && _host.HasIndependentAuthority(x.KingdomId)))
            foreach (var document in documents.Where(x => (x["known_by"] as JArray)?.Values<string>().Contains(party.KingdomId) == true
                && IsFreshOrdinaryContinuation(round, party.KingdomId, ResolveDocument((string)x["DocumentId"]))))
                result.Add(new JObject { ["next_actor"] = party.KingdomId, ["source_document"] = document["DocumentId"]?.DeepClone() });
        return result;
    }

    private bool ValidateEventProgressRoute(WorldDiplomacyRound round, JObject progress, out DiplomacyEventWait wait, out string reason)
    {
        wait = null; reason = "";
        if (IsEnding(round)) { reason = "event_ending_is_program_controlled"; return false; }
        string outcome = (string)progress["outcome"];
        if (outcome == "continue")
        {
            string actor = (string)progress["next_actor"], id = (string)progress["source_document"];
            if (_host.IsPlayerParty(actor))
            { reason = "event_progress_player_actor_forbidden:the_human_controls_this_country;choose_an_available_NPC_continuation_or_a_grounded_deferred_or_deadlocked_conclusion;never_simulate_player_reply"; return false; }
            if (!round.Participants.Any(x => x.KingdomId == actor && x.State == "active") || !_host.HasIndependentAuthority(actor))
            { reason = "event_progress_actor_unavailable:choose_available_continuations;observers_cannot_be_forced_into_negotiation"; return false; }
            var source = ResolveDocument(id);
            if (source?.RoundId != round.RoundId || !source.IsReadyForPublication || !DialogueDocumentKnown(actor, id))
            { reason = "event_progress_source_invalid:source_must_be_a_same_event_published_document_known_to_next_actor"; return false; }
            if (!IsFreshOrdinaryContinuation(round, actor, source))
            { reason = "event_progress_repeat_source:choose_a_fresh_corresponding_claim_or_answer_from_available_continuations;do_not_repeat_an_answered_claim_or_reply_to_yourself"; return false; }
        }
        if (outcome != "wait") return true;
        if (progress["wait"] is not JObject row || row["until_day"]?.Type != JTokenType.Integer
            || !int.TryParse((string)row["until_day"], out int until) || until <= _host.CurrentDay()
            || row["reason"]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)row["reason"]))
        { reason = "event_progress_wait_shape_invalid:wait_requires_future_integer_until_day_and_reason"; return false; }
        string sourceId = (string)row["source_id"], action = (string)row["source_action_id"];
        if ((string)row["kind"] != "offer" || string.IsNullOrEmpty(action)
            || ResolveDocument(sourceId)?.RoundId != round.RoundId || ResolveDocument(sourceId)?.IsReadyForPublication != true
            || !round.PendingOffers.Any(x => x.SourceDocumentId == sourceId && x.SourceActionId == action && x.Status == "open"))
        { reason = "event_progress_wait_without_open_offer:use_exact_pending_offers_source_document_and_source_action_id;warnings_and_optional_player_replies_are_not_offers;choose_available_NPC_continuation_or_grounded_deferred_or_deadlocked_closure"; return false; }
        wait = new DiplomacyEventWait { Kind = "offer", SourceId = sourceId, SourceActionId = action, Reason = (string)row["reason"], UntilDay = until };
        return true;
    }
}
