using System.Reflection;
using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class SpeechReviewReplay
{
    private sealed class Host : ConcurrentOralMigrationReplay.Host
    { internal bool Enabled = true; public override bool WorldDiplomacyEnabled() => Enabled; }
    private readonly struct Source : IDiplomacyOralTagSource, IDiplomacyCommitmentReviewSource
    {
        private readonly WorldDiplomacyOrchestration o; private readonly DialogueInteractionOrigin origin;
        internal Source(WorldDiplomacyOrchestration owner, DialogueInteractionOrigin captured) { o = owner; origin = captured; }
        public bool HasSpeaker => true; public bool IsAvailable => true; public string SpeakerHeroId => "ruler_a";
        public bool CaptureCommitmentReview(string hints) => o.CaptureSpeechReview("ruler_a", "a", origin, hints);
        private static string Unexpected() => throw new Exception("A tag bypassed speech audit");
        public string DeclareWar(string s) => Unexpected(); public string MakePeace(string s) => Unexpected();
        public string IndependentClanPeace(string s) => Unexpected(); public string FormAlliance(string s) => Unexpected();
        public string BreakAlliance(string s) => Unexpected(); public string MakeTrade(string s) => Unexpected(); public string CancelTrade(string s) => Unexpected();
        public void Log(string s) { }
    }
    private static object? Invoke(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args);
    private static (Host h, WorldDiplomacyOrchestration o) Fixture()
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o; return (h, o);
    }
    private static JObject Extraction(string npc, string player = "", bool assent = true) => new() {
        ["unresolved"] = false, ["commitments"] = new JArray(new JObject { ["payload"] = "action=Trade;move=NewMatter;target=p;days=14",
            ["npc_quote"] = npc, ["player_agreed"] = assent, ["player_quote"] = player }) };
    private static void Complete(WorldDiplomacyOrchestration o, WorldDiplomacyJob job, JObject json) =>
        o.TryCompleteDiplomacyPipeline(job, new LlmJobResult { Success = true, Content = json.ToString() });
    internal static void Run()
    {
        EventAttribution();
        foreach (string channel in new[] { "native", "scene", "courier" })
        {
            var (h, o) = Fixture();
            var origin = new DialogueInteractionOrigin(channel, "turn_" + channel, "session", "Trade for fourteen days", "I agree to trade for fourteen days");
            var source = new Source(o, origin); string text = "I agree to trade for fourteen days";
            DiplomacyOralTagApplication.Process(source, ref text);
            var job = o.CurrentStorage.Jobs.Single();
            Test.True(job.Kind == "oral_review" && o.CurrentStorage.ConcurrentRounds.Count == 0 && h.TradeExecutions == 0, "untagged actual speech is queued without a speculative case: " + channel);
            Test.True(o.CanDispatchDiplomacyJob(job) && o.RequestLane(job) == "oral", "review uses actual common oral scheduler");
            o.PrepareSharedRequest(job);
            Test.True(job.UserPrompt.Contains(origin.FullNpcText) && job.SystemPrompt != "", "real review request includes exact source and package instructions");
            Complete(o, job, Extraction(origin.FullNpcText, origin.FullPlayerText));
            Test.True(job.Kind == "oral_review_audit" && o.CurrentStorage.DialogueArrangements.Count == 0, "extraction alone cannot register consent");
            Complete(o, job, JObject.Parse("{\"approved\":true}"));
            var agreement = o.CurrentStorage.DialogueArrangements.Single();
            Test.True(agreement.ConfirmedPlayerRulerId == "ruler_p" && agreement.Terms.DurationDays == 14 && agreement.SourceChannel == channel, "audited exact original terms reach the shared commitment owner");
            o.CurrentStorage.Jobs.Clear(); agreement.GenerationFailureAttempts = 3; Invoke(o, "MaintainDialogueArrangements");
            Test.True(h.TradeExecutions == 1 && h.TradeDuration == 14, "omitted-tag promise really executes through existing declaration executor");
            Test.True(o.CurrentStorage.SpeechReviews.Count == 0 && o.CurrentStorage.ReviewedSpeechSources.Count == 1, "completed extraction retains only a light provenance receipt");
            text = "[ACTION:DIPLOMACY:MAKE_TRADE:p:14]"; DiplomacyOralTagApplication.Process(source, ref text);
            Test.True(h.TradeExecutions == 1 && text == "" && !o.CurrentStorage.Jobs.Any(x => x.Kind.StartsWith("oral_review")), "duplicate callback with tags cannot bypass source receipt");
        }
        var (nh, no) = Fixture();
        var initiative = new DialogueInteractionOrigin("scene", "offer", "session", "How is your realm?", "I will propose trade for fourteen days");
        no.CaptureSpeechReview("ruler_a", "a", initiative, ""); var review = no.CurrentStorage.Jobs.Single();
        Complete(no, review, Extraction(initiative.FullNpcText, "", false)); Complete(no, review, JObject.Parse("{\"approved\":true}"));
        Test.True(no.CurrentStorage.DialogueArrangements.Single().ConfirmedPlayerRulerId == "", "NPC offer with unrelated player words never fabricates reciprocal consent");
        var (eh, eo) = Fixture();
        eo.CaptureSpeechReview("ruler_a", "a", new("native", "bad", "session", "Maybe", "I will consider it"), ""); var bad = eo.CurrentStorage.Jobs.Single();
        Complete(eo, bad, Extraction("Invented quotation", "Maybe"));
        Test.True(eo.CurrentStorage.DialogueArrangements.Count == 0 && eo.CurrentStorage.SpeechReviews.Single().Attempts == 1, "fabricated evidence cannot create a promise");
        for (int i = 1; i < 3; i++) { eh.CurrentDayValue++; Invoke(eo, "QueueSpeechReview", eo.CurrentStorage.SpeechReviews.Single()); var retry = eo.CurrentStorage.Jobs.FirstOrDefault(); if (retry == null) { eh.CurrentDayValue++; Invoke(eo, "QueueSpeechReview", eo.CurrentStorage.SpeechReviews.Single()); retry = eo.CurrentStorage.Jobs.Single(); } Invoke(eo, "FailSpeechReview", retry!, "offline_failure", false); }
        Test.True(eo.CurrentStorage.SpeechReviews.Single().Status == "technical_hold" && eo.CurrentStorage.Jobs.Count == 0, "three failures preserve evidence without unbounded timer retries");
        var (ch, co) = Fixture();
        string longNpc = new string('x', 900) + " I agree to trade for fourteen days";
        co.CaptureSpeechReview("ruler_a", "a", new("courier", "long", "session", "Trade fourteen days", longNpc), "");
        Test.True(co.CurrentStorage.SpeechReviews.Single().NpcText == longNpc, "source beyond old 600-character excerpt remains available to extraction");
        var saved = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(co.CurrentStorage))!;
        co.ReplaceStorage(saved); var held = saved.Jobs.Single();
        Test.True(saved.SpeechReviews.Single().NpcText == longNpc && co.CanDispatchDiplomacyJob(held), "pending unbound review survives save and common scheduler admission");
        ch.Enabled = false; co.HandleDisabledState();
        Test.True(!co.AdmitCompletion(held, new LlmJobResult { Success = true }), "disabled completion cannot register a promise");
        foreach (var pair in new[] { ("event_boundary", "started"), ("event_boundary_audit", "ended"), ("player_lifecycle", "recover_published_lifecycle"), ("player_decision", "opening") })
        {
            var routed = new WorldDiplomacyJob { Kind = pair.Item1, ForcedIntent = pair.Item2 };
            WorldDiplomacyRoundLifecycleRules.NormalizeJobRecord(routed);
            Test.True(routed.ForcedIntent == pair.Item2, "save normalization preserves routing metadata: " + pair.Item1);
        }
    }
}
