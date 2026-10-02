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
        public void RemoveJob(string jobId) => _owner._orchestration.RemoveJob(jobId);
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
    }
}
