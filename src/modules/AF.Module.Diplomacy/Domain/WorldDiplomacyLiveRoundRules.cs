using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.Refactor.Domain;

internal static class WorldDiplomacyLiveRoundRules
{
    internal static IEnumerable<WorldDiplomacyRound> Live(WorldDiplomacyStorage storage)
    {
        if (storage?.ActiveRound != null) yield return storage.ActiveRound;
        if (storage?.ConcurrentRounds == null) yield break;
        foreach (var round in storage.ConcurrentRounds)
            if (round != null && !ReferenceEquals(round, storage.ActiveRound)) yield return round;
    }
    internal static bool Contains(WorldDiplomacyStorage storage, WorldDiplomacyRound round) => round != null
        && WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State)
        && (ReferenceEquals(storage?.ActiveRound, round) || storage?.ConcurrentRounds?.Contains(round) == true);
    internal static bool IsOrdinary(WorldDiplomacyRound round) => round != null && !round.IsPlayerInsertion
        && !string.Equals(round.EventSourceType, "dialogue_commitment", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(round.EventSourceType, "player_followup", StringComparison.OrdinalIgnoreCase);
    internal static bool HasInitiator(WorldDiplomacyStorage storage, string id) => Live(storage).Any(r => Contains(storage, r)
        && IsOrdinary(r) && string.Equals(r.InitiatorKingdomId, id, StringComparison.OrdinalIgnoreCase));
}
