using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    internal readonly struct CompletionSource : IWorldDiplomacyCompletionSource
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal CompletionSource(WorldDiplomacyBehavior owner) => _owner = owner;
        public bool TryDequeue(out LlmJobResult result) => _owner._completedJobs.TryDequeue(out result);
        public void ReleaseLease(string jobId, long generation) => _owner._llmRequestLease.TryRelease(jobId, generation);
        public long RuntimeGeneration => _owner._runtimeGeneration;
        public bool IsSaveRuntimeStale(long generation) => SaveRuntimeGuard.IsStale(generation, "world_diplomacy_commit");
        public WorldDiplomacyStorage Storage => _owner._storage;
        public int CurrentHour => WorldDiplomacyBehavior.CurrentHour();
        public int FailedServiceCooldownHours => WorldDiplomacyBehavior.FailedServiceCooldownHours;
        public void LogUsage(WorldDiplomacyJob job, LlmJobResult result) => _owner.LogPromptCacheUsage(job, result);
        public bool HasStaleThreatPresentation(WorldDiplomacyJob job) => _owner.HasStaleDiplomaticThreatPresentation(job);
        public bool RefreshThreatPresentation(WorldDiplomacyJob job) => _owner.RefreshDiplomaticThreatPresentationAndPrompt(job);
        public bool HasStaleActionPresentation(WorldDiplomacyJob job) => WorldDiplomacyRoundLifecycleRules.HasStaleDiplomaticActionPresentation(job, _owner.BuildGenerationLegalActionSignature);
        public bool RefreshActionPresentation(WorldDiplomacyJob job) => _owner.RefreshDiplomaticActionPresentationAndPrompt(job);
        public void HandleTruncatedDraft(WorldDiplomacyJob job, string content) => _owner.RejectGeneratedDraftBeforePublication(
            job, content, ResolveKingdom(job.AuthorKingdomId), ResolveKingdom(job.TargetKingdomId), "output_truncated", null);
        public void CommitGeneratedDocument(WorldDiplomacyJob job, string content) => _owner.CommitGeneratedDocument(job, content);
        public void CommitAnalysis(WorldDiplomacyJob job, string content) => _owner.CommitAnalysis(job, content);
        public void CommitCompression(WorldDiplomacyJob job, string content) => _owner.CommitCompression(job, content);
        public void CommitRoundPlan(WorldDiplomacyJob job, string content) => _owner.CommitRoundPlan(job, content);
        public void CommitRoundCompression(WorldDiplomacyJob job, string content) => _owner.CommitRoundCompression(job, content);
        public void CommitFailedJob(WorldDiplomacyJob job, string content) => _owner.CommitFailedJob(job, content);
        public void RemoveJob(string jobId) => _owner.RemoveJob(jobId);
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
    }
}
