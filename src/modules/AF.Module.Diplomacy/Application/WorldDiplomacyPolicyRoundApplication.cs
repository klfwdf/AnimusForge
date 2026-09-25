using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// One policy signal per existing daily scheduling pass; active unrelated rounds wait.
internal static class WorldDiplomacyPolicyRoundApplication
{
    internal static void TrySchedule(
        WorldDiplomacyStorage storage,
        Func<WorldDiplomacyPolicySignal, Parties> resolveParties,
        Func<string, bool> hasActionableTarget,
        Func<bool> requestRunning,
        Func<bool> consumeBudget,
        Func<int> currentDay,
        Func<string, WorldDiplomacyRound> openRound,
        Action<WorldDiplomacyRound, WorldDiplomacyPolicySignal> attach,
        Action<WorldDiplomacyPolicySignal, string> complete,
        Action<int> scheduleNext,
        Action<string, WorldDiplomacyRound> enqueue)
    {
        WorldDiplomacyPolicySignal signal = (storage.PendingPolicySignals ?? new List<WorldDiplomacyPolicySignal>())
            .FirstOrDefault(item => item != null && !string.IsNullOrWhiteSpace(item.SignalKey));
        if (signal == null)
        {
            return;
        }

        Parties parties = resolveParties(signal);
        if (!parties.ValidParties)
        {
            complete(signal, "invalid_parties");
            return;
        }
        if (parties.IssuerId == null || parties.AffectedId == null || parties.IssuerId == parties.AffectedId)
        {
            complete(signal, "same_or_invalid_diplomatic_representative");
            return;
        }
        WorldDiplomacyRound activeRound = storage.ActiveRound;
        if (activeRound != null)
        {
            if (WorldDiplomacyStructureRules.RoundContainsKingdom(activeRound, parties.IssuerId) || WorldDiplomacyStructureRules.RoundContainsKingdom(activeRound, parties.AffectedId))
            {
                attach(activeRound, signal);
                complete(signal, "attached_to_active_round");
            }
            return;
        }
        string author = parties.AffectedIsPlayer ? parties.IssuerId : parties.AffectedId;
        if (!hasActionableTarget(author))
        {
            complete(signal, "no_actionable_diplomatic_target");
            scheduleNext(currentDay());
            return;
        }
        if (storage.Jobs.Count > 0 || requestRunning() || !consumeBudget())
        {
            return;
        }

        WorldDiplomacyRound round = openRound(author);
        attach(round, signal);
        scheduleNext(currentDay());
        enqueue(author, round);
        complete(signal, "opened_round");
    }
    internal sealed class Parties
    {
        internal bool ValidParties { get; }
        internal string IssuerId { get; }
        internal string AffectedId { get; }
        internal bool AffectedIsPlayer { get; }
        internal Parties(bool validParties, string issuerId, string affectedId, bool affectedIsPlayer)
        {
            ValidParties = validParties; IssuerId = issuerId;
            AffectedId = affectedId; AffectedIsPlayer = affectedIsPlayer;
        }
    }
}
