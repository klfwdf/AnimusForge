using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Adapters;

/// <summary>One binary WAV speech request. No retries or provider fallback.</summary>
public sealed class OpenAiCompatibleTtsGateway
{
    private const int MaxAudioBytes = 32 * 1024 * 1024;
    private readonly HttpClient _client;
    public OpenAiCompatibleTtsGateway(HttpClient client) => _client = client ?? throw new ArgumentNullException(nameof(client));

    public async Task<TtsSynthesisResult> SynthesizeAsync(OpenAiTtsSynthesisRequest request, string credential, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(credential) || string.IsNullOrWhiteSpace(request.Model) || string.IsNullOrWhiteSpace(request.Voice))
            return Failed("tts_openai_configuration_incomplete");
        if (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) || !string.IsNullOrEmpty(uri.UserInfo))
            return Failed("tts_openai_endpoint_invalid");
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 4096) return Failed("tts_openai_text_invalid");
        if (float.IsNaN(request.Speed) || float.IsInfinity(request.Speed) || request.Speed < 0.25f || request.Speed > 4f)
            return Failed("tts_openai_speed_invalid");
        if (request.TimeoutSeconds < 10 || request.TimeoutSeconds > 120) return Failed("tts_openai_timeout_invalid");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));
        try
        {
            var payload = new JObject { ["model"] = request.Model.Trim(), ["input"] = request.Text,
                ["voice"] = request.Voice.Trim(), ["response_format"] = "wav", ["speed"] = request.Speed };
            using var message = new HttpRequestMessage(HttpMethod.Post, uri);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential.Trim());
            message.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
            using var response = await _client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            int status = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode) return Failed("tts_http_" + status, status);
            if (response.Content.Headers.ContentLength > MaxAudioBytes) return Failed("tts_response_too_large", status);
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var buffer = new MemoryStream();
            byte[] chunk = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(chunk, 0, chunk.Length, timeout.Token).ConfigureAwait(false)) > 0)
            {
                if (buffer.Length + count > MaxAudioBytes) return Failed("tts_response_too_large", status);
                buffer.Write(chunk, 0, count);
            }
            timeout.Token.ThrowIfCancellationRequested();
            byte[] audio = buffer.ToArray();
            string error = ValidateWave(audio);
            return error == null ? new TtsSynthesisResult(true, audio, status, "") : Failed(error, status);
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested) throw;
            return Failed("tts_openai_timeout");
        }
        catch (Exception exception) { return Failed("tts_gateway_" + exception.GetType().Name); }
    }

    private static TtsSynthesisResult Failed(string error, int? status = null) => new TtsSynthesisResult(false, null, status, error);

    // Validate before the legacy player can interpret unknown/compressed bytes as raw PCM.
    internal static string ValidateWave(byte[] data)
    {
        if (data == null || data.Length < 44 || Encoding.ASCII.GetString(data, 0, 4) != "RIFF" || Encoding.ASCII.GetString(data, 8, 4) != "WAVE")
            return "tts_openai_audio_invalid";
        bool formatSeen = false;
        int channels = 0;
        for (int offset = 12; offset <= data.Length - 8;)
        {
            uint length = BitConverter.ToUInt32(data, offset + 4);
            int start = offset + 8;
            if (length > data.Length - start) return "tts_openai_audio_invalid";
            string kind = Encoding.ASCII.GetString(data, offset, 4);
            if (kind == "fmt ")
            {
                if (formatSeen || length < 16 || BitConverter.ToUInt16(data, start) != 1 || BitConverter.ToUInt16(data, start + 14) != 16)
                    return "tts_openai_audio_invalid";
                channels = BitConverter.ToUInt16(data, start + 2);
                int rate = BitConverter.ToInt32(data, start + 4);
                if (channels < 1 || channels > 2 || rate < 8000 || rate > 48000 ||
                    BitConverter.ToUInt16(data, start + 12) != channels * 2 || BitConverter.ToInt32(data, start + 8) != rate * channels * 2)
                    return "tts_openai_audio_invalid";
                formatSeen = true;
            }
            else if (kind == "data")
            {
                if (!formatSeen || length == 0 || length % (channels * 2) != 0) return "tts_openai_audio_invalid";
                // Test the mono mix that the player actually uses (stereo phases can cancel).
                for (int i = start; i < start + (int)length; i += channels * 2)
                {
                    int value = BitConverter.ToInt16(data, i);
                    if (channels == 2) value = (value + BitConverter.ToInt16(data, i + 2)) / 2;
                    if (value != 0) return null;
                }
                return "tts_audio_silent";
            }
            long next = (long)start + length + (length & 1);
            if (next > data.Length) return "tts_openai_audio_invalid";
            offset = (int)next;
        }
        return "tts_openai_audio_invalid";
    }
}
