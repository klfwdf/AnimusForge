using AnimusForge;

internal static class ImmediateActionReplay
{
    private sealed class Port : IWorldDiplomacyImmediateActionPort
    {
        internal readonly List<string> Events = new();
        internal bool Allowed = true;
        public WorldDiplomacyStorage Storage { get; } = new();
        public bool IsPlayerDiplomacy(WorldDiplomacyDocument document) => false;
        public int CurrentDay => 42;
        public bool CanAiAuthor(string id, out string reason) { Events.Add("authority"); reason = "blocked"; return Allowed; }
        public WorldDiplomacyImmediateActionReceipt DeclareWar(string a, string t, WorldDiplomacyDocument d) { Events.Add("war"); return new(true, "war ok"); }
        public WorldDiplomacyImmediateActionReceipt BreakAlliance(string a, string t, WorldDiplomacyDocument d) { Events.Add("alliance"); return new(false, "alliance failed"); }
        public WorldDiplomacyImmediateActionReceipt CancelTrade(string a, string t, WorldDiplomacyDocument d) { Events.Add("trade"); return new(true, "trade ok"); }
        public void Log(string message) => Events.Add("log");
    }
    internal static void Run()
    {
        var p = new Port(); var d = new WorldDiplomacyDocument { DocumentId = "d" };
        WorldDiplomacyImmediateActionApplication.Execute(p, "a", "b", "declare_war", d);
        Test.True(p.Events.SequenceEqual(new[] { "authority", "war" }) && d.ChangedDiplomaticState && d.MechanicalResult == "war ok",
            "application owns immediate-intent dispatch and records the typed effect receipt");
        p = new Port(); d = new WorldDiplomacyDocument { DocumentId = "d" };
        WorldDiplomacyImmediateActionApplication.Execute(p, "a", "b", "break_alliance", d);
        Test.True(p.Events.SequenceEqual(new[] { "authority", "alliance" }) && !d.ChangedDiplomaticState,
            "failed effect receipt cannot mark a diplomatic state change");
        p = new Port { Allowed = false }; d = new WorldDiplomacyDocument { DocumentId = "d" };
        WorldDiplomacyImmediateActionApplication.Execute(p, "a", "b", "cancel_trade", d);
        Test.True(p.Events.SequenceEqual(new[] { "authority", "log" }) && !d.ChangedDiplomaticState,
            "AI authority rejection prevents game action dispatch");
        p = new Port(); d = new WorldDiplomacyDocument { IsPlayerAuthored = true, ChangedDiplomaticState = true, MechanicalResult = "prior" };
        WorldDiplomacyImmediateActionApplication.Execute(p, "a", "b", "unknown", d);
        Test.True(p.Events.Count == 0 && d.ChangedDiplomaticState && d.MechanicalResult == "prior", "unknown intent preserves the existing result");
        WorldDiplomacyImmediateActionApplication.Execute(p, "a", "b", "break_alliance", d);
        Test.True(d.ChangedDiplomaticState, "a failed later action does not erase an earlier state change");
        WorldDiplomacyImmediateActionApplication.Execute(p, "a", "b", "declare_war", d);
        Test.True(p.Storage.LastOffensiveWarDayByKingdom["a"] == 42, "Application records successful offensive-war state");
    }
}
