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
            || job.RetryAfterUtcTicks > DateTime.UtcNow.Ticks || Storage.ServiceRetryAfterUtcTicks > DateTime.UtcNow.Ticks) return false;
        if (!CanDispatchPlayerResponse(job) || !Storage.RequestBudget.CanAdmit(IsPlayerSchedulingJob(job))) return false;
        if (job.Kind == "generate")
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
        var round = ResolveRound(job.RoundId);
        bool frozenRepair = job.SemanticRepairAttempts > 0 && round != null && job.RoundConversationRevision == round.ConversationRevision;
        if (frozenRepair) return;
        PrepareSharedEventRequest(job);
        job.RequestLane = IsPlayerSchedulingJob(job) ? "player" : round?.EventSourceType == "dialogue_commitment" ? "oral" : "normal";
        job.PersonalMemoryRulerId = _host.PartyRulerId(job.AuthorKingdomId);
        if (job.Kind == "generate" && !string.IsNullOrEmpty(job.PersonalMemoryRulerId))
            job.UserPrompt += "\n" + DialogueHost?.PersonalMemory(job.PersonalMemoryRulerId,
                ResolveRound(job.RoundId)?.RoundTopic, _host.PartyNameOrEmpty(job.TargetKingdomId));
    }
    internal bool AdmitCompletion(WorldDiplomacyJob job, LlmJobResult result)
    {
        if (result.RequestAttempt > 0 && result.RequestAttempt != job.RequestAttempt) return false;
        job.IsRunning = false;
        if (result.Error == "world_diplomacy_request_budget_deferred") return false;
        var round = ResolveRound(job.RoundId);
        if (job.Kind == "generate" && (!IsLiveRound(round)
            || result.RoundConversationRevision != round.ConversationRevision
            || (!string.IsNullOrEmpty(job.PersonalMemoryRulerId) && !DialogueRulerIsCurrent(job.AuthorKingdomId, job.PersonalMemoryRulerId))))
        {
            job.LlmMessages?.Clear(); job.SemanticRepairAttempts = 0;
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
            Storage.ServiceRetryAfterUtcTicks = DateTime.UtcNow.AddSeconds(60).Ticks;
        return true;
    }
    internal void MaintainDialogueAndMemory()
    {
        RetryPendingDiplomaticPersonalMemories(); RetryPendingDialoguePersonalMemories();
        if (_host.WorldDiplomacyEnabled()) MaintainDialogueArrangements();
    }
}
