using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge;

// Detached HTTP data only. Credentials and live HttpResponseMessage instances never
// leave the transport; caller-specific text/error/retry policy stays with its owner.
internal sealed class LlmNonStreamingResponse
{
    internal HttpStatusCode StatusCode { get; }
    internal string ReasonPhrase { get; }
    internal string Body { get; }
    internal int? RetryAfterSeconds { get; }
    internal IReadOnlyDictionary<string, string[]> Headers { get; }
    internal bool Discarded { get; }
    internal bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode <= 299;

    internal LlmNonStreamingResponse(
        HttpStatusCode statusCode,
        string reasonPhrase,
        string body,
        int? retryAfterSeconds,
        IReadOnlyDictionary<string, string[]> headers,
        bool discarded)
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase ?? string.Empty;
        Body = body ?? string.Empty;
        RetryAfterSeconds = retryAfterSeconds;
        Headers = headers ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        Discarded = discarded;
    }
}

internal static class LlmNonStreamingTransport
{
    // One invocation is exactly one attempt. Thinking/empty/user-requested retries
    // must be explicitly requested by their existing policy owner, never hidden here.
    internal static async Task<LlmNonStreamingResponse> SendAsync(
        string endpoint, string apiKey, string preparedJson,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sender,
        CancellationToken cancellationToken,
        Func<HttpStatusCode, bool> acceptResponse = null, Func<bool> acceptBody = null)
    {
        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, endpoint))
        {
            LlmApiCompat.ApplyAuthenticationHeaders(request, endpoint, apiKey);
            request.Content = new StringContent(preparedJson, Encoding.UTF8, "application/json");
            using (HttpResponseMessage response = await sender(request, cancellationToken).ConfigureAwait(false))
            {
                int? retryAfterSeconds = GetRetryAfterSeconds(response);
                IReadOnlyDictionary<string, string[]> headers = SnapshotHeaders(response);
                // The primary channel keeps its exact post-await save-generation checks;
                // optional/domain callers need no Bannerlord dependency in this owner.
                if (acceptResponse != null && !acceptResponse(response.StatusCode))
                    return new LlmNonStreamingResponse(response.StatusCode, response.ReasonPhrase, "", retryAfterSeconds, headers, true);
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (acceptBody != null && !acceptBody())
                    return new LlmNonStreamingResponse(response.StatusCode, response.ReasonPhrase, "", retryAfterSeconds, headers, true);
                return new LlmNonStreamingResponse(response.StatusCode, response.ReasonPhrase, body, retryAfterSeconds, headers, false);
            }
        }
    }

    // Existing configured-Gateway timeout semantics: one budget covers both attempts,
    // and disposing this linked source never cancels/disposes the caller's source.
    internal static CancellationTokenSource CreateTimeout(int timeoutMilliseconds, CancellationToken callerToken)
    {
        CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        if (timeoutMilliseconds > 0) linked.CancelAfter(timeoutMilliseconds);
        return linked;
    }

    internal static int? GetRetryAfterSeconds(HttpResponseMessage response)
    {
        try
        {
            if (response?.Headers?.RetryAfter?.Delta != null)
                return Math.Max(0, (int)Math.Ceiling(response.Headers.RetryAfter.Delta.Value.TotalSeconds));
            if (response?.Headers?.RetryAfter?.Date != null)
                return Math.Max(0, (int)Math.Ceiling((response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow).TotalSeconds));
            if (response?.Headers != null && response.Headers.TryGetValues("Retry-After", out IEnumerable<string> values))
                foreach (string value in values)
                {
                    string text = (value ?? string.Empty).Trim();
                    if (int.TryParse(text, out int seconds))
                        return Math.Max(0, seconds);
                    if (DateTimeOffset.TryParse(text, out DateTimeOffset retryAt))
                        return Math.Max(0, (int)Math.Ceiling((retryAt - DateTimeOffset.UtcNow).TotalSeconds));
                }
        }
        catch
        {
            // Preserve the previous best-effort metadata policy; never retry a send.
        }
        return null;
    }

    private static IReadOnlyDictionary<string, string[]> SnapshotHeaders(HttpResponseMessage response)
    {
        Dictionary<string, string[]> headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (response?.Headers == null) return headers;
        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
            headers[header.Key ?? string.Empty] = header.Value == null ? Array.Empty<string>() : new List<string>(header.Value).ToArray();
        return headers;
    }
}
