using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
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
        SetOption(value, "DirectorApproximateTokens", 2000);
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
    private static string GenerateFailure(MemoryHandler handler, object options, Array references, string reason)
    {
        try { Generate(handler, options, references); }
        catch (InvalidOperationException ex)
        {
            Check(!string.IsNullOrWhiteSpace(ex.Message), reason + ": failure has a bounded user-facing reason");
            return ex.Message;
        }
        throw new Exception("FAIL " + reason + ": expected director failure");
    }
    private static void RunCustomDirectorChecks(Type director, Assembly assembly)
    {
        const string Sentinel = "CUSTOM_DIRECTOR_RULE_SENTINEL";
        const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var settingsType = assembly.GetType("AnimusForge.Illustrator.IllustratorSettings", true);
        object settings = Activator.CreateInstance(settingsType);
        Check((string)settingsType.GetProperty("CustomDirectorPrompt").GetValue(settings, null) == string.Empty,
            "old settings default to empty director customization");
        Check(settingsType.GetProperty("EditCustomDirectorPrompt").GetValue(settings, null) is Action &&
            settingsType.GetProperty("EditCustomDirectorPrompt").GetCustomAttributesData().Any(a => a.AttributeType.Name == "SettingPropertyButtonAttribute"),
            "MCM exposes a wired long-text director editor button");
        settingsType.GetProperty("CustomDirectorPrompt").SetValue(settings, "  " + Sentinel + "  ", null);
        var ctor = optionsType.GetConstructors(Instance).Single(c => c.GetParameters().Length == 4);
        object frozen = ctor.Invoke(new object[] { settings, "http://offline.invalid/v1", "fixture", "director" });
        Check(Property<string>(frozen, "CustomDirectorPrompt") == Sentinel, "game-thread options freeze and trim director rules");
        settingsType.GetProperty("CustomDirectorPrompt").SetValue(settings, "CHANGED_AFTER_START", null);
        Check(Property<string>(frozen, "CustomDirectorPrompt") == Sentinel, "editing settings cannot mutate an in-flight generation snapshot");
        object modified = ctor.Invoke(new object[] { settings, "http://offline.invalid/v1", "fixture", "director" });
        Check(Property<string>(frozen, "StyleFingerprint") != Property<string>(modified, "StyleFingerprint"),
            "director rule changes invalidate cached-settings fingerprint");
        settingsType.GetProperty("CustomDirectorPrompt").SetValue(settings, "", null);
        object empty = ctor.Invoke(new object[] { settings, "http://offline.invalid/v1", "fixture", "director" });
        settingsType.GetProperty("CustomDirectorPrompt").SetValue(settings, "  ", null);
        object whitespace = ctor.Invoke(new object[] { settings, "http://offline.invalid/v1", "fixture", "director" });
        Check(Property<string>(empty, "StyleFingerprint") == Property<string>(whitespace, "StyleFingerprint"), "blank rules preserve default cache identity");

        object options = Options();
        var build = director.GetMethod("BuildDirectorPayload", Static);
        var planType = plan.GetType();
        foreach (string mode in new[] { "人物百科纪事", "最近2条对话联动的场景插画", "周报历史纪事插画", "通用插画" })
        {
            object modePlan = Activator.CreateInstance(planType, new object[] { mode, "HARD_FACT_SENTINEL", "", "" });
            string blank = build.Invoke(null, new object[] { modePlan, options, null, false }).ToString();
            SetOption(options, "CustomDirectorPrompt", "  ");
            Check(build.Invoke(null, new object[] { modePlan, options, null, false }).ToString() == blank,
                mode + " empty rules leave entire request payload unchanged");
            SetOption(options, "CustomDirectorPrompt", Sentinel);
            foreach (bool textOnly in new[] { false, true })
            {
                string payload = build.Invoke(null, new object[] { modePlan, options, textOnly ? null : References(), textOnly }).ToString();
                Check(payload.Contains(Sentinel) && payload.Split(new[] { Sentinel }, StringSplitOptions.None).Length == 2 && payload.Contains("HARD_FACT_SENTINEL"),
                    mode + " director receives custom rule once alongside facts; textOnly=" + textOnly);
                Check(payload.Contains("自定义规则边界") && payload.Contains("不是已发生事实") && payload.Contains("四段正文格式"),
                    mode + " rules retain factual and output-format precedence; textOnly=" + textOnly);
            }
            SetOption(options, "CustomDirectorPrompt", null);
        }
        SetOption(options, "CustomDirectorPrompt", Sentinel);
        var handler = new MemoryHandler();
        handler.Add(HttpStatusCode.OK, Reply("stop", NamedBody, null));
        object result = Generate(handler, options, References());
        Check(handler.Bodies.Count == 1 && handler.Bodies[0].Contains(Sentinel) && !Property<string>(result, "Prompt").Contains(Sentinel),
            "real HTTP director receives rule but image prompt does not append the raw rule");
        SetOption(options, "EnableLlmPromptExpansion", false);
        handler = new MemoryHandler(); result = Generate(handler, options, References());
        Check(handler.Bodies.Count == 0 && !Property<string>(result, "Prompt").Contains(Sentinel),
            "disabled director neither sends rule nor leaks it into local image fallback");
    }

    private static void RunPlayerRedrawChecks(Type director)
    {
        const string Sentinel = "PLAYER_REDRAW_REQUEST_SENTINEL";
        object original = plan;
        var build = director.GetMethod("BuildDirectorPayload", Static);
        object options = Options();
        SetOption(options, "CustomDirectorPrompt", "GLOBAL_RULE_SENTINEL");
        try
        {
            foreach (string mode in new[] { "人物百科纪事", "最近2条对话联动的场景插画", "周报历史纪事插画" })
            {
                plan = Activator.CreateInstance(original.GetType(), new object[] { mode, "现有布衣和手中文书。", "OPEN_ART_SENTINEL", "DIRECTOR_FACT_SENTINEL", "  " + Sentinel + "\n第二行要求  " });
                Check(Property<string>(plan, "PlayerRedrawPrompt") == Sentinel + "\n第二行要求", mode + " per-redraw prompt is trimmed and preserves newlines");
                foreach (bool textOnly in new[] { false, true })
                {
                    string payload = build.Invoke(null, new object[] { plan, options, textOnly ? null : References(), textOnly }).ToString();
                    Check(payload.Contains(Sentinel) && payload.Split(new[] { Sentinel }, StringSplitOptions.None).Length == 2,
                        mode + " redraw request reaches director exactly once; textOnly=" + textOnly);
                    Check(payload.Contains("GLOBAL_RULE_SENTINEL") && payload.Contains("DIRECTOR_FACT_SENTINEL") && payload.Contains("现有布衣"),
                        mode + " redraw preserves global rules and original facts");
                    Check(payload.Contains("不是已发生事实") && payload.Contains("不复述原始提示词"), mode + " redraw preserves factual and visual-output boundary");
                }
                var handler = new MemoryHandler();
                handler.Add(HttpStatusCode.OK, Reply("stop", NamedBody, null));
                object result = Generate(handler, options, References());
                Check(handler.Bodies.Count == 1 && handler.Bodies[0].Contains(Sentinel) && !Property<string>(result, "Prompt").Contains(Sentinel),
                    mode + " actual director request uses player prompt, final image prompt does not append raw input");
                Check(Property<string>(original, "PlayerRedrawPrompt") == string.Empty, "ordinary plan cannot inherit the previous redraw request");
            }
            Type popup = director.Assembly.GetType("AnimusForge.Illustrator.UI.Overlays.IllustrationCardPopup", true);
            Type snapshot = popup.GetNestedType("ConversationIllustrationSessionSnapshot", BindingFlags.NonPublic);
            object session = Activator.CreateInstance(snapshot, true);
            snapshot.GetField("StableHardFacts", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, "FROZEN_FACT_SENTINEL");
            snapshot.GetField("SceneDirectorNote", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, "SCENE_NOTE_SENTINEL");
            object sessionPlan = popup.GetMethod("BuildConversationSessionPlan", Static).Invoke(null, new object[] { plan, session, null });
            Check(Property<string>(sessionPlan, "PlayerRedrawPrompt") == Property<string>(plan, "PlayerRedrawPrompt"), "real conversation session reuse retains this redraw's prompt");
            Check(Property<string>(sessionPlan, "HardFacts").Contains("FROZEN_FACT_SENTINEL") && Property<string>(sessionPlan, "DirectorOnlyFacts").Contains("SCENE_NOTE_SENTINEL"),
                "adding redraw prompt does not replace frozen scene facts or director-only scene note");
            var invalid = new MemoryHandler();
            invalid.Add(HttpStatusCode.OK, Reply("stop", "近景观察人物手势。", null));
            string invalidFailure = GenerateFailure(invalid, Options(), References(), "unusable player redraw direction");
            Check(invalidFailure.Contains("本地构图"), "guided redraw stops instead of silently dropping player input in local fallback");
            Check(invalid.Bodies.Count == 1, "invalid guided output has no automatic paid retry");
            object guidedPlan = plan;
            plan = original;
            invalid = new MemoryHandler(); invalid.Add(HttpStatusCode.OK, Reply("stop", "近景观察人物手势。", null));
            object ordinaryFallback = Generate(invalid, Options(), References());
            Check(Property<bool>(ordinaryFallback, "UsedLocalFallback") && invalid.Bodies.Count == 1, "ordinary redraw keeps existing local fallback behavior");
            plan = guidedPlan;
            var disabled = Options(); SetOption(disabled, "EnableLlmPromptExpansion", false);
            var blocked = new MemoryHandler();
            GenerateFailure(blocked, disabled, References(), "disabled director rejects player redraw");
            Check(blocked.Bodies.Count == 0, "no paid request or local fallback for unavailable player-directed redraw");
            disabled = Options(); SetOption(disabled, "DirectorApiBaseUrl", "");
            blocked = new MemoryHandler();
            GenerateFailure(blocked, disabled, References(), "missing director rejects player redraw");
            Check(blocked.Bodies.Count == 0, "unconfigured director never silently drops redraw input");
        }
        finally { plan = original; }
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

        RunCustomDirectorChecks(director, assembly);
        RunPlayerRedrawChecks(director);

        var handler = new MemoryHandler();
        handler.Add(HttpStatusCode.OK, Reply("stop", NamedBody, null));
        object result = Generate(handler, Options(), References());
        Check(Property<string>(result, "DirectionStatus") == "complete" && !Property<bool>(result, "UsedLocalFallback"), "completed response has explicit successful status");
        Check(!Property<bool>(result, "UsedTextOnlyDirector"), "visual director records that it received references");
        Check(Property<string>(result, "Prompt").Contains(Body) && Property<string>(result, "Title") == "灯下裁决", "completed direction keeps full body and separate title");
        Check(Property<string>(result, "FinishReason") == "stop" && Property<int?>(result, "PromptTokens") == 321 && Property<int?>(result, "CompletionTokens") == 123 && Property<int?>(result, "TotalTokens") == 444, "finish reason and provider usage preserved");
        Check(handler.Bodies.Count == 1 && handler.Bodies[0].Contains("image_url") && handler.Bodies[0].Contains("director-audit"), "production payload sends visual references and selected model");
        Check(!handler.Bodies[0].Contains("\"max_tokens\"") && handler.Bodies[0].Contains("约 2000 tokens") && handler.Bodies[0].Contains("不是硬性限制"), "director payload uses an approximate length target without a hard token cap");
        Check(!Property<string>(result, "Prompt").Contains("画作标题") && !Property<string>(result, "Prompt").Contains("director-audit"), "metadata and API model never enter image body");

        foreach (string finish in new[] { "length", "max_tokens", "max_output_tokens" })
        {
            handler = new MemoryHandler();
            handler.Add(HttpStatusCode.OK, Reply(finish, NamedBody, null));
            string failure = GenerateFailure(handler, Options(), References(), finish);
            Check(failure.Contains("截断") && handler.Bodies.Count == 1, finish + ": partial body stops generation without retry");
        }
        foreach (string finish in new[] { "content_filter", "tool_calls", "unknown_provider_reason" })
        {
            handler = new MemoryHandler();
            handler.Add(HttpStatusCode.OK, Reply(finish, NamedBody, null));
            GenerateFailure(handler, Options(), References(), finish);
            Check(handler.Bodies.Count == 1, finish + ": abnormal ending stops without retry");
        }
        handler = new MemoryHandler();
        handler.Add(HttpStatusCode.OK, Reply("stop", NamedBody, "refused"));
        Check(GenerateFailure(handler, Options(), References(), "refusal field").Contains("拒绝"), "refusal field stops generation");

        handler = new MemoryHandler();
        handler.Add(HttpStatusCode.OK, Reply(null, NamedBody, null));
        result = Generate(handler, Options(), null);
        Check(Property<string>(result, "DirectionStatus") == "complete" && Property<string>(result, "StatusText").Contains("未返回结束标记"), "legacy API without finish reason remains usable with explicit uncertainty");
        handler = new MemoryHandler();
        handler.Add(HttpStatusCode.OK, Reply("stop", new[] { new { type = "text", text = NamedBody } }, null));
        Check(Property<string>(Generate(handler, Options(), null), "DirectionStatus") == "complete", "text content parts parse through actual HTTP path");

        foreach (string badBody in new[] { Reply(null, null, null), "{invalid-json" })
        {
            handler = new MemoryHandler();
            handler.Add(HttpStatusCode.OK, badBody);
            GenerateFailure(handler, Options(), References(), "empty or malformed direction");
            Check(handler.Bodies.Count == 1, "empty or malformed direction stops without retry");
        }
        handler = new MemoryHandler();
        handler.Add(HttpStatusCode.OK, Reply("stop", "【画作标题】灯下裁决只有装备清单", null));
        result = Generate(handler, Options(), References());
        Check(Property<string>(result, "DirectionStatus") == "local_fallback" && Property<bool>(result, "UsedLocalFallback") && handler.Bodies.Count == 1, "usable but structurally rejected direction keeps the approved local composition fallback");

        handler = new MemoryHandler();
        handler.Add(HttpStatusCode.BadRequest, "model does not support image input");
        handler.Add(HttpStatusCode.OK, Reply("stop", NamedBody, null));
        result = Generate(handler, Options(), References());
        Check(Property<string>(result, "DirectionStatus") == "vision_unsupported" && Property<bool>(result, "VisionUnsupported") && !Property<bool>(result, "UsedLocalFallback"), "unsupported vision followed by useful text is distinctly labeled");
        Check(Property<bool>(result, "UsedTextOnlyDirector"), "vision fallback requests real-scene calibration on the image side");
        Check(handler.Bodies.Count == 2 && handler.Bodies[0].Contains("image_url") && !handler.Bodies[1].Contains("image_url") && handler.Bodies[1].Contains("本次仅提供文字"), "single permitted retry removes images and tells director no visual evidence exists");
        Check(Property<string>(result, "StatusText").Contains("不支持识图") && Property<string>(result, "FallbackReason").Length > 0, "text-only degradation has visible status and saved reason");

        handler = new MemoryHandler();
        handler.Add(HttpStatusCode.BadRequest, "unsupported vision");
        handler.Add(HttpStatusCode.OK, Reply("length", NamedBody, null));
        string visionFailure = GenerateFailure(handler, Options(), References(), "text-only retry truncated");
        Check(visionFailure.Contains("不支持图片输入") && visionFailure.Contains("截断") && handler.Bodies.Count == 2, "vision and truncation causes survive together without a third request");

        foreach (var error in new[] {
            new { Status = HttpStatusCode.ServiceUnavailable, Body = "vision upstream timeout offline-secret-sentinel" },
            new { Status = HttpStatusCode.Unauthorized, Body = "unsupported image model offline-secret-sentinel" } })
        {
            handler = new MemoryHandler();
            handler.Add(error.Status, error.Body);
            string httpFailure = GenerateFailure(handler, Options(), References(), "HTTP " + (int)error.Status);
            Check(handler.Bodies.Count == 1 && !httpFailure.Contains("offline-secret"), "generic HTTP errors do not retry or expose provider error secrets");
        }

        handler = new MemoryHandler { Timeout = true };
        string timeoutFailure = GenerateFailure(handler, Options(), References(), "transport timeout");
        Check(timeoutFailure.Contains("超时") && handler.Bodies.Count == 1, "transport timeout stops with an identified reason without retry");
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
        Check(handler.Bodies.Count == 0 && Property<string>(result, "DirectionStatus") == "local_fallback" && Property<string>(result, "FallbackReason").Contains("已关闭"), "disabled director reports local route without HTTP");
        handler = new MemoryHandler();
        options = Options();
        SetOption(options, "EnableMultimodalVision", false);
        handler.Add(HttpStatusCode.OK, Reply("stop", NamedBody, null));
        result = Generate(handler, options, References());
        Check(handler.Bodies.Count == 1 && !handler.Bodies[0].Contains("image_url") && !Property<bool>(result, "VisionUnsupported"), "intentional text-only setting is distinguished from provider degradation");
        Check(Property<bool>(result, "UsedTextOnlyDirector") && handler.Bodies[0].Contains("本次仅提供文字"), "intentional text-only director records missing visual evidence for scene routing");
        Console.WriteLine("RESULT: " + checks + " PASS / 0 FAIL");
    }
}
