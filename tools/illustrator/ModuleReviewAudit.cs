using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

// Production managed methods with synthetic campaign/screen identities and MCM eligibility.
// No native rendering, game launch, player data writes, HTTP requests or model charges.
public static class ModuleReviewAudit
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static int checks;
    private static object harmony;
    private static Type harmonyType;
    private static Type runtime;
    private static Type scopeType;
    private static Type screenType;
    private static object screen;
    private const string PatchId = "AnimusForge.Illustrator.OfflineModuleAudit";

    private static void Check(bool result, string name)
    {
        if (!result) throw new Exception("FAIL " + name);
        checks++;
        Console.WriteLine("PASS " + name);
    }
    private static object Call(Type type, string name, object instance, params object[] args)
    { return type.GetMethod(name, All).Invoke(instance, args); }
    private static void SetField(Type type, string name, object instance, object value)
    { type.GetField(name, All).SetValue(instance, value); }
    private static void SetProperty(Type type, string name, object instance, object value)
    { type.GetProperty(name, All).GetSetMethod(true).Invoke(instance, new[] { value }); }
    public static bool EnabledForAudit(ref bool __result) { __result = true; return false; }
    public static bool RejectOverlandProbe() { throw new Exception("Historical event queried the player's current terrain."); }

    private static void Prefix(MethodInfo target, string method)
    {
        Type hm = harmonyType.Assembly.GetType("HarmonyLib.HarmonyMethod", true);
        object prefix = Activator.CreateInstance(hm, new object[] { typeof(ModuleReviewAudit).GetMethod(method) });
        MethodInfo patch = harmonyType.GetMethod("Patch");
        var args = new object[patch.GetParameters().Length];
        args[0] = target; args[1] = prefix;
        patch.Invoke(harmony, args);
    }
    private static void PumpUntil(Func<bool> finished)
    {
        var clock = Stopwatch.StartNew();
        while (!finished())
        {
            Call(runtime, "Tick", null);
            if (clock.ElapsedMilliseconds > 5000) throw new TimeoutException("Managed audit completion queue did not drain.");
            Thread.Sleep(5);
        }
    }
    private static object NewScope(Action closed)
    {
        return Activator.CreateInstance(scopeType, All, null, new object[] { screen, null, closed }, null);
    }
    private static bool RunScope(object scope, Func<CancellationToken, Task<int>> work, Action<int> complete, Action<string> fail)
    {
        return (bool)scopeType.GetMethod("Run", All).MakeGenericMethod(typeof(int))
            .Invoke(scope, new object[] { work, complete, fail });
    }
    private static void DrainWorkers()
    { PumpUntil(() => (int)runtime.GetField("_workers", All).GetValue(null) == 0); }

    public static void Run(string dllPath, string repoRoot)
    {
        checks = 0;
        Assembly module = Assembly.LoadFrom(dllPath);
        Type cache = module.GetType("AnimusForge.Illustrator.Engine.DiskImageCacheManager", true);
        FieldInfo cacheRoot = cache.GetField("CacheBaseDir", All);
        object originalCacheRoot = cacheRoot.GetValue(null);
        string auditRoot = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(dllPath), "module-audit-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(auditRoot);
        cacheRoot.SetValue(null, auditRoot);
        runtime = module.GetType("AnimusForge.Illustrator.Core.IllustratorRuntime", true);
        scopeType = module.GetType("AnimusForge.Illustrator.Core.IllustrationScope", true);
        Type campaign = scopeType.GetField("_campaign", All).FieldType;
        screenType = scopeType.GetField("_screen", All).FieldType;
        Type screenManager = screenType.Assembly.GetType("TaleWorlds.ScreenSystem.ScreenManager", true);
        object oldCampaign = campaign.GetProperty("Current", All).GetValue(null, null);
        object oldScreen = screenManager.GetProperty("TopScreen", All).GetValue(null, null);
        string oldKey = (string)runtime.GetProperty("CampaignKey").GetValue(null, null);
        harmonyType = Assembly.Load("0Harmony").GetType("HarmonyLib.Harmony", true);
        harmony = Activator.CreateInstance(harmonyType, new object[] { PatchId });
        try
        {
            // Only the MCM setting predicate is mocked; actual scope.IsCurrent still checks
            // campaign identity, screen identity, close flag, revision and request token.
            Prefix(runtime.GetMethod("IsEnabled", All), "EnabledForAudit");
            object campaignFixture = FormatterServices.GetUninitializedObject(campaign);
            SetProperty(campaign, "Current", null, campaignFixture);
            Type tracker = campaign.GetProperty("MapTimeTracker", All).PropertyType;
            SetProperty(campaign, "MapTimeTracker", campaignFixture, FormatterServices.GetUninitializedObject(tracker));
            Type campaignTime = campaign.Assembly.GetType("TaleWorlds.CampaignSystem.CampaignTime", true);
            SetField(campaignTime, "TimeTicksPerSeason", null, 100L);
            SetField(campaignTime, "SeasonsInYear", null, 4);

            var generated = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("IllustratorAuditScreen"), AssemblyBuilderAccess.Run);
            var builder = generated.DefineDynamicModule("main").DefineType("AuditScreen", TypeAttributes.Public, screenType);
            builder.DefineDefaultConstructor(MethodAttributes.Public);
            screen = FormatterServices.GetUninitializedObject(builder.CreateType());
            SetProperty(screenManager, "TopScreen", null, screen);
            SetField(runtime, "_mainThread", null, Environment.CurrentManagedThreadId);
            SetField(runtime, "_running", null, true);
            SetProperty(runtime, "CampaignKey", null, "module-audit");

            string failure = null;
            int completeCount = 0;
            object timeoutScope = NewScope(() => { });
            Check(RunScope(timeoutScope, token => Task.FromException<int>(new TaskCanceledException("upstream deadline")),
                value => completeCount++, error => failure = error), "scope accepts upstream timeout fixture");
            DrainWorkers();
            Check(failure != null && failure.Contains("超时") && !failure.Contains("界面已切换") && completeCount == 0,
                "uncancelled caller TaskCanceledException reports upstream timeout");
            Call(scopeType, "Close", timeoutScope);
            Call(runtime, "Tick", null);

            int closedCount = 0, lateSuccess = 0, lateFailure = 0;
            CancellationToken observed = CancellationToken.None;
            var started = new ManualResetEventSlim();
            var pending = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            object closingScope = NewScope(() => closedCount++);
            Check(RunScope(closingScope, token => { observed = token; started.Set(); return pending.Task; },
                value => lateSuccess++, error => lateFailure++), "scope accepts deferred completion fixture");
            Check(started.Wait(3000), "worker captured cancellation token");
            Call(scopeType, "Close", closingScope);
            Check(observed.IsCancellationRequested, "scope close cancels the request token");
            pending.SetResult(7);
            DrainWorkers();
            Check(lateSuccess == 0 && lateFailure == 0 && closedCount == 1,
                "late success after scope.Close cannot update retired UI");

            Type weekly = module.GetType("AnimusForge.Illustrator.UI.Patches.WeeklyReportIllustrationOverlayVM", true);
            int redraws = 0;
            object vm = Activator.CreateInstance(weekly, new object[] { "offline", (Action)(() => redraws++) });
            SetProperty(weekly, "IsLoading", vm, true);
            Call(weekly, "ExecuteRegenerate", vm); Call(weekly, "ExecuteRegenerate", vm);
            Check(redraws == 0, "weekly repeated redraw clicks do not interrupt active generation");
            SetProperty(weekly, "IsLoading", vm, false);
            Call(weekly, "ExecuteRegenerate", vm);
            Check(redraws == 1, "weekly redraw resumes after loading completes");

            Type environment = module.GetType("AnimusForge.Illustrator.Context.EnvironmentVisualProfile", true);
            foreach (string season in new[] { "春季", "夏季", "秋季", "冬季" })
            {
                object profile = Activator.CreateInstance(environment);
                SetProperty(environment, "Season", profile, season);
                string facts = (string)Call(environment, "BuildHardFactsSummary", profile);
                Check(facts.Contains("【当前季节】" + season), "conversation sends explicit season: " + season);
            }
            Type extractor = module.GetType("AnimusForge.Illustrator.Context.EnvironmentVisualExtractor", true);
            Prefix(extractor.GetMethod("ResolveOverlandTerrain", All), "RejectOverlandProbe");
            Prefix(extractor.GetMethod("TryGetOverlandTerrain", All), "RejectOverlandProbe");
            object unknown = Call(extractor, "Extract", null, null, true, "卡拉迪亚历 · 冬季 · 周报发布日");
            string unknownFacts = (string)Call(environment, "BuildHardFactsSummary", unknown);
            Check(!unknownFacts.Contains("地貌类型") && !unknownFacts.Contains("定居点") && !unknownFacts.Contains("荒野"),
                "historical event with no settlement does not inspect or inherit player terrain");
            Check(unknownFacts.Contains("【季节参考】冬季") && !unknownFacts.Contains("【当前季节】"),
                "weekly date remains a season reference rather than exact event season");

            foreach (string location in new[] { "center", "" })
            {
                object besieged = Activator.CreateInstance(environment);
                Call(extractor, "ResolveBesiegedLocation", null, besieged, false, location, false);
                string facts = (string)Call(environment, "BuildHardFactsSummary", besieged);
                Check(!facts.Contains("城头守将") && !facts.Contains("护城河") && !facts.Contains("城门") &&
                    facts.Contains(location == "center" ? "城镇街道" : "具体地点未确认"),
                    "siege status alone does not invent gate parley: " + location);
                SetProperty(environment, "HostSceneDescription", besieged, "玩家站在城墙上，对方站在城墙下进行围城交涉。");
                string confirmed = (string)Call(environment, "BuildHardFactsSummary", besieged);
                Check(confirmed.Contains("玩家站在城墙上，对方站在城墙下"),
                    "confirmed host scene still supplies wall height relationship: " + location);
            }
            foreach (string location in new[] { "", "tavern", "prison" })
            {
                object profile = Activator.CreateInstance(environment);
                Call(extractor, "ResolveBesiegedLocation", null, profile, location == "", location, location != "");
                string facts = (string)Call(environment, "BuildHardFactsSummary", profile);
                Check(!facts.Contains("旷野") && !facts.Contains("阵前谈判") && !facts.Contains("庇护所") &&
                    !facts.Contains("避难处境") && !facts.Contains("牢狱关押"),
                    "siege location metadata does not invent physical setting or activity: " + location);
            }
            object unspecified = Activator.CreateInstance(environment);
            Call(extractor, "ResolveSpecificLocation", null, unspecified, null);
            string unspecifiedFacts = (string)Call(environment, "BuildHardFactsSummary", unspecified);
            Check(unspecifiedFacts.Contains("具体子场景位置未确认") && !unspecifiedFacts.Contains("两军阵前"),
                "missing ordinary conversation location does not imply a field parley");
            object unsentTemplates = Activator.CreateInstance(environment);
            foreach (string field in new[] { "IndoorOutdoorDetails", "LightingAndAtmosphere", "SurroundingCharacters", "SurroundingProps" })
                SetProperty(environment, field, unsentTemplates, "UNCONFIRMED_TEMPLATE_SENTINEL");
            string routedTemplates = (string)Call(environment, "BuildSummary", unsentTemplates) +
                (string)Call(environment, "BuildDirectorOnlyFacts", unsentTemplates);
            Check(!routedTemplates.Contains("UNCONFIRMED_TEMPLATE_SENTINEL"),
                "legacy descriptive template fields are not routed to director or image facts");

            Type fitType = Assembly.Load("TaleWorlds.GauntletUI").GetType("TaleWorlds.GauntletUI.ImageFit", true);
            PropertyInfo fitMode = fitType.GetProperty("Type");
            MethodInfo fitRect = fitType.GetMethod("GetFittedRectangle");
            Type vector = fitRect.GetParameters()[0].ParameterType.GetElementType();
            foreach (string prefab in new[] { "EncyclopediaIllustrationOverlay", "ConversationIllustrationOverlay", "WeeklyReportIllustrationOverlay", "IllustratorGalleryPopup" })
            {
                var xml = new XmlDocument();
                xml.Load(Path.Combine(repoRoot, "extensions/AnimusForge.Illustrator/GUI/Prefabs/" + prefab + ".xml"));
                XmlElement image = (XmlElement)xml.SelectSingleNode("//Widget[@Sprite='@SpriteName' or @Sprite='@SelectedSpriteName']");
                Check(image != null && image.GetAttribute("ImageFit.Type") == "Contain", prefab + " uses native contain fitting");
            }
            var galleryXml = new XmlDocument();
            galleryXml.Load(Path.Combine(repoRoot, "extensions/AnimusForge.Illustrator/GUI/Prefabs/IllustratorGalleryPopup.xml"));
            XmlElement theme = (XmlElement)galleryXml.SelectSingleNode("//RichTextWidget[@Text='@SelectedTheme']");
            Check(theme != null && theme.SelectSingleNode("ancestor::Widget[@IsVisible='@HasSelection']") != null &&
                theme.SelectSingleNode("ancestor::Widget[@IsVisible='@HasNoSelection']") == null,
                "gallery theme is visible in selected artwork details");
            Type galleryVm = module.GetType("AnimusForge.Illustrator.UI.Gallery.IllustratorGalleryPopupVM", true);
            Check(galleryVm.GetProperty("SelectedTheme") != null &&
                File.ReadAllText(Path.Combine(repoRoot, "extensions/AnimusForge.Illustrator/src/UI/Gallery/IllustratorGalleryPopupVM.cs"))
                    .Contains("SelectedTheme = selected.Item.DisplayStatusText"),
                "gallery theme binding is populated from selected cached artwork");
            var cachedType = module.GetType("AnimusForge.Illustrator.Engine.CachedIllustrationItem", true);
            var artwork = Activator.CreateInstance(cachedType);
            cachedType.GetProperty("Theme").SetValue(artwork, "主题验收", null);
            cachedType.GetProperty("DirectorStatus").SetValue(artwork, "truncated", null);
            cachedType.GetProperty("DirectorStatusText").SetValue(artwork, "导演输出截断，已使用本地构图", null);
            string shown = (string)cachedType.GetProperty("DisplayStatusText").GetValue(artwork, null);
            Check(shown.Contains("主题验收") && shown.Contains("导演输出截断"), "selected artwork keeps theme and director fallback status visible");
            cachedType.GetProperty("DirectorStatus").SetValue(artwork, "complete", null);
            Check(!((string)cachedType.GetProperty("DisplayStatusText").GetValue(artwork, null)).Contains("截断"), "complete director does not show stale degradation text");
            XmlElement promptPanel = (XmlElement)theme.ParentNode.SelectSingleNode("ScrollablePanel");
            Check(float.Parse(promptPanel.GetAttribute("MarginTop")) >=
                float.Parse(theme.GetAttribute("MarginTop")) + float.Parse(theme.GetAttribute("SuggestedHeight")),
                "gallery reserves vertical space before prompt text");
            foreach (float[] dimensions in new[] { new[] { 1280f, 720f }, new[] { 1024f, 1536f }, new[] { 1024f, 1024f } })
            {
                object fit = Activator.CreateInstance(fitType);
                fitMode.SetValue(fit, Enum.Parse(fitMode.PropertyType, "Contain"), null);
                object rectangle = fitRect.Invoke(fit, new[] {
                    Activator.CreateInstance(vector, new object[] { 380f, 380f }),
                    Activator.CreateInstance(vector, new object[] { dimensions[0], dimensions[1] }) });
                float width = (float)rectangle.GetType().GetField("Width").GetValue(rectangle);
                float height = (float)rectangle.GetType().GetField("Height").GetValue(rectangle);
                Check(Math.Abs(width / height - dimensions[0] / dimensions[1]) < 0.0001f && width <= 380.01f && height <= 380.01f,
                    "native fitting preserves complete image aspect: " + dimensions[0] + "x" + dimensions[1]);
            }
            Console.WriteLine("TOTAL " + checks + " PASS / 0 FAIL");
        }
        finally
        {
            harmonyType.GetMethod("UnpatchAll").Invoke(harmony, new object[] { PatchId });
            SetProperty(campaign, "Current", null, oldCampaign);
            SetProperty(screenManager, "TopScreen", null, oldScreen);
            SetProperty(runtime, "CampaignKey", null, oldKey);
            cacheRoot.SetValue(null, originalCacheRoot);
        }
    }
}
