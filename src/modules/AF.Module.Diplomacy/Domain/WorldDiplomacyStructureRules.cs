using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Domain
{
    /// <summary>
    /// DPL-060AO: pure round/document structure rules extracted from the host.
    /// Pure rules over Refactor records only; no engine or host references.
    /// </summary>
    public static class WorldDiplomacyStructureRules
    {
    public static bool NeedsCanonicalHistoryRetry(WorldDiplomacyDocument document)
    {
        return WorldDiplomacyRoundLifecycleRules.NeedsCanonicalHistoryRetry(document);
    }

    public static bool HasOpenRoundOffers(WorldDiplomacyRound round)
    {
        return round?.PendingOffers?.Any(x => x != null && WorldDiplomacyRoundLifecycleRules.IsOpenLifecycleStatus(x.Status)) == true;
    }

    public static bool RoundContainsKingdom(WorldDiplomacyRound round, string kingdomId)
    {
        return round?.Participants?.Any(x => x != null && string.Equals(x.KingdomId, kingdomId, StringComparison.OrdinalIgnoreCase)) == true;
    }

    public static bool RoundRouteContainsKingdom(WorldDiplomacyRound round, string kingdomId)
    {
        return !string.IsNullOrWhiteSpace(kingdomId)
            && round?.RelayRouteKingdomIds?.Contains(kingdomId, StringComparer.OrdinalIgnoreCase) == true;
    }

    public static WorldDiplomacyRoundParticipant EnsureRoundParticipant(WorldDiplomacyRound round, string kingdomId, string state, bool mandatoryReply)
    {
        if (round == null || string.IsNullOrWhiteSpace(kingdomId))
        {
            return null;
        }
        round.Participants ??= new List<WorldDiplomacyRoundParticipant>();
        WorldDiplomacyRoundParticipant participant = WorldDiplomacyRoundLifecycleRules.SelectParticipantByKingdom(
            round.Participants, kingdomId);
        if (participant == null)
        {
            participant = new WorldDiplomacyRoundParticipant { KingdomId = kingdomId, State = state ?? "observer" };
            round.Participants.Add(participant);
        }
        else if (WorldDiplomacyRoundLifecycleRules.ShouldRefreshParticipantState(participant.State, mandatoryReply, state))
        {
            participant.State = WorldDiplomacyRoundLifecycleRules.FirstNonEmpty(state, participant.State, "observer");
        }
        participant.MandatoryReplyPending |= mandatoryReply;
        return participant;
    }

    public static bool AddParticipantToRelayRouteIfNeeded(WorldDiplomacyRound round, string kingdomId, int participantLimit)
    {
        if (round == null) return false;
        round.RelayRouteKingdomIds ??= new List<string>();
        switch (WorldDiplomacyRoundLifecycleRules.EvaluateRouteAdmission(
            round.RelayPlanned,
            !string.IsNullOrWhiteSpace(kingdomId),
            round.RelayRouteKingdomIds.Contains(kingdomId, StringComparer.OrdinalIgnoreCase),
            round.RelayRouteKingdomIds.Count,
            participantLimit))
        {
            case WorldDiplomacyRouteAdmission.AlreadyOnRoute:
                return true;
            case WorldDiplomacyRouteAdmission.Admitted:
                round.RelayRouteKingdomIds.Add(kingdomId);
                return true;
            default:
                return false;
        }
    }

    public static bool IsCourtArrival(WorldDiplomacyPropagationArrival arrival)
    {
        return WorldDiplomacyRoundLifecycleRules.IsCourtPropagationArrival(arrival);
    }

    public static bool DocumentRequiresResponseFrom(WorldDiplomacyDocument document, string kingdomId)
    {
        return WorldDiplomacyRoundLifecycleRules.IsResponseRequiredFrom(document, kingdomId);
    }

    public static List<string> GetDocumentTargetIds(WorldDiplomacyDocument document, bool changedOnly = false)
    {
        return WorldDiplomacyRoundLifecycleRules.CollectDocumentTargetIds(document, changedOnly);
    }

    public static bool DocumentRespondsTo(WorldDiplomacyDocument document, string sourceDocumentId)
    {
        return WorldDiplomacyRoundLifecycleRules.DocumentRespondsToSource(document, sourceDocumentId);
    }
    }
}
