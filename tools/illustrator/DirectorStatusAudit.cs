using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

// Invokes the shipped typed director path with an in-memory HTTP handler.
// No network, provider account, game state or GPU is used.
public static class DirectorStatusAudit
{
    private const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private static int checks;
    private static Type optionsType;
    private static Type referenceType;
    private static Type referenceKind;
    private static MethodInfo create;
    private static object plan;
    private const string Body = "【人物与镜头】人物身着现有衣物，低头审视手中现有文书，手指轻触纸边。" +
        "【场景空间】环境材质向远处延展，形成清晰且柔和的空间层次，墙体和地面保留自然的材质变化。" +
        "【光线与色彩】自然光与环境反光协调过渡，暗部纹理清晰可辨，衣物和周围环境保持统一光照。" +
        "【空间关系】近处与远处通过遮挡和景深形成连续纵深关系，人物与环境之间保持真实尺度。";
    private const string NamedBody = "【画作标题】灯下裁决【画作主题】战前权衡【人物行动】审视手中文书。" + Body;

    private static void Check(bool pass, string name)
    {
        if (!pass) throw new Exception("FAIL " + name);
        checks++;
        Console.WriteLine("PASS " + name);
    }
    private static T Property<T>(object value, string name)
    { return (T)value.GetType().GetProperty(name).GetValue(value, null); }
    private static void SetOption(object value, string name, object data)
    { optionsType.GetField("<" + name + ">k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(value, data); }
    private static object Options()
    {
        object value = FormatterServices.GetUninitializedObject(optionsType);
        SetOption(value, "EnableLlmPromptExpansion", true);
        SetOption(value, "EnableMultimodalVision", true);
        SetOption(value, "DirectorApiBaseUrl", "http://offline.invalid/v1");
        SetOption(value, "DirectorApiKey", "offline-secret-sentinel");
        SetOption(value, "DirectorModelName", "director-audit");
        SetOption(value, "DirectorMaxTokens", 1600);
        return value;
    }
    private static Array References()
    {
        Array images = Array.CreateInstance(referenceType, 1);
        images.SetValue(Activator.CreateInstance(referenceType, new object[] { "iVBORw0KGgo=", "原始人物参考", Enum.Parse(referenceKind, "Character") }), 0);
        return images;
    }
    private static string Reply(string finish, object content, string refusal)
    {
        return new JavaScriptSerializer().Serialize(new {
            choices = new[] { new { finish_reason = finish, message = new { content = content, refusal = refusal } } },
            usage = new { prompt_tokens = 321, completion_tokens = 123, total_tokens = 444 }
        });
    }
    private sealed class MemoryHandler : HttpMessageHandler
    {
        public readonly Queue<HttpResponseMessage> Replies = new Queue<HttpResponseMessage>();
        public readonly List<string> Bodies = new List<string>();
        public bool Timeout;
        public void Add(HttpStatusCode status, string body)
        { Replies.Enqueue(new HttpResponseMessage(status) { Content = new StringContent(body) }); }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Bodies.Add(await request.Content.ReadAsStringAsync().ConfigureAwait(false));
            if (Timeout) throw new TaskCanceledException("in-memory timeout");
            if (Replies.Count == 0) throw new Exception("unexpected extra director request");
            return Replies.Dequeue();
        }
    }
    private static object Generate(MemoryHandler handler, object options, Array references, CancellationToken token)
    {
        using (var client = new HttpClient(handler, false))
        {
            Task task = (Task)create.Invoke(null, new object[] { plan, references, options, client, token });
            task.GetAwaiter().GetResult();
            return task.GetType().GetProperty("Result").GetValue(task, null);
        }
    }
    private static object Generate(MemoryHandler handler, object options, Array references)
    { return Generate(handler, options, references, CancellationToken.None); }
    private static void VerifyFallback(object result, string status, string reason)
    {
        Check(Property<string>(result, "DirectionStatus") == status && Property<bool>(result, "UsedLocalFallback"), reason + ": status identifies local fallback");
        Check(Property<string>(result, "Title") == "" && !Property<string>(result, "Prompt").Contains("灯下裁决"), reason + ": discarded response metadata never leaks");
        Check(!string.IsNullOrWhiteSpace(Property<string>(result, "FallbackReason")), reason + ": reason retained separately");
        Check(!Property<string>(result, "Prompt").Contains(Property<string>(result, "StatusText")), reason + ": status never enters image prompt");
    }
    public static void Run(string dllPath)
    {
        checks = 0;
        Assembly assembly = Assembly.LoadFrom(dllPath);
        string core = "AnimusForge.Illustrator.Core.";
        Type director = assembly.GetType(core + "VisualDirectorEngine", true);
        optionsType = assembly.GetType(core + "IllustrationOptions", true);
        referenceType = assembly.GetType(core + "IllustrationReferenceImage", true);
        referenceKind = assembly.GetType(core + "IllustrationReferenceKind", true);
        create = director.GetMethod("CreateDirectionWithClientAsync", Static);
        plan = Activator.CreateInstance(assembly.GetType(core + "IllustrationPromptPlan", true), new object[] { "人物百科纪事", "现有布衣和手中文书。", "", "" });
        Check(director.GetMethod("CallLlmDirectorAsync", Static).ReturnType == typeof(Task<string>), "legacy private string method remains compatible");

        var handler = new MemoryHandler();
        handler.Add(HttpStatusCode.OK, Reply("stop", NamedBody, null));
        object result = Generate(handler, Options(), References());
        Check(Property<string>(result, "DirectionStatus") == "complete" && !Property<bool>(result, "UsedLocalFallback"), "completed response has explicit successful status");
        Check(Property<string>(result, "Prompt").Contains(Body) && Property<string>(result, "Title") == "灯下裁决", "completed direction keeps full body and separate title");
        Check(Property<string>(result, "FinishReason") == "stop" && Property<int?>(result, "PromptTokens") == 321 && Property<int?>(result, "CompletionTokens") == 123 && Property<int?>(result, "TotalTokens") == 444, "finish reason and provider usage preserved");
        Check(handler.Bodies.Count == 1 && handler.Bodies[0].Contains("image_url") && handler.Bodies[0].Contains("director-audit"), "production payload sends visual references and selected model");
        Check(!Property<string>(result, "Prompt").Contains("画作标题") && !Property<string>(result, "Prompt").Contains("director-audit"), "metadata and API model never enter image body");

        foreach (string finish in new[] { "length", "max_tokens", "max_output_tokens" })
        {
            handler = new MemoryHandler();
            handler.Add(HttpStatusCode.OK, Reply(finish, NamedBody, null));
            result = Generate(handler, Options(), References());
            VerifyFallback(result, "truncated", finish);
            Check(!Property<string>(result, "Prompt").Contains(Body) && Property<string>(result, "FinishReason") == finish && handler.Bodies.Count == 1, finish + ": even a four-section body is discarded without retry");
        }
        foreach (string finish in new[] { "content_filter", "tool_calls", "unknown_provider_reason" })
        {
            handler = new MemoryHandler();
            handler.Add(HttpStatusCode.OK, Reply(finish, NamedBody, null));
            result = Generate(handler, Options(), References());
            VerifyFallback(result, "local_fallback", finish);
            Check(handler.Bodies.Count == 1 && Property<string>(result, "FinishReason") == finish, finish + ": no retry and raw ending preserved");
        }
        handler = new MemoryHandler();
        handler.Add(HttpStatusCode.OK, Reply("stop", NamedBody, "refused"));
        VerifyFallback(Generate(handler, Options(), References()), "local_fallback", "refusal field");

        handler = new MemoryHandler();
        handler.Add(HttpStatusCode.OK, Reply(null, NamedBody, null));
        result = Generate(handler, Options(), null);
        Check(Property<string>(result, "DirectionStatus") == "complete" && Property<string>(result, "StatusText").Contains("未返回结束标记"), "legacy API without finish reason remains usable with explicit uncertainty");
        handler = new MemoryHandler();
        handler.Add(HttpStatusCode.OK, Reply("stop", new[] { new { type = "text", text = NamedBody } }, null));
        Check(Property<string>(Generate(handler, Options(), null), "DirectionStatus") == "complete", "text content parts parse through actual HTTP path");

        foreach (string badBody in new[] { Reply(null, null, null), "{invalid-json", Reply("stop", "【画作标题】灯下裁决只有装备清单", null) })
        {
            handler = new MemoryHandler();
            handler.Add(HttpStatusCode.OK, badBody);
            result = Generate(handler, Options(), References());
            VerifyFallback(result, "local_fallback", "empty malformed or rejected direction");
            Check(handler.Bodies.Count == 1, "empty malformed or structurally rejected direction is not retried");
        }

        handler = new MemoryHandler();
        handler.Add(HttpStatusCode.BadRequest, "model does not support image input");
        handler.Add(HttpStatusCode.OK, Reply("stop", NamedBody, null));
        result = Generate(handler, Options(), References());
        Check(Property<string>(result, "DirectionStatus") == "vision_unsupported" && Property<bool>(result, "VisionUnsupported") && !Property<bool>(result, "UsedLocalFallback"), "unsupported vision followed by useful text is distinctly labeled");
        Check(handler.Bodies.Count == 2 && handler.Bodies[0].Contains("image_url") && !handler.Bodies[1].Contains("image_url") && handler.Bodies[1].Contains("本次仅提供文字"), "single permitted retry removes images and tells director no visual evidence exists");
        Check(Property<string>(result, "StatusText").Contains("不支持识图") && Property<string>(result, "FallbackReason").Length > 0, "text-only degradation has visible status and saved reason");

        handler = new MemoryHandler();
        handler.Add(HttpStatusCode.BadRequest, "unsupported vision");
        handler.Add(HttpStatusCode.OK, Reply("length", NamedBody, null));
        result = Generate(handler, Options(), References());
        VerifyFallback(result, "truncated", "text-only retry truncated");
        Check(Property<bool>(result, "VisionUnsupported") && Property<string>(result, "FallbackReason").Contains("截断") && handler.Bodies.Count == 2, "vision and truncation causes survive together without a third request");

        foreach (var error in new[] {
            new { Status = HttpStatusCode.ServiceUnavailable, Body = "vision upstream timeout offline-secret-sentinel" },
            new { Status = HttpStatusCode.Unauthorized, Body = "unsupported image model offline-secret-sentinel" } })
        {
            handler = new MemoryHandler();
            handler.Add(error.Status, error.Body);
            result = Generate(handler, Options(), References());
            VerifyFallback(result, "local_fallback", "HTTP " + (int)error.Status);
            Check(handler.Bodies.Count == 1 && !Property<string>(result, "FallbackReason").Contains("offline-secret"), "generic HTTP errors do not retry or expose provider error secrets");
        }

        handler = new MemoryHandler { Timeout = true };
        result = Generate(handler, Options(), References());
        Check(Property<string>(result, "DirectionStatus") == "local_fallback" && Property<string>(result, "FallbackReason").Contains("超时") && handler.Bodies.Count == 1, "transport timeout becomes identified local fallback without retry");
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            bool cancelled = false;
            try { Generate(new MemoryHandler(), Options(), References(), cancellation.Token); }
            catch (OperationCanceledException) { cancelled = true; }
            Check(cancelled, "caller cancellation is never mistaken for local fallback");
        }
        handler = new MemoryHandler();
        object options = Options();
        SetOption(options, "EnableLlmPromptExpansion", false);
        result = Generate(handler, options, References());
        Check(handler.Bodies.Count == 0 && Property<string>(result, "DirectionStatus") == "local_fallback" && Property<string>(result, "FallbackReason").Contains("未启用"), "disabled director reports local route without HTTP");
        handler = new MemoryHandler();
        options = Options();
        SetOption(options, "EnableMultimodalVision", false);
        handler.Add(HttpStatusCode.OK, Reply("stop", NamedBody, null));
        result = Generate(handler, options, References());
        Check(handler.Bodies.Count == 1 && !handler.Bodies[0].Contains("image_url") && !Property<bool>(result, "VisionUnsupported"), "intentional text-only setting is distinguished from provider degradation");
        Console.WriteLine("RESULT: " + checks + " PASS / 0 FAIL");
    }
}
