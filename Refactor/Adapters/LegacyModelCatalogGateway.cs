using System;
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
    {
        RequestUrl = requestUrl ?? string.Empty;
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase ?? string.Empty;
        ResponseBody = responseBody ?? string.Empty;
        ErrorMessage = errorMessage ?? string.Empty;
        Cancelled = cancelled;
    }

    public string RequestUrl { get; }
    public int StatusCode { get; }
    public bool HasStatusCode => StatusCode > 0;
    public bool IsSuccessStatusCode => StatusCode >= 200 && StatusCode <= 299;
    public string ReasonPhrase { get; }
    public string ResponseBody { get; }
    public string ErrorMessage { get; }
    public bool Cancelled { get; }
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
                "API 地址为空，无法访问模型列表。",
                cancelled: false);
        }
        if (includeAuthentication && string.IsNullOrWhiteSpace(apiKey))
        {
            return new ModelCatalogExchange(
                requestUrl,
                0,
                string.Empty,
                string.Empty,
                "API Key 为空，无法拉取模型列表。",
                cancelled: false);
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
                    return new ModelCatalogExchange(
                        requestUrl,
                        (int)response.StatusCode,
                        response.ReasonPhrase,
                        responseBody,
                        string.Empty,
                        cancelled: false);
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
                cancelled: true);
        }
        catch (Exception exception)
        {
            return new ModelCatalogExchange(
                requestUrl,
                0,
                string.Empty,
                string.Empty,
                exception.Message,
                cancelled: false);
        }
    }
}
