using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Contract adapter for the existing WorldDiplomacy client. The domain keeps
/// ownership of route selection, credentials, retry/backoff, thinking fallback,
/// token accounting and stale-generation checks; this adapter only supplies a
/// frozen role/content package and maps the domain result to the shared result.
/// </summary>
public sealed class LegacyWorldDiplomacyLlmGateway : ILlmGateway
{
    private readonly Func<bool> _admitRequest;
    private readonly Func<CancellationToken, Task<WorldDiplomacyApiCallResult>> _preparedCall;
    public LegacyWorldDiplomacyLlmGateway() : this(null) { }
    public LegacyWorldDiplomacyLlmGateway(Func<bool> admitRequest) { _admitRequest = admitRequest; }
    // Constructed on the game thread; workers and retries never read MCM owners.
    internal LegacyWorldDiplomacyLlmGateway(LlmGenerateRequest request, Func<bool> admitRequest)
        : this(admitRequest)
    {
        string error;
        try
        {
            if (WorldDiplomacyLlmClient.TryPrepareCallWithRetries(JArray.FromObject(ToJsonMessages(request.Prompt)),
                Math.Max(16, request.Prompt.MaxTokens),
                request.Provider.TimeoutMilliseconds > 0 ? request.Provider.TimeoutMilliseconds : 300000,
                "refactor_world_diplomacy", request.Trace.RuntimeGeneration, out var call, out error, admitRequest))
            { _preparedCall = call; return; }
        }
        catch (Exception ex) { error = "world_diplomacy_preparation_" + ex.GetType().Name; }
        string preparationError = error;
        _preparedCall = token => Task.FromResult(new WorldDiplomacyApiCallResult { ErrorMessage = preparationError });
    }
    public async Task<LlmGenerateResult> GenerateAsync(
        LlmGenerateRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            WorldDiplomacyApiCallResult result = _preparedCall != null
                ? await _preparedCall(cancellationToken).ConfigureAwait(false)
                : await WorldDiplomacyLlmClient.CallMessagesWithRetriesAsync(
                JArray.FromObject(ToJsonMessages(request.Prompt)),
                Math.Max(16, request.Prompt.MaxTokens),
                request.Provider.TimeoutMilliseconds > 0 ? request.Provider.TimeoutMilliseconds : 300000,
                "refactor_world_diplomacy",
                request.Trace.RuntimeGeneration,
                cancellationToken: cancellationToken, admitRequest: _admitRequest).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            LlmGenerateMetadata metadata = ToMetadata(result);
            if (result != null && result.Success && !string.IsNullOrWhiteSpace(result.Content))
            {
                return new LlmGenerateResult(LlmResultStatus.Succeeded, result.Content, result.PromptTokens ?? 0, result.CompletionTokens ?? 0, string.Empty, metadata);
            }
            return new LlmGenerateResult(
                result?.IsTimeout == true || result?.IsRateLimit == true ? LlmResultStatus.RetryableFailure : LlmResultStatus.NonRetryableFailure,
                string.Empty,
                result?.PromptTokens ?? 0,
                result?.CompletionTokens ?? 0,
                result?.BudgetDeferred == true ? "world_diplomacy_request_budget_deferred"
                    : result?.IsOutputTruncated == true ? "world_diplomacy_output_truncated" : "world_diplomacy_domain_failure",
                metadata);
        }
        catch (OperationCanceledException)
        {
            return new LlmGenerateResult(LlmResultStatus.Cancelled, string.Empty, 0, 0, "cancelled");
        }
        catch (Exception exception)
        {
            return new LlmGenerateResult(LlmResultStatus.NonRetryableFailure, string.Empty, 0, 0, "world_diplomacy_gateway_" + exception.GetType().Name);
        }
    }

    public static PromptPackage BuildPromptPackage(JArray messages, int maxTokens, string model = "world-diplomacy")
    {
        List<PromptMessage> copied = new List<PromptMessage>();
        foreach (JToken token in messages?.Children() ?? Enumerable.Empty<JToken>())
        {
            JObject item = token as JObject;
            string role = item?["role"]?.ToString() ?? "user";
            string content = item?["content"]?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(content))
            {
                copied.Add(new PromptMessage(role, content));
            }
        }
        return new PromptPackage(copied, maxTokens, model);
    }

    private static LlmGenerateMetadata ToMetadata(WorldDiplomacyApiCallResult result)
    {
        return result == null ? LlmGenerateMetadata.Empty : new LlmGenerateMetadata(
            statusCode: result.StatusCode,
            finishReason: result.FinishReason,
            resolvedRoute: result.ResolvedRoute,
            isOutputTruncated: result.IsOutputTruncated,
            isRateLimit: result.IsRateLimit,
            isRequestsPerMinuteLimit: result.IsRequestsPerMinuteLimit,
            isQuotaLimit: result.IsQuotaLimit,
            isAuthFailure: result.IsAuthFailure,
            isTimeout: result.IsTimeout,
            retryAfterSeconds: result.RetryAfterSeconds,
            promptCacheHitTokens: result.PromptCacheHitTokens,
            promptCacheMissTokens: result.PromptCacheMissTokens,
            promptCacheCreationTokens: result.PromptCacheCreationTokens,
            promptUncachedTokens: result.PromptUncachedTokens);
    }

    private static IReadOnlyList<object> ToJsonMessages(PromptPackage prompt)
    {
        return (prompt?.Messages ?? Array.Empty<PromptMessage>())
            .Select(message => (object)new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = message?.Role ?? "user",
                ["content"] = message?.Content ?? string.Empty
            })
            .ToList();
    }
}
