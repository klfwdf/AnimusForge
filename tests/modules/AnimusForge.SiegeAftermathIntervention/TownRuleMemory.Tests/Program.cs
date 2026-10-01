using System.Reflection;
using System.Text;
using AnimusForge;
using AnimusForge.SiegeAftermathIntervention;

static class Program
{
    private static int _checks;
    internal static readonly string Narrative = new string('甲', 55);
    internal static void Check(bool value, string name)
    {
        if (!value) throw new Exception("FAIL: " + name);
        _checks++;
        Console.WriteLine("PASS: " + name);
    }
    private static SettlementRuleMemoryRecord Observe(SettlementRuleMemoryStore store, int day = 100,
        string ruler = "a", string culture = "culture_a", string personality = "merciful", string town = "town")
        => store.Observe(new SettlementRuleMemoryObservation(town, town, ruler, ruler, culture,
            culture, personality, day, false)).Record;
    private static SettlementRuleMemoryRecord Current(SettlementRuleMemoryStore store, string id = "town")
    { store.TryGet(id, out var record); return record; }
    private static void Fact(SettlementRuleMemoryStore store, string id, int day = 101)
        => store.TryRecordConfirmedEvent("town", new SettlementRuleMemoryFact(id, "confirmed " + id, day));

    public static void Main()
    {
        var store = new SettlementRuleMemoryStore();
        var record = Observe(store);
        Check(SettlementRuleMemoryEvolution.ShouldGenerate(record, 100), "empty narrative generates immediately");
        Check(store.TryStoreGeneratedNarrative(record, 100, Narrative), "initial result commits");
        record = Current(store);
        Check(ReferenceEquals(record, Observe(store)), "unchanged observation reuses snapshot");
        Check(!SettlementRuleMemoryEvolution.ShouldGenerate(record, 200), "elapsed time alone does not spend a request");
        Fact(store, "one");
        var one = Current(store);
        Check(!store.TryRecordConfirmedEvent("town", new SettlementRuleMemoryFact("one", "duplicate", 101)), "duplicate fact rejected");
        Check(one.CurrentRule.Narrative == Narrative, "fact keeps last good text");
        Check(!SettlementRuleMemoryEvolution.ShouldGenerate(one, 102), "three-day generation floor");
        Check(SettlementRuleMemoryEvolution.ShouldGenerate(one, 103), "one matured change eventually refreshes");
        Check(!store.TryStoreGeneratedNarrative(record, 103, Narrative), "event arriving during request rejects late result");
        Check(store.TryStoreGeneratedNarrative(one, 103, Narrative), "fresh revision commits");
        Fact(store, "two", 106); Fact(store, "three", 106); Fact(store, "four", 106);
        Check(SettlementRuleMemoryEvolution.ShouldGenerate(Current(store), 106), "three changes bypass one-day accumulation after cadence");
        var beforeCulture = Current(store);
        var culture = Observe(store, 106, culture: "culture_b");
        Check(culture.CurrentRule.Narrative == Narrative, "culture preserves text until replacement");
        Check(!store.TryStoreGeneratedNarrative(beforeCulture, 106, Narrative), "culture mutation rejects stale response");
        var persona = Observe(store, 106, culture: "culture_b", personality: "cruel");
        Check(persona.CurrentRule.Evolution.Revision > culture.CurrentRule.Evolution.Revision, "personality invalidates generation source");
        store.TrySetNarrative("town", "a", 100, "manual", true, out var manual);
        Check(!store.TryStoreGeneratedNarrative(persona, 106, Narrative), "manual edit wins over in-flight result");
        Fact(store, "five", 107);
        var manualCulture = Observe(store, 108, culture: "culture_c");
        Check(manualCulture.CurrentRule.Narrative == "manual" && !SettlementRuleMemoryEvolution.ShouldGenerate(manualCulture, 200, true), "manual protected from facts culture and force queue");
        var b = Observe(store, 110, ruler: "b");
        Check(b.RuleStartDay == 110 && b.PreviousRuleDurationDays == 10, "owner event date freezes exact old tenure");
        Check(b.CurrentRule.Narrative.Length == 0 && b.RulerMemories[1].Narrative == "manual", "new ruler blank; historical manual untouched");
        var aAgain = Observe(store, 110, ruler: "a");
        var bAgain = Observe(store, 110, ruler: "b");
        Check(!store.TryStoreGeneratedNarrative(b, 110, Narrative), "same-day ABA owner changes reject old result");
        Check(bAgain.RulerMemories.Count == 3, "tenure bound retained");
        for (int i = 0; i < 40; i++) Fact(store, "bounded-" + i, 111);
        Check(Current(store).CurrentRule.Evolution.Facts.Count == 12, "confirmed event history bounded");
        var encoded = SettlementRuleMemorySaveCodec.Encode(store.Export());
        var decoded = SettlementRuleMemorySaveCodec.Decode(encoded);
        var restored = new SettlementRuleMemoryStore();
        Check(decoded.RejectedCount == 0 && restored.Restore(decoded.Records) == 0, "v3 save round trip");
        var restoredRule = Current(restored).CurrentRule;
        Check(restoredRule.Evolution.Revision == Current(store).CurrentRule.Evolution.Revision
            && restoredRule.Evolution.Facts.Last().Id == "bounded-39", "save preserves dirty source and event history");
        var oldV2 = "v2|" + B64("old town") + "|100|1|" + B64("a") + "|" + B64("a")
            + "|" + B64("c") + "|" + B64("c") + "|" + B64("kind") + "|100|168|0|0|" + B64("old manual") + "|1";
        Check(SettlementRuleMemoryCodec.TryDecode("legacy", oldV2, out var legacy)
            && legacy.CurrentRule.NarrativeIsManual && legacy.CurrentRule.Narrative == "old manual", "v2 manual compatibility");
        string oldV1 = "v1|" + B64("old town") + "|" + B64("a") + "|" + B64("a") + "|" + B64("c") + "|" + B64("c")
            + "|" + B64("kind") + "|100|100|168||||||0|0";
        Check(SettlementRuleMemoryCodec.TryDecode("legacy", oldV1, out legacy) && legacy.RulerId == "a", "v1 flat compatibility");
        Check(!SettlementRuleMemoryCodec.TryDecode("bad", encoded["town"] + "|extra", out _), "corrupt v3 rejected");
        var prompt = TownPromptComposer.BuildSettlementRuleMemoryGenerationPrompt(Current(store), 120, null);
        Check(prompt.UserPrompt.Contains("confirmed bounded-39") && prompt.UserPrompt.Contains("Previous prose"), "actual prompt includes bounded confirmed facts and prior prose");
        Check(SettlementRuleMemoryEventText.Policy("target_lost", "test") == "", "remaining policy targets are not falsely marked lost");
        Check(SettlementRuleMemoryEventText.CompletedIntervention("actor", "ShowMercy", 5, 6, 7, 8, 9, 10).Contains("平民死亡7"), "completed counters override nominal mercy impression");
        VerifyAsyncBridge();
        HostReplay.Run();
        MeasureBoundedCore();
        Console.WriteLine("RESULT: " + _checks + " checks passed (real core + real bridge, fake auxiliary transport).");
    }

    private static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
    private static void MeasureBoundedCore()
    {
        var store = new SettlementRuleMemoryStore();
        var observation = new SettlementRuleMemoryObservation("perf", "perf", "a", "a", "c", "c", "kind", 100, false);
        store.Observe(observation);
        for (int i = 0; i < 1000; i++) store.Observe(observation);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 50000; i++) store.Observe(observation);
        double observationUs = watch.Elapsed.TotalMilliseconds * 1000 / 50000;
        watch.Restart();
        for (int i = 0; i < 10000; i++) store.TryRecordConfirmedEvent("perf", new SettlementRuleMemoryFact("fact-" + i, "confirmed fixture", 100));
        double factUs = watch.Elapsed.TotalMilliseconds * 1000 / 10000;
        Console.WriteLine("CORE BENCHMARK (fixture, not game frame): unchanged observation=" + observationUs.ToString("F3")
            + " us, bounded event insert=" + factUs.ToString("F3") + " us; 50000/10000 iterations.");
    }
    private static void WaitCompleted(int count = 1)
    {
        var queue = typeof(GcczTownRuleMemoryGenerationBridge).GetField("Completed", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        if (!SpinWait.SpinUntil(() => (int)queue.GetType().GetProperty("Count").GetValue(queue) >= count, 5000)) throw new Exception("completion timeout");
    }
    private static void ClearRateLimit()
    {
        var rate = (Dictionary<string, DateTime>)typeof(GcczTownRuleMemoryGenerationBridge).GetField("RetryAfter", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        rate.Clear(); // Virtual passage of wall time, without waiting a minute per boundary case.
    }
    private static void VerifyAsyncBridge()
    {
        GcczTownRuleMemoryGenerationBridge.Reset();
        var store = new SettlementRuleMemoryStore();
        var record = Observe(store);
        int mainThread = Environment.CurrentManagedThreadId, commits = 0;
        GcczTownRuleMemoryGenerationBridge.TryStoreNarrative accept = (expected, day, text) => {
            Check(Environment.CurrentManagedThreadId == mainThread, "commit stays on caller main tick");
            commits++; return store.TryStoreGeneratedNarrative(expected, day, text); };
        AIConfigHandler.Reset();
        GcczTownRuleMemoryGenerationBridge.Queue(record, 100, false, accept, _ => { });
        WaitCompleted();
        Check(commits == 0 && Current(store).CurrentRule.Narrative.Length == 0, "worker cannot mutate store");
        GcczTownRuleMemoryGenerationBridge.OnApplicationTick();
        Check(commits == 1 && Current(store).CurrentRule.Narrative == Narrative, "main tick publishes successful result");
        Check(AIConfigHandler.LastRequestedTokens == 384, "town generator requests bounded 384-token output");
        GcczTownRuleMemoryGenerationBridge.Queue(Current(store), 110, true, accept, _ => { });
        Check(AIConfigHandler.Calls == 1, "force request still obeys wall-clock floor");
        ClearRateLimit();
        AIConfigHandler.Response = "invalid";
        GcczTownRuleMemoryGenerationBridge.Queue(Current(store), 110, true, accept, _ => { });
        WaitCompleted(); GcczTownRuleMemoryGenerationBridge.OnApplicationTick();
        Check(Current(store).CurrentRule.Narrative == Narrative && commits == 1, "malformed response preserves existing text");
        ClearRateLimit(); AIConfigHandler.Success = false;
        GcczTownRuleMemoryGenerationBridge.Queue(Current(store), 110, true, accept, _ => { });
        WaitCompleted(); GcczTownRuleMemoryGenerationBridge.OnApplicationTick();
        Check(Current(store).CurrentRule.Narrative == Narrative && commits == 1, "provider failure preserves text and does not commit");
        AIConfigHandler.Success = true;
        ClearRateLimit(); AIConfigHandler.Response = "{\"memory\":\"" + Narrative + "\"}";
        GcczTownRuleMemoryGenerationBridge.Queue(Current(store), 110, true, accept, _ => { });
        WaitCompleted(); Fact(store, "during-transport", 110);
        GcczTownRuleMemoryGenerationBridge.OnApplicationTick();
        Check(Current(store).CurrentRule.Evolution.Revision > Current(store).CurrentRule.Evolution.GeneratedRevision, "late callback leaves newer facts pending");
        ClearRateLimit();
        GcczTownRuleMemoryGenerationBridge.Queue(Current(store), 110, true, accept, _ => { });
        WaitCompleted(); int before = commits; SaveRuntimeGuard.Generation++;
        GcczTownRuleMemoryGenerationBridge.OnApplicationTick();
        Check(commits == before, "changed save generation drops completion before callback");
        GcczTownRuleMemoryGenerationBridge.Reset(); AIConfigHandler.Reset(); AIConfigHandler.Release.Reset();
        var first = Observe(store, town: "first"); var second = Observe(store, town: "second"); var third = Observe(store, town: "third");
        GcczTownRuleMemoryGenerationBridge.Queue(first, 100, false, accept, _ => { });
        GcczTownRuleMemoryGenerationBridge.Queue(first, 100, false, accept, _ => { });
        GcczTownRuleMemoryGenerationBridge.Queue(second, 100, false, accept, _ => { });
        GcczTownRuleMemoryGenerationBridge.Queue(third, 100, false, accept, _ => { });
        Check(SpinWait.SpinUntil(() => AIConfigHandler.Calls == 2, 5000), "global two-request bound and same-town coalescing");
        GcczTownRuleMemoryGenerationBridge.Reset();
        GcczTownRuleMemoryGenerationBridge.Queue(third, 100, false, accept, _ => { });
        Check(AIConfigHandler.Calls == 2, "save reset cannot exceed physical HTTP concurrency bound");
        AIConfigHandler.Release.Set();
        Check(SpinWait.SpinUntil(() => AIConfigHandler.Returns == 2, 5000), "retired workers finish without blocking");
        var workers = typeof(GcczTownRuleMemoryGenerationBridge).GetField("_activeWorkers", BindingFlags.NonPublic | BindingFlags.Static);
        Check(SpinWait.SpinUntil(() => (int)workers.GetValue(null) == 0, 5000), "retired requests release global slots");
        GcczTownRuleMemoryGenerationBridge.Queue(third, 100, false, accept, _ => { });
        WaitCompleted(); before = commits; GcczTownRuleMemoryGenerationBridge.OnApplicationTick();
        Check(commits == before + 1 && Current(store, "third").CurrentRule.Narrative == Narrative, "old epoch cannot retire or commit current work");
    }
}

namespace AnimusForge
{
    internal static class SaveRuntimeGuard
    {
        internal static long Generation = 1;
        internal static long CaptureGeneration() => Generation;
        internal static bool IsStale(long generation) => generation != Generation;
    }
    internal static class GcczTownPromptResourceProvider
    { internal static TownPromptTextCatalog GetCatalog() => null; }
    internal static class Logger
    { internal static bool IsModLogicEnabled => false; internal static void Log(string source, string message) { } }
    internal static class AIConfigHandler
    {
        internal static readonly ManualResetEventSlim Release = new ManualResetEventSlim(true);
        internal static int Calls, Returns, LastRequestedTokens;
        internal static string Response;
        internal static bool Success = true;
        internal static void Reset() { Calls = Returns = 0; Success = true; Release.Set(); Response = "{\"memory\":\"" + Program.Narrative + "\"}"; }
        internal static bool TryCallBoundedAuxiliarySimpleDialogueOnceForExternal(IEnumerable<object> messages,
            int tokens, float temperature, out string content, out string error)
        {
            Interlocked.Increment(ref Calls);
            LastRequestedTokens = tokens;
            if (!Release.Wait(5000)) throw new Exception("test transport blocked");
            content = Response; error = null;
            Interlocked.Increment(ref Returns);
            return Success;
        }
    }
}
