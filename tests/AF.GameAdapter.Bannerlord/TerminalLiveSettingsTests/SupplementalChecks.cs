using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using HarmonyLib;
using MCM.Abstractions;

// Separate-process managed tests. No Game/Campaign/native UI or live HTTP.
static class SupplementalChecks
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    static LlmProviderSnapshot captured;
    static bool credentialMatches;
    static int gatewayCalls;
    static readonly List<string> notices = new List<string>();
    static void RecordNotice(object message)
    {
        notices.Add((string)message.GetType().GetProperty("Information").GetValue(message));
    }
    static bool GatewayPrefix(LegacyConfiguredChatGateway __instance, LlmGenerateRequest request, ref Task<LlmGenerateResult> __result)
    {
        captured = request.Provider;
        var resolver = (Func<LlmProviderSnapshot, string>)typeof(LegacyConfiguredChatGateway).GetField("_credentialResolver", All).GetValue(__instance);
        credentialMatches = resolver(captured) == "offline-page-key";
        Interlocked.Increment(ref gatewayCalls);
        __result = Task.FromResult(new LlmGenerateResult(LlmResultStatus.Succeeded, "OK", 4, 2, ""));
        return false;
    }
    sealed class UnsafeObject
    {
        public int Calls;
        public override string ToString() { Calls++; throw new NullReferenceException("offline unsafe formatter"); }
    }
    sealed class StackProbe : Exception
    {
        public int Reads;
        public override string StackTrace { get { Reads++; return "offline unrelated frame"; } }
    }
    static void NormalMarker() { }
    static void Tick() { }
    static void LocalizedNewlineChecks(Action<bool, string> check, string root, string language)
    {
        var textObject = Assembly.Load("TaleWorlds.Localization").GetType("TaleWorlds.Localization.TextObject", true);
        var english = new Dictionary<string, string>();
        foreach (string path in Directory.GetFiles(Path.Combine(root, "ModuleData", "Languages"), "*.xml"))
        foreach (var entry in XDocument.Load(path).Descendants("string"))
            english[(string)entry.Attribute("id")] = (string)entry.Attribute("text");
        string folder = Path.Combine(root, "ModuleData", "Languages", language == "English" ? "" : "CNs");
        int checkedEntries = 0;
        foreach (string path in Directory.GetFiles(folder, "*.xml"))
        foreach (var entry in XDocument.Load(path).Descendants("string"))
        {
            string text = (string)entry.Attribute("text"), id = (string)entry.Attribute("id");
            if (text == null) continue;
            check(!text.Contains(@"\n"), "fixed UI resource has no literal newline escape: " + id);
            if (!text.Contains("\n")) continue;
            string rendered = Activator.CreateInstance(textObject, new object[] { "{=" + id + "}" + english[id], null }).ToString();
            check(rendered.Count(c => c == '\n') == text.Count(c => c == '\n') && !rendered.Contains(@"\n"), "native text retains actual line breaks: " + id + " " + language);
            checkedEntries++;
        }
        check(checkedEntries > 30, "native newline replay covers affected dialogs: " + language);
        var dynamicText = Activator.CreateInstance(textObject, new object[] { "{=AF_UI_Persona_RerollFailedBody}" + english["AF_UI_Persona_RerollFailedBody"], null });
        textObject.GetMethod("SetTextVariable", new[] { typeof(string), typeof(string) }).Invoke(dynamicText, new object[] { "DETAIL", @"C:\new\npc\rules.json" });
        check(dynamicText.ToString().Contains(@"C:\new\npc\rules.json"), "dynamic paths are not unescaped: " + language);
    }
    static void McmGroupChecks(Assembly af, DuelSettings owner, Action<bool, string> check, string language)
    {
        var visibility = af.GetType("AnimusForge.AnimusForgeMcmVisibility", true).GetMethod("IsVisibleProperty", All);
        var definitions = owner.GetAllSettingPropertyDefinitions().Where(d => (bool)visibility.Invoke(null, new object[] { owner.Id, d.Id, d.GroupName })).ToArray();
        var groups = SettingsUtils.GetSettingsPropertyGroups(owner.SubGroupDelimiter, definitions).ToArray();
        var textObject = Assembly.Load("TaleWorlds.Localization").GetType("TaleWorlds.Localization.TextObject", true);
        string Display(SettingsPropertyGroupDefinition group) => Activator.CreateInstance(textObject, new object[] { group.GroupName, null }).ToString();
        bool chinese = language == "简体中文";
        string parent = chinese ? "1. AI 核心配置" : "1. AI core configuration";
        var roots = groups.Where(g => Display(g) == parent).ToArray();
        check(roots.Length == 1, "MCM API groups share one translated parent: " + language);
        var names = SettingsUtils.GetAllSettingPropertyGroupDefinitions(roots[0]).Select(Display).ToArray();
        foreach (string name in chinese
            ? new[] { "1. 主API（正文生成）", "2. 前处理 API（规则检索与简易对话链路）", "3. 后处理 API（动作标签与情绪标签判定）", "4. 事件与王国叛乱 API（周报生成与叛乱命名）" }
            : new[] { "1. Main API (response generation)", "2. Preprocessing API (rule retrieval and lightweight conversation)", "3. Postprocessing API (action and emotion tags)", "4. Event and kingdom-rebellion API (weekly reports and naming)" })
            check(names.Count(n => n == name) == 1, "MCM translated API subgroup appears once: " + name);
        check(names.All(n => !n.Contains("/") && !n.Contains("{=")), "MCM group labels contain no raw path or localization marker");
    }
    internal static void Run(Assembly af, object owner, Action<bool, string> check, string root)
    {
        var promptRules = af.GetType("AnimusForge.NpcPersonaTextRules", true);
        var prompts = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(root, "ModuleData", "PersonaPromptTexts.json")));
        foreach (string kind in new[] { "Native", "Promoted" })
        {
            object[] input = kind == "Native" ? new object[] { "facts", "old", true, "" }
                : new object[] { "name", "full", "troop", "id", "culture", "scene", "joined", "history", "equipment", "facts", "" };
            object request = promptRules.GetMethod("Build" + kind, All).Invoke(null, input);
            string system = (string)request.GetType().GetProperty("System", All).GetValue(request);
            string expected = (string)prompts["AF_PERSONA_PROMPT_" + kind + "System"];
            check(system.StartsWith(expected) && (expected.Contains("profile must be a non-empty string") || expected.Contains("profile 必须是非空字符串")), "actual persona request uses selected CN/EN string protocol: " + kind);
        }
        var duel = (DuelSettings)owner;
        var patcher = af.GetType("AnimusForge.DynamicPatcher", true);
        var unsafeValue = new UnsafeObject();
        duel.EnableDeepTrace = true;
        check(TraceHelper.IsEnabled, "controlled actual settings enable diagnostic prefix");
        var normal = typeof(SupplementalChecks).GetMethod("NormalMarker", All);
        for (int i = 0; i < 10000; i++) DynamicPatcher.UniversalPrefix(normal, new object[] { unsafeValue });
        check(unsafeValue.Calls == 0, "10000 actual prefixes never call custom ToString");
        duel.EnableDeepTrace = false;
        var format = patcher.GetMethod("FormatDiagnosticArgument", All);
        check((string)format.Invoke(null, new object[] { null }) == "null", "null diagnostic argument");
        check((string)format.Invoke(null, new object[] { 123 }) == "123", "primitive diagnostic argument");
        check(((string)format.Invoke(null, new object[] { new string('x', 600) })).Length == 515, "diagnostic string bounded");
        check(((string)format.Invoke(null, new object[] { unsafeValue })).Contains("UnsafeObject") && unsafeValue.Calls == 0, "custom argument uses type metadata");
        var timers = patcher.GetField("_methodLogTimers", All).GetValue(null);
        void Clear() => timers.GetType().GetMethod("Clear").Invoke(timers, null);
        var admit = patcher.GetMethod("TryAdmitDiagnosticLog", All);
        bool Admit(MethodBase method, int now) => (bool)admit.Invoke(null, new object[] { method, now });
        Clear(); check(Admit(normal, 0) && !Admit(normal, 999) && Admit(normal, 1000), "normal throttle boundaries");
        Clear(); var tick = typeof(SupplementalChecks).GetMethod("Tick", All);
        check(Admit(tick, 0) && !Admit(tick, 59999) && Admit(tick, 60000), "hot method minute throttle boundaries");
        Clear(); check(Admit(normal, int.MaxValue - 500) && Admit(normal, unchecked(int.MaxValue - 500 + 1000)), "signed clock wrap preserves interval");
        Clear(); int winners = 0;
        Parallel.For(0, 128, _ => { if (Admit(normal, 10)) Interlocked.Increment(ref winners); });
        check(winners == 1, "parallel diagnostic callers admit one log");
        var sentinel = af.GetType("AnimusForge.BannerlordExceptionSentinel", true);
        var update = sentinel.GetMethod("UpdateFirstChanceHandler", All);
        var callback = sentinel.GetMethod("CurrentDomain_FirstChanceException", All);
        var registered = sentinel.GetField("_firstChanceHandlerRegistered", All);
        try
        {
            update.Invoke(null, new object[] { true });
            check((bool)registered.GetValue(null), "first chance listener enabled");
            update.Invoke(null, new object[] { false });
            check(!(bool)registered.GetValue(null), "first chance listener detached when disabled");
            var probe = new StackProbe();
            callback.Invoke(null, new object[] { null, new FirstChanceExceptionEventArgs(probe) });
            check(probe.Reads == 0, "disabled exception callback never reads stack");
            update.Invoke(null, new object[] { true });
            sentinel.GetField("_handlingFirstChance", All).SetValue(null, true);
            callback.Invoke(null, new object[] { null, new FirstChanceExceptionEventArgs(probe) });
            check(probe.Reads == 0, "reentrant exception callback never reads stack");
            sentinel.GetField("_handlingFirstChance", All).SetValue(null, false);
            callback.Invoke(null, new object[] { null, new FirstChanceExceptionEventArgs(probe) });
            check(probe.Reads > 0, "enabled exception callback retains diagnostic inspection");
        }
        finally { sentinel.GetField("_handlingFirstChance", All).SetValue(null, false); update.Invoke(null, new object[] { false }); }

        var harmony = new Harmony("af.tests.ambient.snapshot");
        var gateway = typeof(LegacyConfiguredChatGateway).GetMethod("GenerateAsync");
        var localization = Assembly.Load("TaleWorlds.Localization");
        localization.GetType("TaleWorlds.Localization.LocalizedTextManager", true).GetMethod("LoadLocalizationXmls").Invoke(null, new object[] { new[] { root } });
        var language = localization.GetType("TaleWorlds.Localization.MBTextManager", true).GetMethod("ChangeLanguage");
        var information = Assembly.Load("TaleWorlds.Library").GetType("TaleWorlds.Library.InformationManager", true);
        var noticeEvent = information.GetEvent("DisplayMessageInternal");
        var noticeArg = Expression.Parameter(noticeEvent.EventHandlerType.GetMethod("Invoke").GetParameters()[0].ParameterType);
        var observer = Expression.Lambda(noticeEvent.EventHandlerType,
            Expression.Call(typeof(SupplementalChecks).GetMethod("RecordNotice", All), Expression.Convert(noticeArg, typeof(object))), noticeArg).Compile();
        noticeEvent.AddEventHandler(null, observer);
        try
        {
            harmony.Patch(gateway, prefix: new HarmonyMethod(typeof(SupplementalChecks).GetMethod("GatewayPrefix", All)));
            var incomplete = TownAmbientAiClient.TestConnectionAsync("", "", "").GetAwaiter().GetResult();
            check(!incomplete.Success && gatewayCalls == 0, "incomplete ambient config sends no request");
            foreach (string selectedLanguage in new[] { "English", "简体中文" })
            {
            language.Invoke(null, new object[] { selectedLanguage });
            McmGroupChecks(af, duel, check, selectedLanguage);
            LocalizedNewlineChecks(check, root, selectedLanguage);
            notices.Clear(); gatewayCalls = 0;
            var page = new DuelSettings();
            page.TownAmbientAiApiUrl = "https://offline.invalid/v1/chat/completions";
            page.TownAmbientAiApiKey = "offline-page-key";
            page.TownAmbientAiModelName = "offline-page-model";
            duel.TownAmbientAiApiUrl = "https://global.invalid/v1/chat/completions";
            duel.TownAmbientAiApiKey = "offline-global-key";
            duel.TownAmbientAiModelName = "offline-global-model";
            string main = duel.ModelName;
            check(page.TestTownAmbientAiConnection != null, "environment connection action bound on actual UI owner");
            page.TestTownAmbientAiConnection();
            page.TownAmbientAiApiUrl = "https://later.invalid/v1/chat/completions";
            page.TownAmbientAiApiKey = "offline-later-key";
            page.TownAmbientAiModelName = "offline-later-model";
            var refresh = af.GetType("AnimusForge.McmDropdownRuntimeRefresh", true);
            var count = refresh.GetField("MainThreadActionCount", All);
            check(SpinWait.SpinUntil(() => (int)count.GetValue(null) > 0, 10000), "ambient completion marshalled to main thread queue");
            check(gatewayCalls == 1 && captured.Endpoint == "https://offline.invalid/v1/chat/completions" && captured.Model == "offline-page-model" && credentialMatches,
                "ambient test captures page config before worker without global/main fallback");
            refresh.GetMethod("DrainMainThreadActions", All).Invoke(null, null);
            check((int)count.GetValue(null) == 0 && duel.ModelName == main, "completion drains and main model unchanged");
            check(notices.Count == 2 && notices[0].Contains(selectedLanguage == "English" ? "Testing the configured" : "正在测试")
                && notices[1].Contains(selectedLanguage == "English" ? "Connection is working" : "连接成功")
                && !notices[1].Contains("{INPUT}") && !notices[1].Contains("{OUTPUT}"), "actual ambient test notices projected " + selectedLanguage);
            }
        }
        finally { harmony.Unpatch(gateway, HarmonyPatchType.Prefix, harmony.Id); noticeEvent.RemoveEventHandler(null, observer); language.Invoke(null, new object[] { "English" }); }
    }
}
