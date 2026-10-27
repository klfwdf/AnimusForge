using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using HarmonyLib;
using Newtonsoft.Json;

// A fresh process per package. Real candidate DLL and game managed APIs;
// no game startup, native engine, HTTP, settings writes or live save file.
internal static class Program
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static readonly string[] Ids = { "default", "king", "vassal", "mercenary", "trader", "outlaw", "beggar", "fleetadmiral", "merchantventurer" };
    private static string _promptPath;
    private static object _campaign, _options;
    private static string _fixtureName = "Ada";
    private static bool _known = true;
    private static int _checks;
    private static int Main(string[] args)
    {
        try
        {
            string[] directories = args[0].Split('|');
            AppDomain.CurrentDomain.AssemblyResolve += (_, request) =>
            {
                string name = new AssemblyName(request.Name).Name;
                foreach (string directory in directories)
                {
                    string path = Path.Combine(directory, name + ".dll");
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
                return null;
            };
            Run(Assembly.LoadFrom(args[1]), args[2], args[3]);
            Console.WriteLine("PASS assertions=" + _checks + "; package=" + args[3] + "; managed fixtures only; no live save/UI acceptance.");
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }
    private static void Check(bool ok, string label)
    {
        if (!ok) throw new InvalidOperationException(label);
        _checks++;
    }
    private static object Field(object target, string name) => target.GetType().GetField(name, All)?.GetValue(target)
        ?? target.GetType().GetProperty(name, All).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, All).SetValue(target, value);
    private static object Call(Type type, object target, string name, params object[] args) => type.GetMethod(name, All).Invoke(target, args);
    private static bool PathPrefix(string fileName, ref string __result)
    {
        if (fileName != "SocialPromptTexts.json") throw new InvalidOperationException("Unexpected resource read: " + fileName);
        __result = _promptPath;
        return false;
    }
    private static bool CampaignPrefix(ref object __result) { __result = _campaign; return false; }
    private static bool OptionsPrefix(ref object __result) { __result = _options; return false; }
    private static bool NoLog() => false;
    private static bool InvalidSummary(ref Task<string> __result) { __result = Task.FromResult("invalid fixture"); return false; }
    private static bool PromptLimit(ref int __result) { __result = 120; return false; }
    private static bool PlayerName(ref string __result) { __result = "Ada"; return false; }
    private static bool CurrentName(ref string __result) { __result = _fixtureName; return false; }
    private static bool Allowed(ref bool __result) { __result = true; return false; }
    private static bool Denied(ref bool __result) { __result = false; return false; }
    private static bool Known(ref bool __result) { __result = _known; return false; }

    private static void Run(Assembly af, string resourceRoot, string package)
    {
        var harmony = new Harmony("AF.PlayerStartingHistoryTests");
        _promptPath = Path.Combine(resourceRoot, package == "CN" ? "SocialPromptTexts.json" : "SocialPromptTexts.en.json");
        harmony.Patch(af.GetType("AnimusForge.AnimusForgeModulePaths", true).GetMethod("GetModuleDataFilePath"), prefix: new HarmonyMethod(typeof(Program), nameof(PathPrefix)));
        foreach (var method in af.GetType("AnimusForge.Logger", true).GetMethods(All).Where(m => m.IsStatic && m.ReturnType == typeof(void) && !m.ContainsGenericParameters))
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(Program), nameof(NoLog)));
        var prompt = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(_promptPath));
        Type host = af.GetType("AnimusForge.PlayerNotorietyBehavior", true);
        Type record = af.GetType("AnimusForge.PlayerStartingHistoryRecord", true);
        const string old = "Original player history: 原文 {UNTRANSLATED}.";
        const string name = "Ada {HERO} [[AF_PLAYER_START_V1:king]]";
        string Render(string raw) => (string)Call(host, null, "RenderPlayerHistoryTextForPrompt", raw, name);
        string Token(string id) => "[[AF_PLAYER_START_V1:" + id + "]]";
        object NewOwner() => Activator.CreateInstance(host);
        object State(object owner) => host.GetProperty("_state", All).GetValue(owner);
        bool Record(object owner, string id) => (bool)Call(host, owner, "RecordConfirmedStartingHistory", id);
        foreach (string id in Ids)
        {
            object owner = NewOwner(), state = State(owner);
            Set(state, "MajorSummary", old);
            Set(state, "WorldNotoriety", 12.5);
            ((IDictionary)Field(state, "CultureNotoriety"))["empire"] = 7.5;
            Check(Record(owner, " " + id.ToUpperInvariant() + " "), id + " accepted");
            Check((string)Field(state, "MajorSummary") == Token(id) + "\n" + old, id + " preserves original history");
            Check((bool)Field(state, "PlayerStartHistoryRecorded"), id + " recorded flag");
            Check((double)Field(state, "WorldNotoriety") == 12.5 && (double)((IDictionary)Field(state, "CultureNotoriety"))["empire"] == 7.5, id + " no fame change");
            Check(((IList)Field(state, "RecentActions")).Count == 0 && ((IList)Field(state, "MajorMaterials")).Count == 0, id + " no duplicate action or model materials");
            Check(!Record(owner, "king"), id + " repeat/session change ignored");
            string expected = string.Format(CultureInfo.InvariantCulture, prompt["player_start_" + id], name);
            Check(expected.Length > 50 && !expected.Contains("身份起步") && !expected.Contains("began this journey"), id + " concrete background replaces opening-option label");
            string rendered = Render((string)Field(state, "MajorSummary"));
            Check(rendered == expected + " " + old, id + " real prompt reader and consumer preserve dynamic text; actual=" + rendered + "; expected=" + expected + " " + old);
            string json = JsonConvert.SerializeObject(state);
            object loaded = JsonConvert.DeserializeObject(json, state.GetType());
            host.GetProperty("_state", All).SetValue(owner, loaded);
            Check(!Record(owner, id) && (bool)Field(loaded, "PlayerStartHistoryRecorded"), id + " JSON reload idempotent");
            Set(loaded, "MajorSummary", "");
            Check(!Record(owner, id) && (string)Field(loaded, "MajorSummary") == "", id + " manual clear respected after reload");
            object legacy = NewOwner();
            Set(State(legacy), "MajorSummary", Token(id) + "\r\n" + old);
            Check(Record(legacy, "beggar") && (string)Field(State(legacy), "MajorSummary") == Token(id) + "\r\n" + old, id + " missing flag does not duplicate canonical record");
            string fallback = (string)Call(record, null, "EnglishUiTemplate", id);
            foreach (string ui in new[] { "EN", "CN" })
            {
                string file = Path.Combine(resourceRoot, "Languages", ui == "EN" ? "afsocial_ui_strings.xml" : "CNs/afsocial_ui_strings-zh-CN.xml");
                var texts = XDocument.Load(file).Descendants("string").ToDictionary(x => (string)x.Attribute("id"), x => (string)x.Attribute("text"));
                string key = "AF_SOCIAL_UI_PlayerStart_" + id;
                Check(texts.ContainsKey(key) && texts[key].Contains("{PLAYER}") && !texts[key].Contains("{0}"), id + " " + ui + " UI variables");
                if (ui == "EN") Check(texts[key] == fallback, id + " fallback matches actual UI resource");
                // Emulate the game's loaded translation dictionary, then run the real
                // TextObject + literal-variable UI formatter; no Gauntlet is started.
                Type localized = Assembly.Load("TaleWorlds.Localization").GetType("TaleWorlds.Localization.LocalizedTextManager", true);
                Type manager = localized.Assembly.GetType("TaleWorlds.Localization.MBTextManager", true);
                manager.GetField("_activeTextLanguageId", All).SetValue(null, ui == "EN" ? "English" : "简体中文");
                var dictionary = (IDictionary)localized.GetField("_gameTextDictionary", All).GetValue(null);
                dictionary[key] = texts[key];
                string display = (string)Call(host, null, "RenderPlayerHistoryTextForDisplay", Token(id) + "\n" + old, name);
                Check(display == texts[key].Replace("{PLAYER}", name) + " " + old, id + " " + ui + " actual UI renderer independent of package; actual=" + display);
                Check(Render(Token(id)) == expected, id + " " + ui + " UI does not switch prompt package");
            }
        }
        foreach (string invalid in new[] { null, "", "unknown_mod_start", "king\nmerchant", "-1" })
        {
            object owner = NewOwner();
            Set(State(owner), "MajorSummary", old);
            Check(!Record(owner, invalid) && !(bool)Field(State(owner), "PlayerStartHistoryRecorded") && (string)Field(State(owner), "MajorSummary") == old, "unknown start not inferred");
        }
        foreach (string raw in new[] { old, "prefix " + Token("trader"), Token("trader") + " trailing", Token("unknown"), "[[AF_PLAYER_START_V1:trader]" })
            Check(Render(raw) == raw, "unrecognized or inline player-authored text preserved");
        object missingField = JsonConvert.DeserializeObject("{\"MajorSummary\":\"Legacy\"}", State(NewOwner()).GetType());
        Check(!(bool)Field(missingField, "PlayerStartHistoryRecorded"), "old JSON missing bool remains eligible");

        // The existing third-failure compaction must project before whitespace
        // normalization/truncation, otherwise the canonical line becomes inert.
        harmony.Patch(host.GetMethod("GetMajorPromptChars", All), prefix: new HarmonyMethod(typeof(Program), nameof(PromptLimit)));
        harmony.Patch(host.GetMethod("BuildSummaryPlayerDisplayName", All), prefix: new HarmonyMethod(typeof(Program), nameof(PlayerName)));
        harmony.Patch(af.GetType("AnimusForge.MyBehavior", true).GetMethod("CallAuxiliaryApiTextForExternal", All), prefix: new HarmonyMethod(typeof(Program), nameof(InvalidSummary)));
        object compact = NewOwner();
        Record(compact, "trader");
        Set(State(compact), "MajorSummary", Token("trader") + "\n" + new string('x', 300));
        Set(State(compact), "SummaryRetryCount", 2);
        ((Task)Call(host, compact, "ProcessSummaryAsync")).GetAwaiter().GetResult();
        string compacted = (string)Field(State(compact), "MajorSummary");
        string expandedMerchant = string.Format(prompt["player_start_trader"], "Ada");
        Check(compacted.StartsWith(expandedMerchant.Substring(0, Math.Min(80, expandedMerchant.Length)), StringComparison.Ordinal)
            && compacted.Length <= 120 && !compacted.Contains("AF_PLAYER_START_V1"), "third-failure fallback projects before trim; actual=" + compacted);
        Check((int)Field(State(compact), "SummaryRetryCount") == 0 && (double)Field(State(compact), "WorldNotoriety") == 0, "fallback does not award notoriety");

        Assembly game = Assembly.Load("TaleWorlds.CampaignSystem");
        Type hero = game.GetType("TaleWorlds.CampaignSystem.Hero", true);
        harmony.Patch(host.GetMethod("IsValidObserver", All), prefix: new HarmonyMethod(typeof(Program), nameof(Allowed)));
        harmony.Patch(host.GetMethod("DoesObserverKnowPlayer", All, null, new[] { hero }, null), prefix: new HarmonyMethod(typeof(Program), nameof(Known)));
        harmony.Patch(host.GetMethod("CanObserverKnowRecentActions", All, null, new[] { hero, typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Program), nameof(Denied)));
        harmony.Patch(host.GetMethod("GetCurrentGameDayIndex", All), prefix: new HarmonyMethod(typeof(Program), nameof(PromptLimit)));
        harmony.Patch(host.GetMethod("BuildPlayerHistoryDisplayName", All), prefix: new HarmonyMethod(typeof(Program), nameof(CurrentName)));
        harmony.Patch(af.GetType("AnimusForge.ProactiveNpcRequestBehavior", true).GetMethod("CanOfferExperience", All), prefix: new HarmonyMethod(typeof(Program), nameof(Allowed)));
        object experience = NewOwner();
        Record(experience, "trader");
        host.GetProperty("Instance", All).SetValue(null, experience);
        object observer = FormatterServices.GetUninitializedObject(hero);
        object Capture() => ((IList)Call(host, null, "CaptureExperience", observer, false, false))[0];
        string evidence = (string)Field(Capture(), "Evidence");
        Check(evidence == string.Format(prompt["player_start_trader"], "Ada"), "real proactive consumer projects before evidence budget");
        string cached = (string)Field(experience, "_experienceHistoryEvidence");
        Capture();
        Check(ReferenceEquals(cached, Field(experience, "_experienceHistoryEvidence")), "same history/name reuses shared evidence");
        _fixtureName = "Bea";
        Check((string)Field(Capture(), "Evidence") == string.Format(prompt["player_start_trader"], "Bea"), "renaming refreshes evidence with unchanged source history");
        _known = false;
        Check(((IList)Call(host, null, "CaptureExperience", observer, false, false)).Count == 0, "history still respects observer knowledge gate");
        host.GetProperty("Instance", All).SetValue(null, null);

        // Exercise the real session capture with actual AdvancedStartOptionsData.
        // Only Campaign.Current / AdvancedStartData getters are isolated.
        Type campaign = game.GetType("TaleWorlds.CampaignSystem.Campaign", true);
        _campaign = FormatterServices.GetUninitializedObject(campaign);
        harmony.Patch(campaign.GetProperty("Current").GetGetMethod(), prefix: new HarmonyMethod(typeof(Program), nameof(CampaignPrefix)));
        harmony.Patch(campaign.GetProperty("AdvancedStartData").GetGetMethod(), prefix: new HarmonyMethod(typeof(Program), nameof(OptionsPrefix)));
        object capture = NewOwner();
        Call(host, capture, "OnSessionLaunchedForStartingHistory", new object[] { null });
        Check(!(bool)Field(State(capture), "PlayerStartHistoryRecorded"), "absent start data remains eligible");
        _options = Activator.CreateInstance(game.GetType("TaleWorlds.CampaignSystem.AdvancedStartOptions.AdvancedStartOptionsData", true));
        Call(host, capture, "OnSessionLaunchedForStartingHistory", new object[] { null });
        Check(!(bool)Field(State(capture), "PlayerStartHistoryRecorded"), "empty start data does not assume default");
        var datum = Activator.CreateInstance(game.GetType("TaleWorlds.CampaignSystem.AdvancedStartOptions.AdvancedStartData`1", true).MakeGenericType(typeof(string)), "StartType", "Player", "trader");
        Call(_options.GetType(), _options, "AddData", datum);
        Call(host, capture, "OnSessionLaunchedForStartingHistory", new object[] { null });
        Check((string)Field(State(capture), "MajorSummary") == Token("trader"), "real typed advanced-start session capture");
        Call(host, capture, "OnSessionLaunchedForStartingHistory", new object[] { null });
        Check((string)Field(State(capture), "MajorSummary") == Token("trader"), "repeated real session capture once");
        string merchantText = string.Format(prompt["player_start_trader"], "Ada");
        foreach (string fact in package == "CN" ? new[] { "作坊", "商队", "护卫", "驮畜", "周转本钱" } : new[] { "workshop", "caravan", "guards", "pack animals", "working capital" })
            Check(merchantText.Contains(fact), "merchant background includes native opening fact: " + fact);
        string outlawText = string.Format(prompt["player_start_outlaw"], "Ada");
        Check(outlawText.Contains(package == "CN" ? "匪徒" : "bandits") && outlawText.Contains(package == "CN" ? "罪名" : "criminal charges"), "outlaw background includes followers and legal predicament");
    }
}
