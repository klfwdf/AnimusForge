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
        public string LastCacheAffinityKey => _owner._runtime.LastLlmCacheAffinityKey;
        public void SetLastCacheAffinityKey(string value) => _owner._runtime.LastLlmCacheAffinityKey = value;
        public bool HasStaleThreatPresentation(WorldDiplomacyJob job) => _owner.HasStaleDiplomaticThreatPresentation(job);
        public bool HasStaleActionPresentation(WorldDiplomacyJob job) => WorldDiplomacyRoundLifecycleRules.HasStaleDiplomaticActionPresentation(job, _owner._orchestration.BuildGenerationLegalActionSignature);
        public string GetAuthorBlockReason(WorldDiplomacyJob job)
        { string reason; return CanAiAuthorDiplomaticDocument(ResolveKingdom(job.AuthorKingdomId), out reason) ? null : reason; }
        public string GetLlmConfigError() { string error; return WorldDiplomacyLlmClient.IsConfigured(out error) ? null : error; }
        public bool TryConsumeRequestBudget(bool consume) => _owner.TryConsumeDiplomacyLlmRequestBudget(consume);
        public JArray BuildMessageArray(WorldDiplomacyJob job) => WorldDiplomacyPromptContractRules.BuildLlmMessageArray(job, _owner._orchestration.BuildCanonicalHistoryBlock);
        public long InputTokenLimit => GetHistoryCompressionTriggerTokens();
        public int HistoryCompressionTargetTokens => GetHistoryCompressionTargetTokens();
        public int EstimateTokens(string text) => Logger.EstimateTokens(text);
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
