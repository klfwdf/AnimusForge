using AnimusForge;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;

internal static class LlmDispatchApplicationReplay
{
    private sealed class State
    {
        internal readonly WorldDiplomacyStorage Storage = new();
        internal readonly WorldDiplomacyRequestLeaseCoordinator Lease = new();
        internal readonly List<string> Events = new();
        internal bool Enabled = true, Fits = true, Budget = true, Claim = true;
        internal long Generation = 7;
        internal string Affinity = "", ConfigError;
        internal WorldDiplomacyRequestSnapshot Request;
    }
    private readonly struct Source : IWorldDiplomacyLlmDispatchSource
    {
        private readonly State s;
        internal Source(State state) => s = state;
        public bool IsEnabled => s.Enabled;
        public bool IsRequestRunning => s.Lease.IsRunning;
        public WorldDiplomacyStorage Storage => s.Storage;
        public int CurrentHour => 100;
        public string LastCacheAffinityKey => s.Affinity;
        public void SetLastCacheAffinityKey(string value) { s.Affinity = value; s.Events.Add("affinity"); }
        public bool HasStaleThreatPresentation(WorldDiplomacyJob job) => false;
        public bool RefreshThreatPresentation(WorldDiplomacyJob job) => true;
        public bool HasStaleActionPresentation(WorldDiplomacyJob job) => false;
        public bool RefreshActionPresentation(WorldDiplomacyJob job) => true;
        public bool RebuildPendingJob(WorldDiplomacyJob job) => true;
        public string GetAuthorBlockReason(WorldDiplomacyJob job) => null;
        public void AbandonGeneration(WorldDiplomacyJob job, string reason) => s.Events.Add("abandon");
        public bool EnsureStrategicProfile(WorldDiplomacyJob job) => true;
        public string GetLlmConfigError() => s.ConfigError;
        public bool TryConsumeRequestBudget(bool consume) { s.Events.Add(consume ? "consume" : "budget?"); return s.Budget; }
        public void CaptureCanonicalHistory(WorldDiplomacyJob job) => s.Events.Add("history");
        public JArray BuildMessageArray(WorldDiplomacyJob job) { s.Events.Add("messages"); return new JArray(); }
        public long InputTokenLimit => s.Fits ? long.MaxValue : -1L;
        public int HistoryCompressionTargetTokens => 200;
        public int EstimateTokens(string text) => 0;
        public string BuildHistoryBlock(long throughSequence) => "";
        public void ScheduleTokenCompression() => s.Events.Add("compress");
        public void CommitFailedJob(WorldDiplomacyJob job, string error) => s.Events.Add("failed:" + error);
        public void RemoveJob(string jobId) => s.Storage.Jobs.RemoveAll(j => j.JobId == jobId);
        public void Log(string message) => s.Events.Add("log:" + message);
        public bool TryClaim(string id, long generation, int maxTokens, int timeout, out WorldDiplomacyRequestSnapshot request)
        { s.Events.Add("claim"); request = null; return s.Claim && s.Lease.TryClaim(id, generation, maxTokens, timeout, out request); }
        public void MarkRunning(WorldDiplomacyJob job, string key) { job.IsRunning = true; job.CacheAffinityKey = key; s.Events.Add("running"); }
        public void LogPromptCacheShape(WorldDiplomacyJob job) => s.Events.Add("shape");
        public void StartRequest(WorldDiplomacyRequestSnapshot request, JArray messages) { s.Request = request; s.Events.Add("start"); }
        public long RuntimeGeneration => s.Generation;
        public int DefaultApiTimeoutMilliseconds => 90000;
        public int CompressionTimeoutMilliseconds => 200000;
    }
    private static State New(string kind = "analyze")
    {
        var state = new State();
        state.Storage.Jobs.Add(new WorldDiplomacyJob { JobId = "j", Kind = kind, SystemPrompt = "sys", UserPrompt = "u", MaxTokens = 1000,
            LlmMessages = new List<WorldDiplomacyLlmMessage>() });
        if (kind == "compress")
        {
            var job = state.Storage.Jobs[0];
            job.CacheAffinityKey = WorldDiplomacyPromptContractRules.CanonicalHistoryCacheAffinityKey;
            job.SystemPrompt = string.Join("\n", WorldDiplomacyPromptContractRules.DiplomaticDeclarationWritingContractMarker,
                WorldDiplomacyPromptContractRules.DiplomacyModeDispatchContractMarker,
                WorldDiplomacyPromptContractRules.DiplomaticDeclarationModeContractMarker,
                WorldDiplomacyPromptContractRules.CanonicalHistoryCompressionModeContractMarker,
                WorldDiplomacyPromptContractRules.CanonicalHistoryContractMarker);
            job.UserPrompt = "【MODE=COMPACT】";
        }
        return state;
    }
    private static void Run(State state) { var source = new Source(state); WorldDiplomacyLlmDispatchApplication.Run(ref source); }
    internal static void Run()
    {
        foreach (string kind in new[] { "analyze", "compress" })
        {
            var s = New(kind); Run(s);
            Test.True(s.Events.SequenceEqual(new[] { "budget?", "messages", "consume", "claim", "running", "affinity", "shape", "start" })
                && s.Request.JobId == "j" && s.Request.RuntimeGeneration == 7
                && s.Request.MaxTokens == 1000 && s.Request.TimeoutMilliseconds == (kind == "compress" ? 200000 : 90000),
                "launch keeps budget/claim/cache/start order and timeout selection: " + string.Join(",", s.Events));
            int count = s.Events.Count; Run(s);
            Test.True(s.Events.Count == count, "single active lease prevents repeated launch");
        }
        var empty = new State(); Run(empty); Test.True(empty.Events.Count == 0, "empty queue has no preflight effects");
        var disabled = New(); disabled.Enabled = false; Run(disabled); Test.True(disabled.Events.Count == 0, "disabled launch has no effects");
        var cooldown = New(); cooldown.Storage.ServiceCooldownUntilHour = 101; Run(cooldown); Test.True(cooldown.Events.Count == 0, "service cooldown blocks preflight");
        foreach (bool budget in new[] { false, true })
        {
            var s = New(); s.Budget = budget; s.Fits = false; Run(s);
            Test.True(!s.Lease.IsRunning && !s.Storage.Jobs[0].IsRunning && s.Request == null && !s.Events.Contains("consume"),
                "budget/input rejection cannot claim or start a request");
        }
        var config = New(); config.ConfigError = "missing"; Run(config);
        Test.True(config.Events.SequenceEqual(new[] { "failed:api not configured: missing" }), "configuration failure precedes request budget");
        foreach (long generation in new[] { 7L, 0L })
        {
            var s = New(); s.Generation = generation; s.Claim = generation == 0L; Run(s);
            Test.True(!s.Storage.Jobs[0].IsRunning && s.Events.Contains("consume") && s.Events.Any(e => e.StartsWith("log:")) && s.Request == null,
                "claim rejection clears selection flag without refunding already consumed budget");
        }
    }
}
