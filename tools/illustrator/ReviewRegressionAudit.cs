using System;
using System.Collections;
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

// Regression checks against the supplied production DLL. HTTP stays in memory.
public static class ReviewRegressionAudit
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static Assembly assembly;
    private static Type optionsType, client, reference, kind;
    private static string png;
    private static int count;
    private static object Call(Type type, string name, params object[] args) { return type.GetMethod(name, All).Invoke(null, args); }
    private static object Property(object value, string name) { return value.GetType().GetProperty(name, All).GetValue(value, null); }
    private static void Option(object value, string name, object data) { optionsType.GetField("<" + name + ">k__BackingField", All).SetValue(value, data); }
    private static void Confirm(bool value, string name, string evidence)
    {
        if (!value) throw new Exception("FAIL: " + name);
        count++;
        Console.WriteLine("PASS " + name + ": " + evidence);
    }
    private static object Options(string url, bool exact, string model)
    {
        var value = FormatterServices.GetUninitializedObject(optionsType);
        Option(value, "EnableImageGeneration", true);
        Option(value, "EnableReferenceImageForGeneration", true);
        Option(value, "ApiBaseUrl", url);
        Option(value, "ApiKey", "");
        Option(value, "ModelName", model);
        Option(value, "ImageSize", "1024x1024");
        Option(value, "SelectedStyle", "classic-oil");
        Option(value, "UseExactEndpointUrl", exact);
        return value;
    }
    private static object Ref(string data, string label, string role)
    { return Activator.CreateInstance(reference, new object[] { data, label, Enum.Parse(kind, role) }); }
    private static Array Refs(params object[] values)
    {
        var result = Array.CreateInstance(reference, values.Length);
        for (int i = 0; i < values.Length; i++) result.SetValue(values[i], i);
        return result;
    }
    private static object Generate(object options, Array refs)
    {
        var method = client.GetMethods(All).Single(m => m.Name == "GenerateImageAsync" && m.GetParameters()[1].ParameterType != typeof(string));
        var task = (Task)method.Invoke(null, new object[] { "REVIEW_IMAGE_PROMPT", refs, options, CancellationToken.None });
        task.GetAwaiter().GetResult();
        return Property(task, "Result");
    }
    private sealed class Handler : HttpMessageHandler
    {
        public string Url, Body;
        public int Count;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Count++;
            Url = request.RequestUri.AbsoluteUri;
            if (request.Content is MultipartFormDataContent)
            {
                foreach (var part in (MultipartFormDataContent)request.Content)
                    if (part.Headers.ContentDisposition.Name.Trim('"') == "prompt")
                        Body = await part.ReadAsStringAsync().ConfigureAwait(false);
            }
            else
            Body = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"data\":[{\"b64_json\":\"" + png + "\"}]}") };
        }
    }
    private static object[] Parts(string json)
    {
        var data = (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(json);
        var message = (Dictionary<string, object>)((object[])data["messages"])[0];
        return (object[])message["content"];
    }
    public static void Run(string path)
    {
        assembly = Assembly.LoadFrom(path);
        optionsType = assembly.GetType("AnimusForge.Illustrator.Core.IllustrationOptions", true);
        client = assembly.GetType("AnimusForge.Illustrator.Core.UniversalOpenAiImageClient", true);
        reference = assembly.GetType("AnimusForge.Illustrator.Core.IllustrationReferenceImage", true);
        kind = assembly.GetType("AnimusForge.Illustrator.Core.IllustrationReferenceKind", true);
        using (var bitmap = new Bitmap(24, 12))
        using (var memory = new MemoryStream())
        {
            using (var graphics = Graphics.FromImage(bitmap)) graphics.Clear(Color.CornflowerBlue);
            bitmap.Save(memory, ImageFormat.Png);
            png = Convert.ToBase64String(memory.ToArray());
        }
        RuntimeHelpersInit(client);
        var httpField = client.GetField("HttpClient", All);
        var original = httpField.GetValue(null);
        var handler = new Handler();
        using (var http = new HttpClient(handler))
        {
            httpField.SetValue(null, http);
            try
            {
                var exact = Generate(Options("https://review.invalid/v1/images/generations", true, "gemini-3.1-flash-image"),
                    Refs(Ref(png, "VALID_IDENTITY", "Character")));
                Confirm(!(bool)Property(exact, "Success") && handler.Count == 0,
                    "exact-generations-rejects-references", "Chat model name does not bypass the Images reference guard");
                foreach (string url in new[] { "https://review.invalid/v1/images/generations", "https://review.invalid/V1/IMAGES/GENERATIONS/?route=/chat/completions" })
                {
                    handler.Count = 0;
                    var options = Options(url, true, "gemini-3.1-flash-image");
                    Option(options, "PreferChatImageProtocol", true);
                    var result = Generate(options, null);
                    Confirm((bool)Property(result, "Success") && handler.Count == 1 && handler.Url == url && handler.Body.Contains("\"prompt\"") && !handler.Body.Contains("\"messages\""),
                        "exact-generations-schema", "URL including case, trailing slash and query is preserved with an Images body");
                }
                Confirm((bool)Call(client, "IsChatCompletionProtocol", "gemini-3.1-flash-image", "https://review.invalid/custom-route", true),
                    "custom-relay-preserves-model-routing", "unknown custom endpoint paths retain model detection");

                foreach (string route in new[] { "chat/completions", "images/edits" })
                foreach (string invalid in new[] { "invalid-base64", "", "data:image/png;base64,broken", Convert.ToBase64String(new byte[50]) })
                {
                    handler.Count = 0;
                    var partial = Generate(Options("https://review.invalid/v1/" + route, true, "gemini-3.1-flash-image"),
                        Refs(Ref(png, "VALID_PLAYER_IDENTITY", "Character"), Ref(invalid, "MISSING_NPC_IDENTITY", "Character")));
                    Confirm(!(bool)Property(partial, "Success") && handler.Count == 0 && ((string)Property(partial, "ErrorMessage")).Contains("参考图 2"),
                        "partial-reference-failure-" + route, "a missing or invalid selected identity stops the entire request locally");
                }
                handler.Count = 0;
                var missing = Generate(Options("https://review.invalid/v1/chat/completions", true, "audit"),
                    Refs(Ref(png, "PLAYER", "Character"), null));
                Confirm(!(bool)Property(missing, "Success") && handler.Count == 0 && ((string)Property(missing, "ErrorMessage")).Contains("参考图 2"),
                    "null-selected-reference-stops-request", "null entries cannot silently drop a selected identity");
                var valid = Generate(Options("https://review.invalid/v1/chat/completions", true, "gemini-3.1-flash-image"),
                    Refs(Ref(png, "PLAYER", "Character"), Ref(png, "NPC", "Character")));
                Confirm((bool)Property(valid, "Success") && handler.Count == 1 && Parts(handler.Body).Cast<Dictionary<string, object>>().Count(p => (string)p["type"] == "image_url") == 2,
                    "valid-identities-preserved", "both identities survive preparation and are sent");
            }
            finally { httpField.SetValue(null, original); }
        }
        var director = assembly.GetType("AnimusForge.Illustrator.Core.VisualDirectorEngine", true);
        var planType = assembly.GetType("AnimusForge.Illustrator.Core.IllustrationPromptPlan", true);
        string action = "她将右手轻轻放在玩家的左肩上，身体略微向前倾";
        string narrative = "艾拉：她抬眼看向玩家。" + action + "。";
        string facts = "玩家与艾拉在室内交谈，两人均未骑乘。";
        var plan = Activator.CreateInstance(planType, new object[] { "最近一轮对话联动的场景插画", facts, "自然构图", narrative });
        string body = "【人物与镜头】" + action + "，玩家转头回应她，镜头采用中景。" +
            "【场景空间】两人身后的石墙与木桌形成清楚纵深，地面向远处延伸。" +
            "【光影与色彩】柔和光线沿两人衣褶连续铺展，受光与投影保持一致。" +
            "【空间关系】两人并肩站在木桌前方，手部接触与身体支撑关系明确。";
        Confirm((bool)Call(director, "HasRequiredSceneDescription", body), "action-fixture-is-structurally-complete", "all four production section checks accept the fixture");
        var direction = Call(director, "ResolveDirection", body, plan, null);
        Confirm(!(bool)Property(direction, "UsedLocalFallback") && ((string)Property(direction, "Prompt")).Contains(action),
            "valid-narrated-action-retained", "confirmed hand-on-shoulder action remains in the final image prompt");
        string speech = "我愿意与你一同守护这片土地直到最后一刻";
        foreach (string copied in new[] { "她说道：“" + speech + "”。", "字幕写着" + speech + "。" })
        {
            var speechPlan = Activator.CreateInstance(planType, new object[] { "最近一轮对话联动的场景插画", facts, "", "艾拉：“" + speech + "”。" });
            var rejected = Call(director, "ResolveDirection", body.Replace(action, copied), speechPlan, null);
            Confirm((bool)Property(rejected, "UsedLocalFallback") && !((string)Property(rejected, "Prompt")).Contains(speech),
                "spoken-text-stays-with-director", "verbatim dialogue and caption copies still trigger the echo guard");
        }
        string biography = "她从幼年起就在遥远边境跟随家族长辈学习治理领地";
        var backgroundPlan = Activator.CreateInstance(planType, new object[] { "最近一轮对话联动的场景插画", facts, "", narrative + "\n【人物背景】艾拉\n【人物生平参考】" + biography + "。" });
        Confirm((bool)Property(Call(director, "ResolveDirection", body.Replace(action, biography), backgroundPlan, null), "UsedLocalFallback"),
            "biography-echo-still-rejected", "action preservation does not disable the separate biography guard");
        var rules = assembly.GetType("AnimusForge.Illustrator.Core.VisualFidelityRules", true);
        string priority = (string)rules.GetField("ConversationActionPriority", All).GetRawConstantValue();
        string system = (string)director.GetField("ConversationSystemPrompt", All).GetRawConstantValue();
        Confirm(system.Contains(priority) && ((string)Property(direction, "Prompt")).Contains(priority) && !system.Contains("当前画面才用于校验可见人物关系"),
            "dialogue-action-priority-both-stages", "director and image prompts use dialogue actions; screenshots only locate the scene");
        var capture = assembly.GetType("AnimusForge.Illustrator.Engine.ScreenCaptureHelper", true);
        string sceneLabel = (string)Call(capture, "SceneReferenceLabel");
        Confirm(sceneLabel.Contains("仅用于判断当前位置与环境定位") && !sceneLabel.Contains("校验现场采光、可见人物行动"),
            "screenshot-label-has-location-only-role", "standing pose in a screenshot cannot override narrated action");
        string mapNote = (string)Call(capture, "MapConversationReferenceNote", true);
        Confirm(mapNote.Contains("仅供当前位置与环境定位") && mapNote.Contains("不以截图里的待机姿势覆盖"),
            "map-screenshot-has-location-only-role", "map conversation applies the same action precedence");
        var popup = assembly.GetType("AnimusForge.Illustrator.UI.Overlays.IllustrationCardPopup", true);
        Confirm(!((string)Call(popup, "GenerateConversationSceneVariation", new object[] { null })).Contains("两人处于面对面真实交谈"),
            "no-forced-face-to-face", "conversation composition no longer injects an unconditional face-to-face action");

        // Capture the real image request for the conflicting dialogue/screenshot case.
        handler = new Handler();
        using (var http = new HttpClient(handler))
        {
            httpField.SetValue(null, http);
            try
            {
                foreach (string route in new[] { "chat/completions", "images/edits" })
                {
                    handler.Count = 0;
                    handler.Body = null;
                    var method = client.GetMethods(All).Single(m => m.Name == "GenerateImageAsync" && m.GetParameters()[1].ParameterType != typeof(string));
                    var task = (Task)method.Invoke(null, new object[] { Property(direction, "Prompt"),
                        Refs(Ref(png, sceneLabel, "Scene")), Options("https://review.invalid/v1/" + route, true, "audit"), CancellationToken.None });
                    task.GetAwaiter().GetResult();
                    Confirm((bool)Property(Property(task, "Result"), "Success") && handler.Count == 1 && handler.Body.Contains(action) && handler.Body.Contains(priority), "action-precedence-on-wire-" + route,
                        "actual image request retains the narrated action and the location-only screenshot constraint");
                }
            }
            finally { httpField.SetValue(null, original); }
        }

        var directionType = assembly.GetType("AnimusForge.Illustrator.Core.IllustrationDirection", true);
        var textDirection = Activator.CreateInstance(directionType);
        directionType.GetProperty("VisionUnsupported", All).SetValue(textDirection, true, null);
        directionType.GetProperty("DirectionStatus", All).SetValue(textDirection, "vision_unsupported", null);
        var listType = typeof(List<>).MakeGenericType(reference);
        var generationRefs = (IList)Activator.CreateInstance(listType);
        var routing = assembly.GetType("AnimusForge.Illustrator.Core.IllustrationReferenceRouting", true);
        Call(routing, "AddSceneReferences", generationRefs,
            Refs(Ref(png, "STATIC_PANORAMA", "ScenePanorama"), Ref(png, "ACTUAL_LIGHT_AND_ACTORS", "Scene")), textDirection, CancellationToken.None);
        Confirm(generationRefs.Count == 2 && Property(generationRefs[0], "Kind").ToString() == "ScenePerspective" && Property(generationRefs[1], "Kind").ToString() == "Scene",
            "text-only-director-keeps-real-location", "actual screenshot accompanies the neutral static perspective for location only");
        directionType.GetProperty("VisionUnsupported", All).SetValue(textDirection, false, null);
        directionType.GetProperty("UsedTextOnlyDirector", All).SetValue(textDirection, true, null);
        generationRefs.Clear();
        Call(routing, "AddSceneReferences", generationRefs, Refs(Ref(png, sceneLabel, "Scene")), textDirection, CancellationToken.None);
        Confirm(generationRefs.Count == 1 && Property(generationRefs[0], "Kind").ToString() == "Scene",
            "intentional-text-only-keeps-location", "disabled vision preserves the available location screenshot without inventing a panorama");
        Console.WriteLine("RESULT: " + count + " checks passed; external HTTP requests: 0");
    }
    private static void RuntimeHelpersInit(Type type) { System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle); }
}
