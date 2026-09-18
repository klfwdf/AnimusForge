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
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
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
        string facts = "青年女性平民，裸头，无甲，穿布衣。FACT_SENTINEL";
        string[] modes = { "人物百科纪事", "周报历史纪事插画", "最近三轮对话联动的场景插画", "通用插画" };
        string valid = "【人物与镜头】人物身着现有衣物，自然呈现神情与动作。" +
            "【场景空间】环境材质向远处延展，形成清晰且柔和的空间层次。" +
            "【光线与色彩】自然光与环境反光协调过渡，暗部纹理清晰可辨。" +
            "【空间关系】近处与远处通过遮挡和景深形成连续纵深关系。";
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
            }
            finally { httpField.SetValue(null, original); }
        }
        Console.WriteLine("TOTAL " + checks + " PASS / 0 FAIL");
    }
}
