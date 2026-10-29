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
    private int _playerMaintenanceHour = -1;
    private int _nextRequestLane;
    private long _requestTurn;
    private readonly Dictionary<string, long> _lastCaseDispatch = new(StringComparer.Ordinal);
    private int _playerRoundCursor;
    private int _playerAuthorityDay = -1;
    private string _observedAuthorityKingdom = "", _observedAuthorityRuler = "";
    private readonly Queue<string> _playerDirtyRounds = new();
    private readonly HashSet<string> _playerDirtyRoundIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<string> _playerAnalysisBacklog = new();
    private readonly HashSet<string> _pendingPlayerAnalysisIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<WorldDiplomacyPlayerDisputeRefusal>> _refusalsByKingdom = new(StringComparer.OrdinalIgnoreCase);
    private readonly SortedDictionary<double, Queue<KeyValuePair<string, string>>> _silenceDeadlines = new();
    private readonly HashSet<string> _silenceTicketIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, KeyValuePair<WorldDiplomacyRound, WorldDiplomacyPlayerWait>> _waitingByDocument = new(StringComparer.OrdinalIgnoreCase);

    private void IndexPlayerWait(WorldDiplomacyRound round, WorldDiplomacyPlayerWait wait)
    {
        if (ResolveDocument(wait.SourceDocumentId)?.LegacyNoticeSuppressed == true)
            wait.RequiresAnswer = false;
        _waitingByDocument[wait.SourceDocumentId] = new(round, wait);
        if (wait.NotifiedHour >= 0 && wait.RequiresAnswer && string.IsNullOrEmpty(wait.AnswerDocumentId))
            EnqueueSilenceDeadline(round.RoundId, wait.SourceDocumentId, wait.NotifiedHour + 72);
    }
    private void EnqueueSilenceDeadline(string roundId, string sourceId, double due)
    {
        if (!_silenceTicketIds.Add(sourceId)) return;
        if (!_silenceDeadlines.TryGetValue(due, out var tickets)) _silenceDeadlines[due] = tickets = new();
        tickets.Enqueue(new(roundId, sourceId));
    }
    internal void RecordActualPlayerNotice(WorldDiplomacyDocument document, double hour)
    {
        if (document == null || !_waitingByDocument.TryGetValue(document.DocumentId, out var entry)) return;
        if (entry.Value.NotifiedHour < 0) entry.Value.NotifiedHour = hour;
        IndexPlayerWait(entry.Key, entry.Value);
    }
    private void ProcessDuePlayerSilence()
    {
        if (_silenceDeadlines.Count == 0) return;
        double now = _host.CurrentCampaignHours();
        for (int i = 0; i < 4 && _silenceDeadlines.Count > 0; i++)
        {
            KeyValuePair<double, Queue<KeyValuePair<string, string>>> first;
            using (var iterator = _silenceDeadlines.GetEnumerator()) { iterator.MoveNext(); first = iterator.Current; }
            if (first.Key > now) return;
            var ticket = first.Value.Dequeue();
            if (first.Value.Count == 0) _silenceDeadlines.Remove(first.Key);
            _silenceTicketIds.Remove(ticket.Value);
            var round = ResolveRound(ticket.Key);
            if (!IsLiveRound(round) || !DiplomacyEventRules.IsEvent(round)
                || !_waitingByDocument.TryGetValue(ticket.Value, out var entry)) continue;
            if (!DiplomacyEventRules.SilenceDue(entry.Value, now)) { IndexPlayerWait(round, entry.Value); continue; }
            if (!string.IsNullOrEmpty(round.TechnicalHoldReason))
            { EnqueueSilenceDeadline(round.RoundId, ticket.Value, now + 0.25); continue; }
            if (_pendingPlayerAnalysisIds.Count > 0 || _pendingPlayerReplyRoutingIds.Count > 0 || HasEventSpeakerWork(round))
            { EnqueueSilenceDeadline(round.RoundId, ticket.Value, now + 0.25); continue; }
            var source = ResolveDocument(ticket.Value);
            if (source?.IsReadyForPublication != true) { round.TechnicalHoldReason = "silence_source_unavailable"; continue; }
            EnqueueGeneration(source.AuthorKingdomId, CurrentPlayerAuthority(), null, true, source, 95, roundId: round.RoundId);
            var closure = Storage.Jobs.LastOrDefault(x => x.Kind == "generate" && x.RoundId == round.RoundId);
            if (closure != null) closure.SilenceSourceDocumentId = ticket.Value;
            else EnqueueSilenceDeadline(round.RoundId, ticket.Value, now + 0.25);
        }
    }

    private bool HasActiveDisputeRefusal(string actor) => _refusalsByKingdom.TryGetValue(actor ?? "", out var refusals)
        && refusals.Any(x => x.UntilDay > _host.CurrentDay());

    private int NextPlayerContactDay() => Storage.LastNpcPlayerContactDay < 0 ? 0 : Storage.LastNpcPlayerContactDay + Math.Max(1, Math.Min(30,
            (_host as IDiplomacyPlayerContactHost)?.PlayerContactIntervalDays() ?? 5));
    private bool CanInitiatePlayerContact() => _host.CurrentDay() >= NextPlayerContactDay();

    private string ContactIdentity(WorldDiplomacyJob job)
    {
        if (!string.IsNullOrEmpty(job.PlayerContactId)) return job.PlayerContactId;
        var source = ResolveDocument(job.SourceDocumentId);
        if (job.IsReminder && source?.IsReadyForPublication == true && source.AuthorKingdomId == job.AuthorKingdomId
            && source.ResponseRequests.Any(x => x.Kind == "invitation" && x.KingdomId == CurrentPlayerAuthority()))
            return job.PlayerContactId = string.IsNullOrEmpty(source.PlayerContactId) ? source.DocumentId : source.PlayerContactId;
        return job.PlayerContactId = "contact:" + job.JobId;
    }

    private bool IsNewPlayerContact(WorldDiplomacyJob job, Newtonsoft.Json.Linq.JObject draft)
    {
        bool invitation = (draft["response_requests"] as Newtonsoft.Json.Linq.JArray)?.OfType<Newtonsoft.Json.Linq.JObject>()
            .Any(x => (string)x["kind"] == "invitation" && (string)x["kingdom"] == CurrentPlayerAuthority()) == true;
        var round = ResolveRound(job.RoundId);
        bool opening = round?.NpcInitiatedPlayerEvent == true && string.IsNullOrEmpty(round.RootDocumentId)
            && job.TargetKingdomId == CurrentPlayerAuthority();
        if (!invitation && !opening) return false;
        string identity = ContactIdentity(job);
        return Storage.PublishedPlayerContacts?.Contains(identity) != true;
    }

    private bool DeferNewPlayerContact(WorldDiplomacyJob job, string raw)
    {
        if (CanInitiatePlayerContact() || !WorldDiplomacyEnvelopeJsonRules.TryParseJsonObject(raw, out var draft)
            || !IsNewPlayerContact(job,draft)) return false;
        var round = ResolveRound(job.RoundId);
        // Required replies must be drafted without an unsolicited new invitation;
        // they never enter the autonomous-contact waiting queue.
        if (round?.PlayerResponses.Any(x => x.KingdomId == job.AuthorKingdomId && x.Status == "pending"
            && (x.SourceDocumentId == job.SourceDocumentId || job.PlayerResponseSourceIds.Contains(x.SourceDocumentId))) == true)
            return false;
        job.DeferredContactDraft = raw; job.ContactNotBeforeDay = NextPlayerContactDay();
        job.Kind = "contact_wait"; job.IsRunning = false; job.LlmMessages?.Clear();
        return true;
    }

    private void RecordPublishedPlayerContact(WorldDiplomacyRound round, WorldDiplomacyDocument document)
    {
        if (document.IsPlayerAuthored || document.PlayerContactCounted || !document.IsReadyForPublication) return;
        bool newInvitation = document.ResponseRequests.Any(x => x.Kind == "invitation" && x.KingdomId == CurrentPlayerAuthority());
        bool opening = round.NpcInitiatedPlayerEvent && round.RootDocumentId == document.DocumentId
            && GetDocumentTargetIds(document).Contains(CurrentPlayerAuthority());
        if (!opening && !newInvitation) return;
        document.PlayerContactCounted = true;
        if (string.IsNullOrEmpty(document.PlayerContactId)) document.PlayerContactId = document.DocumentId;
        Storage.PublishedPlayerContacts ??= new(StringComparer.Ordinal);
        if (!Storage.PublishedPlayerContacts.Add(document.PlayerContactId)) return;
        Storage.LastNpcPlayerContactDay = Math.Max(Storage.LastNpcPlayerContactDay, _host.CurrentDay());
        Storage.NpcPlayerContactCount++;
    }
    private void RecordDisputeRefusal(WorldDiplomacyRound round, WorldDiplomacyDocument document)
    {
        Storage.PlayerDisputeRefusals ??= new();
        if (Storage.PlayerDisputeRefusals.Any(x => x.SourceDocumentId == document.DocumentId)) return;
        var refusal = new WorldDiplomacyPlayerDisputeRefusal { KingdomId = document.AuthorKingdomId, SourceDocumentId = document.DocumentId,
            RoundId = round.RoundId, Topic = round.RoundTopic, PublicScope = document.Body, UntilDay = _host.CurrentDay() + 3 };
        Storage.PlayerDisputeRefusals.Add(refusal);
        if (!_refusalsByKingdom.TryGetValue(refusal.KingdomId, out var rows)) _refusalsByKingdom[refusal.KingdomId] = rows = new();
        rows.Add(refusal);
    }

    private string BuildDisputeRefusalContext(string actor) => JsonConvert.SerializeObject(
        _refusalsByKingdom.TryGetValue(actor ?? "", out var rows) ? rows.Where(x => x.UntilDay > _host.CurrentDay()) : Enumerable.Empty<WorldDiplomacyPlayerDisputeRefusal>());

    private void MarkEventDirty(WorldDiplomacyRound round)
    {
        if (round != null && _playerDirtyRoundIds.Add(round.RoundId)) _playerDirtyRounds.Enqueue(round.RoundId);
        _diplomacyWorkNeedsReconcile = true;
    }

    private void ReconcileEventQueue()
    {
        int count = Math.Min(8, _playerDirtyRounds.Count);
        for (int i = 0; i < count; i++)
        {
            string id = _playerDirtyRounds.Dequeue(); _playerDirtyRoundIds.Remove(id);
            var round = ResolveRound(id);
            if (!IsLiveRound(round)) continue;
            ReconcileEventEligibility(round);
            AdvanceEventEnding(round);
            SchedulePlayerResponseWork(round); ScheduleEventCourtWork(round);
        }
        _diplomacyWorkNeedsReconcile = _playerDirtyRounds.Count > 0;
    }

    private void RememberPendingPlayerAnalysis(WorldDiplomacyDocument document)
    {
        if (document?.IsPlayerAuthored == true && document.AnalysisStatus == "pending_analysis" && !document.PlayerAnalysisCommitted
            && _pendingPlayerAnalysisIds.Add(document.DocumentId)) _playerAnalysisBacklog.Enqueue(document.DocumentId);
    }

    private void MaintainPlayerAnalysisBacklog()
    {
        int count = Math.Min(4, _playerAnalysisBacklog.Count);
        for (int i = 0; i < count; i++)
        {
            string id = _playerAnalysisBacklog.Dequeue(); var document = ResolveDocument(id);
            if (document?.AnalysisStatus != "pending_analysis" || document.PlayerAnalysisCommitted)
            { _pendingPlayerAnalysisIds.Remove(id); continue; }
            if (!Storage.Jobs.Any(x => x.Kind == "analyze" && x.DocumentId == id)) EnqueueAnalysisJob(document, 100);
            _playerAnalysisBacklog.Enqueue(id);
        }
    }

    private void RefreshPlayerAuthority()
    {
        string kingdom = CurrentPlayerAuthority(), ruler = _host.PartyRulerId(kingdom) ?? "";
        if (kingdom == _observedAuthorityKingdom && ruler == _observedAuthorityRuler) return;
        _observedAuthorityKingdom = kingdom; _observedAuthorityRuler = ruler;
        MaintainPlayerAuthority();
    }

    private bool HasEventSpeakerWork(WorldDiplomacyRound round) => Storage.Jobs.Any(x => x != null
        && x.Kind != "contact_wait"
        && x.RoundId == round.RoundId && (x.Kind == "generate" || IsEventPipelineJob(x)));

    private bool IsCurrentSilenceJob(WorldDiplomacyJob job)
    {
        var wait = ResolveRound(job.RoundId)?.PlayerNegotiationWaits?.FirstOrDefault(x => x.SourceDocumentId == job.SilenceSourceDocumentId);
        return DiplomacyEventRules.SilenceDue(wait, _host.CurrentCampaignHours())
            && _pendingPlayerAnalysisIds.Count == 0 && _pendingPlayerReplyRoutingIds.Count == 0;
    }

    private void RegisterPublishedPlayerAnswer(WorldDiplomacyDocument document)
    {
        if (document?.IsPlayerAuthored != true || !document.IsReadyForPublication
            || document.AnalysisStatus == "pending_analysis" || CurrentPlayerAuthority() != document.AuthorKingdomId) return;
        // The drafting shortcut is advisory. Only analyzed relevance and exact
        // mechanical/discussion source bindings answer a particular incoming matter.
        var sources = (document.AnsweredNpcDocumentIds ?? new()).Concat(new[] {
            document.RespondingToOfferDocumentId, document.RespondingToThreatDocumentId, document.DiscussionSourceDocumentId })
            .Concat((document.Actions ?? new()).Where(x => x != null).SelectMany(x => new[] {
                x.RespondingToOfferDocumentId, x.RespondingToThreatDocumentId }))
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (string id in sources)
        {
            var source = ResolveDocument(id); var round = ResolveRound(source?.RoundId);
            if (source?.IsReadyForPublication != true || !IsLiveRound(round) || source.AuthorKingdomId == document.AuthorKingdomId) continue;
            EnsurePlayerDocumentParticipant(round, document);
            TransferEvent(round, true, "player_analyzed_related_reply");
            foreach (var wait in round.PlayerNegotiationWaits ?? new())
                if (wait.SourceDocumentId == id && string.IsNullOrEmpty(wait.AnswerDocumentId) && wait.RequiresAnswer)
                    wait.AnswerDocumentId = document.DocumentId;
            foreach (var staleClosing in Storage.Jobs.Where(x => x.RoundId == round.RoundId && !string.IsNullOrEmpty(x.SilenceSourceDocumentId)
                && round.PlayerNegotiationWaits.Any(w => w.SourceDocumentId == x.SilenceSourceDocumentId && !string.IsNullOrEmpty(w.AnswerDocumentId))).ToList())
                RemoveJob(staleClosing.JobId);
            WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(Storage.KingdomKnowledge, document.AuthorKingdomId, source.DocumentId, _host.CurrentDay());
            MarkEventDirty(round);
        }
    }

    private WorldDiplomacyRoundParticipant ObserveEventTarget(WorldDiplomacyRound round, string kingdom, bool requiresResponse)
    {
        var participant = round.Participants.FirstOrDefault(x => x.KingdomId == kingdom)
            ?? EnsureRoundParticipant(round, kingdom, "observer", false);
        if (participant?.State == "active") participant.MandatoryReplyPending |= requiresResponse;
        return participant;
    }

    private bool HandleEventPublication(WorldDiplomacyDocument document)
    {
        var round = ResolveRound(document?.RoundId);
        if (!DiplomacyEventRules.IsEvent(round) || !IsLiveRound(round)) return false;
        if (document.RoundProgressHandled || !document.IsReadyForPublication) return true;
        if (IsEnding(round) && !IsNewEndingDutyPublication(round, document))
        {
            // Retain the published text in history, but additional commentary
            // cannot invalidate the finite decision/announcement in flight.
            document.RoundAccountingHandled = document.RoundProgressHandled = true;
            return true;
        }
        round.RootDocumentId = string.IsNullOrEmpty(round.RootDocumentId) ? document.DocumentId : round.RootDocumentId;
        round.RoundTopic = string.IsNullOrEmpty(round.RoundTopic) ? document.Title : round.RoundTopic;
        round.LastActivityDay = _host.CurrentDay();
        var referencedReplies = round.PlayerResponses.Where(x => x.KingdomId == document.AuthorKingdomId
            && (DocumentRespondsTo(document, x.SourceDocumentId) || document.AuditedAgendaAnswers.Contains(x.SourceDocumentId))).ToList();
        // A published answer is an immutable receipt. Later references to the
        // invitation are discussion, not another admission or delivery receipt.
        var replyObligations = referencedReplies.Where(x => x.Status == "pending" && string.IsNullOrEmpty(x.AnswerDocumentId)).ToList();
        var participant = round.Participants.FirstOrDefault(x => x.KingdomId == document.AuthorKingdomId)
            ?? EnsureRoundParticipant(round, document.AuthorKingdomId, "observer", false);
        var acceptedInvitation = replyObligations.FirstOrDefault(x => x.RequestKind == "invitation" && x.ParticipationDecision == "accept");
        bool admission = participant.State != "active" && acceptedInvitation != null;
        bool explicitIntervention = !string.IsNullOrEmpty(participant.PendingParticipationSourceId)
            && participant.PendingParticipationSourceId == document.SourceDocumentId;
        bool limitedAnswer = participant.State != "active" && referencedReplies.Count > 0 && acceptedInvitation == null && !explicitIntervention;
        if (document.IsPlayerAuthored && document.PlayerEventParticipation is "decline" or "answer_only") limitedAnswer = true;
        if (!limitedAnswer) participant.State = "active";
        if (participant.State == "active" && participant.PendingParticipationSourceId == document.SourceDocumentId)
            participant.PendingParticipationSourceId = "";
        if (document.IsPlayerAuthored && document.PlayerEventParticipation == "decline") participant.State = "withdrawn";
        if (admission && acceptedInvitation?.DecisionPosition is WorldDiplomacyRoundParticipant acceptedPosition)
        {
            participant.Agenda = acceptedPosition.Agenda; participant.PreferredOutcome = acceptedPosition.PreferredOutcome;
            participant.ExpectedCost = acceptedPosition.ExpectedCost; participant.RedLine = acceptedPosition.RedLine;
            participant.Alternative = acceptedPosition.Alternative;
            participant.AgendaAddressTo = new() { ResolveDocument(acceptedInvitation.SourceDocumentId).AuthorKingdomId };
            // The acceptance answers the invitation; the newly admitted position still needs discussion.
            participant.AgendaExpressionDocumentId = "";
            participant.AgendaAnswerDocumentIds.Clear();
        }
        foreach (var work in round.PlayerCourtWork.Where(x => x.KingdomId == document.AuthorKingdomId
            && DocumentRespondsTo(document, x.SourceDocumentId))) work.Status = "answered";
        participant.LastSpokeDay = document.Day;
        participant.TurnCount++;
        foreach (var obligation in replyObligations)
        { obligation.Status = "answered"; obligation.AnswerDocumentId = document.DocumentId; }
        if (!document.IsPlayerAuthored && !limitedAnswer && !admission
            && !string.IsNullOrWhiteSpace(participant.Agenda) && !string.IsNullOrWhiteSpace(document.AuditedAgendaQuote)
            && document.Body.Contains(document.AuditedAgendaQuote))
        {
            if (string.IsNullOrEmpty(participant.AgendaExpressionDocumentId)) participant.AgendaExpressionDocumentId = document.DocumentId;
        }
        foreach (var other in round.Participants.Where(x => !string.IsNullOrEmpty(x.AgendaExpressionDocumentId)
            && x.KingdomId != document.AuthorKingdomId && (document.AuditedAgendaAnswers.Contains(x.AgendaExpressionDocumentId)
                || document.IsPlayerAuthored && document.AnalysisStatus != "pending_analysis" && DocumentRespondsTo(document,x.AgendaExpressionDocumentId))))
        {
            if (!other.AgendaAnswerDocumentIds.Contains(document.DocumentId)) other.AgendaAnswerDocumentIds.Add(document.DocumentId);
            var expression = ResolveDocument(other.AgendaExpressionDocumentId);
            if (expression != null && !_host.IsPlayerParty(other.KingdomId))
                RecordCountryParticipation(expression);
        }
        if (!document.RoundAccountingHandled)
        {
            round.ExecutedActionCount += WorldDiplomacyRoundLifecycleRules.CountSuccessfulMechanicalActions(document);
            if (document.MadeDiplomaticProgress) round.SubstantiveProgressCount++;
        }
        foreach (string target in GetDocumentTargetIds(document))
        {
            ObserveEventTarget(round, target, document.RequiresResponse);
        }
        if (!document.IsPlayerAuthored) RegisterNpcResponseObligations(round,document);
        RegisterAgendaObserverResponses(round, participant, document);
        if (document.IsPlayerAuthored)
        {
            RegisterPublishedPlayerAnswer(document);
            foreach (var suspended in Storage.Jobs.Where(x => x.RoundId == round.RoundId && !string.IsNullOrEmpty(x.SuspendedReason)).ToList())
            {
                // New player input is an explicit recovery opportunity, not a timer retry loop.
                TryResumeTechnicalRequest(suspended, explicitResume: true);
                suspended.LifecycleClassificationAttempts = 0;
                if (suspended.Kind == "player_lifecycle" && suspended.ForcedIntent != "recover_published_lifecycle") suspended.Kind = "generate";
                suspended.PrivacyPublicContext = ""; suspended.PrivacyRevision = -1; suspended.PrivacyRewrites = 0;
                suspended.PrivacyDraft = "";
                InvalidateDraftRepair(suspended);
                if (IsPrivacyPipelineJob(suspended)) suspended.Kind = "generate";
            }
        }
        else if (document.RoundParticipation == "withdraw" || document.RoundStatus is "resolved" or "deadlocked")
        {
            participant.State = "withdrawn";
            participant.WithdrawalSourceDocumentId = document.DocumentId;
            foreach (var offer in round.PendingOffers.Where(x => x.ProposerKingdomId == document.AuthorKingdomId && x.Status == "open")) offer.Status = "withdrawn";
            foreach (var wait in round.PlayerNegotiationWaits.Where(x => ResolveDocument(x.SourceDocumentId)?.AuthorKingdomId == document.AuthorKingdomId))
                wait.RequiresAnswer = false;
        }
        if (!document.IsPlayerAuthored && document.RequiresResponse && participant.State != "withdrawn"
            && (GetDocumentTargetIds(document).Contains(CurrentPlayerAuthority())
                || document.ResponseRequests.Any(x => x.KingdomId == CurrentPlayerAuthority()))
            && !round.PlayerNegotiationWaits.Any(x => x.SourceDocumentId == document.DocumentId))
        {
            var wait = new WorldDiplomacyPlayerWait { SourceDocumentId = document.DocumentId, NotifiedHour = document.ActualPlayerNoticeHour };
            round.PlayerNegotiationWaits.Add(wait); IndexPlayerWait(round, wait);
        }
        document.RoundAccountingHandled = document.RoundProgressHandled = true;
        RecordConvergencePublication(round, document);
        RecordPublishedPlayerContact(round, document);
        round.ConversationRevision++;
        MarkEventDirty(round);
        return true;
    }

    private void RegisterAgendaObserverResponses(WorldDiplomacyRound round, WorldDiplomacyRoundParticipant participant, WorldDiplomacyDocument document)
    {
        if (participant.AgendaExpressionDocumentId == document.DocumentId)
        {
            foreach (string target in participant.AgendaAddressTo.Where(x => !_host.IsPlayerParty(x)
                && round.Participants.Any(p => p.KingdomId == x && p.State == "observer")))
            {
                if (round.PlayerResponses.Any(x => x.SourceDocumentId == document.DocumentId && x.KingdomId == target)) continue;
                round.PlayerResponses.Add(new() { SourceDocumentId = document.DocumentId, KingdomId = target,
                    OriginalRoundId = round.RoundId, CreatedDay = document.Day, RequestKind = "case_question",
                    RequestQuote = document.AuditedAgendaQuote });
            }
        }
    }

    private void RecordCountryParticipation(WorldDiplomacyDocument expression)
    {
        Storage.CountryParticipation ??= new(StringComparer.Ordinal);
        // Absence is measured in game days. Within one day use a stable canonical
        // document ID, not callback order or the day on which its answer arrived.
        if (Storage.CountryParticipation.TryGetValue(expression.AuthorKingdomId, out var last)
            && (last.LastExpressionDay > expression.Day || last.LastExpressionDay == expression.Day
                && string.CompareOrdinal(last.LastDocumentId, expression.DocumentId) >= 0)) return;
        Storage.CountryParticipation[expression.AuthorKingdomId] = new DiplomacyCountryParticipation {
            LastExpressionDay = expression.Day, LastDocumentId = expression.DocumentId, LastEventId = expression.RoundId,
            ExpressionKind = expression.RoundParticipation == "withdraw" ? "withdrawal" : "negotiation" };
    }

    private void RegisterNpcResponseObligations(WorldDiplomacyRound round, WorldDiplomacyDocument source)
    {
        if (!source.IsReadyForPublication || source.IsPlayerAuthored) return;
        foreach (var group in source.ResponseRequests.Where(x => x.KingdomId != source.AuthorKingdomId && !_host.IsPlayerParty(x.KingdomId)).GroupBy(x => x.KingdomId))
        {
            if (IsEnding(round) && !IsEndingDutySource(round, group.Key, source.DocumentId)) continue;
            if (round.PlayerResponses.Any(x => x.SourceDocumentId == source.DocumentId && x.KingdomId == group.Key)) continue;
            round.PlayerResponses.Add(new() { SourceDocumentId = source.DocumentId, KingdomId = group.Key,
                OriginalRoundId = round.RoundId, CreatedDay = source.Day,
                RequestKind = group.Any(x => x.Kind == "invitation") ? "invitation" : group.Any(x => x.Kind == "case_question") ? "case_question" : "side_question",
                RequestQuote = string.Join("\n",group.Select(x => x.Quote)) });
            ObserveEventTarget(round,group.Key,true);
        }
        // Recorded at publication, dispatched only after actual court knowledge.
        MarkEventDirty(round);
    }

    private void MaintainEvents()
    {
        int hour = _host.CurrentHour(), day = _host.CurrentDay();
        if (hour == _playerMaintenanceHour) return;
        _playerMaintenanceHour = hour;
        if (Storage.PlayerLifecycleSchemaVersion < 1) NormalizePlayerLifecycleRecovery();
        MaintainPlayerAnalysisBacklog();
        MaintainPlayerReplyRoutingBacklog();
        if (_playerAuthorityDay != day)
        {
            _playerAuthorityDay = day; MaintainPlayerAuthority();
            Storage.PlayerDisputeRefusals?.RemoveAll(x => x.UntilDay <= day);
            foreach (var rows in _refusalsByKingdom.Values) rows.RemoveAll(x => x.UntilDay <= day);
        }
        // Bounded round slices on game-hour changes, no archive scanning between them.
        int count = (Storage.ConcurrentRounds?.Count ?? 0) + 1;
        for (int i = 0; i < Math.Min(8, count); i++)
        {
            int index = _playerRoundCursor++ % count;
            var round = index == 0 ? Storage.ActiveRound : Storage.ConcurrentRounds[index - 1];
            if (!IsLiveRound(round) || !DiplomacyEventRules.IsEvent(round)) continue;
            ReconcileEventEligibility(round);
            AdvanceEventEnding(round);
            SchedulePlayerResponseWork(round);
            ScheduleEventCourtWork(round);
            PruneInvalidOffers(round);
            if (_pendingPlayerAnalysisIds.Count == 0 && _pendingPlayerReplyRoutingIds.Count == 0)
                AdvanceEventProgress(round);
        }
        StartNextPlayerLifecycleRecovery();
        TrySchedulePlayerAgenda();
    }

    private void TrySchedulePlayerAgenda()
    {
        int day = _host.CurrentDay();
        string player = CurrentPlayerAuthority();
        if (Storage.PlayerLifecycleRecoveryRoundIds?.Count > 0 || string.IsNullOrEmpty(player) || !CanInitiatePlayerContact() || day < Storage.NextPlayerAgendaDay || Storage.LastPlayerAgendaEvaluationDay == day
            || DiplomacyEventRules.HasNpcOpening(GetLiveRounds()) || Storage.Jobs.Count(x => x.SchedulingOwner == "player") >= _host.MaxPendingJobs()
            || _host.PromptWorld() == null) return;
        Storage.LastPlayerAgendaEvaluationDay = day;
        var candidates = _host.AllKingdomIds().Where(x => x != player && _host.PartyResolved(x)
            && !_host.IsEliminatedParty(x) && _host.HasIndependentAuthority(x)).OrderBy(x => x, StringComparer.Ordinal).ToList();
        if (candidates.Count == 0) return;
        Storage.PlayerAgendaCandidates ??= new();
        Storage.PlayerAgendaCandidates.RemoveAll(x => x.PlayerKingdomId != player || x.LastObservedDay < day - 30 || !candidates.Contains(x.KingdomId));
        var opportunity = Storage.PlayerAgendaCandidates.Where(x => x.LastEvaluatedDay < x.LastObservedDay)
            .OrderBy(x => x.LastEvaluatedDay).ThenByDescending(x => x.LastObservedDay).FirstOrDefault();
        string actor = opportunity?.KingdomId ?? candidates[Math.Abs(Storage.PlayerAgendaCursor++ % candidates.Count)];
        if (opportunity != null) opportunity.LastEvaluatedDay = day;
        var round = CreateIndependentDialogueRound(actor, player, "npc_player_agenda");
        round.RoundTopic = "";
        round.NpcInitiatedPlayerEvent = true;
        round.EventSourceType = "npc_player_agenda";
        EnqueuePlayerDecision(round, actor, player, null, "opening");
    }

    private void RegisterPlayerAgendaCandidate(string sourceKey, string first, string second)
    {
        string player = CurrentPlayerAuthority();
        if (string.IsNullOrEmpty(player)) return;
        Storage.PlayerAgendaCandidates ??= new();
        foreach (string actor in new[] { first, second }.Where(x => !string.IsNullOrEmpty(x) && x != player).Distinct())
        {
            var candidate = Storage.PlayerAgendaCandidates.FirstOrDefault(x => x.KingdomId == actor && x.PlayerKingdomId == player);
            if (candidate == null)
            { candidate = new WorldDiplomacyPlayerAgendaCandidate { KingdomId = actor, PlayerKingdomId = player }; Storage.PlayerAgendaCandidates.Add(candidate); }
            candidate.LastObservedDay = _host.CurrentDay();
            if (!candidate.SourceKeys.Contains(sourceKey)) candidate.SourceKeys.Add(sourceKey);
            // Candidate references are bounded; the original world fact owner retains its evidence.
            if (candidate.SourceKeys.Count > 8) candidate.SourceKeys.RemoveAt(0);
        }
    }

    private void EnqueuePlayerDecision(WorldDiplomacyRound round, string actor, string target, WorldDiplomacyDocument source, string purpose)
    {
        if (HasEventSpeakerWork(round)) return;
        var job = new WorldDiplomacyJob { JobId = _host.NewId("player_diplomacy_decision"), Kind = "player_decision",
            RoundId = round.RoundId, ExchangeId = round.RoundId, AuthorKingdomId = actor, TargetKingdomId = target,
            SourceDocumentId = source?.DocumentId ?? "", ForcedIntent = purpose, Priority = 95,
            CreatedDay = _host.CurrentDay(), MaxTokens = _host.AnalysisMaxTokens(),
            PersonalMemoryRulerId = _host.PartyRulerId(actor), RoundConversationRevision = round.ConversationRevision,
            SystemPrompt = PromptComposerTexts.ApplyOutputLanguage(DiplomacyPromptTexts.Get("PlayerAgendaDecision",
                "Privately decide whether this ruler has a current reason to open, join, or resume public diplomatic negotiations. Use known facts, interests, relationships and relevant memory, without manufacturing motives or reviving stale disputes. Major independent issues never override the one active NPC-initiated event slot. For temporary refusal of the same dispute, ignore repetition unless there are substantive new terms, an actual new development, valid acceptance or fulfillment. Pure closing notices need no answer. Choose freely, not by fixed political rules. Return only JSON {\"participate\":true,\"reason\":\"current motivation\"}; use participate=false if there is no useful reason. This is an internal decision, not a public declaration. When purpose is reconsider_temporarily_refused_dispute, compare the supplied prior refusal scopes with this declaration and add same_dispute_repeat=true only for unchanged repetition of the same dispute; otherwise false. New disputes, substantive new terms, valid acceptance or fulfillment must receive a formal response, which may refuse or close. Never treat an unclassified technical failure as a political refusal. When purpose is join_known_event and participate=true, also return position={agenda,gain,cost,red_line,alternative}, each a concrete nonempty string based on known material. Being named or receiving a declaration alone does not establish participation.")),
            UserPrompt = BuildGenerationPromptForJob(actor, target, null, source != null, source, false, round.RoundId,
                false, new List<string> { target }, source != null) + "\n" + BuildEventContinuity(round, actor) };
        job.UserPrompt += "\n" + BuildPlayerAgendaMaterial(actor);
        job.UserPrompt += "\n" + BuildDisputeRefusalContext(actor);
        if (purpose != "opening") job.UserPrompt += "\n" + purpose;
        if (purpose == "answer_invitation") job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("InvitationDecision", "");
        EnqueueJob(job);
    }

    private void CompletePlayerDecision(WorldDiplomacyJob job, Newtonsoft.Json.Linq.JObject json)
    {
        var round = ResolveRound(job.RoundId);
        if (IsUnsolicitedCourtJoinJob(job)) { RetireUnsolicitedCourtJoins(round); return; }
        if (json["participate"]?.Type != Newtonsoft.Json.Linq.JTokenType.Boolean)
        { SuspendDiplomacyJob(job, "agenda_decision_invalid"); return; }
        bool participate = json["participate"].Value<bool>();
        if (job.ForcedIntent == "answer_invitation")
        {
            if (!IsLiveRound(round) || !_host.HasIndependentAuthority(job.AuthorKingdomId)
                || !_host.CanAiAuthorParty(job.AuthorKingdomId, out _))
            { SuspendDiplomacyJob(job, "invitation_authority_changed"); return; }
            var obligation = round?.PlayerResponses.FirstOrDefault(x => x.SourceDocumentId == job.SourceDocumentId
                && x.KingdomId == job.AuthorKingdomId && x.Status == "pending");
            string decision = (string)json["decision"], explanation = (string)json["reason"];
            if (obligation == null || decision is not "accept" and not "decline" and not "question" and not "conditional" and not "reserve"
                || string.IsNullOrWhiteSpace(explanation))
            { SuspendDiplomacyJob(job, "invitation_decision_invalid"); return; }
            WorldDiplomacyRoundParticipant invitationPosition = null;
            if (decision == "accept" && (json["position"] is not JObject p || !TryReadEventPosition(p, out invitationPosition)))
            { SuspendDiplomacyJob(job, "invitation_position_missing"); return; }
            // A decision is drafting input, never a published answer or membership grant.
            obligation.ParticipationDecision = decision; obligation.DecisionReason = explanation;
            obligation.DecisionPosition = invitationPosition;
            RemoveJob(job.JobId);
            EnqueueGeneration(job.AuthorKingdomId, job.TargetKingdomId, null, true,
                ResolveDocument(job.SourceDocumentId), 95, roundId: round.RoundId, externalResponseOnly: true);
            return;
        }
        if (job.ForcedIntent == "reconsider_temporarily_refused_dispute" && !participate)
        {
            if (json["same_dispute_repeat"]?.Type != JTokenType.Boolean)
            { SuspendDiplomacyJob(job, "repeat_scope_decision_missing"); return; }
            if (!json["same_dispute_repeat"].Value<bool>()) participate = true;
        }
        string reason = json["reason"]?.Type == JTokenType.String ? json["reason"].Value<string>() : "";
        if (participate && string.IsNullOrWhiteSpace(reason)) { SuspendDiplomacyJob(job, "agenda_reason_missing"); return; }
        WorldDiplomacyRoundParticipant position = null;
        if (participate && job.ForcedIntent == "join_known_event"
            && (json["position"] is not JObject row || !TryReadEventPosition(row, out position)))
        { SuspendDiplomacyJob(job, "event_intervention_position_missing"); return; }
        if (participate && (!_host.HasIndependentAuthority(job.AuthorKingdomId)
            || !_host.CanAiAuthorParty(job.AuthorKingdomId, out _)))
        { SuspendDiplomacyJob(job, "event_intervention_authority_changed"); return; }
        RemoveJob(job.JobId);
        if (!participate)
        {
            if (job.ForcedIntent == "opening") CloseRound("player_agenda_no_opportunity", round);
            else if (round.PlayerResponses.Any(x => x.KingdomId == job.AuthorKingdomId
                && x.SourceDocumentId == job.SourceDocumentId && x.Status == "pending"))
            {
                foreach (var response in round.PlayerResponses.Where(x => x.KingdomId == job.AuthorKingdomId
                    && x.SourceDocumentId == job.SourceDocumentId && x.Status == "pending"))
                { response.ParticipationDecision = "decline"; response.DecisionReason = reason; }
                EnqueueGeneration(job.AuthorKingdomId, job.TargetKingdomId, null, true,
                    ResolveDocument(job.SourceDocumentId), 95, roundId: round.RoundId, externalResponseOnly: true);
                return;
            }
            foreach (var work in round.PlayerCourtWork.Where(x => x.KingdomId == job.AuthorKingdomId && x.SourceDocumentId == job.SourceDocumentId)) work.Status = "declined";
            MarkEventDirty(round);
            return;
        }
        var participant = EnsureRoundParticipant(round, job.AuthorKingdomId, position == null ? "active" : "observer", true);
        participant.RefuseRepeatedUntilDay = 0;
        if (position != null)
        {
            participant.Agenda = position.Agenda; participant.PreferredOutcome = position.PreferredOutcome;
            participant.ExpectedCost = position.ExpectedCost; participant.RedLine = position.RedLine; participant.Alternative = position.Alternative;
            participant.PendingParticipationSourceId = job.SourceDocumentId;
            participant.AgendaAddressTo = new() { job.TargetKingdomId };
        }
        round.ConversationRevision++; MarkEventDirty(round);
        // Private motives pass through isolation before any public drafting.
        round.TopicSeedContext = reason;
        if (job.ForcedIntent == "opening")
        {
            EnqueueEventConcept(round, _host.AllKingdomIds().Where(x => !string.IsNullOrEmpty(x)
                && _host.HasIndependentAuthority(x) && !_host.IsEliminatedParty(x)).Distinct().ToList(), job);
            return;
        }
        EnqueueGeneration(job.AuthorKingdomId, job.TargetKingdomId, null, job.ForcedIntent != "opening",
            ResolveDocument(job.SourceDocumentId), 95, roundId: round.RoundId,
            externalResponseOnly: job.ForcedIntent != "opening");
    }

    private bool ProcessPlayerEventCourtArrival(string receiver, WorldDiplomacyDocument document)
    {
        var round = ResolveRound(document?.RoundId);
        if (!DiplomacyEventRules.IsEvent(round)) return false;
        if (_host.IsPlayerAffiliatedParty(receiver)) document.HasReachedPlayerCourt = true;
        foreach (var deliveredAnswer in round.PlayerResponses.Where(x => x.AnswerDocumentId == document.DocumentId
            && ResolveDocument(x.SourceDocumentId)?.AuthorKingdomId == receiver)) deliveredAnswer.AnswerDelivered = true;
        if (!IsLiveRound(round)) return true;
        if (_host.IsPlayerParty(receiver)) return true;
        if (receiver == document.AuthorKingdomId || !_host.HasIndependentAuthority(receiver) || !DialogueDocumentKnown(receiver, document.DocumentId)) return true;
        round.CourtKnowledgeReceipts ??= new(); round.CourtKnowledgeVersions ??= new(StringComparer.OrdinalIgnoreCase);
        round.CourtKnowledgeReceiptIndex ??= new(round.CourtKnowledgeReceipts, StringComparer.OrdinalIgnoreCase);
        string receipt = receiver + ":" + document.DocumentId;
        if (round.CourtKnowledgeReceiptIndex.Add(receipt))
        {
            round.CourtKnowledgeReceipts.Add(receipt);
            round.CourtKnowledgeVersions[receiver] = DiplomacyEventRules.KnowledgeRevision(round, receiver) + 1;
            // A core agenda needs an answer even when the declaration does not
            // ask a classified question. Wake the bounded queue on delivery,
            // including while the campaign clock is paused.
            MarkEventDirty(round);
        }
        bool direct = HasExplicitCourtContact(document, receiver);
        bool answered = Storage.Documents.Any(x => x?.IsReadyForPublication == true && x.RoundId == round.RoundId
            && x.AuthorKingdomId == receiver && DocumentRespondsTo(x, document.DocumentId));
        if (answered) return true;
        // Propagation grants knowledge, not a new admission decision. Core
        // participants are selected before writing; genuine later contacts
        // retain their invitation/question/limited-response route.
        if (!direct) return true;
        if (document.IsPlayerAuthored) RegisterPlayerResponseWork(document);
        else if (document.ResponseRequests.Any(x => x.KingdomId == receiver))
        {
            RegisterNpcResponseObligations(round,document); SchedulePlayerResponseWork(round);
        }
        else if (document.RequiresResponse || (round.Participants.Any(x => x.KingdomId == receiver && x.State == "active")
            && WorldDiplomacyThreatScope.For(Storage, receiver, round.RoundId).Any(x => x.IssuerKingdomId == receiver
                && x.TargetKingdomId == document.AuthorKingdomId && x.TargetDecision == "noncomplied"
                && x.TargetDecisionDocumentId == document.DocumentId)))
            // A delivered refusal activates the issuer's existing follow-through
            // duty even if the refusing author does not request another reply.
            QueuePlayerCourtWork(round, receiver, document, "response");
        return true;
    }

    private void QueuePlayerCourtWork(WorldDiplomacyRound round, string receiver, WorldDiplomacyDocument source, string purpose)
    {
        if (!round.PlayerCourtWork.Any(x => x.KingdomId == receiver && x.SourceDocumentId == source.DocumentId))
            round.PlayerCourtWork.Add(new WorldDiplomacyPlayerCourtWork { KingdomId = receiver, SourceDocumentId = source.DocumentId, Purpose = purpose });
        MarkEventDirty(round);
        ScheduleEventCourtWork(round);
    }

    private string BuildPlayerAgendaMaterial(string actor) => JsonConvert.SerializeObject(new {
        candidates = Storage.PlayerAgendaCandidates?.Where(x => x.KingdomId == actor),
        current_war_pairs = _host.ActiveWarKingdomPairs().Select(x => new { first = x.firstId, second = x.secondId }),
        captured_native_requests = Storage.NativeSignals?.Where(x => x != null && (x.SourceKingdomId == actor || x.TargetKingdomId == actor))
            .OrderByDescending(x => x.Day).Take(16).Select(x => new { x.SignalId, x.SourceKingdomId, x.TargetKingdomId,
                requested_action = x.Action, request_reason = x.Reason, x.Day,
                execution_status = "intercepted_request_not_execution_receipt",
                currently_at_war = _host.IsAtWarByKingdomIds(x.SourceKingdomId, x.TargetKingdomId) }),
        known_battle_facts = Storage.RecentBattles?.Where(x => x != null && (x.AttackerKingdomIds?.Contains(actor) == true || x.DefenderKingdomIds?.Contains(actor) == true))
            .OrderByDescending(x => x.Day).Take(16)
    });

    private void ScheduleEventCourtWork(WorldDiplomacyRound round)
    {
        if (HasEventSpeakerWork(round)) return;
        if (IsEnding(round))
            foreach (var pending in round.PlayerCourtWork.Where(x => x.Status == "pending"
                && !IsEndingDutySource(round, x.KingdomId, x.SourceDocumentId))) pending.Status = "event_ending";
        var work = round.PlayerCourtWork.FirstOrDefault(x => x.Status == "pending" && x.Purpose != "join_known_event")
            ?? round.PlayerCourtWork.FirstOrDefault(x => x.Status == "pending");
        // New knowledge wakes established core exchanges without waiting for
        // another campaign hour. Explicit response work remains first.
        if ((work == null || work.Purpose == "join_known_event")
            && IsLiveRound(round) && round.EventSourceType != "player_followup"
            && string.IsNullOrEmpty(round.TechnicalHoldReason)
            && _pendingPlayerAnalysisIds.Count == 0 && _pendingPlayerReplyRoutingIds.Count == 0
            && EventBoundaryComplete(round, "started") && ScheduleUnexpressedPosition(round)) return;
        if (work == null) return;
        var source = ResolveDocument(work.SourceDocumentId);
        if (source?.IsReadyForPublication != true || _host.IsEliminatedParty(work.KingdomId)) { work.Status = "unavailable"; return; }
        if (!DialogueDocumentKnown(work.KingdomId, source.DocumentId)) return;
        var participant = round.Participants.FirstOrDefault(x => x.KingdomId == work.KingdomId);
        if (!source.ChangedDiplomaticState && (participant?.RefuseRepeatedUntilDay > _host.CurrentDay() || HasActiveDisputeRefusal(work.KingdomId)))
            EnqueuePlayerDecision(round, work.KingdomId, source.AuthorKingdomId, source, "reconsider_temporarily_refused_dispute");
        else if (work.Purpose == "join_known_event" || participant?.State != "active")
            EnqueuePlayerDecision(round, work.KingdomId, source.AuthorKingdomId, source, "join_known_event");
        else EnqueueGeneration(work.KingdomId, source.AuthorKingdomId, null, true, source, 95, roundId: round.RoundId);
        if (HasEventSpeakerWork(round)) work.Status = "queued";
    }

    private static bool HasExplicitCourtContact(WorldDiplomacyDocument source, string receiver) => source != null
        && (GetDocumentTargetIds(source).Contains(receiver) || source.AddressedKingdomIds.Contains(receiver)
            || source.ResponseRequests.Any(x => x.KingdomId == receiver));

    private bool IsUnsolicitedCourtJoinJob(WorldDiplomacyJob job)
    {
        if (job == null || string.IsNullOrEmpty(job.SourceDocumentId)) return false;
        var round = ResolveRound(job.RoundId);
        if (!DiplomacyEventRules.IsEvent(round)) return false;
        var source = ResolveDocument(job.SourceDocumentId);
        if (source == null || HasExplicitCourtContact(source, job.AuthorKingdomId)
            || round.PlayerResponses.Any(x => x.KingdomId == job.AuthorKingdomId && x.SourceDocumentId == job.SourceDocumentId)) return false;
        if (job.Kind == "player_decision" && job.ForcedIntent == "join_known_event") return true;
        // Old saves may have completed the unsolicited decision but not yet
        // published its resulting declaration. Do not admit that late draft.
        return (job.Kind == "generate" || IsPrivacyPipelineJob(job))
            && round.PlayerCourtWork.Any(x => x.KingdomId == job.AuthorKingdomId && x.SourceDocumentId == job.SourceDocumentId
                && x.Purpose == "join_known_event" && (x.Status == "retired_unsolicited"
                    || x.Status is "pending" or "queued" && round.Participants.Any(p => p.KingdomId == job.AuthorKingdomId
                        && p.State == "observer" && p.PendingParticipationSourceId == job.SourceDocumentId)));
    }

    private void RetireUnsolicitedCourtJoins(WorldDiplomacyRound round)
    {
        if (!DiplomacyEventRules.IsEvent(round)) return;
        var jobs = Storage.Jobs.Where(x => x.RoundId == round.RoundId && IsUnsolicitedCourtJoinJob(x)).ToList();
        bool ownedHold = jobs.Any(x => !string.IsNullOrEmpty(x.SuspendedReason) && x.SuspendedReason == round.TechnicalHoldReason);
        foreach (var job in jobs) RemoveJob(job.JobId);
        bool changed = jobs.Count > 0;
        foreach (var work in round.PlayerCourtWork.Where(x => x.Purpose == "join_known_event" && x.Status is "pending" or "queued"))
        {
            var source = ResolveDocument(work.SourceDocumentId);
            if (source == null || HasExplicitCourtContact(source, work.KingdomId)
                || round.PlayerResponses.Any(x => x.KingdomId == work.KingdomId && x.SourceDocumentId == work.SourceDocumentId)) continue;
            work.Status = "retired_unsolicited"; changed = true;
            var party = round.Participants.FirstOrDefault(x => x.KingdomId == work.KingdomId);
            if (party?.State == "observer" && party.PendingParticipationSourceId == work.SourceDocumentId)
                party.PendingParticipationSourceId = "";
        }
        if (ownedHold && !Storage.Jobs.Any(x => x.RoundId == round.RoundId && !string.IsNullOrEmpty(x.SuspendedReason))
            && !Storage.Documents.Any(x => x.RoundId == round.RoundId && x.EventExecutionPending)
            && !Storage.EventEffects.Any(x => x.EventId == round.RoundId && x.Status is "applying" or "uncertain"))
        {
            AccrueTechnicalPause(round); round.TechnicalHoldStartedHour = -1;
            round.TechnicalRequestHoldReason = ""; round.TechnicalHoldReason = "";
        }
        if (changed) { MarkEventDirty(round); _host.Log("diplomacy.unsolicited_court_work_retired round=" + round.RoundId + " jobs=" + jobs.Count); }
    }

    private string CurrentPlayerAuthority()
    {
        string id = _host.PlayerKingdomId();
        return !string.IsNullOrEmpty(id) && DialogueHost?.IsPlayerRuler(_host.PartyRulerId(id)) == true ? id : "";
    }

    private void InitializeEventOwner(WorldDiplomacyRound round, bool player)
    {
        if (round == null) return;
        round.EventSystemVersion = 1;
        if (string.IsNullOrEmpty(round.BirthLane))
            round.BirthLane = round.EventSourceType is "dialogue_commitment" or "legacy_accepted_aid" ? "oral" : player ? "player" : "normal";
        round.SchedulingOwner = round.BirthLane;
        round.OwnershipEpoch = Math.Max(1, round.OwnershipEpoch);
        if (player && DiplomacyEventRules.ContainsParty(round, CurrentPlayerAuthority()))
        {
            round.PlayerAuthorityKingdomId = CurrentPlayerAuthority();
            round.PlayerAuthorityRulerId = _host.PartyRulerId(round.PlayerAuthorityKingdomId) ?? "";
        }
        else round.PlayerAuthorityKingdomId = round.PlayerAuthorityRulerId = "";
        round.PlayerNegotiationWaits ??= new();
        round.PlayerCourtWork ??= new();
        round.CourtKnowledgeReceipts ??= new(); round.CourtKnowledgeVersions ??= new(StringComparer.OrdinalIgnoreCase);
    }

    // Participation/authority changes invalidate old drafts, never birth ownership.
    private void TransferEvent(WorldDiplomacyRound round, bool player, string reason)
    {
        string authority = CurrentPlayerAuthority();
        bool represented = DiplomacyEventRules.ContainsParty(round, authority);
        string kingdom = represented ? authority : "";
        string ruler = represented ? _host.PartyRulerId(authority) ?? "" : "";
        if (round == null || (round.PlayerAuthorityKingdomId == kingdom && round.PlayerAuthorityRulerId == ruler)) return;
        RefreshEventRequests(round, reason);
    }

    private void RefreshEventRequests(WorldDiplomacyRound round, string reason)
    {
        if (round == null) return;
        if (reason == "enabled_revalidate" && !string.IsNullOrEmpty(round.EventConvergence.TechnicalFailure))
        {
            round.EventConvergence.ExecutionRecoveryAttempts = 0;
            round.EventConvergence.NextExecutionRecoveryUtcTicks = 0;
        }
        RequestLeases.InvalidateRound(round.RoundId);
        round.OwnershipEpoch = Math.Max(1, round.OwnershipEpoch) + 1;
        string authority = CurrentPlayerAuthority();
        bool represented = DiplomacyEventRules.ContainsParty(round, authority);
        round.PlayerAuthorityKingdomId = represented ? authority : "";
        round.PlayerAuthorityRulerId = represented ? _host.PartyRulerId(authority) ?? "" : "";
        round.ConversationRevision++;
        foreach (var job in Storage.Jobs.Where(x => x != null && (x.RoundId == round.RoundId || x.ExchangeId == round.RoundId)).ToList())
        {
            if (reason == "enabled_revalidate") TryResumeTechnicalRequest(job, explicitResume: true);
            job.JobId = _host.NewId("diplomacy_revision");
            job.IsRunning = false; job.RequestAttempt = 0;
            job.LlmMessages?.Clear(); job.SemanticRepairAttempts = 0;
            job.PersonalMemoryRulerId = "";
            if (IsPrivacyPipelineJob(job)) job.Kind = "generate";
            if (job.Kind == "event_boundary_audit") job.Kind = "event_boundary";
            if (job.Kind == "event_concept_audit") job.Kind = "event_concept";
            if (job.Kind == "event_progress_audit") job.Kind = "event_progress";
            job.PrivacyPublicContext = ""; job.PrivacySourceMaterial = ""; job.PrivacyRevision = -1;
            if (!IsUnpublishedPlayerLifecycleJob(job)) job.PrivacyDraft = "";
            InvalidateDraftRepair(job);
            job.PrivacyApproved = false;
            job.RoundConversationRevision = round.ConversationRevision;
            DiplomacyEventRules.Bind(round, job);
        }
        MarkEventDirty(round);
        _host.Log("diplomacy.event_revision round=" + round.RoundId + " owner=" + round.SchedulingOwner
            + " epoch=" + round.OwnershipEpoch + " reason=" + reason);
    }

    private void NormalizePlayerEventState()
    {
        string authority = CurrentPlayerAuthority();
        bool migration = Storage.PlayerDiplomacySchemaVersion < 1;
        Storage.PlayerAgendaCandidates ??= new();
        Storage.PlayerDisputeRefusals ??= new();
        _refusalsByKingdom.Clear();
        foreach (var refusal in Storage.PlayerDisputeRefusals)
        {
            if (!_refusalsByKingdom.TryGetValue(refusal.KingdomId, out var rows)) _refusalsByKingdom[refusal.KingdomId] = rows = new();
            rows.Add(refusal);
        }
        foreach (var document in Storage.Documents) RememberPendingPlayerAnalysis(document);
        foreach (var round in GetLiveRounds().ToList())
        {
            if (round.EventSystemVersion == 0) MigrateLegacyEvent(round);
            if (!IsLiveRound(round)) continue;
            round.PlayerNegotiationWaits ??= new();
            round.PlayerCourtWork ??= new();
            MarkEventDirty(round);
        }
        foreach (var job in Storage.Jobs.Where(x => x != null))
            if (job.OwnershipEpoch == 0) DiplomacyEventRules.Bind(ResolveRound(job.RoundId), job);
        foreach (var round in GetLiveRounds().Where(DiplomacyEventRules.IsEvent))
            foreach (var work in round.PlayerCourtWork.Where(x => x.Status == "queued"))
                if (!Storage.Jobs.Any(x => x.RoundId == round.RoundId && x.AuthorKingdomId == work.KingdomId && x.SourceDocumentId == work.SourceDocumentId))
                    work.Status = "pending";
        if (migration)
        {
            foreach (var round in GetLiveRounds().Where(DiplomacyEventRules.IsEvent))
                foreach (var document in Storage.Documents.Where(x => x != null && x.RoundId == round.RoundId
                    && !x.IsPlayerAuthored && x.IsReadyForPublication && x.RequiresResponse
                    && GetDocumentTargetIds(x).Contains(authority)))
                {
                    var reply = Storage.Documents.FirstOrDefault(x => x?.IsPlayerAuthored == true && x.IsReadyForPublication
                        && x.RoundId == round.RoundId && x.Day >= document.Day && DocumentRespondsTo(x, document.DocumentId));
                    if (!round.PlayerNegotiationWaits.Any(x => x.SourceDocumentId == document.DocumentId))
                        round.PlayerNegotiationWaits.Add(new WorldDiplomacyPlayerWait { SourceDocumentId = document.DocumentId,
                            AnswerDocumentId = reply?.DocumentId ?? "", NotifiedHour = document.ActualPlayerNoticeHour });
                }
            // Old boolean flags never prove display. Only reply-bearing incoming artifacts are renotified.
            foreach (var document in Storage.Documents.Where(x => x != null && !x.IsPlayerAuthored
                && !x.LegacyNoticeSuppressed && x.IsReadyForPublication && x.RequiresResponse && x.ActualPlayerNoticeHour < 0
                && DiplomacyEventRules.IsEvent(ResolveRound(x.RoundId))
                && ResolveRound(x.RoundId).PlayerNegotiationWaits.Any(w => w.SourceDocumentId == x.DocumentId && string.IsNullOrEmpty(w.AnswerDocumentId))))
            { document.FormalNoticeShown = false; document.IsNotified = false; }
            Storage.PlayerDiplomacySchemaVersion = 1;
        }
        _waitingByDocument.Clear();
        foreach (var round in GetLiveRounds().Where(DiplomacyEventRules.IsEvent))
            foreach (var wait in round.PlayerNegotiationWaits) IndexPlayerWait(round, wait);
    }

    private void MaintainPlayerAuthority()
    {
        string authority = CurrentPlayerAuthority();
        _pendingPlayerReplyRoutingIds.RemoveWhere(id => ResolveDocument(id)?.AuthorKingdomId != authority
            || ResolveDocument(id)?.PlayerReplyRoutingPending != true);
        foreach (var round in GetLiveRounds().Where(IsLiveRound).ToList())
        {
            if (!DiplomacyEventRules.IsEvent(round)) continue;
            if (!string.IsNullOrEmpty(round.PlayerAuthorityKingdomId)
                && (authority != round.PlayerAuthorityKingdomId || _host.PartyRulerId(authority) != round.PlayerAuthorityRulerId))
            {
                // Ruler changes revoke every old request identity, even if the new player realm participates.
                TransferEvent(round, false, "player_authority_changed");
            }
        }
    }

    internal string RequestLane(WorldDiplomacyJob job)
    {
        var round = ResolveRound(job?.RoundId);
        string birth = !string.IsNullOrEmpty(round?.BirthLane) ? round.BirthLane
            : !string.IsNullOrEmpty(round?.SchedulingOwner) ? round.SchedulingOwner : job?.RequestLane;
        return birth is "player" or "oral" ? birth : "normal";
    }
    private static int LaneIndex(string lane) => lane == "player" ? 1 : lane == "oral" ? 2 : 0;
    private static string DispatchCaseKey(WorldDiplomacyJob job) => !string.IsNullOrEmpty(job.RoundId)
        ? job.RoundId : "maintenance:" + job.Kind;
    internal long LaneDispatchPriority(WorldDiplomacyJob job, int day)
    {
        int rank = 3 - ((LaneIndex(RequestLane(job)) - _nextRequestLane + 3) % 3);
        _lastCaseDispatch.TryGetValue(DispatchCaseKey(job), out long turn);
        // Lane rotation first, then least-recently-served event, then urgency within that event.
        return rank * (1L << 60) - turn * 256L + Math.Min(100, job.Priority + Math.Max(0, day - job.CreatedDay) * 5);
    }
    internal void RecordLaneDispatch(WorldDiplomacyJob job)
    {
        _nextRequestLane = (LaneIndex(RequestLane(job)) + 1) % 3;
        _lastCaseDispatch[DispatchCaseKey(job)] = ++_requestTurn;
    }

    private string BuildEventContinuity(WorldDiplomacyRound round, string author)
    {
        // Current clauses are lossless; summaries never substitute for live proposals.
        var offers = (round.PendingOffers ?? new()).Where(x => x != null && x.Status == "open")
            .Where(x => DialogueDocumentKnown(author, x.SourceDocumentId)).Select(x => new {
                source = x.SourceDocumentId, action = x.SourceActionId, proposer = x.ProposerKingdomId,
                target = x.TargetKingdomId, intent = x.Intent, status = x.Status,
                terms = ResolveDialogueTerms(ResolveDocument(x.SourceDocumentId), x.SourceActionId),
                condition = ResolveDocument(x.SourceDocumentId)?.Actions?.FirstOrDefault(a => a.ActionId == x.SourceActionId)
            });
        return DiplomacyPromptTexts.Get("PlayerEventContinuity", "[Event continuity: recorded intentions are revisable; executed facts and effective clauses are authoritative]")
            + "\n" + BuildAidContext(round, author)
            + "\n" + JsonConvert.SerializeObject(new { round.RoundId, round.RoundTopic, round.RoundStatus,
                round.ExternalOpeningContext, round.EventBackground, round.EventNarrative, round.TopicSeedContext,
                world_book_sources = ReadEventLore(round, author),
                offers = offers.ToList(), participants = round.Participants.Select(x => new { x.KingdomId, x.State,
                    x.MandatoryReplyPending, x.LastTriggeredDocumentId }),
                public_boundaries = Storage.EventAnnouncements.Where(x => x.EventId == round.RoundId && x.State == "public"),
                recorded_intention = round.Participants.FirstOrDefault(x => x.KingdomId == author) });
    }
}
