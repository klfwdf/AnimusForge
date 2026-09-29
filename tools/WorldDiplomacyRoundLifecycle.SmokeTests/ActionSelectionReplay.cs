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
        public bool CanIssueWarThreat(string a, string b) => PermitWar;
        public bool CanDeclareWar(string a, string b, bool enforcing) => PermitWar;
        public int LastFailedRoundDay(WorldDiplomacyOfferCooldownKey key) => Cooldown ? 9 : -1;
        public int CooldownDays() => 5;
        public int CurrentDay() => 10;
        public WorldDiplomacyDocument ResolveDocument(string id) => null;
        public bool IsNonRootAiRelayNoActionAllowed(WorldDiplomacyRound round, string slot, string author, string target, bool relay, bool external, WorldDiplomacyDocument source)
        { NoActionCalls++; return NoAction; }
        public bool CanUseResultSettlementTarget(WorldDiplomacyRound round, string a, string b) => b == "c";
    }
    internal static void Run()
    {
        var port = new Port(); var app = new WorldDiplomacyActionSelectionApplication(port);
        Test.True(app.GetActionableDiplomaticTargets(null).Count == 0 && port.Enumerations == 0,
            "missing author does not enumerate the world");
        Test.True(app.GetActionableDiplomaticTargets("a").SequenceEqual(new[] { "b", "c" }) && port.Enumerations == 1 && port.Captures == 2,
            "one request enumerates once, filters eligibility before pair capture and preserves sorted identity order");
        port.War = true; port.ThreatReads = 0;
        Test.True(app.BuildPotentialDiplomaticActionIntents("a", "b").SequenceEqual(new[] { "propose_peace" }) && port.ThreatReads == 0,
            "war retains peace and skips threat/cooldown work");
        port.War = false;
        Test.True(string.Join(",", app.BuildPotentialDiplomaticActionIntents("a", "b")) == "warning,ultimatum,declare_war,propose_alliance,propose_trade",
            "peaceful pair preserves original ordered action options");
        port.Cooldown = true;
        Test.True(string.Join(",", app.BuildPotentialDiplomaticActionIntents("a", "b")) == "warning,ultimatum,declare_war",
            "directed failed-round cooldown suppresses only new alliance and trade proposals");
        port.Allied = port.Trading = true;
        Test.True(app.BuildPotentialDiplomaticActionIntents("a", "b").TakeLast(2).SequenceEqual(new[] { "break_alliance", "cancel_trade" }),
            "cooldown never suppresses termination of current agreements");
        var round = new WorldDiplomacyRound();
        round.PendingOffers.Add(new WorldDiplomacyRoundOffer { Status = "open", SourceDocumentId = "offer-doc", SourceActionId = "offer-action", Intent = "propose_peace", ProposerKingdomId = "b", TargetKingdomId = "a" });
        port.NoAction = true; port.NoActionCalls = 0;
        Test.True(app.BuildLegalDiplomaticDeclarationIntents(round, "a", "b", true).SequenceEqual(new[] { "accept_peace", "reject_peace" }) && port.NoActionCalls == 0,
            "exclusive peace response cannot be bypassed by no-action authorization");
        port.PermitWar = false; port.Allied = port.Trading = false; round.PendingOffers.Clear();
        round.PendingOffers.Add(new WorldDiplomacyRoundOffer { Status = "open", SourceDocumentId = "offer-doc", SourceActionId = "offer-action", Intent = "propose_alliance", ProposerKingdomId = "a", TargetKingdomId = "b" });
        Test.True(app.GetRoundPlanActionableParticipants("a", round).SequenceEqual(new[] { "b" }),
            "planning includes a candidate whose only legal role is replying to the author's proposal");
        round.ResultSettlementPending = true;
        Test.True(app.GetResultSettlementActionableTargets(round, "a").SequenceEqual(new[] { "c" }),
            "settlement target admission composes the current slot with authorized statement fallback");
    }
}
