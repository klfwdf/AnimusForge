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

    private bool IsDirectPlayerSchedulingJob(WorldDiplomacyJob job) => job != null &&
        (job.PlayerResponseSourceIds?.Count > 0
         || ((job.Kind == "analyze" || job.Kind == "round_plan") && ResolveDocument(job.DocumentId)?.IsPlayerAuthored == true)
         || (job.Kind == "generate" && (ResolveRound(job.RoundId)?.PlayerResponses?.Any(x => x != null
             && x.Status == "pending" && x.KingdomId == job.AuthorKingdomId
             && HasCourtDocumentKnowledge(job.AuthorKingdomId, x.SourceDocumentId)) == true)));
    private bool IsPlayerSchedulingJob(WorldDiplomacyJob job) => IsDirectPlayerSchedulingJob(job)
        || (job?.Kind == "compress" && Storage.Jobs.Any(x => x != null && x.AwaitingHistoryCompression && IsDirectPlayerSchedulingJob(x)));







    private void BindPlayerDeclarationToSharedEvent(WorldDiplomacyDocument document)
    {
        var provisional = ResolveRound(document.RoundId);
        if (provisional == null || provisional.DialogueArrangementId != "player_manual_declaration") return;
        // Shared subject/parties do not turn a new player action or counterproposal
        // into an existing AI turn with response/capacity restrictions.
        string playerIntent = NormalizeIntent(document.Intent);
        if (string.IsNullOrEmpty(ResponseIntentToProposalIntent(playerIntent))
            && playerIntent != "comply_ultimatum" && playerIntent != "withdraw_offer") return;
        var explicitSources = new[] { document.RespondingToOfferDocumentId, document.RespondingToThreatDocumentId, document.SourceDocumentId, document.DiscussionSourceDocumentId }
            .Concat((document.Actions ?? new List<WorldDiplomacyDocumentAction>()).SelectMany(x =>
                new[] { x.RespondingToOfferDocumentId, x.RespondingToThreatDocumentId }))
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var sources = explicitSources.Select(ResolveDocument).Where(x => x != null)
            .Select(x => ResolveRound(x.RoundId)).Where(x => IsLiveRound(x) && !ReferenceEquals(x, provisional))
            .Distinct().Take(2).ToList();
        if (!string.IsNullOrWhiteSpace(document.DiscussionRoundId))
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
        document.RoundId = document.ExchangeId = destination.RoundId;
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
                && _host.HasIndependentAuthority(x)).Distinct().ToList();
        if (receivers.Count == 0)
        {
            // Existing event participants first; deterministic rotation breaks
            // ties for a wholly independent, untargeted declaration.
            receivers = _host.AllKingdomIds().Where(id => _host.HasIndependentAuthority(id) && _host.CanAiAuthorParty(id, out _)).OrderByDescending(x => RoundContainsKingdom(round, x))
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

    private void SchedulePlayerResponseWork(WorldDiplomacyRound round)
    {
        if (!IsLiveRound(round) || round.PlayerResponses == null) return;
        foreach (var group in round.PlayerResponses.Where(x => x != null && string.IsNullOrWhiteSpace(x.AnswerDocumentId)
            && x.Status == "pending").GroupBy(x => x.KingdomId).OrderBy(x => x.Min(y => y.CreatedDay)))
        {
            string receiver = ResolveDialogueParty(group.Key);
            if (receiver == null || _host.IsEliminatedParty(receiver) || !_host.HasIndependentAuthority(receiver))
            {
                foreach (var item in group) item.Status = "unavailable";
                _host.Notify("宣言的原回应国已失去独立外交资格；该国回应无法继续。");
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
        job.PlayerResponseSourceIds = DiplomacyRoundWorkRules.SelectResponseBatch(round, job.AuthorKingdomId, knownDocuments);
        string tail = DiplomacyRoundWorkRules.BuildRequestTail(round, knownDocuments, job.PlayerResponseSourceIds);
        int modeMarker = job.UserPrompt.LastIndexOf("【MODE=DECLARE】", StringComparison.Ordinal);
        string basePrompt = modeMarker >= 0 ? job.UserPrompt.Substring(0, modeMarker) : job.UserPrompt;
        job.UserPrompt = BuildDeclareModePrompt(basePrompt + tail);
        if (job.PlayerResponseSourceIds.Count > 0) job.Priority = Math.Max(job.Priority, 95);
    }

    private string BuildPlayerRoundRoutingContext(WorldDiplomacyDocument document)
    {
        if (document?.IsPlayerAuthored != true) return "";
        var sb = new StringBuilder("\n【玩家发言与当前公开交涉的归属判断】\n");
        foreach (var round in GetLiveRounds().Where(x => x.RoundId != document.RoundId).Take(32))
        {
            var root = ResolveDocument(round.RootDocumentId);
            if (root?.IsReadyForPublication != true || !DialogueDocumentKnown(document.AuthorKingdomId, root.DocumentId)) continue;
            sb.AppendLine("事件=" + round.RoundId + "；公开来源=" + root.DocumentId + "；主题=" + round.RoundTopic
                + "；当事国=" + string.Join(",", round.Participants.Select(x => x.KingdomId)) + "\n"
                + Limit(root.Body, 700));
        }
        sb.AppendLine("若玩家是在上述某场交涉中提供担保、调整条件、评论正在讨论的事情或作出回应，JSON增加 related_round_id 与 related_public_document_id，填写唯一匹配的事件和其公开来源ID；不要只因国家相同或某案较新就关联。无法唯一确定时两个字段留空，玩家宣言仍然公开并开启独立回合。此关联只决定讨论归属，不能替代原有接受条款、来源核验及动作判定。");
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
        var pending = (closed.PlayerResponses ?? new List<WorldDiplomacyPlayerResponse>()).Where(x => x != null
            && x.Status == "pending" && string.IsNullOrWhiteSpace(x.AnswerDocumentId)).ToList();
        if (pending.Count == 0 || closed.CloseReason == "closed_disabled") return;
        var followup = new WorldDiplomacyRound { SchemaVersion = _host.RelaySchemaVersion(), RoundId = _host.NewId("diplomacy_followup"),
            EventSourceType = "player_followup", IsPlayerInsertion = true, InitiatorKingdomId = closed.InitiatorKingdomId,
            State = "active", StartedDay = _host.CurrentDay(), LastActivityDay = _host.CurrentDay(),
            SoftEndDay = _host.CurrentDay() + _host.RoundTargetDurationDays(), HardEndDay = _host.CurrentDay() + _host.RoundHardDurationDays(_host.RoundTargetDurationDays()),
            RelayPassDurationDays = _host.CourtMaxDeliveryDays(),
            RoundTopic = "对原交涉中玩家宣言的后续回应", RelayPlanned = true, RootDocumentId = pending[0].SourceDocumentId,
            ExternalOpeningContext = "原事件=" + closed.RoundId + " 已按原规则结束；这里只回应尚未答复的公开宣言。旧提议已失效，不得恢复或直接接受原协议。" };
        foreach (var item in pending)
        {
            item.Status = "transferred";
            followup.PlayerResponses.Add(new WorldDiplomacyPlayerResponse { SourceDocumentId = item.SourceDocumentId,
                KingdomId = item.KingdomId, CreatedDay = item.CreatedDay, OriginalRoundId = item.OriginalRoundId });
            if (!followup.RelayRouteKingdomIds.Contains(item.KingdomId)) followup.RelayRouteKingdomIds.Add(item.KingdomId);
            EnsureRoundParticipant(followup, item.KingdomId, "active", true);
        }
        var player = ResolveDocument(pending[0].SourceDocumentId)?.AuthorKingdomId;
        if (!string.IsNullOrWhiteSpace(player))
        { followup.RelayRouteKingdomIds.Add(player); EnsureRoundParticipant(followup, player, "active", false).IsPlayerAsync = true; }
        Storage.ConcurrentRounds.Add(followup);
        _diplomacyWorkNeedsReconcile = true;
        InvalidateDialogueIndex();
        _host.Notify("原外交交涉已结束；尚未回应你的宣言已转入优先后续回应。");
    }
}
