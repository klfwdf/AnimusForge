using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

internal static class OpenAiCases
{
    private static int _checks;
    private static void Assert(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); _checks++; }

    public static async Task Run(Assembly assembly)
    {
        Type requestType = assembly.GetType("AnimusForge.Refactor.Contracts.OpenAiTtsSynthesisRequest", true);
        Type gatewayType = assembly.GetType("AnimusForge.Refactor.Adapters.OpenAiCompatibleTtsGateway", true);
        object Request(string url = "https://fixture.invalid/v1/audio/speech", float speed = 1f, int timeout = 60, string text = "你好，旅行者。")
            => Activator.CreateInstance(requestType, url, "gpt-sovits-v4", "zh_default", text, speed, timeout);
        async Task<(bool Ok, byte[] Audio, string Error)> Send(HttpClient client, object request, CancellationToken cancel = default, string key = "fixture-openai-secret")
        {
            object gateway = Activator.CreateInstance(gatewayType, client);
            var task = (Task)gatewayType.GetMethod("SynthesizeAsync").Invoke(gateway, new[] { request, key, cancel });
            await task;
            object result = task.GetType().GetProperty("Result").GetValue(task);
            object Get(string name) => result.GetType().GetProperty(name).GetValue(result);
            return ((bool)Get("Success"), (byte[])Get("AudioBytes"), (string)Get("ErrorCode"));
        }

        using (var handler = new Handler(Wave()))
        using (var client = new HttpClient(handler))
        {
            var result = await Send(client, Request());
            Assert(result.Ok && result.Audio.SequenceEqual(Wave()), "Binary WAV success");
            using (var body = JsonDocument.Parse(handler.Body)) Assert(body.RootElement.GetProperty("stream").ValueKind == JsonValueKind.False, "Buffered TTS explicitly requests stream=false");
            Assert(handler.Calls == 1 && handler.Url == "https://fixture.invalid/v1/audio/speech", "One exact endpoint attempt");
            Assert(handler.Authorization == "Bearer fixture-openai-secret", "Standard bearer authentication");
            using var payload = JsonDocument.Parse(handler.Body);
            Assert(payload.RootElement.GetProperty("model").GetString() == "gpt-sovits-v4" &&
                payload.RootElement.GetProperty("voice").GetString() == "zh_default" &&
                payload.RootElement.GetProperty("response_format").GetString() == "wav" &&
                payload.RootElement.GetProperty("input").GetString() == "你好，旅行者。", "Exact payload fields");
            Assert(!handler.Body.Contains("fixture-openai-secret") && !handler.Body.Contains("appid") && !handler.Body.Contains("resource"), "Credential/provider boundary");
            Assert(handler.Content.Disposed, "Success response disposed");
        }
        foreach (int status in new[] { 401, 403, 429, 504, 302 })
        {
            using var handler = new Handler(Encoding.UTF8.GetBytes("error"), status);
            using var client = new HttpClient(handler);
            var result = await Send(client, Request());
            Assert(!result.Ok && result.Error == "tts_http_" + status && handler.Calls == 1 && handler.Content.Disposed, "HTTP failure/disposal/no retry " + status);
        }
        foreach (var pair in new[] {
            (Encoding.UTF8.GetBytes("ID3-not-a-wave"), "tts_openai_audio_invalid"),
            (Encoding.UTF8.GetBytes("{\"error\":\"still HTTP 200\"}"), "tts_openai_audio_invalid"),
            (Wave(silent:true), "tts_audio_silent"),
            (Wave(antiPhase:true), "tts_audio_silent"),
            (Wave().Take(46).ToArray(), "tts_openai_audio_invalid") })
        {
            using var handler = new Handler(pair.Item1);
            using var client = new HttpClient(handler);
            var result = await Send(client, Request());
            Assert(!result.Ok && result.Error == pair.Item2 && handler.Content.Disposed, "Invalid/silent WAV " + pair.Item2);
        }
        using (var handler = new Handler(Wave(), declaredLength: 33L * 1024 * 1024))
        using (var client = new HttpClient(handler))
        {
            var result = await Send(client, Request());
            Assert(result.Error == "tts_response_too_large" && handler.Content.Disposed, "Declared size cap");
        }
        using (var handler = new Handler(new byte[32 * 1024 * 1024 + 1]))
        using (var client = new HttpClient(handler))
            Assert((await Send(client, Request())).Error == "tts_response_too_large", "Streamed size cap without Content-Length");

        using (var handler = new Handler(Wave()))
        using (var client = new HttpClient(handler))
        {
            foreach (var item in new[] {
                (Request(url:"file:///tmp/speech"), "tts_openai_endpoint_invalid"),
                (Request(speed:float.NaN), "tts_openai_speed_invalid"),
                (Request(timeout:5), "tts_openai_timeout_invalid"),
                (Request(text:new string('a',4097)), "tts_openai_text_invalid") })
                Assert((await Send(client, item.Item1)).Error == item.Item2, "Input validation " + item.Item2);
            Assert((await Send(client, Request(), key:"")).Error == "tts_openai_configuration_incomplete" && handler.Calls == 0, "Invalid input sends no request");
        }
        using (var handler = new Handler(Wave(), delay:true))
        using (var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan })
        using (var cancel = new CancellationTokenSource(80))
        {
            bool cancelled = false;
            try { await Send(client, Request(), cancel.Token); } catch (OperationCanceledException) { cancelled = true; }
            Assert(cancelled && handler.Calls == 1, "Caller cancellation is propagated");
        }
        using (var handler = new Handler(Wave(), delay:true))
        using (var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan })
            Assert((await Send(client, Request(timeout:10))).Error == "tts_openai_timeout" && handler.Calls == 1, "Owner timeout/no retry");

        using (var handler = new Handler(Wave(), slowBody:true))
        using (var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan })
        using (var cancel = new CancellationTokenSource(80))
        {
            bool cancelled = false;
            try { await Send(client, Request(), cancel.Token); } catch (OperationCanceledException) { cancelled = true; }
            Assert(cancelled && handler.Content.Disposed, "Body cancellation releases response and propagates");
        }
        using (var handler = new Handler(Wave(), slowBody:true))
        using (var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan })
            Assert((await Send(client, Request(timeout:10))).Error == "tts_openai_timeout" && handler.Content.Disposed, "Body stall shares the total deadline");

        const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        Type settingsType = assembly.GetType("AnimusForge.DuelSettings", true);
#pragma warning disable SYSLIB0050
        object settings = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(settingsType);
#pragma warning restore SYSLIB0050
        void Set(string name, object value) => settingsType.GetProperty(name).SetValue(settings, value);
        bool Enabled() => (bool)settingsType.GetProperty("IsSelectedTtsEnabled", hidden).GetValue(settings);
        Set("EnableTtsSpeech", true); Set("TtsVolcDedicatedEnabled", false);
        Assert(!Enabled(), "Legacy provider disabled preserved");
        Set("TtsVolcDedicatedEnabled", true);
        Assert(Enabled(), "Missing provider field uses legacy Volcengine");
        object dropdown = settingsType.GetMethod("BuildTtsProviderDropdown", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] {1});
        Set("TtsProviderDropdown", dropdown); Set("TtsVolcDedicatedEnabled", false);
        Assert(Enabled(), "OpenAI service does not require Volcengine enable");
        Set("TtsOpenAiApiUrl", "https://snapshot.invalid/v1/audio/speech"); Set("TtsOpenAiApiKey", "snapshot-secret");
        Set("TtsOpenAiModel", "snapshot-model"); Set("TtsOpenAiVoice", "zh_default"); Set("TtsOpenAiSpeed", 1f); Set("TtsOpenAiTimeoutSeconds", 60);
        Type snapshotType = assembly.GetType("AnimusForge.TtsEngine+ProviderSnapshot", true);
        object snapshot = Activator.CreateInstance(snapshotType, hidden, null, new[] { settings }, null);
        Set("TtsOpenAiApiUrl", "https://changed.invalid/v1/audio/speech"); Set("TtsOpenAiApiKey", "changed-secret");
        Assert((string)snapshotType.GetField("Url", hidden).GetValue(snapshot) == "https://snapshot.invalid/v1/audio/speech" &&
            (string)snapshotType.GetField("Credential", hidden).GetValue(snapshot) == "snapshot-secret", "Queued config snapshot is stable");
        Set("EnableTtsSpeech", false); Assert(!Enabled(), "Master switch disables new service");
        Console.WriteLine("PASS OpenAI-compatible production gateway/config checks=" + _checks);

        string session = Environment.GetEnvironmentVariable("AF_TTS_LIVE_SESSION");
        if (!string.IsNullOrEmpty(session))
        {
            string key = null;
            using var sessionStream = new FileStream(session, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sessionReader = new StreamReader(sessionStream);
            string line;
            while ((line = sessionReader.ReadLine()) != null)
            {
                using var doc = JsonDocument.Parse(line);
                if (!doc.RootElement.TryGetProperty("payload", out var p) || !p.TryGetProperty("role", out var role) || role.GetString() != "user") continue;
                string text = string.Join(" ", p.GetProperty("content").EnumerateArray().Select(c => c.TryGetProperty("text", out var t) ? t.GetString() : ""));
                if (!text.Contains("shenlanqaq.com")) continue;
                Match match = Regex.Match(text, "sk-[A-Za-z0-9_-]{20,}");
                if (match.Success) { key = match.Value; break; }
            }
            if (key == null) throw new InvalidOperationException("Referenced conversation credential unavailable");
            using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan };
            var start = System.Diagnostics.Stopwatch.StartNew();
            var result = await Send(client, Request("https://www.shenlanqaq.com/v1/audio/speech"), key:key);
            Assert(result.Ok, "Live production gateway: " + result.Error);
            object[] parseArgs = { result.Audio, "wav", null, 0 };
            assembly.GetType("AnimusForge.TtsEngine", true).GetMethod("ParseAudioData", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, parseArgs);
            byte[] pcm = (byte[])parseArgs[2]; int rate = (int)parseArgs[3];
            Assert(pcm.Length > 0 && pcm.Any(b => b != 0), "Live actual player parsing produces audible PCM");
            string output = Environment.GetEnvironmentVariable("AF_TTS_LIVE_OUTPUT");
            File.WriteAllBytes(Path.Combine(output, "production-gateway.wav"), result.Audio);
            var report = new { success = true, elapsedSeconds = start.Elapsed.TotalSeconds, bytes = result.Audio.Length, sampleRate = rate, durationSeconds = pcm.Length / (rate * 2.0), model = "gpt-sovits-v4", voice = "zh_default" };
            File.WriteAllText(Path.Combine(output, "production-live.json"), JsonSerializer.Serialize(report));
            Console.WriteLine("PASS live production gateway + actual player WAV parser " + JsonSerializer.Serialize(report));
        }
    }

    private static byte[] Wave(bool silent = false, bool antiPhase = false)
    {
        using var memory = new MemoryStream(); using var writer = new BinaryWriter(memory);
        int channels = antiPhase ? 2 : 1;
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + 32 * channels); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16); writer.Write((short)1); writer.Write((short)channels); writer.Write(24000); writer.Write(24000 * channels * 2); writer.Write((short)(channels * 2)); writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(32 * channels);
        for (int i = 0; i < 16; i++) { writer.Write((short)(silent ? 0 : 1000)); if (antiPhase) writer.Write((short)-1000); }
        return memory.ToArray();
    }
    private sealed class TrackedContent : StreamContent
    {
        public bool Disposed;
        public TrackedContent(byte[] bytes) : base(new MemoryStream(bytes)) { }
        public TrackedContent(Stream stream) : base(stream) { }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
    }
    private sealed class Handler : HttpMessageHandler
    {
        private readonly byte[] _audio; private readonly int _status; private readonly bool _delay, _slowBody; private readonly long? _length;
        public int Calls; public string Url, Authorization, Body; public TrackedContent Content;
        public Handler(byte[] audio, int status = 200, bool delay = false, long? declaredLength = null, bool slowBody = false) { _audio = audio; _status = status; _delay = delay; _length = declaredLength; _slowBody = slowBody; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken cancel)
        {
            Calls++; Url = message.RequestUri.ToString(); Authorization = message.Headers.Authorization?.ToString(); Body = await message.Content.ReadAsStringAsync(cancel);
            if (_delay) await Task.Delay(Timeout.Infinite, cancel);
            Content = _slowBody ? new TrackedContent(new StalledStream()) : new TrackedContent(_audio); if (_length.HasValue) Content.Headers.ContentLength = _length;
            return new HttpResponseMessage((HttpStatusCode)_status) { Content = Content };
        }
    }
    private sealed class StalledStream : Stream
    {
        public override bool CanRead => true; public override bool CanSeek => false; public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override async Task<int> ReadAsync(byte[] bytes, int offset, int count, CancellationToken cancel) { await Task.Delay(Timeout.Infinite, cancel); return 0; }
        public override int Read(byte[] bytes, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long length) => throw new NotSupportedException();
        public override void Write(byte[] bytes, int offset, int count) => throw new NotSupportedException();
    }
}
