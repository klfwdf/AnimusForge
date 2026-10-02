using AnimusForge;

// Exercises the real queue -> admission -> dispatch -> state transition owner.
// The effects and orchestration boundary are fakes; document execution and
// live-game acceptance are separate gates.
internal static class CompletionApplicationReplay
{
    private sealed class State
    {
        internal readonly Queue<LlmJobResult> Queue = new();
        internal readonly WorldDiplomacyStorage Storage = new();
        internal readonly WorldDiplomacyRequestLeaseCoordinator Lease = new();
        internal readonly List<string> Events = new();
        internal long Generation = 7;
        internal bool Stale, Threat, Action, Refresh = true, ThrowCommit, ThrowRemove, ThrowTruncated, RealRoundCompression, RealFailure;
        internal string Signature = "";
        internal int Reads;
    }

    private readonly struct Source : IWorldDiplomacyCompletionSource
    {
        private readonly State s;
        internal Source(State state) => s = state;
        public bool TryDequeue(out LlmJobResult result) => s.Queue.TryDequeue(out result);
        public void ReleaseLease(string id, long generation)
        { s.Events.Add("release"); s.Lease.TryRelease(id, generation); }
        public long RuntimeGeneration => s.Generation;
        public bool IsSaveRuntimeStale(long generation) { s.Events.Add("guard"); return s.Stale; }
        public WorldDiplomacyStorage Storage { get { s.Reads++; return s.Storage; } }
        public int CurrentHour => 100;
        public int FailedServiceCooldownHours => 12;
        public void LogUsage(WorldDiplomacyJob job, LlmJobResult result)
        { Test.True(!job.IsRunning, "running flag clears before usage and effects"); s.Events.Add("usage"); }
        public void RemoveJob(string id)
        {
            s.Events.Add("remove");
            if (s.ThrowRemove) throw new InvalidOperationException("remove");
            s.Storage.Jobs.RemoveAll(j => j.JobId == id);
        }
        public void Log(string message) => s.Events.Add("log:" + message);
    }

    private sealed class Orch : FakeOrchestration
    {
        private readonly State s;
        internal Orch(State state) => s = state;
        private void Commit(string kind, string content)
        { s.Events.Add(kind + ":" + content); if (s.ThrowCommit) throw new InvalidOperationException("effect"); }
        public override void CommitGeneratedDocument(WorldDiplomacyJob job, string content) => Commit("generate", content);
        public override void CommitAnalysis(WorldDiplomacyJob job, string content) => Commit("analyze", content);
        public override void CommitCompression(WorldDiplomacyJob job, string content) => Commit("compress", content);
        public override void CommitRoundPlan(WorldDiplomacyJob job, string content) => Commit("round_plan", content);
        public override void CommitRoundCompression(WorldDiplomacyJob job, string content)
        {
            Commit("round_compress", content);
            if (s.RealRoundCompression)
                WorldDiplomacyRoundCompressionApplication.Commit(s.Storage, job, content, 30, day => "day " + day);
        }
        public override void CommitFailedJob(WorldDiplomacyJob job, string error)
        {
            s.Events.Add("failed:" + error);
            if (s.RealFailure)
            {
                State state = s;
                WorldDiplomacyFailureApplication.Commit(job, error, state.Storage, 96, 6, () => 100,
                    (j, a, t, reason) => state.Events.Add("abandon:" + reason),
                    j => "{}", (j, raw) => state.Events.Add("analysis-fallback"),
                    j => state.Events.Add("threat-fallback"), (j, raw) => state.Events.Add("plan-fallback"),
                    j => "{}", (j, raw) => state.Events.Add("archive-fallback"),
                    id => { state.Events.Add("remove"); state.Storage.Jobs.RemoveAll(j => j.JobId == id); },
                    msg => state.Events.Add("log:" + msg));
            }
        }
        public override bool RefreshDiplomaticThreatPresentationAndPrompt(WorldDiplomacyJob job) { s.Events.Add("threat-refresh"); return s.Refresh; }
        public override bool RefreshDiplomaticActionPresentationAndPrompt(WorldDiplomacyJob job) { s.Events.Add("action-refresh"); return s.Refresh; }
        public override string BuildGenerationLegalActionSignature(WorldDiplomacyJob job) => s.Signature;
        public override void RejectGeneratedDraftBeforePublication(WorldDiplomacyJob job, string rejectedRaw,
            string authorId, string targetId, string reason, Newtonsoft.Json.Linq.JObject parsedJson)
        { s.Events.Add("truncated"); if (s.ThrowTruncated) throw new InvalidOperationException("draft"); }
    }

    private static WorldDiplomacyJob Enqueue(State s, string kind = "analyze", bool success = true,
        long generation = 7, string id = "j", bool service = false, bool truncated = false, string content = "raw")
    {
        var job = new WorldDiplomacyJob { JobId = id, Kind = kind, IsRunning = true };
        s.Storage.Jobs.Add(job);
        s.Queue.Enqueue(new LlmJobResult { JobId = id, RuntimeGeneration = generation,
            Success = success, Content = content, Error = "failure", IsServiceFailure = service, IsOutputTruncated = truncated });
        return job;
    }
    private static void Run(State s) { var source = new Source(s); var orch = new Orch(s); WorldDiplomacyCompletionApplication.Run(ref source, orch); }

    internal static void Run()
    {
        var empty = new State(); Run(empty);
        Test.True(empty.Reads == 0 && empty.Events.Count == 0, "idle tick neither scans jobs nor invokes dependencies");

        foreach (string kind in new[] { "generate", "analyze", "compress", "round_plan", "round_compress" })
        {
            var s = new State(); s.Storage.ConsecutiveServiceFailures = 1;
            var job = Enqueue(s, "  " + kind.ToUpperInvariant() + "  ");
            s.Lease.TryClaim("j", 7, 256, 100, out _); Run(s);
            var expected = new List<string> { "release", "guard", "usage" };
            expected.AddRange(new[] { kind + ":raw", "remove" });
            Test.True(s.Events.SequenceEqual(expected) && !s.Lease.IsRunning && s.Storage.Jobs.Count == 0
                && s.Storage.ConsecutiveServiceFailures == 0 && job.Kind == "  " + kind.ToUpperInvariant() + "  ",
                "all five routes preserve payload, ordering, removal, counter reset and persisted kind");
        }

        var late = new State(); var currentJob = Enqueue(late, generation: 6);
        late.Queue.Enqueue(null); late.Queue.Enqueue(new LlmJobResult { JobId = " ", RuntimeGeneration = 7 });
        late.Lease.TryClaim("j", 7, 256, 100, out _); Run(late);
        Test.True(late.Lease.IsRunning && currentJob.IsRunning && late.Reads == 0
            && late.Events.SequenceEqual(new[] { "release", "release", "release", "guard" }),
            "old generation/null/blank result cannot read current jobs or release current lease");
        var stale = new State { Stale = true }; var staleJob = Enqueue(stale);
        stale.Lease.TryClaim("j", 7, 256, 100, out _); Run(stale);
        Test.True(!stale.Lease.IsRunning && staleJob.IsRunning && stale.Reads == 0, "stale save releases matching lease before rejection");
        var zero = new State { Generation = 0 }; Enqueue(zero, generation: 0); Run(zero);
        Test.True(zero.Reads == 0, "zero runtime generation is rejected");
        var missing = new State(); Enqueue(missing); missing.Storage.Jobs.Clear(); Run(missing);
        Test.True(missing.Events.SequenceEqual(new[] { "release", "guard" }), "removed job receives no completion effects");

        var duplicate = new State(); Enqueue(duplicate); duplicate.Queue.Enqueue(duplicate.Queue.Peek()); Run(duplicate);
        Test.True(duplicate.Events.Count(e => e == "analyze:raw") == 1 && duplicate.Storage.Jobs.Count == 0,
            "duplicate completion cannot dispatch a removed job again");
        var archive = new State { RealRoundCompression = true };
        archive.Storage.RoundSummaries.Add(new WorldDiplomacyRoundSummary { RoundId = "r", Summary = "old" });
        archive.Storage.RoundSummaries.Add(new WorldDiplomacyRoundSummary { RoundId = "other", Summary = "keep" });
        var archiveJob = Enqueue(archive, "round_compress", content: "{\"summary\":\"new\",\"facts\":[{\"text\":\"confirmed\"}]}");
        archiveJob.RoundId = "r";
        archive.Queue.Enqueue(archive.Queue.Peek());
        Run(archive);
        Test.True(archive.Storage.Jobs.Count == 0
            && archive.Storage.RoundSummaries.Count == 2
            && archive.Storage.RoundSummaries.Single(x => x.RoundId == "r").Summary == "new"
            && archive.Storage.RoundSummaries.Single(x => x.RoundId == "r").Facts[0].Text == "confirmed"
            && archive.Storage.RoundSummaries.Single(x => x.RoundId == "other").Summary == "keep"
            && archive.Events.Count(x => x.StartsWith("round_compress:")) == 1,
            "admitted completion replaces only its round archive once; duplicate result does not replay it");
        var lateArchive = new State { RealRoundCompression = true };
        lateArchive.Storage.RoundSummaries.Add(new WorldDiplomacyRoundSummary { RoundId = "r", Summary = "current" });
        Enqueue(lateArchive, "round_compress", generation: 6, content: "{\"summary\":\"stale\"}").RoundId = "r";
        Run(lateArchive);
        Test.True(lateArchive.Storage.RoundSummaries[0].Summary == "current"
            && !lateArchive.Events.Any(x => x.StartsWith("round_compress:")),
            "late completion cannot overwrite the current archive");
        var failedCompression = new State { RealFailure = true };
        Enqueue(failedCompression, "compress", success: false, service: true);
        Run(failedCompression);
        Test.True(failedCompression.Storage.Jobs.Count == 0
            && failedCompression.Storage.DiplomacyCompressionPending
            && failedCompression.Storage.CompressionRetryAttempts == 1
            && failedCompression.Storage.CompressionRetryAfterHour == 106
            && failedCompression.Events.IndexOf("failed:failure") < failedCompression.Events.IndexOf("remove"),
            "admitted transport failure schedules bounded compression retry before removing the dead job");
        var lateFailure = new State { RealFailure = true };
        Enqueue(lateFailure, "compress", success: false, generation: 6);
        Run(lateFailure);
        Test.True(!lateFailure.Storage.DiplomacyCompressionPending
            && lateFailure.Storage.CompressionRetryAttempts == 0
            && lateFailure.Storage.Jobs.Count == 1,
            "late failure cannot schedule retry or remove a current job");
        foreach (bool threat in new[] { true, false })
        foreach (bool refresh in new[] { true, false })
        {
            var s = new State { Refresh = refresh, Signature = "sig" };
            s.Storage.ConsecutiveServiceFailures = 1;
            var job = Enqueue(s, "generate");
            job.AuthorKingdomId = "a";
            job.PresentedLegalActionSignature = "";
            if (threat)
            {
                s.Storage.DiplomaticThreats.Add(new WorldDiplomacyThreat
                {
                    ThreatId = "t", Status = "open", IssuerKingdomId = "a", StageDocumentId = "doc-t"
                });
            }
            Run(s);
            Test.True(s.Events.Contains(threat ? "threat-refresh" : "action-refresh")
                && (!threat || !s.Events.Contains("action-refresh")) && !s.Events.Contains("generate:raw")
                && !s.Events.Contains("remove") && s.Storage.ConsecutiveServiceFailures == 1
                && s.Events.Any(e => e.StartsWith(refresh ? "log:" : "failed:")),
                "stale refresh has threat precedence, preserves job/counter, and logs or fails");
        }
        foreach (bool throws in new[] { false, true })
        {
            var s = new State { ThrowTruncated = throws }; s.Storage.ConsecutiveServiceFailures = 1;
            Enqueue(s, "generate", success: false, service: true, truncated: true); Run(s);
            Test.True(s.Events.Contains("truncated") && s.Storage.ConsecutiveServiceFailures == 0
                && (throws ? s.Events.Contains("failed:truncated generated draft handling failed: draft") : s.Events.Contains("remove")),
                "nonempty truncated generation takes precedence over service failure and handles effect exception");
        }
        var failures = new State(); Enqueue(failures, success: false, service: true, id: "a");
        Enqueue(failures, "generate", success: false, service: true, truncated: true, content: " ", id: "b"); Run(failures);
        Test.True(failures.Storage.ServiceCooldownUntilHour == 112 && failures.Storage.ConsecutiveServiceFailures == 0
            && failures.Events.Count(e => e == "failed:failure") == 2 && !failures.Events.Contains("truncated"),
            "two service failures open cooldown; blank truncated content remains a service failure");
        var parse = new State(); parse.Storage.ConsecutiveServiceFailures = 1;
        Enqueue(parse, success: false); Run(parse);
        Test.True(parse.Storage.ConsecutiveServiceFailures == 1 && parse.Storage.ServiceCooldownUntilHour == 0,
            "nonservice failures preserve existing service counter");
        foreach (bool remove in new[] { false, true })
        {
            var s = new State { ThrowCommit = !remove, ThrowRemove = remove }; Enqueue(s); Run(s);
            Test.True(s.Events.Last() == "failed:" + (remove ? "remove" : "effect") && s.Storage.Jobs.Count == 1,
                "effect/removal exception reaches failure handling without losing the job");
        }
        var unknown = new State(); Enqueue(unknown, "unknown"); Run(unknown);
        Test.True(unknown.Events.Last() == "failed:unknown job kind" && unknown.Storage.Jobs.Count == 1,
            "unknown route fails without normal removal");
    }
}
