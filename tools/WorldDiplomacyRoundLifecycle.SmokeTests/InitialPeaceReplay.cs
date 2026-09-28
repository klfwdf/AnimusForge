using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge;
using AnimusForge.Refactor.Domain;

internal static class InitialPeaceReplay
{
    private sealed class Port : IWorldDiplomacyInitialPeacePort
    {
        internal List<string> Ids = new() { "c", "a", "b" };
        internal readonly List<string> Effects = new();
        internal int Scans;
        internal bool Fail = true;
        public bool CampaignAvailable => true;
        public bool Enabled { get; set; } = true;
        public IEnumerable<string> ActiveKingdomIds() { Scans++; return Ids; }
        public bool IsAtWar(string first, string second) => first == "a";
        public void MakePeace(string first, string second)
        { Effects.Add(first + "|" + second); if (Fail && second == "b") throw new InvalidOperationException("failed pair"); }
        public void SanitizeNativeQueue() => Effects.Add("sanitize");
        public void ClearWarSituationCache() => Effects.Add("cache");
        public int CurrentDay() => 5;
        public void Log(string message) { }
    }
    internal static void Run()
    {
        var storage = new WorldDiplomacyStorage { InitialPeacePending = true };
        bool attempted = false, sanitized = false;
        var port = new Port { Enabled = false };
        WorldDiplomacyInitialPeaceApplication.Apply(storage, ref attempted, ref sanitized, ref port);
        Test.True(!attempted && port.Scans == 0, "disabled initial peace exits before world enumeration");
        port.Enabled = true; port.Ids = new() { "a" };
        WorldDiplomacyInitialPeaceApplication.Apply(storage, ref attempted, ref sanitized, ref port);
        Test.True(!attempted && storage.InitialPeacePending, "insufficient kingdoms preserve initial-peace retry");
        port.Ids = new() { "c", "a", "b" };
        WorldDiplomacyInitialPeaceApplication.Apply(storage, ref attempted, ref sanitized, ref port);
        Test.True(string.Join(",", port.Effects) == "a|b,a|c,sanitize,cache" && attempted && sanitized
            && storage.InitialPeaceApplied && !storage.InitialPeacePending,
            "initial peace sorts pairs, tolerates one failed effect, then commits cleanup in order");
        Test.True(storage.LastPeaceDayByPair.Count == 1 && storage.LastPeaceDayByPair[WorldDiplomacyRoundLifecycleRules.PairKey("a", "c")] == 5,
            "only successful peace records its day");
        int scans = port.Scans;
        WorldDiplomacyInitialPeaceApplication.Apply(storage, ref attempted, ref sanitized, ref port);
        Test.True(port.Scans == scans, "later ticks skip completed initial peace without scanning");
    }
}
