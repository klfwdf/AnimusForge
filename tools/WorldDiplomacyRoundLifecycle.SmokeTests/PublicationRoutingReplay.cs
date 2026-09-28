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
        public string ResolveKingdomId(string id) => id == "missing" ? null : id;
        public bool CanAiAuthor(string id, out string reason) { Events.Add("authority"); reason = "blocked"; return Allowed; }
        public bool HasAuthority(string id) => id != "vassal";
        public bool IsPlayerAffiliated(string id) => id == "a";
        public bool IsPlayerKingdom(string id) => id == "a";
        public bool RepresentsAddressedVassal(string id, WorldDiplomacyDocument document) => false;
        public WorldDiplomacyRound ResolveRound(string id) => Storage.ActiveRound;
        public WorldDiplomacyRound EnsureRound(string author, string target, bool player)
        { Events.Add("round"); return Storage.ActiveRound = new WorldDiplomacyRound { RoundId = "r", State = "active" }; }
        public string ResolveOriginSettlementId(string author) { Events.Add("origin"); return "origin"; }
        public WorldDiplomacyPublicationSnapshot CaptureDestinations(string author, string origin)
        {
            Captures++; Events.Add("snapshot");
            return new WorldDiplomacyPublicationSnapshot(
                new[] { new WorldDiplomacyPropagationApplication.SettlementTarget { Id = "village", Distance = 10 } },
                new[] { new WorldDiplomacyPropagationApplication.CourtTarget { KingdomId = "b", SettlementId = "court", Distance = 5 } }, 10, 5);
        }
        public int CurrentDay => 10;
        public int ParticipantLimit => 4;
        public int CivilianSpreadDays => 4;
        public int CourtDeliveryDays => 2;
        public void RecordWeeklyMaterial(WorldDiplomacyDocument doc) => Events.Add("weekly");
        public void Reject(WorldDiplomacyDocument doc, string reason) => Events.Add("reject:" + reason);
        public void ScheduleMandatoryResponse(WorldDiplomacyRound round, WorldDiplomacyRoundParticipant participant, string receiver, WorldDiplomacyDocument doc) => Events.Add("respond:" + receiver);
        public void Log(string message) => Events.Add("log");
    }
    internal static void Run()
    {
        var p = new Port(); var doc = new WorldDiplomacyDocument { DocumentId = "d", AuthorKingdomId = "a", TargetKingdomId = "b" };
        WorldDiplomacyPublicationRoutingApplication.Start(p, doc, "a");
        Test.True(doc.PropagationCompleted && doc.HasReachedPlayerCourt && doc.IsReadyForPublication && p.Captures == 1
            && p.Events.IndexOf("authority") < p.Events.IndexOf("round") && p.Events.IndexOf("weekly") < p.Events.IndexOf("snapshot"),
            "publication admission, canonical state, weekly material and bounded snapshot preserve order");
        Test.True(p.Storage.PropagationArrivals.Count == 2 && p.Storage.PropagationArrivals[0].DueDay <= p.Storage.PropagationArrivals[1].DueDay,
            "geography snapshot schedules sorted civilian and court arrivals");
        WorldDiplomacyPublicationRoutingApplication.Start(p, doc, "a");
        Test.True(p.Captures == 1 && p.Storage.PropagationArrivals.Count == 2, "completed publication never recaptures or duplicates deliveries");
        p = new Port { Allowed = false }; doc = new WorldDiplomacyDocument { DocumentId = "d" };
        WorldDiplomacyPublicationRoutingApplication.Start(p, doc, "a");
        Test.True(p.Events.SequenceEqual(new[] { "authority", "reject:blocked" }) && p.Captures == 0 && !doc.IsReadyForPublication,
            "rejected AI publication performs no world capture, state mutation or scheduling");
        p = new Port(); doc = new WorldDiplomacyDocument { DocumentId = "d", AuthorKingdomId = "a", TargetKingdomId = "b", IsPlayerAuthored = true };
        WorldDiplomacyPublicationRoutingApplication.Start(p, doc, "a");
        foreach (string receiver in new[] { "a", "b", "bystander", "vassal", "missing" })
            WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(p.Storage.KingdomKnowledge, receiver, "d", 10);
        p.Events.Clear();
        WorldDiplomacyPublicationRoutingApplication.ReconcileReachedCourts(p, doc);
        Test.True(p.Events.SequenceEqual(new[] { "respond:b" }), "reconciliation schedules only eligible, addressed, formally reached court");
        p.Events.Clear(); p.Storage.ActiveRound.State = "closed";
        WorldDiplomacyPublicationRoutingApplication.ReconcileReachedCourts(p, doc);
        Test.True(p.Events.Count == 0, "closed round cannot reschedule reached-court responses");
    }
}
