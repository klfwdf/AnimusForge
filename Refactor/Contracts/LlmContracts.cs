using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.Refactor.Contracts;

public enum LlmResultStatus
{
    Succeeded,
    RetryableFailure,
    NonRetryableFailure,
    Cancelled,
    EmptyResponse,
    InvalidResponse
}

public sealed class LlmProviderSnapshot
{
    public LlmProviderSnapshot(string providerId, string endpoint, string model, int timeoutMilliseconds, int maxTokens)
    {
        ProviderId = ContractGuard.Required(providerId, nameof(providerId));
        Endpoint = ContractGuard.Required(endpoint, nameof(endpoint));
        Model = ContractGuard.Required(model, nameof(model));
        TimeoutMilliseconds = timeoutMilliseconds;
        MaxTokens = maxTokens;
    }

    public string ProviderId { get; }
    public string Endpoint { get; }
    public string Model { get; }
    public int TimeoutMilliseconds { get; }
    public int MaxTokens { get; }
}

public sealed class LlmGenerateRequest
{
    public LlmGenerateRequest(TraceContext trace, LlmProviderSnapshot provider, PromptPackage prompt, InteractionStage stage = InteractionStage.MainReply)
    {
        Trace = trace ?? throw new ArgumentNullException(nameof(trace));
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        Stage = stage;
    }

    public TraceContext Trace { get; }
    public LlmProviderSnapshot Provider { get; }
    public PromptPackage Prompt { get; }
    public InteractionStage Stage { get; }
}

public sealed class LlmGenerateResult
{
    public LlmGenerateResult(
        LlmResultStatus status,
        string rawText,
        int promptTokens,
        int completionTokens,
        string errorCode,
        LlmGenerateMetadata metadata = null)
    {
        Status = status;
        RawText = rawText ?? string.Empty;
        PromptTokens = promptTokens;
        CompletionTokens = completionTokens;
        ErrorCode = errorCode ?? string.Empty;
        Metadata = metadata ?? LlmGenerateMetadata.Empty;
    }

    public LlmResultStatus Status { get; }
    public string RawText { get; }
    public int PromptTokens { get; }
    public int CompletionTokens { get; }
    public string ErrorCode { get; }
    public LlmGenerateMetadata Metadata { get; }
}

/// <summary>
/// Non-secret provider diagnostics preserved when a domain-specific legacy
/// client is adapted to the shared Gateway contract. Response bodies and keys
/// deliberately do not cross this DTO.
/// </summary>
public sealed class LlmGenerateMetadata
{
    public static readonly LlmGenerateMetadata Empty = new LlmGenerateMetadata();

    public LlmGenerateMetadata(
        int? statusCode = null,
        string finishReason = "",
        string resolvedRoute = "",
        bool isOutputTruncated = false,
        bool isRateLimit = false,
        bool isRequestsPerMinuteLimit = false,
        bool isQuotaLimit = false,
        bool isAuthFailure = false,
        bool isTimeout = false,
        int attemptsUsed = 0,
        int? retryAfterSeconds = null,
        int? promptCacheHitTokens = null,
        int? promptCacheMissTokens = null,
        int? promptCacheCreationTokens = null,
        int? promptUncachedTokens = null)
    {
        StatusCode = statusCode;
        FinishReason = finishReason ?? string.Empty;
        ResolvedRoute = resolvedRoute ?? string.Empty;
        IsOutputTruncated = isOutputTruncated;
        IsRateLimit = isRateLimit;
        IsRequestsPerMinuteLimit = isRequestsPerMinuteLimit;
        IsQuotaLimit = isQuotaLimit;
        IsAuthFailure = isAuthFailure;
        IsTimeout = isTimeout;
        AttemptsUsed = attemptsUsed;
        RetryAfterSeconds = retryAfterSeconds;
        PromptCacheHitTokens = promptCacheHitTokens;
        PromptCacheMissTokens = promptCacheMissTokens;
        PromptCacheCreationTokens = promptCacheCreationTokens;
        PromptUncachedTokens = promptUncachedTokens;
    }

    public int? StatusCode { get; }
    public string FinishReason { get; }
    public string ResolvedRoute { get; }
    public bool IsOutputTruncated { get; }
    public bool IsRateLimit { get; }
    public bool IsRequestsPerMinuteLimit { get; }
    public bool IsQuotaLimit { get; }
    public bool IsAuthFailure { get; }
    public bool IsTimeout { get; }
    public int AttemptsUsed { get; }
    public int? RetryAfterSeconds { get; }
    public int? PromptCacheHitTokens { get; }
    public int? PromptCacheMissTokens { get; }
    public int? PromptCacheCreationTokens { get; }
    public int? PromptUncachedTokens { get; }
}

public interface ILlmGateway
{
    Task<LlmGenerateResult> GenerateAsync(LlmGenerateRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Optional streaming capability for a channel gateway. Delta callbacks are
/// observational only; the returned result is the authoritative final text.
/// Implementations must not expose credentials, live game objects or mutable
/// provider configuration through this contract.
/// </summary>
public interface ILlmStreamingGateway
{
    Task<LlmGenerateResult> GenerateStreamAsync(
        LlmGenerateRequest request,
        Action<string> onDelta,
        CancellationToken cancellationToken);
}

public interface IVisibleReplyNormalizer
{
    string Normalize(string rawText, IEnumerable<string> internalTagFamilies);
}

public interface IPostprocessPromptComposer
{
    PromptPackage Compose(
        InteractionEnvelope envelope,
        RuleSelection selection,
        string visibleReply,
        string rawReply,
        PostprocessContext context);
}
