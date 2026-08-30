using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Dedicated Volcengine V1 TTS transport. Provider payload and headers stay
/// here; playback, audio parsing and queue lifecycle stay in TtsEngine.
/// </summary>
public sealed class LegacyVolcTtsGateway : ITtsGateway
{
    private readonly HttpClient _httpClient;

    public LegacyVolcTtsGateway(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<TtsSynthesisResult> SynthesizeAsync(TtsSynthesisRequest request, string credentialToken, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(credentialToken))
        {
            return new TtsSynthesisResult(false, null, null, "tts_configuration_incomplete");
        }
        string extra = NormalizeExtraParameters(request.ExtraParametersJson);
        if (extra == null)
        {
            return new TtsSynthesisResult(false, null, null, "tts_extra_parameters_invalid");
        }
        string requestId = Guid.NewGuid().ToString();
        JObject payload = new JObject
        {
            ["app"] = new JObject { ["appid"] = request.AppId, ["token"] = "token", ["cluster"] = "volcano_tts" },
            ["user"] = new JObject { ["uid"] = "animusforge" },
            ["audio"] = new JObject
            {
                ["voice_type"] = request.VoiceId,
                ["encoding"] = request.Encoding,
                ["speed_ratio"] = Math.Round(request.SpeedRatio, 2),
                ["rate"] = request.SampleRate,
                ["loudness_ratio"] = Math.Round(request.LoudnessRatio, 2)
            },
            ["request"] = new JObject
            {
                ["reqid"] = requestId,
                ["text"] = request.Text ?? string.Empty,
                ["operation"] = "query",
                ["extra_param"] = extra
            }
        };
        using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, request.Endpoint);
        try
        {
            httpRequest.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
            httpRequest.Headers.TryAddWithoutValidation("Authorization", "Bearer;" + credentialToken.Trim());
            httpRequest.Headers.TryAddWithoutValidation("X-Api-App-Id", request.AppId.Trim());
            // Preserve the legacy V1 header mapping for compatibility.
            httpRequest.Headers.TryAddWithoutValidation("X-Api-App-Key", request.AppId.Trim());
            httpRequest.Headers.TryAddWithoutValidation("X-Api-Access-Key", credentialToken.Trim());
            httpRequest.Headers.TryAddWithoutValidation("X-Api-Resource-Id", request.ResourceId.Trim());
            httpRequest.Headers.TryAddWithoutValidation("X-Api-Request-Id", requestId);
            using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            string responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false) ?? string.Empty;
            int statusCode = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                return new TtsSynthesisResult(false, null, statusCode, "tts_http_" + statusCode);
            }
            JObject result;
            try { result = JObject.Parse(responseText); }
            catch { return new TtsSynthesisResult(false, null, statusCode, "tts_response_invalid_json"); }
            int code = (int?)result["code"] ?? -1;
            if (code != 3000)
            {
                return new TtsSynthesisResult(false, null, statusCode, "tts_provider_code_" + code);
            }
            string encodedAudio = result["data"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(encodedAudio))
            {
                return new TtsSynthesisResult(false, null, statusCode, "tts_audio_empty");
            }
            try { return new TtsSynthesisResult(true, Convert.FromBase64String(encodedAudio.Trim()), statusCode, string.Empty); }
            catch { return new TtsSynthesisResult(false, null, statusCode, "tts_audio_base64_invalid"); }
        }
        catch (OperationCanceledException)
        {
            return new TtsSynthesisResult(false, null, null, "tts_cancelled");
        }
        catch (Exception exception)
        {
            return new TtsSynthesisResult(false, null, null, "tts_gateway_" + exception.GetType().Name);
        }
    }

    private static string NormalizeExtraParameters(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "{}";
        try { return JToken.Parse(json).ToString(Formatting.None); }
        catch { return null; }
    }
}
