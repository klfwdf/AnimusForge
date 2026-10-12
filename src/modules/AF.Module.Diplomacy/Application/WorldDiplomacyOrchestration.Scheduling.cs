using AnimusForge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

using AnimusForge.Refactor.Domain;
using static AnimusForge.Refactor.Domain.WorldDiplomacyIntentVocabulary;
using static AnimusForge.Refactor.Domain.WorldDiplomacyTextRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyRoundLifecycleRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyStructureRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyEnvelopeJsonRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyDocumentFactRules;
using static AnimusForge.Refactor.Domain.WorldDiplomacyPromptContractRules;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    private const int MaxConcurrentDiplomacyRequests = 3;
    private long _nextDiplomacyDispatchUtcTicks;
    private int _lastPlayerPendingNoticeDay = -1;
    private bool _diplomacyWorkNeedsReconcile = true;
    private int _diplomacyDispatchDay = -1;

    private bool HasOrdinaryRoundForInitiator(string kingdomId) => GetLiveRounds().Any(round =>
        IsLiveRound(round) && string.Equals(round.InitiatorKingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)
        && !round.IsPlayerInsertion
        && !string.Equals(round.EventSourceType, "dialogue_commitment", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(round.EventSourceType, "player_followup", StringComparison.OrdinalIgnoreCase));

    internal bool IsPlayerDiplomacyContext(string author, string target, WorldDiplomacyRound round, WorldDiplomacyDocument source = null) =>
        _host.IsPlayerAffiliatedParty(author) || _host.IsPlayerAffiliatedParty(target)
        || WorldDiplomacyPlayerApplication.InvolvesPlayer(source, round, _host.IsPlayerAffiliatedParty);
    internal bool IsPlayerDiplomacyDocument(WorldDiplomacyDocument document) =>
        WorldDiplomacyPlayerApplication.InvolvesPlayer(document, ResolveRound(document?.RoundId), _host.IsPlayerAffiliatedParty);
    internal bool IsPlayerGeneratedEnvelope(WorldDiplomacyJob job, Newtonsoft.Json.Linq.JObject json) =>
        IsPlayerDiplomacyJob(job) || (json?["actions"] is Newtonsoft.Json.Linq.JArray actions
            && actions.OfType<Newtonsoft.Json.Linq.JObject>().Any(x => _host.IsPlayerAffiliatedParty(
                WorldDiplomacyEnvelopeJsonRules.ReadString(x, "target_kingdom_id", "primary_target_kingdom_id", "target"))));
    internal bool IsPlayerDiplomacyJob(WorldDiplomacyJob job) => job != null &&
        (job.PlayerResponseSourceIds?.Count > 0 || _host.IsPlayerAffiliatedParty(job.AuthorKingdomId)
         || _host.IsPlayerAffiliatedParty(job.TargetKingdomId)
         || IsPlayerDiplomacyDocument(ResolveDocument(job.DocumentId))
         || IsPlayerDiplomacyDocument(ResolveDocument(job.SourceDocumentId))
         || WorldDiplomacyPlayerApplication.InvolvesPlayer(null, ResolveRound(job.RoundId), _host.IsPlayerAffiliatedParty));
    private bool IsDirectPlayerSchedulingJob(WorldDiplomacyJob job) => job != null &&
        (IsOralDeclarationJob(job) || IsPlayerDiplomacyJob(job)
         || ((job.Kind == "analyze" || job.Kind == "round_plan") && ResolveDocument(job.DocumentId)?.IsPlayerAuthored == true)
         || (job.Kind == "generate" && (ResolveRound(job.RoundId)?.PlayerResponses?.Any(x => x != null
             && x.Status == "pending" && x.KingdomId == job.AuthorKingdomId
             && HasCourtDocumentKnowledge(job.AuthorKingdomId, x.SourceDocumentId)) == true)));
    private bool IsPlayerSchedulingJob(WorldDiplomacyJob job) => IsDirectPlayerSchedulingJob(job)
        || (job?.Kind == "compress" && Storage.Jobs.Any(x => x != null && x.AwaitingHistoryCompression && IsDirectPlayerSchedulingJob(x)));







    private bool CanPlayerDocumentJoinRound(WorldDiplomacyDocument document, WorldDiplomacyRound round)
    {
        if (!IsLiveRound(round)) return false;
        if (!round.ResultSettlementPending) return true;
        var newTargets = GetDocumentTargetIds(document).Where(id =>
            !WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return (round.RelayRouteKingdomIds?.Count ?? 0) + newTargets.Count <= _host.MaxRelayParticipants()
            && newTargets.All(id => _host.PartyResolved(id) && _host.HasIndependentAuthority(id));
    }

    // Submission/analysis/load/close boundaries only; no archive work on ticks.
    internal WorldDiplomacyRound EnsurePlayerDocumentRound(WorldDiplomacyDocument document)
    {
        if (document?.IsPlayerAuthored != true) return ResolveRound(document?.RoundId);
        var previous = ResolveRound(document.RoundId);
        if (CanPlayerDocumentJoinRound(document, previous)) return previous;
        return MovePlayerDocumentToIndependentRound(document, previous);
    }

    private WorldDiplomacyRound MovePlayerDocumentToIndependentRound(WorldDiplomacyDocument document, WorldDiplomacyRound previous)
    {
        var source = ResolveDocument(document.SourceDocumentId);
        string target = FirstNonEmpty(document.TargetKingdomId, source?.AuthorKingdomId);
        var round = CreateIndependentDialogueRound(document.AuthorKingdomId, target, "player_manual_declaration");
        round.RootDocumentId = document.DocumentId;
        round.RoundTopic = FirstNonEmpty(document.Title, DiplomacyMiscPromptTexts.Get("Document_DefaultReplyTitle", "Diplomatic Reply"));
        round.ExternalOpeningContext = string.Format(DiplomacyMiscPromptTexts.Get("Scheduling_PlayerReplyIndependentFormat", "Player statement established independently; original round={0}; context document={1}. Context connection does not reopen closed rounds or expired proposals; any acceptance still requires validating current validity of original terms."), previous?.RoundId ?? "", source?.DocumentId ?? document.SourceDocumentId ?? "");
        MovePlayerDocumentRouting(document, round);
        document.ResultSettlementSlotId = "";
        document.IsRelayTurn = false;
        document.IsExternalResponseOnly = false;
        document.RoundAccountingHandled = false;
        document.RoundProgressHandled = false;
        _diplomacyWorkNeedsReconcile = true;
        _host.Log("player declaration independent round document=" + document.DocumentId
            + " previous=" + (previous?.RoundId ?? "") + " round=" + round.RoundId);
        return round;
    }

    private void MovePlayerDocumentRouting(WorldDiplomacyDocument document, WorldDiplomacyRound destination)
    {
        document.RoundId = document.ExchangeId = destination.RoundId;
        foreach (var job in Storage.Jobs.Where(x => x != null && x.Kind == "analyze" && x.DocumentId == document.DocumentId))
            job.RoundId = job.ExchangeId = destination.RoundId;
        foreach (var arrival in Storage.PropagationArrivals.Where(x => x != null && x.DocumentId == document.DocumentId))
            arrival.RoundId = destination.RoundId;
        WorldDiplomacyRequestHistoryApplication.InvalidateDocumentRouting(Storage);
        InvalidateDialogueIndex();
    }

    private void PreservePendingPlayerAnalysisForClosingRound(WorldDiplomacyRound round, string reason)
    {
        foreach (var document in Storage.Documents.Where(x => x?.IsPlayerAuthored == true && x.RoundId == round.RoundId
            && x.IsReadyForPublication && x.AnalysisStatus == "pending_analysis" && !x.PlayerAnalysisCommitted).ToList())
        {
            if (reason == "closed_disabled")
            {
                WorldDiplomacyAnalysisApplication.MarkPlayerAnalysisFailed(document, _host.Log);
                continue;
            }
            MovePlayerDocumentToIndependentRound(document, round);
        }
    }

    private void BindPlayerDeclarationToSharedEvent(WorldDiplomacyDocument document)
    {
        var provisional = ResolveRound(document.RoundId);
        if (provisional == null || provisional.DialogueArrangementId != "player_manual_declaration") return;
        // Shared subject/parties do not turn a new player action or counterproposal
        // into an existing AI turn with response/capacity restrictions.
        string playerIntent = NormalizeIntent(document.Intent);
        bool explicitDiscussion = playerIntent == "statement"
            && !string.IsNullOrWhiteSpace(document.DiscussionSourceDocumentId)
            && (document.Actions == null || document.Actions.All(x => x == null || NormalizeIntent(x.Intent) == "statement"));
        if (explicitDiscussion)
        {
            var discussionSource = ResolveDocument(document.DiscussionSourceDocumentId);
            if (discussionSource?.IsReadyForPublication != true
                || !DialogueDocumentKnown(document.AuthorKingdomId, discussionSource.DocumentId)) return;
        }
        if (string.IsNullOrEmpty(ResponseIntentToProposalIntent(playerIntent))
            && playerIntent != "comply_ultimatum" && playerIntent != "withdraw_offer" && !explicitDiscussion) return;
        // Mechanical response sources outrank advisory discussion metadata.
        bool offerResponse = !string.IsNullOrEmpty(ResponseIntentToProposalIntent(playerIntent));
        var mechanicalSources = (offerResponse
                ? new[] { document.RespondingToOfferDocumentId }.Concat(
                    (document.Actions ?? new List<WorldDiplomacyDocumentAction>()).Where(x => x != null)
                        .Select(x => x.RespondingToOfferDocumentId))
                : playerIntent == "comply_ultimatum"
                    ? new[] { document.RespondingToThreatDocumentId }.Concat(
                        (document.Actions ?? new List<WorldDiplomacyDocumentAction>()).Where(x => x != null)
                            .Select(x => x.RespondingToThreatDocumentId))
                    : Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        bool hasMechanicalSource = mechanicalSources.Count > 0;
        var explicitSources = hasMechanicalSource ? mechanicalSources : new[] { document.RespondingToOfferDocumentId, document.RespondingToThreatDocumentId, document.SourceDocumentId, document.DiscussionSourceDocumentId }
            .Concat((document.Actions ?? new List<WorldDiplomacyDocumentAction>()).Where(x => x != null).SelectMany(x =>
                new[] { x.RespondingToOfferDocumentId, x.RespondingToThreatDocumentId }))
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var sources = explicitSources.Select(ResolveDocument).Where(x => x != null)
            .Select(x => ResolveRound(x.RoundId)).Where(x => IsLiveRound(x) && !ReferenceEquals(x, provisional))
            .Distinct().Take(2).ToList();
        if (!hasMechanicalSource && !string.IsNullOrWhiteSpace(document.DiscussionRoundId))
            sources = sources.Where(x => x.RoundId == document.DiscussionRoundId).ToList();
        if (sources.Count == 0 && explicitSources.Count == 0)
        {
            // Match a unique existing public subject AND parties. Neither same
            // country alone nor most-recent-event ordering selects a proposal.
            string intent = NormalizeIntent(document.Intent);
            string subject = intent.Contains("peace") ? "peace" : intent.Contains("trade") ? "trade"
                : intent.Contains("alliance") ? "alliance" : intent.Contains("war") ? "war" : "";
            var targets = GetDocumentTargetIds(document);
            if (subject.Length > 0 && targets.Count > 0)
                sources = GetLiveRounds().Where(x => !ReferenceEquals(x, provisional)
                    && targets.Any(id => RoundContainsKingdom(x, id))
                    && (Storage.Documents.Any(d => d != null && d.IsReadyForPublication
                        && d.RoundId == x.RoundId && NormalizeIntent(d.Intent).Contains(subject))))
                    .Take(2).ToList();
        }
        if (sources.Count != 1) return; // Independent player speech is always retained.
        var destination = sources[0];
        if (!CanPlayerDocumentJoinRound(document, destination)) return;
        MovePlayerDocumentRouting(document, destination);
        destination.ConversationRevision++;
        EnsureRoundParticipant(destination, document.AuthorKingdomId, "active", false);
        IntegratePlayerDeclaration(destination, document);
        CloseActiveRound("player_declaration_attached_to_shared_event", provisional);
        InvalidateDialogueIndex();
    }

    private void RegisterPlayerResponseWork(WorldDiplomacyDocument document, bool schedule = true)
    {
        if (document?.IsPlayerAuthored != true || document.AnalysisStatus == "pending_analysis") return;
        _diplomacyWorkNeedsReconcile = true;
        BindPlayerDeclarationToSharedEvent(document);
        var round = ResolveRound(document.RoundId);
        if (!IsLiveRound(round)) return;
        round.PlayerResponses ??= new List<WorldDiplomacyPlayerResponse>();
        var receivers = GetDocumentTargetIds(document)
            .Concat(document.AddressedKingdomIds ?? Enumerable.Empty<string>()).Select(_host.ResolvePartyId)
            .Select(_host.ResolveRepresentativeId).Where(x => x != null && !_host.IsPlayerParty(x)
                && !_host.IsEliminatedParty(x)).Distinct().ToList();
        if (receivers.Count == 0)
        {
            // Existing event participants first; deterministic rotation breaks
            // ties for a wholly independent, untargeted declaration.
            receivers = _host.AllKingdomIds().Where(id => !_host.IsEliminatedParty(id) && !_host.IsPlayerAffiliatedParty(id)).OrderByDescending(x => RoundContainsKingdom(round, x))
                .ThenByDescending(x => document.MentionedKingdomIds?.Contains(x, StringComparer.OrdinalIgnoreCase) == true)
                .ThenByDescending(x => _host.PartiesAtWar(document.AuthorKingdomId, x))
                .ThenBy(x => x, StringComparer.OrdinalIgnoreCase).Take(1).ToList();
        }
        bool added = false;
        foreach (var receiver in receivers)
        {
            if (round.PlayerResponses.Any(x => x.SourceDocumentId == document.DocumentId && x.KingdomId == receiver)) continue;
            round.PlayerResponses.Add(new WorldDiplomacyPlayerResponse { SourceDocumentId = document.DocumentId,
                KingdomId = receiver, OriginalRoundId = round.RoundId, CreatedDay = document.Day });
            added = true;
            EnsureRoundParticipant(round, receiver, "active", true);
            // The old selection limit still applies to optional observers;
            // explicitly owed speakers and the player must fit this negotiation.
            foreach (string id in new[] { receiver, document.AuthorKingdomId })
            {
                if (round.RelayPlanned && !round.RelayRouteKingdomIds.Contains(id)) round.RelayRouteKingdomIds.Add(id);
                var participant = EnsureRoundParticipant(round, id, "active", id == receiver);
                participant.SelectedForRelay = round.RelayPlanned;
                participant.IsPlayerAsync = id == document.AuthorKingdomId;
            }
        }
        if (added) round.ConversationRevision++;
        if (schedule) SchedulePlayerResponseWork(round);
    }

    private string PlayerResponseUnavailableReason(string receiverId, string sourceId)
    {
        var source = ResolveDocument(sourceId);
        if (source?.IsPlayerAuthored != true || !source.IsReadyForPublication) return "source_unavailable";
        // Resolve the original parties only. A changed suzerain never inherits this declaration.
        string author = _host.ResolvePartyId(source.AuthorKingdomId);
        if (author == null || _host.IsEliminatedParty(author))
            return "source_author_unavailable";
        string receiver = _host.ResolvePartyId(receiverId);
        if (receiver == null || _host.IsEliminatedParty(receiver))
            return "receiver_unavailable";
        return "";
    }

    private void ValidatePlayerResponseObligations(WorldDiplomacyRound round)
    {
        string firstReason = "";
        foreach (var item in round?.PlayerResponses ?? Enumerable.Empty<WorldDiplomacyPlayerResponse>())
        {
            if (item == null || item.Status != "pending" || !string.IsNullOrWhiteSpace(item.AnswerDocumentId)) continue;
            string reason = PlayerResponseUnavailableReason(item.KingdomId, item.SourceDocumentId);
            if (string.IsNullOrEmpty(reason)) continue;
            item.Status = "unavailable"; item.FailureReason = reason;
            if (firstReason.Length == 0) firstReason = reason;
        }
        if (firstReason.Length > 0)
        {
            _diplomacyWorkNeedsReconcile = true;
            _host.Notify(firstReason == "source_author_unavailable"
                ? DiplomacyUiLocalization.Text("AF_DIP_UI_Orch_SourceAuthorUnavailable", "The kingdom of your original declaration no longer exists; unanswered diplomatic responses have stopped. Prior commitments are not transferred to the suzerain.")
                : DiplomacyUiLocalization.Text("AF_DIP_UI_Orch_SourceOrTargetUnavailable", "The original declaration or responding kingdom no longer exists; incomplete responses have stopped. Check the original document and current situation."));
        }
    }

    private bool CanDispatchPlayerResponse(WorldDiplomacyJob job)
    {
        var round = ResolveRound(job?.RoundId);
        bool followup = string.Equals(round?.EventSourceType, "player_followup", StringComparison.OrdinalIgnoreCase);
        if (followup && (job.Kind == "round_plan" || (job.Kind == "generate" && !job.IsExternalResponseOnly))) return false;
        if (job?.Kind != "generate") return true;
        var sourceIds = (job.PlayerResponseSourceIds ?? new List<string>()).ToList();
        if (job.IsExternalResponseOnly && ResolveDocument(job.SourceDocumentId)?.IsPlayerAuthored == true)
            sourceIds.Add(job.SourceDocumentId);
        if (sourceIds.Count == 0) return true;
        ValidatePlayerResponseObligations(round);
        foreach (string id in sourceIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (PlayerResponseUnavailableReason(job.AuthorKingdomId, id).Length > 0) return false;
            var obligation = round?.PlayerResponses?.FirstOrDefault(x => x != null && x.KingdomId == job.AuthorKingdomId && x.SourceDocumentId == id);
            if (obligation != null && (obligation.Status != "pending" || obligation.RetryNotBeforeDay > _host.CurrentDay())) return false;
        }
        return true;
    }

    private void DeferUnpublishedPlayerResponses(WorldDiplomacyJob job)
    {
        if (job?.IsExternalResponseOnly != true) return;
        var round = ResolveRound(job.RoundId);
        ValidatePlayerResponseObligations(round);
        int tomorrow = _host.CurrentDay() + 1;
        foreach (var item in round?.PlayerResponses ?? Enumerable.Empty<WorldDiplomacyPlayerResponse>())
        {
            if (item != null && item.Status == "pending" && item.KingdomId == job.AuthorKingdomId
                && ((job.PlayerResponseSourceIds?.Contains(item.SourceDocumentId) ?? false) || item.SourceDocumentId == job.SourceDocumentId))
            { item.RetryNotBeforeDay = Math.Max(item.RetryNotBeforeDay, tomorrow); item.FailureReason = "unpublished_response"; }
        }
        _diplomacyWorkNeedsReconcile = true;
    }

    private bool MaintainPlayerFollowup(WorldDiplomacyRound round)
    {
        if (!string.Equals(round?.EventSourceType, "player_followup", StringComparison.OrdinalIgnoreCase)) return false;
        ValidatePlayerResponseObligations(round);
        bool pending = round.PlayerResponses?.Any(x => x != null && x.Status == "pending" && string.IsNullOrWhiteSpace(x.AnswerDocumentId)) == true;
        if (!pending) { CloseRound("player_responses_finished", round); return true; }
        // These obligations do not own a relay circuit. Withdrawn AI participants
        // cannot close and re-create the same request; only the existing deadline ends it.
        if (!IsHardEndReached(_host.CurrentDay(), round.HardEndDay))
        { SchedulePlayerResponseWork(round); return true; }
        if (Storage.Jobs.Any(x => x != null && x.RoundId == round.RoundId && x.IsRunning)) return true;
        CloseRound("player_response_deadline", round);
        return true;
    }

    private void SchedulePlayerResponseWork(WorldDiplomacyRound round)
    {
        if (!IsLiveRound(round) || round.PlayerResponses == null) return;
        ValidatePlayerResponseObligations(round);
        if (string.Equals(round.EventSourceType, "player_followup", StringComparison.OrdinalIgnoreCase))
            foreach (var obsolete in Storage.Jobs.Where(x => x != null && !x.IsRunning && x.RoundId == round.RoundId
                && (x.Kind == "round_plan" || (x.Kind == "generate" && !x.IsExternalResponseOnly))).ToList()) RemoveJob(obsolete.JobId);
        // Selection predicates must not modify the job list while it is being iterated.
        // Retire now on the owning reconciliation path, outside dispatch selection.
        foreach (var job in Storage.Jobs.Where(x => x != null && !x.IsRunning && x.IsExternalResponseOnly && x.RoundId == round.RoundId).ToList())
            if (!CanDispatchPlayerResponse(job)) RemoveJob(job.JobId);
        foreach (var group in round.PlayerResponses.Where(x => x != null && string.IsNullOrWhiteSpace(x.AnswerDocumentId)
            && x.Status == "pending" && x.RetryNotBeforeDay <= _host.CurrentDay()).GroupBy(x => x.KingdomId).OrderBy(x => x.Min(y => y.CreatedDay)))
        {
            string receiver = ResolveDialogueParty(group.Key);
            if (receiver == null || _host.IsEliminatedParty(receiver))
            {
                foreach (var item in group) item.Status = "unavailable";
                _host.Notify(DiplomacyUiLocalization.Text("AF_DIP_UI_Orch_TargetUnavailable", "The original responding kingdom no longer exists; that kingdom's response cannot continue."));
                continue;
            }
            var received = group.Where(x => DialogueDocumentKnown(receiver, x.SourceDocumentId)).ToList();
            var source = received.Select(x => ResolveDocument(x.SourceDocumentId)).FirstOrDefault(x => x != null);
            if (source == null) continue;
            var candidates = Storage.Jobs.Where(x => x != null && x.Kind == "generate"
                && x.RoundId == round.RoundId && x.AuthorKingdomId == group.Key)
                .OrderByDescending(x => x.IsRunning).ThenByDescending(x => x.Priority).ToList();
            var existing = candidates.FirstOrDefault();
            if (existing != null)
            {
                foreach (var duplicate in candidates.Skip(1).Where(x => !x.IsRunning)) RemoveJob(duplicate.JobId);
                if (!existing.IsRunning)
                {
                    DiplomacyRoundWorkRules.MergeQueuedSpeaker(existing, received.Select(x => x.SourceDocumentId));
                }
                continue; // Rebuilt from all pending sources at actual send.
            }
            if (round.ResultSettlementPending)
            {
                foreach (var item in received) WorldDiplomacyResultSlotApplication.AddOrMergeResultSettlementSlot(round, group.Key, "player_response",
                    item.SourceDocumentId, source.AuthorKingdomId, true, TryIncludeResultSettlementTarget, _host.NewId);
                // A pending old slot/job must finish before the merged slot.
                ScheduleNextResultSettlementTurn(round);
                continue;
            }
            if (Storage.Jobs.Count >= _host.MaxPendingJobs()) return; // Obligation persists; no eviction.
            // The existing priority reply completes in this event, without
            // cancelling a scheduled hop or moving the ordinary relay cursor.
            EnqueueGeneration(receiver, ResolveDialogueParty(source.AuthorKingdomId), null, true, source,
                95, externalResponseOnly: true, roundId: round.RoundId, isRelayTurn: round.RelayPlanned,
                previousKingdomId: source.AuthorKingdomId, scheduledDay: _host.CurrentDay());
        }
    }

    private void PrepareSharedEventRequest(WorldDiplomacyJob job)
    {
        if (job.Kind != "generate") return;
        var round = ResolveRound(job.RoundId);
        if (!IsLiveRound(round)) return;
        if (job.IsRelayTurn && !job.IsExternalResponseOnly && !round.ResultSettlementPending)
        {
            round.RelayWaiting = true;
            int index = round.RelayRouteKingdomIds.FindIndex(x => x == job.AuthorKingdomId);
            if (index >= 0) round.RelayCursor = index;
        }
        // A repair retains its frozen chain only while its event is unchanged.
        if (job.SemanticRepairAttempts > 0 && job.RoundConversationRevision == round.ConversationRevision) return;
        job.LlmMessages?.Clear();
        job.SemanticRepairAttempts = 0;
        if (!TryRebuildPendingJob(job)) return;
        job.RoundConversationRevision = round.ConversationRevision;
        // Build the bounded knowledge set once per request, then walk the same
        // document window once; never resolve each ID by rescanning the archive.
        var knownIds = CollectKnownDocumentIds(null, Storage.NobleKnowledge, Storage.KingdomKnowledge,
            null, job.AuthorKingdomId, true, true);
        var knownDocuments = Storage.Documents.Where(x => x != null && x.IsReadyForPublication
            && (x.AuthorKingdomId == job.AuthorKingdomId || knownIds.Contains(x.DocumentId))).ToList();
        job.PlayerResponseSourceIds = DiplomacyRoundWorkRules.SelectResponseBatch(round, job.AuthorKingdomId, knownDocuments, _host.CurrentDay());
        string tail = DiplomacyRoundWorkRules.BuildRequestTail(round, knownDocuments, job.PlayerResponseSourceIds, _host.PromptWorld().LocalizePrompt);
        int modeMarker = job.UserPrompt.LastIndexOf("【MODE=DECLARE】", StringComparison.Ordinal);
        string basePrompt = modeMarker >= 0 ? job.UserPrompt.Substring(0, modeMarker) : job.UserPrompt;
        job.UserPrompt = BuildDeclareModePrompt(basePrompt + tail, _host.PromptWorld().LocalizePrompt);
        if (job.PlayerResponseSourceIds.Count > 0) job.Priority = Math.Max(job.Priority, 95);
    }

    private string BuildPlayerRoundRoutingContext(WorldDiplomacyDocument document)
    {
        if (document?.IsPlayerAuthored != true) return "";
        var sb = new StringBuilder(DiplomacyMiscPromptTexts.Get("Scheduling_HeaderPlayerAttribution", "\n[Player Statement Attribution to Public Negotiations]\n"));
        foreach (var round in GetLiveRounds().Where(x => x.RoundId != document.RoundId).Take(32))
        {
            var root = ResolveDocument(round.RootDocumentId);
            if (root?.IsReadyForPublication != true || !DialogueDocumentKnown(document.AuthorKingdomId, root.DocumentId)) continue;
            sb.AppendLine(string.Format(DiplomacyMiscPromptTexts.Get("Scheduling_RoundListingFormat", "Round={0}; public source={1}; topic={2}; involved kingdoms={3}\n{4}"), round.RoundId, root.DocumentId, round.RoundTopic, string.Join(",", round.Participants.Select(x => x.KingdomId)), Limit(root.Body, 700)));

        }
        sb.AppendLine(DiplomacyMiscPromptTexts.Get("Scheduling_InstructionAttributionRules", "If the player provides guarantees, modifies terms, comments on the matter under discussion, or responds in one of the negotiations above, add related_round_id and related_public_document_id in JSON with the unique matching event and source IDs; do not associate merely because kingdom is same or case is newer. Leave both fields empty if cannot uniquely determine; player declaration remains public in an independent round. This association only determines discussion attribution and does not substitute for acceptance terms, source verification, or action determinations."));
        return sb.ToString();
    }

    private void RecoverRoundSchedulingAfterLoad()
    {
        if (Storage.RoundSchedulingSchemaVersion >= 1) return;
        foreach (var document in Storage.Documents.Where(x => x != null && x.IsPlayerAuthored
            && x.IsReadyForPublication && x.AnalysisStatus != "pending_analysis"
            && IsLiveRound(ResolveRound(x.RoundId))).ToList())
        {
            RegisterPlayerResponseWork(document, schedule: false);
            var round = ResolveRound(document.RoundId);
            foreach (var receipt in round?.PlayerResponses ?? Enumerable.Empty<WorldDiplomacyPlayerResponse>())
            {
                var reply = Storage.Documents.FirstOrDefault(x => x != null && x.IsReadyForPublication && !x.IsPlayerAuthored
                    && x.AuthorKingdomId == receipt.KingdomId && DocumentRespondsTo(x, receipt.SourceDocumentId));
                if (reply != null) { receipt.Status = "answered"; receipt.AnswerDocumentId = reply.DocumentId; }
            }
        }
        foreach (var round in GetLiveRounds().ToList()) SchedulePlayerResponseWork(round);
        Storage.RoundSchedulingSchemaVersion = 1;
    }

    private bool ValidatePlayerResponseCoverage(WorldDiplomacyJob job, Newtonsoft.Json.Linq.JObject json)
    {
        if (job.PlayerResponseSourceIds?.Count == 0) return true;
        var covered = ReadStringList(json, "answered_player_document_ids");
        return job.PlayerResponseSourceIds.All(id => covered.Contains(id, StringComparer.OrdinalIgnoreCase));
    }

    private void CommitPlayerResponseCoverage(WorldDiplomacyJob job, WorldDiplomacyDocument document)
    {
        var round = ResolveRound(job.RoundId);
        document.AnsweredPlayerDocumentIds = new List<string>(job.PlayerResponseSourceIds ?? new List<string>());
        DiplomacyRoundWorkRules.SealCoverage(round, document);
    }

    private void CarryUnansweredPlayerResponses(WorldDiplomacyRound closed)
    {
        if (closed.CloseReason == "closed_disabled") return;
        ValidatePlayerResponseObligations(closed);
        if (string.Equals(closed.EventSourceType, "player_followup", StringComparison.OrdinalIgnoreCase))
        {
            bool unresolved = false;
            foreach (var item in closed.PlayerResponses ?? Enumerable.Empty<WorldDiplomacyPlayerResponse>())
                if (item != null && item.Status == "pending" && string.IsNullOrWhiteSpace(item.AnswerDocumentId))
                { item.Status = "unanswered"; item.FailureReason = closed.CloseReason ?? "closed"; unresolved = true; }
            if (unresolved) _host.Notify(DiplomacyUiLocalization.Text("AF_DIP_UI_Orch_ResponsesEndedUnresolved", "Follow-up diplomatic responses have concluded, but some kingdoms could not reply; original declarations and incomplete records are preserved and new declarations may be issued without restoring prior proposals."));
            return;
        }
        var pending = (closed.PlayerResponses ?? new List<WorldDiplomacyPlayerResponse>()).Where(x => x != null
            && x.Status == "pending" && string.IsNullOrWhiteSpace(x.AnswerDocumentId)).ToList();
        if (pending.Count == 0 || closed.CloseReason == "closed_disabled") return;
        var followup = new WorldDiplomacyRound { SchemaVersion = _host.RelaySchemaVersion(), RoundId = _host.NewId("diplomacy_followup"),
            EventSourceType = "player_followup", IsPlayerInsertion = true, InitiatorKingdomId = closed.InitiatorKingdomId,
            State = "active", StartedDay = _host.CurrentDay(), LastActivityDay = _host.CurrentDay(),
            SoftEndDay = _host.CurrentDay() + _host.RoundTargetDurationDays(), HardEndDay = _host.CurrentDay() + _host.RoundHardDurationDays(_host.RoundTargetDurationDays()),
            RelayPassDurationDays = _host.CourtMaxDeliveryDays(),
            RoundTopic = DiplomacyMiscPromptTexts.Get("Scheduling_FollowupReplyTitle", "Follow-up reply to player declaration in original negotiation"), RelayPlanned = true, RootDocumentId = pending[0].SourceDocumentId,
            ExternalOpeningContext = string.Format(DiplomacyMiscPromptTexts.Get("Scheduling_OriginalEventClosedFormat", "Original round={0} ended under original rules; this only responds to unanswered public declarations. Legacy proposals are invalid and cannot be restored or directly accepted."), closed.RoundId) };
        foreach (var item in pending)
        {
            item.Status = "transferred";
            followup.PlayerResponses.Add(new WorldDiplomacyPlayerResponse { SourceDocumentId = item.SourceDocumentId,
                KingdomId = item.KingdomId, CreatedDay = item.CreatedDay, OriginalRoundId = item.OriginalRoundId,
                RetryNotBeforeDay = item.RetryNotBeforeDay, FailureReason = item.FailureReason });
            if (!followup.RelayRouteKingdomIds.Contains(item.KingdomId)) followup.RelayRouteKingdomIds.Add(item.KingdomId);
            EnsureRoundParticipant(followup, item.KingdomId, "active", true);
        }
        var player = ResolveDocument(pending[0].SourceDocumentId)?.AuthorKingdomId;
        if (!string.IsNullOrWhiteSpace(player))
        { followup.RelayRouteKingdomIds.Add(player); EnsureRoundParticipant(followup, player, "active", false).IsPlayerAsync = true; }
        Storage.ConcurrentRounds.Add(followup);
        _diplomacyWorkNeedsReconcile = true;
        InvalidateDialogueIndex();
        _host.Notify(DiplomacyUiLocalization.Text("AF_DIP_UI_Orch_RoundTransitionFollowup", "The prior diplomatic exchange has ended; declarations that have not yet replied to you have been transferred to priority follow-up."));
    }
}
