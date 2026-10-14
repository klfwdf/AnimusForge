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
    private bool _preferPlayerDispatch;
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
            if (!IsLiveRound(round) || !DiplomacyPlayerEventRules.IsPlayer(round)
                || !_waitingByDocument.TryGetValue(ticket.Value, out var entry)
                || !DiplomacyPlayerEventRules.SilenceDue(entry.Value, now)) continue;
            if (_pendingPlayerAnalysisIds.Count > 0 || _pendingPlayerReplyRoutingIds.Count > 0 || HasPlayerSpeakerWork(round))
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

    private void MarkPlayerEventDirty(WorldDiplomacyRound round)
    {
        if (round != null && _playerDirtyRoundIds.Add(round.RoundId)) _playerDirtyRounds.Enqueue(round.RoundId);
        _diplomacyWorkNeedsReconcile = true;
    }

    private void ReconcilePlayerEventQueue()
    {
        int count = Math.Min(8, _playerDirtyRounds.Count);
        for (int i = 0; i < count; i++)
        {
            string id = _playerDirtyRounds.Dequeue(); _playerDirtyRoundIds.Remove(id);
            var round = ResolveRound(id);
            if (!IsLiveRound(round)) continue;
            SchedulePlayerResponseWork(round); SchedulePlayerCourtWork(round);
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

    private bool HasPlayerSpeakerWork(WorldDiplomacyRound round) => Storage.Jobs.Any(x => x != null
        && x.RoundId == round.RoundId && (x.Kind == "generate" || IsPlayerPipelineJob(x)));

    private bool IsCurrentSilenceJob(WorldDiplomacyJob job)
    {
        var wait = ResolveRound(job.RoundId)?.PlayerNegotiationWaits?.FirstOrDefault(x => x.SourceDocumentId == job.SilenceSourceDocumentId);
        return DiplomacyPlayerEventRules.SilenceDue(wait, _host.CurrentCampaignHours())
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
            EnsureRoundParticipant(round, document.AuthorKingdomId, "active", false);
            TransferEvent(round, true, "player_analyzed_related_reply");
            foreach (var wait in round.PlayerNegotiationWaits ?? new())
                if (wait.SourceDocumentId == id && string.IsNullOrEmpty(wait.AnswerDocumentId) && wait.RequiresAnswer)
                    wait.AnswerDocumentId = document.DocumentId;
            foreach (var staleClosing in Storage.Jobs.Where(x => x.RoundId == round.RoundId && !string.IsNullOrEmpty(x.SilenceSourceDocumentId)
                && round.PlayerNegotiationWaits.Any(w => w.SourceDocumentId == x.SilenceSourceDocumentId && !string.IsNullOrEmpty(w.AnswerDocumentId))).ToList())
                RemoveJob(staleClosing.JobId);
            WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(Storage.KingdomKnowledge, document.AuthorKingdomId, source.DocumentId, _host.CurrentDay());
            MarkPlayerEventDirty(round);
        }
    }

    private bool HandlePlayerEventPublication(WorldDiplomacyDocument document)
    {
        var round = ResolveRound(document?.RoundId);
        if (!DiplomacyPlayerEventRules.IsPlayer(round) || !IsLiveRound(round)) return false;
        if (document.RoundProgressHandled || !document.IsReadyForPublication) return true;
        round.RootDocumentId = string.IsNullOrEmpty(round.RootDocumentId) ? document.DocumentId : round.RootDocumentId;
        round.RoundTopic = string.IsNullOrEmpty(round.RoundTopic) ? document.Title : round.RoundTopic;
        round.LastActivityDay = _host.CurrentDay();
        round.RelayPlanned = true;
        round.RelayWaiting = false;
        var participant = EnsureRoundParticipant(round, document.AuthorKingdomId, "active", false);
        participant.State = "active";
        foreach (var work in round.PlayerCourtWork.Where(x => x.KingdomId == document.AuthorKingdomId
            && DocumentRespondsTo(document, x.SourceDocumentId))) work.Status = "answered";
        participant.LastSpokeDay = document.Day;
        participant.TurnCount++;
        if (!document.RoundAccountingHandled)
        {
            round.ExecutedActionCount += WorldDiplomacyRoundLifecycleRules.CountSuccessfulMechanicalActions(document);
            if (document.MadeDiplomaticProgress) round.SubstantiveProgressCount++;
        }
        if (!round.RelayRouteKingdomIds.Contains(document.AuthorKingdomId)) round.RelayRouteKingdomIds.Add(document.AuthorKingdomId);
        foreach (string target in GetDocumentTargetIds(document))
        {
            EnsureRoundParticipant(round, target, "active", document.RequiresResponse);
            if (!round.RelayRouteKingdomIds.Contains(target)) round.RelayRouteKingdomIds.Add(target);
        }
        if (document.IsPlayerAuthored)
        {
            RegisterPublishedPlayerAnswer(document);
            foreach (var suspended in Storage.Jobs.Where(x => x.RoundId == round.RoundId && !string.IsNullOrEmpty(x.SuspendedReason)).ToList())
            {
                // New player input is an explicit recovery opportunity, not a timer retry loop.
                suspended.SuspendedReason = ""; suspended.TransportRetryAttempts = 0;
                suspended.LifecycleClassificationAttempts = 0;
                if (suspended.Kind == "player_lifecycle" && suspended.ForcedIntent != "recover_published_lifecycle") suspended.Kind = "generate";
                suspended.PrivacyPublicContext = ""; suspended.PrivacyRevision = -1; suspended.PrivacyRewrites = 0;
                suspended.PrivacyDraft = "";
                if (IsPrivacyPipelineJob(suspended)) suspended.Kind = "generate";
            }
            round.TechnicalHoldReason = "";
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
            && GetDocumentTargetIds(document).Contains(CurrentPlayerAuthority())
            && !round.PlayerNegotiationWaits.Any(x => x.SourceDocumentId == document.DocumentId))
        {
            var wait = new WorldDiplomacyPlayerWait { SourceDocumentId = document.DocumentId, NotifiedHour = document.ActualPlayerNoticeHour };
            round.PlayerNegotiationWaits.Add(wait); IndexPlayerWait(round, wait);
        }
        document.RoundAccountingHandled = document.RoundProgressHandled = true;
        round.ConversationRevision++;
        MarkPlayerEventDirty(round);
        return true;
    }

    private void MaintainPlayerEvents()
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
            if (!IsLiveRound(round) || !DiplomacyPlayerEventRules.IsPlayer(round)) continue;
            SchedulePlayerResponseWork(round);
            SchedulePlayerCourtWork(round);
            PruneInvalidOffers(round);
            if (HasExplicitPlayerEventConclusion(round) && _pendingPlayerAnalysisIds.Count == 0 && _pendingPlayerReplyRoutingIds.Count == 0
                && !HasPlayerSpeakerWork(round) && string.IsNullOrEmpty(round.TechnicalHoldReason)
                && !round.PlayerResponses.Any(x => x.Status == "pending")
                && !round.PlayerCourtWork.Any(x => x.Status is "pending" or "queued")
                && !round.PlayerNegotiationWaits.Any(x => x.RequiresAnswer && string.IsNullOrEmpty(x.AnswerDocumentId) && string.IsNullOrEmpty(x.ClosureDocumentId))
                && !(round.PendingOffers?.Any(x => x.Status == "open") ?? false)
                && ResolveDocument(round.RootDocumentId)?.IsReadyForPublication == true)
                CloseRound("player_negotiation_finished", round);
        }
        StartNextPlayerLifecycleRecovery();
        TrySchedulePlayerAgenda();
    }

    private void TrySchedulePlayerAgenda()
    {
        int day = _host.CurrentDay();
        string player = CurrentPlayerAuthority();
        if (Storage.PlayerLifecycleRecoveryRoundIds?.Count > 0 || string.IsNullOrEmpty(player) || day < Storage.NextPlayerAgendaDay || Storage.LastPlayerAgendaEvaluationDay == day
            || DiplomacyPlayerEventRules.HasNpcOpening(GetLiveRounds()) || Storage.Jobs.Count(x => x.SchedulingOwner == "player") >= _host.MaxPendingJobs()
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
        if (HasPlayerSpeakerWork(round)) return;
        var job = new WorldDiplomacyJob { JobId = _host.NewId("player_diplomacy_decision"), Kind = "player_decision",
            RoundId = round.RoundId, ExchangeId = round.RoundId, AuthorKingdomId = actor, TargetKingdomId = target,
            SourceDocumentId = source?.DocumentId ?? "", ForcedIntent = purpose, Priority = 95,
            CreatedDay = _host.CurrentDay(), MaxTokens = _host.AnalysisMaxTokens(),
            PersonalMemoryRulerId = _host.PartyRulerId(actor), RoundConversationRevision = round.ConversationRevision,
            SystemPrompt = PromptComposerTexts.ApplyOutputLanguage(DiplomacyPromptTexts.Get("PlayerAgendaDecision",
                "Privately decide whether this ruler has a current reason to open, join, or resume public diplomatic negotiations. Use known facts, interests, relationships and relevant memory, without manufacturing motives or reviving stale disputes. Major independent issues never override the one active NPC-initiated event slot. For temporary refusal of the same dispute, ignore repetition unless there are substantive new terms, an actual new development, valid acceptance or fulfillment. Pure closing notices need no answer. Choose freely, not by fixed political rules. Return only JSON {\"participate\":true,\"reason\":\"current motivation\"}; use participate=false if there is no useful reason. This is an internal decision, not a public declaration. When purpose is reconsider_temporarily_refused_dispute, compare the supplied prior refusal scopes with this declaration and add same_dispute_repeat=true only for unchanged repetition of the same dispute; otherwise false. New disputes, substantive new terms, valid acceptance or fulfillment must receive a formal response, which may refuse or close. Never treat an unclassified technical failure as a political refusal.")),
            UserPrompt = BuildGenerationPromptForJob(actor, target, null, source != null, source, false, round.RoundId,
                false, new List<string> { target }, source != null) + "\n" + BuildPlayerEventContinuity(round, actor)
                + "\n" + DialogueHost?.PersonalMemory(_host.PartyRulerId(actor), round.RoundTopic, _host.PartyNameOrEmpty(target)) };
        job.UserPrompt += "\n" + BuildPlayerAgendaMaterial(actor);
        job.UserPrompt += "\n" + BuildDisputeRefusalContext(actor);
        if (purpose != "opening") job.UserPrompt += "\n" + purpose;
        EnqueueJob(job);
    }

    private void CompletePlayerDecision(WorldDiplomacyJob job, Newtonsoft.Json.Linq.JObject json)
    {
        var round = ResolveRound(job.RoundId);
        if (json["participate"]?.Type != Newtonsoft.Json.Linq.JTokenType.Boolean)
        { SuspendDiplomacyJob(job, "agenda_decision_invalid"); return; }
        bool participate = json["participate"].Value<bool>();
        if (job.ForcedIntent == "reconsider_temporarily_refused_dispute" && !participate)
        {
            if (json["same_dispute_repeat"]?.Type != JTokenType.Boolean)
            { SuspendDiplomacyJob(job, "repeat_scope_decision_missing"); return; }
            if (!json["same_dispute_repeat"].Value<bool>()) participate = true;
        }
        string reason = json["reason"]?.Type == JTokenType.String ? json["reason"].Value<string>() : "";
        if (participate && string.IsNullOrWhiteSpace(reason)) { SuspendDiplomacyJob(job, "agenda_reason_missing"); return; }
        RemoveJob(job.JobId);
        if (!participate)
        {
            if (job.ForcedIntent == "opening") CloseRound("player_agenda_no_opportunity", round);
            else foreach (var response in round.PlayerResponses.Where(x => x.KingdomId == job.AuthorKingdomId
                && x.SourceDocumentId == job.SourceDocumentId && x.Status == "pending")) response.Status = "declined_repeat";
            foreach (var work in round.PlayerCourtWork.Where(x => x.KingdomId == job.AuthorKingdomId && x.SourceDocumentId == job.SourceDocumentId)) work.Status = "declined";
            return;
        }
        var participant = EnsureRoundParticipant(round, job.AuthorKingdomId, "active", true);
        participant.RefuseRepeatedUntilDay = 0;
        // Private motives pass through isolation before any public drafting.
        round.TopicSeedContext = reason;
        EnqueueGeneration(job.AuthorKingdomId, job.TargetKingdomId, null, job.ForcedIntent != "opening",
            ResolveDocument(job.SourceDocumentId), 95, roundId: round.RoundId,
            externalResponseOnly: job.ForcedIntent != "opening");
    }

    private bool ProcessPlayerEventCourtArrival(string receiver, WorldDiplomacyDocument document)
    {
        var round = ResolveRound(document?.RoundId);
        if (!DiplomacyPlayerEventRules.IsPlayer(round) || !IsLiveRound(round)) return false;
        if (_host.IsPlayerAffiliatedParty(receiver)) { document.HasReachedPlayerCourt = true; return true; }
        if (receiver == document.AuthorKingdomId || !_host.HasIndependentAuthority(receiver) || !DialogueDocumentKnown(receiver, document.DocumentId)) return true;
        round.CourtKnowledgeReceipts ??= new(); round.CourtKnowledgeVersions ??= new(StringComparer.OrdinalIgnoreCase);
        round.CourtKnowledgeReceiptIndex ??= new(round.CourtKnowledgeReceipts, StringComparer.OrdinalIgnoreCase);
        string receipt = receiver + ":" + document.DocumentId;
        if (round.CourtKnowledgeReceiptIndex.Add(receipt))
        {
            round.CourtKnowledgeReceipts.Add(receipt);
            round.CourtKnowledgeVersions[receiver] = DiplomacyPlayerEventRules.KnowledgeRevision(round, receiver) + 1;
        }
        bool direct = GetDocumentTargetIds(document).Contains(receiver) || document.AddressedKingdomIds.Contains(receiver);
        bool answered = Storage.Documents.Any(x => x?.IsReadyForPublication == true && x.RoundId == round.RoundId
            && x.AuthorKingdomId == receiver && DocumentRespondsTo(x, document.DocumentId));
        if (answered) return true;
        var participant = round.Participants.FirstOrDefault(x => x.KingdomId == receiver);
        if (!direct)
        {
            if (participant?.State == "active" || document.RoundParticipation == "withdraw") return true;
            QueuePlayerCourtWork(round, receiver, document, "join_known_event");
        }
        else if (document.IsPlayerAuthored) RegisterPlayerResponseWork(document);
        else if (document.RequiresResponse) QueuePlayerCourtWork(round, receiver, document, "response");
        return true;
    }

    private void QueuePlayerCourtWork(WorldDiplomacyRound round, string receiver, WorldDiplomacyDocument source, string purpose)
    {
        if (!round.PlayerCourtWork.Any(x => x.KingdomId == receiver && x.SourceDocumentId == source.DocumentId))
            round.PlayerCourtWork.Add(new WorldDiplomacyPlayerCourtWork { KingdomId = receiver, SourceDocumentId = source.DocumentId, Purpose = purpose });
        MarkPlayerEventDirty(round);
        SchedulePlayerCourtWork(round);
    }

    private string BuildPlayerAgendaMaterial(string actor) => JsonConvert.SerializeObject(new {
        candidates = Storage.PlayerAgendaCandidates?.Where(x => x.KingdomId == actor),
        known_native_facts = Storage.NativeSignals?.Where(x => x != null && (x.SourceKingdomId == actor || x.TargetKingdomId == actor))
            .OrderByDescending(x => x.Day).Take(16),
        known_battle_facts = Storage.RecentBattles?.Where(x => x != null && (x.AttackerKingdomIds?.Contains(actor) == true || x.DefenderKingdomIds?.Contains(actor) == true))
            .OrderByDescending(x => x.Day).Take(16)
    });

    private void SchedulePlayerCourtWork(WorldDiplomacyRound round)
    {
        if (HasPlayerSpeakerWork(round)) return;
        var work = round.PlayerCourtWork.FirstOrDefault(x => x.Status == "pending");
        if (work == null) return;
        var source = ResolveDocument(work.SourceDocumentId);
        if (source?.IsReadyForPublication != true || _host.IsEliminatedParty(work.KingdomId)) { work.Status = "unavailable"; return; }
        if (!DialogueDocumentKnown(work.KingdomId, source.DocumentId)) return;
        var participant = round.Participants.FirstOrDefault(x => x.KingdomId == work.KingdomId);
        if (!source.ChangedDiplomaticState && (participant?.RefuseRepeatedUntilDay > _host.CurrentDay() || HasActiveDisputeRefusal(work.KingdomId)))
            EnqueuePlayerDecision(round, work.KingdomId, source.AuthorKingdomId, source, "reconsider_temporarily_refused_dispute");
        else if (work.Purpose == "join_known_event") EnqueuePlayerDecision(round, work.KingdomId, source.AuthorKingdomId, source, work.Purpose);
        else EnqueueGeneration(work.KingdomId, source.AuthorKingdomId, null, true, source, 95, roundId: round.RoundId);
        if (HasPlayerSpeakerWork(round)) work.Status = "queued";
    }

    private string CurrentPlayerAuthority()
    {
        string id = _host.PlayerKingdomId();
        return !string.IsNullOrEmpty(id) && DialogueHost?.IsPlayerRuler(_host.PartyRulerId(id)) == true ? id : "";
    }

    private void InitializeEventOwner(WorldDiplomacyRound round, bool player)
    {
        if (round == null) return;
        round.SchedulingOwner = player ? "player" : "normal";
        round.OwnershipEpoch = Math.Max(1, round.OwnershipEpoch);
        if (player && DiplomacyPlayerEventRules.ContainsParty(round, CurrentPlayerAuthority()))
        {
            round.PlayerAuthorityKingdomId = CurrentPlayerAuthority();
            round.PlayerAuthorityRulerId = _host.PartyRulerId(round.PlayerAuthorityKingdomId) ?? "";
        }
        else round.PlayerAuthorityKingdomId = round.PlayerAuthorityRulerId = "";
        round.PlayerNegotiationWaits ??= new();
        round.PlayerCourtWork ??= new();
        round.CourtKnowledgeReceipts ??= new(); round.CourtKnowledgeVersions ??= new(StringComparer.OrdinalIgnoreCase);
    }

    // One event changes epoch. Other event leases and the canonical history stay intact.
    private void TransferEvent(WorldDiplomacyRound round, bool player, string reason)
    {
        if (round == null) return;
        string destination = player ? "player" : "normal";
        if (round.SchedulingOwner == destination) return;
        RequestLeases.InvalidateRound(round.RoundId);
        round.OwnershipEpoch = Math.Max(1, round.OwnershipEpoch) + 1;
        InitializeEventOwner(round, player);
        round.ConversationRevision++;
        foreach (var job in Storage.Jobs.Where(x => x != null && (x.RoundId == round.RoundId || x.ExchangeId == round.RoundId)).ToList())
        {
            // Rekey before reusing a request: an in-flight old ID can never find the successor.
            job.JobId = _host.NewId("diplomacy_handoff");
            job.IsRunning = false;
            job.RequestAttempt = 0;
            job.LlmMessages?.Clear();
            job.SemanticRepairAttempts = 0;
            job.PersonalMemoryRulerId = "";
            if (IsPrivacyPipelineJob(job)) job.Kind = "generate";
            job.PrivacyPublicContext = ""; job.PrivacySourceMaterial = ""; job.PrivacyRevision = -1;
            job.RoundConversationRevision = round.ConversationRevision;
            DiplomacyPlayerEventRules.Bind(round, job);
        }
        if (!player)
        {
            foreach (var wait in round.PlayerNegotiationWaits) wait.RequiresAnswer = false;
            round.NpcInitiatedPlayerEvent = false;
            round.PlayerAuthorityKingdomId = round.PlayerAuthorityRulerId = "";
            round.IsPlayerInsertion = false;
            foreach (var participant in round.Participants)
            {
                participant.IsPlayerAsync = _host.IsPlayerParty(participant.KingdomId);
                participant.SelectedForRelay = participant.State != "withdrawn" && _host.HasIndependentAuthority(participant.KingdomId);
            }
            round.RelayWaiting = false;
        }
        _diplomacyWorkNeedsReconcile = true;
        _host.Log("diplomacy.event_handoff round=" + round.RoundId + " owner=" + destination
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
        foreach (var round in GetLiveRounds())
        {
            if (string.IsNullOrEmpty(round.SchedulingOwner))
                InitializeEventOwner(round, round.EventSourceType == "dialogue_commitment"
                    || DiplomacyPlayerEventRules.ContainsParty(round, authority)
                    || (round.IsPlayerInsertion && !string.IsNullOrEmpty(authority) && round.InitiatorKingdomId == authority));
            round.PlayerNegotiationWaits ??= new();
            round.PlayerCourtWork ??= new();
            MarkPlayerEventDirty(round);
        }
        foreach (var job in Storage.Jobs.Where(x => x != null))
            if (job.OwnershipEpoch == 0) DiplomacyPlayerEventRules.Bind(ResolveRound(job.RoundId), job);
        foreach (var round in GetLiveRounds().Where(DiplomacyPlayerEventRules.IsPlayer))
            foreach (var work in round.PlayerCourtWork.Where(x => x.Status == "queued"))
                if (!Storage.Jobs.Any(x => x.RoundId == round.RoundId && x.AuthorKingdomId == work.KingdomId && x.SourceDocumentId == work.SourceDocumentId))
                    work.Status = "pending";
        if (migration)
        {
            foreach (var round in GetLiveRounds().Where(DiplomacyPlayerEventRules.IsPlayer))
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
                && x.IsReadyForPublication && x.RequiresResponse && x.ActualPlayerNoticeHour < 0
                && DiplomacyPlayerEventRules.IsPlayer(ResolveRound(x.RoundId))
                && ResolveRound(x.RoundId).PlayerNegotiationWaits.Any(w => w.SourceDocumentId == x.DocumentId && string.IsNullOrEmpty(w.AnswerDocumentId))))
            { document.FormalNoticeShown = false; document.IsNotified = false; }
            Storage.PlayerDiplomacySchemaVersion = 1;
        }
        _waitingByDocument.Clear();
        foreach (var round in GetLiveRounds().Where(DiplomacyPlayerEventRules.IsPlayer))
            foreach (var wait in round.PlayerNegotiationWaits) IndexPlayerWait(round, wait);
    }

    private void MaintainPlayerAuthority()
    {
        string authority = CurrentPlayerAuthority();
        _pendingPlayerReplyRoutingIds.RemoveWhere(id => ResolveDocument(id)?.AuthorKingdomId != authority
            || ResolveDocument(id)?.PlayerReplyRoutingPending != true);
        foreach (var round in GetLiveRounds().Where(IsLiveRound).ToList())
        {
            if (!DiplomacyPlayerEventRules.IsPlayer(round)) continue;
            if (!string.IsNullOrEmpty(round.PlayerAuthorityKingdomId)
                && (authority != round.PlayerAuthorityKingdomId || _host.PartyRulerId(authority) != round.PlayerAuthorityRulerId))
            {
                // Ruler changes revoke every old request identity, even if the new player realm participates.
                bool retain = DiplomacyPlayerEventRules.ContainsParty(round, authority);
                TransferEvent(round, false, "player_authority_changed");
                if (retain) TransferEvent(round, true, "new_player_realm_participates");
            }
        }
    }

    internal int LaneDispatchPriority(WorldDiplomacyJob job, int day)
    {
        bool player = IsPlayerSchedulingJob(job);
        return (player == _preferPlayerDispatch ? 1000 : 0) + Math.Min(100, job.Priority + Math.Max(0, day - job.CreatedDay) * 5);
    }
    internal void RecordLaneDispatch(WorldDiplomacyJob job) => _preferPlayerDispatch = !IsPlayerSchedulingJob(job);

    private string BuildPlayerEventContinuity(WorldDiplomacyRound round, string author)
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
            + "\n" + JsonConvert.SerializeObject(new { round.RoundId, round.RoundTopic, round.RoundStatus,
                round.ExternalOpeningContext, round.TopicSeedContext, round.RelayRouteKingdomIds, round.RelayCursor,
                offers = offers.ToList(), participants = round.Participants.Select(x => new { x.KingdomId, x.State,
                    x.MandatoryReplyPending, x.LastTriggeredDocumentId }),
                recorded_intention = round.Participants.FirstOrDefault(x => x.KingdomId == author) });
    }
}
