using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Runs synchronously at the existing daily boundary over the canonical sorted queue.
// Only the due prefix is inspected; game-object resolution stays in the host ports.
internal static class WorldDiplomacyPropagationApplication
{
    internal static void BeginPublication(
        WorldDiplomacyStorage storage,
        WorldDiplomacyDocument document,
        string authorId,
        Func<string, WorldDiplomacyRound> resolveRound,
        Func<WorldDiplomacyRound> ensureRound,
        Func<string> resolveOriginSettlementId,
        Func<bool> isPlayerAffiliatedAuthor,
        Func<bool> isPlayerKingdom,
        Func<int> currentDay,
        Func<int> participantLimit,
        Action<WorldDiplomacyDocument> recordWeeklyMaterial)
    {
        WorldDiplomacyRound round = resolveRound(document.RoundId);
        if (round == null
            && !string.Equals(document.AnalysisStatus, "external_fact", StringComparison.OrdinalIgnoreCase))
        {
            round = ensureRound();
            document.RoundId = round?.RoundId ?? "";
            document.ExchangeId = document.RoundId;
        }
        string originId = resolveOriginSettlementId();
        document.OriginSettlementId = originId ?? "";
        if (!document.IsPlayerAuthored && isPlayerAffiliatedAuthor())
            document.HasReachedPlayerCourt = true;
        document.PropagationStarted = true;
        document.IsReadyForPublication = true;
        if (round != null)
        {
            round.RootDocumentId = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(
                round.RootDocumentId, document.DocumentId);
            round.LastActivityDay = currentDay();
            WorldDiplomacyRoundParticipant authorParticipant = WorldDiplomacyStructureRules.EnsureRoundParticipant(
                round, authorId, "active", mandatoryReply: false);
            authorParticipant.SelectedForRelay = true;
            authorParticipant.IsPlayerAsync = isPlayerKingdom();
            WorldDiplomacyStructureRules.AddParticipantToRelayRouteIfNeeded(round, authorId, participantLimit());
            authorParticipant.LastSpokeDay = currentDay();
            if (document.IsResponse)
            {
                authorParticipant.MandatoryReplyPending = false;
                authorParticipant.LastTriggeredDocumentId = document.SourceDocumentId ?? "";
            }
        }
        WorldDiplomacyDocumentFactRules.RecordSettlementKnowledge(
            storage.SettlementKnowledge, originId, document.DocumentId, currentDay());
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(
            storage.KingdomKnowledge, authorId, document.DocumentId, currentDay());
        WorldDiplomacyDocumentFactRules.RecordNobleKnowledge(
            storage.NobleKnowledge, authorId, document.DocumentId, currentDay());
        recordWeeklyMaterial(document);
    }

    internal static void RecoverPlayerCourtReceipts(
        WorldDiplomacyStorage storage,
        string playerKingdomId,
        Action<string> log)
    {
        if (playerKingdomId == null || storage?.Documents == null) return;
        WorldDiplomacyKingdomKnowledge knowledge = (storage.KingdomKnowledge
                ?? new List<WorldDiplomacyKingdomKnowledge>())
            .FirstOrDefault(x => x != null
                && string.Equals(x.KingdomId, playerKingdomId, StringComparison.OrdinalIgnoreCase));
        if (knowledge?.DocumentIds == null || knowledge.DocumentIds.Count == 0) return;
        HashSet<string> knownDocumentIds = new HashSet<string>(knowledge.DocumentIds, StringComparer.OrdinalIgnoreCase);
        int recovered = 0;
        foreach (WorldDiplomacyDocument document in storage.Documents)
        {
            if (document == null || document.IsPlayerAuthored || !document.IsReadyForPublication
                || document.HasReachedPlayerCourt || document.FormalNoticeShown
                || !knownDocumentIds.Contains(document.DocumentId ?? "")) continue;
            document.HasReachedPlayerCourt = true;
            recovered++;
        }
        if (recovered > 0)
            log("formal-player-receipts.recovered kingdom=" + playerKingdomId
                + " documents=" + recovered.ToString(CultureInfo.InvariantCulture));
    }

    internal static void ReceivePlayerRelay(
        string receiverId,
        WorldDiplomacyDocument document,
        Func<bool> isPlayerAffiliated,
        Action processCourtArrival,
        Func<int> currentDay,
        Action<string> log)
    {
        if (receiverId == null || document == null || document.IsPlayerAuthored
            || document.HasReachedPlayerCourt || !isPlayerAffiliated()) return;
        if (string.Equals(receiverId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase))
            document.HasReachedPlayerCourt = true;
        else
            processCourtArrival();
        log("formal-player-relay.received document=" + document.DocumentId
            + " receiver=" + receiverId
            + " day=" + currentDay().ToString(CultureInfo.InvariantCulture));
    }

    internal static void RetryDeferred(
        WorldDiplomacyStorage storage,
        Func<string, bool> resolveAuthor,
        Action<WorldDiplomacyDocument> startPropagation,
        Action<string> log)
    {
        foreach (WorldDiplomacyDocument document in WorldDiplomacyRoundLifecycleRules.OrderDocumentsChronologically(
                     (storage.Documents ?? new List<WorldDiplomacyDocument>())
                         .Where(x => x != null && x.IsReadyForPublication && !x.PropagationCompleted)).Take(8))
        {
            if (!resolveAuthor(document.AuthorKingdomId)) continue;
            try { startPropagation(document); }
            catch (Exception ex)
            {
                log("deferred propagation retry failed document=" + document.DocumentId + " error=" + ex.Message);
            }
        }
    }

    internal struct SettlementTarget
    {
        internal string Id;
        internal float Distance;
        internal bool IsOrigin;
    }

    internal struct CourtTarget
    {
        internal string KingdomId;
        internal string SettlementId;
        internal float Distance;
        internal bool IsPlayerAffiliated;
    }

    internal sealed class ScheduleResult
    {
        internal int LatestCivilianDueDay;
        internal int LatestCourtDueDay;
    }

    internal sealed class DistanceSnapshot
    {
        internal float MaxCivilianDistance;
        internal float MaxCourtDistance;
        internal Dictionary<string, float> SettlementDistances;
        internal Dictionary<string, float> CourtDistances;

        internal bool TryGetDistance(string settlementId, out float distance)
        {
            distance = 0f;
            return !string.IsNullOrWhiteSpace(settlementId)
                && SettlementDistances.TryGetValue(settlementId, out distance);
        }
    }

    // Called once per published document. The host captures game identities and
    // distances on the main thread; no live campaign object enters this owner.
    internal static ScheduleResult SchedulePublication(
        WorldDiplomacyStorage storage,
        WorldDiplomacyDocument document,
        int day,
        int civilianSpreadDays,
        int courtDeliveryDays,
        IReadOnlyList<SettlementTarget> settlements,
        float maxCivilianDistance,
        IReadOnlyList<CourtTarget> courts,
        float maxCourtDistance)
    {
        int latestCivilianDueDay = day;
        int latestCourtDueDay = day;
        var newArrivals = new List<WorldDiplomacyPropagationArrival>(settlements.Count + courts.Count);
        HashSet<string> knownSettlementIds = WorldDiplomacyDocumentFactRules.GetKnownSettlementIdsForDocument(
            storage.SettlementKnowledge, document.DocumentId);
        HashSet<string> knownKingdomIds = WorldDiplomacyDocumentFactRules.GetKnownKingdomIdsForDocument(
            storage.KingdomKnowledge, document.DocumentId);
        foreach (SettlementTarget settlement in settlements)
        {
            if (settlement.IsOrigin || knownSettlementIds.Contains(settlement.Id)) continue;
            int travelDays = maxCivilianDistance <= 0.01f
                ? 1
                : WorldDiplomacyRoundLifecycleRules.CalculatePropagationDays(
                    settlement.Distance, maxCivilianDistance, civilianSpreadDays);
            latestCivilianDueDay = Math.Max(latestCivilianDueDay, day + travelDays);
            newArrivals.Add(new WorldDiplomacyPropagationArrival
            {
                DocumentId = document.DocumentId,
                RoundId = document.RoundId,
                SettlementId = settlement.Id,
                Scope = "civilian",
                DueDay = day + travelDays
            });
        }
        foreach (CourtTarget court in courts)
        {
            bool playerCourtReceiptMissing = court.IsPlayerAffiliated && !document.HasReachedPlayerCourt;
            if (knownKingdomIds.Contains(court.KingdomId) && !playerCourtReceiptMissing) continue;
            int travelDays = maxCourtDistance <= 0.01f
                ? courtDeliveryDays
                : WorldDiplomacyRoundLifecycleRules.CalculatePropagationDays(
                    court.Distance, maxCourtDistance, courtDeliveryDays);
            latestCourtDueDay = Math.Max(latestCourtDueDay, day + travelDays);
            newArrivals.Add(new WorldDiplomacyPropagationArrival
            {
                DocumentId = document.DocumentId,
                RoundId = document.RoundId,
                SettlementId = court.SettlementId,
                KingdomId = court.KingdomId,
                Scope = "court",
                DueDay = day + travelDays
            });
        }
        storage.PropagationArrivals = WorldDiplomacyRoundLifecycleRules.OrderPropagationArrivalsByDueDate(
                (storage.PropagationArrivals ?? new List<WorldDiplomacyPropagationArrival>())
                    .Where(x => x != null && !WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.DocumentId, document.DocumentId))
                    .Concat(newArrivals))
            .ToList();
        document.PropagationCompleted = true;
        return new ScheduleResult
        {
            LatestCivilianDueDay = latestCivilianDueDay,
            LatestCourtDueDay = latestCourtDueDay
        };
    }

    // Settings changes are checked by the host before it captures bounded map
    // values. This reproduces the two original queue passes and due-day floor.
    internal static void RecalculatePending(
        WorldDiplomacyStorage storage,
        int day,
        int civilianSpreadDays,
        int courtDeliveryDays,
        IReadOnlyList<CourtTarget> courts,
        Func<WorldDiplomacyDocument, DistanceSnapshot> captureDistances)
    {
        List<string> pendingDocumentIds = WorldDiplomacyRoundLifecycleRules.NormalizeIdListPreserveOrder(
            storage.PropagationArrivals.Where(x => x != null).Select(x => x.DocumentId));
        foreach (string documentId in pendingDocumentIds)
        {
            WorldDiplomacyDocument document = WorldDiplomacyRoundLifecycleRules.ResolveDocument(storage.Documents, documentId);
            if (document == null) continue;
            DistanceSnapshot map = captureDistances(document);
            if (map == null) continue;
            foreach (CourtTarget court in courts)
            {
                bool playerCourtReceiptMissing = court.IsPlayerAffiliated && !document.HasReachedPlayerCourt;
                if (string.Equals(court.KingdomId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
                    || (WorldDiplomacyDocumentFactRules.HasKingdomKnowledge(
                        storage.KingdomKnowledge, court.KingdomId, document.DocumentId) && !playerCourtReceiptMissing)
                    || storage.PropagationArrivals.Any(x => x != null && WorldDiplomacyStructureRules.IsCourtArrival(x)
                        && WorldDiplomacyRoundLifecycleRules.MatchesDocumentId(x.DocumentId, document.DocumentId)
                        && string.Equals(x.KingdomId, court.KingdomId, StringComparison.OrdinalIgnoreCase))) continue;
                float distance = map.CourtDistances.TryGetValue(court.KingdomId, out float courtDistance)
                    ? courtDistance : map.MaxCourtDistance;
                int travelDays = map.MaxCourtDistance <= 0.01f
                    ? courtDeliveryDays
                    : WorldDiplomacyRoundLifecycleRules.CalculatePropagationDays(
                        distance, map.MaxCourtDistance, courtDeliveryDays);
                storage.PropagationArrivals.Add(new WorldDiplomacyPropagationArrival
                {
                    DocumentId = document.DocumentId,
                    RoundId = document.RoundId,
                    SettlementId = court.SettlementId,
                    KingdomId = court.KingdomId,
                    Scope = "court",
                    DueDay = Math.Max(day, document.Day + travelDays)
                });
            }
        }
        foreach (IGrouping<string, WorldDiplomacyPropagationArrival> group in storage.PropagationArrivals
                     .Where(x => x != null).GroupBy(x => x.DocumentId, StringComparer.OrdinalIgnoreCase))
        {
            WorldDiplomacyDocument document = WorldDiplomacyRoundLifecycleRules.ResolveDocument(storage.Documents, group.Key);
            if (document == null) continue;
            DistanceSnapshot map = captureDistances(document);
            if (map == null) continue;
            foreach (WorldDiplomacyPropagationArrival arrival in group)
            {
                bool court = WorldDiplomacyStructureRules.IsCourtArrival(arrival);
                bool found = map.TryGetDistance(arrival.SettlementId, out float settlementDistance);
                if (!court && !found) continue;
                float maximumDistance = court ? map.MaxCourtDistance : map.MaxCivilianDistance;
                int maximumDays = court ? courtDeliveryDays : civilianSpreadDays;
                float distance = found ? settlementDistance : maximumDistance;
                int travelDays = maximumDistance <= 0.01f
                    ? maximumDays
                    : WorldDiplomacyRoundLifecycleRules.CalculatePropagationDays(
                        distance, maximumDistance, maximumDays);
                arrival.DueDay = Math.Max(day, document.Day + travelDays);
            }
        }
        storage.PropagationArrivals = WorldDiplomacyRoundLifecycleRules.OrderPropagationArrivalsByDueDate(
            storage.PropagationArrivals).ToList();
        storage.LastAppliedContinentSpreadDays = civilianSpreadDays;
        storage.LastAppliedCivilianSpreadDays = civilianSpreadDays;
        storage.LastAppliedCourtDeliveryDays = courtDeliveryDays;
    }

    internal static void ProcessDue(
        WorldDiplomacyStorage storage,
        int day,
        int maximumArrivals,
        Func<string, WorldDiplomacyDocument> resolveDocument,
        Action<WorldDiplomacyPropagationArrival, WorldDiplomacyDocument, int> deliverCourt,
        Func<string, string> resolveSettlementId)
    {
        var arrivals = storage.PropagationArrivals;
        int count = 0;
        while (count < maximumArrivals && count < arrivals.Count)
        {
            WorldDiplomacyPropagationArrival arrival = arrivals[count];
            if (arrival == null || arrival.DueDay > day) break;
            count++;
        }
        if (count == 0) return;

        var due = arrivals.GetRange(0, count);
        // Preserve dequeue-before-effects, including exception/reentrant-enqueue behavior.
        arrivals.RemoveRange(0, count);
        foreach (WorldDiplomacyPropagationArrival arrival in due)
        {
            WorldDiplomacyDocument document = resolveDocument(arrival.DocumentId);
            if (document == null) continue;
            if (WorldDiplomacyStructureRules.IsCourtArrival(arrival))
            {
                deliverCourt(arrival, document, day);
                continue;
            }
            string settlementId = resolveSettlementId(arrival.SettlementId);
            if (settlementId != null)
                WorldDiplomacyDocumentFactRules.RecordSettlementKnowledge(
                    storage.SettlementKnowledge, settlementId, document.DocumentId, day);
        }
    }

    internal static void ReceiveCourt(
        WorldDiplomacyStorage storage,
        WorldDiplomacyDocument document,
        string receiverId,
        int day,
        Func<bool> isPlayerAffiliated,
        Action processCourtArrival)
    {
        WorldDiplomacyDocumentFactRules.RecordNobleKnowledge(
            storage.NobleKnowledge, receiverId, document.DocumentId, day);
        bool newlyKnown = WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(
            storage.KingdomKnowledge, receiverId, document.DocumentId, day);
        if (newlyKnown || (isPlayerAffiliated() && !document.HasReachedPlayerCourt))
            processCourtArrival();
    }
}
