using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

// Runs production methods from the built DLL. HTTP is replaced in-process;
// no provider request, game state or GPU is used.
public static class PromptRoutingAudit
{
    private const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private static int checks;
    private static void Check(bool passed, string name)
    {
        if (!passed) throw new Exception("FAIL " + name);
        checks++;
        Console.WriteLine("PASS " + name);
    }
    private static object Call(Type type, string name, params object[] args)
    { return type.GetMethod(name, Static).Invoke(null, args); }
    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string Body;
        public readonly Dictionary<string, string> Fields = new Dictionary<string, string>();
        public readonly List<string> Images = new List<string>();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Fields.Clear(); Images.Clear();
            var multipart = request.Content as MultipartFormDataContent;
            if (multipart != null)
            {
                foreach (HttpContent part in multipart)
                {
                    var disposition = part.Headers.ContentDisposition;
                    string name = disposition.Name.Trim('"');
                    if (name == "image[]")
                        Images.Add(disposition.FileName.Trim('"'));
                    else
                        Fields[name] = await part.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            Body = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("offline audit") };
        }
    }
    public static void Run(string dllPath)
    {
        checks = 0;
        Assembly assembly = Assembly.LoadFrom(dllPath);
        string core = "AnimusForge.Illustrator.Core.";
        Type director = assembly.GetType(core + "VisualDirectorEngine", true);
        Type planType = assembly.GetType(core + "IllustrationPromptPlan", true);
        string system = (string)director.GetField("SystemPrompt", Static).GetRawConstantValue();
        Check(system.Contains("成图质量底线") && system.Contains("每个人物只选择一个清楚的主要体态"), "director receives simple physically coherent pose constraint");
        Check(system.Contains("不能仅保留立绘轮廓再更换背景") && system.Contains("环境反光"), "director must reconstruct figure and scene together");
        Check(system.Contains("头部装备的盔壳轮廓") && system.Contains("面部实际覆盖范围") && system.Contains("披肩不能概括成内衬"), "director describes visible equipment landmarks instead of generic costume");
        Check(system.Contains("物品名称、ID、文化和头衔只辅助识别") && system.Contains("不能把白天改成夜晚"), "names and painting style cannot override observed appearance or time");
        Check(system.Contains("楼梯所在墙面及走向") && system.Contains("不据酒馆或大厅等名称重新设计建筑"), "director reconstructs scene topology instead of a generic location template");
        Type capture = assembly.GetType("AnimusForge.Illustrator.Engine.ScreenCaptureHelper", true);
        var screenshotDefaults = capture.GetMethod("CaptureConversationSceneBase64", Static).GetParameters();
        Check((float)screenshotDefaults[1].DefaultValue == 1f && (int)screenshotDefaults[0].DefaultValue == 1024, "scene screenshot includes ground and foreground at full height");
        Type frameType = capture.GetMethod("BuildPanoramaFrame", Static).GetParameters()[0].ParameterType;
        object originalFrame = frameType.GetProperty("Identity", Static).GetValue(null);
        Type vectorType = frameType.GetField("origin").FieldType;
        frameType.GetField("origin").SetValue(originalFrame, Activator.CreateInstance(vectorType, new object[] { 12f, 25f, 3f, -1f }));
        var frames = new object[4];
        for (int i = 0; i < 4; i++)
        {
            frames[i] = Call(capture, "BuildPanoramaFrame", originalFrame, i);
            object origin = frameType.GetField("origin").GetValue(frames[i]);
            Check((float)vectorType.GetField("x").GetValue(origin) == 12f && (float)vectorType.GetField("y").GetValue(origin) == 25f && (float)vectorType.GetField("z").GetValue(origin) == 3f, "panorama rotates in place without camera translation: " + i);
            object rotation = frameType.GetField("rotation").GetValue(frames[i]);
            object axis = rotation.GetType().GetField("u").GetValue(rotation);
            Check(Math.Abs((float)vectorType.GetField("z").GetValue(axis)) < 0.0001f, "panorama levels vertical initial view: " + i);
            Check((bool)Call(capture, "IsUsablePanoramaFrame", frames[i], frames[i], 1.74533f), "matching rendered camera accepted: " + i);
        }
        Check(!(bool)Call(capture, "IsUsablePanoramaFrame", frames[1], frames[0], 1.74533f), "unchanged rendered view is not mislabeled as next direction");
        Check(!(bool)Call(capture, "IsUsablePanoramaFrame", frames[0], frames[0], 0.8f), "zoomed camera cannot masquerade as panoramic coverage");
        Check(((string)Call(capture, "SceneReferenceLabel", 2)).Contains("同一空间") && ((string)Call(capture, "SceneReferenceLabel", -1)).Contains("当前玩家视角"), "scene views distinguish current context from environment sweep");
        Type heroExtractor = assembly.GetType("AnimusForge.Illustrator.Context.HeroVisualExtractor", true);
        foreach (bool hair in new[] { false, true })
        foreach (bool beard in new[] { false, true })
        {
            string head = (string)Call(heroExtractor, "BuildHeadgearDescription", "羽饰战冠", "empire_battle_crown_north", "金属甲胄", hair, beard);
            Check(head.Contains("装备标记隐藏全部头发") == hair && head.Contains("装备标记隐藏全部胡须") == beard, "hair and beard visibility flags stay independent: " + hair + "/" + beard);
            Check(head.Contains("遮发/遮须标记不等于面部全遮覆") && !head.Contains("完全遮蔽住整张面孔"), "mesh hiding never asserts full face coverage: " + hair + "/" + beard);
        }
        Type popup = assembly.GetType("AnimusForge.Illustrator.UI.Overlays.IllustrationCardPopup", true);
        string pose = (string)Call(popup, "GenerateDiversePoseDirective");
        Check(pose.Contains("站立、坐姿") && !pose.Contains("未经思考") && !pose.Contains("严禁与上一版重复"), "portrait input permits natural standing without compulsory pose change");
        string variation = (string)Call(director, "BuildRedrawVariationDirective", 3);
        Check(variation.Contains("行动意图") && variation.Contains("不强迫复杂动作") && variation.Contains("保持该事实"), "redraw varies activity without contortion or overriding event facts");
        Check(system.Contains("先决定人物此刻正在做什么，再推导姿态、手部动作和视线") && pose.Contains("不把双手下垂展示装备作为默认"), "director chooses activity before pose instead of default display stance");
        string facts = "青年女性平民，裸头，无甲，穿布衣。FACT_SENTINEL";
        string[] modes = { "人物百科纪事", "周报历史纪事插画", "最近三轮对话联动的场景插画", "通用插画" };
        string valid = "【人物与镜头】人物身着现有衣物，自然呈现神情与动作。" +
            "【场景空间】环境材质向远处延展，形成清晰且柔和的空间层次。" +
            "【光线与色彩】自然光与环境反光协调过渡，暗部纹理清晰可辨。" +
            "【空间关系】近处与远处通过遮挡和景深形成连续纵深关系。";
        Type directionType = assembly.GetType(core + "IllustrationDirection", true);
        object metadataPlan = Activator.CreateInstance(planType, new object[] { "人物百科纪事", facts, "", "" });
        string withMetadata = "【画作标题】灯下裁决【画作主题】战前权衡【人物行动】俯身审视地图，一手指向路线，视线落在指尖。" + valid;
        object named = Call(director, "ResolveDirection", withMetadata, metadataPlan, null);
        string namedPrompt = (string)directionType.GetProperty("Prompt").GetValue(named);
        Check((string)directionType.GetProperty("Title").GetValue(named) == "灯下裁决" && (string)directionType.GetProperty("Theme").GetValue(named) == "战前权衡", "director title and theme parsed independently");
        Check(!namedPrompt.Contains("灯下裁决") && !namedPrompt.Contains("战前权衡") && !namedPrompt.Contains("【人物行动】"), "metadata never enters image prompt");
        Check(namedPrompt.Contains(valid) && !namedPrompt.Contains("FACT_SENTINEL"), "named artwork retains director body without reinjecting raw facts");
        object rejected = Call(director, "ResolveDirection", "【画作标题】错误标题【画作主题】错误主题【人物行动】错误动作正文", metadataPlan, null);
        Check((string)directionType.GetProperty("Title").GetValue(rejected) == "" && !(string.Concat(directionType.GetProperty("ActionSummary").GetValue(rejected))).Contains("错误"), "fallback discards metadata from rejected direction");
        object sanitized = Call(directionType, "SplitMetadata", "【画作标题】<b>作品</b>{bad}【画作主题】主题\n" + valid);
        Check((string)directionType.GetProperty("Title").GetValue(sanitized) == "作品", "UI labels strip markup");

        Type cachedType = assembly.GetType("AnimusForge.Illustrator.Engine.CachedIllustrationItem", true);
        Array past = Array.CreateInstance(cachedType, 5);
        for (int i = 0; i < past.Length; i++)
        {
            object item = Activator.CreateInstance(cachedType);
            cachedType.GetProperty("CreatedTime").SetValue(item, new DateTime(2026, 9, 18).AddMinutes(i));
            cachedType.GetProperty("ActionSummary").SetValue(item, "行动" + i);
            cachedType.GetProperty("Prompt").SetValue(item, "【人物与镜头】旧画双手下垂，目视前方。【场景空间】旧布景");
            if (i == 3) cachedType.GetProperty("ActionSummary").SetValue(item, "");
            if (i == 4) cachedType.GetProperty("Deleted").SetValue(item, true);
            past.SetValue(item, i);
        }
        string history = (string)Call(directionType, "BuildActionHistory", past, false);
        Check(history.Contains("旧画双手下垂") && history.Contains("行动2") && history.Contains("行动1") && !history.Contains("行动0") && !history.Contains("行动4"), "redraw uses three newest surviving actions with legacy prompt fallback");
        Check(history.Contains("不是本次现场事实") && history.Contains("不能仅换背景"), "previous poses are variation context, not new hard facts");
        string eventHistory = (string)Call(directionType, "BuildActionHistory", past, true);
        Check(eventHistory.Contains("不能为动作去重改变事件") && !eventHistory.Contains("选择符合人物的新行动"), "conversation and weekly redraw history cannot rewrite events");
        foreach (string mode in modes)
        {
            object plan = Activator.CreateInstance(planType, new object[] { mode, facts, "META_SENTINEL", "NARRATIVE_SENTINEL" });
            string context = (string)planType.GetMethod("BuildDirectorContext").Invoke(plan, null);
            Check(context.Contains("FACT_SENTINEL") && context.Contains("META_SENTINEL") && context.Contains("NARRATIVE_SENTINEL"), mode + " director input");
            foreach (string output in new[] { valid, "近景画面", "" })
            {
                string result = (string)Call(director, "ResolveDirectorOutput", output, plan, null);
                Check(result.Contains("双人动态交互") == (mode == modes[2]), mode + " composition routing: " + output.Length);
                Check(!result.Contains("NARRATIVE_SENTINEL"), mode + " narrative stays with director: " + output.Length);
                if (output == valid)
                    Check(!result.Contains("FACT_SENTINEL") && !result.Contains("META_SENTINEL"), mode + " no raw director context in successful output");
                else
                    Check(result.Contains("FACT_SENTINEL"), mode + " local fallback retains identity facts: " + output.Length);
            }
            if (mode == modes[0])
            {
                string local = (string)Call(director, "BuildLocalSceneDirection", plan);
                Check((bool)Call(director, "HasRequiredSceneDescription", local), "portrait fallback has four sections");
                Check(local.Contains("非具名艺术布景") && !local.Contains("战盔") && !local.Contains("胸甲") && !local.Contains("长戟") && !local.Contains("军械"), "civilian fallback does not invent equipment or setting");
            }
        }

        Type conversationType = assembly.GetType("AnimusForge.Illustrator.Context.ConversationVisualContext", true);
        object conversation = Activator.CreateInstance(conversationType);
        conversationType.GetProperty("SceneFacts").SetValue(conversation, "玩家未骑乘，对方骑乘");
        conversationType.GetProperty("SceneDirective").SetValue(conversation, "META_POSE_SENTINEL");
        string conversationFacts = (string)conversationType.GetMethod("BuildHardFacts").Invoke(conversation, null);
        string direction = (string)conversationType.GetMethod("BuildArtDirection").Invoke(conversation, new object[] { null });
        Check(conversationFacts.Contains("玩家未骑乘") && !conversationFacts.Contains("META_POSE_SENTINEL"), "conversation hard facts exclude pose guidance");
        Check(direction.Contains("META_POSE_SENTINEL"), "pose guidance retained for director");
        Type conversationExtractor = assembly.GetType("AnimusForge.Illustrator.Context.ConversationContextExtractor", true);
        Check(((string)Call(conversationExtractor, "DescribeMountState", "玩家", false, false)).Contains("未确认"), "absent agent is unknown, not walking");
        Check(((string)Call(conversationExtractor, "DescribeMountState", "玩家", true, false)).Contains("未骑乘"), "dismounted agent does not imply standing on ground");
        Check(((string)Call(conversationExtractor, "DescribeMountState", "玩家", true, true)).Contains("处于骑乘"), "mounted state preserved");

        Type profileType = assembly.GetType("AnimusForge.Illustrator.Context.EnvironmentVisualProfile", true);
        // A real mission may exist without a successfully read scene clock.
        foreach (bool hasMission in new[] { false, true })
        foreach (bool hasSceneTime in new[] { false, true })
        {
            object timing = Activator.CreateInstance(profileType);
            profileType.GetProperty("HasLiveScene").SetValue(timing, hasMission);
            profileType.GetProperty("HasSceneTime").SetValue(timing, hasSceneTime);
            profileType.GetProperty("TimeOfDay").SetValue(timing, "沉寂夜幕 (Midnight)");
            profileType.GetProperty("LightingAndAtmosphere").SetValue(timing, "月光");
            profileType.GetMethod("UseConversationTimeEvidence", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(timing, null);
            string timingFacts = (string)profileType.GetMethod("BuildHardFactsSummary").Invoke(timing, null);
            Check(timingFacts.Contains("【现场时段】沉寂夜幕") == hasSceneTime, "only verified scene clock supplies conversation time: " + hasMission + "/" + hasSceneTime);
            Check(((string)profileType.GetProperty("LightingAndAtmosphere").GetValue(timing) == "月光") == hasSceneTime, "unverified campaign lighting is removed: " + hasMission + "/" + hasSceneTime);
            Check(timingFacts.Contains(hasSceneTime ? "当前 Mission 场景时间" : "否则时段未确认"), "time source is explicit: " + hasMission + "/" + hasSceneTime);
        }
        object historical = Activator.CreateInstance(profileType);
        profileType.GetProperty("TimeOfDay").SetValue(historical, "事件记录中的夜晚");
        Check(((string)profileType.GetMethod("BuildHardFactsSummary").Invoke(historical, null)).Contains("事件记录中的夜晚"), "non-conversation historical time remains intact");
        Type extractor = assembly.GetType("AnimusForge.Illustrator.Context.EnvironmentVisualExtractor", true);
        string[] locations = { "tavern", "prison", "lordshall", "keep", "mod_room", "" };
        string[] expected = { "酒馆", "石牢", "议事正厅", "议事正厅", "用途未确认", "用途未确认" };
        for (int i = 0; i < locations.Length; i++)
        {
            object profile = Activator.CreateInstance(profileType);
            Call(extractor, "ResolveBesiegedLocation", profile, false, locations[i], true);
            string result = (string)profileType.GetMethod("BuildHardFactsSummary").Invoke(profile, null);
            Check(result.Contains(expected[i]), "siege indoor fact routing: " + locations[i]);
        }
        foreach (bool outdoor in new[] { false, true })
        {
            object profile = Activator.CreateInstance(profileType);
            Call(extractor, "ResolveBesiegedLocation", profile, outdoor, "", false);
            string location = (string)profileType.GetProperty("SpecificLocation").GetValue(profile);
            Check(location.Contains(outdoor ? "旷野谈判" : "城门"), "siege outdoor routing: " + outdoor);
        }

        Type reference = assembly.GetType(core + "IllustrationReferenceImage", true);
        Type kind = assembly.GetType(core + "IllustrationReferenceKind", true);
        Array refs = Array.CreateInstance(reference, 3);
        string[] kinds = { "Character", "Emblem", "Scene" };
        string[] labels = { "登场人物的身份参考图，装备与实际纹章载体上的家族纹章；不作为现场", "仅作徽记样图", "实际场景" };
        for (int i = 0; i < refs.Length; i++)
            refs.SetValue(Activator.CreateInstance(reference, new object[] { "AA==", labels[i], Enum.Parse(kind, kinds[i]) }), i);
        Type client = assembly.GetType(core + "UniversalOpenAiImageClient", true);
        FieldInfo httpField = client.GetField("HttpClient", Static);
        var original = (HttpClient)httpField.GetValue(null);
        var handler = new CaptureHandler();
        using (var http = new HttpClient(handler))
        {
            httpField.SetValue(null, http);
            try
            {
                var task = (Task)Call(client, "AttemptGenerateOnceAsync", "http://offline.invalid/chat/completions", "audit", valid,
                    "1024x1024", "", "", refs, "", true, CancellationToken.None, null);
                task.GetAwaiter().GetResult();
                string body = handler.Body;
                Check(body != null && body.Contains("核心人物官方真实视觉基准图"), "weekly character gets identity mandate despite label keywords");
                string marker = "【家族纹章图案样板】";
                Check(body.IndexOf(marker, StringComparison.Ordinal) == body.LastIndexOf(marker, StringComparison.Ordinal) && body.Contains(marker), "only emblem gets emblem mandate");
                Check(body.Contains("现场3D实景采光与地形参考"), "scene gets scene mandate");
                Check(body.Contains("人物与镜头") && body.Contains("data:image/"), "director prompt and reference image sent together");
                Check(!body.Contains("四层纵深") && !body.Contains("生动舒展") && !body.Contains("身着真实战甲"), "transport does not impose pose, layers or armor");
                Check(body.Contains("整幅重新绘制"), "Chat receives whole-image repaint contract");
                Check(body.Contains("若文字概括与可见外观冲突，保留参考图外观") && body.Contains("披肩轮廓"), "Chat keeps reference appearance above conflicting director paraphrase");

                // Exercise the real multipart adapter with valid PNG reference files.
                string png;
                using (var bitmap = new System.Drawing.Bitmap(8, 8))
                using (var stream = new System.IO.MemoryStream())
                {
                    bitmap.SetPixel(0, 0, System.Drawing.Color.Red);
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    png = Convert.ToBase64String(stream.ToArray());
                }
                for (int i = 0; i < refs.Length; i++)
                    refs.SetValue(Activator.CreateInstance(reference, new object[] { png, labels[i], Enum.Parse(kind, kinds[i]) }), i);

                // Isolate actual cache save/load in the audit artifact directory, never a player cache.
                Type cache = assembly.GetType("AnimusForge.Illustrator.Engine.DiskImageCacheManager", true);
                string fixtureRoot = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(dllPath), "cache-audit-" + Guid.NewGuid().ToString("N"));
                cache.GetField("CacheBaseDir", Static).SetValue(null, fixtureRoot);
                object saved = Call(cache, "SaveImage", "hero-audit", Convert.FromBase64String(png), namedPrompt, "灯下裁决", "encyclopedia", "offline-fixture", 10, false, false, "战前权衡", "指向地图");
                Check(saved != null, "artwork metadata saved with valid image in isolated cache");
                object loaded = Call(cache, "LoadImage", "hero-audit", "offline-fixture", "encyclopedia");
                Check((string)cachedType.GetProperty("Title").GetValue(loaded) == "灯下裁决" && (string)cachedType.GetProperty("Theme").GetValue(loaded) == "战前权衡" && (string)cachedType.GetProperty("ActionSummary").GetValue(loaded) == "指向地图", "title theme and activity survive cache reload");
                Check((string)cachedType.GetProperty("Prompt").GetValue(loaded) == namedPrompt, "cache prompt remains only the image request text");
                Check((string)cachedType.GetProperty("ThemeText").GetValue(loaded) == "主题：战前权衡", "theme display text available after reload");
                string metadataPath = System.IO.Path.ChangeExtension((string)cachedType.GetProperty("FilePath").GetValue(saved), ".json");
                string legacyJson = System.Text.RegularExpressions.Regex.Replace(System.IO.File.ReadAllText(metadataPath), @"\s*""(?:Theme|ActionSummary)""\s*:\s*""[^""]*"",?", "");
                System.IO.File.WriteAllText(metadataPath, legacyJson);
                Call(cache, "InvalidateCache");
                object legacy = Call(cache, "LoadImage", "hero-audit", "offline-fixture", "encyclopedia");
                Check(legacy != null && (string)cachedType.GetProperty("ThemeText").GetValue(legacy) == "纪事画卷", "legacy metadata without new fields still loads");

                foreach (bool chat in new[] { false, true })
                {
                    string effective = (string)Call(client, "BuildEffectivePrompt", "裸头人物露出面容", "1024x1024", "", "", "", null, chat, 0);
                    Check(!effective.Contains("露脸") && !effective.Contains("mascot") && !effective.Contains("full helmet") && effective.Contains("extra limbs"), "generic negatives no longer conflict with open face: " + chat);
                    string custom = (string)Call(client, "BuildEffectivePrompt", "主体", "1024x1024", "", "", "", "CUSTOM_NEGATIVE", chat, 0);
                    Check(custom.Contains("CUSTOM_NEGATIVE"), "user custom negatives preserved: " + chat);
                }
                var editTask = (Task)Call(client, "AttemptImagesEditsAsync", "http://offline.invalid/v1", "audit", valid,
                    "1024x1024", "", "", refs, "", CancellationToken.None);
                editTask.GetAwaiter().GetResult();
                Check(handler.Images.Count == 3 && handler.Images[0] == "reference_0.png", "Edits sends all reference files in order");
                Check(!handler.Fields.ContainsKey("mask"), "identity redraw does not synthesize a repair mask");
                string editPrompt = handler.Fields["prompt"];
                Check(editPrompt.Contains("整幅重新绘制") && editPrompt.Contains("不是保留人物像素的换背景"), "Edits explicitly requests full figure repaint");
                Check(editPrompt.Contains("人物身份参考：") && editPrompt.Contains("纹章样图：") && editPrompt.Contains("现场参考："), "Edits reference roles remain distinct despite label keywords");
                Check(editPrompt.Contains(valid) && editPrompt.Contains("光源") && editPrompt.Contains("衣褶"), "Edits retains director composition and unified figure lighting");
                Check(editPrompt.Contains("若文字概括与可见外观冲突，保留参考图外观") && editPrompt.Contains("披肩轮廓"), "Edits uses the same appearance precedence as Chat");
                object tuple = editTask.GetType().GetProperty("Result").GetValue(editTask);
                string resolved = (string)tuple.GetType().GetField("Item6").GetValue(tuple);
                Check(resolved == editPrompt, "returned Edits prompt equals exact multipart prompt");
            }
            finally { httpField.SetValue(null, original); }
        }
        Console.WriteLine("TOTAL " + checks + " PASS / 0 FAIL");
    }
}
