using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

/// <summary>One endpoint, one protocol, one attempt. Never retry using another credential scheme.</summary>
public sealed class VolcTtsGateway : ITtsGateway
{
    private readonly HttpClient _httpClient;
    private readonly HttpClient _v3HttpClient;
    public VolcTtsGateway(HttpClient httpClient, HttpClient v3HttpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _v3HttpClient = v3HttpClient ?? throw new ArgumentNullException(nameof(v3HttpClient));
    }

    // Custom V1-compatible proxy URLs remain supported. Other V3 protocols must not receive a V1 body.
    public static int GetVersion(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || !string.IsNullOrEmpty(uri.UserInfo)) return 0;
        string path = uri.AbsolutePath.TrimEnd('/');
        if (path.Equals("/api/v3/tts/unidirectional", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/v3/plan/tts/unidirectional", StringComparison.OrdinalIgnoreCase)) return 3;
        if (path.StartsWith("/api/v3", StringComparison.OrdinalIgnoreCase)) return 0;
        return 1;
    }

    public Task<TtsSynthesisResult> SynthesizeAsync(TtsSynthesisRequest request, string credentialToken, CancellationToken cancellationToken)
    {
        int version = GetVersion(request?.Endpoint);
        if (version == 0) return Task.FromResult(new TtsSynthesisResult(false, null, null, "tts_endpoint_unsupported"));
        ITtsGateway gateway = version == 3 ? (ITtsGateway)new VolcV3TtsGateway(_v3HttpClient) : new LegacyVolcTtsGateway(_httpClient);
        return gateway.SynthesizeAsync(request, credentialToken, cancellationToken);
    }

    public static string DescribeError(string code)
    {
        switch (code)
        {
            case "tts_endpoint_unsupported": return "Invalid API URL or unsupported protocol; V3 requires /api/v3/tts/unidirectional and Agent Plan requires /api/v3/plan/tts/unidirectional (HTTP, not SSE/WS).";
            case "tts_configuration_incomplete": return "Check the credential, resource ID, and voice; V1 also requires an App ID.";
            case "tts_extra_parameters_invalid": return "Extra parameters must be valid JSON; V3 requires a JSON object.";
            case "tts_v3_speed_invalid": return "V3 speed must be 0.5–2.0 times normal; the setting was not changed automatically.";
            case "tts_v3_sample_rate_invalid": return "V3 sample rate must be 8000, 16000, 22050, 24000, 32000, 44100, or 48000 Hz.";
            case "tts_audio_format_unsupported": return "The player supports wav or pcm only.";
            case "tts_v3_text_invalid": return "V3 text is empty or exceeds the AF limit of 64 KiB UTF-8 per request.";
            case "tts_v3_loudness_invalid": return "V3 cloud volume must be 0.5–2.0 times normal.";
            case "tts_timeout": return "Synthesis timed out after 30 seconds; no automatic retry.";
            case "tts_cancelled": return "The voice request was cancelled.";
            case "tts_http_401": return "Authentication failed; V3 requires the matching service API key, Agent Plan requires its dedicated API key, and resource/voice authorization must match.";
            case "tts_http_403": return "Authentication failed; V3 requires the matching service API key, Agent Plan requires its dedicated API key, and resource/voice authorization must match.";
            case "tts_http_429": return "Rate limited; check quota/concurrency. No automatic retry.";
            case "tts_response_too_large": return "The response exceeded the AF audio or transfer size limit.";
            default: return "Synthesis failed (" + (code ?? "unknown") + "); check the TtsEngine log. No automatic retry.";
        }
    }
}
