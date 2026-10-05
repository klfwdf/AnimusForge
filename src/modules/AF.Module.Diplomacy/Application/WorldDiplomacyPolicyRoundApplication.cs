using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Policy topics share ordinary event capacity and the persisted daily opening limit.
internal static class WorldDiplomacyPolicyRoundApplication
{
    internal static void RefreshSignals(WorldDiplomacyStorage storage, Func<int> currentDay,
        Func<IEnumerable<WorldDiplomacyPolicySignalSnapshot>> getSignals, int retentionDays, int maxSignals)
    {
		storage.PendingPolicySignals ??= new List<WorldDiplomacyPolicySignal>();
		storage.ProcessedPolicySignalKeys ??= new List<string>();
		storage.RecentTopicUses ??= new List<WorldDiplomacyTopicUse>();
		HashSet<string> known = new HashSet<string>(storage.ProcessedPolicySignalKeys, StringComparer.OrdinalIgnoreCase);
		foreach (WorldDiplomacyPolicySignal pending in storage.PendingPolicySignals.Where(item => item != null))
		{
			known.Add(pending.SignalKey ?? "");
		}

		int day = currentDay();
		foreach (WorldDiplomacyPolicySignalSnapshot snapshot in getSignals())
		{
			if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.SignalKey) || known.Contains(snapshot.SignalKey)
				|| day - snapshot.PublishedDay > retentionDays)
			{
				continue;
			}
			storage.PendingPolicySignals.Add(new WorldDiplomacyPolicySignal
			{
				SignalKey = snapshot.SignalKey,
				PolicyId = snapshot.PolicyId,
				PolicyKind = snapshot.PolicyKind,
				PolicyName = snapshot.PolicyName,
				PolicySummary = snapshot.PolicySummary,
				IssuerKingdomId = snapshot.IssuerKingdomId,
				IssuerKingdomName = snapshot.IssuerKingdomName,
				TargetKingdomId = snapshot.TargetKingdomId,
				TargetKingdomName = snapshot.TargetKingdomName,
				DirectEffect = snapshot.DirectEffect,
				PublishedDay = snapshot.PublishedDay
			});
			known.Add(snapshot.SignalKey);
		}
		storage.PendingPolicySignals = WorldDiplomacyRoundLifecycleRules.SelectRetainedPolicySignals(
			storage.PendingPolicySignals, day, retentionDays, maxSignals);
	}

    internal static void TrySchedule(
        WorldDiplomacyStorage storage,
        Func<WorldDiplomacyPolicySignal, Parties> resolveParties,
        Func<string, bool> hasActionableTarget,
        Func<bool> requestRunning,
        Func<bool> consumeBudget,
        Func<int> currentDay,
        Func<string, WorldDiplomacyRound> openRound,
        Action<WorldDiplomacyPolicySignal, string> complete,
        Action<int> scheduleNext,
        Action<string, WorldDiplomacyRound> enqueue, int maxOrdinaryRounds = 3)
    {
        if (storage?.PendingPolicySignals == null || storage.PendingPolicySignals.Count == 0) return;
        int day = currentDay();
        bool canOpen = storage.Jobs.Count < 24
            && WorldDiplomacyLiveRoundRules.CanStartOrdinary(storage, day, maxOrdinaryRounds);
        // The retained signal queue is bounded. Skip a busy author so it cannot
        // prevent later signals from attaching or using an available opening.
        foreach (WorldDiplomacyPolicySignal signal in storage.PendingPolicySignals)
        {
            if (signal == null || string.IsNullOrWhiteSpace(signal.SignalKey)) continue;
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
            string author = parties.AffectedIsPlayer ? parties.IssuerId : parties.AffectedId;
            WorldDiplomacyRound activeRound = WorldDiplomacyLiveRoundRules.Live(storage).FirstOrDefault(r =>
                WorldDiplomacyRoundLifecycleRules.IsActiveRoundState(r.State)
                && WorldDiplomacyLiveRoundRules.IsOrdinary(r)
                && string.Equals(r.InitiatorKingdomId, author, StringComparison.OrdinalIgnoreCase));
            if (activeRound != null)
            {
                if (WorldDiplomacyStructureRules.RoundContainsKingdom(activeRound, parties.IssuerId)
                    || WorldDiplomacyStructureRules.RoundContainsKingdom(activeRound, parties.AffectedId))
                {
                    AttachSignalToRound(activeRound, signal, parties);
                    complete(signal, "attached_to_active_round");
                    return;
                }
                continue;
            }
            if (!canOpen) continue; // Retain pending policy; merging remains available at capacity.
            if (!hasActionableTarget(author))
            {
                complete(signal, "no_actionable_diplomatic_target");
                return; // No opening was used; normal selection may still run today.
            }
            if (!consumeBudget()) return;
            WorldDiplomacyRound round = openRound(author);
            if (round == null) return;
            storage.LastOrdinaryRoundStartedDay = day;
            storage.NextNormalRoundDay = WorldDiplomacyRoundLifecycleRules.ComputeNextRoundDay(day, 1);
            AttachSignalToRound(round, signal, parties);
            scheduleNext(day);
            enqueue(author, round);
            complete(signal, "opened_round");
            return;
        }
    }

    private static void AttachSignalToRound(WorldDiplomacyRound round, WorldDiplomacyPolicySignal signal, Parties parties)
    {
        if (round == null || signal == null)
        {
            return;
        }
        WorldDiplomacyRoundLifecycleRules.AttachPolicySignalToRound(round, signal);
        HashSet<string> attached = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach ((string kingdomId, bool isPlayer) in new[]
        {
            (parties.IssuerId, parties.IssuerIsPlayer),
            (parties.AffectedId, parties.AffectedIsPlayer)
        })
        {
            if (string.IsNullOrWhiteSpace(kingdomId) || !attached.Add(kingdomId))
            {
                continue;
            }
            WorldDiplomacyRoundParticipant participant = WorldDiplomacyStructureRules.EnsureRoundParticipant(
                round, kingdomId, "observer", mandatoryReply: false);
            if (participant != null)
            {
                participant.IsPlayerAsync = isPlayer;
            }
        }
    }

    internal sealed class Parties
    {
        internal bool ValidParties { get; }
        internal string IssuerId { get; }
        internal bool IssuerIsPlayer { get; }
        internal string AffectedId { get; }
        internal bool AffectedIsPlayer { get; }
        internal Parties(bool validParties, string issuerId, string affectedId, bool affectedIsPlayer,
            bool issuerIsPlayer = false)
        {
            ValidParties = validParties; IssuerId = issuerId; IssuerIsPlayer = issuerIsPlayer;
            AffectedId = affectedId; AffectedIsPlayer = affectedIsPlayer;
        }
    }
}
