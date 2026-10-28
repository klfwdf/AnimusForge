using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using AnimusForge;

internal static class AsyncEmbeddingReplay
{
    private sealed class SemanticHost : EventStructureReplay.Host, IWorldDiplomacySemanticSource
    {
        internal int Calls;
        internal bool Pending = true;
        public float[] Embed(string text)
        { Calls++; if (Pending) throw new WorldDiplomacyPreparationPendingException(); return new[] { 1f, 0f }; }
    }
    private sealed class Port : IWorldDiplomacyHistoryCapturePort
    {
        public int CurrentHour() => 960;
        public long WeeklyRevision() => 0;
        public IEnumerable<WorldDiplomacyWeeklyArtifact> WeeklyArtifacts() => Array.Empty<WorldDiplomacyWeeklyArtifact>();
    }
    private static T Ready<T>(Func<T> read)
    {
        var timer = Stopwatch.StartNew();
        while (true)
        {
            try { return read(); }
            catch (WorldDiplomacyPreparationPendingException)
            { if (timer.ElapsedMilliseconds > 10000) throw new Exception("preparation did not finish"); Thread.Sleep(1); }
        }
    }
    private static void Pending(Action read)
    {
        bool pending = false;
        try { read(); } catch (WorldDiplomacyPreparationPendingException) { pending = true; }
        Test.True(pending, "incomplete inference yields without returning a partial vector");
    }
    internal static void Run()
    {
        using var cache = new WorldDiplomacyAsyncEmbeddingCache();
        object engine = new(); int caller = Environment.CurrentManagedThreadId, calls = 0, worker = caller;
        using var gate = new ManualResetEventSlim();
        float[] Infer(string text) { Interlocked.Increment(ref calls); worker = Environment.CurrentManagedThreadId; gate.Wait(); return new[] { 1f, 0f }; }
        try
        {
            for (int i = 0; i < 3; i++) Pending(() => cache.Get("blocked" + i, 1, engine, Infer));
            Pending(() => cache.Get("fourth", 1, engine, Infer));
            Test.True(cache.Count == 3 && calls <= 3, "pending preparation is bounded to three workers");
        }
        finally { gate.Set(); }
        var first = Ready(() => cache.Get("blocked0", 1, engine, Infer));
        Test.True(worker != caller && ReferenceEquals(first, cache.Get("blocked0", 1, engine, Infer)), "inference runs off caller and completed results are reused");
        Ready(() => cache.Get("blocked1", 1, engine, Infer)); Ready(() => cache.Get("blocked2", 1, engine, Infer));
        for (int i = 0; i < 140; i++) Ready(() => cache.Get("item" + i, 1, engine, _ => new[] { 1f }));
        Test.True(cache.Count == 128, "completed vector retention remains bounded");
        int failures = 0;
        Ready(() => cache.Get("unavailable", 1, engine, _ => { failures++; return null!; }));
        Test.True(cache.Get("unavailable", 1, engine, _ => throw new Exception()) == null && failures == 1,
            "unavailable vectors retain lexical fallback without restarting every preparation poll");
        using var late = new ManualResetEventSlim(); using var entered = new ManualResetEventSlim();
        try
        {
            Pending(() => cache.Get("late", 1, engine, _ => { entered.Set(); late.Wait(); return new[] { 7f }; }));
            Test.True(entered.Wait(5000), "old-generation inference entered controlled worker");
            Test.True(Ready(() => cache.Get("late", 2, engine, _ => new[] { 9f }))[0] == 9f && cache.Count == 1,
                "generation retirement never waits for or publishes an old inference");
        }
        finally { late.Set(); }
        object replacementEngine = new();
        Test.True(Ready(() => cache.Get("late", 2, replacementEngine, _ => new[] { 11f }))[0] == 11f,
            "engine replacement invalidates the vector cache");

        using var timeoutGate = new ManualResetEventSlim();
        try
        {
            Pending(() => cache.Get("timeout", 3, engine, _ => { timeoutGate.Wait(); return new[] { 1f }; }));
            var entries = (IDictionary)typeof(WorldDiplomacyAsyncEmbeddingCache).GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(cache)!;
            object entry = entries["timeout"]!;
            entry.GetType().GetField("Started", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(entry, Stopwatch.GetTimestamp() - 181L * Stopwatch.Frequency);
            string reason = "";
            try { cache.Get("timeout", 3, engine, _ => throw new Exception()); }
            catch (WorldDiplomacyPreparationException ex) { reason = ex.Message; }
            Test.True(reason == "diplomacy_embedding_timeout" && cache.Count == 0, "hung inference produces a bounded explicit failure without waiting");
        }
        finally { timeoutGate.Set(); }
        MemoryParity(); RequestBoundary();
        Console.WriteLine("PASS async diplomatic embedding: bounded workers, retirement, timeout, complete relevance and send deferral");
    }
    private static WorldDiplomacyMemorySnapshot Snapshot()
    {
        var storage = new WorldDiplomacyStorage(); var known = new HashSet<string>();
        for (int i = 0; i < 31; i++)
        {
            storage.Documents.Add(new() { DocumentId = "d" + i, RoundId = "old", Title = "Treaty " + i,
                Body = i == 30 ? "HIDDEN" : "Original terms " + i, IsReadyForPublication = true, Day = i });
            if (i < 30) known.Add("d" + i);
        }
        return new(storage, "a", known);
    }
    private static void MemoryParity()
    {
        var syncInputs = new List<string>();
        string expected = WorldDiplomacyMemoryApplication.Build(Snapshot(), "Treaty", x => x.ToString(), 7000,
            text => { syncInputs.Add(text); return new[] { 1f, 0f }; });
        using var cache = new WorldDiplomacyAsyncEmbeddingCache(); object engine = new();
        var asyncInputs = new ConcurrentBag<string>(); var snapshot = Snapshot();
        string actual = Ready(() => WorldDiplomacyMemoryApplication.Build(snapshot, "Treaty", x => x.ToString(), 7000,
            text => cache.Get(text, 1, engine, input => { asyncInputs.Add(input); return new[] { 1f, 0f }; })));
        Test.True(actual == expected && actual.Length > 0, "deferred inference preserves complete synchronous ranking and original records");
        Test.True(syncInputs.Count == 25 && asyncInputs.Count == 25 && !asyncInputs.Any(x => x.Contains("HIDDEN")),
            "retries warm exactly the same 24 visible rows plus query, never hidden records or the entire archive");
        Test.True(asyncInputs.OrderBy(x => x).SequenceEqual(syncInputs.OrderBy(x => x)), "pending cursor rollback preserves the original inference batch");
    }
    private static void RequestBoundary()
    {
        var host = new SemanticHost(); var owner = new WorldDiplomacyOrchestration(host, new WorldDiplomacyRuntimeState()); host.Owner = owner;
        var storage = owner.CurrentStorage;
        storage.CompletedRounds.Add(new() { RoundId = "topic", RoundTopic = "Treaty", State = "closed" });
        var job = new WorldDiplomacyJob { JobId = "deferred", Kind = "generate", RoundId = "topic", AuthorKingdomId = "a", TargetKingdomId = "b", SystemPrompt = "rules", UserPrompt = "request" };
        WorldDiplomacyHistoryCaptureApplication.Capture(storage, job, false, long.MaxValue, new Port(), owner);
        Test.True(host.Calls == 0 && job.DeclarationHistoryBlock.Length > 0,
            "queued generation captures its history stamp without synchronous inference");
        Pending(() => WorldDiplomacyHistoryCaptureApplication.Capture(storage, job, true, long.MaxValue, new Port(), owner));
        Test.True(host.Calls == 1, "actual send still prepares semantic history");
        owner.PrepareSharedRequest(job);
        Test.True(job.RequestPreparationPending && !job.IsRunning && job.Kind == "generate" && job.SuspendedReason == "",
            "private material preparation yields without publishing an old request or holding the job");
        storage.Jobs.Add(job); int spent = 0, messagesBuilt = 0;
        var chosen = WorldDiplomacyLlmDispatchApplication.SelectAndPrepareLlmJob(storage, 960, "", _ => "", _ => true, _ => true, _ => true, _ => "", (_, _) => {}, () => "",
            consume => { if (consume) spent++; return true; }, _ => throw new WorldDiplomacyPreparationPendingException(),
            _ => { messagesBuilt++; return new Newtonsoft.Json.Linq.JArray(); }, out var messages, 32000, 4800, s => s.Length, _ => "", () => {},
            (_, reason) => throw new Exception(reason), _ => {}, _ => {}, eligible: _ => true, prepareShared: owner.PrepareSharedRequest);
        Test.True(chosen == null && messages == null && spent == 0 && messagesBuilt == 0,
            "semantic preparation cannot spend request budget or send stale messages");
        host.Pending = false; owner.PrepareSharedRequest(job);
        Test.True(!job.RequestPreparationPending && job.Kind == "privacy_material" && !string.IsNullOrEmpty(job.PrivacySourceMaterial),
            "completed semantic preparation enters the original privacy pipeline");
    }
}
