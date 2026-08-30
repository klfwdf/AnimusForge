using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
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
/// Shared OpenAI-compatible transport for legacy optional/domain clients.
/// Credentials are resolved by a caller-owned delegate at the send boundary;
/// they are never stored in a contract DTO, snapshot, log or save. This class
/// centralizes auth, timeout, cancellation and assistant-text extraction while
/// leaving each domain's prompt and response validation in its owner.
/// </summary>
public sealed class LegacyConfiguredChatGateway : ILlmGateway
{
    private readonly Func<LlmProviderSnapshot, string> _credentialResolver;
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

    public async Task<LlmGenerateResult> GenerateAsync(
        LlmGenerateRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        LlmProviderSnapshot provider = request.Provider;
        string endpoint = (provider.Endpoint ?? string.Empty).Trim();
        string model = (provider.Model ?? string.Empty).Trim();
        string apiKey = _credentialResolver(provider) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(apiKey))
        {
            return new LlmGenerateResult(LlmResultStatus.NonRetryableFailure, string.Empty, 0, 0, "provider_configuration_incomplete");
        }

        DuelSettings settings = _useConfiguredMaxTokens || _useConfiguredTemperature
            ? DuelSettings.GetSettings()
            : null;
        int maxTokens = _useConfiguredMaxTokens
            ? Math.Max(16, settings?.GetAuxiliaryApiMaxTokens() ?? request.Prompt.MaxTokens)
            : Math.Max(16, request.Prompt.MaxTokens);
        JObject payload = new JObject
        {
            ["model"] = model,
            ["messages"] = JArray.FromObject(ToJsonMessages(request.Prompt)),
            ["stream"] = false,
            ["max_tokens"] = maxTokens
        };
        float? effectiveTemperature = _temperature;
        if (!effectiveTemperature.HasValue && _useConfiguredTemperature && settings != null)
        {
            effectiveTemperature = settings.GetAuxiliaryApiTemperature();
        }
        if (effectiveTemperature.HasValue)
        {
            payload["temperature"] = Math.Max(0f, Math.Min(1.5f, effectiveTemperature.Value));
        }
        if (_disableThinking)
        {
            DuelSettings.ApplyThinkingControls(
                payload,
                endpoint,
                model,
                thinkingEnabled: false,
                DuelSettings.ReasoningEffortLow,
                out _);
        }
        else
        {
            DuelSettings.ApplyThinkingControls(
                payload,
                endpoint,
                model,
                _thinkingEnabled,
                _reasoningEffort,
                out _);
        }

        string body = LlmApiCompat.PrepareChatRequestJson(endpoint, payload);
        using CancellationTokenSource timeout = CreateTimeout(provider.TimeoutMilliseconds, cancellationToken);
        try
        {
            GatewayExchange exchange = await SendOnceAsync(endpoint, apiKey, body, timeout.Token).ConfigureAwait(false);
            if (_retryWithoutThinkingOnBadRequest &&
                exchange.StatusCode == 400 &&
                !_disableThinking &&
                DuelSettings.ResolveThinkingControlFormat(endpoint, model) != "plain" &&
                AIConfigHandler.LooksLikeAuxiliaryThinkingControlErrorForExternal(exchange.ResponseBody))
            {
                DuelSettings.RemoveThinkingControls(payload);
                body = LlmApiCompat.PrepareChatRequestJson(endpoint, payload);
                exchange = await SendOnceAsync(endpoint, apiKey, body, timeout.Token).ConfigureAwait(false);
            }
            return exchange.Result;
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
                "configured_gateway_" + exception.GetType().Name);
        }
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
        using CancellationTokenSource timeout = CreateTimeout(provider.TimeoutMilliseconds, cancellationToken);
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
        using CancellationTokenSource timeout = CreateTimeout(provider.TimeoutMilliseconds, cancellationToken);
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
        public LlmGenerateResult Result { get; set; }
    }

    private static async Task<GatewayExchange> SendOnceAsync(
        string endpoint,
        string apiKey,
        string body,
        CancellationToken cancellationToken)
    {
        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, endpoint))
        {
            LlmApiCompat.ApplyAuthenticationHeaders(request, endpoint, apiKey);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            using (HttpResponseMessage response = await DuelSettings.GlobalClient
                .SendAsync(request, cancellationToken).ConfigureAwait(false))
            {
                string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                int statusCode = (int)response.StatusCode;
                if (!response.IsSuccessStatusCode)
                {
                    return new GatewayExchange
                    {
                        StatusCode = statusCode,
                        ResponseBody = responseBody ?? string.Empty,
                        Result = new LlmGenerateResult(
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
                                isAuthFailure: statusCode == 401 || statusCode == 403))
                    };
                }

                string rawText;
                try
                {
                    rawText = (LlmApiCompat.ExtractAssistantText(JObject.Parse(responseBody)) ?? string.Empty).Trim();
                }
                catch
                {
                    rawText = (LlmApiCompat.ExtractAssistantText(responseBody) ?? string.Empty).Trim();
                }
                return new GatewayExchange
                {
                    StatusCode = statusCode,
                    ResponseBody = responseBody ?? string.Empty,
                    Result = string.IsNullOrWhiteSpace(rawText)
                        ? new LlmGenerateResult(LlmResultStatus.EmptyResponse, string.Empty, 0, 0, "empty_response", new LlmGenerateMetadata(statusCode: statusCode))
                        : new LlmGenerateResult(LlmResultStatus.Succeeded, rawText, 0, 0, string.Empty, new LlmGenerateMetadata(statusCode: statusCode))
                };
            }
        }
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

    private static CancellationTokenSource CreateTimeout(int timeoutMilliseconds, CancellationToken callerToken)
    {
        CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        if (timeoutMilliseconds > 0)
        {
            linked.CancelAfter(timeoutMilliseconds);
        }
        return linked;
    }
}
