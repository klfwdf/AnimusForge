using System.Reflection;
using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class EventRecoveryReplay
{
    private sealed class Host : AidRequestReplay.Host, IDiplomacyEventHost
    {
        internal bool EffectsAvailable;
        internal float Relation;
        public override IWorldDiplomacyJobPreparationPort JobPreparation() => new Preparation(this);
        private sealed class Preparation : IWorldDiplomacyJobPreparationPort
        {
            private readonly Host h;
            internal Preparation(Host host) => h = host;
            public WorldDiplomacyStorage Storage => h.Owner.CurrentStorage;
            public int GenerationMaxTokens => 1000;
            public int AnalysisMaxTokens => 1000;
            public (int minimum, int maximum) CharacterRange() => (0, 300);
            public bool KingdomExists(string id) => h.PartyResolved(id);
            public WorldDiplomacyRound ResolveRound(string id) => h.Owner.ResolveRound(id);
            public string CommonContract(WorldDiplomacyRound round) => "";
            public WorldDiplomacyDocument ResolveDocument(string id) => h.Owner.ResolveDocument(id);
            public bool TryBuildProfile(string author, string marker, out string prompt) { prompt = marker + " Current ruler " + h.PartyRulerId(author); return true; }
            public void LogProfile(WorldDiplomacyJob job, string prompt) { }
        }
        public long EventGeneration => 1;
        public string CaptureEventWorld(IReadOnlyCollection<string> participants) => "{}";
        public bool TryReadEventEffect(DiplomacyEventEffectState e, out float value, out float min, out float max)
        { value = Relation; min = -100; max = 100; return EffectsAvailable; }
        public void ApplyEventEffectDelta(DiplomacyEventEffectState e, float delta) => Relation += delta;
        public bool ShowEvent(DiplomacyEventPresentation data, out string imageId, Action ack, IReadOnlyList<string> recent)
        { imageId = ""; return false; }
    }
    private static object? Invoke(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args);
    private static (Host h, WorldDiplomacyOrchestration o) Fixture()
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o; return (h, o);
    }
    private static void PublishOpening(Host h, WorldDiplomacyOrchestration o, string roundId)
    {
        h.EffectsAvailable = true;
        var round = o.ResolveRound(roundId);
        var parties = round.Participants.Select(x => x.KingdomId).ToList();
        var job = o.CurrentStorage.Jobs.Single(x => x.RoundId == roundId && x.Kind == "event_boundary");
        job.SuspendedReason = ""; job.RetryAfterUtcTicks = 0;
        o.PrepareSharedRequest(job);
        Test.True((bool)JObject.Parse(job.UserPrompt)["needs_foundation"]!, "actual oral/player opening request requires durable background and positions");
        var body = new JObject { ["background"] = "The courts are addressing their stated undertaking and its unresolved conditions.",
            ["positions"] = new JArray(round.Participants.Where(x => x.State != "observer" && !h.IsPlayerParty(x.KingdomId)).Select(x => EventBoundaryReplay.Position(x.KingdomId))), ["title"] = "A promised arrangement", ["body"] = "The courts announce the arrangement and its remaining conditions.",
            ["scene"] = "Negotiation", ["effects"] = new JArray(new JObject { ["kind"] = "ruler_relation", ["kingdom"] = parties[0],
                ["other"] = parties[1], ["delta"] = 1, ["days"] = 0, ["reason"] = "Public recognition of the undertaking." }),
            ["remove_effects"] = new JArray(), ["confirmed_documents"] = new JArray(), ["narrative"] = new JArray() };
        foreach (var response in new[] { body, new JObject { ["approved"] = true } })
        {
            o.PrepareSharedRequest(job);
            Test.True(o.TryCompleteDiplomacyPipeline(job, new LlmJobResult { JobId = job.JobId, Success = true, Content = (response["approved"] != null ? MaterialCountryThirdReplay.Approved(job) : response).ToString() }), "real boundary pipeline accepts recovery response");
        }
        Test.True(round.EventBeginningId != "" && h.Relation == 1, "recovered opening applies its actual effect before pending execution");
        var review = new WorldDiplomacyJob { Kind = "event_progress", RoundId = round.RoundId, AuthorKingdomId = round.InitiatorKingdomId };
        o.PrepareSharedRequest(review);
        Test.True(review.UserPrompt.Contains(round.EventBackground) && round.EventBackground.Length > 0
            && round.Participants.Where(x => x.State != "observer" && !h.IsPlayerParty(x.KingdomId)).All(x => x.Agenda != "" && x.ExpectedCost != "" && x.Alternative != ""),
            "the real subsequent request consumes the original opening foundation");
    }
    internal static void Run()
    {
        var (h, o) = Fixture();
        o.SubmitOralDiplomaticCommitment("ruler_a", "a", "action=Trade;move=NewMatter;target=p;days=14",
            new DialogueInteractionOrigin("courier", "trade", "session", "Trade fourteen days", "Agreed"));
        var promise = o.CurrentStorage.DialogueArrangements.Single();
        // Retained pre-admission promise recovery; new oral planning is covered
        // end to end in CountryAdmissionReplay with the current ports.
        o.ResolveRound(promise.RoundId).CountryAdmissionVersion=0;
        o.CurrentStorage.Jobs.Clear(); promise.GenerationFailureAttempts = 3;
        Invoke(o, "MaintainDialogueArrangements");
        Test.True(h.TradeExecutions == 0 && DiplomacyCommitmentRules.IsPending(promise), "accepted oral promise remains durable until a real opening is public");
        Test.True(o.CurrentStorage.EventAnnouncements.Count == 0 && o.CurrentStorage.Jobs.Any(x => x.Kind == "event_boundary"), "boundary remains technical work, not a fabricated public outcome");
        var boundary = o.CurrentStorage.Jobs.First(x => x.Kind == "event_boundary");
        for (int i = 0; i < 3; i++) Invoke(o, "RetryEventBoundary", boundary, "no_effect_available");
        Invoke(o, "MaintainDialogueArrangements");
        Test.True(h.TradeExecutions == 0 && boundary.SuspendedReason != "" && DiplomacyCommitmentRules.IsPending(promise), "failed boundary neither executes secretly nor revokes the accepted promise");
        PublishOpening(h, o, promise.RoundId);
        Invoke(o, "MaintainDialogueArrangements"); Invoke(o, "MaintainDialogueArrangements");
        Test.True(h.TradeExecutions == 1 && promise.Status == "executed", "recovery executes the accepted oral arrangement exactly once after the actual opening");
        var (ah, ao) = Fixture(); ah.War("a", "enemy");
        var aid = ao.CaptureAidRequest("a", "b", "enemy", "war_1", "legacy_accepted_decision", true, 250);
        Invoke(ao, "MaintainAidRequests");
        Test.True(aid.Status == "accepted" && ah.WarExecutions == 0 && ah.CostExecutions == 0, "accepted ally duty and its cost remain pending while the opening is unavailable");
        Test.True(ao.CurrentStorage.EventAnnouncements.Count == 0 && ao.ResolveDocument(aid.AnswerDocumentId)?.IsReadyForPublication != true, "unpublished aid cannot masquerade as an executed public declaration");
        ah.CurrentDayValue++; Invoke(ao, "MaintainAidRequests");
        Test.True(ah.WarExecutions == 0 && ah.CostExecutions == 0, "maintenance cannot bypass the aid opening boundary");
        PublishOpening(ah, ao, aid.RoundId);
        Invoke(ao, "MaintainAidRequests"); Invoke(ao, "MaintainAidRequests");
        Test.True(aid.Status == "fulfilled" && ah.WarExecutions == 1 && ah.CostExecutions == 1, "recovery fulfills accepted aid and agreed cost exactly once after the opening: " + JsonConvert.SerializeObject(new { aid, ah.WarExecutions, ah.CostExecutions, documents = ao.CurrentStorage.Documents.Select(x => new { x.DocumentId, x.Intent, x.Commitment, x.AnalysisStatus, x.MechanicalResult, x.EventExecutionPending, x.IsReadyForPublication }) }));
        var (wh, wo) = Fixture();
        Test.True(wo.CapturePlayerHostility("p", "b", "war_1") == null, "no factual war case before actual war");
        wh.War("p", "b");
        var round = wo.CapturePlayerHostility("b", "p", "war_1");
        Test.True(round != null && round.BirthLane == "player" && round.EventSourceType == "player_hostility", "actual player hostility opens its own factual event regardless of native argument order");
        Test.True(wo.CurrentStorage.Documents.Count == 0 && wh.WarExecutions == 0, "actual war never invents a player statement or executes war again");
        Test.True(wo.CurrentStorage.Jobs.Any(x => x.Kind == "generate" && x.AuthorKingdomId == "b") && wo.CurrentStorage.Jobs.Any(x => x.Kind == "event_boundary"), "neutral opening and NPC response are independently scheduled");
        var responseJob = wo.CurrentStorage.Jobs.Single(x => x.Kind == "generate");
        Test.True(!wo.CanDispatchDiplomacyJob(responseJob), "queued NPC response cannot draft ahead of the player's actual opening foundation");
        string originalPlayerRedLine = round.Participants.Single(x => x.KingdomId == "p").RedLine;
        PublishOpening(wh, wo, round.RoundId);
        Test.True(wo.CanDispatchDiplomacyJob(responseJob) && round.Participants.Single(x => x.KingdomId == "p").RedLine == originalPlayerRedLine,
            "published opening releases response drafting while unstated player positions remain unknown");
        Test.True(wo.CapturePlayerHostility("p", "b", "war_1").RoundId == round!.RoundId && wo.CurrentStorage.ConcurrentRounds.Count == 1, "same native war notification cannot duplicate case");
        var saved = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(wo.CurrentStorage))!;
        var loaded = new WorldDiplomacyOrchestration(wh, new WorldDiplomacyRuntimeState()); wh.Owner = loaded; loaded.ReplaceStorage(saved);
        Test.True(loaded.CapturePlayerHostility("p", "b", "war_1").RoundId == round.RoundId && saved.ConcurrentRounds.Count == 1, "war identity survives reload");
        Test.True(loaded.CapturePlayerHostility("p", "b", "war_2").RoundId != round.RoundId, "distinct actual war can open a distinct event");
        wh.Enabled = false;
        Test.True(loaded.CapturePlayerHostility("p", "b", "war_3") == null, "disabled system does not open hostility cases");
    }
}
