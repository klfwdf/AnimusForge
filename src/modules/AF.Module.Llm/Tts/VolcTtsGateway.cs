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
    public VolcTtsGateway(HttpClient httpClient) : this(httpClient, httpClient) { }
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
        if (path.Equals("/api/v3/tts/unidirectional", StringComparison.OrdinalIgnoreCase)) return 3;
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
            case "tts_endpoint_unsupported": return "API 地址无效或协议不支持；V3 请使用 /api/v3/tts/unidirectional（非 SSE/WS）。";
            case "tts_configuration_incomplete": return "请检查凭据、Resource ID 和音色；V1 还需要 AppID。";
            case "tts_extra_parameters_invalid": return "附加参数不是有效 JSON；V3 要求 JSON 对象。";
            case "tts_v3_speed_invalid": return "V3 语速须为 0.5–2.0 倍，未自动修改您的设置。";
            case "tts_v3_sample_rate_invalid": return "V3 采样率须为 8000、16000、22050、24000、32000、44100 或 48000。";
            case "tts_audio_format_unsupported": return "播放器只支持 wav 或 pcm。";
            case "tts_v3_text_invalid": return "V3 文本为空或超过 AF 单次 64 KiB UTF-8 限制。";
            case "tts_v3_loudness_invalid": return "V3 云端音量倍率须为 0.5–2.0。";
            case "tts_timeout": return "合成超时（30 秒）；未自动重试。";
            case "tts_cancelled": return "语音请求已取消。";
            case "tts_http_401": case "tts_http_403": return "鉴权失败；V3 请填写新控制台 API Key，并检查资源与音色授权。";
            case "tts_http_429": return "请求受限；请检查额度/并发，未自动重试。";
            case "tts_response_too_large": return "响应超过 AF 音频/传输大小限制。";
            default: return "合成失败（" + (code ?? "unknown") + "），请查看 TtsEngine 日志；未自动重试。";
        }
    }
}
