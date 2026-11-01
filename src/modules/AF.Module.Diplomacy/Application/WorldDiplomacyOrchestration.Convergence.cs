using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static AnimusForge.Refactor.Domain.WorldDiplomacyStructureRules;

namespace AnimusForge;

internal interface IDiplomacyConvergenceHost
{
    int WarTendency();
    int DrawEndingRoute(int exclusiveMaximum);
}

internal sealed partial class WorldDiplomacyOrchestration
{
    private bool _schedulingFinalDecision;
    private static bool IsEnding(WorldDiplomacyRound round) => round?.EventConvergence?.Ending == true;

    // The admitted position is already durable on the participant. This also
    // covers acceptance of a pre-existing invitation during finite closure.
    private bool HasUnexpressedEndingAgenda(WorldDiplomacyRound round, string actor) =>
        round.Participants.Any(x => x.KingdomId == actor && x.State == "active" && !_host.IsPlayerParty(actor)
            && _host.HasIndependentAuthority(actor) && !_host.IsEliminatedParty(actor)
            && !string.IsNullOrWhiteSpace(x.Agenda) && string.IsNullOrEmpty(x.AgendaExpressionDocumentId)
            && x.AgendaAddressTo.Any(target => _host.HasIndependentAuthority(target) && !_host.IsEliminatedParty(target)
                && round.Participants.Any(p => p.KingdomId == target && p.State != "unavailable")));

    private IEnumerable<string> NegotiationSources(WorldDiplomacyDocument doc) =>
        (doc.AuditedAgendaAnswers ?? new()).Concat(doc.AnsweredNpcDocumentIds ?? new()).Concat(new[] { doc.SourceDocumentId, doc.DiscussionSourceDocumentId,
            doc.RespondingToOfferDocumentId, doc.RespondingToThreatDocumentId })
            .Concat((doc.Actions ?? new()).SelectMany(x => new[] { x.RespondingToOfferDocumentId, x.RespondingToThreatDocumentId }));

    private bool IsNewEndingDutyPublication(WorldDiplomacyRound round, WorldDiplomacyDocument doc)
    {
        if (!IsEnding(round)) return true;
        if (HasUnexpressedEndingAgenda(round, doc.AuthorKingdomId) && !string.IsNullOrWhiteSpace(doc.AuditedAgendaQuote)
            && (doc.Body ?? "").Contains(doc.AuditedAgendaQuote)) return true;
        if (doc.ChangedDiplomaticState || doc.EventExecutionPending || !string.IsNullOrEmpty(doc.DialogueArrangementId)) return true;
        var state = round.EventConvergence;
        if (state.Actor == doc.AuthorKingdomId && state.Target == doc.TargetKingdomId && string.IsNullOrEmpty(state.DecisionDocumentId)) return true;
        return NegotiationSources(doc).Any(id => IsEndingDutySource(round, doc.AuthorKingdomId, id)
            && !state.FinalDocumentIds.Select(ResolveDocument).Any(previous => previous != null
                && previous.AuthorKingdomId == doc.AuthorKingdomId && NegotiationSources(previous).Contains(id)));
    }

    private DiplomacyEventConvergence EnsureConvergence(WorldDiplomacyRound round)
    {
        var state = round.EventConvergence ??= new();
        state.Receipts ??= new(); state.RequiredSources ??= new(); state.FinalDocumentIds ??= new();
        state.RequiredJobIds ??= new();
        if (state.Version >= 1) return state;
        // One load/adoption migration, never a per-tick archive scan. Only published
        // same-case source chains count; earlier chats and model drafts do not.
        var root = ResolveDocument(round.RootDocumentId);
        if (root?.IsReadyForPublication != true) return state;
        DiplomacyConvergenceRules.Record(state, root, round.RootDocumentId, Array.Empty<string>());
        var published = Storage.Documents.Where(x => x.RoundId == round.RoundId && x.IsReadyForPublication)
            .OrderBy(x => x.Day).ThenBy(x => x.CreatedUtcTicks).ToList();
        // Legacy timestamps may tie or a source may predate creation. Four
        // bounded dependency passes recover the two claim/answer pairs without
        // trusting list order or manufacturing missing source documents.
        for (int pass = 0; pass < 4; pass++)
        {
            int before = state.Receipts.Count;
            foreach (var doc in published) DiplomacyConvergenceRules.Record(state, doc, round.RootDocumentId, NegotiationSources(doc));
            if (state.Receipts.Count == before) break;
        }
        state.Version = 1;
        if (!state.Ending)
        {
            var executed = published.LastOrDefault(x => HasConfirmedDocumentResult(x) || x.Actions?.Any(a => a.ChangedDiplomaticState) == true);
            if (executed != null)
            {
                state.ActualWar = published.Any(x => IsDirectWarResult(x) || x.Intent == "declare_war" && x.ChangedDiplomaticState
                    || x.Actions?.Any(a => a.Intent == "declare_war" && a.ChangedDiplomaticState) == true);
                state.Result = state.ActualWar ? "war_executed" : "action_executed";
            }
            else if (Storage.DiplomaticThreats.Any(x => x.StageRoundId == round.RoundId && x.Status is "complied" or "breached" or "enforced"))
                state.Result = "threat_settled";
            else if (published.Any(x => x.SilenceConsequenceSettled)) state.Result = "silence_concluded";
            else if (round.PendingOffers.Any(x => x.Status is "rejected" or "withdrawn")) state.Result = "agreement_not_reached";
        }
        return state;
    }

    private void RecordConvergencePublication(WorldDiplomacyRound round, WorldDiplomacyDocument document)
    {
        var state = EnsureConvergence(round);
        if (!string.IsNullOrEmpty(document.EndingRecoveryDocumentId)
            && state.TechnicalFailure == "war_execution_failed:" + document.EndingRecoveryDocumentId
            && document.ChangedDiplomaticState)
            ClearEndingExecutionHold(round);
        if (state.Ending)
        {
            if (round.Participants.Any(x => x.AgendaExpressionDocumentId == document.DocumentId)
                && !state.RequiredSources.Contains(document.DocumentId)) state.RequiredSources.Add(document.DocumentId);
            if (!state.FinalDocumentIds.Contains(document.DocumentId)) state.FinalDocumentIds.Add(document.DocumentId);
            if (document.AuthorKingdomId == state.Actor && document.TargetKingdomId == state.Target
                && string.IsNullOrEmpty(state.DecisionDocumentId)) state.DecisionDocumentId = document.DocumentId;
        }
        else DiplomacyConvergenceRules.Record(state, document, round.RootDocumentId, NegotiationSources(document));
        bool failedWar = document.Intent == "declare_war" && !document.ChangedDiplomaticState
            || document.Actions?.Any(x => x.Intent == "declare_war" && !x.ChangedDiplomaticState) == true;
        bool failedAgreement = round.PendingOffers.Any(x => x.Status is "execution_failed" or "partially_executed");
        if (failedWar || failedAgreement)
        {
            BeginEventEnding(round, "");
            HoldEndingExecution(round, failedWar ? "war_execution_failed:" + document.DocumentId : "agreement_execution_failed:" + document.DocumentId);
            return;
        }
        if (HasConfirmedDocumentResult(document) || document.Actions?.Any(x => x.ChangedDiplomaticState) == true)
        {
            bool war = IsDirectWarResult(document) || document.Intent == "declare_war" && document.ChangedDiplomaticState
                || document.Actions?.Any(x => x.Intent == "declare_war" && x.ChangedDiplomaticState) == true;
            state.ActualWar |= war;
            BeginEventEnding(round, war ? "war_executed" : "action_executed");
        }
        else if (document.SilenceConsequenceSettled) BeginEventEnding(round, "silence_concluded");
        else if (Storage.DiplomaticThreats.Any(x => x.StageRoundId == round.RoundId && x.Status != "open"
            && (x.ResolutionDocumentId == document.DocumentId || x.ComplianceDocumentId == document.DocumentId)))
            BeginEventEnding(round, "threat_settled");
        else if (round.PendingOffers.Any(x => x.Status is "rejected" or "withdrawn"
            && NegotiationSources(document).Contains(x.SourceDocumentId)))
            BeginEventEnding(round, "agreement_not_reached");
        else if (state.CompletedRounds >= DiplomacyConvergenceRules.OrdinaryRoundLimit)
            BeginEventEnding(round, "");
    }

    private void BeginEventEnding(WorldDiplomacyRound round, string result)
    {
        var state = EnsureConvergence(round);
        if (!string.IsNullOrEmpty(result) && state.Result != "war_executed"
            && (state.Result != "action_executed" || result == "war_executed")) state.Result = result;
        if (state.Ending) return;
        state.Ending = true;
        if (round.EventPhase == "closing")
        {
            if (Storage.EventAnnouncements.Any(x => x.EventId == round.RoundId && x.Stage == "ended"))
                HoldEndingExecution(round, "legacy_ending_has_staged_receipt_review_required");
            else
            {
                round.EventPhase = "discussing";
                foreach (var stale in Storage.Jobs.Where(x => x.RoundId == round.RoundId
                    && x.Kind is "event_boundary" or "event_boundary_audit" && x.ForcedIntent == "ended").ToList()) RemoveJob(stale.JobId);
            }
        }
        state.RequiredJobIds = Storage.Jobs.Where(x => x.RoundId == round.RoundId && !IsEventProgressJob(x)).Select(x => x.JobId).ToList();
        state.RequiredSources = round.PlayerResponses.Where(x => x.Status == "pending").Select(x => x.SourceDocumentId)
            .Concat(round.PlayerCourtWork.Where(x => x.Status is "pending" or "queued").Select(x => x.SourceDocumentId))
            .Concat(round.PlayerNegotiationWaits.Where(x => x.RequiresAnswer).Select(x => x.SourceDocumentId))
            .Concat(Storage.Jobs.Where(x => x.RoundId == round.RoundId && !IsEventProgressJob(x)).Select(x => x.SourceDocumentId))
            .Concat(round.Participants.Where(x => x.AgendaAnswerDocumentIds.Count == 0).Select(x => x.AgendaExpressionDocumentId))
            .Where(x => !string.IsNullOrEmpty(x)).Distinct(StringComparer.Ordinal).ToList();
        // Revoke the old discretionary continuation, including an in-flight
        // reviewer. Existing job identity/lease guards reject its late callback.
        foreach (var job in Storage.Jobs.Where(x => x.RoundId == round.RoundId && IsEventProgressJob(x)).ToList()) RemoveJob(job.JobId);
        round.EventWait = null; round.ConversationRevision++;
        _host.Log("diplomacy.ending_enter event=" + round.RoundId + " completed_rounds=" + state.CompletedRounds + " result=" + result);
        MarkEventDirty(round);
    }

    private bool IsEndingDutySource(WorldDiplomacyRound round, string actor, string sourceId)
    {
        if (!IsEnding(round)) return true;
        var state = round.EventConvergence;
        return state.RequiredSources.Contains(sourceId)
            || round.PendingOffers.Any(x => x.Status == "open" && x.SourceDocumentId == sourceId
                && (x.TargetKingdomId == actor || x.ProposerKingdomId == actor))
            || Storage.DiplomaticThreats.Any(x => x.StageRoundId == round.RoundId && x.Status == "open"
                && (x.IssuerKingdomId == actor || x.TargetKingdomId == actor)
                && (x.StageDocumentId == sourceId || x.TargetDecisionDocumentId == sourceId));
    }

    private bool IsEndingBlockingJob(WorldDiplomacyRound round, WorldDiplomacyJob job)
    {
        if (!IsEnding(round)) return true;
        var state = round.EventConvergence;
        return !string.IsNullOrEmpty(job.DialogueArrangementId) || state.RequiredJobIds.Contains(job.JobId)
            || HasUnexpressedEndingAgenda(round, job.AuthorKingdomId)
            || job.Kind is "event_boundary" or "event_boundary_audit"
            || IsEndingDutySource(round, job.AuthorKingdomId, job.SourceDocumentId)
            || !string.IsNullOrEmpty(state.Actor) && state.Actor == job.AuthorKingdomId && state.Target == job.TargetKingdomId
                && string.IsNullOrEmpty(state.DecisionDocumentId);
    }

    private static bool IsEndingDelivery(WorldDiplomacyRound round, string documentId) => !IsEnding(round)
        || round.EventConvergence.RequiredSources.Contains(documentId)
        || round.EventConvergence.FinalDocumentIds.Contains(documentId)
        || round.EventConvergence.Receipts.Any(x => x.DocumentId == documentId);

    private bool HasUnfinishedEndingObligations(WorldDiplomacyRound round) =>
        round.PendingOffers.Any(x => x.Status is "open" or "execution_failed" or "partially_executed" or "execution_uncertain")
        || Storage.DiplomaticThreats.Any(x => x.StageRoundId == round.RoundId && x.Status == "open")
        || Storage.Documents.Any(x => x.RoundId == round.RoundId && x.EventExecutionPending)
        || Storage.DialogueArrangements.Any(x => x.RoundId == round.RoundId && AnimusForge.DiplomacyDialogue.DiplomacyCommitmentRules.IsPending(x))
        || Storage.AidRequests.Any(x => x.RoundId == round.RoundId && x.Status == "accepted");

    private bool AllowEndingDeclaration(WorldDiplomacyRound round, string actor, WorldDiplomacyDocument source)
    {
        if (!IsEnding(round) || _schedulingFinalDecision) return true;
        if (HasUnexpressedEndingAgenda(round, actor)) return true;
        if (source == null || !IsEndingDutySource(round, actor, source.DocumentId)) return false;
        // Each pre-existing duty gets its actual answer, not an unbounded answer
        // to that answer. Political final decisions have their own stage receipt.
        return !round.EventConvergence.FinalDocumentIds.Select(ResolveDocument).Any(x => x != null
            && x.AuthorKingdomId == actor && DocumentRespondsTo(x, source.DocumentId));
    }

    private bool IsFreshOrdinaryContinuation(WorldDiplomacyRound round, string actor, WorldDiplomacyDocument source)
    {
        if (source == null || source.AuthorKingdomId == actor) return false;
        var state = EnsureConvergence(round);
        if (state.Ending || state.CompletedRounds >= DiplomacyConvergenceRules.OrdinaryRoundLimit) return false;
        state.ReceiptIndex ??= state.Receipts.ToDictionary(x => x.DocumentId, StringComparer.Ordinal);
        if (!state.ReceiptIndex.ContainsKey(source.DocumentId)) return false;
        return !state.Receipts.Any(x => x.Author == actor && x.SourceId == source.DocumentId);
    }

    // Runs on the existing dirty-event queue and bounded hourly round slices.
    // It is before reply scheduling, so new requests cannot starve transition.
    private bool AdvanceEventEnding(WorldDiplomacyRound round)
    {
        if (round != null) RecoverSettledSilence(round);
        if (!IsLiveRound(round) || !DiplomacyEventRules.IsEvent(round) || !EventBoundaryComplete(round, "started")) return false;
        var state = EnsureConvergence(round);
        if (!state.Ending && (state.CompletedRounds >= DiplomacyConvergenceRules.OrdinaryRoundLimit || !string.IsNullOrEmpty(state.Result)
            || round.EventPhase == "closing" && HasUnfinishedEndingObligations(round))) BeginEventEnding(round, "");
        if (!state.Ending) return false;
        if (round.EventPhase == "closing" && round.Participants.Any(x => HasUnexpressedEndingAgenda(round, x.KingdomId)))
        {
            if (Storage.EventAnnouncements.Any(x => x.EventId == round.RoundId && x.Stage == "ended"))
            { HoldEndingExecution(round, "ending_core_agenda_has_staged_receipt_review_required"); return true; }
            round.EventPhase = "discussing"; round.ConversationRevision++;
            foreach (var stale in Storage.Jobs.Where(x => x.RoundId == round.RoundId
                && x.Kind is "event_boundary" or "event_boundary_audit" && x.ForcedIntent == "ended").ToList()) RemoveJob(stale.JobId);
        }
        TryRecoverEndingExecution(round);
        if (!string.IsNullOrEmpty(round.TechnicalHoldReason) || !string.IsNullOrEmpty(state.TechnicalFailure)) return true;
        if (round.EventPhase == "closing")
        {
            if (!HasUnfinishedEndingObligations(round)) EnsureEventBoundary(round, "ended");
            return true;
        }
        if (Storage.Documents.Any(x => x.RoundId == round.RoundId && x.EventExecutionPending)
            || Storage.DialogueArrangements.Any(x => x.RoundId == round.RoundId && AnimusForge.DiplomacyDialogue.DiplomacyCommitmentRules.IsPending(x))
            || Storage.AidRequests.Any(x => x.RoundId == round.RoundId && x.Status == "accepted")) return true;
        if (Storage.Jobs.Any(x => x.RoundId == round.RoundId && x.Kind != "compress"
            && IsEndingBlockingJob(round, x) && !IsSideQuestionJob(round, x))) return true;

        foreach (var open in Storage.DiplomaticThreats.Where(x => x.StageRoundId == round.RoundId && x.Status == "open"))
        {
            string invalid = _host.ValidateOpenThreatWorldEligibility(open);
            if (!string.IsNullOrEmpty(invalid))
            {
                if (string.IsNullOrEmpty(state.Route)) state.Route = "existing_threat";
                state.Result = "route_unavailable";
                WorldDiplomacyRoundLifecycleRules.InvalidateThreatForNormalization(open, invalid, _host.CurrentDay());
                _host.Log("diplomacy.ending_obligation_unavailable event=" + round.RoundId + " threat=" + open.ThreatId + " reason=" + invalid);
            }
        }
        bool hadOpenOffer = round.PendingOffers.Any(x => x.Status == "open");
        PruneInvalidOffers(round);
        if (hadOpenOffer && !round.PendingOffers.Any(x => x.Status == "open"))
        {
            if (string.IsNullOrEmpty(state.Route)) state.Route = "existing_agreement";
            state.Result = "route_unavailable";
        }

        // Existing threats and offers take precedence over selection, and each
        // retains its original source, target, stage, consent and execution owner.
        var threat = Storage.DiplomaticThreats.FirstOrDefault(x => x.StageRoundId == round.RoundId && x.Status == "open");
        if (threat != null)
        {
            if (string.IsNullOrEmpty(state.Route)) state.Route = "existing_threat";
            bool refusal = threat.TargetDecision == "noncomplied";
            QueueEndingDecision(round, refusal ? threat.IssuerKingdomId : threat.TargetKingdomId,
                refusal ? threat.TargetKingdomId : threat.IssuerKingdomId,
                ResolveDocument(refusal ? threat.TargetDecisionDocumentId : threat.StageDocumentId));
            return true;
        }
        var offer = round.PendingOffers.FirstOrDefault(x => x.Status == "open");
        if (offer != null)
        {
            if (string.IsNullOrEmpty(state.Route)) state.Route = "existing_agreement";
            QueueEndingDecision(round, offer.TargetKingdomId, offer.ProposerKingdomId, ResolveDocument(offer.SourceDocumentId));
            return true;
        }
        if (round.PendingOffers.Any(x => x.Status is "execution_failed" or "partially_executed"))
        { HoldEndingExecution(round, "agreement_execution_failed"); return true; }
        // Necessary answers and their delivery finish, without introducing new
        // countries or extra last-word speeches.
        if (ScheduleUnexpressedPosition(round) || EventHasPendingWork(round)
            || round.Participants.Any(x => HasUnexpressedEndingAgenda(round, x.KingdomId))) return true;
        if (!string.IsNullOrEmpty(state.Result)) { FinishEventEnding(round, state.Result); return true; }
        if (state.Route is "existing_threat" or "existing_agreement")
        { FinishEventEnding(round, state.Route == "existing_threat" ? "threat_settled" : "agreement_not_reached"); return true; }
        if (string.IsNullOrEmpty(state.Route)) SelectEventEnding(round);
        if (state.Route == "deadlock") { FinishEventEnding(round, "deadlock"); return true; }
        if (!string.IsNullOrEmpty(state.DecisionDocumentId))
        {
            // A final attempt which produced no new duty is a genuine refusal
            // or retreat. Execution errors are held separately at publication.
            FinishEventEnding(round, state.Route == "agreement" ? "agreement_not_reached" : "threat_settled");
            return true;
        }
        if (!_host.HasIndependentAuthority(state.Actor) || !_host.HasIndependentAuthority(state.Target)
            || _host.IsEliminatedParty(state.Actor) || _host.IsEliminatedParty(state.Target)
            || !new WorldDiplomacyActionSelectionApplication(_host.ActionSelection()).BuildPotentialDiplomaticActionIntents(state.Actor, state.Target).Contains(state.Intent))
        { FinishEventEnding(round, "route_unavailable"); return true; }
        var source = state.Receipts.AsEnumerable().Reverse().Select(x => ResolveDocument(x.DocumentId))
            .FirstOrDefault(x => x != null && DialogueDocumentKnown(state.Actor, x.DocumentId));
        QueueEndingDecision(round, state.Actor, state.Target, source);
        return true;
    }

    private bool HasBoundEndingJob(WorldDiplomacyRound round, WorldDiplomacyJob job, string author)
        => job != null && job.AuthorKingdomId == author
            && HasBoundEndingDecision(round, author, job.TargetKingdomId, job.SourceDocumentId);

    internal bool CanPublishBoundEndingDecision(WorldDiplomacyRound round, WorldDiplomacyDocument document)
        => document?.Actions?.Count > 0 && document.Actions.All(x => x != null
            && HasBoundEndingDecision(round, document.AuthorKingdomId, x.TargetKingdomId, document.SourceDocumentId));

    private bool HasBoundEndingDecision(WorldDiplomacyRound round, string author, string target, string sourceId)
    {
        if (!IsEnding(round) || _host.IsPlayerParty(author)
            || !round.Participants.Any(x => x.KingdomId == author && x.State is "active" or "withdrawn" or "observer")) return false;
        var source = ResolveDocument(sourceId);
        if (source?.IsReadyForPublication != true || source.RoundId != round.RoundId
            || !DialogueDocumentKnown(author, source.DocumentId)) return false;
        if (!_host.HasIndependentAuthority(author) || !_host.CanAiAuthorParty(author, out _)
            || !_host.HasIndependentAuthority(target) || _host.IsEliminatedParty(target)) return false;
        if (Storage.DiplomaticThreats.Any(x => x.StageRoundId == round.RoundId && x.Status == "open"
            && (x.TargetDecision == "noncomplied"
                ? x.IssuerKingdomId == author && x.TargetKingdomId == target && x.TargetDecisionDocumentId == source.DocumentId
                : x.TargetKingdomId == author && x.IssuerKingdomId == target && x.StageDocumentId == source.DocumentId))) return true;
        if (round.PendingOffers.Any(x => x.Status == "open" && x.TargetKingdomId == author
            && x.ProposerKingdomId == target && x.SourceDocumentId == source.DocumentId)) return true;
        var state = round.EventConvergence;
        return state.Actor == author && state.Target == target && string.IsNullOrEmpty(state.DecisionDocumentId)
            && string.IsNullOrEmpty(state.Result) && state.Route is "threat" or "agreement"
            && state.Receipts.Any(x => x.DocumentId == source.DocumentId);
    }

    private void QueueEndingDecision(WorldDiplomacyRound round, string actor, string target, WorldDiplomacyDocument source)
    {
        if (_host.IsPlayerParty(actor)) return; // Existing notice and silence clock own the human turn.
        if (!_host.HasIndependentAuthority(actor) || _host.IsEliminatedParty(actor)
            || !_host.HasIndependentAuthority(target) || _host.IsEliminatedParty(target))
        { MarkEventDirty(round); return; } // Original obligation owner invalidates objective impossibility.
        if (source == null) { HoldEndingExecution(round, "final_decision_source_missing"); return; }
        if (!DialogueDocumentKnown(actor, source.DocumentId)) return;
        if (round.EventConvergence.FinalDocumentIds.Select(ResolveDocument).Any(x => x != null
            && x.AuthorKingdomId == actor && DocumentRespondsTo(x, source.DocumentId)))
        { HoldEndingExecution(round, "final_decision_did_not_settle_bound_obligation:" + source.DocumentId); return; }
        try
        {
            _schedulingFinalDecision = true;
            EnqueueGeneration(actor, target, null, true, source, 100, roundId: round.RoundId);
        }
        finally { _schedulingFinalDecision = false; }
    }

    private void SelectEventEnding(WorldDiplomacyRound round)
    {
        var state = round.EventConvergence;
        var root = ResolveDocument(round.RootDocumentId);
        var actor = state.Receipts.AsEnumerable().Reverse().Select(x => x.Author).Append(round.InitiatorKingdomId)
            .FirstOrDefault(x => !_host.IsPlayerParty(x) && _host.HasIndependentAuthority(x)
                && !_host.IsEliminatedParty(x) && round.Participants.Any(p => p.KingdomId == x && p.State == "active"));
        var lastActorDocument = state.Receipts.AsEnumerable().Reverse().Where(x => x.Author == actor)
            .Select(x => ResolveDocument(x.DocumentId)).FirstOrDefault(x => x != null);
        string target = lastActorDocument?.TargetKingdomId;
        if (string.IsNullOrEmpty(target)) target = actor == root?.AuthorKingdomId ? root.TargetKingdomId : root?.AuthorKingdomId;
        if (string.IsNullOrEmpty(actor) || string.IsNullOrEmpty(target) || actor == target
            || !_host.HasIndependentAuthority(target) || _host.IsEliminatedParty(target))
        { state.Route = "deadlock"; return; }
        state.Actor = actor; state.Target = target;
        var world = _host.PromptWorld();
        var war = world.WarSituation(actor, target) ?? new();
        var relation = world.RelationProfile(actor, target) ?? new();
        var enemies = world.CurrentWarKingdomIds(actor) ?? Array.Empty<string>();
        bool common = enemies.Intersect(world.CurrentWarKingdomIds(target) ?? Array.Empty<string>()).Any();
        var legal = new WorldDiplomacyActionSelectionApplication(_host.ActionSelection()).BuildLegalDiplomaticActionIntents(round, actor, target);
        state.Intent = _host.PartiesAtWar(actor, target) && legal.Contains("propose_peace") ? "propose_peace"
            : common && legal.Contains("propose_alliance") ? "propose_alliance"
            : legal.Contains("propose_trade") && HasRecordedTradeInterest(round) ? "propose_trade" : "";
        var host = _host as IDiplomacyConvergenceHost;
        var weights = DiplomacyConvergenceRules.Weights(host?.WarTendency() ?? 50, relation.AverageRelation,
            world.RulerRelation(actor, target), war.AuthorStrength, war.TargetStrength, war.AuthorOtherWars, common,
            _host.PartiesAllied(actor, target), _host.TradeAgreementExists(actor, target),
            !_host.PartiesAtWar(actor, target) && legal.Contains("warning") && legal.Contains("declare_war"), !string.IsNullOrEmpty(state.Intent));
        state.ThreatWeight = weights.threat; state.AgreementWeight = weights.agreement; state.DeadlockWeight = weights.deadlock;
        // The persisted draw and route are committed together on the owner thread.
        state.Draw = host?.DrawEndingRoute(weights.threat + weights.agreement + weights.deadlock) ?? 0;
        state.Route = DiplomacyConvergenceRules.Select(weights, state.Draw);
        if (state.Route == "threat") state.Intent = "warning";
        round.ConversationRevision++;
        _host.Log("diplomacy.ending_route event=" + round.RoundId + " route=" + state.Route + " weights="
            + state.ThreatWeight + "/" + state.AgreementWeight + "/" + state.DeadlockWeight + " draw=" + state.Draw);
    }

    private static readonly Lazy<string[]> RecordedTradeInterestCues = new(() =>
        DiplomacyCompatibilityTexts.Get("Event_TradeInterestCues").Split('|'));

    private static bool HasRecordedTradeInterest(WorldDiplomacyRound round)
    {
        if (round.PotentialActionIntents.Contains("propose_trade")) return true;
        // Existing audited interests only, not world-book search, hidden history
        // or new model extraction. The declaration auditor still verifies that
        // the actual final terms serve these interests rather than invent trade.
        string interests = string.Join("\n", round.Participants.SelectMany(x => new[] { x.Agenda, x.PreferredOutcome, x.Alternative }));
        return RecordedTradeInterestCues.Value
            .Any(x => interests.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void HoldEndingExecution(WorldDiplomacyRound round, string reason)
    {
        if (round.EventConvergence.TechnicalFailure == reason) return;
        round.EventConvergence.TechnicalFailure = reason;
        HoldEventForInvalidDraft(round, "event_ending_execution:" + reason);
        _host.Log("diplomacy.ending_technical_hold event=" + round.RoundId + " reason=" + reason);
    }

    private void FinishEventEnding(WorldDiplomacyRound round, string result)
    {
        var state = round.EventConvergence;
        state.Result = result;
        round.EventOutcome = state.ActualWar ? "escalated" : result == "action_executed" ? "resolved" : "deadlocked";
        round.EventOutcomeReason = DiplomacyPromptTexts.Get("EndingResult_" + result, result);
        round.EventPhase = "closing"; round.EventCloseReason = "event_" + round.EventOutcome;
        round.EventClosureReviewVersion = 1; round.ConversationRevision++;
        _host.Log("diplomacy.ending_result event=" + round.RoundId + " route=" + state.Route + " result=" + result
            + " actual_war=" + state.ActualWar);
        CloseRound(round.EventCloseReason, round);
    }

    private List<string> FilterEndingIntents(WorldDiplomacyRound round, string actor, string target, List<string> intents)
    {
        if (!IsEnding(round)) return intents;
        var state = round.EventConvergence;
        var allowed = new HashSet<string>(new[] { "statement", "condemn", "apology", "concession", "withdraw_offer" }, StringComparer.Ordinal);
        if (state.TechnicalFailure.StartsWith("war_execution_failed:", StringComparison.Ordinal))
        {
            var failed = ResolveDocument(state.TechnicalFailure.Substring("war_execution_failed:".Length));
            if (failed?.AuthorKingdomId == actor && failed.TargetKingdomId == target && CanRecoverEndingWar(failed))
                allowed.Add("declare_war");
        }
        foreach (var intent in intents.Where(x => x.StartsWith("accept_", StringComparison.Ordinal) || x.StartsWith("reject_", StringComparison.Ordinal))) allowed.Add(intent);
        // Preserve the existing notified, overdue human-silence policy. This
        // does not classify silence or invent consent; its original audited
        // consequence and exact source are still bound by BindSilenceConsequence.
        if (_host.IsPlayerParty(target) && round.PlayerNegotiationWaits.Any(x =>
            DiplomacyEventRules.SilenceDue(x, _host.CurrentCampaignHours())
            && ResolveDocument(x.SourceDocumentId)?.AuthorKingdomId == actor)) allowed.Add("declare_war");
        foreach (var threat in WorldDiplomacyThreatScope.For(Storage, actor, round.RoundId))
        {
            if (threat.TargetKingdomId == actor && threat.IssuerKingdomId == target && threat.TargetDecision == "pending")
            { allowed.Add("comply_ultimatum"); if (!intents.Contains("comply_ultimatum")) intents.Add("comply_ultimatum"); }
            if (threat.IssuerKingdomId == actor && threat.TargetKingdomId == target && threat.TargetDecision == "noncomplied")
                allowed.Add(threat.Stage == "warning" ? "ultimatum" : "declare_war");
        }
        if (state.Actor == actor && state.Target == target && string.IsNullOrEmpty(state.DecisionDocumentId)
            && !string.IsNullOrEmpty(state.Intent) && string.IsNullOrEmpty(state.Result)) allowed.Add(state.Intent);
        return intents.Where(allowed.Contains).ToList();
    }

    private string BuildEndingContext(WorldDiplomacyRound round) => round == null ? "" :
        DiplomacyPromptTexts.Get("EventEndingContract", "") + "\n" + DiplomacyPromptTexts.Get("EventEndingCoreAgenda", "") + "\n" + JsonConvert.SerializeObject(new {
            ordinary_round_limit = DiplomacyConvergenceRules.OrdinaryRoundLimit,
            unexpressed_core_kingdoms = round.Participants.Where(x => HasUnexpressedEndingAgenda(round, x.KingdomId)).Select(x => x.KingdomId),
            convergence = round.EventConvergence,
            existing_threats = Storage.DiplomaticThreats.Where(x => x.StageRoundId == round.RoundId)
                .Select(x => new { x.Stage, x.Status, x.StageDocumentId, x.TargetDecision, x.TargetDecisionDocumentId, x.ResolutionDocumentId }) });

    private bool ValidateEndingDraft(WorldDiplomacyJob job, JObject draft, out string reason)
    {
        reason = "";
        var round = ResolveRound(job.RoundId);
        if (!IsEnding(round) || !string.IsNullOrEmpty(job.DialogueArrangementId)) return true;
        var rows = (draft?["actions"] as JArray)?.OfType<JObject>().ToList() ?? new();
        if (rows.Count == 0 && draft != null) rows.Add(draft);
        foreach (var action in rows)
        {
            string intent = WorldDiplomacyIntentVocabulary.NormalizeIntent((string)action["intent"]);
            string target = (string)action["target_kingdom_id"] ?? (string)action["primary_target_kingdom_id"] ?? (string)action["target"] ?? job.TargetKingdomId;
            if (FilterEndingIntents(round, job.AuthorKingdomId, target, new() { intent }).Count == 0)
            { reason = "event_ending_route_locked:action=" + intent + ";finish_original_obligation_or_selected_route;no_new_chain_or_counteroffer"; return false; }
            if (action["requires_response"]?.Value<bool>() == true && intent is not "warning" and not "ultimatum"
                && !intent.StartsWith("propose_", StringComparison.Ordinal))
            { reason = "event_ending_no_extra_action_reply:set_requires_response_false"; return false; }
        }
        if ((draft?["response_requests"] as JArray)?.Count > 0)
        { reason = "event_ending_no_new_questions_or_invitations:answer_current_duty_only;use_empty_response_requests"; return false; }
        var state = round.EventConvergence;
        if (state.Actor == job.AuthorKingdomId && state.Target == job.TargetKingdomId && state.Route == "threat"
            && string.IsNullOrEmpty(state.DecisionDocumentId) && !rows.Any(x => (string)x["intent"] == "warning"))
        { reason = "event_ending_selected_warning_required:use_warning_not_condemn;do_not_choose_another_route"; return false; }
        bool createsDuty = rows.Any(x => (string)x["intent"] is "warning" or "ultimatum"
            || ((string)x["intent"] ?? "").StartsWith("propose_", StringComparison.Ordinal));
        bool firstCoreToPlayer = HasUnexpressedEndingAgenda(round, job.AuthorKingdomId) && _host.IsPlayerParty(job.TargetKingdomId)
            && round.Participants.Any(x => x.KingdomId == job.AuthorKingdomId && x.AgendaAddressTo.Contains(job.TargetKingdomId));
        if (!createsDuty && !firstCoreToPlayer && draft?["requires_response"]?.Value<bool>() == true)
        { reason = "event_ending_no_extra_last_word:set_requires_response_false_unless_issuing_the_selected_threat_or_proposal"; return false; }
        var boundOffer = round.PendingOffers.FirstOrDefault(x => x.Status == "open" && x.TargetKingdomId == job.AuthorKingdomId
            && x.ProposerKingdomId == job.TargetKingdomId && x.SourceDocumentId == job.SourceDocumentId);
        if (boundOffer != null && string.IsNullOrEmpty(job.SilenceSourceDocumentId)
            && !rows.Any(x => ((string)x["intent"] ?? "").StartsWith("accept_", StringComparison.Ordinal)
            || ((string)x["intent"] ?? "").StartsWith("reject_", StringComparison.Ordinal)))
        { reason = "event_ending_offer_needs_explicit_accept_or_reject:counterconditions_or_refusal_must_reject_original_offer;no_new_offer"; return false; }
        return true;
    }
}
