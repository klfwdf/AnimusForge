using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Real production transitions with detached campaign/transport leaves.
internal static class LifecycleRepairReplay
{
    private static object? Invoke(WorldDiplomacyOrchestration owner, string method, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(owner, args);

    internal static void Run()
    {
        DeadlinesAndRetirement();
        PlayerRouting();
        Submission();
        TreatyClauses();
    }

    private static void DeadlinesAndRetirement()
    {
        foreach (bool running in new[] { false, true })
        foreach (bool settlement in new[] { false, true })
        foreach (bool breaker in new[] { false, true })
        {
            var state = new WorldDiplomacyStorage();
            var round = new WorldDiplomacyRound { RoundId = "r", State = "active", HardEndDay = 10,
                ResultSettlementPending = settlement, AutomaticCircuitBreakerTripped = breaker };
            state.ActiveRound = round;
            state.Jobs.Add(new() { JobId = "own", RoundId = "r", Kind = "generate", IsRunning = running });
            state.Jobs.Add(new() { JobId = "other", RoundId = "other", Kind = "generate" });
            int closed = 0;
            WorldDiplomacyRoundApplication.ProcessRoundLifecycle(state, () => 50, _ => null, (_, _) => { }, _ => { }, _ => { },
                why => { closed++; WorldDiplomacyRoundApplication.Close(state, why, () => 50, _ => { }, _ => { }, (_, _) => { }, () => { }, _ => { }, round); }, _ => { }, round);
            Test.True(closed == (running ? 0 : 1), "only an actual running request delays terminal closure");
            Test.True(state.Jobs.Any(x => x.JobId == "other") && state.Jobs.Any(x => x.JobId == "own") == running,
                "closure retires scoped jobs without consuming unrelated capacity");
        }
        var (h, o) = ConcurrentOralMigrationReplay.Fixture();
        var state2 = new WorldDiplomacyStorage { RoundSchedulingSchemaVersion = 1 };
        state2.CompletedRounds.Add(new() { RoundId = "closed", State = "closed" });
        state2.Jobs.Add(new() { JobId = "old", RoundId = "closed", Kind = "generate" });
        state2.Jobs.Add(new() { JobId = "missing", RoundId = "missing", Kind = "round_plan" });
        state2.Jobs.Add(new() { JobId = "archive", Kind = "compress" });
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(state2))!);
        o.ReconcileActiveDiplomacyAfterLoad();
        Test.True(o.CurrentStorage.Jobs.Select(x => x.JobId).SequenceEqual(new[] { "archive" }), "old-save orphan retirement retains archive compression");
        o.ReconcileActiveDiplomacyAfterLoad();
        Test.True(o.CurrentStorage.Jobs.Count == 1, "load reconciliation is idempotent");

        var active = o.EnsureActiveRound("a", "b", false);
        var inflight = new WorldDiplomacyJob { JobId = "lease", Kind = "generate", RoundId = active.RoundId };
        o.CurrentStorage.Jobs.Add(inflight);
        Test.True(o.RequestLeases.TryClaim(inflight, 1, 512, 1000, false, out var lease), "claim real lease before close");
        o.CloseRound("manual_close", active);
        Test.True(o.RequestLeases.Count == 1 && !o.CurrentStorage.Jobs.Contains(inflight), "record retirement retains actual inflight lease");
        Test.True(o.RequestLeases.TryRelease(lease.JobId, 1, lease.Attempt) && o.RequestLeases.Count == 0, "late completion releases matching lease without replay");
    }

    private static void PlayerRouting()
    {
        var (h, o) = ConcurrentOralMigrationReplay.Fixture();
        var old = o.EnsureActiveRound("a", "p", false);
        var source = new WorldDiplomacyDocument { DocumentId = "old-source", RoundId = old.RoundId, AuthorKingdomId = "a", IsReadyForPublication = true };
        var player = new WorldDiplomacyDocument { DocumentId = "player", RoundId = old.RoundId, ExchangeId = old.RoundId,
            AuthorKingdomId = "p", TargetKingdomId = "a", SourceDocumentId = source.DocumentId, IsPlayerAuthored = true,
            IsReadyForPublication = true, AnalysisStatus = "pending_analysis", Body = "仍愿讨论", ResultSettlementSlotId = "old-slot" };
        o.CurrentStorage.Documents.AddRange(new[] { source, player });
        old.RootDocumentId = source.DocumentId;
        var analysis = new WorldDiplomacyJob { JobId = "analysis", Kind = "analyze", RoundId = old.RoundId,
            ExchangeId = old.RoundId, DocumentId = player.DocumentId, IsRunning = true };
        o.CurrentStorage.Jobs.Add(analysis);
        o.CurrentStorage.PropagationArrivals.Add(new() { DocumentId = player.DocumentId, RoundId = old.RoundId, KingdomId = "a", Scope = "court", DueDay = 15 });
        Test.True(ReferenceEquals(o.EnsurePlayerDocumentRound(player), old), "open usable event preserves original insertion");
        o.CloseRound("relay_hard_end", old);
        var independent = o.ResolveRound(player.RoundId);
        Test.True(independent != null && independent.State == "active" && independent.IsPlayerInsertion && independent.RootDocumentId == player.DocumentId,
            "closure moves pending player analysis into its own live event");
        Test.True(player.Body == "仍愿讨论" && player.SourceDocumentId == source.DocumentId && player.IsReadyForPublication
            && player.ResultSettlementSlotId == "", "rehome retains same public artifact and context without old settlement slot");
        Test.True(analysis.RoundId == independent.RoundId && analysis.ExchangeId == independent.RoundId && analysis.IsRunning
            && o.CurrentStorage.Jobs.Contains(analysis), "pending/running analysis keeps transport identity and follows document");
        Test.True(o.CurrentStorage.PropagationArrivals.Single().RoundId == independent.RoundId, "scheduled publication delivery follows reassigned event");
        Test.True(ReferenceEquals(o.EnsurePlayerDocumentRound(player), independent), "repeat late-result routing creates no duplicate event");
        player.AnalysisStatus = "success"; player.Intent = "statement";
        Invoke(o, "RegisterPlayerResponseWork", player, false);
        Test.True(independent.PlayerResponses.Single().KingdomId == "a", "new event owns explicit response obligation");
        Test.True(!o.CurrentStorage.Jobs.Any(x => x.Kind == "generate"), "unreceived player document does not start reply");
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge, "a", player.DocumentId, 15);
        WorldDiplomacyDocumentFactRules.RecordNobleKnowledge(o.CurrentStorage.NobleKnowledge, "a", player.DocumentId, 15);
        Test.True(o.CanDispatchDiplomacyJob(new() { Kind = "generate", JobId = "owed", RoundId = independent.RoundId,
            AuthorKingdomId = "a", SourceDocumentId = player.DocumentId, IsExternalResponseOnly = true }), "delivered independent reply is dispatchable");

        var blocked = o.EnsureActiveRound("b", "p", false);
        blocked.ResultSettlementPending = true; blocked.RelayRouteKingdomIds.Add("b");
        // This fixture's MaxRelayParticipants is zero: adding a new target is denied.
        player.RoundId = blocked.RoundId; player.ExchangeId = blocked.RoundId;
        Test.True(o.EnsurePlayerDocumentRound(player).RoundId != blocked.RoundId, "unusable settlement insertion becomes independent");

        var legacy = new WorldDiplomacyStorage { RoundSchedulingSchemaVersion = 1 };
        legacy.CompletedRounds.Add(new() { RoundId = "legacy", State = "closed" });
        var pending = new WorldDiplomacyDocument { DocumentId = "legacy-player", RoundId = "legacy", AuthorKingdomId = "p", TargetKingdomId = "a",
            IsPlayerAuthored = true, IsReadyForPublication = true, AnalysisStatus = "pending_analysis", Body = "旧档原文" };
        legacy.Documents.Add(pending); legacy.Jobs.Add(new() { JobId = "legacy-analysis", Kind = "analyze", RoundId = "legacy", DocumentId = pending.DocumentId });
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(legacy))!);
        Invoke(o, "RecoverPlayerDocumentRoutingAndRetireClosedJobs");
        var saved = o.ResolveDocument(pending.DocumentId);
        Test.True(o.ResolveRound(saved.RoundId)?.State == "active" && o.CurrentStorage.Jobs.Single().RoundId == saved.RoundId,
            "old-save pending player document is recovered before retiring closed jobs");

        var (lateHost, lateOwner) = ConcurrentOralMigrationReplay.Fixture();
        lateHost.PublishEnabled = true;
        var finished = lateOwner.EnsureActiveRound("a", "p", false);
        finished.State = "closed"; lateOwner.CurrentStorage.CompletedRounds.Add(finished);
        lateOwner.CurrentStorage.ActiveRound = null;
        var late = new WorldDiplomacyDocument { DocumentId = "late-result", RoundId = finished.RoundId, AuthorKingdomId = "p",
            TargetKingdomId = "a", IsPlayerAuthored = true, IsReadyForPublication = true, Body = "原文保持", Intent = "statement", AnalysisStatus = "success" };
        lateOwner.CurrentStorage.Documents.Add(late);
        lateOwner.ProcessAnalyzedDocument(late, "statement", "none", false, "neutral", 1);
        Test.True(lateOwner.ResolveRound(late.RoundId)?.State == "active" && late.RoundId != finished.RoundId && late.Body == "原文保持",
            "actual analyzed-result entry rehomes a declaration after its old round closes");
        Test.True(lateOwner.ResolveRound(late.RoundId).PlayerResponses.Any(x => x.SourceDocumentId == late.DocumentId && x.KingdomId == "a"),
            "late analysis establishes response work in the new event");
        var expiredSource = new WorldDiplomacyDocument { DocumentId = "expired-offer", RoundId = finished.RoundId, AuthorKingdomId = "a",
            TargetKingdomId = "p", IsReadyForPublication = true, Intent = "propose_trade" };
        lateOwner.CurrentStorage.Documents.Add(expiredSource);
        finished.PendingOffers.Add(new() { SourceDocumentId = expiredSource.DocumentId, ProposerKingdomId = "a", TargetKingdomId = "p", Intent = "propose_trade", Status = "expired" });
        var acceptance = new WorldDiplomacyDocument { DocumentId = "expired-acceptance", RoundId = late.RoundId, AuthorKingdomId = "p",
            TargetKingdomId = "a", Intent = "accept_trade", RespondingToOfferDocumentId = expiredSource.DocumentId };
        Test.True(lateOwner.TrySettleRelayOffer(acceptance) == WorldDiplomacyOfferOutcome.Failed
            && finished.PendingOffers.Single().Status == "expired" && !acceptance.ChangedDiplomaticState,
            "independent context cannot execute or reopen an expired original offer");
    }

    private static void Submission()
    {
        var world = new PlayerWorld(); var orch = new PlayerOrch(world);
        string Submit() => WorldDiplomacyPlayerApplication.Execute(world, new WorldDiplomacyPlayerDocumentCommand("回应原文", 1, "source", "old"), orch);
        Test.True(Submit().Contains("已经公开发布") && world.Published!.RoundId == "new" && orch.Openings == 1,
            "normal shortcut uses archive compose routing before semantic analysis");
        world.Round.State = "closed";
        Test.True(WorldDiplomacyPresentationQueries.Detail(world.Source, world.Round, world.Player, _ => "today").CanReply,
            "closed foreign public document retains existing reply entry");
        Test.True(Submit().Contains("已经公开发布") && world.Published!.RoundId == "new" && world.Published.SourceDocumentId == "source"
            && orch.Openings == 2 && world.NewRound.RootDocumentId == world.Published.DocumentId, "late shortcut uses the same compose routing");
        world.Player = new(1, "p", false, true, "p");
        Test.True(Submit().Contains("不再是王国统治者") && orch.Openings == 2, "reply submission revalidates ruler before effects");
    }

    private static void TreatyClauses()
    {
        var (_, o) = ConcurrentOralMigrationReplay.Fixture();
        foreach (string field in new[] { "duration_days", "daily_tribute", "cession_settlement_id" })
        foreach (string envelope in new[] { "treaty_terms", "peace_terms" })
        foreach (string intent in new[] { "propose_tributary", "accept_tributary" })
        {
            var termsJson = JObject.Parse("{\"treaty_terms\":{\"receiving_kingdom_id\":\"a\",\"joining_kingdom_id\":\"b\"},\"peace_terms\":{}}");
            ((JObject)termsJson[envelope]!)[field] = field == "cession_settlement_id" ? JToken.FromObject("town") : JToken.FromObject(-30);
            var terms = (WorldDiplomacyDialogueTerms)Invoke(o, "ParseFormalTreatyTerms", termsJson, intent, "a", "b")!;
            var doc = new WorldDiplomacyDocument { Body = "b向a臣服。", AuthorKingdomId = "a" };
            Test.True(!o.ValidateFormalTreatyDeclaration(doc, intent, terms, "a", "b", "", "", out var reason)
                && reason == "unsupported_extra_treaty_clause", "explicit extra clause cannot disappear or fall back to original: " + field + envelope + intent);
        }
        var legal = (WorldDiplomacyDialogueTerms)Invoke(o, "ParseFormalTreatyTerms", JObject.Parse("{\"treaty_terms\":{\"receiving_kingdom_id\":\"a\",\"joining_kingdom_id\":\"b\"}}"), "propose_tributary", "a", "b")!;
        Test.True(o.ValidateFormalTreatyDeclaration(new() { Body = "b向a臣服。" }, "propose_tributary", legal, "a", "b", "", "", out _), "supported formal roles remain valid");
    }

    private sealed class PlayerWorld : IWorldDiplomacyPlayerWorld
    {
        public WorldDiplomacyPlayerContext Player { get; set; } = new(1, "p", true, true, "p");
        internal WorldDiplomacyRound Round = new() { RoundId = "old", State = "active" };
        internal WorldDiplomacyRound NewRound = new() { RoundId = "new", State = "active", IsPlayerInsertion = true };
        internal WorldDiplomacyDocument Source = new() { DocumentId = "source", RoundId = "old", AuthorKingdomId = "a", IsReadyForPublication = true };
        internal WorldDiplomacyDocument? Published;
        public bool KingdomExists(string id) => true;
        public WorldDiplomacyDocument ResolveDocument(string id) => Source;
        public WorldDiplomacyRound ResolveRound(string id) => Round;
        public int CurrentDay() => 12;
    }
    private sealed class PlayerOrch : FakeOrchestration
    {
        private readonly PlayerWorld _world;
        internal int Openings;
        internal PlayerOrch(PlayerWorld world) => _world = world;
        public override WorldDiplomacyRound EnsureActiveRound(string author, string target, bool isPlayerInsertion)
        {
            Openings++;
            return _world.NewRound = new() { RoundId = "new", State = "active", IsPlayerInsertion = true };
        }
        public override WorldDiplomacyDocument CreateDocument(string author, string target, string title, string body, string origin,
            bool isPlayerAuthored, bool isResponse, string exchangeId) => new() { DocumentId = "reply-" + Openings, AuthorKingdomId = author,
                TargetKingdomId = target, Body = body, IsPlayerAuthored = isPlayerAuthored, IsResponse = isResponse, ExchangeId = exchangeId };
        public override void AddDocument(WorldDiplomacyDocument doc) => _world.Published = doc;
        public override void PublishPlayerAuthoredDocumentImmediately(WorldDiplomacyDocument doc) => doc.IsReadyForPublication = true;
    }
}
