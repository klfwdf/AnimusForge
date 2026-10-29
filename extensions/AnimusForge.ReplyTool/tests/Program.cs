using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.ReplyTool;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class Program
{
    private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static int _checks;
    private static string _root, _api, _game;
    private static ReplyPrompt _prompt;
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); _checks++; }
    private static void Bad(Action action, string name)
    {
        try { action(); } catch (InvalidDataException) { _checks++; return; }
        throw new Exception("Expected rejection: " + name);
    }
    private static int Main(string[] args)
    {
        _root = args[0]; _api = args[2]; _game = args[3];
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try { Run(args[1]).GetAwaiter().GetResult(); Console.WriteLine("PASS " + _checks + " assertions; actual AF " + _api); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally { ReplyToolInstaller.Uninstall(); }
    }
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name + ".dll";
        foreach (var dir in new[] { Path.Combine(_root, _api == "1.3" ? "_deps_auto" : ".tmp/build_check/1.4"),
            Path.Combine(_root,"_deps_auto"), Path.Combine(_game,"bin/Win64_Shipping_Client"),
            Path.Combine(_game,"Modules/Native/bin/Win64_Shipping_Client"), Path.Combine(_game,"Modules/AnimusForge/bin/Win64_Shipping_Client") })
        { string path = Path.Combine(dir, name); if (File.Exists(path)) return Assembly.LoadFrom(path); }
        return null;
    }
    private static JObject Payload(bool stream = false) => new JObject
    { ["model"] = "gemini-test", ["stream"] = stream, ["max_tokens"] = 120,
      ["messages"] = new JArray(new JObject { ["role"] = "system", ["content"] = "original rules" },
          new JObject { ["role"] = "user", ["content"] = "玩家原文" }) };
    private static string Response(string text, string finish = "tool_calls") => new JObject
    { ["id"] = "reply", ["choices"] = new JArray(new JObject { ["index"] = 0, ["finish_reason"] = finish,
      ["message"] = new JObject { ["role"] = "assistant", ["content"] = null,
        ["tool_calls"] = new JArray(new JObject { ["id"] = "call_1", ["type"] = "function", ["function"] = new JObject
          { ["name"] = "output_reply", ["arguments"] = new JObject { ["content"] = text }.ToString(Formatting.None) } }) } }),
      ["usage"] = new JObject { ["prompt_tokens"] = 23, ["completion_tokens"] = 12 } }.ToString(Formatting.None);
    private static string Delta(string args, string name = null, int index = 0) => new JObject
    { ["choices"] = new JArray(new JObject { ["index"] = 0, ["delta"] = new JObject
      { ["tool_calls"] = new JArray(new JObject { ["index"] = index, ["function"] = new JObject
        { ["name"] = name, ["arguments"] = args } }) }, ["finish_reason"] = null }) }.ToString(Formatting.None);
    private static string Finish(string reason) => "{\"choices\":[{\"index\":0,\"delta\":{},\"finish_reason\":\"" + reason + "\"}]}";
    private static string Sse(string text)
    {
        // Every SSE JSON frame must itself be valid Unicode; fragment the escaped
        // argument representation rather than putting half a raw emoji in a frame.
        string args = JsonConvert.SerializeObject(new JObject { ["content"] = text }, Formatting.None,
            new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeNonAscii });
        return string.Concat(args.Select((c, i) => "data: " + Delta(c.ToString(), i == 0 ? "output_reply" : null) + "\r\n\r\n"))
            + "data: " + Finish("tool_calls") + "\n\n"
            + "data: {\"choices\":[],\"usage\":{\"prompt_tokens\":23,\"completion_tokens\":12}}\n\n"
            + "data: [DONE]\n\n";
    }
    private static async Task Run(string afPath)
    {
        string ext = Path.Combine(_root, "extensions/AnimusForge.ReplyTool");
        string promptJson = File.ReadAllText(Path.Combine(ext,"content/ReplyToolPrompt.CN.json"));
        _prompt = new ReplyPrompt(promptJson);
        foreach (var lang in new[] { "CN", "EN" })
        {
            var prompt = new ReplyPrompt(File.ReadAllText(Path.Combine(ext,"content/ReplyToolPrompt." + lang + ".json")));
            var payload = Payload(); string raw = payload.ToString();
            Check(ReplyProtocol.TryPrepare(new Uri("https://example.invalid/v1/chat/completions"), raw, prompt, out var prepared, out var stream), "prepare " + lang);
            var body = JObject.Parse(prepared);
            Check(body["tool_choice"]["function"]["name"].ToString() == "output_reply", "forced tool");
            Check(JToken.DeepEquals(payload["messages"], new JArray(((JArray)body["messages"]).Skip(1))), "original messages unchanged");
            Check(body["messages"][0]["content"].ToString() == prompt.Instruction && !stream, "actual localized prompt");
        }
        foreach (var field in new[] { "tools", "functions", "tool_choice", "function_call", "response_format" })
        {
            var payload = Payload(); payload[field] = new JObject(); string raw = payload.ToString();
            Check(!ReplyProtocol.TryPrepare(new Uri("https://example.invalid/chat/completions"), raw, _prompt, out var prepared, out _)
                && prepared == raw, "passthrough " + field);
        }
        foreach (var endpoint in new[] { "/v1/messages", "/v1/audio/speech", "/v1/embeddings", "/v1/images/generations" })
            Check(!ReplyProtocol.TryPrepare(new Uri("https://example.invalid" + endpoint), Payload().ToString(), _prompt, out _, out _), "endpoint excluded " + endpoint);
        var other = Payload(); other["model"] = "another-model";
        Check(!ReplyProtocol.TryPrepare(new Uri("https://example.invalid/chat/completions"), other.ToString(), _prompt, out _, out _), "non Gemini passthrough");

        string text = "第一段。\n\"quoted\" \\ 😀 [ACTION:KEEP] {\"a\":1}";
        string arguments = new JObject { ["content"] = text }.ToString(Formatting.None);
        foreach (string encoded in new[] { arguments, " { \"content\" : \"中文\\n\\uD83D\\uDE00\" } " })
        {
            string expected = JObject.Parse(encoded)["content"].ToString();
            for (int cut = 0; cut <= encoded.Length; cut++)
            {
                var parser = new ReplyStringDecoder();
                string actual = parser.Push(encoded.Substring(0, cut)) + parser.Push(encoded.Substring(cut)); parser.Finish();
                Check(actual == expected, "every JSON split " + cut);
            }
        }
        foreach (string malformed in new[] { "{}", "{\"content\":1}", "{\"content\":\"a", "{\"content\":\"a\",\"x\":1}",
            "{\"content\":\"\\q\"}", "{\"content\":\"\\uD800\"}", "{\"content\":\"\\uDC00\"}", "{\"content\":\"a\"}garbage" })
            Bad(() => { var decoder = new ReplyStringDecoder(); decoder.Push(malformed); decoder.Finish(); }, malformed);
        var restored = JObject.Parse(ReplyProtocol.Restore(Response(text)));
        Check(restored.SelectToken("choices[0].message.content").ToString() == text, "nonstream raw text restored");
        Check((int)restored["usage"]["prompt_tokens"] == 23 && restored.SelectToken("choices[0].finish_reason").ToString() == "stop", "usage and finish");
        Bad(() => ReplyProtocol.Restore(Response(text, "length")), "truncated tool");
        Bad(() => ReplyProtocol.Restore(Response(text, "content_filter")), "filtered tool");
        Check(ReplyProtocol.Restore("{\"error\":{\"message\":\"denied\"}}") == "{\"error\":{\"message\":\"denied\"}}", "provider error preserved");

        var state = new ReplyStreamState();
        string joined = "";
        for (int i = 0; i < arguments.Length; i++) joined += JObject.Parse(state.Transform(Delta(arguments[i].ToString(), i == 0 ? "output_reply" : null))).SelectToken("choices[0].delta.content");
        Check(joined == text, "stream restored without repeats");
        Check(JObject.Parse(state.Transform(Finish("tool_calls"))).SelectToken("choices[0].finish_reason").ToString() == "stop", "stream finish converted");
        state.Transform("[DONE]"); state.EndOfStream();
        var splitName = new ReplyStreamState(); splitName.Transform(Delta("", "output_"));
        Check(JObject.Parse(splitName.Transform(Delta(arguments, "reply"))).SelectToken("choices[0].delta.content").ToString() == text, "split tool name");
        var interrupted = new ReplyStreamState(); interrupted.Transform(Delta("{\"content\":\"part", "output_reply"));
        Bad(interrupted.EndOfStream, "interrupted stream rejected");
        var two = new ReplyStreamState(); two.Transform(Delta(arguments, "output_reply"));
        Bad(() => two.Transform(Delta(arguments, "output_reply", 1)), "multiple calls");
        var mixed = new ReplyStreamState(); mixed.Transform("{\"choices\":[{\"delta\":{\"content\":\"plain\"}}]}");
        Bad(() => mixed.Transform(Delta(arguments, "output_reply")), "late tool cannot duplicate published plain text");
        Check(new ReplyStreamState().Transform("{\"choices\":[{\"delta\":{\"content\":\"plain\"}}]}").Contains("plain"), "plain fallback");

        using (var input = new ByteStream(Encoding.UTF8.GetBytes(Sse(text))))
        using (var stream = new ReplySseStream(input, CancellationToken.None))
        using (var reader = new StreamReader(stream))
        {
            string converted = await reader.ReadToEndAsync();
            Check(!converted.Contains("tool_calls\":") && converted.Contains("[DONE]"), "byte fragmented UTF8 SSE converted");
        }
        await Task.WhenAll(Enumerable.Range(0, 32).Select(n => Task.Run(async () =>
        {
            using (var stream = new ReplySseStream(new ByteStream(Encoding.UTF8.GetBytes(Sse("reply " + n))), CancellationToken.None))
            using (var reader = new StreamReader(stream))
            {
                string converted = await reader.ReadToEndAsync();
                string actual = string.Concat(converted.Split('\n').Where(l => l.StartsWith("data: {")).Select(l => JObject.Parse(l.Substring(6)).SelectToken("choices[0].delta.content")?.ToString()));
                if (actual != "reply " + n) throw new Exception("concurrent response cross-talk");
            }
        })));
        Check(true, "32 independent concurrent responses");

        // Execute the actual installed AF transports with a deterministic sender.
        var af = Assembly.LoadFrom(afPath);
        Check(ReplyToolInstaller.Install(af, promptJson, Console.WriteLine), "actual host installation");
        Check(ReplyToolInstaller.Install(af, promptJson, Console.WriteLine), "idempotent install");
        foreach (bool streaming in new[] { false, true })
        {
            int sends = 0; var chunks = new StringBuilder();
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sender = async (request, token) =>
            {
                sends++;
                var body = JObject.Parse(await request.Content.ReadAsStringAsync());
                Check(body["tools"][0]["function"]["name"].ToString() == "output_reply", "actual patched sender payload");
                Check(request.Headers.Authorization.Parameter == "test-key" && request.RequestUri.Host == "example.invalid", "original auth and endpoint");
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(streaming ? Sse(text) : Response(text), Encoding.UTF8, streaming ? "text/event-stream" : "application/json") };
            };
            var result = await Invoke(af, streaming, Payload(streaming).ToString(), sender, CancellationToken.None, true, value => chunks.Append(Property(value,"Content")));
            Check(sends == 1 && Property(result,"Discarded").Equals(false), "exactly one original send");
            if (streaming) Check(Property(result,"Content").ToString() == text && chunks.ToString() == text, "AF final and callback match once");
            else
            {
                var extractor = af.GetType("AnimusForge.LlmApiCompat").GetMethod("ExtractAssistantText", new[] { typeof(string) });
                Check((string)extractor.Invoke(null, new[] { Property(result,"Body") }) == text, "AF actual consumer text");
            }
            int failures = 0;
            string errorBody = "{\"error\":{\"message\":\"limited\"}}";
            var failed = await Invoke(af, streaming, Payload(streaming).ToString(), (request, token) =>
            {
                failures++;
                var response = new HttpResponseMessage((HttpStatusCode)429) { Content = new StringContent(errorBody) };
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(5));
                return Task.FromResult(response);
            }, CancellationToken.None, true, null);
            Check(failures == 1 && (HttpStatusCode)Property(failed,"StatusCode") == (HttpStatusCode)429
                && Property(failed, streaming ? "ErrorBody" : "Body").ToString() == errorBody
                && (int)Property(failed,"RetryAfterSeconds") == 5, "HTTP failure and retry metadata preserved without resend");
        }
        int reads = 0; bool disposed = false;
        var discarded = await Invoke(af, false, Payload().ToString(), (request, token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
          { Content = new ObserveContent(() => reads++, () => disposed = true) }), CancellationToken.None, false, null);
        Check(Property(discarded,"Discarded").Equals(true) && reads == 0 && disposed, "AF rejection before response body read and disposal");
        using (var cts = new CancellationTokenSource())
        {
            var block = new BlockingStream();
            var task = Invoke(af, true, Payload(true).ToString(), (request, token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StreamContent(block) }), cts.Token, true, null);
            Check(await Task.WhenAny(block.Started.Task, task, Task.Delay(3000)) == block.Started.Task, "blocking read started");
            cts.Cancel();
            Check(await Task.WhenAny(task, Task.Delay(3000)) == task, "cancellation stops pending response read");
            try { await task; throw new Exception("cancellation not propagated"); } catch (OperationCanceledException) { Check(block.Disposed, "cancelled stream disposed"); }
        }
        ReplyToolInstaller.Uninstall();
        Check(Harmony.GetAllPatchedMethods().All(m => !Harmony.GetPatchInfo(m).Owners.Contains(ReplyToolInstaller.Owner)), "unload removes only own patches");
        bool originalPayload = false;
        await Invoke(af, false, Payload().ToString(), async (request, token) =>
        {
            originalPayload = JObject.Parse(await request.Content.ReadAsStringAsync())["tools"] == null;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"plain\"}}]}") };
        }, CancellationToken.None, true, null);
        Check(originalPayload, "uninstall restores original outgoing request");
        Check(!ReplyToolInstaller.Install(typeof(Program).Assembly, promptJson, Console.WriteLine), "unsupported AF shape adds no patches");
    }

    private static object Property(object value, string name) => value.GetType().GetProperty(name, Any).GetValue(value);
    private static async Task<object> Invoke(Assembly af, bool stream, string json,
        Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> sender, CancellationToken token, bool accept, Action<object> observer)
    {
        var method = af.GetType("AnimusForge." + (stream ? "LlmStreamingTransport" : "LlmNonStreamingTransport")).GetMethod("SendAsync", Any);
        var parameters = method.GetParameters();
        var args = parameters.Select(p => p.HasDefaultValue ? p.DefaultValue : null).ToArray();
        args[0] = "https://example.invalid/v1/chat/completions"; args[1] = "test-key"; args[2] = json; args[3] = sender; args[4] = token;
        args[5] = new Func<HttpStatusCode,bool>(_ => accept);
        if (stream && observer != null)
        {
            int index = Array.FindIndex(parameters, p => p.Name == "onDelta");
            var value = Expression.Parameter(parameters[index].ParameterType.GenericTypeArguments[0]);
            args[index] = Expression.Lambda(parameters[index].ParameterType, Expression.Invoke(Expression.Constant(observer), Expression.Convert(value, typeof(object))), value).Compile();
        }
        var task = (Task)method.Invoke(null, args); await task; return Property(task, "Result");
    }
    private sealed class ByteStream : MemoryStream
    {
        internal ByteStream(byte[] bytes) : base(bytes) { }
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 1));
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) => base.ReadAsync(buffer, offset, Math.Min(count,1), token);
    }
    private sealed class ObserveContent : HttpContent
    {
        private readonly Action _read, _dispose;
        internal ObserveContent(Action read, Action dispose) { _read = read; _dispose = dispose; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) { _read(); return Task.CompletedTask; }
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override void Dispose(bool disposing) { if (disposing) _dispose(); base.Dispose(disposing); }
    }
    private sealed class BlockingStream : Stream
    {
        internal readonly TaskCompletionSource<bool> Started = new TaskCompletionSource<bool>();
        private readonly TaskCompletionSource<int> _read = new TaskCompletionSource<int>();
        internal bool Disposed;
        public override Task<int> ReadAsync(byte[] buffer,int offset,int count,CancellationToken token) { Started.TrySetResult(true); return _read.Task; }
        protected override void Dispose(bool disposing) { Disposed = true; _read.TrySetCanceled(); base.Dispose(disposing); }
        public override int Read(byte[] buffer,int offset,int count) => throw new NotSupportedException();
        public override bool CanRead => true; public override bool CanSeek => false; public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { } public override long Seek(long o, SeekOrigin s) => throw new NotSupportedException();
        public override void SetLength(long v) => throw new NotSupportedException();
        public override void Write(byte[] b,int o,int c) => throw new NotSupportedException();
    }
}
