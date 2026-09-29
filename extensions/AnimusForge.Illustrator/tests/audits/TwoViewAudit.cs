using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

// Exercises the production director, metadata parser and projection/routing boundary.
// No game state, GPU or external HTTP is used.
public static class TwoViewAudit
{
    private const BindingFlags All = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const string Main = "【环境取景】yaw=0;pitch=0;hfov=55\n";
    private const string Auxiliary = "【环境辅助取景】yaw=25;pitch=0;hfov=55\n";
    private const string Body = "【人物与镜头】两人并肩站在木桌前，玩家低头看向桌面，对话对象抬手指向桌边，采用中景。" +
        "【场景空间】两人身后的石墙与木桌形成清楚纵深，地面向远处延伸，背景结构保留原有排列。" +
        "【光影与色彩】柔和光线沿两人衣褶连续铺展，受光与投影保持一致，暗部保留环境反光。" +
        "【空间关系】两人并肩站在木桌前方，手部接触与身体支撑关系明确，桌腿投影与地面相接。";
    private static Type direction, routing, reference, kind, projection;
    private static Array scenes;
    private static byte[] panorama;
    private static int checks;

    private static object Call(Type owner, string name, params object[] args)
    { return owner.GetMethod(name, All).Invoke(null, args); }
    private static object Get(object target, string name)
    { return target.GetType().GetProperty(name, All).GetValue(target, null); }
    private static void Set(object target, string name, object value)
    { target.GetType().GetProperty(name, All).SetValue(target, value, null); }
    private static void Check(bool ok, string name)
    { if (!ok) throw new Exception("FAIL " + name); checks++; Console.WriteLine("PASS " + name); }
    private static object Parse(string text) { return Call(direction, "SplitMetadata", text); }
    private static object Ref(string data, string label, string role)
    { return Activator.CreateInstance(reference, new object[] { data, label, Enum.Parse(kind, role) }); }
    private static IList Route(object selected, bool current)
    {
        var refs = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(reference));
        refs.Add(Ref("identity", "EXISTING_IDENTITY", "Character"));
        Array source = scenes;
        if (!current) { source = Array.CreateInstance(reference, 1); source.SetValue(scenes.GetValue(0), 0); }
        Call(routing, "AddSceneReferences", refs, source, selected, CancellationToken.None);
        Check((string)Get(refs[refs.Count - 1], "Label") == "EXISTING_IDENTITY" &&
            refs.Cast<object>().All(r => Get(r, "Kind").ToString() != "ScenePanorama"), "routing preserves identity order and excludes full panorama");
        return refs;
    }
    private static void ExpectException<T>(Action action, string name) where T : Exception
    {
        try { action(); }
        catch (TargetInvocationException ex) { Check(ex.InnerException is T, name); return; }
        throw new Exception("FAIL expected " + typeof(T).Name + ": " + name);
    }
    private sealed class Handler : HttpMessageHandler
    {
        public int Count;
        public string Request;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Count++;
            if (Count > 1) throw new Exception("Unexpected extra director call");
            Request = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            string json = new JavaScriptSerializer().Serialize(new {
                choices = new[] { new { finish_reason = "stop", message = new { content = Main + Auxiliary + Body } } }
            });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        }
    }
    public static void Run(string dllPath)
    {
        checks = 0;
        var assembly = Assembly.LoadFrom(dllPath);
        const string core = "AnimusForge.Illustrator.Core.";
        direction = assembly.GetType(core + "IllustrationDirection", true);
        routing = assembly.GetType(core + "IllustrationReferenceRouting", true);
        reference = assembly.GetType(core + "IllustrationReferenceImage", true);
        kind = assembly.GetType(core + "IllustrationReferenceKind", true);
        projection = assembly.GetType("AnimusForge.Illustrator.Engine.ScenePerspectiveProjection", true);
        using (var bitmap = new Bitmap(360, 180))
        using (var memory = new MemoryStream())
        {
            for (int y = 0; y < 180; y++)
                for (int x = 0; x < 360; x++) bitmap.SetPixel(x, y, Color.FromArgb(x * 255 / 359, y * 255 / 179, 64));
            bitmap.Save(memory, ImageFormat.Png);
            panorama = memory.ToArray();
        }
        scenes = Array.CreateInstance(reference, 2);
        scenes.SetValue(Ref(Convert.ToBase64String(panorama), "PANORAMA", "ScenePanorama"), 0);
        scenes.SetValue(Ref(Convert.ToBase64String(panorama), "CURRENT_LOCATION", "Scene"), 1);
        var selected = Parse("【画作标题】厅中交谈\n" + Main + Auxiliary + Body);
        Check((double)Get(selected, "SceneYawDegrees") == 0 && (double)Get(selected, "AuxiliarySceneYawDegrees") == 25 &&
            (double)Get(selected, "AuxiliarySceneHorizontalFovDegrees") == 55, "both views parse without overwriting one another");
        Check((string)Get(selected, "Prompt") == Body && (string)Get(selected, "Title") == "厅中交谈", "both metadata lines are removed from image prose");
        var culture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var fractional = Parse(Main + "【环境辅助取景】YAW=+25.5;PITCH=-4.25;HFOV=60.5;\n" + Body);
            Check((double)Get(fractional, "AuxiliarySceneYawDegrees") == 25.5 && (string)Get(fractional, "Prompt") == Body,
                "auxiliary numbers remain invariant-culture and metadata never leaks");
        }
        finally { Thread.CurrentThread.CurrentCulture = culture; }
        Check(Route(selected, true).Count == 3, "valid visual direction routes two perspectives plus existing identity");
        foreach (string bad in new[] {
            "", "【环境辅助取景】yaw=25;pitch=0", "【环境辅助取景】yaw=181;pitch=0;hfov=55",
            "【环境辅助取景】yaw=25;pitch=61;hfov=55", "【环境辅助取景】yaw=25;pitch=0;hfov=101",
            "【环境辅助取景】yaw=NaN;pitch=0;hfov=55", "【环境辅助取景】yaw=Infinity;pitch=0;hfov=55",
            "【环境辅助取景】yaw=0;pitch=0;hfov=55", "【环境辅助取景】yaw=2;pitch=0;hfov=55",
            "【环境辅助取景】yaw=180;pitch=0;hfov=55", "【环境辅助取景】yaw=50;pitch=0;hfov=55",
            "【环境辅助取景】yaw=0;pitch=60;hfov=55" })
        {
            var fallback = Parse(Main + bad + "\n" + Body);
            Check((string)Get(fallback, "Prompt") == Body && Route(fallback, false).Count == 2,
                "absent, invalid, duplicate or disjoint auxiliary keeps the valid primary: " + bad);
        }
        Check(Route(Parse("【环境取景】yaw=170;pitch=0;hfov=55\n【环境辅助取景】yaw=-165;pitch=0;hfov=55"), false).Count == 3,
            "adjacent views across the plus/minus 180 seam remain usable");
        Check(Route(Parse("【环境取景】yaw=180;pitch=0;hfov=55\n【环境辅助取景】yaw=-180;pitch=0;hfov=55"), false).Count == 2,
            "equivalent plus/minus 180 views do not send a duplicate");
        Check(Route(Parse(Auxiliary + Body), false).Count == 2, "auxiliary alone cannot override the default primary");
        foreach (string flag in new[] { "UsedLocalFallback", "UsedTextOnlyDirector", "VisionUnsupported" })
        {
            var fallback = Parse(Main + Auxiliary + Body);
            Set(fallback, flag, true);
            var refs = Route(fallback, true);
            Check(refs.Count == 3 && Get(refs[1], "Kind").ToString() == "Scene", "fallback retains location screenshot and ignores ungrounded views: " + flag);
        }
        var director = assembly.GetType(core + "VisualDirectorEngine", true);
        var plan = Activator.CreateInstance(assembly.GetType(core + "IllustrationPromptPlan", true),
            new object[] { "最近一轮对话联动的场景插画", "玩家与对话对象在石墙和木桌前交谈。", "自然构图", "" });
        var local = Call(director, "ResolveDirection", Main + Auxiliary + "无效正文", plan, null);
        Check((bool)Get(local, "UsedLocalFallback") && new[] { "SceneYawDegrees", "ScenePitchDegrees", "SceneHorizontalFovDegrees",
            "AuxiliarySceneYawDegrees", "AuxiliaryScenePitchDegrees", "AuxiliarySceneHorizontalFovDegrees" }.All(p => Get(local, p) == null),
            "body fallback clears all six framing values");
        var pair = (byte[][])Call(projection, "ProjectPair", panorama, 0d, 0d, 55d, 25d, 0d, 55d, CancellationToken.None);
        Check(pair.Length == 2 && !pair[0].SequenceEqual(pair[1]), "paired projection produces two distinct images");
        Check(pair[0].SequenceEqual((byte[])Call(projection, "Project", panorama, 0d, 0d, 55d, CancellationToken.None)) &&
            pair[1].SequenceEqual((byte[])Call(projection, "Project", panorama, 25d, 0d, 55d, CancellationToken.None)),
            "paired projection exactly preserves both standalone projection results");
        for (int i = 0; i < 2; i++)
            using (var memory = new MemoryStream(pair[i]))
            using (var bitmap = new Bitmap(memory))
            {
                Color center = bitmap.GetPixel(384, 384);
                Check(bitmap.Width == 768 && bitmap.Height == 768 && Math.Abs(center.R - (i == 0 ? 127 : 145)) <= 2 &&
                    Math.Abs(center.G - 127) <= 2, "projected center matches the independently encoded longitude and latitude");
            }
        ExpectException<OperationCanceledException>(() => Call(projection, "ProjectPair", panorama, 0d, 0d, 55d, 25d, 0d, 55d, new CancellationToken(true)),
            "cancelled pair never returns a partial reference set");
        ExpectException<ArgumentOutOfRangeException>(() => Call(projection, "ProjectPair", panorama, 0d, 0d, 55d, double.NaN, 0d, 55d, CancellationToken.None),
            "pair validates auxiliary before allocating pixels");
        ExpectException<InvalidDataException>(() => Call(projection, "ProjectPair", new byte[33], 0d, 0d, 55d, 25d, 0d, 55d, CancellationToken.None),
            "pair retains bounded PNG validation");
        var optionsType = assembly.GetType(core + "IllustrationOptions", true);
        var options = FormatterServices.GetUninitializedObject(optionsType);
        foreach (var entry in new Dictionary<string, object> {
            { "EnableLlmPromptExpansion", true }, { "EnableMultimodalVision", true },
            { "DirectorApiBaseUrl", "http://offline.invalid/v1" }, { "DirectorApiKey", "offline" },
            { "DirectorModelName", "audit" }, { "DirectorApproximateTokens", 2000 }
        }) optionsType.GetField("<" + entry.Key + ">k__BackingField", All).SetValue(options, entry.Value);
        var handler = new Handler();
        using (var http = new HttpClient(handler))
        {
            var task = (Task)Call(director, "CreateDirectionWithClientAsync", plan, scenes, options, http, CancellationToken.None);
            task.GetAwaiter().GetResult();
            var result = Get(task, "Result");
            Check(handler.Count == 1 && !(bool)Get(result, "UsedLocalFallback") &&
                (double)Get(result, "AuxiliarySceneYawDegrees") == 25 && !( (string)Get(result, "Prompt")).Contains("yaw="),
                "one real director pipeline request supplies two stripped framing values");
            Check(handler.Request.Contains("环境辅助取景") && Route(result, false).Count == 3 && handler.Count == 1,
                "routing the director result performs no extra director request");
        }
        Console.WriteLine("TWO VIEW AUDIT: " + checks + " PASS / 0 FAIL (external HTTP: 0)");
    }
}
