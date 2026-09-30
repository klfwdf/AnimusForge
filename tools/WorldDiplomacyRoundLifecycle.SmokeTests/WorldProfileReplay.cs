using AnimusForge;
using AnimusForge.Refactor.Domain;

internal static class WorldProfileReplay
{
    internal static WorldDiplomacyClanSnapshot Clan(string id, string leader = "v", bool ruling = false,
        bool eliminated = false, bool service = false, bool mercenary = false, bool belongs = true, bool leaderIsRuler = false,
        int tier = 0, float influence = 0, int fiefs = 0)
        => new(id, leader, true, belongs, ruling, eliminated, service, mercenary, leaderIsRuler, tier, influence, fiefs);
    private static WorldDiplomacyFortSnapshot Fort(string id, int kingdom, float prosperity = 0, bool ruling = false,
        bool active = true, bool fort = true)
        => new(id, id, "k" + kingdom, kingdom, active, fort, ruling, prosperity);
    internal static void Run()
    {
        var raw = new[] { Fort("a", 0), Fort("A", 1), Fort("b", 1), Fort("village", 2, fort: false), Fort("gone", 3, active: false) };
        Test.True(WorldDiplomacyWorldProfileRules.SelectBorderFortIndices(raw).SequenceEqual(new[] { 0, 2 }),
            "border selection retains first case-insensitive fort identity and rejects nonfort/eliminated owners");
        foreach (int count in new[] { 0, 1 })
        {
            var borders = WorldDiplomacyWorldProfileRules.BuildBorders(raw.Take(count).ToArray(), new float[count, count], out float threshold);
            Test.True(borders.Count == 0 && threshold == 24f, "empty/single fort uses the default border threshold");
        }
        var forts = new[] { Fort("a", 0), Fort("b", 1), Fort("c", 2), Fort("d", 3) };
        var pairs = WorldDiplomacyWorldProfileRules.BuildBorders(forts,
            new float[,] { { 0, 1, 2, 20 }, { 1, 0, 5, 3 }, { 2, 5, 0, 4 }, { 20, 3, 4, 0 } }, out float lower);
        Test.True(lower == 24f && !pairs.ContainsKey(WorldDiplomacyRoundLifecycleRules.PairKey("k0", "k3")),
            "foreign neighbor limit excludes a third neighbor even within the distance threshold");
        Test.True(pairs[WorldDiplomacyRoundLifecycleRules.PairKey("k0", "k1")].FirstSettlementId == "a",
            "equal reverse pair distance keeps the first direction and names");
        WorldDiplomacyWorldProfileRules.BuildBorders(forts,
            new float[,] { { 0, 1, 40, 40 }, { 1, 0, 40, 40 }, { 40, 40, 0, 8 }, { 40, 40, 8, 0 } }, out float median);
        Test.True(median == 28f, "even nearest-distance sample uses the upper median with float scaling");
        WorldDiplomacyWorldProfileRules.BuildBorders(forts.Take(2).ToArray(), new float[,] { { 0, 100 }, { 100, 0 } }, out float upper);
        Test.True(upper == 72f, "border threshold upper clamp remains 72");
        var sameOwner = new[] { Fort("a", 0), Fort("b", 0), Fort("c", 1), Fort("d", 1) };
        WorldDiplomacyWorldProfileRules.BuildBorders(sameOwner,
            new float[,] { { 0, 1, 50, 50 }, { 1, 0, 50, 50 }, { 50, 50, 0, 1 }, { 50, 50, 1, 0 } }, out float domestic);
        Test.True(domestic == 24f, "same-country forts still determine the neighbor-distance threshold");
        Test.True(WorldDiplomacyWorldProfileRules.SelectCourtIndex(new[] { Fort("rich", 0, 900), Fort("ruler", 0, 1, ruling: true) }) == 1,
            "court prioritizes ruling ownership over prosperity");
        Test.True(WorldDiplomacyWorldProfileRules.SelectCourtIndex(new[] { Fort("z", 0, 90), Fort("B", 0, 90), Fort("a", 0, 89) }) == 1,
            "court fallback uses prosperity then case-insensitive identity");
        Test.True(WorldDiplomacyWorldProfileRules.SelectCourtIndex(Array.Empty<WorldDiplomacyFortSnapshot>()) == -1,
            "no fort has no court");
        var clans = Enumerable.Range(0, 10).Select(i => Clan("c" + i, tier: 3, influence: 20)).ToArray();
        Test.True(WorldDiplomacyWorldProfileRules.SelectRealmClanIndices(clans).SequenceEqual(Enumerable.Range(0, 8)),
            "realm top-eight retains source ordering on equal tier and influence");
        clans[9] = Clan("ruler", ruling: true);
        Test.True(WorldDiplomacyWorldProfileRules.SelectRealmClanIndices(clans).SequenceEqual(new[] { 9, 0, 1, 2, 3, 4, 5, 6 }),
            "ruling clan leads the bounded sample regardless of tier");
        var profile = WorldDiplomacyWorldProfileRules.BuildRealmProfile(new[] { Clan("a", tier: -10) },
            new[] { Clan("b"), Clan("c") }, new int[,] { { 10, -10 } }, 30);
        Test.True(profile.AverageRelation == 0f && profile.PositiveRatio == .5f && profile.HostileRatio == .5f
            && profile.Polarization == 10f && profile.RulerEliteGap == 30f && profile.SamplePairCount == 2,
            "realm relation weights clamp at one and +/-10 count inclusively");
        var empty = WorldDiplomacyWorldProfileRules.BuildRealmProfile(Array.Empty<WorldDiplomacyClanSnapshot>(), clans, new int[0, 10], 27);
        Test.True(empty.AverageRelation == 27 && empty.Polarization == 0 && empty.SamplePairCount == 0, "empty realm falls back to ruler relation");
        var candidates = new[] { Clan("normal"), Clan("no-leader", leader: null!), Clan("r", ruling: true),
            Clan("gone", eliminated: true), Clan("hire", service: true), Clan("merc", mercenary: true),
            Clan("foreign", belongs: false), Clan("same-leader", leaderIsRuler: true), Clan("", leader: "blank-clan-leader") };
        Test.True(WorldDiplomacyWorldProfileRules.SelectThreatClans(candidates).SetEquals(new[] { "normal", "no-leader", "same-leader" }),
            "threat qualification permits missing leaders but requires clan identity");
        Test.True(WorldDiplomacyWorldProfileRules.SelectPrestigeLeaders(candidates).SequenceEqual(new[] { "v", "blank-clan-leader" }),
            "prestige qualification requires distinct leader, preserving its different clan-ID semantics");
        foreach (WorldDiplomacyLifecycleEvent lifecycle in Enum.GetValues<WorldDiplomacyLifecycleEvent>())
        {
            var orchestration = new FakeOrchestration();
            var source = new LifecycleSource(orchestration.Calls);
            WorldDiplomacyLifecycleApplication.Run(lifecycle, orchestration, ref source);
            string[] expected = lifecycle == WorldDiplomacyLifecycleEvent.NewGame
                ? new[] { "ResetStorageForNewGame", "EnsureScheduleInitialized", "reset:new-game" }
                : new[] { "NormalizeStorage", "RecoverUnsettledAiInternationalReputation", "RecoverPlayerCourtReceiptsFromKnowledge",
                    "EnsureScheduleInitialized", lifecycle == WorldDiplomacyLifecycleEvent.Loaded ? "reset:game-loaded" : "reset:session-launched", "ReconcileActiveDiplomacyAfterLoad" };
            Test.True(orchestration.Calls.SequenceEqual(expected), "Application owns complete lifecycle order: " + lifecycle);
        }
    }
    private readonly struct LifecycleSource : IWorldDiplomacyLifecycleSource
    {
        private readonly List<string> _events;
        internal LifecycleSource(List<string> events) => _events = events;
        public bool StartAtPeace => true;
        public void ResetTransientRuntime(string reason) => _events.Add("reset:" + reason);
    }
}
