using AnimusForge;
using AnimusForge.Refactor.Domain;

internal static class PublicationRoutingReplay
{
    private sealed class Port : IWorldDiplomacyPublicationPort
    {
        public WorldDiplomacyStorage Storage { get; } = new();
        internal readonly List<string> Events = new();
        internal bool Allowed = true;
        internal int Captures;
        internal bool FailCapture;
        public string ResolveKingdomId(string id) => id == "missing" ? null : id;
        public bool CanAiAuthor(string id, out string reason) { Events.Add("authority"); reason = "blocked"; return Allowed; }
        public bool HasAuthority(string id) => id != "vassal";
        public bool IsPlayerAffiliated(string id) => id == "a";
        public bool IsPlayerKingdom(string id) => id == "a";
        public bool RepresentsAddressedVassal(string id, WorldDiplomacyDocument document) => false;
        public WorldDiplomacyRound ResolveRound(string id) => Storage.ActiveRound;
        public string ResolveOriginSettlementId(string author) { Events.Add("origin"); return "origin"; }
        public WorldDiplomacyPublicationSnapshot CaptureDestinations(string author, string origin)
        {
            Captures++; Events.Add("snapshot");
            if (FailCapture) throw new InvalidOperationException("geography unavailable");
            return new WorldDiplomacyPublicationSnapshot(
                new[] { new WorldDiplomacyPropagationApplication.SettlementTarget { Id = "village", Distance = 10 } },
                new[] { new WorldDiplomacyPropagationApplication.CourtTarget { KingdomId = "b", SettlementId = "court", Distance = 5 } }, 10, 5);
        }
        public int CurrentDay => 10;
        public int ParticipantLimit => 4;
        public int CivilianSpreadDays => 4;
        public int CourtDeliveryDays => 2;
        public void Log(string message) => Events.Add("log");
    }
    private sealed class Orch : FakeOrchestration
    {
        private readonly Port _p;
        internal Orch(Port port) { _p = port; }
        public override WorldDiplomacyRound EnsureActiveRound(string initiatorId, string targetId, bool isPlayerInsertion)
        { _p.Events.Add("round"); return _p.Storage.ActiveRound = new WorldDiplomacyRound { RoundId = "r", State = "active" }; }
        public override void RecordDiplomacyWeeklyMaterial(WorldDiplomacyDocument doc) => _p.Events.Add("weekly");
        public override void SuppressInvalidDocumentBeforePropagation(WorldDiplomacyDocument doc, string reason) => _p.Events.Add("reject:" + reason);
        public override void TryScheduleMandatoryCourtResponse(WorldDiplomacyRound round, WorldDiplomacyRoundParticipant participant, string receiverId, WorldDiplomacyDocument trigger) => _p.Events.Add("respond:" + receiverId);
    }
    internal static void Run()
    {
        var immediate = new Port { FailCapture = true };
        var orchIm = new Orch(immediate);
        var player = new WorldDiplomacyDocument { DocumentId = "player", AuthorKingdomId = "a", TargetKingdomId = "b", IsPlayerAuthored = true };
        WorldDiplomacyDocumentPublicationApplication.PublishPlayerImmediately(player, immediate, orchIm);
        Test.True(player.IsReadyForPublication && player.AnalysisStatus == "pending_analysis" && !player.PropagationCompleted
            && immediate.Events.Last() == "log", "immediate player publication survives delivery failure while awaiting analysis");
        immediate.FailCapture = false;
        WorldDiplomacyDocumentPublicationApplication.PublishPlayerImmediately(player, immediate, orchIm);
        Test.True(player.PropagationCompleted && immediate.Storage.PropagationArrivals.Count == 2, "retry repairs propagation without hiding the document");
        WorldDiplomacyDocumentPublicationApplication.PublishPlayerImmediately(player, immediate, orchIm);
        Test.True(immediate.Storage.PropagationArrivals.Count == 2 && immediate.Captures == 2, "duplicate immediate publication preserves completed delivery");
        var missing = new WorldDiplomacyDocument { IsPlayerAuthored = true, AuthorKingdomId = "missing" };
        WorldDiplomacyDocumentPublicationApplication.PublishPlayerImmediately(missing, immediate, orchIm);
        Test.True(missing.IsReadyForPublication && missing.AnalysisStatus == "pending_analysis" && immediate.Captures == 2,
            "unresolved author remains visible without triggering world capture");
        var ai = new WorldDiplomacyDocument();
        WorldDiplomacyDocumentPublicationApplication.PublishPlayerImmediately(ai, immediate, orchIm);
        Test.True(!ai.IsReadyForPublication && immediate.Captures == 2, "player-only publication cannot publish an AI document");
        var p = new Port(); var orch = new Orch(p); var doc = new WorldDiplomacyDocument { DocumentId = "d", AuthorKingdomId = "a", TargetKingdomId = "b" };
        WorldDiplomacyPublicationRoutingApplication.Start(p, orch, doc, "a");
        Test.True(doc.PropagationCompleted && doc.HasReachedPlayerCourt && doc.IsReadyForPublication && p.Captures == 1
            && p.Events.IndexOf("authority") < p.Events.IndexOf("round") && p.Events.IndexOf("weekly") < p.Events.IndexOf("snapshot"),
            "publication admission, canonical state, weekly material and bounded snapshot preserve order");
        Test.True(p.Storage.PropagationArrivals.Count == 2 && p.Storage.PropagationArrivals[0].DueDay <= p.Storage.PropagationArrivals[1].DueDay,
            "geography snapshot schedules sorted civilian and court arrivals");
        WorldDiplomacyPublicationRoutingApplication.Start(p, orch, doc, "a");
        Test.True(p.Captures == 1 && p.Storage.PropagationArrivals.Count == 2, "completed publication never recaptures or duplicates deliveries");
        p = new Port { Allowed = false }; orch = new Orch(p); doc = new WorldDiplomacyDocument { DocumentId = "d" };
        WorldDiplomacyPublicationRoutingApplication.Start(p, orch, doc, "a");
        Test.True(p.Events.SequenceEqual(new[] { "authority", "reject:blocked" }) && p.Captures == 0 && !doc.IsReadyForPublication,
            "rejected AI publication performs no world capture, state mutation or scheduling");
        p = new Port(); orch = new Orch(p); doc = new WorldDiplomacyDocument { DocumentId = "d", AuthorKingdomId = "a", TargetKingdomId = "b", IsPlayerAuthored = true };
        WorldDiplomacyPublicationRoutingApplication.Start(p, orch, doc, "a");
        foreach (string receiver in new[] { "a", "b", "bystander", "vassal", "missing" })
            WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(p.Storage.KingdomKnowledge, receiver, "d", 10);
        p.Events.Clear();
        WorldDiplomacyPublicationRoutingApplication.ReconcileReachedCourts(p, orch, doc);
        Test.True(p.Events.SequenceEqual(new[] { "respond:b" }), "reconciliation schedules only eligible, addressed, formally reached court");
        p.Events.Clear(); p.Storage.ActiveRound.State = "closed";
        WorldDiplomacyPublicationRoutingApplication.ReconcileReachedCourts(p, orch, doc);
        Test.True(p.Events.Count == 0, "closed round cannot reschedule reached-court responses");
    }
}
