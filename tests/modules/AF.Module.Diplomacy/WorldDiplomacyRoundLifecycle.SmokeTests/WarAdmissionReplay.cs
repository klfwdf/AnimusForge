using AnimusForge;

internal static class WarAdmissionReplay
{
    private sealed class Port : IWorldDiplomacyWarAdmissionPort
    {
        internal int Scans;
        public bool ValidPair { get; set; } = true;
        public bool HasIndependentAuthority { get; set; } = true;
        public bool AtWar { get; set; }
        public bool Allied { get; set; }
        public bool BlocksNewOffensiveWar { get; set; }
        public bool PendingThreatDecision { get; set; }
        public int CurrentDay => 42;
        public int PeaceProtectionDays => 10;
        internal int? PeaceDay, WarDay;
        public bool TryGetLastPeaceDay(out int day) { day = PeaceDay ?? 0; return PeaceDay.HasValue; }
        public int OffensiveWarCooldownDays => 10;
        public bool TryGetLastOffensiveWarDay(out int day) { day = WarDay ?? 0; return WarDay.HasValue; }
        internal int Wars;
        public int ActiveWars { get { Scans++; return Wars; } }
        public int MaxConcurrentOffensiveWars => 2;
    }
    internal static void Run()
    {
        foreach (string blocker in new[] { "invalid", "authority", "war", "alliance", "peace", "civil-war", "pending", "cooldown", "capacity", "none" })
        foreach (bool enforce in new[] { false, true })
        {
            var p = new Port { ValidPair = blocker != "invalid", HasIndependentAuthority = blocker != "authority", AtWar = blocker == "war", Allied = blocker == "alliance",
                BlocksNewOffensiveWar = blocker == "civil-war", PendingThreatDecision = blocker == "pending", PeaceDay = blocker == "peace" ? 41 : null, WarDay = blocker == "cooldown" ? 41 : null, Wars = blocker == "capacity" ? 2 : 0 };
            bool allowed = WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref p, out string reason, enforce);
            Test.True(allowed == (blocker == "none" || enforce && (blocker is "cooldown" or "capacity")), "ultimatum bypasses only pacing gates: " + blocker);
            Test.True(p.Scans == (!enforce && (blocker is "capacity" or "none") ? 1 : 0), "world scan stays after all short-circuit guards: " + blocker);
            Test.True(allowed == string.IsNullOrEmpty(reason), "admission retains its failure reason");
        }
        var boundary = new Port { PeaceDay = 32, WarDay = 32 };
        Test.True(WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref boundary, out _), "protection and cooldown expire exactly at their boundary");
        var threatOnly = new Port { PendingThreatDecision = true, Wars = 2 };
        Test.True(WorldDiplomacyWarAdmissionApplication.CanIssueWarThreat(ref threatOnly, out _) && threatOnly.Scans == 0, "threat query never reads pacing state or scans wars");
    }
}
