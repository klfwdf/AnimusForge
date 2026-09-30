using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Contract adapter for the existing NPC policy LLM owner. Policy-specific
/// profile resolution and compatibility probing remain in PolicyLlmClient;
/// credentials never cross into the shared request contract.
/// </summary>
public sealed class LegacyPolicyLlmGateway : ILlmGateway
{
    private readonly bool _eventAndRebellionRoute;
    private readonly PolicyApiExecutionProfile _profileOverride;

    public LegacyPolicyLlmGateway(bool eventAndRebellionRoute = false)
        : this(eventAndRebellionRoute, null)
    {
    }

    internal LegacyPolicyLlmGateway(bool eventAndRebellionRoute, PolicyApiExecutionProfile profileOverride)
    {
        _eventAndRebellionRoute = eventAndRebellionRoute;
        _profileOverride = profileOverride?.Clone();
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
        JArray messages = JArray.FromObject(ToJsonMessages(request.Prompt));
        try
        {
            NpcPolicyApiCallResult result;
            string source = string.IsNullOrWhiteSpace(request.Provider.ProviderId)
                ? "refactor_policy"
                : request.Provider.ProviderId;
            if (_eventAndRebellionRoute)
            {
                string systemPrompt = string.Join("\n\n", request.Prompt.Messages
                    .Where(message => string.Equals(message?.Role, "system", StringComparison.OrdinalIgnoreCase))
                    .Select(message => message.Content ?? string.Empty));
                if (_profileOverride == null)
                {
					result = await PolicyLlmClient.CallEventAndRebellionApiWithRetriesAsync(
						systemPrompt,
						Math.Max(16, request.Prompt.MaxTokens),
						request.Provider.TimeoutMilliseconds > 0 ? request.Provider.TimeoutMilliseconds : 300000,
						source,
						request.Trace.RuntimeGeneration,
						cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    PolicyApiExecutionProfile eventProfile = _profileOverride.Clone();
                    eventProfile.MaxTokens = Math.Max(16, Math.Min(eventProfile.MaxTokens, request.Prompt.MaxTokens));
                    result = await PolicyLlmClient.CallPolicyApiWithRetriesAsync(
                        JArray.FromObject(ToJsonMessages(BuildPromptPackage(systemPrompt, eventProfile.MaxTokens, eventProfile.ModelName))),
                        eventProfile,
                        request.Provider.TimeoutMilliseconds > 0 ? request.Provider.TimeoutMilliseconds : 300000,
                        source,
                        request.Trace.RuntimeGeneration,
                        cancellationToken: cancellationToken,
                        enablePolicyCompatibility: false).ConfigureAwait(false);
                }
            }
            else
            {
                PolicyApiExecutionProfile profile = _profileOverride?.Clone();
                if (profile == null && !PolicyLlmClient.TryResolveNpcPolicyProfile(out profile, out _))
                {
                    return new LlmGenerateResult(LlmResultStatus.NonRetryableFailure, string.Empty, 0, 0, "policy_configuration_incomplete");
                }
                profile.MaxTokens = Math.Max(1, Math.Min(profile.MaxTokens, request.Prompt.MaxTokens));
                result = await PolicyLlmClient.CallPolicyApiWithRetriesAsync(
                    messages,
                    profile,
                    request.Provider.TimeoutMilliseconds > 0 ? request.Provider.TimeoutMilliseconds : 300000,
                    source,
                    request.Trace.RuntimeGeneration,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (result != null && result.Success && !string.IsNullOrWhiteSpace(result.Content))
            {
                return new LlmGenerateResult(LlmResultStatus.Succeeded, result.Content, result.PromptTokens ?? 0, result.CompletionTokens ?? 0, string.Empty, ToMetadata(result));
            }
            return new LlmGenerateResult(
                result?.IsTimeout == true || result?.IsRateLimit == true ? LlmResultStatus.RetryableFailure : LlmResultStatus.NonRetryableFailure,
                string.Empty,
                result?.PromptTokens ?? 0,
                result?.CompletionTokens ?? 0,
                "policy_domain_failure",
                ToMetadata(result));
        }
        catch (OperationCanceledException)
        {
            return new LlmGenerateResult(LlmResultStatus.Cancelled, string.Empty, 0, 0, "cancelled");
        }
        catch (Exception exception)
        {
            return new LlmGenerateResult(LlmResultStatus.NonRetryableFailure, string.Empty, 0, 0, "policy_gateway_" + exception.GetType().Name);
        }
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

    private static LlmGenerateMetadata ToMetadata(NpcPolicyApiCallResult result)
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
            attemptsUsed: result.AttemptsUsed,
            retryAfterSeconds: result.RetryAfterSeconds,
            promptCacheHitTokens: result.PromptCacheHitTokens,
            promptCacheMissTokens: result.PromptCacheMissTokens,
            promptCacheCreationTokens: null,
            promptUncachedTokens: null);
    }

    internal static LlmGenerateRequest BuildRequest(
        PromptPackage prompt,
        PolicyApiExecutionProfile profile,
        string source,
        long runtimeGeneration,
        int timeoutMilliseconds,
        InteractionStage stage)
    {
        string safeSource = string.IsNullOrWhiteSpace(source) ? "policy" : source.Trim();
#if BANNERLORD_1_4_OR_GREATER
        string apiLine = "1.4";
#else
        string apiLine = "1.3";
#endif
        string model = string.IsNullOrWhiteSpace(profile?.ModelName) ? safeSource : profile.ModelName;
        string endpoint = string.IsNullOrWhiteSpace(profile?.EffectiveApiUrl) ? "legacy-policy" : profile.EffectiveApiUrl;
        TraceContext trace = new TraceContext(
            "af-policy-" + safeSource + "-" + runtimeGeneration.ToString(System.Globalization.CultureInfo.InvariantCulture),
            runtimeGeneration,
            runtimeGeneration,
            "policy",
            apiLine);
        LlmProviderSnapshot provider = new LlmProviderSnapshot(
            safeSource,
            endpoint,
            model,
            timeoutMilliseconds,
            Math.Max(1, prompt?.MaxTokens ?? profile?.MaxTokens ?? 1));
        return new LlmGenerateRequest(trace, provider, prompt ?? new PromptPackage(Array.Empty<PromptMessage>(), 1, model), stage);
    }

    internal static PromptPackage BuildPromptPackage(string systemPrompt, int maxTokens, string model = "policy")
    {
        return new PromptPackage(
            new[] { new PromptMessage("system", systemPrompt ?? string.Empty) },
            Math.Max(1, maxTokens),
            string.IsNullOrWhiteSpace(model) ? "policy" : model);
    }

    internal static PromptPackage BuildPromptPackage(JArray messages, int maxTokens, string model = "policy")
    {
        List<PromptMessage> copied = new List<PromptMessage>();
        foreach (JToken token in messages?.Children() ?? Enumerable.Empty<JToken>())
        {
            JObject item = token as JObject;
            copied.Add(new PromptMessage(
                item?["role"]?.ToString() ?? "user",
                item?["content"]?.ToString() ?? string.Empty));
        }
        return new PromptPackage(copied, Math.Max(1, maxTokens), string.IsNullOrWhiteSpace(model) ? "policy" : model);
    }

    internal static NpcPolicyApiCallResult ToLegacyResult(LlmGenerateResult result)
    {
        LlmGenerateMetadata metadata = result?.Metadata ?? LlmGenerateMetadata.Empty;
        return new NpcPolicyApiCallResult
        {
            Success = result?.Status == LlmResultStatus.Succeeded,
            Content = result?.RawText ?? string.Empty,
            ErrorMessage = result?.ErrorCode ?? string.Empty,
            PromptTokens = result?.PromptTokens,
            CompletionTokens = result?.CompletionTokens,
            TotalTokens = (result?.PromptTokens ?? 0) + (result?.CompletionTokens ?? 0),
            PromptCacheHitTokens = metadata.PromptCacheHitTokens,
            PromptCacheMissTokens = metadata.PromptCacheMissTokens,
            StatusCode = metadata.StatusCode,
            IsRateLimit = metadata.IsRateLimit,
            IsRequestsPerMinuteLimit = metadata.IsRequestsPerMinuteLimit,
            IsQuotaLimit = metadata.IsQuotaLimit,
            IsAuthFailure = metadata.IsAuthFailure,
            IsTimeout = metadata.IsTimeout,
            RetryAfterSeconds = metadata.RetryAfterSeconds,
            AttemptsUsed = Math.Max(1, metadata.AttemptsUsed),
            ResolvedRoute = metadata.ResolvedRoute ?? string.Empty
        };
    }
}
