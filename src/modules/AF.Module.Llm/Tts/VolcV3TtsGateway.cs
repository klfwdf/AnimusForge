using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Adapters;

/// <summary>V3 HTTP chunked JSON synthesis, buffered before the existing playback pipeline.</summary>
public sealed class VolcV3TtsGateway : ITtsGateway
{
    // AF client safety budgets, not provider quotas. Full buffering is intentional for current playback/lipsync.
    private const int MaxWireBytes = 32 * 1024 * 1024;
    private const int MaxAudioBytes = 16 * 1024 * 1024;
    private readonly HttpClient _httpClient;
    public VolcV3TtsGateway(HttpClient httpClient) => _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<TtsSynthesisResult> SynthesizeAsync(TtsSynthesisRequest request, string credentialToken, CancellationToken cancellationToken)
    {
        string logId = string.Empty;
        int? status = null;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        TtsSynthesisResult Fail(string code) => new TtsSynthesisResult(false, null, status, code, logId);
        try
        {
            deadline.Token.ThrowIfCancellationRequested();
            if (request == null || !SafeHeader(credentialToken) || !SafeHeader(request.ResourceId) || string.IsNullOrWhiteSpace(request.VoiceId))
                return Fail("tts_configuration_incomplete");
            if (VolcTtsGateway.GetVersion(request.Endpoint) != 3) return Fail("tts_endpoint_unsupported");
            if (request.Encoding != "pcm" && request.Encoding != "wav") return Fail("tts_audio_format_unsupported");
            if (float.IsNaN(request.SpeedRatio) || request.SpeedRatio < 0.5f || request.SpeedRatio > 2f) return Fail("tts_v3_speed_invalid");
            if (float.IsNaN(request.LoudnessRatio) || request.LoudnessRatio < 0.5f || request.LoudnessRatio > 2f) return Fail("tts_v3_loudness_invalid");
            if (!new[] { 8000, 16000, 22050, 24000, 32000, 44100, 48000 }.Contains(request.SampleRate)) return Fail("tts_v3_sample_rate_invalid");
            if (string.IsNullOrWhiteSpace(request.Text) || Encoding.UTF8.GetByteCount(request.Text) > 64 * 1024) return Fail("tts_v3_text_invalid");
            string additions;
            try { additions = JObject.Parse(string.IsNullOrWhiteSpace(request.ExtraParametersJson) ? "{}" : request.ExtraParametersJson).ToString(Formatting.None); }
            catch (JsonException) { return Fail("tts_extra_parameters_invalid"); }
            var payload = new JObject
            {
                ["user"] = new JObject { ["uid"] = "animusforge" },
                ["req_params"] = new JObject
                {
                    ["text"] = request.Text,
                    ["speaker"] = request.VoiceId,
                    ["audio_params"] = new JObject
                    {
                        ["format"] = "pcm",
                        ["sample_rate"] = request.SampleRate,
                        ["speech_rate"] = (int)Math.Round((request.SpeedRatio - 1.0) * 100),
                        ["loudness_rate"] = (int)Math.Round((request.LoudnessRatio - 1.0) * 100)
                    },
                    ["additions"] = additions
                }
            };
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, request.Endpoint);
            httpRequest.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
            httpRequest.Headers.Add("X-Api-Key", credentialToken.Trim());
            httpRequest.Headers.Add("X-Api-Resource-Id", request.ResourceId.Trim());
            httpRequest.Headers.Add("X-Api-Request-Id", Guid.NewGuid().ToString());
            using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
            status = (int)response.StatusCode;
            if (response.Headers.TryGetValues("X-Tt-Logid", out var ids))
                logId = new string((ids.FirstOrDefault() ?? "").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').Take(128).ToArray());
            if (!response.IsSuccessStatusCode) return Fail("tts_http_" + status);
            if (response.Content.Headers.ContentLength > MaxWireBytes) return Fail("tts_response_too_large");
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            // Also abort blocked reads on .NET Framework handlers that don't promptly honor ReadAsync's token.
            using var cancellation = deadline.Token.Register(() => { try { stream.Dispose(); } catch (ObjectDisposedException) { } });
            using var bounded = new BoundedReadStream(stream, MaxWireBytes);
            using var textReader = new StreamReader(bounded, new UTF8Encoding(false, true));
            using var reader = new JsonTextReader(textReader) { SupportMultipleContent = true, MaxDepth = 32, DateParseHandling = DateParseHandling.None };
            using var audio = new MemoryStream();
            // JSON boundaries, not TCP chunks or lines: tolerate split UTF-8 and pretty-printed JSON.
            while (await reader.ReadAsync(deadline.Token).ConfigureAwait(false))
            {
                if (reader.TokenType != JsonToken.StartObject) return Fail("tts_response_invalid_json");
                JObject frame = await JObject.LoadAsync(reader, deadline.Token).ConfigureAwait(false);
                if (frame["code"]?.Type != JTokenType.Integer) return Fail("tts_response_invalid_code");
                long code = (long)frame["code"];
                if (code != 0 && code != 20000000) return Fail("tts_provider_code_" + code);
                var data = frame["data"];
                if (data == null || data.Type == JTokenType.Null) continue; // Usage/sentence-only frames.
                if (data.Type != JTokenType.String) return Fail("tts_audio_base64_invalid");
                byte[] chunk;
                try { chunk = Convert.FromBase64String((string)data); }
                catch (FormatException) { return Fail("tts_audio_base64_invalid"); }
                if (audio.Length + chunk.Length > MaxAudioBytes) return Fail("tts_response_too_large");
                audio.Write(chunk, 0, chunk.Length);
            }
            // A normally framed HTTP EOF is valid; don't invent a mandatory SSE [DONE] marker.
            deadline.Token.ThrowIfCancellationRequested();
            if (audio.Length == 0) return Fail("tts_audio_empty");
            if ((audio.Length & 1) != 0) return Fail("tts_audio_pcm_invalid");
            byte[] pcm = audio.ToArray();
            byte[] result = request.Encoding == "wav" ? WrapWav(pcm, request.SampleRate) : pcm;
            return new TtsSynthesisResult(true, result, status, string.Empty, logId);
        }
        catch (Exception) when (deadline.IsCancellationRequested)
        {
            return Fail(cancellationToken.IsCancellationRequested ? "tts_cancelled" : "tts_timeout");
        }
        catch (ResponseLimitException) { return Fail("tts_response_too_large"); }
        catch (JsonException) { return Fail("tts_response_invalid_json"); }
        catch (DecoderFallbackException) { return Fail("tts_response_invalid_utf8"); }
        catch (Exception exception) { return Fail("tts_gateway_" + exception.GetType().Name); }
    }

    private static bool SafeHeader(string value) => !string.IsNullOrWhiteSpace(value) && value.IndexOfAny(new[] { '\r', '\n' }) < 0;

    private static byte[] WrapWav(byte[] pcm, int sampleRate)
    {
        using var output = new MemoryStream(44 + pcm.Length);
        using var writer = new BinaryWriter(output, Encoding.ASCII, true);
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + pcm.Length);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
        writer.Write((short)1); writer.Write((short)1); writer.Write(sampleRate);
        writer.Write(sampleRate * 2); writer.Write((short)2); writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(pcm.Length); writer.Write(pcm);
        return output.ToArray();
    }

    private sealed class ResponseLimitException : IOException { }

    private sealed class BoundedReadStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _limit;
        private long _read;
        public BoundedReadStream(Stream inner, long limit) { _inner = inner; _limit = limit; }
        private int Count(int count) { _read += count; if (_read > _limit) throw new ResponseLimitException(); return count; }
        public override int Read(byte[] buffer, int offset, int count) => Count(_inner.Read(buffer, offset, count));
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) => Count(await _inner.ReadAsync(buffer, offset, count, token).ConfigureAwait(false));
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
