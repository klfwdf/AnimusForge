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
        ["unresolved"] = false, ["commitments"] = new JArray(new JObject { ["payload"] = "action=Annexation;move=NewMatter;target=p;receiving=a;joining=p",
            ["npc_quote"] = npc, ["player_agreed"] = assent, ["player_quote"] = player }) };
    private static void Complete(WorldDiplomacyOrchestration o, WorldDiplomacyJob job, JObject json) =>
        o.TryCompleteDiplomacyPipeline(job, new LlmJobResult { Success = true, Content = json.ToString() });
    internal static void Run()
    {
        EventAttribution();
        BilateralReplay.Run(); // Replaces superseded omitted-tag bilateral execution assertions.
        var (nh, no) = Fixture();
        var initiative = new DialogueInteractionOrigin("scene", "offer", "session", "How is your realm?", "I will propose annexation with a receiving p");
        no.CaptureSpeechReview("ruler_a", "a", initiative, "[ACTION:DIPLOMACY:REVIEW_REMAINING]"); var review = no.CurrentStorage.Jobs.Single();
        Complete(no, review, Extraction(initiative.FullNpcText, "", false)); Complete(no, review, JObject.Parse("{\"approved\":true}"));
        Test.True(no.CurrentStorage.DialogueArrangements.Single().ConfirmedPlayerRulerId == "", "NPC offer with unrelated player words never fabricates reciprocal consent");
        var (eh, eo) = Fixture();
        eo.CaptureSpeechReview("ruler_a", "a", new("native", "bad", "session", "Maybe", "I will consider it"), "[ACTION:DIPLOMACY:REVIEW_REMAINING]"); var bad = eo.CurrentStorage.Jobs.Single();
        Complete(eo, bad, Extraction("Invented quotation", "Maybe"));
        Test.True(eo.CurrentStorage.DialogueArrangements.Count == 0 && eo.CurrentStorage.SpeechReviews.Single().Attempts == 1, "fabricated evidence cannot create a promise");
        for (int i = 1; i < 3; i++) { eh.CurrentDayValue++; Invoke(eo, "QueueSpeechReview", eo.CurrentStorage.SpeechReviews.Single()); var retry = eo.CurrentStorage.Jobs.FirstOrDefault(); if (retry == null) { eh.CurrentDayValue++; Invoke(eo, "QueueSpeechReview", eo.CurrentStorage.SpeechReviews.Single()); retry = eo.CurrentStorage.Jobs.Single(); } Invoke(eo, "FailSpeechReview", retry!, "offline_failure", false); }
        Test.True(eo.CurrentStorage.SpeechReviews.Single().Status == "technical_hold" && eo.CurrentStorage.Jobs.Count == 0, "three failures preserve evidence without unbounded timer retries");
        var (ch, co) = Fixture();
        string longNpc = new string('x', 900) + " I agree to annexation with a receiving p";
        co.CaptureSpeechReview("ruler_a", "a", new("courier", "long", "session", "Discuss annexation", longNpc), "[ACTION:DIPLOMACY:REVIEW_REMAINING]");
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
