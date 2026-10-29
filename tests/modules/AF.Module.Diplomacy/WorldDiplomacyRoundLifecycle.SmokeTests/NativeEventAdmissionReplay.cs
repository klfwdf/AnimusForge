using AnimusForge;

internal static class NativeEventAdmissionReplay
{
    internal static void Run()
    {
        var first = new WorldDiplomacyRound { RoundId = "one", State = "active", InitiatorKingdomId = "a",
            Participants = new() { new() { KingdomId = "a", State = "active" }, new() { KingdomId = "b", State = "active" }, new() { KingdomId = "c", State = "observer" } } };
        var second = new WorldDiplomacyRound { RoundId = "two", State = "active", InitiatorKingdomId = "b" };
        var rounds = new[] { first, second };
        bool Peace(string a, string b, bool enabled = true, bool authorized = false, bool player = false)
            => DiplomacyNativeAdmissionRules.AllowsPeace(enabled, authorized, player, false, rounds, a, b);
        Test.True(!Peace("a", "unrelated_enemy") && !Peace("unrelated_enemy", "b"), "participant occupation covers every frontline in either direction");
        Test.True(Peace("c", "d"), "a mere observer does not occupy native peace");
        Test.True(Peace("a", "b", enabled: false), "disabling AF releases all native peace interception");
        Test.True(Peace("a", "b", authorized: true), "validated AF agreement can execute while event occupies both sides");
        Test.True(!Peace("c", "d", player: true), "player ruler cannot bypass through native peace without event occupation");
        first.State = "closed";
        Test.True(!Peace("b", "d") && Peace("a", "d"), "closing one case releases only its own participants");
        second.State = "closed";
        Test.True(Peace("b", "d"), "last event closure releases native peace");
        first.State = "active"; first.Participants[0].State = "withdrawn"; first.Participants[1].State = "eliminated";
        Test.True(Peace("a", "b"), "withdrawn and eliminated participants do not retain occupation");
        first.Participants[1].State = "active";
        Test.True(!Peace("a", "b"), "an actual participant still holds its other frontlines");
        Test.True(DiplomacyNativeAdmissionRules.AllowsPeace(true, false, false, false, null, "a", "b"), "no events permits ordinary NPC peace, including player-vassal realm");
        RetainedHostRulesReplay.Run();
    }
}
