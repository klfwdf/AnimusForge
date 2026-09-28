using AnimusForge;
using AnimusForge.Refactor.Domain;
internal static class JobPreparationReplay
{
    private sealed class Port : IWorldDiplomacyJobPreparationPort
    {
        public WorldDiplomacyStorage Storage { get; } = new();
        public int GenerationMaxTokens => 100;
        public int AnalysisMaxTokens => 50;
        internal bool ProfileAvailable = true;
        internal int Profiles, Logs;
        internal readonly List<string> Events = new();
        internal WorldDiplomacyRound Round = new() { RoundId = "r", ResultSettlementPending = true };
        public (int minimum, int maximum) CharacterRange() => (10, 100);
        public bool KingdomExists(string id) => id is "a" or "b";
        public List<string> SettlementTargets(WorldDiplomacyRound round, string author) { Events.Add("targets"); return new() { "b" }; }
        public WorldDiplomacyRound ResolveRound(string id) => Round;
        public string CommonContract(WorldDiplomacyRound round) => "contract";
        public WorldDiplomacyDocument ResolveDocument(string id) => null;
        public string RelayPrompt(WorldDiplomacyRound round, WorldDiplomacyJob job, WorldDiplomacyDocument source) { Events.Add("relay"); return "relay text"; }
        public string GenerationPrompt(WorldDiplomacyJob job, WorldDiplomacyExchange exchange, WorldDiplomacyDocument source, List<string> candidates) => "generate text";
        public string LegalSignature(WorldDiplomacyJob job) { Events.Add("signature"); return "current"; }
        public void CaptureHistory(WorldDiplomacyJob job) { Events.Add("history"); Test.True(job.PresentedLegalActionSignature == "current", "capture occurs after legal signature stamping"); }
        public string AnalysisPrompt(WorldDiplomacyDocument document) => "analysis";
        public string RoundPlanSystemPrompt(WorldDiplomacyRound round) => "plan system";
        public string RoundPlanPrompt(WorldDiplomacyDocument document, List<string> candidates) => "plan";
        public bool TryBuildProfile(string authorId, string marker, out string prompt) { Profiles++; prompt = marker + "\nprofile"; return ProfileAvailable; }
        public void LogProfile(WorldDiplomacyJob job, string prompt) { Logs++; }
    }
    internal static void Run()
    {
        var port = new Port();
        var job = new WorldDiplomacyJob { Kind = "generate", AuthorKingdomId = " a ", UserPrompt = "original",
            LlmMessages = new() { new() { Role = "user", Content = "first" }, new() { Role = "assistant", Content = "answer" }, new() { Role = "user", Content = "latest" } } };
        Test.True(WorldDiplomacyJobPreparationApplication.EnsureGenerationJobHasKingdomStrategicProfile(port, job)
            && job.StrategicProfileKingdomId == "a" && job.LlmMessages[2].Content.Contains("profile")
            && job.LlmMessages[0].Content == "first" && job.UserPrompt == job.LlmMessages[2].Content && port.Logs == 1,
            "profile injection normalizes author identity and updates only the latest user message");
        string content = job.UserPrompt;
        Test.True(WorldDiplomacyJobPreparationApplication.EnsureGenerationJobHasKingdomStrategicProfile(port, job)
            && job.UserPrompt == content && port.Logs == 1, "repeated profile admission does not append duplicate context");
        port.ProfileAvailable = false;
        Test.True(!WorldDiplomacyJobPreparationApplication.EnsureGenerationJobHasKingdomStrategicProfile(port, job) && job.UserPrompt == content,
            "unavailable live profile fails without mutating persisted request");
        job = new WorldDiplomacyJob { Kind = "generate", AuthorKingdomId = "a", TargetKingdomId = "b", IsRelayTurn = true, RoundId = "r",
            ResultSettlementSlotId = "slot", IsRunning = true, SemanticRepairAttempts = 2, HistoryPrefixHash = "old", LlmMessages = new() { new() { Role = "user", Content = "stale" } } };
        Test.True(WorldDiplomacyJobPreparationApplication.RefreshDiplomaticActionPresentationAndPrompt(port, job)
            && !job.IsRunning && job.SemanticRepairAttempts == 0 && job.HistoryPrefixHash == "" && job.LlmMessages.Count == 0
            && job.MaxTokens == 100 && job.CandidateKingdomIds.SequenceEqual(new[] { "b" })
            && string.Join(",", port.Events) == "targets,relay,signature,history", "stale-action refresh clears the old request before rebuilding candidates, prompt, signature and history");
        port.Storage.DiplomaticThreats.Add(new WorldDiplomacyThreat { Status = "open", Stage = "warning", StageDocumentId = "warning",
            IssuerKingdomId = "b", TargetKingdomId = "a", TargetDecision = "pending" });
        job.AuthorKingdomId = "missing"; job.IsRunning = true; job.SemanticRepairAttempts = 1;
        Test.True(!WorldDiplomacyJobPreparationApplication.RefreshDiplomaticThreatPresentationAndPrompt(port, job)
            && !job.IsRunning && job.SemanticRepairAttempts == 0 && job.PresentedThreatDocumentIds.Count == 0,
            "failed threat refresh still retires stale request markers before the missing-author gate");
        job.Kind = "analyze"; job.IsRunning = true; port.Events.Clear();
        Test.True(!WorldDiplomacyJobPreparationApplication.RefreshDiplomaticActionPresentationAndPrompt(port, job)
            && job.IsRunning && port.Events.Count == 0, "non-generation jobs cannot be reset by generation refresh");
    }
}
