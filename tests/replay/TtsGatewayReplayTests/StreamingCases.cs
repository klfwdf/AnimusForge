using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

internal static class StreamingCases
{
    private static int checks;
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    internal static async Task Run(Assembly af)
    {
        byte[] pcm = Enumerable.Range(0, 96000).SelectMany(i => BitConverter.GetBytes((short)(i % 97 + 1))).ToArray();
        byte[] Wave(byte[] audio, bool unknown = false, int channels = 1)
        {
            using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
            writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(unknown ? 36 : audio.Length + 36); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16); writer.Write((short)1); writer.Write((short)channels); writer.Write(24000); writer.Write(48000 * channels); writer.Write((short)(2 * channels)); writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(unknown ? 0 : audio.Length); writer.Write(audio); return stream.ToArray();
        }
        object Request() => Activator.CreateInstance(af.GetType("AnimusForge.Refactor.Contracts.OpenAiTtsSynthesisRequest", true), "https://fixture.invalid/v1/audio/speech", "gpt-sovits-v4", "zh_default", "text", 1f, 60);
        Task Invoke(HttpClient client, Action<byte[], int, long?> callback, CancellationToken cancel)
        {
            var type = af.GetType("AnimusForge.Refactor.Adapters.OpenAiCompatibleTtsGateway", true);
            return (Task)type.GetMethod("StreamAsync").Invoke(Activator.CreateInstance(type, client), new object[] { Request(), "fixture-key", callback, cancel });
        }
        bool Success(Task task) => (bool)task.GetType().GetProperty("Result").GetValue(task).GetType().GetProperty("Success").GetValue(task.GetType().GetProperty("Result").GetValue(task));
        string Error(Task task) { var result = task.GetType().GetProperty("Result").GetValue(task); return (string)result.GetType().GetProperty("ErrorCode").GetValue(result); }
        foreach (bool unknown in new[] { false, true })
        {
            using var stream = new GatedStream(Wave(pcm, unknown), 30044);
            var handler = new Handler(stream);
            using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
            var first = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var result = new List<byte>(); int segments = 0;
            Task send = Invoke(client, (bytes, rate, samples) => {
                Check(rate == 24000 && samples == (unknown ? (long?)null : 96000), "WAV format/duration");
                if (segments++ == 0) { Check(stream.Position < stream.Length, "first PCM before response completion"); first.TrySetResult(true); }
                result.AddRange(bytes);
            }, CancellationToken.None);
            Check(handler.StreamRequested == true, "Streaming TTS explicitly requests stream=true in actual HTTP JSON");
            Check(await Task.WhenAny(first.Task, Task.Delay(3000)) == first.Task && !send.IsCompleted, "first segment is delivered while later audio is blocked");
            stream.Release.TrySetResult(true); await send;
            Check(Success(send) && result.SequenceEqual(pcm) && segments == 3, "known/unknown WAV chunks reconstruct exact PCM without repeats");
            Check(stream.Disposed, "WAV response released");
        }
        // Arbitrary tiny transport reads and stereo downmix are independent of PCM frame boundaries.
        var stereoBytes = new List<byte>();
        for (int i = 0; i < pcm.Length; i += 2) { stereoBytes.Add(pcm[i]); stereoBytes.Add(pcm[i + 1]); stereoBytes.Add(pcm[i]); stereoBytes.Add(pcm[i + 1]); }
        using (var stream = new GatedStream(Wave(stereoBytes.ToArray(), channels:2), int.MaxValue, 7))
        using (var client = new HttpClient(new Handler(stream)))
        {
            var output = new List<byte>(); var send = Invoke(client, (b,r,s) => output.AddRange(b), CancellationToken.None); await send;
            Check(Success(send) && output.SequenceEqual(pcm), "split stereo PCM frames downmix correctly");
        }
        foreach (byte[] input in new[] { Wave(new byte[32000]), Wave(pcm).Take(40000).ToArray(), Encoding.UTF8.GetBytes("not audio") })
        using (var stream = new GatedStream(input, int.MaxValue))
        using (var client = new HttpClient(new Handler(stream)))
        {
            var send = Invoke(client, (_,_,_) => { }, CancellationToken.None); await send;
            Check(!Success(send), "silent/truncated/invalid streamed audio fails");
        }
        using (var stream = new GatedStream(Wave(pcm), 30044))
        using (var client = new HttpClient(new Handler(stream)))
        using (var cancel = new CancellationTokenSource())
        {
            var first = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var send = Invoke(client, (_,_,_) => first.TrySetResult(true), cancel.Token);
            Check(await Task.WhenAny(first.Task, Task.Delay(3000)) == first.Task, "stream can be cancelled after partial publication");
            cancel.Cancel(); bool cancelled = false;
            try { await send; } catch (OperationCanceledException) { cancelled = true; }
            Check(cancelled && stream.Disposed, "partial stream cancellation releases network and propagates");
        }
        Console.WriteLine("PASS production streaming gateway checks=" + checks);
        await StreamingLiveCase.Run(af);
        if (Environment.GetEnvironmentVariable("AF_TTS_NATIVE_TEST") == "1")
        {
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var engineType = af.GetType("AnimusForge.TtsEngine", true);
            var engine = Activator.CreateInstance(engineType, true);
            var requestType = engineType.GetNestedType("PlaybackRequest", all);
            var jobType = engineType.GetNestedType("TtsJob", all);
            var playerType = engineType.GetNestedType("NativeSegmentOutput", all);
            using var cancel = new CancellationTokenSource();
            object request = Activator.CreateInstance(requestType, all, null, new object[] { 123L, -1, cancel.Token, CancellationToken.None }, null);
            object job = Activator.CreateInstance(jobType, true); jobType.GetField("Request", all).SetValue(job, request);
            object player = Activator.CreateInstance(playerType, all, null, new object[] { engine, job, 24000, 0f }, null);
            try
            {
                var clock = System.Diagnostics.Stopwatch.StartNew();
                playerType.GetMethod("Write", all).Invoke(player, new object[] { pcm.Take(4800).ToArray(), false });
                playerType.GetMethod("Write", all).Invoke(player, new object[] { pcm.Take(4800).ToArray(), false });
                Check(clock.Elapsed.TotalSeconds < 1.0, "native streaming writes have no old extra 300ms/segment delay");
                cancel.CancelAfter(50);
                bool cancelled = false;
                try { playerType.GetMethod("Write", all).Invoke(player, new object[] { pcm, false }); }
                catch (TargetInvocationException e) when (e.InnerException is OperationCanceledException) { cancelled = true; }
                Check(cancelled, "actual waveOut cancellation resets pinned native buffer safely");
            }
            finally { ((IDisposable)player).Dispose(); ((IDisposable)engine).Dispose(); }
            Console.WriteLine("PASS actual winmm streaming buffers + cancellation (volume=0, no game started)");
        }
    }
    private sealed class Handler : HttpMessageHandler
    {
        private readonly Stream stream;
        internal bool? StreamRequested;
        internal Handler(Stream stream) => this.stream = stream;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
        {
            using var body = System.Text.Json.JsonDocument.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            StreamRequested = body.RootElement.TryGetProperty("stream", out var flag) ? flag.GetBoolean() : (bool?)null;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) });
        }
    }
    private sealed class GatedStream : MemoryStream
    {
        private readonly int gate, fragment;
        internal readonly TaskCompletionSource<bool> Release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        internal bool Disposed;
        internal GatedStream(byte[] bytes, int gate, int fragment = 8192) : base(bytes) { this.gate=gate; this.fragment=fragment; }
        public override int Read(byte[] bytes, int offset, int count) => ReadAsync(bytes,offset,count,CancellationToken.None).GetAwaiter().GetResult();
        public override async Task<int> ReadAsync(byte[] bytes, int offset, int count, CancellationToken cancel)
        {
            if (Position >= gate && !Release.Task.IsCompleted)
            {
                await Task.WhenAny(Release.Task, Task.Delay(Timeout.Infinite, cancel)); cancel.ThrowIfCancellationRequested();
            }
            count = Math.Min(count,fragment);
            if (Position < gate) count = Math.Min(count, gate - (int)Position);
            return base.Read(bytes,offset,count);
        }
        protected override void Dispose(bool disposing) { Disposed=true; Release.TrySetResult(true); base.Dispose(disposing); }
    }
}
