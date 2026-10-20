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
using MCM.Abstractions.Base;

// Separate-process managed tests. No Game/Campaign/native UI or live HTTP.
static class SupplementalChecks
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
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
    private static void CheckDropdownSelectionBoundary(Assembly af, DuelSettings owner, Action<bool, string> check)
    {
        var itemType = af.GetType("AnimusForge.AnimusForgeTerminalItemVM", true);
        int writes = 0, notifications = 0;
        MCM.Common.Dropdown<string> current = null;
        var definition = new TerminalSettingDef("OfflineDropdown", "AI", "", "", "",
            TerminalSettingType.Dropdown, 0, 0, 1, "", s => current?.SelectedValue ?? "",
            (s, value) => { writes++; current.SelectedIndex = (int)value; }, null, s => current);
        void Verify(string expected, int selectorIndex, string name)
        {
            int? before = current?.SelectedIndex;
            check((string)definition.Getter(owner) == expected, "dropdown getter " + name);
            var item = Activator.CreateInstance(itemType, new object[] { definition, (Action)(() => notifications++), null });
            object selector = itemType.GetProperty("DropdownSelector").GetValue(item);
            check((string)itemType.GetProperty("DisplayValue").GetValue(item) == expected, "dropdown display " + name);
            check(current?.SelectedIndex == before && writes == 0 && notifications == 0, "dropdown read preserves owner " + name);
            check(selector == null ? current == null || current.Count == 0 :
                (int)selector.GetType().GetProperty("SelectedIndex").GetValue(selector) == selectorIndex, "dropdown selector " + name);
        }
        Verify("", -1, "null");
        current = new MCM.Common.Dropdown<string>(new string[0], 0);
        Verify("", -1, "empty");
        current = new MCM.Common.Dropdown<string>(new[] { "first", "second" }, 0);
        foreach (int index in new[] { -1, -3, 2, int.MinValue, int.MaxValue })
        {
            current.SelectedIndex = index;
            Verify("", -1, "invalid " + index);
        }
        current.SelectedIndex = 1;
        Verify("second", 1, "valid");
        current.SelectedIndex = -1;
        var editable = Activator.CreateInstance(itemType, new object[] { definition, (Action)(() => notifications++), null });
        object editableSelector = itemType.GetProperty("DropdownSelector").GetValue(editable);
        editableSelector.GetType().GetProperty("SelectedIndex").SetValue(editableSelector, 1);
        check(current.SelectedIndex == 1 && writes == 1 && notifications == 1, "dropdown user can replace invalid selection");
        check((string)itemType.GetProperty("DisplayValue").GetValue(editable) == "second", "dropdown user selection display");

        // Exercise the same automatic coverage path named in the crash report.
        var definitions = new List<TerminalSettingDef>();
        af.GetType("AnimusForge.TerminalSettingsCoverage", true).GetMethod("AddMissing", All).Invoke(null, new object[] { definitions });
        var covered = definitions.Single(d => d.Id == "PlayerPolicyApiSourceDropdown");
        var original = owner.PlayerPolicyApiSourceDropdown;
        try
        {
            owner.PlayerPolicyApiSourceDropdown = new MCM.Common.Dropdown<string>(new string[0], 0);
            check((string)covered.Getter(owner) == "", "automatic coverage empty dropdown");
            Activator.CreateInstance(itemType, new object[] { covered, (Action)(() => notifications++), null });
            check(writes == 1 && notifications == 1, "automatic coverage opening does not save");
            owner.PlayerPolicyApiSourceDropdown = new MCM.Common.Dropdown<string>(new[] { "first" }, 0);
            owner.PlayerPolicyApiSourceDropdown.SelectedIndex = 4;
            check((string)covered.Getter(owner) == "", "automatic coverage invalid dropdown");
        }
        finally { owner.PlayerPolicyApiSourceDropdown = original; }
        Console.WriteLine("Terminal dropdown boundary regression PASS");
    }

    internal static void Run(Assembly af, object owner, Action<bool, string> check, string root)
    {
        CheckDropdownSelectionBoundary(af, (DuelSettings)owner, check);
        var promptRules = af.GetType("AnimusForge.NpcPersonaTextRules", true);
        var prompts = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(root, "ModuleData", "PersonaPromptTexts.json")));
        foreach (string kind in new[] { "Native", "Promoted" })
        {
            object[] input = kind == "Native" ? new object[] { "facts", "old", true, "", "" }
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

        check(af.GetType("AnimusForge.TownAmbientAiClient", false) == null
            && af.GetType("AnimusForge.TownAmbientDialogueMissionBehavior", false) == null,
            "retired ambient API client and automatic scene behavior absent from actual DLL");
        foreach (string name in new[] { "EnableTownAmbientAi", "EnableTownAmbientDialogue", "TownAmbientAiApiUrl", "TownAmbientAiModelDropdown", "TestTownAmbientAiConnection" })
            check(typeof(DuelSettings).GetProperty(name) == null, "retired setting absent: " + name);
        var formatType = typeof(BaseSettings).Assembly.GetType("MCM.Implementation.JsonSettingsFormat", true);
        var loggerType = typeof(BaseSettings).Assembly.GetType("BUTR.DependencyInjection.Logger.DefaultBUTRLogger`1", true).MakeGenericType(formatType);
        object ambientFormat = Activator.CreateInstance(formatType, Activator.CreateInstance(loggerType));
        var legacy = new DuelSettings();
        formatType.GetMethod("LoadFromJson").Invoke(ambientFormat, new object[] { legacy,
            "{\"EnableTownAmbientAi\":true,\"EnableTownAmbientDialogue\":true,\"TownAmbientAiApiUrl\":\"https://offline.invalid\"}" });
        string saved = (string)formatType.GetMethod("SaveJson").Invoke(ambientFormat, new object[] { legacy });
        check(!saved.Contains("TownAmbientAi") && !saved.Contains("EnableTownAmbientDialogue"),
            "obsolete JSON fields are ignored and cannot re-enable retired ambient execution");
    }
}
