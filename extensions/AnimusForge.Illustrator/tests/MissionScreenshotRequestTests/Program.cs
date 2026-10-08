// Executes the real compiled DLL. Every HTTP request is intercepted in memory; no paid/network IO.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

internal static class Program
{
    private const BindingFlags AllStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Assembly _dll;
    private static Type _options, _reference, _kind, _client, _director, _plan;
    private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = 40 * 1024 * 1024 };
    private static string _pngA, _pngB;
    private static int _checks;
    private static void Check(bool pass, string label) { if (!pass) throw new Exception(label); _checks++; }
    private static string Png(Color color)
    {
        using (var bitmap = new Bitmap(12, 12, PixelFormat.Format32bppArgb))
        using (var stream = new MemoryStream())
        { using (var graphics = Graphics.FromImage(bitmap)) graphics.Clear(color); bitmap.Save(stream, ImageFormat.Png); return Convert.ToBase64String(stream.ToArray()); }
    }
    private static void Set(object obj, string name, object value)
    {
        FieldInfo field = obj.GetType().GetField("<" + name + ">k__BackingField", AllInstance) ?? obj.GetType().GetField(name, AllInstance);
        if (field != null) field.SetValue(obj, value); else obj.GetType().GetProperty(name, AllInstance).SetValue(obj, value);
    }
    private static T Get<T>(object obj, string name) => (T)obj.GetType().GetProperty(name, AllInstance).GetValue(obj);
    private static object Options(bool chat = false, bool player2 = false)
    {
        object options = FormatterServices.GetUninitializedObject(_options);
        Set(options, "EnableImageGeneration", true); Set(options, "EnableReferenceImageForGeneration", false);
        Set(options, "PreserveEquipmentFidelity", false); Set(options, "IsMissionScreenshot", true);
        Set(options, "EnableLlmPromptExpansion", true); Set(options, "EnableMultimodalVision", false);
        Set(options, "ApiBaseUrl", "http://fixture.invalid/v1"); Set(options, "ModelName", "gpt-image-2");
        Set(options, "ApiKey", ""); Set(options, "ImageSize", "1024x1024"); Set(options, "SelectedStyle", "classic-oil");
        Set(options, "SelectedQuality", ""); Set(options, "Randomness", 100); Set(options, "PreferChatImageProtocol", chat);
        Set(options, "DirectorApiBaseUrl", "http://fixture.invalid/v1"); Set(options, "DirectorModelName", "vision-fixture"); Set(options, "DirectorApiKey", "");
        Set(options, "DirectorApiMaxTokens", 25000); Set(options, "OutputFrameRequirement", "正方形1:1，目标尺寸1024x1024");
        Set(options, "UsePlayer2ImageApi", player2); Set(options, "_player2Resolved", true);
        return options;
    }
    private static Array References(string kind = "MissionScreenshot")
    {
        Array array = Array.CreateInstance(_reference, 2);
        array.SetValue(Activator.CreateInstance(_reference, new object[] { _pngA, "截图A原始机位：保持站位距离朝向", Enum.Parse(_kind, kind) }), 0);
        array.SetValue(Activator.CreateInstance(_reference, new object[] { _pngB, "截图B反侧回望：同一冻结现场", Enum.Parse(_kind, kind) }), 1);
        return array;
    }
    private static object Await(object task)
    { ((Task)task).GetAwaiter().GetResult(); return task.GetType().GetProperty("Result").GetValue(task); }
    private static object Generate(object options, Array refs)
    {
        MethodInfo method = _client.GetMethod("GenerateImageAsync", AllStatic, null,
            new[] { typeof(string), typeof(IReadOnlyList<>).MakeGenericType(_reference), _options, typeof(CancellationToken) }, null);
        return Await(method.Invoke(null, new object[] { "【人物与镜头】冻结战斗动作\n【场景空间】现场空间\n【光影与色彩】现场采光\n【空间关系】保持相对距离与朝向", refs, options, CancellationToken.None }));
    }
    private sealed class Record { internal string Url, Text, GoogleKey; internal bool HasBearer; internal int Images; }
    private sealed class Handler : HttpMessageHandler
    {
        internal readonly List<Record> Records = new List<Record>();
        internal string Response;
        internal HttpStatusCode Status = HttpStatusCode.OK;
        internal void Reset(string response, HttpStatusCode status = HttpStatusCode.OK) { Records.Clear(); Response = response; Status = status; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            // Force a real yield to exercise the production continuations without any socket.
            await Task.Yield(); token.ThrowIfCancellationRequested();
            if (request.Method != HttpMethod.Post || (request.RequestUri.Host != "fixture.invalid" && request.RequestUri.Host != "generativelanguage.googleapis.com")) throw new Exception("Unexpected HTTP: all network prohibited");
            var record = new Record { Url = request.RequestUri.AbsolutePath, HasBearer = request.Headers.Authorization != null,
                GoogleKey = request.Headers.TryGetValues("x-goog-api-key", out var keys) ? keys.Single() : null };
            if (request.Content is MultipartFormDataContent multipart)
            {
                foreach (var part in multipart)
                {
                    string name = part.Headers.ContentDisposition.Name.Trim('"');
                    if (name == "image[]")
                    {
                        byte[] bytes = await part.ReadAsByteArrayAsync();
                        Check(bytes.Length > 0, "edits sends actual image bytes"); record.Images++;
                    }
                    else if (name == "prompt") record.Text = await part.ReadAsStringAsync();
                }
            }
            else
            {
                record.Text = await request.Content.ReadAsStringAsync();
                if (record.Url.EndsWith("/image/edit"))
                {
                    var payload = (Dictionary<string, object>)Json.DeserializeObject(record.Text);
                    record.Images = ((object[])payload["images"]).Length;
                }
                else if (record.Url.EndsWith(":generateContent")) record.Images = record.Text.Split(new[] { "\"inlineData\"" }, StringSplitOptions.None).Length - 1;
                else record.Images = record.Text.Split(new[] { "\"type\":\"image_url\"" }, StringSplitOptions.None).Length - 1;
            }
            Records.Add(record);
            return new HttpResponseMessage(Status) { Content = new StringContent(Response) };
        }
    }
    private static void ImageContracts(Handler handler)
    {
        var refs = References();
        string success = "{\"data\":[{\"b64_json\":\"" + _pngA + "\"}]}";
        foreach (string route in new[] { "edits", "chat", "player2" })
        {
            object options = Options(route == "chat", route == "player2"); handler.Reset(success);
            var result = Generate(options, refs);
            Check(Get<bool>(result, "Success"), route + " success with references disabled in ordinary settings");
            Check(handler.Records.Count == 1 && handler.Records[0].Images == 2, route + " exactly one request with two separate screenshots");
            string text = handler.Records[0].Text;
            Check(text.Contains("截图A") && text.Contains("截图B") && text.Contains("两个相反观察机位"), route + " independent A/B labels and spatial contract");
            Check(!text.Contains("不得把身份图的姿势") && !text.Contains("允许自定义服装装备"), route + " no portrait pose or free-equipment override");
            Check(text.Contains("正方形1:1"), route + " output size constraint retained");
            handler.Reset("{\"error\":{\"message\":\"image inputs not supported\"}}", HttpStatusCode.BadRequest);
            result = Generate(options, refs);
            Check(!Get<bool>(result, "Success") && handler.Records.Count == 1, route + " failed input never retries or drops images");
        }
        handler.Reset(success); var exact = Options(); Set(exact, "UseExactEndpointUrl", true); Set(exact, "ApiBaseUrl", "http://fixture.invalid/v1/images/generations");
        Check(!Get<bool>(Generate(exact, refs), "Success") && handler.Records.Count == 0, "text-only exact endpoint rejected before send");
        handler.Reset(success); var missing = Array.CreateInstance(_reference, 1); missing.SetValue(refs.GetValue(0), 0);
        Check(!Get<bool>(Generate(Options(), missing), "Success") && handler.Records.Count == 0, "missing second screenshot rejected before HTTP");
        Check(!Get<bool>(Generate(Options(), References("Character")), "Success") && handler.Records.Count == 0, "ordinary character references cannot replace screenshots");
        var damaged = References(); damaged.SetValue(Activator.CreateInstance(_reference, new object[] { "not-valid-base64", "B", Enum.Parse(_kind, "MissionScreenshot") }), 1);
        Check(!Get<bool>(Generate(Options(), damaged), "Success") && handler.Records.Count == 0, "malformed second image never silently dropped");
    }
    private static void ChatSchemaContracts(Handler handler)
    {
        string success = "{\"data\":[{\"b64_json\":\"" + _pngA + "\"}]}";
        string[] models = { "gemini-3.1-flash-image", "gemini-2.5-flash-image", "chat-image-fixture" };
        string[] sizes = { "1024x1024", "1280x720", "1024x1536" };
        string[] frames = { "正方形1:1", "横向16:9", "竖向2:3" };
        for (int i = 0; i < models.Length; i++)
        {
            object options = Options(true);
            Set(options, "IsMissionScreenshot", false); Set(options, "UseExactEndpointUrl", true);
            Set(options, "ApiBaseUrl", "http://fixture.invalid/v1/chat/completions");
            Set(options, "ModelName", models[i]); Set(options, "ImageSize", sizes[i]);
            Set(options, "OutputFrameRequirement", frames[i]); Set(options, "EnableReferenceImageForGeneration", true);
            handler.Reset(success);
            var result = Generate(options, i == 1 ? References("Character") : null);
            Check(Get<bool>(result, "Success"), models[i] + " compatible chat success");
            Check(handler.Records.Count == 1 && handler.Records[0].Url == "/v1/chat/completions", "exact chat URL and one attempt");
            var payload = (Dictionary<string, object>)Json.DeserializeObject(handler.Records[0].Text);
            Check(payload.Count == 2 && payload.ContainsKey("model") && payload.ContainsKey("messages") && !payload.ContainsKey("aspect_ratio"), "portable chat body excludes unsupported extensions");
            Check(handler.Records[0].Text.Contains(frames[i]) && Get<string>(result, "ResolvedPrompt").Contains(frames[i]), "fixed frame survives in actual request and resolved prompt");
            Check(handler.Records[0].Images == (i == 1 ? 2 : 0), "identity references are preserved");
            handler.Reset("{\"error\":{\"message\":\"Unknown name aspect_ratio\"}}", HttpStatusCode.BadRequest);
            result = Generate(options, i == 1 ? References("Character") : null);
            Check(!Get<bool>(result, "Success") && handler.Records.Count == 1 && Get<string>(result, "ErrorMessage").Contains("HTTP 400"), "schema rejection stops without a paid retry");
        }
    }
    private static void NativeGeminiContracts(Handler handler)
    {
        string success = Json.Serialize(new { candidates = new[] { new { finishReason = "STOP", content = new { parts = new object[] {
            new { thought = true, inlineData = new { mimeType = "image/png", data = _pngB } },
            new { inlineData = new { mimeType = "image/png", data = _pngA } } } } } } });
        string endpoint = "/v1beta/models/gemini-3.1-flash-image:generateContent";
        string[] sizes = { "1024x1024", "1280x720", "1344x768", "1024x1536" };
        string[] ratios = { "1:1", "16:9", null, "2:3" };
        for (int i = 0; i < sizes.Length; i++)
        {
            object options = Options(); Set(options, "IsMissionScreenshot", false); Set(options, "PreferChatImageProtocol", false);
            Set(options, "UseExactEndpointUrl", i != 1); Set(options, "ModelName", "gemini-3.1-flash-image");
            Set(options, "ApiBaseUrl", i == 1 ? "https://generativelanguage.googleapis.com/v1beta" : "http://fixture.invalid" + endpoint);
            Set(options, "ApiKey", "native-fixture-key"); Set(options, "ImageSize", sizes[i]);
            Set(options, "OutputFrameRequirement", "本次尺寸" + sizes[i]); Set(options, "EnableReferenceImageForGeneration", true);
            Array refs = i == 0 ? null : References("Character");
            handler.Reset(success); var result = Generate(options, refs);
            Check(Get<bool>(result, "Success") && handler.Records.Count == 1, "native request succeeds in one attempt");
            Record sent = handler.Records.Single();
            Check(sent.Url == endpoint && sent.GoogleKey == "native-fixture-key" && !sent.HasBearer, "native path and API-key header only");
            var payload = (Dictionary<string, object>)Json.DeserializeObject(sent.Text);
            Check(payload.Count == 2 && payload.ContainsKey("contents") && payload.ContainsKey("generationConfig")
                && !payload.ContainsKey("messages") && !payload.ContainsKey("aspect_ratio") && !sent.Text.Contains("native-fixture-key"), "native body excludes Chat schema and credentials");
            var config = (Dictionary<string, object>)payload["generationConfig"];
            Check(((object[])config["responseModalities"]).Contains("IMAGE"), "native image modality requested");
            Check(ratios[i] == null ? !config.ContainsKey("imageConfig")
                : (string)((Dictionary<string, object>)config["imageConfig"])["aspectRatio"] == ratios[i], "only exact supported ratio is structured");
            Check(sent.Images == (refs == null ? 0 : 2), "all native references retained");
            Check(sent.Text.Contains("本次尺寸" + sizes[i]) && Get<string>(result, "ResolvedPrompt").Contains(sizes[i]), "native fixed output requirement retained");
            Check(Convert.ToBase64String(Get<byte[]>(result, "ImageBytes")) == _pngA, "thought image skipped; final image decoded");
            handler.Reset("{\"error\":{\"message\":\"invalid API key\"}}", HttpStatusCode.Unauthorized);
            result = Generate(options, refs);
            Check(!Get<bool>(result, "Success") && handler.Records.Count == 1 && Get<string>(result, "ErrorMessage").Contains("HTTP 401"), "native auth failure never retries");
            handler.Reset("{\"promptFeedback\":{\"blockReason\":\"SAFETY\"}}"); result = Generate(options, refs);
            Check(!Get<bool>(result, "Success") && handler.Records.Count == 1 && Get<string>(result, "ErrorMessage").Contains("过滤"), "native blocked response clear and no retry");
        }
        object invalid = Options(); Set(invalid, "IsMissionScreenshot", false); Set(invalid, "UseExactEndpointUrl", true);
        Set(invalid, "ApiBaseUrl", "https://generativelanguage.googleapis.com/v1beta"); handler.Reset(success);
        Check(!Get<bool>(Generate(invalid, null), "Success") && handler.Records.Count == 0, "exact root rejected before network");
        Set(invalid, "ApiBaseUrl", "http://fixture.invalid/v1beta/models/gemini:streamGenerateContent");
        Check(!Get<bool>(Generate(invalid, null), "Success") && handler.Records.Count == 0, "streaming native image URL rejected before send");
        Set(invalid, "ApiBaseUrl", "http://fixture.invalid" + endpoint); Set(invalid, "IsCurrentImageEdit", true);
        var editReference = Array.CreateInstance(_reference, 1);
        editReference.SetValue(Activator.CreateInstance(_reference, new object[] { _pngA, "当前成图", Enum.Parse(_kind, "GeneratedImage") }), 0);
        handler.Reset(success); var editResult = Generate(invalid, editReference);
        Check(Get<bool>(editResult, "Success") && handler.Records.Count == 1 && handler.Records[0].Images == 1, "native current-image edit retains exactly one base image");
    }
    private static void NativeProtocolContracts()
    {
        Type protocol = _dll.GetType("AnimusForge.Illustrator.Core.GeminiNativeImageProtocol", true);
        Type jsonObject = Type.GetType("Newtonsoft.Json.Linq.JObject, Newtonsoft.Json", true);
        Func<string, object> parse = text => jsonObject.GetMethod("Parse", new[] { typeof(string) }).Invoke(null, new object[] { text });
        MethodInfo isNative = protocol.GetMethod("IsNativeUrl", AllStatic);
        Check(!(bool)isNative.Invoke(null, new object[] { "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions" }), "Google compatible Chat keeps Bearer schema");
        Check(!(bool)isNative.Invoke(null, new object[] { "http://fixture.invalid/v1/images/edits" }), "edits never treated as native Gemini");
        foreach (string input in new[] { "https://generativelanguage.googleapis.com/v1beta", "https://generativelanguage.googleapis.com/v1beta/models/gemini:generateContent" })
            using (var request = (HttpRequestMessage)protocol.GetMethod("CreateModelListRequest", AllStatic).Invoke(null, new object[] { input, "native-fixture-key" }))
            {
                Check(request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath == "/v1beta/models", "native catalog root/model endpoint resolves correctly");
                Check(request.Headers.Authorization == null && request.Headers.GetValues("x-goog-api-key").Single() == "native-fixture-key", "native catalog key header only");
            }
        var models = (List<string>)protocol.GetMethod("ReadModelIds", AllStatic).Invoke(null, new[] { parse("{\"models\":[{\"name\":\"models/gemini-image\"},{\"name\":\"gemini-pro\"},{}]}") });
        Check(models.SequenceEqual(new[] { "gemini-image", "gemini-pro" }), "native catalog IDs read without models prefix");
        Type diagnostic = _dll.GetType("AnimusForge.Illustrator.Core.GenerationDiagnostics", true);
        object scope = FormatterServices.GetUninitializedObject(diagnostic);
        Set(scope, "_gate", new object()); Set(scope, "_secrets", new List<string>());
        Set(scope, "_disposed", true); Set(scope, "_references", Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), jsonObject)));
        object inputData = parse(Json.Serialize(new { inlineData = new { mimeType = "image/png", data = _pngA }, @secret = "fake-secret" }));
        MethodInfo sanitize = diagnostic.GetMethod("Sanitize", AllInstance);
        string cleanedResponse = sanitize.Invoke(scope, new object[] { inputData, false }).ToString();
        string cleanedRequest = sanitize.Invoke(scope, new object[] { inputData, true }).ToString();
        Check(!cleanedResponse.Contains(_pngA) && !cleanedResponse.Contains("fake-secret"), "native inline response and key sanitized");
        Check(!cleanedRequest.Contains(_pngA) && cleanedRequest.Contains("reference"), "native reference recorded as metadata rather than raw base64");
        string header = sanitize.Invoke(scope, new[] { parse("{\"x-goog-api-key\":\"fake-key\"}"), (object)false }).ToString();
        Check(!header.Contains("fake-key") && header.Contains("redacted"), "native key name redacted in diagnostics");
    }
    private static object Direction(object options, Array refs, HttpClient client)
    {
        object plan = Activator.CreateInstance(_plan, new object[] { "场景喊话双截图插画", "玩家与目标相距2米。", "保持相对站位距离朝向。", "玩家：对白完整原文。\nNPC甲：动作已经发生。\nNPC乙：完整多人回应。" });
        MethodInfo method = _director.GetMethod("CreateDirectionWithClientAsync", AllStatic);
        return Await(method.Invoke(null, new[] { plan, (object)refs, options, client, CancellationToken.None }));
    }
    private static void RejectDirection(object options, Array refs, HttpClient client, string name)
    { try { Direction(options, refs, client); } catch (InvalidOperationException) { _checks++; return; } throw new Exception("Must fail: " + name); }
    private static void DirectorContracts(Handler handler, HttpClient http)
    {
        string direction = "【画作标题】现场交锋\n【画作主题】人物与关系\n【人物行动】玩家朝向目标举手，目标正抬手回应。\n" +
            "【人物与镜头】玩家朝向目标自然举手，选择侧面中景呈现双方回应。\n【场景空间】沿用两张截图共同确认的墙壁、门窗和地面材质布局。\n" +
            "【光影与色彩】两图现场暖光统一照亮人物与衣甲，保持固有颜色。\n【空间关系】玩家与目标保持两米距离和身体朝向，遮挡支撑服从现场。";
        Func<string, string> reply = text => Json.Serialize(new { choices = new[] { new { message = new { content = text }, finish_reason = "stop" } } });
        handler.Reset(reply(direction)); object options = Options();
        object result = Direction(options, References(), http);
        Check(!Get<bool>(result, "UsedLocalFallback") && handler.Records.Count == 1 && handler.Records[0].Images == 2, "vision director mandatory and receives exactly two screenshots");
        string request = handler.Records[0].Text;
        Check(request.Contains("完整多人回应") && request.Contains("对白完整原文") && request.Contains("只有对白明确发生的空间变化"), "full frozen dialogue and action priority reach director");
        Check(!request.Contains("允许自定义服装装备"), "screenshot identities remain fixed with ordinary equipment switch off");
        string finalPrompt = Get<string>(result, "Prompt");
        Check(!finalPrompt.Contains("对白完整原文") && finalPrompt.Contains("玩家与目标相距2米")
            && finalPrompt.Contains("玩家镜头水平方向") && finalPrompt.Contains("前后平视机位")
            && finalPrompt.Contains("参考图的居中与透视只是采集构图，不是最终画面的固定要求")
            && finalPrompt.Contains("没有一张图指定最终主视角") && finalPrompt.Contains("自行推演机位、景别和构图"),
            "final image uses direction/facts/contract, never raw dialogue or forced player centering");
        handler.Reset(reply("")); RejectDirection(options, References(), http, "empty director output");
        Check(handler.Records.Count == 1, "empty director fails without local substitute or retry");
        handler.Reset(reply("【人物与镜头】人物挥手。")); RejectDirection(options, References(), http, "incomplete director output");
        Check(handler.Records.Count == 1, "incomplete director fails single attempt");
        handler.Reset("{\"error\":{\"message\":\"image_url not supported\"}}", HttpStatusCode.BadRequest);
        RejectDirection(options, References(), http, "vision unsupported"); Check(handler.Records.Count == 1, "unsupported vision never resends text-only");
        handler.Reset(reply(direction)); Set(options, "EnableLlmPromptExpansion", false);
        RejectDirection(options, References(), http, "disabled director"); Check(handler.Records.Count == 0, "disabled director rejected before request");
    }
    private static void DialogueContracts()
    {
        Type history = _dll.GetType("AnimusForge.SceneConversationHistoryOwner", true);
        Type message = _dll.GetType("AnimusForge.ConversationMessage", true);
        object owner = Activator.CreateInstance(history, AllInstance, null, new object[] { new object() }, null);
        Action<string, string, string> append = (role, content, name) => {
            object row = Activator.CreateInstance(message); Set(row, "Role", role); Set(row, "Content", content); Set(row, "SpeakerName", name);
            history.GetMethod("AppendPublic", AllInstance).Invoke(owner, new[] { row });
        };
        append("user", "旧轮玩家", "玩家"); append("assistant", "旧轮回应", "NPC甲");
        append("user", "第二轮玩家", "玩家"); append("assistant", new string('甲', 2400), "NPC甲"); append("assistant", "第二轮多人回应", "NPC乙");
        append("user", "最近轮玩家", "玩家"); append("assistant", "最近轮回应", "NPC丙"); append("system", "AFEF已发生事实", null);
        MethodInfo capture = history.GetMethod("CaptureIllustrationDialogue", AllInstance);
        string snapshot = (string)capture.Invoke(owner, null);
        Check(!snapshot.Contains("旧轮") && snapshot.Contains("第二轮玩家") && snapshot.Contains("最近轮玩家"), "exact latest two sent player rounds");
        Check(snapshot.Contains(new string('甲', 2400)) && snapshot.Contains("NPC乙：第二轮多人回应") && snapshot.Contains("NPC丙：最近轮回应"), "all full multi-NPC replies retained");
        Check(snapshot.Contains("AFEF已发生事实"), "confirmed fact line retained");
        append("assistant", "晚到下一条回应", "NPC丁"); Check(!snapshot.Contains("晚到") && ((string)capture.Invoke(owner, null)).Contains("晚到"), "click snapshot immutable after source history changes");
        history.GetMethod("Reset", AllInstance).Invoke(owner, null);
        Check((string)capture.Invoke(owner, null) == "", "new mission history empty after owner reset");
    }

    private static void PresentationContracts()
    {
        var owner = _dll.GetType("AnimusForge.Illustrator.Core.MissionScreenshotIllustration", true);
        var shout = _dll.GetType("AnimusForge.ShoutBehavior", true);
        owner.GetMethod("Install", AllStatic).Invoke(null, null);
        var busy = (Func<bool>)shout.GetField("SceneIllustrationBusyHook", AllStatic).GetValue(null);
        shout.GetField("SceneIllustrationAvailableHook", AllStatic).SetValue(null, (Func<bool>)(() => true));
        int galleryCalls = 0, newRequests = 0;
        shout.GetField("SceneIllustrationGalleryHook", AllStatic).SetValue(null, (Action)(() => galleryCalls++));
        var scopeField = owner.GetField("_scope", AllStatic);
        var captureField = owner.GetField("_capture", AllStatic);
        scopeField.SetValue(null, FormatterServices.GetUninitializedObject(scopeField.FieldType));
        var inputType = _dll.GetType("AnimusForge.ShoutTextInputPopupVM", true);
        var input = FormatterServices.GetUninitializedObject(inputType);
        Set(input, "_allowIllustration", true); Set(input, "_onIllustration", (Action)(() => newRequests++));
        Check(!busy() && Get<bool>(input, "CanIllustrate"), "background scene generation no longer disables another screenshot");
        inputType.GetMethod("ExecuteIllustrate").Invoke(input, null);
        Check(newRequests == 1, "scene input admits another request while previous scope exists");
        var capture = FormatterServices.GetUninitializedObject(captureField.FieldType);
        var doneField = captureField.FieldType.GetField("_done", AllInstance);
        var done = Activator.CreateInstance(doneField.FieldType);
        doneField.SetValue(capture, done); captureField.SetValue(null, capture);
        Check(busy() && !Get<bool>(input, "CanIllustrate"), "only native screenshot acquisition holds camera gate");
        inputType.GetMethod("ExecuteOpenGallery").Invoke(input, null);
        Check(galleryCalls == 1, "gallery command is independent of image busy state");
        doneField.FieldType.GetMethod("SetResult").Invoke(done, new object[] { null });
        Check(!busy() && Get<bool>(input, "CanIllustrate"), "finished screenshot unlocks input before network finishes");
        var sessionType = _dll.GetType("AnimusForge.DialogueUI.Scene.SceneSessionVM", true);
        var session = FormatterServices.GetUninitializedObject(sessionType);
        sessionType.GetMethod("ExecuteOpenGallery").Invoke(session, null);
        ((Action)sessionType.GetField("_pending", AllInstance).GetValue(session)).Invoke();
        Check(galleryCalls == 2, "persistent session gallery uses same installed callback");
        scopeField.SetValue(null, null); captureField.SetValue(null, null);
        Type vm = _dll.GetType("AnimusForge.AnimusForgeNativeConversationOverlayVM", true);
        bool enabled = true; int calls = 0;
        object host = Activator.CreateInstance(vm, new object[] { null, null, null, null, null, null,
            (Func<bool>)(() => enabled), (Action)(() => calls++) });
        Check(Get<bool>(host, "IsIllustrationAvailable"), "original UI illustration available without skin dependency");
        vm.GetMethod("ShowIllustration").Invoke(host, null);
        Check(calls == 1, "original UI command reaches supplied callback once");
        enabled = false; vm.GetMethod("ShowIllustration").Invoke(host, null);
        Check(!Get<bool>(host, "IsIllustrationAvailable") && calls == 1, "disabled illustrator cannot execute from original UI");
        Type card = _dll.GetType("AnimusForge.Illustrator.UI.Overlays.IllustrationCardVM", true);
        object readOnlyCard = Activator.CreateInstance(card, new object[] { (Action)(() => calls++), null, null, null });
        card.GetMethod("SetLoading").Invoke(readOnlyCard, new object[] { "正在导演推演" });
        Check(Get<bool>(readOnlyCard, "IsLoading") && !Get<bool>(readOnlyCard, "CanRegenerate"), "joined screenshot progress disables duplicate generation");
        card.GetMethod("SetReady").Invoke(readOnlyCard, new object[] { "生图失败：测试错误" });
        Check(!Get<bool>(readOnlyCard, "IsLoading") && Get<string>(readOnlyCard, "StatusText").Contains("测试错误"), "error ends busy presentation with explicit reason");
        Check(!Get<bool>(readOnlyCard, "CanRegenerate") && !Get<bool>(readOnlyCard, "CanRegenerateWithPrompt"), "subscriber card cannot initiate unowned generation");
        object normalCard = Activator.CreateInstance(card, new object[] { null, (Action)(() => calls++), null, null });
        Check(Get<bool>(normalCard, "CanRegenerate"), "existing manual card retains regenerate");
        card.GetMethod("SetLoading").Invoke(normalCard, new object[] { "忙碌" });
        Check(!Get<bool>(normalCard, "CanRegenerate"), "normal card blocks regenerate while busy");
        Type diagnostics = _dll.GetType("AnimusForge.Illustrator.Core.GenerationDiagnostics", true);
        var names = (System.Text.RegularExpressions.Regex)diagnostics.GetField("RecordName", AllStatic).GetValue(null);
        Check(names.IsMatch("general_1008-062342_abcd") && names.IsMatch("conversation_1008-062342_abcd")
            && !names.IsMatch("unbounded_unknown_1008-062342_abcd"), "new screenshot diagnostics participate in existing bounded retention");
    }
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length < 2) throw new Exception("Usage: exe DLL referenceDir [dependencyDir...]");
            using (var resolver = new OfflineAssemblyResolver(args.Skip(1).Prepend(Path.GetDirectoryName(Path.GetFullPath(args[0]))).ToArray()))
            {
                _dll = Assembly.LoadFrom(Path.GetFullPath(args[0])); string core = "AnimusForge.Illustrator.Core.";
                _options = _dll.GetType(core + "IllustrationOptions", true); _reference = _dll.GetType(core + "IllustrationReferenceImage", true);
                _kind = _dll.GetType(core + "IllustrationReferenceKind", true); _client = _dll.GetType(core + "UniversalOpenAiImageClient", true);
                _director = _dll.GetType(core + "VisualDirectorEngine", true); _plan = _dll.GetType(core + "IllustrationPromptPlan", true);
                _pngA = Png(Color.Red); _pngB = Png(Color.Blue);
                var handler = new Handler();
                FieldInfo field = _client.GetField("HttpClient", AllStatic); var previous = (HttpClient)field.GetValue(null);
                using (var http = new HttpClient(handler))
                {
                    field.SetValue(null, http);
                    try { ImageContracts(handler); ChatSchemaContracts(handler); NativeGeminiContracts(handler); NativeProtocolContracts(); DirectorContracts(handler, http); DialogueContracts(); PresentationContracts(); }
                    finally { field.SetValue(null, previous); }
                }
            }
            Console.WriteLine("PASS " + _checks + " actual-DLL assertions; Edits/Chat/Player2/director/history; every HTTP in-memory, no external API."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
