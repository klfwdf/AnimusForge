using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal static class DiplomacyPlayerEventRules
{
    internal static bool IsPlayer(WorldDiplomacyRound round) => round?.SchedulingOwner == "player";
    internal static bool Matches(WorldDiplomacyRound round, WorldDiplomacyJob job) => round != null && job != null
        && job.OwnershipEpoch == round.OwnershipEpoch && job.SchedulingOwner == round.SchedulingOwner;
    internal static bool SilenceDue(WorldDiplomacyPlayerWait wait, double hour) => wait != null && wait.RequiresAnswer
        && wait.NotifiedHour >= 0 && hour >= wait.NotifiedHour + 72
        && string.IsNullOrEmpty(wait.AnswerDocumentId) && string.IsNullOrEmpty(wait.ClosureDocumentId);
    internal static bool HasPublishedAnswer(WorldDiplomacyPlayerWait wait) => !string.IsNullOrEmpty(wait?.AnswerDocumentId);
    internal static int KnowledgeRevision(WorldDiplomacyRound round, string actor) => round?.CourtKnowledgeVersions != null
        && round.CourtKnowledgeVersions.TryGetValue(actor ?? "", out int version) ? version : 0;
    internal static bool ContainsParty(WorldDiplomacyRound round, string id) => !string.IsNullOrEmpty(id)
        && (round?.InitiatorKingdomId == id || round?.Participants?.Any(x => x?.KingdomId == id) == true
            || round?.RelayRouteKingdomIds?.Contains(id) == true);
    internal static bool HasNpcOpening(IEnumerable<WorldDiplomacyRound> rounds) => rounds?.Any(x =>
        x?.State == "active" && IsPlayer(x) && x.NpcInitiatedPlayerEvent) == true;
    internal static void Bind(WorldDiplomacyRound round, WorldDiplomacyJob job)
    {
        if (round == null || job == null) return;
        job.OwnershipEpoch = round.OwnershipEpoch;
        job.SchedulingOwner = round.SchedulingOwner;
        job.PlayerAuthorityRulerId = round.PlayerAuthorityRulerId;
        job.RequestLane = IsPlayer(round) ? "player" : "normal";
        job.CourtKnowledgeRevision = KnowledgeRevision(round, job.AuthorKingdomId);
    }
}
