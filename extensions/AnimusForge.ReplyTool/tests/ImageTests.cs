using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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

internal static class ImageTests
{
    private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static readonly Uri Endpoint = new Uri("https://example.invalid/v1/chat/completions");
    private static JObject Payload() => new JObject
    {
        ["model"] = "gemini-3.1-flash-image", ["max_tokens"] = 1234,
        ["extra_body"] = new JObject { ["google"] = new JObject { ["image_config"] = new JObject { ["aspect_ratio"] = "7:4", ["image_size"] = "1K" } } },
        ["messages"] = new JArray(new JObject { ["role"] = "system", ["content"] = "original" }, new JObject
        {
            ["role"] = "user", ["content"] = new JArray(
                new JObject { ["type"] = "text", ["text"] = "构图\n\"quoted\" 😀" },
                new JObject { ["type"] = "image_url", ["image_url"] = new JObject { ["url"] = "data:image/png;base64,reference-one", ["detail"] = "high" } },
                new JObject { ["type"] = "text", ["text"] = "第一张人物\n" },
                new JObject { ["type"] = "image_url", ["image_url"] = new JObject { ["url"] = "data:image/png;base64,reference-two" } },
                new JObject { ["type"] = "text", ["text"] = "第二张背景 [AFEF:KEEP]" })
        })
    };

    internal static async Task Run(Assembly af, string root, ReplyPrompt prompt, Action<bool,string> check)
    {
        foreach (string lang in new[] { "CN", "EN" })
        {
            var localized = new ReplyPrompt(File.ReadAllText(Path.Combine(root, "extensions/AnimusForge.ReplyTool/content/ReplyToolPrompt." + lang + ".json")));
            var original = Payload(); string raw = original.ToString();
            check(ImageRequestProtocol.TryPrepare(Endpoint, raw, localized, out var prepared), "image prepare " + lang);
            var actual = JObject.Parse(prepared);
            check(actual["tool_choice"].ToString() == "none" && ((JArray)actual["tools"]).Count == 1
                && actual["tools"][0]["function"]["name"].ToString() == "read_user_input", "image output not forced into a function");
            check(JToken.DeepEquals(original["extra_body"], actual["extra_body"]) && (int)actual["max_tokens"] == 1234
                && JToken.DeepEquals(original["messages"][0], actual["messages"][0]), "image config budget and system preserved");
            var userParts = (JArray)actual["messages"][1]["content"];
            var result = JObject.Parse(actual["messages"][3]["content"].ToString());
            check(userParts[0]["text"].ToString().StartsWith(localized.ImageInputInstruction), "image localized resource consumer " + lang);
            check(userParts.Count == 5 && JToken.DeepEquals(userParts[1], original["messages"][1]["content"][1])
                && JToken.DeepEquals(userParts[3], original["messages"][1]["content"][3]), "image reference positions and metadata preserved");
            var recovered = (JArray)userParts.DeepClone();
            foreach (var part in result["text_parts"]) recovered[(int)part["index"]]["text"] = part["text"].DeepClone();
            check(JToken.DeepEquals(recovered, original["messages"][1]["content"]), "all original text and image order recover exactly");
            check(actual["messages"][2]["tool_calls"][0]["id"].ToString() == actual["messages"][3]["tool_call_id"].ToString(), "image current pair IDs match");
            check(!ImageRequestProtocol.TryPrepare(Endpoint, prepared, localized, out var repeated) && repeated == prepared, "image no nested wrapping");
        }
        foreach (string field in new[] { "tools", "functions", "function_call", "tool_choice", "response_format", "stream", "n" })
        {
            var body = Payload(); body[field] = field == "stream" ? (JToken)true : field == "n" ? (JToken)2 : new JObject();
            string raw = body.ToString();
            check(!ImageRequestProtocol.TryPrepare(Endpoint, raw, prompt, out var prepared) && prepared == raw, "image preserve existing protocol " + field);
        }
        foreach (string path in new[] { "/v1/images/generations", "/v1/images/edits", "/v1beta/models/gemini:generateContent" })
            check(!ImageRequestProtocol.TryPrepare(new Uri("https://example.invalid" + path), Payload().ToString(), prompt, out _), "image route passthrough " + path);
        var other = Payload(); other["model"] = "gpt-image-2.5";
        check(!ImageRequestProtocol.TryPrepare(Endpoint, other.ToString(), prompt, out _), "image non Gemini passthrough");
        var imageOnly = Payload(); imageOnly["messages"][1]["content"] = new JArray(imageOnly["messages"][1]["content"][1].DeepClone());
        check(!ImageRequestProtocol.TryPrepare(Endpoint, imageOnly.ToString(), prompt, out _), "image-only no fabricated input");

        byte[] bytes;
        using (var bitmap = new Bitmap(32, 24, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(bitmap))
        using (var stream = new MemoryStream())
        {
            graphics.Clear(Color.FromArgb(255, 210, 30, 20)); bitmap.Save(stream, ImageFormat.Png); bytes = stream.ToArray();
        }
        string encoded = Convert.ToBase64String(bytes);
        string chat = new JObject { ["choices"] = new JArray(new JObject { ["finish_reason"] = "stop", ["message"] = new JObject
            { ["images"] = new JArray(new JObject { ["image_url"] = new JObject { ["url"] = "data:image/png;base64," + encoded } }) } }) }.ToString();
        // Exercise the installed AF image async method, its patched send site and
        // its real bounded response reader/base64/image normalizer together.
        await WithClient(af, async (invoke, handler) =>
        {
            handler.Response = chat;
            handler.Observe = request =>
            {
                var body = JObject.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                check(body["messages"].Count() == 3 && body["tool_choice"].ToString() == "none", "actual AF image send input wrapped");
                check(JObject.Parse(body["messages"][2]["content"].ToString())["text"].ToString() == "red cube", "actual AF image original prompt preserved");
                check(request.Headers.Authorization.Parameter == "test-key" && request.RequestUri == Endpoint, "actual AF image auth and endpoint");
            };
            var result = await invoke(false, CancellationToken.None);
            check((bool)result.GetType().GetField("Item1").GetValue(result) && handler.Count == 1, "actual AF image success single send");
            byte[] decoded = (byte[])result.GetType().GetField("Item2").GetValue(result);
            using (var image = new Bitmap(new MemoryStream(decoded)))
                check(image.Width == 32 && image.Height == 24 && image.GetPixel(0,0).R == 210, "actual AF base64 decodes expected pixels");
            check(result.GetType().GetField("Item6").GetValue(result).ToString() == "red cube", "AF ResolvedPrompt remains original input");
            handler.Observe = null; handler.Response = "{\"error\":{\"message\":\"limited\"}}"; handler.Status = (HttpStatusCode)429;
            result = await invoke(false, CancellationToken.None);
            check(!(bool)result.GetType().GetField("Item1").GetValue(result) && handler.Count == 2, "AF image HTTP failure preserved no retry");
            handler.Status = HttpStatusCode.OK; handler.Response = "{\"choices\":[{\"finish_reason\":\"stop\",\"message\":{\"content\":\"no image\"}}]}";
            result = await invoke(false, CancellationToken.None);
            check(!(bool)result.GetType().GetField("Item1").GetValue(result) && handler.Count == 3, "AF missing image remains failure no retry");
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                try { await invoke(false, cts.Token); throw new Exception("image cancellation ignored"); }
                catch (OperationCanceledException) { check(handler.Count == 3, "AF image pre-send cancellation"); }
            }
            handler.Response = new JObject { ["candidates"] = new JArray(new JObject { ["finishReason"] = "STOP", ["content"] = new JObject
                { ["parts"] = new JArray(new JObject { ["inlineData"] = new JObject { ["mimeType"] = "image/png", ["data"] = encoded } }) } }) }.ToString();
            handler.Observe = request =>
            {
                var body = JObject.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                check(body["contents"] != null && body["tools"] == null && request.Headers.Contains("x-goog-api-key"), "native Gemini original route unchanged");
            };
            result = await invoke(true, CancellationToken.None);
            check((bool)result.GetType().GetField("Item1").GetValue(result) && handler.Count == 4, "AF native inlineData still decodes");
        });

        // Direct wrapper check: an HTTP response stays the same instance and is
        // not consumed or rewritten before AF can apply its existing bounds.
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(chat) };
        using (var client = new HttpClient(new FakeHandler { FixedResponse = response }))
        using (var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = new StringContent(Payload().ToString()) })
        {
            request.Content.Headers.Add("X-Test-Content", "kept");
            var actual = await ImageTransportPatch.SendAsync(client, request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None);
            check(ReferenceEquals(actual, response) && await actual.Content.ReadAsStringAsync() == chat, "image response byte content and ownership preserved");
            check(request.Content.Headers.GetValues("X-Test-Content").Single() == "kept", "image custom content header retained");
            actual.Dispose();
        }
    }

    internal static async Task CheckUninstalled(Assembly af, Action<bool,string> check)
    {
        await WithClient(af, async (invoke, handler) =>
        {
            handler.Response = "{\"choices\":[]}";
            handler.Observe = request => check(JObject.Parse(request.Content.ReadAsStringAsync().GetAwaiter().GetResult())["tools"] == null, "image uninstall restores original send site");
            await invoke(false, CancellationToken.None);
        });
    }

    private static async Task WithClient(Assembly af, Func<Func<bool,CancellationToken,Task<object>>, FakeHandler, Task> run)
    {
        var type = af.GetType("AnimusForge.Illustrator.Core.UniversalOpenAiImageClient", true);
        var field = type.GetField("HttpClient", Any);
        var original = field.GetValue(null);
        var method = type.GetMethod("AttemptGenerateOnceAsync", Any);
        // Suppress only delivery-log IO in this isolated fixture process.
        var log = af.GetType("AnimusForge.Illustrator.Core.GenerationDiagnostics", true).GetMethod("WriteDelivery", Any);
        var fixture = new Harmony("ReplyTool.ImageTests");
        fixture.Patch(log, prefix: new HarmonyMethod(typeof(ImageTests), nameof(SkipLog)));
        var handler = new FakeHandler();
        using (var client = new HttpClient(handler))
        {
            field.SetValue(null, client);
            try
            {
                await run(async (native, token) =>
                {
                    var values = new Dictionary<string,object>
                    {
                        ["endpointUrl"] = native ? "https://example.invalid/v1beta/models/gemini-3.1-flash-image:generateContent" : Endpoint.ToString(),
                        ["model"] = "gemini-3.1-flash-image", ["effectivePrompt"] = "red cube", ["size"] = "1024x1024",
                        ["quality"] = "auto", ["style"] = "", ["apiKey"] = "test-key", ["isChatProtocol"] = true,
                        ["cancellationToken"] = token, ["nativeGemini"] = native
                    };
                    var args = method.GetParameters().Select(p => values.TryGetValue(p.Name, out var value) ? value : p.HasDefaultValue ? p.DefaultValue : null).ToArray();
                    var task = (Task)method.Invoke(null, args); await task;
                    return task.GetType().GetProperty("Result").GetValue(task);
                }, handler);
            }
            finally { field.SetValue(null, original); fixture.UnpatchAll("ReplyTool.ImageTests"); }
        }
    }
    private static bool SkipLog() => false;
    private sealed class FakeHandler : HttpMessageHandler
    {
        internal int Count;
        internal string Response;
        internal HttpStatusCode Status = HttpStatusCode.OK;
        internal Action<HttpRequestMessage> Observe;
        internal HttpResponseMessage FixedResponse;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Count++; Observe?.Invoke(request);
            return Task.FromResult(FixedResponse ?? new HttpResponseMessage(Status) { Content = new StringContent(Response, Encoding.UTF8, "application/json") });
        }
    }
}
