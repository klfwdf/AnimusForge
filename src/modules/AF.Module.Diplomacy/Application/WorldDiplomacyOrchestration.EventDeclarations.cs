using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal sealed partial class WorldDiplomacyOrchestration
{
    // Shared event drafting. Birth lane is bound by EnqueueJob; it never chooses
    // parties by an old relay cursor, settlement seat or automatic turn quota.
    private void QueueEventDeclaration(WorldDiplomacyRound round, string author, string target,
        WorldDiplomacyDocument source, int priority, bool externalResponse, bool reminder)
    {
        if (!IsLiveRound(round) || round.EventSystemVersion < 1 || !_host.WorldDiplomacyEnabled()) return;
        if (!AllowEndingDeclaration(round, author, source)) return;
        var participant = round.Participants.FirstOrDefault(x => x.KingdomId == author);
        bool owesReply = source != null && round.PlayerResponses.Any(x => x.KingdomId == author
            && x.SourceDocumentId == source.DocumentId && x.Status == "pending" && string.IsNullOrEmpty(x.AnswerDocumentId));
        bool pendingParticipation = source != null && participant?.PendingParticipationSourceId == source.DocumentId;
        if (participant?.State != "active" && !owesReply && !pendingParticipation && !_schedulingFinalDecision) return;
        participant ??= WorldDiplomacyStructureRules.EnsureRoundParticipant(round, author, "observer", false);
        if (!_host.HasIndependentAuthority(author) || !_host.CanAiAuthorParty(author, out _))
        {
            participant.State = "unavailable"; participant.MandatoryReplyPending = false;
            foreach (var work in round.PlayerCourtWork.Where(x => x.KingdomId == author && x.Status is "pending" or "queued"))
                work.Status = "unavailable";
            round.ConversationRevision++; MarkEventDirty(round); return;
        }
        bool carriedResponse = source != null && round.PlayerResponses.Any(x => x.KingdomId == author
            && x.SourceDocumentId == source.DocumentId && x.OriginalRoundId == source.RoundId && x.Status == "pending");
        if (source != null && ((source.RoundId != round.RoundId && !carriedResponse) || !source.IsReadyForPublication
            || !DialogueDocumentKnown(author, source.DocumentId))) return;
        if (Storage.Jobs.Any(x => x.RoundId == round.RoundId && x.AuthorKingdomId == author
            && x.SourceDocumentId == (source?.DocumentId ?? "") && (x.Kind is "generate" or "contact_wait" || IsPrivacyPipelineJob(x)))) return;
        var candidates = round.Participants.Where(x => (x.State != "observer" || x.KingdomId == target) && x.KingdomId != author
            && !_host.IsEliminatedParty(x.KingdomId) && _host.HasIndependentAuthority(x.KingdomId))
            .Select(x => x.KingdomId).Distinct(StringComparer.Ordinal).ToList();
        if (string.IsNullOrEmpty(target)) target = candidates.FirstOrDefault();
        // No eligible counterparty is an objective change for the event progress
        // owner. It is not a reason to manufacture a declaration or end effect.
        if (string.IsNullOrEmpty(target) || !candidates.Contains(target))
        { MarkEventDirty(round); return; }
        PruneInvalidOffers(round);
        var range = _host.DeclarationCharacterRange();
        var job = new WorldDiplomacyJob {
            JobId = _host.NewId("diplomacy_generate"), Kind = "generate", Priority = priority,
            CreatedDay = _host.CurrentDay(), RoundId = round.RoundId, ExchangeId = round.RoundId,
            AuthorKingdomId = author, TargetKingdomId = target, SourceDocumentId = source?.DocumentId ?? "",
            IsResponse = source != null, IsExternalResponseOnly = externalResponse, IsReminder = reminder,
            CandidateKingdomIds = candidates, WasAtWarWhenQueued = _host.PartiesAtWar(author, target),
            SystemPrompt = WorldDiplomacyPromptContractRules.BuildGenerationSystemPrompt(
                _host.CommonDiplomacyContract(round), range.minimum, range.maximum, _host.PromptWorld().LocalizePrompt),
            UserPrompt = WorldDiplomacyPromptContractRules.BuildDeclareModePrompt(
                BuildGenerationPromptForJob(author, target, null, source != null, source, reminder,
                    round.RoundId, false, candidates, externalResponse), _host.PromptWorld().LocalizePrompt),
            MaxTokens = _host.GenerationMaxTokens(), CacheAffinityKey = WorldDiplomacyPromptContractRules.CanonicalHistoryCacheAffinityKey
        };
        job.PresentedLegalActionSignature = BuildGenerationLegalActionSignature(job);
        CaptureCanonicalHistoryForQueuedJob(job);
        EnqueueJob(job);
    }
    internal void HoldEventForInvalidDraft(WorldDiplomacyRound round, string reason)
    {
        if (!IsLiveRound(round)) return;
        round.TechnicalHoldReason = reason;
        round.ConversationRevision++;
        MarkEventDirty(round);
    }
}
