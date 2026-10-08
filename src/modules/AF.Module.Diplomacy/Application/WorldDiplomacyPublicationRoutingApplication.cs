using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

internal static class WorldDiplomacyPublicationRoutingApplication
{
    internal static void Start(IWorldDiplomacyPublicationPort port, IWorldDiplomacyOrchestration orchestration,
        WorldDiplomacyDocument document, string authorId)
    {
        if (document == null || document.PropagationCompleted || authorId == null) return;
        bool playerDiplomacy = WorldDiplomacyPlayerApplication.InvolvesPlayer(document, port.ResolveRound(document.RoundId), port.IsPlayerAffiliated);
        if (!playerDiplomacy && !port.CanAiAuthor(authorId, out string reason))
        {
            orchestration.SuppressInvalidDocumentBeforePropagation(document, reason);
            return;
        }
        string originId = null;
        WorldDiplomacyPropagationApplication.BeginPublication(port.Storage, document, authorId, port.ResolveRound,
            () => orchestration.EnsureActiveRound(authorId, document.TargetKingdomId, document.IsPlayerAuthored),
            () => originId = port.ResolveOriginSettlementId(authorId),
            () => port.IsPlayerAffiliated(authorId), () => port.IsPlayerKingdom(authorId),
            () => port.CurrentDay, () => port.ParticipantLimit, orchestration.RecordDiplomacyWeeklyMaterial);
        // Capture geography once at publication (including retries), never per frame.
        WorldDiplomacyPublicationSnapshot snapshot = port.CaptureDestinations(authorId, originId);
        int civilianDays = port.CivilianSpreadDays;
        int courtDays = port.CourtDeliveryDays;
        var schedule = WorldDiplomacyPropagationApplication.SchedulePublication(port.Storage, document, port.CurrentDay,
            civilianDays, courtDays, snapshot.Settlements, snapshot.MaximumCivilianDistance, snapshot.Courts, snapshot.MaximumCourtDistance);
        port.Log("propagation started document=" + document.DocumentId
            + " round=" + document.RoundId + " origin=" + (originId ?? "none")
            + " settlements=" + snapshot.Settlements.Count.ToString(CultureInfo.InvariantCulture)
            + " civilianDays=" + civilianDays.ToString(CultureInfo.InvariantCulture)
            + " latestCivilianDay=" + schedule.LatestCivilianDueDay.ToString(CultureInfo.InvariantCulture)
            + " courts=" + snapshot.Courts.Count.ToString(CultureInfo.InvariantCulture)
            + " courtDays=" + courtDays.ToString(CultureInfo.InvariantCulture)
            + " latestCourtDay=" + schedule.LatestCourtDueDay.ToString(CultureInfo.InvariantCulture)
            + " addressed=" + string.Join(",", document.AddressedKingdomIds ?? new List<string>()));
    }

    internal static void ReconcileReachedCourts(IWorldDiplomacyPublicationPort port, IWorldDiplomacyOrchestration orchestration,
        WorldDiplomacyDocument document)
    {
        if (document?.IsPlayerAuthored != true) return;
        WorldDiplomacyRound round = port.ResolveRound(document.RoundId);
        if (round == null || !WorldDiplomacyLiveRoundRules.Contains(port.Storage, round)
            || !WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State)) return;
        foreach (string kingdomId in WorldDiplomacyDocumentFactRules.GetKnownKingdomIdsForDocument(port.Storage.KingdomKnowledge, document.DocumentId))
        {
            string receiverId = port.ResolveKingdomId(kingdomId);
            if (receiverId == null || string.Equals(receiverId, document.AuthorKingdomId, StringComparison.OrdinalIgnoreCase)
                || (!WorldDiplomacyPlayerApplication.InvolvesPlayer(document, port.ResolveRound(document.RoundId), port.IsPlayerAffiliated)
                    && !port.HasAuthority(receiverId))) continue;
            bool directlyAddressed = (document.AddressedKingdomIds ?? new List<string>()).Contains(receiverId, StringComparer.OrdinalIgnoreCase)
                || string.Equals(document.TargetKingdomId, receiverId, StringComparison.OrdinalIgnoreCase)
                || port.RepresentsAddressedVassal(receiverId, document);
            bool isPrimaryTarget = string.Equals(document.TargetKingdomId, receiverId, StringComparison.OrdinalIgnoreCase);
            if (!directlyAddressed || (!isPrimaryTarget && !WorldDiplomacyStructureRules.DocumentRequiresResponseFrom(document, receiverId))) continue;
            WorldDiplomacyRoundParticipant participant = WorldDiplomacyStructureRules.EnsureRoundParticipant(round, receiverId, "active", mandatoryReply: true);
            orchestration.TryScheduleMandatoryCourtResponse(round, participant, receiverId, document);
        }
    }
}
