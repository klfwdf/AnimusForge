using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;

namespace AnimusForge.Refactor.Contracts;

/// <summary>
/// Transitional gateway. It provides the new immutable contract over the existing
/// ShoutNetwork implementation; provider configuration remains owned by DuelSettings
/// until the configuration migration slice is complete.
/// </summary>
public sealed class LegacyShoutNetworkGateway : ILlmGateway, ILlmStreamingGateway
{
    private readonly bool _routePostprocessToActionApi;

    public LegacyShoutNetworkGateway(bool routePostprocessToActionApi = true)
    {
        _routePostprocessToActionApi = routePostprocessToActionApi;
    }

    /// <summary>
    /// Compatibility transport boundary for legacy channel owners that still
    /// build their authoritative prompt as the old message list. Keeping the
    /// call here makes the migration explicit without changing ShoutNetwork's
    /// established retry, token accounting, thinking-control or error-text
    /// semantics. The method is a transport facade only; game state remains
    /// owned by the caller and is never accepted as an argument.
    /// </summary>
    public static Task<string> SendLegacyMessagesAsync(
        List<object> messages,
        int maxTokens,
        bool recordTokenStats = true,
        int? overrideMaxTokens = null,
        bool forceDisableThinking = false,
        bool promptRetryOnError = false,
        CancellationToken cancellationToken = default(CancellationToken),
        float? overrideTemperature = null)
    {
        return ShoutNetwork.CallApiWithMessages(
            messages,
            maxTokens,
            recordTokenStats,
            overrideMaxTokens,
            forceDisableThinking,
            promptRetryOnError,
            cancellationToken,
            overrideTemperature);
    }

    /// <summary>
    /// Streaming compatibility boundary for legacy channel owners. The
    /// stream remains observational through callbacks and keeps its existing
    /// stale/cancel/error behavior inside ShoutNetwork.
    /// </summary>
    public static Task SendLegacyMessagesStreamAsync(
        List<object> messages,
        int maxTokens,
        Action<string> onChunk,
        Action<string> onComplete,
        Action<string> onError,
        CancellationToken cancellationToken = default(CancellationToken),
        bool promptRetryOnError = true)
    {
        return ShoutNetwork.CallApiWithMessagesStream(
            messages,
            maxTokens,
            onChunk,
            onComplete,
            onError,
            cancellationToken,
            promptRetryOnError);
    }

    public async Task<LlmGenerateResult> GenerateAsync(LlmGenerateRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        LlmGenerateResult stopped = GetStoppedRequestResult(request, cancellationToken);
        if (stopped != null) { return stopped; }

        if (_routePostprocessToActionApi && request.Stage == InteractionStage.Postprocess)
        {
            return await GenerateActionPostprocessAsync(request, cancellationToken).ConfigureAwait(false);
        }

        List<object> messages = new List<object>(LegacyPromptPackageAdapter.ToLegacyMessages(request.Prompt));

        try
        {
            string rawText = await SendLegacyMessagesAsync(
                messages,
                Math.Max(16, request.Prompt.MaxTokens),
                recordTokenStats: false,
                promptRetryOnError: false,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            stopped = GetStoppedRequestResult(request, cancellationToken);
            if (stopped != null) { return stopped; }
            if (string.Equals((rawText ?? string.Empty).Trim(), SaveRuntimeGuard.BuildStaleRequestErrorText(), StringComparison.Ordinal))
            {
                return new LlmGenerateResult(LlmResultStatus.Cancelled, string.Empty, 0, 0, "stale");
            }
            if (TryClassifyLegacyTransportFailure(rawText, out LlmResultStatus failureStatus, out string failureCode))
            {
                return new LlmGenerateResult(failureStatus, string.Empty, 0, 0, failureCode);
            }

            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new LlmGenerateResult(LlmResultStatus.EmptyResponse, string.Empty, 0, 0, "empty_response");
            }

            return new LlmGenerateResult(LlmResultStatus.Succeeded, rawText, 0, 0, string.Empty);
        }
        catch (OperationCanceledException)
        {
            return new LlmGenerateResult(LlmResultStatus.Cancelled, string.Empty, 0, 0, "cancelled");
        }
        catch (Exception exception)
        {
            return GetStoppedRequestResult(request, cancellationToken) ?? new LlmGenerateResult(
                LlmResultStatus.NonRetryableFailure,
                string.Empty,
                0,
                0,
                "legacy_gateway_" + exception.GetType().Name);
        }
    }

    public async Task<LlmGenerateResult> GenerateStreamAsync(
        LlmGenerateRequest request,
        Action<string> onDelta,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }
        if (request.Stage != InteractionStage.MainReply)
        {
            return new LlmGenerateResult(
                LlmResultStatus.NonRetryableFailure,
                string.Empty,
                0,
                0,
                "stream_stage_not_supported");
        }

        cancellationToken.ThrowIfCancellationRequested();
        List<object> messages = new List<object>(LegacyPromptPackageAdapter.ToLegacyMessages(request.Prompt));
        string finalText = string.Empty;
        string failure = string.Empty;
        try
        {
            await SendLegacyMessagesStreamAsync(
                messages,
                Math.Max(16, request.Prompt.MaxTokens),
                delta =>
                {
                    if (!cancellationToken.IsCancellationRequested && !string.IsNullOrEmpty(delta))
                    {
                        onDelta?.Invoke(delta);
                    }
                },
                completed => finalText = completed ?? string.Empty,
                error => failure = error ?? string.Empty,
                cancellationToken,
                promptRetryOnError: false).ConfigureAwait(false);

            if (!SaveRuntimeGuard.IsCurrentGeneration(request.Trace.RuntimeGeneration))
            {
                return new LlmGenerateResult(LlmResultStatus.Cancelled, string.Empty, 0, 0, "stale");
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return new LlmGenerateResult(LlmResultStatus.Cancelled, string.Empty, 0, 0, "cancelled");
            }
            if (!string.IsNullOrWhiteSpace(failure))
            {
                return new LlmGenerateResult(
                    LlmResultStatus.RetryableFailure,
                    string.Empty,
                    0,
                    0,
                    "legacy_stream_failure");
            }
            if (string.IsNullOrWhiteSpace(finalText))
            {
                return new LlmGenerateResult(LlmResultStatus.EmptyResponse, string.Empty, 0, 0, "empty_response");
            }
            return new LlmGenerateResult(LlmResultStatus.Succeeded, finalText, 0, 0, string.Empty);
        }
        catch (OperationCanceledException)
        {
            return new LlmGenerateResult(LlmResultStatus.Cancelled, string.Empty, 0, 0, "cancelled");
        }
        catch (Exception exception)
        {
            return new LlmGenerateResult(
                LlmResultStatus.NonRetryableFailure,
                string.Empty,
                0,
                0,
                "legacy_stream_gateway_" + exception.GetType().Name);
        }
    }

    private static LlmGenerateResult GetStoppedRequestResult(LlmGenerateRequest request, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new LlmGenerateResult(LlmResultStatus.Cancelled, string.Empty, 0, 0, "cancelled");
        }
        if (!SaveRuntimeGuard.IsCurrentGeneration(request.Trace.RuntimeGeneration))
        {
            return new LlmGenerateResult(LlmResultStatus.Cancelled, string.Empty, 0, 0, "stale");
        }
        return null;
    }

    private static bool TryClassifyLegacyTransportFailure(string rawText, out LlmResultStatus status, out string errorCode)
    {
        status = LlmResultStatus.RetryableFailure;
        errorCode = "legacy_network_failure";
        string text = (rawText ?? string.Empty).TrimStart();
        // ShoutNetwork returns BuildFailureDetail envelopes, not typed errors. Match its
        // reserved reason prefixes AND detail marker, never exception words in NPC dialogue.
        if (text.IndexOf("【模型回复（完整）】", StringComparison.Ordinal) < 0) { return false; }
        if (text.StartsWith("（错误：未配置 API Key）", StringComparison.Ordinal)
            || text.StartsWith("（错误：未配置模型名称）", StringComparison.Ordinal))
        {
            status = LlmResultStatus.NonRetryableFailure;
            errorCode = "legacy_config_invalid";
            return true;
        }
        return text.StartsWith("（API请求失败:", StringComparison.Ordinal)
            || text.StartsWith("（API响应格式错误:", StringComparison.Ordinal)
            || text.StartsWith("（程序错误:", StringComparison.Ordinal);
    }

    private static async Task<LlmGenerateResult> GenerateActionPostprocessAsync(
        LlmGenerateRequest request,
        CancellationToken cancellationToken)
    {
        LlmGenerateResult stopped = GetStoppedRequestResult(request, cancellationToken);
        if (stopped != null) { return stopped; }
        string systemPrompt = string.Empty;
        List<string> userSections = new List<string>();
        foreach (PromptMessage message in request.Prompt.Messages)
        {
            if (string.Equals(message.Role, "system", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(message.Content))
                {
                    systemPrompt = string.IsNullOrWhiteSpace(systemPrompt)
                        ? message.Content
                        : systemPrompt + Environment.NewLine + message.Content;
                }
            }
            else if (!string.IsNullOrWhiteSpace(message.Content))
            {
                userSections.Add("[" + message.Role + "]" + Environment.NewLine + message.Content);
            }
        }

        string userPrompt = string.Join(Environment.NewLine + Environment.NewLine, userSections);
        string content = string.Empty;
        string error = string.Empty;
        try
        {
            bool succeeded = await Task.Run(
                () =>
                {
                    if (GetStoppedRequestResult(request, cancellationToken) != null) { return false; }
                    return AIConfigHandler.TryCallAuxiliaryActionPostprocessOnceForExternal(
                        systemPrompt,
                        userPrompt,
                        Math.Max(16, request.Prompt.MaxTokens),
                        0f,
                        out content,
                        out error);
                },
                cancellationToken).ConfigureAwait(false);
            // The legacy action API is synchronous and has no cancellation parameter.
            // A running call cannot be aborted here; its late result must never become executable.
            stopped = GetStoppedRequestResult(request, cancellationToken);
            if (stopped != null) { return stopped; }
            if (!succeeded)
            {
                return new LlmGenerateResult(
                    LlmResultStatus.RetryableFailure,
                    string.Empty,
                    0,
                    0,
                    string.IsNullOrWhiteSpace(error) ? "action_postprocess_failed" : error);
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return new LlmGenerateResult(LlmResultStatus.EmptyResponse, string.Empty, 0, 0, "empty_response");
            }
            return new LlmGenerateResult(LlmResultStatus.Succeeded, content, 0, 0, string.Empty);
        }
        catch (OperationCanceledException)
        {
            return new LlmGenerateResult(LlmResultStatus.Cancelled, string.Empty, 0, 0, "cancelled");
        }
        catch (Exception exception)
        {
            return GetStoppedRequestResult(request, cancellationToken) ?? new LlmGenerateResult(
                LlmResultStatus.NonRetryableFailure,
                string.Empty,
                0,
                0,
                "legacy_action_gateway_" + exception.GetType().Name);
        }
    }
}
