using AnimusForge;

internal static class WarAdmissionReplay
{
    private sealed class Port : IWorldDiplomacyWarAdmissionPort
    {
        internal int Scans;
        public bool InvolvesPlayer { get; set; }
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
        public override bool HasIndependentAuthority(string id) => Admission.HasIndependentAuthority;
        public override bool IsEliminatedParty(string id) => !Admission.ValidPair && id == "empire_s";
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
            Test.True(!rejected && reason == "", "rhetorical warning/ultimatum does not require an enforceable scripted war stage: " + intent + "/" + blocker);
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
            Test.True(allowed == (blocker is "none" or "peace" or "cooldown" or "capacity" or "pending"), "Q50 retired pacing cannot block either declaration path: " + blocker);
            Test.True(p.Scans == 0, "Q50 retired capacity never scans active wars: " + blocker);
            Test.True(allowed == string.IsNullOrEmpty(reason), "admission retains its failure reason");
        }
        var boundary = new Port { PeaceDay = 32, WarDay = 32 };
        Test.True(WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref boundary, out _), "legacy protection dates have no active consumer");
        var threatOnly = new Port { PendingThreatDecision = true, Wars = 2 };
        Test.True(WorldDiplomacyWarAdmissionApplication.CanIssueWarThreat(ref threatOnly, out _) && threatOnly.Scans == 0, "threat query never reads pacing state or scans wars");
        var cooldown = new Port { WarDay = 41 };
        Test.True(WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref cooldown, out _) && cooldown.Scans == 0, "old offensive cooldown is ignored without scans");
        var peace = new Port { PeaceDay = 41 };
        Test.True(WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref peace, out _), "old peace protection is retired");
        var capacity = new Port { Wars = 3 };
        Test.True(WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref capacity, out _) && capacity.Scans == 0, "old cross-event war cap is retired without scans");
        var structural = new Port { BlocksNewOffensiveWar = true };
        Test.True(!WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref structural, out string cooldownReason), "structural civil-war guard remains active");
        Test.True(WorldDiplomacyAnalysisApplication.DescribeRejectedPlayerMechanic(
            "final_live_legal_action_guard:declare_war_not_legal:" + cooldownReason) == (SmokeLocalizationEnvironment.EnglishUi ? "Declaration of war not executed: " : "宣战未执行：") + cooldownReason,
            "nested legal guard exposes the concrete war reason");
        Test.True(WorldDiplomacyAnalysisApplication.DescribeRejectedPlayerMechanic("final_live_legal_action_guard").Contains(SmokeLocalizationEnvironment.EnglishUi ? "can currently execute" : "可执行"),
            "context-only guard retains fallback");
        foreach (string blocker in new[] { "none", "invalid", "authority", "war", "alliance", "peace", "civil-war", "pending", "cooldown", "capacity" })
        {
            var playerPort = AdmissionFor(blocker);
            bool expected = blocker is "none" or "peace" or "cooldown" or "capacity" or "pending";
            bool allowed = WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref playerPort, out _, isPlayerAuthored: true);
            Test.True(allowed == expected && playerPort.Scans == 0, "player declaration uses shared authority/state without war scan: " + blocker);
            var owner = new WorldDiplomacyOrchestration(new Host { Admission = playerPort }, new WorldDiplomacyRuntimeState());
            var document = new WorldDiplomacyDocument { IsPlayerAuthored = true };
            bool rejected = owner.TryGetPlayerWorldStateIntentViolation(document, "declare_war", "binding", "empire_w", "empire_s", out _);
            Test.True(rejected == !expected, "player validation retains real party identity: " + blocker);
        }
        foreach (string blocker in new[] { "authority", "war", "alliance", "peace", "civil-war", "pending", "cooldown", "capacity", "none", "invalid" })
        {
            Port player = AdmissionFor(blocker); player.InvolvesPlayer = true;
            bool allowed = WorldDiplomacyWarAdmissionApplication.CanDeclareWar(ref player, out string why);
            Test.True(allowed == (blocker is "none" or "peace" or "cooldown" or "capacity" or "pending"), "player pair uses shared authority/state: " + blocker);
            Test.True(player.Scans == 0, "player admission never scans world wars: " + blocker);
            Test.True(allowed == string.IsNullOrEmpty(why), "invalid identity keeps diagnostic");
        }
        RunStateWiring();
        RunThreatWiring();
    }
}
