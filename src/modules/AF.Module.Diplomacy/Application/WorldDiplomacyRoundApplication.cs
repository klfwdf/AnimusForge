using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

// Main-thread application transitions over the canonical persisted state. No live game objects.
// Called at existing day/event boundaries; selection stops at the first eligible candidate.
internal static class WorldDiplomacyRoundApplication
{
    internal static void Close(
        WorldDiplomacyStorage storage,
        string reason,
        Func<int> currentDay,
        Action<WorldDiplomacyRound> settleCooldowns,
        Action<int> scheduleNext,
        Action<WorldDiplomacyRound, List<WorldDiplomacyDocument>> commitSummary,
        Action scheduleCompression,
        Action<string> log)
    {
        WorldDiplomacyRound round = storage.ActiveRound;
        if (round == null) return;
        round.State = "closed";
        round.CompletedDay = currentDay();
        round.CloseReason = reason ?? "";
        string closedStatusName = WorldDiplomacyRoundLifecycleRules.ClosedRoundStatusName(
            WorldDiplomacyRoundLifecycleRules.EvaluateClosedRoundStatus(
                round.CloseReason,
                round.RoundStatus,
                round.ExecutedActionCount,
                round.DiplomaticActionAttemptCount));
        if (!string.IsNullOrEmpty(closedStatusName))
        {
            round.RoundStatus = closedStatusName;
        }
        foreach (WorldDiplomacyRoundOffer offer in (round.PendingOffers ?? new List<WorldDiplomacyRoundOffer>()).Where(x => x != null && WorldDiplomacyRoundLifecycleRules.IsOpenLifecycleStatus(x.Status))) offer.Status = "expired";
        settleCooldowns(round);
        List<WorldDiplomacyDocument> documents = WorldDiplomacyRoundLifecycleRules.SelectPublishedRoundDocuments(storage.Documents, round.RoundId);
        WorldDiplomacyRoundLifecycleRules.SettleDiplomaticThreatObligationsForClosedRound(round, documents, storage.DiplomaticThreats, currentDay());
        round.FinalDocumentId = documents.LastOrDefault()?.DocumentId ?? "";
        WorldDiplomacyRoundLifecycleRules.ClearRoundScopedQueuesAndExpireOpportunities(storage, round);
        storage.CompletedRounds.Add(round);
        storage.ActiveRound = null;
        scheduleNext(currentDay());
        if (documents.Count > 0) commitSummary(round, documents);
        round.CommonContractSnapshot = "";
        round.CommonContractSnapshotInitialized = false;
        log("round closed round=" + round.RoundId
            + " reason=" + round.CloseReason
            + " documents=" + documents.Count.ToString(CultureInfo.InvariantCulture)
            + " substantiveProgress=" + round.SubstantiveProgressCount.ToString(CultureInfo.InvariantCulture)
            + " diplomaticActionAttempts=" + round.DiplomaticActionAttemptCount.ToString(CultureInfo.InvariantCulture)
            + " executedActions=" + round.ExecutedActionCount.ToString(CultureInfo.InvariantCulture));
        scheduleCompression();
    }
    internal static WorldDiplomacyRound EnsureOpen(WorldDiplomacyStorage storage, Func<RoundOpening> resolveOpening)
    {
        if (storage.ActiveRound != null && WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(storage.ActiveRound.State))
        {
            return storage.ActiveRound;
        }
        RoundOpening input = resolveOpening();
        WorldDiplomacyRound round = new WorldDiplomacyRound
        {
            SchemaVersion = input.SchemaVersion,
            RoundId = input.RoundId,
            InitiatorKingdomId = input.InitiatorId ?? "",
            State = "active",
            StartedDay = input.Day,
            LastActivityDay = input.Day,
            SoftEndDay = input.Day + input.DurationDays,
            HardEndDay = input.Day + input.HardDurationDays,
            RelayPassDurationDays = input.RelayPassDays,
            IsPlayerInsertion = input.PlayerInsertion
        };
        storage.ActiveRound = round;
        WorldDiplomacyStructureRules.EnsureRoundParticipant(round, input.InitiatorId, "active", mandatoryReply: false);
        if (!string.Equals(input.TargetId, input.InitiatorId, StringComparison.Ordinal))
        {
            WorldDiplomacyStructureRules.EnsureRoundParticipant(round, input.TargetId, "observer", mandatoryReply: false);
        }
        return round;
    }
    internal static void TryScheduleNormal(
        WorldDiplomacyStorage storage,
        bool requestRunning,
        Func<int> currentDay,
        Func<IReadOnlyList<string>> eligibleIds,
        Func<string, bool> hasActionableTarget,
        Func<bool> consumeBudget,
        Func<string, WorldDiplomacyRound> openRound,
        Action<string, WorldDiplomacyRound> enqueue,
        Action<int> scheduleNext,
        Action<string> log)
    {
        if (storage.ActiveRound != null || storage.Jobs.Count > 0 || requestRunning)
        {
            return;
        }
        int day = currentDay();
        if (!WorldDiplomacyRoundLifecycleRules.IsNormalRoundDue(day, storage.NextNormalRoundDay))
        {
            return;
        }
        IReadOnlyList<string> initiators = eligibleIds();
        if (initiators.Count == 0)
        {
            scheduleNext(day);
            return;
        }
        int startIndex = WorldDiplomacyRoundLifecycleRules.NormalizeRotationStartIndex(storage.RotationIndex, initiators.Count);
        int selectedIndex = -1;
        string initiator = null;
        for (int offset = 0; offset < initiators.Count; offset++)
        {
            int candidateIndex = (startIndex + offset) % initiators.Count;
            string candidate = initiators[candidateIndex];
            if (!hasActionableTarget(candidate)) continue;
            initiator = candidate;
            selectedIndex = candidateIndex;
            break;
        }
        if (initiator == null)
        {
            log("autonomous diplomacy skipped because no eligible kingdom has an actionable target");
            scheduleNext(day);
            return;
        }
        storage.RotationIndex = WorldDiplomacyRoundLifecycleRules.NextRotationIndex(selectedIndex, initiators.Count);
        if (!consumeBudget())
        {
            return;
        }
        WorldDiplomacyRound round = openRound(initiator);
        log("autonomous diplomacy opportunity opened round=" + round.RoundId + " initiator=" + initiator);
        enqueue(initiator, round);
    }
    internal static void AdvanceRelay(
        WorldDiplomacyRound round,
        bool scheduleImmediately,
        Func<int> currentDay,
        Action<WorldDiplomacyRound> scheduleSettlement,
        Action<string> closeRound,
        Action<WorldDiplomacyRound, bool> scheduleRelay)
    {
        if (round == null) return;
        switch (WorldDiplomacyRoundLifecycleRules.EvaluateRelayAdvanceAction(
            WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State),
            round.ResultSettlementPending,
            WorldDiplomacyRoundLifecycleRules.IsHardEndReached(currentDay(), round.HardEndDay)))
        {
            case WorldDiplomacyRelayAdvanceAction.None:
                return;
            case WorldDiplomacyRelayAdvanceAction.ScheduleSettlementTurn:
                round.RelayWaiting = false;
                scheduleSettlement(round);
                return;
            case WorldDiplomacyRelayAdvanceAction.CloseHardEnd:
                closeRound("relay_hard_end");
                return;
            default:
                round.RelayWaiting = false;
                scheduleRelay(round, scheduleImmediately);
                return;
        }
    }
    internal sealed class RoundOpening
    {
        internal int SchemaVersion { get; }
        internal string RoundId { get; }
        internal string InitiatorId { get; }
        internal string TargetId { get; }
        internal int Day { get; }
        internal int DurationDays { get; }
        internal int HardDurationDays { get; }
        internal int RelayPassDays { get; }
        internal bool PlayerInsertion { get; }
        internal RoundOpening(
            int schemaVersion,
            string roundId,
            string initiatorId,
            string targetId,
            int day,
            int durationDays,
            int hardDurationDays,
            int relayPassDays,
            bool playerInsertion)
        {
            SchemaVersion = schemaVersion; RoundId = roundId; InitiatorId = initiatorId; TargetId = targetId;
            Day = day; DurationDays = durationDays; HardDurationDays = hardDurationDays;
            RelayPassDays = relayPassDays; PlayerInsertion = playerInsertion;
        }
    }

    internal static void IntegratePlayerDeclaration(
        WorldDiplomacyStorage storage,
        WorldDiplomacyRound round,
        WorldDiplomacyDocument document,
        Func<int> currentDay,
        Func<int> participantLimit,
        Func<string, string> representativeId,
        Func<string, bool> isPlayer,
        Action<string> log)
    {
        if (round == null || document == null) return;
        WorldDiplomacyRoundParticipant playerParticipant = WorldDiplomacyStructureRules.EnsureRoundParticipant(round, document.AuthorKingdomId, "active", mandatoryReply: false);
        if (playerParticipant != null)
        {
            playerParticipant.IsPlayerAsync = true;
            playerParticipant.LastSpokeDay = currentDay();
            playerParticipant.SelectedForRelay = round.ResultSettlementPending
                ? WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, document.AuthorKingdomId)
                : WorldDiplomacyStructureRules.AddParticipantToRelayRouteIfNeeded(round, document.AuthorKingdomId, participantLimit());
        }
        WorldDiplomacyPlayerOpportunity opportunity = storage.PlayerOpportunities.FirstOrDefault(x => x != null
            && WorldDiplomacyRoundLifecycleRules.IsRecordInRound(x.RoundId, round.RoundId) && WorldDiplomacyRoundLifecycleRules.IsPlayerOpportunityOfStatus(x, "open"));
        if (opportunity != null) opportunity.Status = "answered";
        foreach (string id in WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder((document.AddressedKingdomIds ?? new List<string>())
            .Concat(string.IsNullOrWhiteSpace(document.TargetKingdomId) ? Enumerable.Empty<string>() : new[] { document.TargetKingdomId })))
        {
            string kingdomId = representativeId(id);
            if (kingdomId == null || (round.ResultSettlementPending && !WorldDiplomacyStructureRules.RoundRouteContainsKingdom(round, kingdomId))) continue;
            WorldDiplomacyRoundParticipant participant = WorldDiplomacyStructureRules.EnsureRoundParticipant(round, kingdomId, "active", mandatoryReply: false);
            participant.IsPlayerAsync = isPlayer(kingdomId);
            participant.SelectedForRelay = round.ResultSettlementPending
                ? true
                : WorldDiplomacyStructureRules.AddParticipantToRelayRouteIfNeeded(round, kingdomId, participantLimit());
        }
        log("player declaration appended to relay round=" + round.RoundId + " document=" + document.DocumentId);
    }
    internal static bool AdmitMandatoryReply(
        WorldDiplomacyMandatoryReplyAction action,
        WorldDiplomacyRound round,
        WorldDiplomacyRoundParticipant participant,
        string receiverId,
        string authorBlockReason,
        WorldDiplomacyDocument trigger,
        Action<string> log)
    {
        switch (action)
        {
            case WorldDiplomacyMandatoryReplyAction.Ineligible:
            case WorldDiplomacyMandatoryReplyAction.AlreadyResponded:
            case WorldDiplomacyMandatoryReplyAction.ResponseCapReached:
                if (participant != null) participant.MandatoryReplyPending = false;
                return false;
            case WorldDiplomacyMandatoryReplyAction.SettlementOwned:
                // The settlement queue owns every remaining speaking right. Scheduling the
                // older priority-response path here would create a job without a slot id.
                if (participant != null) participant.MandatoryReplyPending = false;
                return false;
            case WorldDiplomacyMandatoryReplyAction.AuthorBlocked:
                participant.MandatoryReplyPending = false;
                participant.State = "observer";
                log("mandatory response blocked by author authority round=" + round.RoundId
                    + " author=" + receiverId + " reason=" + authorBlockReason);
                return false;
            case WorldDiplomacyMandatoryReplyAction.JobQueued:
                return false;
        }

        participant.LastTriggeredDocumentId = trigger.DocumentId;
        return true;
    }
}
