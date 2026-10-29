using System;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Optional host preparation: game identities stay on the main thread while
// immutable lore candidate selection runs on its existing worker.
internal interface IWorldDiplomacyRealmVoicePreparation
{
    bool PrepareRealmVoice(string kingdomId);
}

internal sealed partial class WorldDiplomacyOrchestration
{
    internal WorldDiplomacyRequestLeaseCoordinator RequestLeases { get; } = new(3);
    internal bool IsPlayerSchedulingJobForDispatch(WorldDiplomacyJob job) => IsPlayerSchedulingJob(job);
    private long LaneRetryAfter(string lane) => lane == "player" ? Storage.PlayerServiceRetryAfterUtcTicks
        : lane == "oral" ? Storage.OralServiceRetryAfterUtcTicks : Storage.ServiceRetryAfterUtcTicks;
    internal bool CanDispatchDiplomacyJob(WorldDiplomacyJob job)
    {
        if (IsUnsolicitedCourtJoinJob(job)) return false;
        if (job != null && !string.IsNullOrEmpty(job.SuspendedReason)) TryResumeTechnicalRequest(job);
        if (job == null || job.IsRunning || RequestLeases.ContainsJob(job.JobId)
            || job.ContactNotBeforeDay > _host.CurrentDay()
            || job.MaterialNextSearchDay > _host.CurrentDay()
            || !string.IsNullOrEmpty(job.SuspendedReason)
            || job.RetryAfterUtcTicks > DateTime.UtcNow.Ticks
            || LaneRetryAfter(RequestLane(job)) > DateTime.UtcNow.Ticks) return false;
        if (IsSpeechReviewJob(job) && !IsCurrentSpeechReview(job)) return false;
        var owningRound = ResolveRound(job.RoundId);
        if (IsEventPlanningJob(job) && IsPlayerDeclarationOpening(owningRound)
            && ResolveDocument(owningRound.RootDocumentId)?.PlayerReplyRoutingPending == true) return false;
        if (owningRound != null && RequestLeases.ContainsSpeech(owningRound.RoundId)) return false;
        if (owningRound != null && (job.Kind == "generate" || IsPrivacyPipelineJob(job))
            && (EventHost != null && IsPlayerDeclarationOpening(owningRound) || Storage.Jobs.Any(x => x.RoundId == owningRound.RoundId && x.ForcedIntent == "started"
                && x.Kind is "event_boundary" or "event_boundary_audit"))
            && !EventBoundaryComplete(owningRound, "started")) return false;
        if (owningRound != null && !DiplomacyEventRules.Matches(owningRound, job)) return false;
        if (!string.IsNullOrEmpty(job.SilenceSourceDocumentId) && !IsCurrentSilenceJob(job)) return false;
        if (!CanDispatchPlayerResponse(job) || !Storage.RequestBudget.CanAdmit(RequestLane(job))) return false;
        if (job.Kind == "generate" || IsEventPipelineJob(job))
        {
            if (job.IsExternalResponseOnly && !string.IsNullOrWhiteSpace(job.SourceDocumentId)
                && !DialogueDocumentKnown(job.AuthorKingdomId, job.SourceDocumentId)) return false;
            string id = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(job.RoundId, job.ExchangeId);
            if (RequestLeases.ContainsSpeech(id)) return false;
            if (!string.IsNullOrWhiteSpace(id) && !IsLiveRound(ResolveRound(id))) return false;
        }
        return true;
    }
    internal void PrepareSharedRequest(WorldDiplomacyJob job)
    {
        job.RequestPreparationPending = false;
        try { PrepareSharedRequestCore(job); }
        catch (WorldDiplomacyPreparationPendingException) { job.RequestPreparationPending = true; }
        catch (WorldDiplomacyPreparationException ex) { SuspendDiplomacyJob(job, ex.Message); }
    }
    private void PrepareSharedRequestCore(WorldDiplomacyJob job)
    {
        job.RequestLane = RequestLane(job);
        if (job.Kind is "generate" or "player_decision" or "contact_wait"
            && _host is IWorldDiplomacyRealmVoicePreparation preparation)
        {
            job.RequestPreparationPending = false;
            try
            {
                if (!preparation.PrepareRealmVoice(job.AuthorKingdomId))
                { job.RequestPreparationPending = true; return; }
            }
            catch (Exception ex)
            { SuspendDiplomacyJob(job, "realm_voice_preparation_failed:" + ex.GetType().Name); return; }
        }
        if (PrepareEventPipelineRequest(job)) return;
        // Refresh restored jobs and manual retries at the main-thread request boundary.
        if (job.Kind == "analyze" || (job.Kind == "generate" && IsEventJob(job))) job.MaxTokens = _host.AnalysisMaxTokens();
        var round = ResolveRound(job.RoundId);
        bool frozenRepair = job.SemanticRepairAttempts > 0 && round != null && job.RoundConversationRevision == round.ConversationRevision;
        if (IsOralDeclarationJob(job))
        {
            RebuildOralDeclarationJob(job);
            job.RequestLane = round?.BirthLane ?? "oral";
            job.PersonalMemoryRulerId = _host.PartyRulerId(job.AuthorKingdomId);
            job.RoundConversationRevision = round?.ConversationRevision ?? 0;
            if (round != null) job.UserPrompt += "\n" + BuildEventContinuity(round, job.AuthorKingdomId);
            PrepareDiplomacyPrivacy(job);
            return;
        }
        if (frozenRepair) return;
        PrepareSharedEventRequest(job);
        if (job.Kind == "analyze") job.SystemPrompt += "\n" + DiplomacyPromptTexts.Get("ResponseRequestContract", "");
        job.RequestLane = RequestLane(job);
        job.PersonalMemoryRulerId = _host.PartyRulerId(job.AuthorKingdomId);
        if (job.Kind == "generate")
        {
            if (DiplomacyEventRules.IsEvent(round)) job.UserPrompt += "\n" + BuildEventContinuity(round, job.AuthorKingdomId);
            PrepareDiplomacyPrivacy(job);
        }
    }
    internal bool AdmitCompletion(WorldDiplomacyJob job, LlmJobResult result)
    {
        if (!_host.WorldDiplomacyEnabled()) { job.IsRunning = false; return false; }
        if (result.RequestAttempt > 0 && result.RequestAttempt != job.RequestAttempt) return false;
        job.IsRunning = false;
        if (IsOralDeclarationJob(job) && !IsCurrentOralDeclarationJob(job))
        { RemoveJob(job.JobId); return false; }
        if (result.Error == "world_diplomacy_request_budget_deferred") return false;
        var round = ResolveRound(job.RoundId);
        if (round != null && (result.OwnershipEpoch != round.OwnershipEpoch || result.SchedulingOwner != round.SchedulingOwner
            || result.PlayerAuthorityRulerId != round.PlayerAuthorityRulerId || !DiplomacyEventRules.Matches(round, job)))
        { return false; }
        if (IsUnsolicitedCourtJoinJob(job)) { RetireUnsolicitedCourtJoins(round); return false; }
        if (!string.IsNullOrEmpty(job.SilenceSourceDocumentId) && !IsCurrentSilenceJob(job))
        { RemoveJob(job.JobId); return false; }
        if (IsSpeechReviewJob(job) && !IsCurrentSpeechReview(job)) { RemoveJob(job.JobId); return false; }
        if (!IsSpeechReviewJob(job) && (job.Kind == "generate" || IsEventPipelineJob(job)) && (!IsLiveRound(round)
            || result.RoundConversationRevision != round.ConversationRevision
            || job.CourtKnowledgeRevision != DiplomacyEventRules.KnowledgeRevision(round, job.AuthorKingdomId)
            || (!string.IsNullOrEmpty(job.PersonalMemoryRulerId) && !DialogueRulerIsCurrent(job.AuthorKingdomId, job.PersonalMemoryRulerId))))
        {
            job.LlmMessages?.Clear(); job.SemanticRepairAttempts = 0;
            if (job.Kind == "event_boundary_audit") { job.Kind = "event_boundary"; job.PrivacyDraft = ""; }
            if (job.Kind == "event_concept_audit") { job.Kind = "event_concept"; job.PrivacyDraft = ""; }
            if (job.Kind == "event_progress_audit") { job.Kind = "event_progress"; job.PrivacyDraft = ""; }
            if (IsPrivacyPipelineJob(job))
            { job.Kind = "generate"; job.PrivacyPublicContext = ""; job.PrivacyRevision = -1; job.PrivacyDraft = ""; InvalidateDraftRepair(job); }
            if (IsLiveRound(round)) _diplomacyWorkNeedsReconcile = true;
            else RemoveJob(job.JobId);
            return false;
        }
        ObserveDiplomacyService(result);
        job.RecoverOnService = !result.Success && result.IsServiceFailure;
        if (!result.Success && result.IsServiceFailure && job.TransportRetryAttempts++ < 2)
        {
            job.RetryAfterUtcTicks = DateTime.UtcNow.AddSeconds(15 * job.TransportRetryAttempts).Ticks;
            return false;
        }
        if (!result.Success && result.IsServiceFailure)
        {
            if (RequestLane(job) == "oral") Storage.OralServiceRetryAfterUtcTicks = DateTime.UtcNow.AddSeconds(60).Ticks;
            else if (RequestLane(job) == "player") Storage.PlayerServiceRetryAfterUtcTicks = DateTime.UtcNow.AddSeconds(60).Ticks;
            else Storage.ServiceRetryAfterUtcTicks = DateTime.UtcNow.AddSeconds(60).Ticks;
        }
        return true;
    }
    internal void MaintainDialogueAndMemory()
    {
        RetryPendingDiplomaticPersonalMemories(); RetryPendingDialoguePersonalMemories();
        if (_host.WorldDiplomacyEnabled()) MaintainDialogueArrangements();
    }
}
