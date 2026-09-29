using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Adapters;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    internal readonly struct LlmDispatchSource : IWorldDiplomacyLlmDispatchSource
    {
        private readonly WorldDiplomacyBehavior _owner;
        internal LlmDispatchSource(WorldDiplomacyBehavior owner) => _owner = owner;
        public bool IsEnabled => IsWorldDiplomacyEnabled();
        public bool IsRequestRunning => _owner._llmRequestLease.IsRunning;
        public WorldDiplomacyStorage Storage => _owner._storage;
        public int CurrentHour => WorldDiplomacyBehavior.CurrentHour();
        public string LastCacheAffinityKey => _owner._lastLlmCacheAffinityKey;
        public void SetLastCacheAffinityKey(string value) => _owner._lastLlmCacheAffinityKey = value;
        public bool HasStaleThreatPresentation(WorldDiplomacyJob job) => _owner.HasStaleDiplomaticThreatPresentation(job);
        public bool RefreshThreatPresentation(WorldDiplomacyJob job) => _owner.RefreshDiplomaticThreatPresentationAndPrompt(job);
        public bool HasStaleActionPresentation(WorldDiplomacyJob job) => WorldDiplomacyRoundLifecycleRules.HasStaleDiplomaticActionPresentation(job, _owner.BuildGenerationLegalActionSignature);
        public bool RefreshActionPresentation(WorldDiplomacyJob job) => _owner.RefreshDiplomaticActionPresentationAndPrompt(job);
        public bool RebuildPendingJob(WorldDiplomacyJob job) => _owner.TryRebuildPendingWorldDiplomacyJob(job);
        public string GetAuthorBlockReason(WorldDiplomacyJob job)
        { string reason; return CanAiAuthorDiplomaticDocument(ResolveKingdom(job.AuthorKingdomId), out reason) ? null : reason; }
        public void AbandonGeneration(WorldDiplomacyJob job, string reason) => _owner.AbandonRejectedGeneration(job, ResolveKingdom(job.AuthorKingdomId), ResolveKingdom(job.TargetKingdomId), reason);
        public bool EnsureStrategicProfile(WorldDiplomacyJob job) => _owner.EnsureGenerationJobHasKingdomStrategicProfile(job);
        public string GetLlmConfigError() { string error; return WorldDiplomacyLlmClient.IsConfigured(out error) ? null : error; }
        public bool TryConsumeRequestBudget(bool consume) => _owner.TryConsumeDiplomacyLlmRequestBudget(consume);
        public void CaptureCanonicalHistory(WorldDiplomacyJob job) => _owner.CaptureCanonicalHistoryForJob(job, syncSources: true);
        public JArray BuildMessageArray(WorldDiplomacyJob job) => WorldDiplomacyPromptContractRules.BuildLlmMessageArray(job, _owner.BuildCanonicalHistoryBlock);
        public long InputTokenLimit => GetHistoryCompressionTriggerTokens();
        public int HistoryCompressionTargetTokens => GetHistoryCompressionTargetTokens();
        public int EstimateTokens(string text) => Logger.EstimateTokens(text);
        public string BuildHistoryBlock(long throughSequence) => _owner.BuildCanonicalHistoryBlock(throughSequence);
        public void ScheduleTokenCompression() => _owner.TryScheduleTokenCompression();
        public void CommitFailedJob(WorldDiplomacyJob job, string error) => _owner.CommitFailedJob(job, error);
        public void RemoveJob(string jobId) => _owner.RemoveJob(jobId);
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
        public bool TryClaim(string jobId, long generation, int maxTokens, int timeoutMilliseconds, out WorldDiplomacyRequestSnapshot request) => _owner._llmRequestLease.TryClaim(jobId, generation, maxTokens, timeoutMilliseconds, out request);
        public void MarkRunning(WorldDiplomacyJob job, string cacheAffinityKey) { job.IsRunning = true; job.CacheAffinityKey = cacheAffinityKey; }
        public void LogPromptCacheShape(WorldDiplomacyJob job) => _owner.LogPromptCacheShape(job);
        public void StartRequest(WorldDiplomacyRequestSnapshot request, JArray messages)
        {
            LlmGenerateRequest detachedRequest = WorldDiplomacyLlmApplication.PrepareRequest(request, messages);
            ILlmGateway gateway = new LegacyWorldDiplomacyLlmGateway();
            var completedJobs = _owner._completedJobs;
            _ = Task.Run(async delegate
            {
                LlmJobResult result = await WorldDiplomacyLlmApplication.ExecuteAsync(
                    request.JobId, detachedRequest, gateway, CancellationToken.None).ConfigureAwait(false);
                completedJobs.Enqueue(result);
            });
        }
        public long RuntimeGeneration => _owner._runtimeGeneration;
        public int DefaultApiTimeoutMilliseconds => WorldDiplomacyBehavior.DefaultApiTimeoutMilliseconds;
        public int CompressionTimeoutMilliseconds => DuelSettings.LlmRequestTimeoutMilliseconds;
    }
}
