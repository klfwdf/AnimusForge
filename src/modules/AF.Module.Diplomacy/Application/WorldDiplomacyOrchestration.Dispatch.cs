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
            || job.RetryAfterUtcTicks > DateTime.UtcNow.Ticks || Storage.ServiceRetryAfterUtcTicks > DateTime.UtcNow.Ticks
            || !Storage.RequestBudget.CanAdmit(IsPlayerSchedulingJob(job))) return false;
        if (job.Kind == "generate")
        {
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
    private void PublishImmediatePublicKnowledge(WorldDiplomacyDocument document)
    {
        _publicDiplomacyDocumentIds = null;
        var round = ResolveRound(document.RoundId);
        if (round != null) round.ConversationRevision++;
        int day = _host.CurrentDay();
        foreach (string id in _host.AllKingdomIds())
        {
            if (_host.IsEliminatedParty(id)) continue;
            WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(Storage.KingdomKnowledge, id, document.DocumentId, day);
            WorldDiplomacyDocumentFactRules.RecordNobleKnowledge(Storage.NobleKnowledge, id, document.DocumentId, day);
            if (_host.IsPlayerAffiliatedParty(id)) document.HasReachedPlayerCourt = true;
        }
        document.PropagationCompleted = true;
        RecordDiplomaticDocumentPersonalMemories(document);
        InvalidateDialogueIndex();
    }
    internal System.Collections.Generic.IEnumerable<string> PublicDocumentIds() => GetPublicDiplomacyDocumentIds();
    internal void MaintainDialogueAndMemory()
    {
        RetryPendingDiplomaticPersonalMemories(); RetryPendingDialoguePersonalMemories();
        if (_host.WorldDiplomacyEnabled()) MaintainDialogueArrangements();
    }
}
