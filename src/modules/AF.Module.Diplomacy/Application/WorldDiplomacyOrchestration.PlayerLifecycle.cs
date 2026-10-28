using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyStructureRules;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private readonly HashSet<string> _pendingPlayerReplyRoutingIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<string> _playerReplyRoutingBacklog = new();

    private bool PlayerKnowsPublicSource(WorldDiplomacyDocument source, WorldDiplomacyDocument reply) =>
        source?.IsReadyForPublication == true && (DialogueDocumentKnown(reply.AuthorKingdomId, source.DocumentId)
            || source.IsRead || reply.SourceDocumentId == source.DocumentId);

    private void QueuePlayerReplyRoutingCheck(WorldDiplomacyDocument document)
    {
        if (document?.IsPlayerAuthored != true || !document.IsReadyForPublication || document.PlayerReplyRoutingRechecked
            || CurrentPlayerAuthority() != document.AuthorKingdomId) return;
        if (Storage.Jobs.Any(x => x.Kind == "player_reply_routing" && x.DocumentId == document.DocumentId)) return;
        if (document.PlayerReplyRoutingPending && Storage.Jobs.Count(x => x.SchedulingOwner == "player") >= _host.MaxPendingJobs()) return;
        var candidates = Storage.Documents.Where(x => x != null && !x.IsPlayerAuthored && x.RoundId != document.RoundId
            && IsLiveRound(ResolveRound(x.RoundId)) && GetDocumentTargetIds(x).Contains(document.AuthorKingdomId)
            && PlayerKnowsPublicSource(x, document)).OrderByDescending(x => _waitingByDocument.ContainsKey(x.DocumentId))
            .ThenByDescending(x => x.Day).Take(64).ToList();
        var advisory = ResolveDocument(document.SourceDocumentId);
        if (advisory != null && !advisory.IsPlayerAuthored && IsLiveRound(ResolveRound(advisory.RoundId))
            && GetDocumentTargetIds(advisory).Contains(document.AuthorKingdomId) && PlayerKnowsPublicSource(advisory, document)
            && !candidates.Contains(advisory)) candidates.Add(advisory);
        if (candidates.Count == 0)
        { document.PlayerReplyRoutingPending = false; _pendingPlayerReplyRoutingIds.Remove(document.DocumentId); return; }
        document.PlayerReplyRoutingPending = true;
        if (_pendingPlayerReplyRoutingIds.Add(document.DocumentId)) _playerReplyRoutingBacklog.Enqueue(document.DocumentId);
        var round = EnsurePlayerDocumentRound(document);
        if (!IsLiveRound(round)) return;
        var job = new WorldDiplomacyJob { JobId = _host.NewId("player_reply_routing"), Kind = "player_reply_routing",
            RoundId = round.RoundId, ExchangeId = round.RoundId, DocumentId = document.DocumentId,
            AuthorKingdomId = document.AuthorKingdomId, Priority = 100, CreatedDay = _host.CurrentDay(),
            ReplyRoutingCandidateDocumentIds = candidates.Select(x => x.DocumentId).ToList() };
        EnqueueJob(job);
    }

    private void MaintainPlayerReplyRoutingBacklog()
    {
        for (int i = 0, count = Math.Min(4, _playerReplyRoutingBacklog.Count); i < count; i++)
        {
            string id = _playerReplyRoutingBacklog.Dequeue();
            var reply = ResolveDocument(id);
            if (reply?.PlayerReplyRoutingPending != true || CurrentPlayerAuthority() != reply.AuthorKingdomId)
            { _pendingPlayerReplyRoutingIds.Remove(id); continue; }
            QueuePlayerReplyRoutingCheck(reply);
            if (reply.PlayerReplyRoutingPending) _playerReplyRoutingBacklog.Enqueue(id);
        }
    }

    private void PreparePlayerReplyRoutingRequest(WorldDiplomacyJob job)
    {
        var reply = ResolveDocument(job.DocumentId);
        job.SystemPrompt = PromptComposerTexts.ApplyOutputLanguage(DiplomacyPromptTexts.Get("PlayerReplyRoutingAnalysis", ""));
        job.UserPrompt = JsonConvert.SerializeObject(new { declaration = new { reply.Title, reply.Body },
            advisory_source = reply.SourceDocumentId,
            public_sources = job.ReplyRoutingCandidateDocumentIds.Select(ResolveDocument).Where(x => PlayerKnowsPublicSource(x, reply))
                .Select(x => new { x.DocumentId, x.RoundId, x.Title, x.Body, x.AuthorKingdomId, x.Day }) });
        job.MaxTokens = _host.AnalysisMaxTokens(); job.LlmMessages?.Clear();
        job.RoundConversationRevision = ResolveRound(job.RoundId)?.ConversationRevision ?? 0;
        job.CourtKnowledgeRevision = DiplomacyEventRules.KnowledgeRevision(ResolveRound(job.RoundId), job.AuthorKingdomId);
        job.PersonalMemoryRulerId = _host.PartyRulerId(job.AuthorKingdomId);
    }

    private void CompletePlayerReplyRouting(WorldDiplomacyJob job, JObject json)
    {
        var reply = ResolveDocument(job.DocumentId);
        if (json["answered_npc_document_ids"] is not JArray answers || answers.Any(x => x.Type != JTokenType.String)
            || answers.Any(x => !job.ReplyRoutingCandidateDocumentIds.Contains(x.Value<string>(), StringComparer.OrdinalIgnoreCase)))
        {
            if (++job.LifecycleClassificationAttempts >= 2) SuspendDiplomacyJob(job, "player_reply_routing_invalid");
            return;
        }
        if (reply?.IsReadyForPublication != true || CurrentPlayerAuthority() != reply.AuthorKingdomId)
        { SuspendDiplomacyJob(job, "player_reply_routing_authority_changed"); return; }
        reply.AnsweredNpcDocumentIds = reply.AnsweredNpcDocumentIds.Concat(answers.Values<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        reply.PlayerReplyRoutingRechecked = true;
        reply.PlayerReplyRoutingPending = false;
        _pendingPlayerReplyRoutingIds.Remove(reply.DocumentId);
        RemoveJob(job.JobId);
        BindPlayerDeclarationToSharedEvent(reply);
        RegisterPublishedPlayerAnswer(reply);
        RegisterPlayerResponseWork(reply);
    }
    private static bool HasExplicitPlayerLifecycle(JObject json) => json?["requires_response"]?.Type == JTokenType.Boolean
        && json["round_participation"]?.Type == JTokenType.String && json["round_participation"].Value<string>() is "continue" or "withdraw"
        && json["round_status"]?.Type == JTokenType.String && json["round_status"].Value<string>() is "continue" or "resolved" or "deadlocked";

    private string PlayerLifecycleContract() => DiplomacyPromptTexts.Get("PlayerLifecycleContract", "");

    private static bool IsUnpublishedPlayerLifecycleJob(WorldDiplomacyJob job)
        => job?.Kind == "player_lifecycle" && job.ForcedIntent != "recover_published_lifecycle";

    private bool DeferIncompletePlayerLifecycle(WorldDiplomacyJob job, string raw)
    {
        if (!IsEventJob(job) || IsOralDeclarationJob(job)) return false;
        if (!WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(raw, out var json) || json["actions"] is not JArray
            || HasExplicitPlayerLifecycle(json)) return false;
        job.PrivacyDraft = raw;
        job.PrivacyDeclarationSystem = job.SystemPrompt;
        job.Kind = "player_lifecycle";
        PreparePlayerLifecycleRequest(job);
        return true;
    }

    private void PreparePlayerLifecycleRequest(WorldDiplomacyJob job)
    {
        var round = ResolveRound(job.RoundId);
        if (IsUnpublishedPlayerLifecycleJob(job) && !HasPlayerLifecycleDraft(job))
        {
            // Older recovery code could persist a metadata job after deleting
            // its draft. Re-enter full generation rather than classify nothing.
            job.Kind = "generate"; job.PrivacyDraft = "";
            job.PrivacyRevision = -1; job.PrivacyPublicContext = "";
            job.SemanticRepairAttempts = 0;
            PrepareSharedRequest(job);
            return;
        }
        if (job.ForcedIntent == "recover_published_lifecycle")
        {
            var saved = ResolveDocument(job.DocumentId);
            if (saved != null) job.PrivacyDraft = JsonConvert.SerializeObject(new { saved.DocumentId, saved.Title, saved.Body, saved.Actions });
        }
        job.SystemPrompt = PromptComposerTexts.ApplyOutputLanguage(DiplomacyPromptTexts.Get("PlayerLifecycleAnalysis", ""));
        job.UserPrompt = JsonConvert.SerializeObject(new {
            purpose = job.ForcedIntent == "recover_published_lifecycle" ? "classify_existing_public_document_without_republication" : "complete_draft_metadata",
            public_declaration = job.PrivacyDraft, event_context = BuildEventContinuity(round, job.AuthorKingdomId),
            validation_error = job.LifecycleClassificationAttempts > 0
                ? "requires_response:boolean;round_participation:continue|withdraw;round_status:continue|resolved|deadlocked;return_complete_metadata_for_same_draft" : ""
        });
        job.MaxTokens = _host.AnalysisMaxTokens();
        job.LlmMessages?.Clear();
        job.RoundConversationRevision = round?.ConversationRevision ?? 0;
        job.CourtKnowledgeRevision = DiplomacyEventRules.KnowledgeRevision(round, job.AuthorKingdomId);
        job.PersonalMemoryRulerId = _host.PartyRulerId(job.AuthorKingdomId);
    }

    private void CompletePlayerLifecycle(WorldDiplomacyJob job, JObject json)
    {
        if (!HasExplicitPlayerLifecycle(json))
        {
            if (++job.LifecycleClassificationAttempts >= 2) SuspendDiplomacyJob(job, "player_lifecycle_metadata_invalid");
            else { job.IsRunning = false; PreparePlayerLifecycleRequest(job); }
            return;
        }
        if (job.ForcedIntent == "recover_published_lifecycle")
        {
            var document = ResolveDocument(job.DocumentId);
            var round = ResolveRound(job.RoundId);
            if (document?.IsReadyForPublication != true || !IsLiveRound(round))
            { SuspendDiplomacyJob(job, "player_lifecycle_recovery_source_missing"); return; }
            document.RequiresResponse = json["requires_response"].Value<bool>();
            document.RoundParticipation = json["round_participation"].Value<string>();
            document.RoundStatus = json["round_status"].Value<string>();
            document.PlayerLifecycleClassified = true;
            var participant = round.Participants.FirstOrDefault(x => x.KingdomId == document.AuthorKingdomId);
            bool finished = document.RoundParticipation == "withdraw" || document.RoundStatus is "resolved" or "deadlocked";
            if (participant != null) participant.State = finished ? "withdrawn" : "active";
            if (!document.LegacyNoticeSuppressed && document.RequiresResponse && !finished && GetDocumentTargetIds(document).Contains(CurrentPlayerAuthority()))
            {
                // The earlier notification never announced the recovered obligation. Notify again; no retrospective penalty.
                document.ActualPlayerNoticeHour = -1; document.FormalNoticeShown = document.IsNotified = false;
                var wait = round.PlayerNegotiationWaits.FirstOrDefault(x => x.SourceDocumentId == document.DocumentId);
                if (wait == null) { wait = new() { SourceDocumentId = document.DocumentId }; round.PlayerNegotiationWaits.Add(wait); }
                wait.RequiresAnswer = true; wait.NotifiedHour = -1; wait.AnswerDocumentId = wait.ClosureDocumentId = "";
                IndexPlayerWait(round, wait);
            }
            round.TechnicalHoldReason = "";
            RemoveJob(job.JobId);
            if (document.RequiresResponse && !finished)
                foreach (string receiver in GetDocumentTargetIds(document).Where(x => x != CurrentPlayerAuthority()
                    && DialogueDocumentKnown(x, document.DocumentId)))
                    ProcessPlayerEventCourtArrival(receiver, document);
            // Existing published replies are reclassified for source attribution, never executed a second time.
            foreach (var reply in Storage.Documents.Where(x => x?.IsPlayerAuthored == true && x.IsReadyForPublication
                && !x.PlayerReplyRoutingRechecked && x.Day >= document.Day && GetDocumentTargetIds(x).Contains(document.AuthorKingdomId)).ToList())
                QueuePlayerReplyRoutingCheck(reply);
            MarkEventDirty(round);
            return;
        }
        if (!HasPlayerLifecycleDraft(job)) { SuspendDiplomacyJob(job, "player_lifecycle_draft_missing"); return; }
        var draft = WorldDiplomacyEnvelopeJsonRules.ParseJsonObject(job.PrivacyDraft);
        foreach (string name in new[] { "requires_response", "round_participation", "round_status" }) draft[name] = json[name].DeepClone();
        job.Kind = "generate"; job.SystemPrompt = job.PrivacyDeclarationSystem;
        // Lifecycle classification can change obligations or withdrawal. Review
        // the merged draft before publication; the earlier audit predates it.
        DeferDraftForPrivacyCheck(job, draft.ToString(Formatting.None));
    }

    private static bool HasPlayerLifecycleDraft(WorldDiplomacyJob job)
        => WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(job.PrivacyDraft, out var draft)
            && draft["title"]?.Type == JTokenType.String && draft["body"]?.Type == JTokenType.String
            && !string.IsNullOrWhiteSpace((string)draft["title"])
            && !string.IsNullOrWhiteSpace((string)draft["body"]) && draft["actions"] is JArray;

    private void RehomePlayerReplyEvent(WorldDiplomacyRound provisional, WorldDiplomacyRound destination)
    {
        RequestLeases.InvalidateRound(provisional.RoundId);
        var provisionalJobs = Storage.Jobs.Where(x => x.RoundId == provisional.RoundId && x.Kind != "compress").ToList();
        // A provisional opening is not continuation work. Its frozen evidence,
        // review and pending transport must never acquire the destination root.
        foreach (var obsolete in provisionalJobs.Where(IsProvisionalOpeningWork)) RemoveJob(obsolete.JobId);
        var migratingJobs = provisionalJobs.Where(x => !IsProvisionalOpeningWork(x)).ToList();
        destination.ConversationRevision++;
        foreach (var doc in Storage.Documents.Where(x => x != null && x.RoundId == provisional.RoundId).ToList())
            MovePlayerDocumentRouting(doc, destination);
        foreach (var participant in provisional.Participants)
            if (!destination.Participants.Any(x => x.KingdomId == participant.KingdomId)) destination.Participants.Add(participant);
        foreach (var id in provisional.RelayRouteKingdomIds)
            if (!destination.RelayRouteKingdomIds.Contains(id)) destination.RelayRouteKingdomIds.Add(id);
        foreach (var offer in provisional.PendingOffers)
            if (!destination.PendingOffers.Any(x => x.SourceDocumentId == offer.SourceDocumentId && x.SourceActionId == offer.SourceActionId)) destination.PendingOffers.Add(offer);
        provisional.PendingOffers.Clear(); // Closing the empty provisional shell must not expire transferred clauses.
        destination.ExecutedActionCount += provisional.ExecutedActionCount;
        destination.SubstantiveProgressCount += provisional.SubstantiveProgressCount;
        destination.DiplomaticActionAttemptCount += provisional.DiplomaticActionAttemptCount;
        foreach (var response in provisional.PlayerResponses)
            if (!destination.PlayerResponses.Any(x => x.SourceDocumentId == response.SourceDocumentId && x.KingdomId == response.KingdomId)) destination.PlayerResponses.Add(response);
        foreach (var work in provisional.PlayerCourtWork)
            if (!destination.PlayerCourtWork.Any(x => x.SourceDocumentId == work.SourceDocumentId && x.KingdomId == work.KingdomId)) destination.PlayerCourtWork.Add(work);
        foreach (var wait in provisional.PlayerNegotiationWaits)
            if (!destination.PlayerNegotiationWaits.Any(x => x.SourceDocumentId == wait.SourceDocumentId))
            { destination.PlayerNegotiationWaits.Add(wait); _silenceTicketIds.Remove(wait.SourceDocumentId); IndexPlayerWait(destination, wait); }
        foreach (var receipt in provisional.CourtKnowledgeReceipts)
            if (!destination.CourtKnowledgeReceipts.Contains(receipt)) destination.CourtKnowledgeReceipts.Add(receipt);
        destination.CourtKnowledgeReceiptIndex = new(destination.CourtKnowledgeReceipts, StringComparer.OrdinalIgnoreCase);
        foreach (var knowledge in provisional.CourtKnowledgeVersions)
            destination.CourtKnowledgeVersions[knowledge.Key] = Math.Max(DiplomacyEventRules.KnowledgeRevision(destination, knowledge.Key), knowledge.Value);
        // These records now belong to the destination. Closing the shell must
        // not mutate the same response objects to "transferred" or spawn a
        // second follow-up case for obligations we already moved.
        provisional.PlayerResponses.Clear();
        provisional.PlayerCourtWork.Clear();
        provisional.PlayerNegotiationWaits.Clear();
        foreach (var job in migratingJobs)
        {
            job.RoundId = job.ExchangeId = destination.RoundId;
            job.JobId = _host.NewId("diplomacy_reply_handoff"); job.IsRunning = false; job.RequestAttempt = 0;
            job.LlmMessages?.Clear(); job.PrivacyRevision = -1; job.PrivacyPublicContext = "";
            if (job.Kind == "generate" || IsPrivacyPipelineJob(job) || (job.Kind == "player_lifecycle" && job.ForcedIntent != "recover_published_lifecycle"))
            { job.Kind = "generate"; job.PrivacyDraft = ""; InvalidateDraftRepair(job); }
            job.RoundConversationRevision = destination.ConversationRevision;
            DiplomacyEventRules.Bind(destination, job);
        }
        _diplomacyWorkNeedsReconcile = true;
        InvalidateDialogueIndex();
    }

    private static bool IsProvisionalOpeningWork(WorldDiplomacyJob job) => IsEventPlanningJob(job)
        || (job.Kind is "event_boundary" or "event_boundary_audit" && job.ForcedIntent == "started");

    private void RecoverMisroutedPlayerOpenings()
    {
        // Load only; recognize the exact invalid ownership combination, not a
        // transient provider failure. Removing the task makes recovery idempotent.
        foreach (var round in GetLiveRounds().Where(IsPlayerDeclarationOpening).ToList())
        {
            if (!EventBoundaryComplete(round, "started")) continue;
            var obsolete = Storage.Jobs.Where(x => x.RoundId == round.RoundId && IsEventPlanningJob(x)
                && x.EventSelectionStage == "player" && !string.IsNullOrEmpty(x.DocumentId)
                && x.DocumentId != round.RootDocumentId
                && ResolveDocument(x.DocumentId) is { IsPlayerAuthored: true, IsReadyForPublication: true } doc
                && doc.RoundId == round.RoundId).ToList();
            if (obsolete.Count == 0) continue;
            bool ownedHold = obsolete.Any(x => !string.IsNullOrEmpty(x.SuspendedReason)
                && (x.SuspendedReason == round.TechnicalHoldReason || x.SuspendedReason == round.TechnicalRequestHoldReason));
            RequestLeases.InvalidateRound(round.RoundId);
            foreach (var job in obsolete) RemoveJob(job.JobId);
            if (ownedHold && !Storage.Jobs.Any(x => x.RoundId == round.RoundId && !string.IsNullOrEmpty(x.SuspendedReason))
                && !Storage.EventEffects.Any(x => x.EventId == round.RoundId && x.Status is "applying" or "uncertain")
                && round.TechnicalHoldReason is not "event_document_execution_uncertain" and not "legacy_unverified_work_preserved")
            {
                AccrueTechnicalPause(round);
                round.TechnicalHoldReason = ""; round.TechnicalRequestHoldReason = ""; round.TechnicalHoldStartedHour = -1;
            }
            foreach (var id in obsolete.Select(x => x.DocumentId).Distinct())
            {
                foreach (var duplicate in GetLiveRounds().Where(x => x != round && x.EventSourceType == "player_followup"
                    && x.RootDocumentId == id && x.PlayerResponses.All(r => r.SourceDocumentId == id)
                    && !Storage.Documents.Any(d => d.RoundId == x.RoundId)
                    && !Storage.EventAnnouncements.Any(a => a.EventId == x.RoundId)
                    && !Storage.EventEffects.Any(e => e.EventId == x.RoundId)
                    && !Storage.DialogueArrangements.Any(a => a.RoundId == x.RoundId)
                    && !Storage.AidRequests.Any(a => a.RoundId == x.RoundId)).ToList())
                {
                    RequestLeases.InvalidateRound(duplicate.RoundId);
                    duplicate.PlayerResponses.Clear(); duplicate.PlayerCourtWork.Clear(); duplicate.PlayerNegotiationWaits.Clear();
                    CloseActiveRound("player_declaration_attached_to_shared_event", duplicate);
                }
                foreach (var response in round.PlayerResponses.Where(x => x.SourceDocumentId == id
                    && x.Status == "transferred" && string.IsNullOrEmpty(x.AnswerDocumentId))) response.Status = "pending";
                RegisterPlayerResponseWork(ResolveDocument(id));
            }
            CommitPendingEventDocuments(round);
            round.ConversationRevision++;
            MarkEventDirty(round);
            _host.Log("diplomacy.retired_misrouted_opening event=" + round.RoundId + " count=" + obsolete.Count);
        }
    }

    private void NormalizePlayerLifecycleRecovery()
    {
        if (Storage.PlayerLifecycleSchemaVersion < 1 && string.IsNullOrEmpty(CurrentPlayerAuthority())) return;
        _pendingPlayerReplyRoutingIds.Clear();
        _playerReplyRoutingBacklog.Clear();
        foreach (var reply in Storage.Documents.Where(x => x?.PlayerReplyRoutingPending == true))
            if (_pendingPlayerReplyRoutingIds.Add(reply.DocumentId)) _playerReplyRoutingBacklog.Enqueue(reply.DocumentId);
        Storage.PlayerLifecycleRecoveryRoundIds ??= new();
        if (Storage.PlayerLifecycleSchemaVersion < 1)
        {
            // Load boundary only. Restrict restoration to this known automatic-close bug, with no expired transactional state.
            var candidates = Storage.CompletedRounds.Where(r => r != null && r.SchedulingOwner == "player" && r.NpcInitiatedPlayerEvent
                && r.CloseReason == "player_negotiation_finished" && r.PlayerAuthorityKingdomId == CurrentPlayerAuthority()
                && r.PlayerAuthorityRulerId == _host.PartyRulerId(CurrentPlayerAuthority()) && r.ExecutedActionCount == 0
                && !(r.PendingOffers?.Any() ?? false))
                .OrderBy(r => r.StartedDay).ThenBy(r => r.RoundId, StringComparer.Ordinal);
            var publishedByRound = Storage.Documents.Where(x => x != null && x.IsReadyForPublication)
                .GroupBy(x => x.RoundId).ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
            foreach (var round in candidates)
            {
                if (!publishedByRound.TryGetValue(round.RoundId, out var documents) || documents.Any(x => x.PlayerLifecycleClassified || x.RoundParticipation == "withdraw"
                    || x.RoundStatus is "resolved" or "deadlocked")) continue;
                if (!Storage.PlayerLifecycleRecoveryRoundIds.Contains(round.RoundId)) Storage.PlayerLifecycleRecoveryRoundIds.Add(round.RoundId);
            }
            Storage.PlayerLifecycleSchemaVersion = 1;
        }
        StartNextPlayerLifecycleRecovery();
    }

    private void StartNextPlayerLifecycleRecovery()
    {
        if (Storage.PlayerLifecycleRecoveryRoundIds?.Count is not > 0 || DiplomacyEventRules.HasNpcOpening(GetLiveRounds())
            || Storage.Jobs.Count(x => x.SchedulingOwner == "player") >= _host.MaxPendingJobs()) return;
        string id = Storage.PlayerLifecycleRecoveryRoundIds[0];
        var round = Storage.CompletedRounds.FirstOrDefault(x => x.RoundId == id);
        var document = ResolveDocument(round?.RootDocumentId);
        if (round == null || document?.IsReadyForPublication != true || CurrentPlayerAuthority() != round.PlayerAuthorityKingdomId
            || _host.PartyRulerId(CurrentPlayerAuthority()) != round.PlayerAuthorityRulerId)
        { Storage.PlayerLifecycleRecoveryRoundIds.RemoveAt(0); return; }
        Storage.CompletedRounds.Remove(round); Storage.ConcurrentRounds.Add(round);
        // The known erroneous internal archive summary is regeneratable, unlike the original published documents/facts.
        Storage.RoundSummaries.RemoveAll(x => x.RoundId == round.RoundId);
        round.State = "active"; round.RoundStatus = "continue"; round.CloseReason = ""; round.CompletedDay = 0;
        round.OwnershipEpoch++; round.ConversationRevision++; round.TechnicalHoldReason = "player_lifecycle_recovery_pending";
        Storage.PlayerLifecycleRecoveryRoundIds.RemoveAt(0);
        InvalidateDialogueIndex();
        var job = new WorldDiplomacyJob { JobId = _host.NewId("player_lifecycle_recovery"), Kind = "player_lifecycle",
            RoundId = round.RoundId, ExchangeId = round.RoundId, DocumentId = document.DocumentId, AuthorKingdomId = document.AuthorKingdomId,
            TargetKingdomId = CurrentPlayerAuthority(), ForcedIntent = "recover_published_lifecycle", Priority = 100, CreatedDay = _host.CurrentDay(),
            PrivacyDraft = JsonConvert.SerializeObject(new { document.DocumentId, document.Title, document.Body, document.Actions }) };
        EnqueueJob(job); PreparePlayerLifecycleRequest(job);
        _host.Log("player.lifecycle_recovery_started round=" + id + " document=" + document.DocumentId);
    }
}
