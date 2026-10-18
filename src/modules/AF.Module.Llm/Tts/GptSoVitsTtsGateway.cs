using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

internal sealed class GptSoVitsSynthesisRequest
{
    internal Uri Endpoint { get; }
    internal string Text { get; }
    internal GptSoVitsVoice Voice { get; }
    internal float Speed { get; }
    internal string ManagedInstance { get; }

    internal GptSoVitsSynthesisRequest WithTone(LocalTtsTone tone) => new GptSoVitsSynthesisRequest(Endpoint.AbsoluteUri, Text, Voice.ForMood((int)tone), Speed, ManagedInstance);

    internal GptSoVitsSynthesisRequest WithText(string text) => new GptSoVitsSynthesisRequest(Endpoint.AbsoluteUri, text, Voice, Speed, ManagedInstance);

    internal GptSoVitsSynthesisRequest(string endpoint, string text, GptSoVitsVoice voice, float speed, string managedInstance = null)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || !uri.IsLoopback || uri.Scheme != "http" ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException("local_gptsovits_requires_loopback_http_endpoint");
        if (string.IsNullOrWhiteSpace(text) || text.Length > 500)
            throw new ArgumentException("local_speech_text_empty_or_over_500_characters");
        if (voice == null) throw new ArgumentNullException(nameof(voice));
        Endpoint = uri; Text = text; Voice = voice; ManagedInstance = managedInstance;
        Speed = float.IsNaN(speed) || float.IsInfinity(speed) ? 1f : Math.Max(0.5f, Math.Min(2f, speed));
    }
}

// One detached HTTP attempt. No credentials, model loading, game objects or paid-provider fallback.
internal sealed class GptSoVitsTtsGateway
{
    private readonly HttpClient _client;
    internal const int MaximumAudioBytes = 32 * 1024 * 1024;
    internal GptSoVitsTtsGateway(HttpClient client) => _client = client ?? throw new ArgumentNullException(nameof(client));

    internal async Task<TtsSynthesisResult> SynthesizeAsync(GptSoVitsSynthesisRequest input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            input.Voice.VerifyReference();
            var payload = new JObject
            {
                ["text"] = input.Text, ["text_lang"] = "auto",
                ["ref_audio_path"] = input.Voice.ReferencePath,
                ["prompt_lang"] = input.Voice.PromptLanguage, ["prompt_text"] = input.Voice.PromptText,
                ["media_type"] = "wav", ["streaming_mode"] = false, ["batch_size"] = 1,
                ["text_split_method"] = "cut5", ["speed_factor"] = input.Speed,
                ["parallel_infer"] = true, ["use_cuda_graph"] = false
            };
            using (var request = new HttpRequestMessage(HttpMethod.Post, input.Endpoint))
            {
                if (!string.IsNullOrEmpty(input.ManagedInstance)) request.Headers.Add("X-AF-Instance", input.ManagedInstance);
                request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                using (var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                        return new TtsSynthesisResult(false, null, (int)response.StatusCode, "gpt_http_" + (int)response.StatusCode);
                    if (response.Content.Headers.ContentLength > MaximumAudioBytes)
                        return new TtsSynthesisResult(false, null, (int)response.StatusCode, "gpt_audio_too_large");
                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var output = new MemoryStream())
                    {
                        var buffer = new byte[16384];
                        int read;
                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
                        {
                            if (output.Length + read > MaximumAudioBytes)
                                return new TtsSynthesisResult(false, null, (int)response.StatusCode, "gpt_audio_too_large");
                            output.Write(buffer, 0, read);
                        }
                        cancellationToken.ThrowIfCancellationRequested();
                        byte[] audio = output.ToArray();
                        return IsMonoPcmWave(audio)
                            ? new TtsSynthesisResult(true, audio, (int)response.StatusCode, "")
                            : new TtsSynthesisResult(false, null, (int)response.StatusCode, "gpt_invalid_wav");
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { return new TtsSynthesisResult(false, null, null, "gpt_timeout"); }
        catch (Exception ex) { return new TtsSynthesisResult(false, null, null, "gpt_" + ex.GetType().Name); }
    }

    internal static bool IsMonoPcmWave(byte[] audio)
    {
        if (audio == null || audio.Length < 44 || Encoding.ASCII.GetString(audio, 0, 4) != "RIFF" || Encoding.ASCII.GetString(audio, 8, 4) != "WAVE") return false;
        bool formatFound = false, dataFound = false;
        for (int offset = 12; offset <= audio.Length - 8;)
        {
            int size = BitConverter.ToInt32(audio, offset + 4);
            if (size < 0 || size > audio.Length - offset - 8) return false;
            string chunk = Encoding.ASCII.GetString(audio, offset, 4);
            if (chunk == "fmt ")
            {
                if (size < 16 || BitConverter.ToUInt16(audio, offset + 8) != 1 ||
                    BitConverter.ToUInt16(audio, offset + 10) != 1 || BitConverter.ToUInt16(audio, offset + 22) != 16) return false;
                int rate = BitConverter.ToInt32(audio, offset + 12);
                if (rate < 8000 || rate > 96000) return false;
                formatFound = true;
            }
            if (chunk == "data") dataFound = size > 0 && size % 2 == 0;
            offset += 8 + size + (size & 1);
        }
        return formatFound && dataFound;
    }
}
