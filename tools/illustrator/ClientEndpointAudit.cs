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

// Exercises production methods from the supplied DLL. All POSTs are intercepted in memory,
// and all successful responses contain local base64 fixtures, never remote image URLs.
public static class ClientEndpointAudit
{
    private const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private static int checks;
    private static Type client;
    private static Type optionsType;
    private static Type referenceType;
    private static Type referenceKind;
    private static MethodInfo generate;
    private static string png;

    private static void Check(bool pass, string name)
    {
        if (!pass) throw new Exception("FAIL " + name);
        checks++;
        Console.WriteLine("PASS " + name);
    }

    private static object Call(Type type, string method, params object[] args)
    { return type.GetMethod(method, Static).Invoke(null, args); }

    private static T Property<T>(object value, string name)
    { return (T)value.GetType().GetProperty(name).GetValue(value, null); }

    private static void SetOption(object options, string property, object value)
    {
        optionsType.GetField("<" + property + ">k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(options, value);
    }

    private static object Options(string url, bool exact, string model, string style)
    {
        var options = FormatterServices.GetUninitializedObject(optionsType);
        SetOption(options, "EnableImageGeneration", true);
        SetOption(options, "EnableReferenceImageForGeneration", true);
        SetOption(options, "ApiBaseUrl", url);
        SetOption(options, "ApiKey", "");
        SetOption(options, "ModelName", model);
        SetOption(options, "ImageSize", "1024x1024");
        SetOption(options, "SelectedQuality", "");
        SetOption(options, "SelectedStyle", style);
        SetOption(options, "CustomStylePrompt", "CUSTOM_STYLE_SENTINEL\n用户第二行");
        SetOption(options, "NegativePrompt", "CUSTOM_NEGATIVE_SENTINEL");
        SetOption(options, "UseExactEndpointUrl", exact);
        SetOption(options, "PreferChatImageProtocol", false);
        SetOption(options, "Randomness", 0);
        return options;
    }

    private static Array References(string data)
    {
        var references = Array.CreateInstance(referenceType, 3);
        string[] kinds = { "Character", "Emblem", "Scene" };
        string[] labels = { "IDENTITY_LABEL：可见发型与衣服", "EMBLEM_LABEL：仅作徽记样图", "SCENE_LABEL：实际环境" };
        for (int i = 0; i < references.Length; i++)
            references.SetValue(Activator.CreateInstance(referenceType, new object[] { data, labels[i], Enum.Parse(referenceKind, kinds[i]) }), i);
        return references;
    }

    private static object Generate(object options, Array references)
    {
        var task = (Task)generate.Invoke(null, new object[] { "DIRECTOR_BODY_SENTINEL 人物正在阅读信件。", references, options, CancellationToken.None });
        task.GetAwaiter().GetResult();
        return task.GetType().GetProperty("Result").GetValue(task, null);
    }

    private sealed class RequestRecord
    {
        public string Url;
        public string MediaType;
        public string Json;
        public readonly Dictionary<string, string> Fields = new Dictionary<string, string>();
        public int Images;
    }

    private sealed class Reply
    {
        public HttpStatusCode Status;
        public string Body;
        public Reply(HttpStatusCode status, string body) { Status = status; Body = body; }
    }

    private sealed class MemoryHandler : HttpMessageHandler
    {
        public readonly List<RequestRecord> Requests = new List<RequestRecord>();
        public readonly Queue<Reply> Replies = new Queue<Reply>();
        public void Reset(params Reply[] replies)
        {
            Requests.Clear(); Replies.Clear();
            foreach (var reply in replies) Replies.Enqueue(reply);
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var record = new RequestRecord { Url = request.RequestUri.OriginalString, MediaType = request.Content.Headers.ContentType.MediaType };
            Requests.Add(record);
            var multipart = request.Content as MultipartFormDataContent;
            if (multipart != null)
            {
                foreach (var part in multipart)
                {
                    string name = part.Headers.ContentDisposition.Name.Trim('"');
                    if (name == "image[]") record.Images++;
                    else record.Fields[name] = await part.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            else record.Json = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (Replies.Count == 0) throw new InvalidOperationException("Unexpected extra HTTP request; in-memory handler never performs network IO.");
            var reply = Replies.Dequeue();
            return new HttpResponseMessage(reply.Status) { Content = new StringContent(reply.Body) };
        }
    }

    private static Reply Success()
    { return new Reply(HttpStatusCode.OK, "{\"data\":[{\"b64_json\":\"" + png + "\"}]}"); }

    private static Reply Unsupported()
    { return new Reply(HttpStatusCode.NotFound, "{\"error\":{\"message\":\"unsupported endpoint\"}}"); }

    private static Reply NeedsChat()
    { return new Reply(HttpStatusCode.BadRequest, "{\"error\":{\"message\":\"field messages is required\"}}"); }

    private static string RequestText(RequestRecord request)
    {
        var payload = ParseJson(request.Json);
        if (!payload.ContainsKey("messages")) return (string)payload["prompt"];
        var messages = (object[])payload["messages"];
        var content = ((Dictionary<string, object>)messages[0])["content"];
        if (content is string) return (string)content;
        return string.Join(Environment.NewLine, ((object[])content).Cast<Dictionary<string, object>>().Where(p => (string)p["type"] == "text").Select(p => (string)p["text"]));
    }

    private static Dictionary<string, object> ParseJson(string json)
    { return (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(json); }

    private static string OptionalText(Dictionary<string, object> json, string key)
    { object value; return json.TryGetValue(key, out value) ? value as string : null; }

    private static void CheckCapturedPrompt(object result, RequestRecord request, string context)
    {
        string expected = request.MediaType == "multipart/form-data" ? request.Fields["prompt"] : RequestText(request);
        string actual = Property<string>(result, "ResolvedPrompt");
        Check(actual == expected, context + " returns exactly the transmitted text");
        Check(!actual.Contains(png) && !actual.Contains("data:image/"), context + " excludes reference image base64");
    }

    private static void CheckStyles(Assembly assembly, MemoryHandler handler)
    {
        var styles = assembly.GetType("AnimusForge.Illustrator.Core.IllustrationStylePresets", true);
        var director = assembly.GetType("AnimusForge.Illustrator.Core.VisualDirectorEngine", true);
        var oil = Call(styles, "Resolve", "classic-oil", "ignored");
        string oilDirector = Property<string>(oil, "DirectorPrompt");
        string oilImage = Property<string>(oil, "ImagePrompt");
        Check(oilDirector.Length > oilImage.Length * 2 && oilImage.Length < 150, "oil director receives detailed painting guidance and image endpoint a compact anchor");
        Check(oilDirector.Contains("光源方向、昼夜、天气和物体固有色服从现场事实") && oilDirector.Contains("接触阴影、环境反光和遮挡关系"), "oil painting guidance preserves actual light colors and spatial integration");
        Check(oilImage.Contains("人物与环境统一绘制") && !oilImage.Contains("8k") && !oilImage.Contains("戏剧性光影微光"), "oil anchor removes generic quality inflation and compulsory dim light");
        Check((string)Call(director, "BuildDirectorStylePreference", new object[] { null }) == oilDirector, "director default resolves shared detailed oil preset");
        Check((string)Call(director, "BuildImageStylePreference", new object[] { null }) == oilImage, "local image fallback resolves shared compact oil preset");
        Check(object.ReferenceEquals(oil, Call(styles, "Resolve", null, "ignored")) && object.ReferenceEquals(oil, Call(styles, "Resolve", "unknown", "ignored")), "unknown and missing presets reuse safe cached oil default");

        string customText = "  用户手写画风\n自定义第二行  ";
        var custom = Call(styles, "Resolve", "custom", customText);
        Check(Property<string>(custom, "DirectorPrompt") == customText && Property<string>(custom, "ImagePrompt") == customText && Property<bool>(custom, "IsCustom"), "custom style is passed unchanged to both stages");
        var emptyCustom = Call(styles, "Resolve", "custom", "");
        Check(Property<string>(emptyCustom, "DirectorPrompt") == "" && Property<string>(emptyCustom, "ImagePrompt") == "", "intentional empty custom style does not become oil");

        string[] names = { "classic-oil", "dark-epic", "cinematic", "mosan-art", "vivid", "natural", "custom" };
        string[] imagePresets = {
            oilImage,
            "暗黑史诗写实, dark epic realism, grim medieval war chronicle, dramatic chiaroscuro, painterly oil texture",
            "电影级光影, cinematic film still, anamorphic composition, movie-grade dramatic lighting and color grading",
            "莫桑艺术, 默兹河流域12世纪罗马式珐琅与手抄本彩饰风格, 景泰蓝式宝石级饱和平涂色块, 金色勾边与装饰性边框纹样, 拉长端庄的程式化人物造型, 浓重黑色轮廓线, 平面化叙事构图, Mosan art, Romanesque manuscript illumination, champleve enamel, jewel-like saturated flat colors, gold outlines, decorative borders",
            null, null, "CUSTOM_STYLE_SENTINEL\n用户第二行"
        };
        string[] negativePresets = {
            "2d flat vector art, cheap cel-shading, lineart sketch, anime, cartoon, 卡通, 动漫风",
            "bright cheerful colors, cartoon, anime, cel shading, clean untarnished surfaces",
            "flat lighting, washed-out colors, cartoon, anime, cluttered composition",
            "photorealism, soft gradients, photographic lighting, cartoon, anime, 摄影光影",
            "dull colors, washed out, cartoon, anime", "oversaturated, cartoon, anime", null
        };
        string[] directorPresets = {
            oilDirector,
            "暗黑史诗写实，沉郁色调与中世纪凝重历史氛围",
            "电影化叙事光影与镜头语言，光照服从现场时间和环境",
            "莫桑艺术（默兹河流域罗马式珐琅与手抄本彩饰）：景泰蓝式宝石级饱和平涂色块、金色勾边、装饰性边框纹样、拉长端庄的程式化人物、浓重黑色轮廓线、平面化叙事构图",
            "色彩鲜明、叙事清晰，材质与空间层次丰富可信",
            "自然写实、克制可信、材质与环境色彩真实",
            "CUSTOM_STYLE_SENTINEL\n用户第二行"
        };
        for (int i = 0; i < names.Length; i++)
        {
            var preset = Call(styles, "Resolve", names[i], imagePresets[i]);
            Check(Property<string>(preset, "ImagePrompt") == imagePresets[i] && Property<string>(preset, "NegativePrompt") == negativePresets[i], "preset image and negative text remain defined: " + names[i]);
            Check(Property<string>(preset, "DirectorPrompt") == directorPresets[i] && Property<bool>(preset, "IsCustom") == (names[i] == "custom"), "preset director wording and custom flag remain defined: " + names[i]);
            bool native = names[i] == "vivid" || names[i] == "natural";
            Check(Property<string>(preset, "ApiStyle") == (native ? names[i] : null), "only native style enums enter API fields: " + names[i]);
            handler.Reset(Success());
            var options = Options("http://offline.invalid/v1/images/generations", true, "audit", names[i]);
            Check((string)Call(director, "BuildDirectorStylePreference", options) == directorPresets[i], "actual director route uses shared selected style: " + names[i]);
            var result = Generate(options, null);
            Check(Property<bool>(result, "Success") && handler.Requests.Count == 1, "preset generation succeeds in memory: " + names[i]);
            var payload = ParseJson(handler.Requests[0].Json);
            string prompt = (string)payload["prompt"];
            Check(OptionalText(payload, "style") == (native ? names[i] : null), "actual JSON style enum follows selected preset: " + names[i]);
            Check(native ? !prompt.Contains("古典写实历史油画") : prompt.Contains(imagePresets[i]), "actual image prompt uses selected style without forced oil: " + names[i]);
            Check(prompt.Contains("CUSTOM_NEGATIVE_SENTINEL") == (names[i] == "custom"), "custom negative text applies only to custom preset: " + names[i]);
            if (negativePresets[i] != null) Check(prompt.Contains(negativePresets[i]), "actual image prompt retains preset negatives: " + names[i]);
        }
    }

    public static void Run(string dllPath)
    {
        checks = 0;
        var assembly = Assembly.LoadFrom(dllPath);
        string core = "AnimusForge.Illustrator.Core.";
        client = assembly.GetType(core + "UniversalOpenAiImageClient", true);
        optionsType = assembly.GetType(core + "IllustrationOptions", true);
        referenceType = assembly.GetType(core + "IllustrationReferenceImage", true);
        referenceKind = assembly.GetType(core + "IllustrationReferenceKind", true);
        generate = client.GetMethod("GenerateImageAsync", Static, null, new[] { typeof(string), typeof(IReadOnlyList<>).MakeGenericType(referenceType), optionsType, typeof(CancellationToken) }, null);
        using (var bitmap = new Bitmap(24, 24))
        using (var buffer = new MemoryStream())
        {
            for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++) bitmap.SetPixel(x, y, Color.FromArgb(255, x * 10, y * 10, (x + y) * 5));
            bitmap.Save(buffer, ImageFormat.Png);
            png = Convert.ToBase64String(buffer.ToArray());
        }
        var references = References(png);
        var httpField = client.GetField("HttpClient", Static);
        var original = (HttpClient)httpField.GetValue(null);
        var handler = new MemoryHandler();
        using (var http = new HttpClient(handler))
        {
            httpField.SetValue(null, http);
            try
            {
                string exactEditUrl = "http://offline.invalid/custom/v1/images/edits?route=keep%2Fvalue&tail=slash/";
                var editOptions = Options(exactEditUrl, true, "gemini-chat-image", "classic-oil");
                SetOption(editOptions, "PreferChatImageProtocol", true);
                handler.Reset(Success());
                var edit = Generate(editOptions, references);
                Check(Property<bool>(edit, "Success") && handler.Requests.Count == 1, "exact Edits succeeds in one request despite Chat model name and preference");
                Check(handler.Requests[0].Url == exactEditUrl && handler.Requests[0].MediaType == "multipart/form-data", "exact Edits preserves full URL query and multipart protocol");
                Check(handler.Requests[0].Images == 3 && !handler.Requests[0].Fields.ContainsKey("mask"), "exact Edits uploads all references without synthetic mask");
                CheckCapturedPrompt(edit, handler.Requests[0], "exact Edits");
                Check(handler.Requests[0].Fields["prompt"].Contains("人物身份参考：") && handler.Requests[0].Fields["prompt"].Contains("纹章样图：") && handler.Requests[0].Fields["prompt"].Contains("现场参考："), "exact Edits preserves each reference role");

                handler.Reset(Unsupported());
                var failedEdit = Generate(editOptions, references);
                Check(!Property<bool>(failedEdit, "Success") && handler.Requests.Count == 1 && handler.Requests[0].Url == exactEditUrl, "exact Edits failure never falls back to another endpoint");
                CheckCapturedPrompt(failedEdit, handler.Requests[0], "failed exact Edits");

                foreach (Array missing in new[] { (Array)null, Array.CreateInstance(referenceType, 0), Array.CreateInstance(referenceType, 2), References("not valid base64") })
                {
                    handler.Reset();
                    var noReference = Generate(editOptions, missing);
                    Check(!Property<bool>(noReference, "Success") && handler.Requests.Count == 0 && !string.IsNullOrWhiteSpace(Property<string>(noReference, "ErrorMessage")), "exact Edits rejects absent or unusable references locally");
                }
                SetOption(editOptions, "EnableReferenceImageForGeneration", false);
                handler.Reset();
                var disabled = Generate(editOptions, references);
                Check(!Property<bool>(disabled, "Success") && handler.Requests.Count == 0, "exact Edits obeys disabled references without a paid request");
                string slashUrl = "http://offline.invalid/V1/IMAGES/EDITS/?version=kept";
                handler.Reset(Success());
                var trailingSlash = Generate(Options(slashUrl, true, "audit", "classic-oil"), references);
                Check(Property<bool>(trailingSlash, "Success") && handler.Requests[0].Url == slashUrl && handler.Requests[0].MediaType == "multipart/form-data", "case and trailing slash recognition preserves exact Edits URL");

                var chatOptions = Options("http://offline.invalid/v1/chat/completions?route=chat", true, "audit", "classic-oil");
                handler.Reset(Success());
                var chat = Generate(chatOptions, references);
                Check(Property<bool>(chat, "Success") && handler.Requests.Count == 1, "Chat succeeds with reference images");
                CheckCapturedPrompt(chat, handler.Requests[0], "Chat with references");
                string chatText = Property<string>(chat, "ResolvedPrompt");
                Check(chatText.Contains("核心人物官方真实视觉基准图") && chatText.Contains("IDENTITY_LABEL") && chatText.Contains("家族纹章图案样板") && chatText.Contains("当前位置与环境定位参考") && chatText.Contains("最终呈现规范/Artistic Redraw"), "Chat cache includes all identity role scene and redraw text");
                Check(chatText.Contains("DIRECTOR_BODY_SENTINEL") && chatText.Contains("焦点精细、次要区域笔触简练") && !chatText.Contains("视觉焦点集中于人物面部"), "Chat sends director body and compact style without detailed director-only style");
                var chatMessage = (Dictionary<string, object>)((object[])ParseJson(handler.Requests[0].Json)["messages"])[0];
                var chatContent = (object[])chatMessage["content"];
                Check(chatContent.Cast<Dictionary<string, object>>().Count(p => (string)p["type"] == "image_url") == 3, "Chat actually sends the three image parts omitted from cached text");

                handler.Reset(Success());
                var textChat = Generate(chatOptions, null);
                Check(Property<bool>(textChat, "Success"), "Chat without references succeeds");
                CheckCapturedPrompt(textChat, handler.Requests[0], "text-only Chat");
                handler.Reset(new Reply(HttpStatusCode.OK, "{\"choices\":[{\"message\":{\"content\":null},\"finish_reason\":null}]}"));
                var emptyChat = Generate(chatOptions, references);
                Check(!Property<bool>(emptyChat, "Success") && handler.Requests.Count == 1, "empty Chat response does not trigger another paid request");
                CheckCapturedPrompt(emptyChat, handler.Requests[0], "empty Chat response");

                handler.Reset(Unsupported());
                var unsupportedEdit = Generate(Options("http://offline.invalid/v1", false, "audit", "classic-oil"), references);
                Check(!Property<bool>(unsupportedEdit, "Success") && handler.Requests.Count == 1 && handler.Requests[0].Url.EndsWith("/images/edits"), "unsupported Edits stops instead of discarding references through a text-only retry");
                CheckCapturedPrompt(unsupportedEdit, handler.Requests[0], "unsupported Edits");
                handler.Reset(NeedsChat(), Success());
                var fallback = Generate(Options("http://offline.invalid/v1", false, "audit", "classic-oil"), null);
                Check(Property<bool>(fallback, "Success") && handler.Requests.Count == 2, "text-only Generations can fall back to Chat");
                Check(handler.Requests[0].Url.EndsWith("/images/generations") && handler.Requests[1].Url.EndsWith("/chat/completions"), "text-only fallback uses the intended endpoint sequence");
                CheckCapturedPrompt(fallback, handler.Requests[1], "successful Chat fallback");
                handler.Reset(NeedsChat(), new Reply(HttpStatusCode.OK, "{}"));
                var failedFallback = Generate(Options("http://offline.invalid/v1", false, "audit", "classic-oil"), null);
                Check(!Property<bool>(failedFallback, "Success") && handler.Requests.Count == 2, "failed text-only Chat fallback stops after two protocol attempts");
                CheckCapturedPrompt(failedFallback, handler.Requests[1], "failed Chat fallback");
                handler.Reset();
                var pinnedGeneration = Generate(Options("http://offline.invalid/v1/images/generations?fixed=1", true, "audit", "classic-oil"), references);
                Check(!Property<bool>(pinnedGeneration, "Success") && handler.Requests.Count == 0, "exact Generations rejects references locally rather than dropping them");

                CheckStyles(assembly, handler);
            }
            finally { httpField.SetValue(null, original); }
        }
        Console.WriteLine("ClientEndpointAudit: " + checks + " PASS, 0 FAIL; all HTTP requests were intercepted in memory.");
    }
}
