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
        public IWorldDiplomacyNoActionPort CaptureNoActionPort(string author, string target)
        { NoActionCalls++; return new NoActionPort(author, target); }
        public int LastFailedRoundDay(WorldDiplomacyOfferCooldownKey key) => Cooldown ? 9 : -1;
        public int CooldownDays() => 5;
        public int CurrentDay() => 10;
        public WorldDiplomacyDocument ResolveDocument(string id) => null;
        public bool CanUseResultSettlementTarget(WorldDiplomacyRound round, string a, string b) => b == "c";
    }
    private sealed class Admission : IWorldDiplomacyWarAdmissionPort
    {
        private readonly bool _permit;
        internal Admission(bool permit) => _permit = permit;
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
    private sealed class NoActionPort : IWorldDiplomacyNoActionPort
    {
        internal NoActionPort(string author, string target) { AuthorId = author; TargetId = target; }
        public bool AuthorResolved => true;
        public bool TargetResolved => true;
        public bool SameParty => false;
        public bool AuthorIsPlayer => false;
        public bool AuthorEliminated => false;
        public bool TargetEliminated => false;
        public bool AuthorHasAuthority => true;
        public bool TargetHasAuthority => true;
        public string AuthorId { get; }
        public string TargetId { get; }
        public int MaxParticipants => 8;
        public WorldDiplomacyDocument ResolveDocument(string id) => id == "root"
            ? new WorldDiplomacyDocument { DocumentId = "root", IsReadyForPublication = true, Intent = "declare_war" } : null;
        public bool IsRepresentativeFor(WorldDiplomacyDocument document) => false;
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
        round.State = "active";
        round.RootDocumentId = "root";
        round.ResultSettlementCurrentSlotId = "slot";
        round.RelayRouteKingdomIds = new List<string> { "a", "c" };
        round.ResultSettlementSlots = new List<WorldDiplomacyResultSettlementSlot>
            { new() { SlotId = "slot", KingdomId = "a", RelatedKingdomIds = new List<string> { "c" } } };
        Test.True(app.GetResultSettlementActionableTargets(round, "a").SequenceEqual(new[] { "c" }),
            "settlement target admission composes the current slot with authorized statement fallback");
    }
}
