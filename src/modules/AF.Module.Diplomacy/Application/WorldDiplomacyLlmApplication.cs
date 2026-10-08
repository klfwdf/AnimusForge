using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Owns the diplomacy request/receipt protocol. AF's gateway owns transport;
// the Campaign adapter owns worker scheduling and main-thread result acceptance.
internal static class WorldDiplomacyLlmApplication
{
    internal static LlmGenerateRequest PrepareRequest(WorldDiplomacyRequestSnapshot snapshot, JArray messages)
    {
        var copied = new List<PromptMessage>();
        if (messages != null)
            foreach (JToken token in messages)
            {
                JObject item = token as JObject;
                string role = item?["role"]?.ToString() ?? "user";
                string content = item?["content"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(content)) copied.Add(new PromptMessage(role, content));
            }
        return new LlmGenerateRequest(
            new TraceContext("world-diplomacy-" + snapshot.JobId, snapshot.RuntimeGeneration, 0, "single-player", "shared"),
            new LlmProviderSnapshot("world-diplomacy", "legacy://world-diplomacy", "world-diplomacy", snapshot.TimeoutMilliseconds, snapshot.MaxTokens),
            new PromptPackage(copied, snapshot.MaxTokens, "world-diplomacy"),
            InteractionStage.MainReply);
    }

    internal static async Task<LlmJobResult> ExecuteAsync(
        string jobId, LlmGenerateRequest request, ILlmGateway gateway, CancellationToken cancellationToken)
    {
        LlmJobResult result = new LlmJobResult
        {
            JobId = jobId,
            RuntimeGeneration = request.Trace.RuntimeGeneration
        };
        try
        {
            LlmGenerateResult generated = await gateway.GenerateAsync(
                request, cancellationToken).ConfigureAwait(false);
            LlmGenerateMetadata metadata = generated.Metadata ?? LlmGenerateMetadata.Empty;
            result.Success = generated.Status == LlmResultStatus.Succeeded;
            result.Content = generated.RawText ?? "";
            result.Error = generated.Status == LlmResultStatus.Succeeded ? "" : (generated.ErrorCode ?? "world_diplomacy_gateway_failure");
            result.IsServiceFailure = !metadata.IsOutputTruncated && (metadata.IsTimeout || metadata.IsRateLimit || metadata.IsQuotaLimit || metadata.IsAuthFailure || generated.Status != LlmResultStatus.Succeeded);
            result.IsOutputTruncated = metadata.IsOutputTruncated;
            result.PromptTokens = generated.PromptTokens;
            result.CompletionTokens = generated.CompletionTokens;
            result.PromptCacheHitTokens = metadata.PromptCacheHitTokens;
            result.PromptCacheMissTokens = metadata.PromptCacheMissTokens;
            result.PromptCacheCreationTokens = metadata.PromptCacheCreationTokens;
            result.PromptUncachedTokens = metadata.PromptUncachedTokens;
        }
        catch (Exception ex)
        {
            result.Error = ex.ToString();
            result.IsServiceFailure = true;
        }
        return result;
    }
}
