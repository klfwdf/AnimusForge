using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Policy topics share ordinary event capacity and the persisted daily opening limit.
internal static class WorldDiplomacyPolicyRoundApplication
{
    internal static bool WasUsed(WorldDiplomacyStorage storage, WorldDiplomacyPolicySignalSnapshot policy)
    {
        var keys = storage.ProcessedPolicySignalKeys;
        if (keys.Contains(policy.SignalKey, StringComparer.OrdinalIgnoreCase)) return true;
        // Old saves recorded adoption by ID/target, without a content revision.
        // Baseline that legacy receipt once, then allow later content revisions.
        // This converts an existing adoption receipt; ordinary queries never mark new policies used.
        string marker = "policy_material_baseline:" + policy.PolicyId;
        if (keys.Contains(marker, StringComparer.OrdinalIgnoreCase)) return false;
        if (!keys.Any(x => x != null && x.StartsWith("policy:" + policy.PolicyId + ":", StringComparison.OrdinalIgnoreCase))) return false;
        keys.Add(marker); keys.Add(policy.SignalKey);
        return true;
    }

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
