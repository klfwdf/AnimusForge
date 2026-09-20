using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Transport result for the OpenAI-compatible model catalog endpoint.
/// This is deliberately not an LLM generation result: onboarding/settings own
/// model parsing and UI/status policy, while this adapter owns only GET
/// transport, authentication, cancellation and response capture.
/// </summary>
public sealed class ModelCatalogExchange
{
    public ModelCatalogExchange(
        string requestUrl,
        int statusCode,
        string reasonPhrase,
        string responseBody,
        string errorMessage,
        bool cancelled)
        : this(requestUrl, statusCode, reasonPhrase, responseBody, errorMessage, cancelled, string.Empty, null)
    {
    }

    public ModelCatalogExchange(
        string requestUrl,
        int statusCode,
        string reasonPhrase,
        string responseBody,
        string errorMessage,
        bool cancelled,
        string errorCode,
        IReadOnlyDictionary<string, string> errorArguments)
    {
        RequestUrl = requestUrl ?? string.Empty;
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase ?? string.Empty;
        ResponseBody = responseBody ?? string.Empty;
        ErrorMessage = errorMessage ?? string.Empty;
        Cancelled = cancelled;
        ErrorCode = errorCode ?? string.Empty;
        ErrorArguments = ModelCatalogErrorFormatter.BoundArguments(errorArguments);
    }

    public string RequestUrl { get; }
    public int StatusCode { get; }
    public bool HasStatusCode => StatusCode > 0;
    public bool IsSuccessStatusCode => StatusCode >= 200 && StatusCode <= 299;
    public string ReasonPhrase { get; }
    public string ResponseBody { get; }
    public string ErrorMessage { get; }
    public bool Cancelled { get; }
    public string ErrorCode { get; }
    public IReadOnlyDictionary<string, string> ErrorArguments { get; }
}

public static class ModelCatalogErrorCodes
{
    public const string UrlMissing = "model_catalog.url_missing";
    public const string ApiKeyMissing = "model_catalog.api_key_missing";
    public const string Cancelled = "model_catalog.cancelled";
    public const string HttpFailure = "model_catalog.http_failure";
    public const string TransportFailed = "model_catalog.transport_failed";
}

public static class ModelCatalogErrorFormatter
{
    private const int MaxArguments = 4;
    private const int MaxArgumentLength = 96;
    private static readonly HashSet<string> AllowedArgumentKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        "status",
        "reason",
        "exceptionType",
    };

    internal static IReadOnlyDictionary<string, string> BoundArguments(IReadOnlyDictionary<string, string> arguments)
    {
        Dictionary<string, string> bounded = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> pair in arguments ?? new Dictionary<string, string>())
        {
            if (bounded.Count >= MaxArguments || string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value)
                || !AllowedArgumentKeys.Contains(pair.Key.Trim()))
            {
                continue;
            }
            bounded[pair.Key.Trim()] = pair.Value.Trim().Length <= MaxArgumentLength
                ? pair.Value.Trim()
                : pair.Value.Trim().Substring(0, MaxArgumentLength);
        }
        return new ReadOnlyDictionary<string, string>(bounded);
    }

    public static string Format(string errorCode, IReadOnlyDictionary<string, string> arguments, string cultureId = "zh-CN", string legacyMessage = null)
    {
        bool chinese = string.IsNullOrWhiteSpace(cultureId)
            || cultureId.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        string status = arguments != null && arguments.TryGetValue("status", out string statusValue) ? statusValue : "";
        string reason = arguments != null && arguments.TryGetValue("reason", out string reasonValue) ? reasonValue : "";
        if (chinese)
        {
            // Keep the legacy display text when an existing owner supplied
            // one (for example, ProbeBaseUrlAsync historically used a
            // different URL-missing sentence).  English callers still get a
            // stable formatter-owned fallback below.
            if (!string.IsNullOrWhiteSpace(legacyMessage))
            {
                return legacyMessage;
            }
            switch (errorCode)
            {
                case ModelCatalogErrorCodes.UrlMissing: return "API 地址为空，无法拉取模型列表。";
                case ModelCatalogErrorCodes.ApiKeyMissing: return "API Key 为空，无法拉取模型列表。";
                case ModelCatalogErrorCodes.Cancelled: return "模型列表拉取已取消。";
                case ModelCatalogErrorCodes.HttpFailure: return "HTTP " + status + (string.IsNullOrWhiteSpace(reason) ? "" : " " + reason);
                case ModelCatalogErrorCodes.TransportFailed: return "模型列表请求失败。";
            }
            return string.IsNullOrWhiteSpace(legacyMessage) ? "模型列表请求失败。" : legacyMessage;
        }
        switch (errorCode)
        {
            case ModelCatalogErrorCodes.UrlMissing: return "Model catalog URL is missing.";
            case ModelCatalogErrorCodes.ApiKeyMissing: return "Model catalog API key is missing.";
            case ModelCatalogErrorCodes.Cancelled: return "Model catalog request was cancelled.";
            case ModelCatalogErrorCodes.HttpFailure: return "HTTP " + status + (string.IsNullOrWhiteSpace(reason) ? "" : " " + reason);
            case ModelCatalogErrorCodes.TransportFailed: return "Model catalog request failed.";
            default: return string.IsNullOrWhiteSpace(legacyMessage) ? "Model catalog request failed." : legacyMessage;
        }
    }
}

/// <summary>
/// Legacy model-list transport used by MCM and onboarding. It intentionally
/// does not parse or cache model names so existing owners retain their exact
/// parsing, ordering, UI and stale-version behavior.
/// </summary>
public sealed class LegacyModelCatalogGateway
{
    private readonly HttpClient _httpClient;

    public LegacyModelCatalogGateway(HttpClient httpClient = null)
    {
        _httpClient = httpClient ?? DuelSettings.GlobalClient;
    }

    public Task<ModelCatalogExchange> ProbeBaseUrlAsync(
        string rawApiUrl,
        CancellationToken cancellationToken)
    {
        return SendAsync(rawApiUrl, string.Empty, includeAuthentication: false, cancellationToken);
    }

    public Task<ModelCatalogExchange> FetchModelsAsync(
        string rawApiUrl,
        string apiKey,
        CancellationToken cancellationToken)
    {
        return SendAsync(rawApiUrl, apiKey, includeAuthentication: true, cancellationToken);
    }

    private async Task<ModelCatalogExchange> SendAsync(
        string rawApiUrl,
        string apiKey,
        bool includeAuthentication,
        CancellationToken cancellationToken)
    {
        string requestUrl = LlmApiCompat.BuildModelListApiUrl(rawApiUrl);
        if (string.IsNullOrWhiteSpace(requestUrl))
        {
            return new ModelCatalogExchange(
                requestUrl,
                0,
                string.Empty,
                string.Empty,
                includeAuthentication
                    ? "API 地址为空，无法拉取模型列表。"
                    : "API 地址为空，无法访问模型列表。",
                cancelled: false,
                ModelCatalogErrorCodes.UrlMissing,
                null);
        }
        if (includeAuthentication && string.IsNullOrWhiteSpace(apiKey))
        {
            return new ModelCatalogExchange(
                requestUrl,
                0,
                string.Empty,
                string.Empty,
                "API Key 为空，无法拉取模型列表。",
                cancelled: false,
                ModelCatalogErrorCodes.ApiKeyMissing,
                null);
        }

        try
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl))
            {
                if (includeAuthentication)
                {
                    LlmApiCompat.ApplyAuthenticationHeaders(request, requestUrl, apiKey);
                }

                using (HttpResponseMessage response = await _httpClient
                    .SendAsync(request, cancellationToken)
                    .ConfigureAwait(false))
                {
                    string responseBody = await response.Content
                        .ReadAsStringAsync()
                        .ConfigureAwait(false);
                    string errorCode = response.IsSuccessStatusCode ? string.Empty : ModelCatalogErrorCodes.HttpFailure;
                    IReadOnlyDictionary<string, string> errorArguments = response.IsSuccessStatusCode
                        ? null
                        : new Dictionary<string, string>
                        {
                            ["status"] = ((int)response.StatusCode).ToString(System.Globalization.CultureInfo.InvariantCulture),
                            ["reason"] = response.ReasonPhrase ?? string.Empty,
                        };
                    return new ModelCatalogExchange(
                        requestUrl,
                        (int)response.StatusCode,
                        response.ReasonPhrase,
                        responseBody,
                        string.IsNullOrEmpty(errorCode)
                            ? string.Empty
                            : ModelCatalogErrorFormatter.Format(errorCode, errorArguments),
                        cancelled: false,
                        errorCode,
                        errorArguments);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new ModelCatalogExchange(
                requestUrl,
                0,
                string.Empty,
                string.Empty,
                "模型列表请求已取消。",
                cancelled: true,
                ModelCatalogErrorCodes.Cancelled,
                null);
        }
        catch (Exception exception)
        {
            return new ModelCatalogExchange(
                requestUrl,
                0,
                string.Empty,
                string.Empty,
                ModelCatalogErrorFormatter.Format(
                    ModelCatalogErrorCodes.TransportFailed,
                    new Dictionary<string, string> { ["exceptionType"] = exception.GetType().Name }),
                cancelled: false,
                ModelCatalogErrorCodes.TransportFailed,
                new Dictionary<string, string> { ["exceptionType"] = exception.GetType().Name });
        }
    }
}
