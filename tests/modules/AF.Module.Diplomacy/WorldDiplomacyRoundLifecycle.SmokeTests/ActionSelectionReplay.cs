using AnimusForge;
internal static class ActionSelectionReplay
{
    private sealed class Port : IWorldDiplomacyActionSelectionPort
    {
        internal int Enumerations, Captures, ThreatReads, NoActionCalls;
        internal bool War, Allied, Trading, Cooldown, NoAction;
        internal bool PermitWar = true;
        internal List<WorldDiplomacyThreat> Items = new();
        public IEnumerable<string> KingdomIds() { Enumerations++; return new[] { "c", "vassal", "b", "a", "dead" }; }
        public bool HasAuthority(string id) => id != "vassal";
        public bool IsEliminated(string id) => id == "dead";
        public WorldDiplomacyPairFacts CapturePair(string a, string b) { Captures++; return new(War, true, true, Allied, Trading); }
        public IReadOnlyList<WorldDiplomacyThreat> Threats { get { ThreatReads++; return Items; } }
        public IWorldDiplomacyWarAdmissionPort CaptureWarAdmission(string first, string second) => new Admission(PermitWar);

        public int LastFailedRoundDay(WorldDiplomacyOfferCooldownKey key) => Cooldown ? 9 : -1;
        public int CooldownDays() => 5;
        public int CurrentDay() => 10;
        public WorldDiplomacyDocument ResolveDocument(string id) => null;

    }
    internal sealed class Admission : IWorldDiplomacyWarAdmissionPort
    {
        private readonly bool _permit;
        internal Admission(bool permit) => _permit = permit;
        public bool InvolvesPlayer { get; set; }
        public bool ValidPair => true;
        public bool HasIndependentAuthority => _permit;
        public bool AtWar => false;
        public bool Allied => false;
        public bool BlocksNewOffensiveWar { get; set; }
        public bool PendingThreatDecision => false;
        public int CurrentDay => 10;
        public int PeaceProtectionDays => 0;
        public bool TryGetLastPeaceDay(out int day) { day = -1; return false; }
        public int OffensiveWarCooldownDays => 0;
        public bool TryGetLastOffensiveWarDay(out int day) { day = -1; return false; }
        public int ActiveWars => 0;
        public int MaxConcurrentOffensiveWars => 3;
    }
    internal static void Run()
    {
        var port = new Port(); var app = new WorldDiplomacyActionSelectionApplication(port);
        Test.True(app.GetActionableDiplomaticTargets(null).Count == 0 && port.Enumerations == 0,
            "missing author does not enumerate the world");
        Test.True(app.GetActionableDiplomaticTargets("a").SequenceEqual(new[] { "b", "c" }) && port.Enumerations == 1 && port.Captures == 2,
            "one request enumerates once, filters eligibility before pair capture and preserves sorted identity order");
        port.War = true; port.ThreatReads = 0;
        var warActions = app.BuildPotentialDiplomaticActionIntents("a", "b");
        Test.True(warActions.Contains("statement") && warActions.Contains("propose_peace")
            && !warActions.Contains("declare_war") && port.ThreatReads == 0, "wartime options follow actual state without threat score reads");
        port.War = false;
        var peaceActions = app.BuildPotentialDiplomaticActionIntents("a", "b");
        Test.True(peaceActions.Contains("declare_war") && peaceActions.Contains("propose_alliance") && peaceActions.Contains("propose_trade"), "eligible independent courts receive current peaceful options");
        port.Cooldown = true;
        Test.True(app.BuildPotentialDiplomaticActionIntents("a", "b").SequenceEqual(peaceActions), "retired cooldown cannot suppress a current proposal");
        port.Allied = port.Trading = true;
        var current = app.BuildPotentialDiplomaticActionIntents("a", "b");
        Test.True(current.Contains("break_alliance") && current.Contains("cancel_trade")
            && !current.Contains("propose_alliance") && !current.Contains("propose_trade"), "existing agreements expose their termination, not duplicate establishment");
        var round = new WorldDiplomacyRound { EventSystemVersion = 1, State = "active" };
        round.PendingOffers.Add(new WorldDiplomacyRoundOffer { Status = "open", SourceDocumentId = "offer-doc", SourceActionId = "offer-action", Intent = "propose_peace", ProposerKingdomId = "b", TargetKingdomId = "a" });
        port.NoActionCalls = 0;
        var actions = app.BuildLegalDiplomaticDeclarationIntents(round, "a", "b", true);
        Test.True(actions.Contains("statement") && actions.Contains("accept_peace") && actions.Contains("reject_peace")
            && port.NoActionCalls == 0, "an existing peace offer adds responses without forcing them or reading old no-action seats");
        Test.True(app.BuildLegalDiplomaticDeclarationIntents(round, "a", "b", false, null, false, null, true).SequenceEqual(actions), "player involvement does not change common action selection");
    }
}
