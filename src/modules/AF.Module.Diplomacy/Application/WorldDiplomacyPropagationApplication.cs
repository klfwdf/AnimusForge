using System;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Runs synchronously at the existing daily boundary over the canonical sorted queue.
// Only the due prefix is inspected; game-object resolution stays in the host ports.
internal static class WorldDiplomacyPropagationApplication
{
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
