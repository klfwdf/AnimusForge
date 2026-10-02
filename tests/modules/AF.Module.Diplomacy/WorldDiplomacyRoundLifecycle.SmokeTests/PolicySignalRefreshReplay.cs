using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;
internal static class PolicySignalRefreshReplay
{
    internal static void Run()
    {
        var storage = new WorldDiplomacyStorage();
        storage.ProcessedPolicySignalKeys.Add("processed");
        storage.PendingPolicySignals.Add(new WorldDiplomacyPolicySignal { SignalKey = "pending", PublishedDay = 10 });
        var signals = new List<WorldDiplomacyPolicySignalSnapshot>();
        void Add(string key, int day) => signals.Add(new WorldDiplomacyPolicySignalSnapshot(key, "policy", "kingdom", "name", "summary", "a", "A", "b", "B", "effect", day));
        Add("PROCESSED", 10); Add("PENDING", 10); Add("expired", 1); Add("new", 10); Add("NEW", 10); Add("edge", 5);
        int reads = 0;
        WorldDiplomacyPolicyRoundApplication.RefreshSignals(storage, () => 10, () => { reads++; return signals; }, 5, 3);
        Test.True(reads == 1 && storage.PendingPolicySignals.Count == 3
            && storage.PendingPolicySignals.Count(s => s.SignalKey == "new") == 1
            && storage.PendingPolicySignals.Any(s => s.SignalKey == "edge"), "signal refresh deduplicates existing, processed and same-batch keys and preserves retention boundary");
        var copied = storage.PendingPolicySignals.Single(s => s.SignalKey == "new");
        Test.True(copied.PolicyName == "name" && copied.PolicySummary == "summary" && copied.DirectEffect == "effect"
            && copied.IssuerKingdomName == "A" && copied.TargetKingdomName == "B", "policy refresh preserves snapshot values in the canonical record");
        WorldDiplomacyPolicyRoundApplication.RefreshSignals(storage, () => 10, () => signals, 5, 3);
        Test.True(storage.PendingPolicySignals.Count == 3, "repeated refresh cannot duplicate pending signals");
        WorldDiplomacyPolicyRoundApplication.RefreshSignals(storage, () => 11, () => Array.Empty<WorldDiplomacyPolicySignalSnapshot>(), 5, 1);
        Test.True(storage.PendingPolicySignals.Count == 1 && storage.PendingPolicySignals[0].PublishedDay == 10,
            "refresh prunes expired signals and enforces the existing bounded queue");
    }
}
