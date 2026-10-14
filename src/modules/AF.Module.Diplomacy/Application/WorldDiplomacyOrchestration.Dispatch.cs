using System;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    internal WorldDiplomacyRequestLeaseCoordinator RequestLeases { get; } = new(3);
    internal bool IsPlayerSchedulingJobForDispatch(WorldDiplomacyJob job) => IsPlayerSchedulingJob(job);
    internal bool CanDispatchDiplomacyJob(WorldDiplomacyJob job)
    {
        if (job == null || job.IsRunning || RequestLeases.ContainsJob(job.JobId)
            || !string.IsNullOrEmpty(job.SuspendedReason)
            || job.RetryAfterUtcTicks > DateTime.UtcNow.Ticks
            || (IsPlayerSchedulingJob(job) ? Storage.PlayerServiceRetryAfterUtcTicks : Storage.ServiceRetryAfterUtcTicks) > DateTime.UtcNow.Ticks) return false;
        var owningRound = ResolveRound(job.RoundId);
        if (owningRound != null && !DiplomacyPlayerEventRules.Matches(owningRound, job)) return false;
        if (!string.IsNullOrEmpty(job.SilenceSourceDocumentId) && !IsCurrentSilenceJob(job)) return false;
        if (!CanDispatchPlayerResponse(job) || !Storage.RequestBudget.CanAdmit(IsPlayerSchedulingJob(job))) return false;
        if (job.Kind == "generate" || IsPlayerPipelineJob(job))
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
        if (PreparePlayerPipelineRequest(job)) return;
        // Refresh restored jobs and manual retries at the main-thread request boundary.
        if (job.Kind == "analyze" || (job.Kind == "generate" && IsPlayerDiplomacyJob(job))) job.MaxTokens = _host.AnalysisMaxTokens();
        var round = ResolveRound(job.RoundId);
        bool frozenRepair = job.SemanticRepairAttempts > 0 && round != null && job.RoundConversationRevision == round.ConversationRevision;
        if (IsOralDeclarationJob(job))
        {
            RebuildOralDeclarationJob(job);
            job.RequestLane = "player";
            job.PersonalMemoryRulerId = _host.PartyRulerId(job.AuthorKingdomId);
            job.RoundConversationRevision = round?.ConversationRevision ?? 0;
            if (round != null) job.UserPrompt += "\n" + BuildPlayerEventContinuity(round, job.AuthorKingdomId);
            PrepareDiplomacyPrivacy(job);
            return;
        }
        if (frozenRepair) return;
        PrepareSharedEventRequest(job);
        job.RequestLane = IsPlayerSchedulingJob(job) ? "player" : round?.EventSourceType == "dialogue_commitment" ? "oral" : "normal";
        job.PersonalMemoryRulerId = _host.PartyRulerId(job.AuthorKingdomId);
        if (job.Kind == "generate")
        {
            if (DiplomacyPlayerEventRules.IsPlayer(round)) job.UserPrompt += "\n" + BuildPlayerEventContinuity(round, job.AuthorKingdomId);
            PrepareDiplomacyPrivacy(job);
        }
    }
    internal bool AdmitCompletion(WorldDiplomacyJob job, LlmJobResult result)
    {
        if (result.RequestAttempt > 0 && result.RequestAttempt != job.RequestAttempt) return false;
        job.IsRunning = false;
        if (IsOralDeclarationJob(job) && !IsCurrentOralDeclarationJob(job))
        { RemoveJob(job.JobId); return false; }
        if (result.Error == "world_diplomacy_request_budget_deferred") return false;
        var round = ResolveRound(job.RoundId);
        if (round != null && (result.OwnershipEpoch != round.OwnershipEpoch || result.SchedulingOwner != round.SchedulingOwner
            || result.PlayerAuthorityRulerId != round.PlayerAuthorityRulerId || !DiplomacyPlayerEventRules.Matches(round, job)))
        { return false; }
        if (!string.IsNullOrEmpty(job.SilenceSourceDocumentId) && !IsCurrentSilenceJob(job))
        { RemoveJob(job.JobId); return false; }
        if ((job.Kind == "generate" || IsPlayerPipelineJob(job)) && (!IsLiveRound(round)
            || result.RoundConversationRevision != round.ConversationRevision
            || job.CourtKnowledgeRevision != DiplomacyPlayerEventRules.KnowledgeRevision(round, job.AuthorKingdomId)
            || (!string.IsNullOrEmpty(job.PersonalMemoryRulerId) && !DialogueRulerIsCurrent(job.AuthorKingdomId, job.PersonalMemoryRulerId))))
        {
            job.LlmMessages?.Clear(); job.SemanticRepairAttempts = 0;
            if (IsPrivacyPipelineJob(job)) { job.Kind = "generate"; job.PrivacyPublicContext = ""; job.PrivacyRevision = -1; }
            if (IsLiveRound(round)) _diplomacyWorkNeedsReconcile = true;
            else RemoveJob(job.JobId);
            return false;
        }
        if (!result.Success && result.IsServiceFailure && job.TransportRetryAttempts++ < 2)
        {
            job.RetryAfterUtcTicks = DateTime.UtcNow.AddSeconds(15 * job.TransportRetryAttempts).Ticks;
            return false;
        }
        if (!result.Success && result.IsServiceFailure)
        {
            if (IsPlayerSchedulingJob(job)) Storage.PlayerServiceRetryAfterUtcTicks = DateTime.UtcNow.AddSeconds(60).Ticks;
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
