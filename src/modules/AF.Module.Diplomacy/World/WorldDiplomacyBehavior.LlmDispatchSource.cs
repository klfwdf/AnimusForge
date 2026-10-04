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
        public string GetAuthorBlockReason(WorldDiplomacyJob job)
        { string reason; return CanAiAuthorDiplomaticDocument(ResolveKingdom(job.AuthorKingdomId), out reason) ? null : reason; }
        public string GetLlmConfigError() { string error; return WorldDiplomacyLlmClient.IsConfigured(out error) ? null : error; }
        public bool TryConsumeRequestBudget(bool consume) => _owner.TryConsumeDiplomacyLlmRequestBudget(consume);
        public JArray BuildMessageArray(WorldDiplomacyJob job) => WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(job, _owner._orchestration.BuildCanonicalHistoryBlock);
        public long InputTokenLimit => GetHistoryCompressionTriggerTokens();
        public int HistoryCompressionTargetTokens => GetHistoryCompressionTargetTokens();
        public int EstimateTokens(string text) => Logger.EstimateTokens(text);
        public void RemoveJob(string jobId) => _owner._orchestration.RemoveJob(jobId);
        public void Log(string message) => WorldDiplomacyBehavior.Log(message);
        public bool TryClaim(string jobId, long generation, int maxTokens, int timeoutMilliseconds, out WorldDiplomacyRequestSnapshot request) => _owner._llmRequestLease.TryClaim(jobId, generation, maxTokens, timeoutMilliseconds, out request);
        public void MarkRunning(WorldDiplomacyJob job, string cacheAffinityKey) { job.IsRunning = true; job.CacheAffinityKey = cacheAffinityKey; }
        public void LogPromptCacheShape(WorldDiplomacyJob job) => _owner.LogPromptCacheShape(job);
        public void StartRequest(WorldDiplomacyRequestSnapshot request, JArray messages)
        {
            LlmGenerateRequest detachedRequest = WorldDiplomacyLlmApplication.PrepareRequest(request, messages);
            var budget = _owner._storage.RequestBudget;
            bool player = request.IsPlayerWork;
            CancellationToken cancellation = request.Cancellation;
            ILlmGateway gateway = new LegacyWorldDiplomacyLlmGateway(() => !cancellation.IsCancellationRequested && budget.TryAdmit(player));
            var completedJobs = _owner._completedJobs;
            _ = Task.Run(async delegate
            {
                LlmJobResult result = await WorldDiplomacyLlmApplication.ExecuteAsync(
                    request.JobId, detachedRequest, gateway, cancellation).ConfigureAwait(false);
                result.RequestAttempt = request.Attempt;
                result.RoundConversationRevision = request.ConversationRevision;
                if (!cancellation.IsCancellationRequested
                    && SaveRuntimeGuard.IsCurrentGeneration(request.RuntimeGeneration))
                    completedJobs.Enqueue(result);
            });
        }
        public long RuntimeGeneration => _owner._runtimeGeneration;
        public int DefaultApiTimeoutMilliseconds => WorldDiplomacyBehavior.DefaultApiTimeoutMilliseconds;
        public int CompressionTimeoutMilliseconds => DuelSettings.LlmRequestTimeoutMilliseconds;
    }
}
