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
    internal string Body { get; }
    internal int? RetryAfterSeconds { get; }
    internal bool Discarded { get; }
    internal bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode <= 299;

    internal LlmNonStreamingResponse(HttpStatusCode statusCode, string body, int? retryAfterSeconds, bool discarded)
    { StatusCode = statusCode; Body = body; RetryAfterSeconds = retryAfterSeconds; Discarded = discarded; }
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
                // The primary channel keeps its exact post-await save-generation checks;
                // optional/domain callers need no Bannerlord dependency in this owner.
                if (acceptResponse != null && !acceptResponse(response.StatusCode))
                    return new LlmNonStreamingResponse(response.StatusCode, "", null, true);
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (acceptBody != null && !acceptBody())
                    return new LlmNonStreamingResponse(response.StatusCode, "", null, true);
                return new LlmNonStreamingResponse(response.StatusCode, body, GetRetryAfterSeconds(response), false);
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
            if (response?.Headers != null && response.Headers.TryGetValues("Retry-After", out IEnumerable<string> values))
                foreach (string value in values)
                    if (int.TryParse((value ?? string.Empty).Trim(), out int seconds))
                        return Math.Max(0, seconds);
        }
        catch
        {
            // Preserve the previous best-effort metadata policy; never retry a send.
        }
        return null;
    }
}
