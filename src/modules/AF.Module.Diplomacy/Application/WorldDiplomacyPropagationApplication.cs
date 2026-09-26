using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Runs synchronously at the existing daily boundary over the canonical sorted queue.
// Only the due prefix is inspected; game-object resolution stays in the host ports.
internal static class WorldDiplomacyPropagationApplication
{
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
