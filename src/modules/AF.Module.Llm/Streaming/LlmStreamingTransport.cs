using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed class LlmStreamingDelta
{
    internal int ReadSequence { get; set; }
    internal int ContentSequence { get; set; }
    internal string Data { get; set; }
    internal string Content { get; set; }
    internal string Reasoning { get; set; }
}

internal sealed class LlmStreamingResponse
{
    internal HttpStatusCode StatusCode { get; set; }
    internal string ErrorBody { get; set; } = "";
    internal string RawStreamSample { get; set; } = "";
    internal string Content { get; set; } = "";
    internal string Reasoning { get; set; } = "";
    internal int? RetryAfterSeconds { get; set; }
    internal bool ParseFailure { get; set; }
    internal bool Discarded { get; set; }
    internal bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode <= 299;
}

// Owns one SSE HTTP attempt and all resources created for that attempt. Retry,
// visible-text filtering and player-facing errors remain with the calling policy.
internal static class LlmStreamingTransport
{
    internal const string PartialRawDataKey = "af.llm.streaming.partial_raw";

    internal static async Task<LlmStreamingResponse> SendAsync(
        string endpoint,
        string apiKey,
        string preparedJson,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sender,
        CancellationToken cancellationToken,
        Func<HttpStatusCode, bool> acceptResponse = null,
        Func<bool> acceptErrorBody = null,
        Action<int> onReadStarted = null,
        Func<int, string, bool> acceptLine = null,
        Action<LlmStreamingDelta> onDelta = null,
        Action<string, string> onParseError = null,
        bool connectionClose = false,
        bool throwOnCancellationBeforeRead = true,
        int rawSampleMaxChars = 12000,
        bool includeDataPrefix = true)
    {
        StringBuilder raw = new StringBuilder();
        StringBuilder content = new StringBuilder();
        StringBuilder reasoning = new StringBuilder();
        bool parseFailure = false;
        int contentSequence = 0;
        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, endpoint))
        {
            LlmApiCompat.ApplyAuthenticationHeaders(request, endpoint, apiKey);
            request.Headers.ConnectionClose = connectionClose;
            request.Content = new StringContent(preparedJson, Encoding.UTF8, "application/json");
            try
            {
                using (HttpResponseMessage response = await sender(request, cancellationToken).ConfigureAwait(false))
                {
                    if (acceptResponse != null && !acceptResponse(response.StatusCode))
                        return new LlmStreamingResponse { StatusCode = response.StatusCode, Discarded = true };
                    if (!response.IsSuccessStatusCode)
                    {
                        string errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (acceptErrorBody != null && !acceptErrorBody())
                            return new LlmStreamingResponse { StatusCode = response.StatusCode, Discarded = true };
                        return new LlmStreamingResponse
                        {
                            StatusCode = response.StatusCode,
                            ErrorBody = errorBody ?? "",
                            RetryAfterSeconds = LlmNonStreamingTransport.GetRetryAfterSeconds(response)
                        };
                    }

                    using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        int readSequence = 0;
                        while (true)
                        {
                            if (throwOnCancellationBeforeRead) cancellationToken.ThrowIfCancellationRequested();
                            readSequence++;
                            onReadStarted?.Invoke(readSequence);
                            string line = await reader.ReadLineAsync().ConfigureAwait(false);
                            if (acceptLine != null && !acceptLine(readSequence, line))
                                return Snapshot(response.StatusCode, raw, content, reasoning, parseFailure, discarded: true);
                            if (line == null || (!throwOnCancellationBeforeRead && cancellationToken.IsCancellationRequested)) break;
                            string trimmed = line.Trim();
                            if (string.IsNullOrEmpty(trimmed) || !trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;
                            string data = trimmed.Substring(5).Trim();
                            if (string.Equals(data, "[DONE]", StringComparison.OrdinalIgnoreCase)) break;
                            if (string.IsNullOrWhiteSpace(data)) continue;
                            AppendRaw(raw, includeDataPrefix ? "data: " + data : data, rawSampleMaxChars);
                            try
                            {
                                JObject json = JObject.Parse(data);
                                string reasoningDelta = LlmApiCompat.ExtractStreamReasoningText(json) ?? "";
                                string contentDelta = LlmApiCompat.ExtractStreamDeltaText(json) ?? "";
                                if (!string.IsNullOrEmpty(reasoningDelta)) reasoning.Append(reasoningDelta);
                                if (!string.IsNullOrEmpty(contentDelta))
                                {
                                    content.Append(contentDelta);
                                    contentSequence++;
                                }
                                try
                                {
                                    onDelta?.Invoke(new LlmStreamingDelta
                                    {
                                        ReadSequence = readSequence,
                                        ContentSequence = contentSequence,
                                        Data = data,
                                        Content = contentDelta,
                                        Reasoning = reasoningDelta
                                    });
                                }
                                catch
                                {
                                    // Delta observers are non-authoritative.
                                }
                            }
                            catch (Exception parseError)
                            {
                                parseFailure = true;
                                try { onParseError?.Invoke(parseError.Message, data); }
                                catch { }
                            }
                        }
                    }
                    return Snapshot(response.StatusCode, raw, content, reasoning, parseFailure, discarded: false);
                }
            }
            catch (Exception error)
            {
                if (raw.Length > 0) error.Data[PartialRawDataKey] = raw.ToString();
                throw;
            }
        }
    }

    private static LlmStreamingResponse Snapshot(HttpStatusCode statusCode, StringBuilder raw,
        StringBuilder content, StringBuilder reasoning, bool parseFailure, bool discarded)
    {
        return new LlmStreamingResponse
        {
            StatusCode = statusCode,
            RawStreamSample = raw.ToString(),
            Content = content.ToString(),
            Reasoning = reasoning.ToString(),
            ParseFailure = parseFailure,
            Discarded = discarded
        };
    }

    private static void AppendRaw(StringBuilder raw, string value, int maxChars)
    {
        if (raw.Length > 0 && (maxChars < 0 || raw.Length < maxChars)) raw.AppendLine();
        if (maxChars < 0) { raw.Append(value); return; }
        if (raw.Length >= maxChars) return;
        int remaining = maxChars - raw.Length;
        raw.Append(value.Length <= remaining ? value : value.Substring(0, remaining));
    }
}
