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
