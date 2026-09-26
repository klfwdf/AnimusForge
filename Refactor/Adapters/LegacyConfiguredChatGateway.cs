using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Adapter-local result for user-invoked connection validation. The response
/// body stays inside the settings/adapter boundary and is not part of the
/// shared LLM contract DTOs.
/// </summary>
public sealed class ConfiguredChatValidationExchange
{
    public ConfiguredChatValidationExchange(int statusCode, string responseBody, LlmGenerateResult result)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody ?? string.Empty;
        Result = result ?? throw new ArgumentNullException(nameof(result));
    }

    public int StatusCode { get; }
    public bool HasStatusCode => StatusCode > 0;
    public string ResponseBody { get; }
    public string ErrorMessage => Result?.ErrorCode ?? string.Empty;
    public bool IsSuccessStatusCode => StatusCode >= 200 && StatusCode <= 299;
    public LlmGenerateResult Result { get; }
}


/// <summary>
/// Adapter-local diagnostics for a configured chat generation. Response bodies
/// are retained only for the legacy caller that owns user-facing diagnostics;
/// they never enter the shared immutable LLM contract or save data.
/// </summary>
public sealed class ConfiguredChatGenerationExchange
{
    public ConfiguredChatGenerationExchange(
        int statusCode,
        string responseBody,
        string rawStreamSample,
        string requestBody,
        string controlMode,
        LlmGenerateResult result)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody ?? string.Empty;
        RawStreamSample = rawStreamSample ?? string.Empty;
        RequestBody = requestBody ?? string.Empty;
        ControlMode = controlMode ?? string.Empty;
        Result = result ?? throw new ArgumentNullException(nameof(result));
    }

    public int StatusCode { get; }
    public string ResponseBody { get; }
    public string RawStreamSample { get; }
    public string RequestBody { get; }
    public string ControlMode { get; }
    public LlmGenerateResult Result { get; }
}

/// <summary>
/// Shared OpenAI-compatible transport for legacy optional/domain clients.
/// Credentials are resolved by a caller-owned delegate at the send boundary;
/// they are never stored in a contract DTO, snapshot, log or save. This class
/// centralizes auth, timeout, cancellation and assistant-text extraction while
/// leaving each domain's prompt and response validation in its owner.
/// </summary>
public sealed class LegacyConfiguredChatGateway : ILlmGateway, ILlmStreamingGateway
{
    private readonly Func<LlmProviderSnapshot, string> _credentialResolver;
    private readonly string _ownerBridgeId = FeatureBridgeIds.ConversationGateway;
    private readonly float? _temperature;
    private readonly bool _disableThinking;
    private readonly bool _useConfiguredMaxTokens;
    private readonly bool _useConfiguredTemperature;
    private readonly bool _retryWithoutThinkingOnBadRequest;
    private readonly bool _thinkingEnabled;
    private readonly string _reasoningEffort;

    public LegacyConfiguredChatGateway(
        Func<LlmProviderSnapshot, string> credentialResolver,
        float? temperature = null,
        bool disableThinking = true,
        bool useConfiguredMaxTokens = false,
        bool useConfiguredTemperature = false,
        bool retryWithoutThinkingOnBadRequest = false,
        bool thinkingEnabled = false,
        string reasoningEffort = null)
    {
        _credentialResolver = credentialResolver ?? throw new ArgumentNullException(nameof(credentialResolver));
        _temperature = temperature;
        _disableThinking = disableThinking;
        _useConfiguredMaxTokens = useConfiguredMaxTokens;
        _useConfiguredTemperature = useConfiguredTemperature;
        _retryWithoutThinkingOnBadRequest = retryWithoutThinkingOnBadRequest;
        _thinkingEnabled = thinkingEnabled;
        _reasoningEffort = string.IsNullOrWhiteSpace(reasoningEffort)
            ? DuelSettings.ReasoningEffortLow
            : reasoningEffort.Trim();
    }

    // Knowledge owns a separate bridge. Sharing provider transport must not make
    // that domain depend on the conversation entry's enablement.
    internal static LegacyConfiguredChatGateway ForKnowledgeProfile(
        Func<LlmProviderSnapshot, string> credentialResolver, float? temperature)
    {
        return new LegacyConfiguredChatGateway(credentialResolver, temperature, FeatureBridgeIds.GatewayKnowledgeProfile);
    }

    private LegacyConfiguredChatGateway(
        Func<LlmProviderSnapshot, string> credentialResolver, float? temperature, string ownerBridgeId)
        : this(credentialResolver, temperature, disableThinking: true)
    {
        _ownerBridgeId = ownerBridgeId;
    }

    public async Task<LlmGenerateResult> GenerateAsync(
        LlmGenerateRequest request,
        CancellationToken cancellationToken)
    {
        ConfiguredChatGenerationExchange exchange = await GenerateExchangeAsync(
            request,
            streamResponse: false,
            onDelta: null,
            cancellationToken).ConfigureAwait(false);
        return exchange.Result;
    }

    /// <summary>
    /// Configured-chat streaming capability. Delta callbacks are observational;
    /// the returned result remains the authoritative complete response.
    /// </summary>
    public async Task<LlmGenerateResult> GenerateStreamAsync(
        LlmGenerateRequest request,
        Action<string> onDelta,
        CancellationToken cancellationToken)
    {
        ConfiguredChatGenerationExchange exchange = await GenerateExchangeAsync(
            request,
            streamResponse: true,
            onDelta,
            cancellationToken).ConfigureAwait(false);
        return exchange.Result;
    }

    /// <summary>
    /// Adapter-local generation entry used by legacy owners that still need
    /// response diagnostics and the prepared request body for token accounting.
    /// The public contract result remains string/status/metadata only.
    /// </summary>
    public async Task<ConfiguredChatGenerationExchange> GenerateExchangeAsync(
        LlmGenerateRequest request,
        bool streamResponse,
        Action<string> onDelta,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        bool bridgeEnabled = _ownerBridgeId == FeatureBridgeIds.GatewayKnowledgeProfile
            ? FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.GatewayKnowledgeProfile)
            : FeatureBridgeRuntime.IsEnabled(FeatureBridgeIds.ConversationGateway);
        if (!bridgeEnabled)
        {
            return CreateExchange(
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                "bridge-disabled",
                new LlmGenerateResult(
                    LlmResultStatus.NonRetryableFailure,
                    string.Empty,
                    0,
                    0,
                    _ownerBridgeId == FeatureBridgeIds.GatewayKnowledgeProfile
                        ? "bridge.gateway_knowledge_profile_disabled"
                        : "bridge.conversation_gateway_disabled"));
        }

        LlmProviderSnapshot provider = request.Provider;
        string endpoint = (provider.Endpoint ?? string.Empty).Trim();
        string model = (provider.Model ?? string.Empty).Trim();
        string apiKey = _credentialResolver(provider) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(apiKey))
        {
            return CreateExchange(
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                "plain",
                new LlmGenerateResult(LlmResultStatus.NonRetryableFailure, string.Empty, 0, 0, "provider_configuration_incomplete"));
        }

        DuelSettings settings = _useConfiguredMaxTokens || _useConfiguredTemperature
            ? DuelSettings.GetSettings()
            : null;
        int maxTokens = _useConfiguredMaxTokens
            ? Math.Max(16, settings?.GetAuxiliaryApiMaxTokens() ?? request.Prompt.MaxTokens)
            : Math.Max(16, request.Prompt.MaxTokens);
        float? effectiveTemperature = _temperature;
        if (!effectiveTemperature.HasValue && _useConfiguredTemperature && settings != null)
        {
            effectiveTemperature = settings.GetAuxiliaryApiTemperature();
        }

        JObject payload = BuildPayload(
            request.Prompt,
            model,
            endpoint,
            maxTokens,
            effectiveTemperature,
            streamResponse,
            out string controlMode);
        string body = LlmApiCompat.PrepareChatRequestJson(endpoint, payload);
        using (CancellationTokenSource timeout = LlmNonStreamingTransport.CreateTimeout(provider.TimeoutMilliseconds, cancellationToken))
        {
            try
            {
                GatewayExchange exchange = await SendOnceAsync(
                    endpoint,
                    apiKey,
                    body,
                    streamResponse,
                    onDelta,
                    timeout.Token).ConfigureAwait(false);
                if (_retryWithoutThinkingOnBadRequest
                    && exchange.StatusCode == 400
                    && !_disableThinking
                    && DuelSettings.ResolveThinkingControlFormat(endpoint, model) != "plain"
                    && AIConfigHandler.LooksLikeAuxiliaryThinkingControlErrorForExternal(exchange.ResponseBody))
                {
                    DuelSettings.RemoveThinkingControls(payload);
                    body = LlmApiCompat.PrepareChatRequestJson(endpoint, payload);
                    exchange = await SendOnceAsync(
                        endpoint,
                        apiKey,
                        body,
                        streamResponse,
                        onDelta,
                        timeout.Token).ConfigureAwait(false);
                    controlMode = controlMode + "_retry_plain";
                }

                return CreateExchange(
                    exchange.StatusCode,
                    exchange.ResponseBody,
                    exchange.RawStreamSample,
                    body,
                    controlMode,
                    exchange.Result);
            }
            catch (OperationCanceledException)
            {
                return CreateExchange(
                    0,
                    string.Empty,
                    string.Empty,
                    body,
                    controlMode,
                    new LlmGenerateResult(LlmResultStatus.Cancelled, string.Empty, 0, 0, "cancelled"));
            }
            catch (Exception exception)
            {
                return CreateExchange(
                    0,
                    string.Empty,
                    string.Empty,
                    body,
                    controlMode,
                    new LlmGenerateResult(
                        LlmResultStatus.NonRetryableFailure,
                        string.Empty,
                        0,
                        0,
                        "configured_gateway_" + exception.GetType().Name));
            }
        }
    }

    private JObject BuildPayload(
        PromptPackage prompt,
        string model,
        string endpoint,
        int maxTokens,
        float? temperature,
        bool streamResponse,
        out string controlMode)
    {
        JObject payload = new JObject
        {
            ["model"] = model,
            ["messages"] = JArray.FromObject(ToJsonMessages(prompt)),
            ["stream"] = streamResponse,
            ["max_tokens"] = Math.Max(16, maxTokens)
        };
        if (temperature.HasValue)
        {
            payload["temperature"] = Math.Max(0f, Math.Min(1.5f, temperature.Value));
        }
        if (_disableThinking)
        {
            DuelSettings.ApplyThinkingControls(
                payload,
                endpoint,
                model,
                thinkingEnabled: false,
                DuelSettings.ReasoningEffortLow,
                out controlMode);
        }
        else
        {
            DuelSettings.ApplyThinkingControls(
                payload,
                endpoint,
                model,
                _thinkingEnabled,
                _reasoningEffort,
                out controlMode);
        }
        return payload;
    }

    private static ConfiguredChatGenerationExchange CreateExchange(
        int statusCode,
        string responseBody,
        string rawStreamSample,
        string requestBody,
        string controlMode,
        LlmGenerateResult result)
    {
        return new ConfiguredChatGenerationExchange(
            statusCode,
            responseBody,
            rawStreamSample,
            requestBody,
            controlMode,
            result);
    }

    /// <summary>
    /// Sends an already composed, non-streaming validation payload. Settings
    /// owners retain prompt/format validation and UI semantics; this adapter
    /// owns only authentication, timeout, cancellation and response capture.
    /// </summary>
    public async Task<ConfiguredChatValidationExchange> SendValidationAsync(
        LlmProviderSnapshot provider,
        JObject payload,
        CancellationToken cancellationToken)
    {
        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }
        if (payload == null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        string endpoint = (provider.Endpoint ?? string.Empty).Trim();
        string model = (provider.Model ?? string.Empty).Trim();
        string apiKey = _credentialResolver(provider) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(apiKey))
        {
            LlmGenerateResult invalid = new LlmGenerateResult(
                LlmResultStatus.NonRetryableFailure,
                string.Empty,
                0,
                0,
                "provider_configuration_incomplete");
            return new ConfiguredChatValidationExchange(0, string.Empty, invalid);
        }

        string body = LlmApiCompat.PrepareChatRequestJson(endpoint, payload);
        using CancellationTokenSource timeout = LlmNonStreamingTransport.CreateTimeout(provider.TimeoutMilliseconds, cancellationToken);
        try
        {
            GatewayExchange exchange = await SendOnceAsync(endpoint, apiKey, body, timeout.Token).ConfigureAwait(false);
            return new ConfiguredChatValidationExchange(exchange.StatusCode, exchange.ResponseBody, exchange.Result);
        }
        catch (OperationCanceledException)
        {
            LlmGenerateResult cancelled = new LlmGenerateResult(
                LlmResultStatus.Cancelled,
                string.Empty,
                0,
                0,
                "cancelled");
            return new ConfiguredChatValidationExchange(0, string.Empty, cancelled);
        }
        catch (Exception exception)
        {
            LlmGenerateResult failed = new LlmGenerateResult(
                LlmResultStatus.NonRetryableFailure,
                string.Empty,
                0,
                0,
                "configured_validation_gateway_" + exception.GetType().Name);
            return new ConfiguredChatValidationExchange(0, string.Empty, failed);
        }
    }


    /// <summary>
    /// Sends JSON that was already prepared by a legacy owner. This is used
    /// only when the owner has already applied provider-specific conversion
    /// (for example, the auxiliary router's Anthropic payload); the adapter
    /// must not convert that body a second time.
    /// </summary>
    public async Task<ConfiguredChatValidationExchange> SendValidationJsonAsync(
        LlmProviderSnapshot provider,
        string preparedJson,
        CancellationToken cancellationToken)
    {
        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        string endpoint = (provider.Endpoint ?? string.Empty).Trim();
        string model = (provider.Model ?? string.Empty).Trim();
        string apiKey = _credentialResolver(provider) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(apiKey))
        {
            LlmGenerateResult invalid = new LlmGenerateResult(
                LlmResultStatus.NonRetryableFailure,
                string.Empty,
                0,
                0,
                "provider_configuration_incomplete");
            return new ConfiguredChatValidationExchange(0, string.Empty, invalid);
        }

        string body = string.IsNullOrWhiteSpace(preparedJson) ? "{}" : preparedJson;
        using CancellationTokenSource timeout = LlmNonStreamingTransport.CreateTimeout(provider.TimeoutMilliseconds, cancellationToken);
        try
        {
            GatewayExchange exchange = await SendOnceAsync(endpoint, apiKey, body, timeout.Token).ConfigureAwait(false);
            return new ConfiguredChatValidationExchange(exchange.StatusCode, exchange.ResponseBody, exchange.Result);
        }
        catch (OperationCanceledException)
        {
            LlmGenerateResult cancelled = new LlmGenerateResult(
                LlmResultStatus.Cancelled,
                string.Empty,
                0,
                0,
                "cancelled");
            return new ConfiguredChatValidationExchange(0, string.Empty, cancelled);
        }
        catch (Exception exception)
        {
            LlmGenerateResult failed = new LlmGenerateResult(
                LlmResultStatus.NonRetryableFailure,
                string.Empty,
                0,
                0,
                "configured_validation_gateway_" + exception.GetType().Name);
            return new ConfiguredChatValidationExchange(0, string.Empty, failed);
        }
    }

    private sealed class GatewayExchange
    {
        public int StatusCode { get; set; }
        public string ResponseBody { get; set; }
        public string RawStreamSample { get; set; }
        public LlmGenerateResult Result { get; set; }
    }

    private static async Task<GatewayExchange> SendOnceAsync(
        string endpoint,
        string apiKey,
        string body,
        CancellationToken cancellationToken)
    {
        return await SendOnceAsync(
            endpoint,
            apiKey,
            body,
            streamResponse: false,
            onDelta: null,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<GatewayExchange> SendOnceAsync(
        string endpoint,
        string apiKey,
        string body,
        bool streamResponse,
        Action<string> onDelta,
        CancellationToken cancellationToken)
    {
        if (!streamResponse)
        {
            LlmNonStreamingResponse response = await LlmNonStreamingTransport.SendAsync(
                endpoint, apiKey, body,
                (request, token) => DuelSettings.GlobalClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, token),
                cancellationToken).ConfigureAwait(false);
            int status = (int)response.StatusCode;
            string rawText = response.IsSuccessStatusCode ? ExtractAssistantText(response.Body) : string.Empty;
            return new GatewayExchange
            {
                StatusCode = status,
                ResponseBody = response.Body ?? string.Empty,
                Result = !response.IsSuccessStatusCode ? CreateHttpFailureResult(status, response.RetryAfterSeconds)
                    : string.IsNullOrWhiteSpace(rawText)
                        ? new LlmGenerateResult(LlmResultStatus.EmptyResponse, string.Empty, 0, 0, "empty_response", new LlmGenerateMetadata(statusCode: status))
                        : new LlmGenerateResult(LlmResultStatus.Succeeded, rawText, 0, 0, string.Empty, new LlmGenerateMetadata(statusCode: status))
            };
        }
        LlmStreamingResponse streamResult = await LlmStreamingTransport.SendAsync(
            endpoint, apiKey, body,
            (request, token) => DuelSettings.GlobalClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token),
            cancellationToken,
            onDelta: delta =>
            {
                if (!string.IsNullOrEmpty(delta.Content)) onDelta?.Invoke(delta.Content);
            },
            throwOnCancellationBeforeRead: true,
            rawSampleMaxChars: 12000,
            includeDataPrefix: true).ConfigureAwait(false);
        int statusCode = (int)streamResult.StatusCode;
        string raw = streamResult.Content ?? string.Empty;
        LlmGenerateResult result = !streamResult.IsSuccessStatusCode
            ? CreateHttpFailureResult(statusCode, streamResult.RetryAfterSeconds)
            : string.IsNullOrWhiteSpace(raw)
                ? new LlmGenerateResult(
                    streamResult.ParseFailure ? LlmResultStatus.InvalidResponse : LlmResultStatus.EmptyResponse,
                    string.Empty, 0, 0,
                    streamResult.ParseFailure ? "stream_parse_failed" : "empty_response",
                    new LlmGenerateMetadata(statusCode: statusCode))
                : new LlmGenerateResult(
                    LlmResultStatus.Succeeded, raw, 0, 0, string.Empty,
                    new LlmGenerateMetadata(statusCode: statusCode));
        return new GatewayExchange
        {
            StatusCode = statusCode,
            ResponseBody = streamResult.ErrorBody ?? string.Empty,
            RawStreamSample = streamResult.RawStreamSample ?? string.Empty,
            Result = result
        };
    }

    private static string ExtractAssistantText(string responseBody)
    {
        try
        {
            return (LlmApiCompat.ExtractAssistantText(JObject.Parse(responseBody ?? string.Empty)) ?? string.Empty).Trim();
        }
        catch
        {
            return (LlmApiCompat.ExtractAssistantText(responseBody ?? string.Empty) ?? string.Empty).Trim();
        }
    }


    private static LlmGenerateResult CreateHttpFailureResult(int statusCode, int? retryAfterSeconds)
    {
        return new LlmGenerateResult(
                            statusCode == 401 || statusCode == 403
                                ? LlmResultStatus.NonRetryableFailure
                                : statusCode == 429 || statusCode >= 500
                                    ? LlmResultStatus.RetryableFailure
                                    : LlmResultStatus.NonRetryableFailure,
                            string.Empty,
                            0,
                            0,
                            "http_" + statusCode,
                            new LlmGenerateMetadata(
                                statusCode: statusCode,
                                isRateLimit: statusCode == 429,
                                isAuthFailure: statusCode == 401 || statusCode == 403,
                                isTimeout: statusCode == 408,
                                retryAfterSeconds: retryAfterSeconds));
    }

    private static IReadOnlyList<object> ToJsonMessages(PromptPackage prompt)
    {
        List<object> messages = new List<object>();
        foreach (PromptMessage message in prompt?.Messages ?? Array.Empty<PromptMessage>())
        {
            messages.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["role"] = message?.Role ?? "user",
                ["content"] = message?.Content ?? string.Empty
            });
        }
        return messages;
    }

    internal static PromptPackage BuildPromptPackage(
        IEnumerable<object> messages,
        int maxTokens,
        string model)
    {
        List<PromptMessage> copied = new List<PromptMessage>();
        foreach (object message in messages ?? Array.Empty<object>())
        {
            JObject item = message == null ? null : JObject.FromObject(message);
            copied.Add(new PromptMessage(
                item?["role"]?.ToString() ?? "user",
                item?["content"]?.ToString() ?? string.Empty));
        }
        return new PromptPackage(
            copied,
            Math.Max(1, maxTokens),
            string.IsNullOrWhiteSpace(model) ? "configured-chat" : model);
    }

}
