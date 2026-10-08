using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;

// Actual Orchestration -> propagation -> court -> personal-memory receipts; only world and memory commit leaves controlled.
internal static class PropagationFailureClosureReplay
{
    private sealed class Host : ConcurrentOralMigrationReplay.Host
    {
        internal int Budget = 1200;
        internal string FailCourtDocument, FailReceiver;
        internal bool FailCourtOnce, EnqueueDuringFailure;
        public override int MaxPropagationArrivalsPerDay() => Budget;
        public override string ResolveSettlementId(string id) => id;
        public override string ResolvePropagationReceiverId(string kingdomId, string settlementId)
        {
            if (FailReceiver == kingdomId) { FailReceiver = null; throw new InvalidOperationException("receiver capture unavailable"); }
            return kingdomId;
        }
        public override void Log(string message)
        {
            if (FailCourtDocument != null && message.StartsWith("court received document=" + FailCourtDocument + " ", StringComparison.Ordinal))
            {
                if (FailCourtOnce) FailCourtDocument = null;
                if (EnqueueDuringFailure)
                {
                    EnqueueDuringFailure = false;
                    Owner.CurrentStorage.PropagationArrivals.Add(Arrival("reentrant", "", "late-village", CurrentDayValue));
                }
                throw new InvalidOperationException("court notification unavailable after memory/knowledge commit");
            }
        }
    }
    private static WorldDiplomacyDocument Document(string id, string receiver) => new()
    {
        DocumentId = id, AuthorKingdomId = "a", AuthorRulerId = "ruler_a", TargetKingdomId = receiver,
        Body = id + " body", IsReadyForPublication = true, PropagationStarted = true, PropagationCompleted = true
    };
    private static WorldDiplomacyPropagationArrival Arrival(string id, string receiver, string place, int day) => new()
    { DocumentId = id, KingdomId = receiver, SettlementId = place, DueDay = day, Scope = string.IsNullOrEmpty(receiver) ? "civilian" : "court" };
    private static WorldDiplomacyOrchestration Owner(Host host, WorldDiplomacyStorage state)
    {
        var owner = new WorldDiplomacyOrchestration(host, new WorldDiplomacyRuntimeState());
        host.Owner = owner; owner.ReplaceStorage(state); return owner;
    }
    internal static void Run()
    {
        var host = new Host { CurrentDayValue = 10, FailCourtDocument = "first", FailCourtOnce = true, EnqueueDuringFailure = true };
        var state = new WorldDiplomacyStorage();
        var first = Document("first", "b"); var second = Document("second", "p");
        state.Documents.AddRange(new[] { first, second, Document("reentrant", "b") });
        state.PropagationArrivals.AddRange(new[] { Arrival("first", "b", "court-b", 10), Arrival("second", "p", "court-p", 10), Arrival("second", "", "village", 10) });
        var owner = Owner(host, state);
        owner.ProcessPropagationArrivals();
        Test.True(second.HasReachedPlayerCourt && state.SettlementKnowledge.Any(k => k.SettlementId == "village"),
            "actual first court exception cannot strand second court or civilian sibling");
        var failed = state.PropagationArrivals.Single(a => a.DocumentId == "first");
        Test.True(failed.CourtEffectPending && failed.DueDay == 11 && state.PropagationArrivals.Count == 2,
            "actual failed court keeps sole ticket at next day while reentrant arrival waits outside due prefix");
        Test.True(WorldDiplomacyDocumentFactRules.HasKingdomKnowledge(state.KingdomKnowledge, "b", "first")
            && first.PersonalMemoryReceipts.Count == 1 && host.Facts.Count == 1,
            "actual failed court retains already committed knowledge and personal-memory receipt");
        owner.ProcessPropagationArrivals();
        Test.True(state.PropagationArrivals.Count == 1 && state.SettlementKnowledge.Any(k => k.SettlementId == "late-village") && host.Facts.Count == 1,
            "same-day reentrant work can advance but deferred failed effect never spins in that day");
        var restored = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(state));
        restored.PropagationArrivals = WorldDiplomacyRoundLifecycleRules.NormalizePropagationArrivalList(restored.PropagationArrivals);
        Test.True(restored.PropagationArrivals.Single().CourtEffectPending && restored.PropagationArrivals.Single().DueDay == 11,
            "actual JSON graph and arrival normalization retain pending court ticket across save/load");
        host.CurrentDayValue = 11; owner = Owner(host, restored); owner.ProcessPropagationArrivals();
        Test.True(restored.PropagationArrivals.Count == 0 && host.Facts.Count == 1 && restored.Documents[0].PersonalMemoryReceipts.Count == 1,
            "actual retry reaches ProcessCourtArrival but committed personal-memory receipt prevents duplicate write");
        restored.PropagationArrivals.Add(Arrival("first", "b", "court-b", 11));
        owner.ProcessPropagationArrivals();
        Test.True(host.Facts.Count == 1 && restored.PropagationArrivals.Count == 0,
            "ordinary known duplicate stays idempotent without forcePending ticket");

        host.FailReceiver = "b"; restored.PropagationArrivals.Add(Arrival("first", "b", "court-b", 11));
        owner.ProcessPropagationArrivals();
        Test.True(!restored.PropagationArrivals.Single().CourtEffectPending && restored.PropagationArrivals.Single().DueDay == 12,
            "receiver capture failure before effect never fabricates a forcePending ticket for known duplicate");
        host.CurrentDayValue = 12; owner.ProcessPropagationArrivals();
        Test.True(host.Facts.Count == 1 && restored.PropagationArrivals.Count == 0,
            "capture retry consumes known duplicate without replaying actual court effect");

        var playerHost = new Host { CurrentDayValue = 4, FailCourtDocument = "player-receipt", FailCourtOnce = true };
        var playerState = new WorldDiplomacyStorage(); var playerDoc = Document("player-receipt", "p");
        playerState.Documents.Add(playerDoc); playerState.PropagationArrivals.Add(Arrival(playerDoc.DocumentId, "p", "court-p", 4));
        var playerOwner = Owner(playerHost, playerState); playerOwner.ProcessPropagationArrivals();
        Test.True(playerDoc.HasReachedPlayerCourt && playerState.PropagationArrivals.Single().CourtEffectPending,
            "actual player flag set before failed log cannot consume pending court completion");
        playerHost.CurrentDayValue = 5; playerOwner.ProcessPropagationArrivals();
        Test.True(playerState.PropagationArrivals.Count == 0 && playerDoc.HasReachedPlayerCourt,
            "actual pending retry completes even when knowledge and player reached flag already exist");

        var fairHost = new Host { CurrentDayValue = 20, Budget = 2, FailCourtDocument = "blocked" };
        var fair = new WorldDiplomacyStorage(); fair.Documents.AddRange(new[] { Document("blocked", "b"), Document("healthy", "p") });
        fair.PropagationArrivals.AddRange(new[] { Arrival("blocked", "b", "b1", 20), Arrival("blocked", "c", "c1", 20), Arrival("healthy", "p", "p1", 20) });
        var fairOwner = Owner(fairHost, fair); fairOwner.ProcessPropagationArrivals();
        Test.True(fair.PropagationArrivals.Count == 3 && fair.PropagationArrivals[0].DocumentId == "healthy" && fair.PropagationArrivals.Skip(1).All(a => a.DueDay == 21),
            "failed capped prefix moves behind unprocessed healthy work without extra same-day attempts");
        fairHost.CurrentDayValue = 21; fairOwner.ProcessPropagationArrivals();
        Test.True(fair.Documents[1].HasReachedPlayerCourt && fair.PropagationArrivals.Count == 2 && fair.PropagationArrivals.Last().DueDay == 22,
            "permanent first failure cannot starve healthy arrival on following bounded pass");
        var oldArrival = JsonConvert.DeserializeObject<WorldDiplomacyPropagationArrival>("{\"documentId\":\"old\",\"scope\":\"court\"}");
        Test.True(!oldArrival.CourtEffectPending, "old saved arrival missing new JSON field remains ordinary and deduplicated");
    }
}
