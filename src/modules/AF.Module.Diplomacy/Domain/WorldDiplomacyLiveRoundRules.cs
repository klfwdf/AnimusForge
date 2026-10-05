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

    // Once per old-save initialization, not once per scheduling pass.
    internal static void InitializeOrdinaryAdmission(WorldDiplomacyStorage storage)
    {
        if (storage == null || storage.LastOrdinaryRoundStartedDay.HasValue) return;
        int latest = -1;
        foreach (var round in Live(storage))
            if (IsOrdinary(round)) latest = Math.Max(latest, round.StartedDay);
        if (storage.CompletedRounds != null)
            foreach (var round in storage.CompletedRounds)
                if (IsOrdinary(round)) latest = Math.Max(latest, round.StartedDay);
        storage.LastOrdinaryRoundStartedDay = latest;
        // Discard the legacy configurable opening interval, retain its save keys.
        storage.NextNormalRoundDay = latest < 0 ? 0 : WorldDiplomacyRoundLifecycleRules.ComputeNextRoundDay(latest, 1);
        storage.LastAppliedRoundIntervalDays = 1;
    }

    internal static bool CanStartOrdinary(WorldDiplomacyStorage storage, int day, int maximum)
    {
        if (storage == null) return false;
        InitializeOrdinaryAdmission(storage);
        if (storage.LastOrdinaryRoundStartedDay.Value >= day) return false;
        int count = 0, limit = Math.Max(1, maximum);
        foreach (var round in Live(storage))
            if (WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(round.State) && IsOrdinary(round)
                && ++count >= limit) return false;
        return true;
    }
}
