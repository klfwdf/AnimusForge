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
    private sealed class Host : FakeOrchestrationHost
    {
        internal Port Admission;
        public override int CurrentDay() => Admission.CurrentDay;
        public override bool PartiesAtWar(string firstId, string secondId) => Admission.AtWar;
        public override bool PartiesAllied(string firstId, string secondId) => Admission.Allied;
        public override IWorldDiplomacyWarAdmissionPort WarAdmission(string firstId, string secondId)
        {
            Test.True(firstId == "empire_w" && secondId == "empire_s", "real owner keeps admission party direction");
            return Admission;
        }
    }
    private static Port AdmissionFor(string blocker) => new()
    {
        ValidPair = blocker != "invalid", HasIndependentAuthority = blocker != "authority",
        AtWar = blocker == "war", Allied = blocker == "alliance",
        BlocksNewOffensiveWar = blocker == "civil-war", PendingThreatDecision = blocker == "pending",
        PeaceDay = blocker == "peace" ? 41 : null, WarDay = blocker == "cooldown" ? 41 : null,
        Wars = blocker == "capacity" ? 2 : 0
    };
    internal static void RunStateWiring()
    {
        // Run the actual orchestration callback between admission and domain validation.
        // The former inverted verdict rejects "none" and accepts every genuine blocker.
        foreach (string blocker in new[] { "none", "invalid", "authority", "war", "alliance", "peace", "civil-war", "pending", "cooldown", "capacity" })
        {
            Port p = AdmissionFor(blocker);
            var owner = new WorldDiplomacyOrchestration(new Host { Admission = p }, new WorldDiplomacyRuntimeState());
            bool rejected = owner.TryGetDiplomaticStateViolation("declare_war", "empire_w", "empire_s", out string reason);
            Port expectedPort = AdmissionFor(blocker);
            bool allowed = WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref expectedPort, out string admissionReason);
            Test.True(rejected == !allowed, "real orchestration preserves declare-war verdict: " + blocker);
            Test.True(reason == (allowed ? "" : "declare_war_not_legal:" + admissionReason), "real declare-war rejection retains reason: " + blocker);
            Test.True(p.Scans == expectedPort.Scans, "real declare-war callback keeps short-circuit scan count: " + blocker);
        }
    }
    internal static void RunThreatWiring()
    {
        foreach (string intent in new[] { "warning", "ultimatum" })
        foreach (string blocker in new[] { "none", "invalid", "authority", "war", "alliance", "peace", "civil-war", "pending", "cooldown", "capacity" })
        {
            Port p = AdmissionFor(blocker);
            var owner = new WorldDiplomacyOrchestration(new Host { Admission = p }, new WorldDiplomacyRuntimeState());
            bool rejected = owner.TryGetDiplomaticThreatIntentViolation(intent, "empire_w", "empire_s", null, out string reason);
            Port expectedPort = AdmissionFor(blocker);
            bool allowed = WorldDiplomacyWarAdmissionApplication.CanIssueWarThreat(ref expectedPort, out string admissionReason);
            Test.True(rejected == !allowed, "real orchestration preserves threat verdict: " + intent + "/" + blocker);
            string expectedReason = p.AtWar ? "threat_intent_between_kingdoms_already_at_war" : allowed ? "" : "threat_cannot_be_enforced:" + admissionReason;
            Test.True(reason == expectedReason, "real threat rejection retains reason: " + intent + "/" + blocker);
            Test.True(p.Scans == 0, "real threat callback never scans active wars: " + intent + "/" + blocker);
        }
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
        var cooldown = new Port { WarDay = 41 };
        Test.True(!WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref cooldown, out string cooldownReason)
            && cooldownReason.Contains("剩余9天") && cooldown.Scans == 0, "cooldown explains remaining days without scanning wars");
        var peace = new Port { PeaceDay = 41 };
        Test.True(!WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref peace, out string peaceReason)
            && peaceReason.Contains("剩余9天"), "peace protection explains remaining days");
        var capacity = new Port { Wars = 3 };
        Test.True(!WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref capacity, out string capacityReason)
            && capacityReason.Contains("3/2") && capacityReason.Contains("被动战争") && capacity.Scans == 1,
            "capacity explains all active wars using one scan");
        Test.True(WorldDiplomacyAnalysisApplication.DescribeRejectedPlayerMechanic(
            "final_live_legal_action_guard:declare_war_not_legal:" + cooldownReason) == "宣战未执行：" + cooldownReason,
            "nested legal guard exposes the concrete war reason");
        Test.True(WorldDiplomacyAnalysisApplication.DescribeRejectedPlayerMechanic("final_live_legal_action_guard").Contains("可执行"),
            "context-only guard retains fallback");
        RunStateWiring();
        RunThreatWiring();
    }
}
