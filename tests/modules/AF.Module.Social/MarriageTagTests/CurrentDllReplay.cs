using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using AnimusForge;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Adapters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

class CurrentDllReplay
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static readonly Assembly Af = typeof(RomanceSystemBehavior).Assembly;
    static readonly Type Host = typeof(RomanceSystemBehavior);
    static int checks;
    static void Check(bool value, string label) { checks++; if (!value) throw new Exception(label); }
    static object Empty(Type type) { return FormatterServices.GetUninitializedObject(type); }
    static void Set(object target, string name, object value)
    {
        Type t = target as Type ?? target.GetType(); object instance = target is Type ? null : target;
        for (; t != null; t = t.BaseType)
        {
            FieldInfo f = t.GetField(name, All | BindingFlags.DeclaredOnly);
            if (f != null) { f.SetValue(instance, value); return; }
            PropertyInfo p = t.GetProperty(name, All | BindingFlags.DeclaredOnly);
            if (p != null && p.GetSetMethod(true) != null) { p.SetValue(instance, value, null); return; }
        }
        throw new Exception("Fixture member missing: " + name);
    }
    static object Call(string name, object target, params object[] values) { return Host.GetMethod(name, All).Invoke(target, values); }
    static Clan NewClan(string id)
    {
        Clan c = (Clan)Empty(typeof(Clan)); Set(c, "StringId", id); Set(c, "Name", new TextObject(id));
        Set(c, "_heroesCache", new MBList<Hero>()); Set(c, "_companionsCache", new MBList<Hero>()); return c;
    }
    static Hero NewHero(string id, Clan clan, int age, bool female)
    {
        Hero h = (Hero)Empty(typeof(Hero)); Set(h, "StringId", id);
        string name = id == "test_daughter" ? "Lina / 莉娜" : id == "test_leader" ? "Oren / 奥伦" : id;
        Set(h, "_name", new TextObject(name));
        Set(h, "_defaultAge", (float)age); Set(h, "_clan", clan); Set(h, "IsFemale", female);
        CharacterObject ch = (CharacterObject)Empty(typeof(CharacterObject)); Set(ch, "_heroObject", h); Set(h, "_characterObject", ch);
        return h;
    }
    static void Corpus(string root, string language)
    {
        string suffix = language == "CN" ? "" : ".en";
        var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(root, "content/modules/AF.Module.Social/ModuleData/SocialPromptTexts" + suffix + ".json")));
        Af.GetType("AnimusForge.SocialPromptTexts", true).GetField("Values", All).SetValue(null, new Lazy<Dictionary<string, string>>(() => values));
    }
    static string ConfiguredModel(JObject settings, JObject cache, string prefix, string optionsName)
    {
        string manual = (string)settings[prefix + "ModelName"] ?? "";
        int index = (int?)settings[prefix == "ActionPostprocess" ? "ActionPostprocessModelDropdown" : "MainModelDropdown"] ?? 0;
        var options = cache[optionsName] as JArray;
        if (index == 0) return manual.Trim();
        if (options == null || index > options.Count) throw new Exception("Configured model dropdown cannot be resolved");
        return ((string)options[index - 1]).Trim();
    }
    static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        try { return Run(args); }
        catch (Exception ex) { Console.WriteLine("FAIL " + ex.GetType().Name + ": " + ex.Message); return 1; }
    }
    static int Run(string[] args)
    {
        string root = args[0], output = args[1]; bool live = args.Contains("--live");
        Campaign campaign = (Campaign)Empty(typeof(Campaign)); Set(typeof(Campaign), "Current", campaign);
        CampaignOptions options = (CampaignOptions)Empty(typeof(CampaignOptions)); Set(campaign, "Options", options);
        Set(options, "_isLifeDeathCycleDisabled", true);
        Clan pc = NewClan("test_player_clan"), tc = NewClan("test_other_clan");
        Hero player = NewHero("test_player", pc, 25, false), brother = NewHero("test_brother", pc, 30, false),
            sister = NewHero("test_sister", pc, 22, true), companion = NewHero("test_companion", pc, 28, false),
            leader = NewHero("test_leader", tc, 48, false), daughter = NewHero("test_daughter", tc, 23, true),
            second = NewHero("test_second_daughter", tc, 21, true), relative = NewHero("test_relative", tc, 27, false);
        Set(pc, "_leader", player); Set(tc, "_leader", leader); Set(campaign, "PlayerDefaultFaction", pc);
        Game game = (Game)Empty(typeof(Game)); Set(typeof(Game), "Current", game); Set(game, "PlayerTroop", player.CharacterObject);
        var playerMembers = (MBList<Hero>)typeof(Clan).GetField("_heroesCache", All).GetValue(pc);
        playerMembers.Add(player); playerMembers.Add(brother); playerMembers.Add(sister);
        ((MBList<Hero>)typeof(Clan).GetField("_companionsCache", All).GetValue(pc)).Add(companion);
        var targetMembers = (MBList<Hero>)typeof(Clan).GetField("_heroesCache", All).GetValue(tc);
        foreach (Hero h in new[] { leader, daughter, second, relative }) targetMembers.Add(h);
        Set(daughter, "_father", leader); Set(second, "_father", leader);
        Hero mother = NewHero("test_mother", null, 55, true); Set(player, "_mother", mother); Set(brother, "_mother", mother); Set(sister, "_mother", mother);
        Hero[] players = ((IEnumerable<Hero>)Call("GetMarriageClanMembers", null, pc)).ToArray();
        Hero[] targets = ((IEnumerable<Hero>)Call("GetMarriageClanMembers", null, tc)).ToArray();
        Check(players.Length == 4 && players.Contains(companion) && players.Contains(sister), "actual DLL includes sibling and companion fixtures");
        Corpus(root, "CN");
        var host = new RomanceSystemBehavior();
        Check((bool)Call("CanExecuteFormalMarriageTagPair", null, leader, brother, daughter, true, null), "actual DLL family execution guard accepts exact partners");
        Check((bool)Call("CanExecuteFormalMarriageTagPair", null, leader, brother, leader, true, null), "actual DLL permits clan leader as actual partner");
        Check(!(bool)Call("CanExecuteFormalMarriageTagPair", null, daughter, brother, second, true, null), "actual DLL rejects nonleader approver");
        Check(!(bool)Call("CanExecuteFormalMarriageTagPair", null, leader, player, daughter, true, null), "actual DLL clan tag cannot substitute player");
        Check(!(bool)Call("CanExecuteElopeMarriageTagPair", null, leader, daughter, null), "actual DLL rejects third-person elopement");
        Type weekly = Af.GetType("AnimusForge.WeeklyMemoryMaterialPolicy", true);
        Check((bool)weekly.GetMethod("IsWeeklyMemoryMaterialMajorTag", All).Invoke(null, new object[] { "[ACTION:MARRIAGE_CLAN:test_brother:test_daughter]" }), "actual DLL weekly major-tag admission");
        Type memory = Af.GetType("AnimusForge.MemoryEditorProjection", true);
        var display = memory.GetMethod("WeeklyMaterialTagForDisplay", All);
        Check((string)display.Invoke(null, new object[] { null, "[ACTION:MARRIAGE_CLAN:test_brother:test_daughter]" })
            == (string)display.Invoke(null, new object[] { null, "[ACTION:MARRIAGE_FORMAL:test_brother:test_daughter]" }), "actual DLL memory UI retains formal marriage label");
        Type contextType = Host.GetNestedType("MarriagePostprocessContext", All);
        JObject settings = null, cache = null; string endpoint = "", key = "", model = "";
        if (live)
        {
            settings = JObject.Parse(File.ReadAllText(args[2])); cache = JObject.Parse(File.ReadAllText(args[3]));
            model = ConfiguredModel(settings, cache, "ActionPostprocess", "ActionPostprocessOptions");
            endpoint = (string)settings["ActionPostprocessApiUrl"] ?? ""; key = (string)settings["ActionPostprocessApiKey"] ?? "";
            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(model))
            { endpoint = (string)settings["ApiUrl"] ?? ""; key = (string)settings["ApiKey"] ?? ""; model = ConfiguredModel(settings, cache, "", "MainOptions"); }
            Check(!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(model), "configured postprocess or primary model is complete");
            endpoint = DuelSettings.GetEffectiveApiUrl(endpoint);
            Console.WriteLine("Live provider model=" + model + "; credentials remain in memory; synthetic dialogue only");
        }
        JArray results = new JArray();
        JArray cases = JArray.Parse(File.ReadAllText(Path.Combine(root, "tests/modules/AF.Module.Social/MarriageTagTests/cases.json")));
        foreach (string lang in new[] { "CN", "EN" })
        {
            Corpus(root, lang);
            string suffix = lang == "CN" ? "" : ".en";
            JObject rulesConfig = JObject.Parse(File.ReadAllText(Path.Combine(root, "content/modules/AF.Module.Prompt/ModuleData/RuleBehaviorPrompts" + suffix + ".json")));
            JObject marriage = rulesConfig["RulePrompts"].OfType<JObject>().Single(r => (string)r["Id"] == "marriage");
            JObject prompts = JObject.Parse(File.ReadAllText(Path.Combine(root, "content/modules/AF.Module.Prompt/ModuleData/ActionPostprocessPrompts" + suffix + ".json")));
            var allRules = marriage["PostprocessRules"].ToObject<List<PostprocessRuleEntry>>();
            foreach (JObject test in cases)
            {
                Set(second, "_name", new TextObject((bool?)test["duplicateNames"] == true ? "Lina / 莉娜" : "test_second_daughter"));
                Hero speaker = (string)test["speaker"] == "daughter" ? daughter : leader;
                bool formal = speaker == leader;
                object context = Call("CaptureMarriagePostprocessContextForExternal", host, speaker);
                Check(((HashSet<string>)contextType.GetField("PlayerHeroIds", All).GetValue(context)).SetEquals(players.Select(h => h.StringId)), "actual captured player members");
                Check((bool)contextType.GetField("FormalAllowed", All).GetValue(context) == formal, "actual captured approver authority");
                var rules = allRules.Where(r => formal || (!r.Tag.StartsWith("[ACTION:MARRIAGE_FORMAL:") && !r.Tag.StartsWith("[ACTION:MARRIAGE_CLAN:"))).ToList();
                string expected = (string)test["expected"];
                if (!live)
                {
                    string actual = (string)Call("NormalizeMarriagePostprocessTags", host, expected, rules, context);
                    Check(actual == expected, "actual DLL normalization " + (string)test["id"]);
                    continue;
                }
                string playerList = (string)contextType.GetField("PlayerCandidates", All).GetValue(context);
                string targetList = (string)contextType.GetField("TargetCandidates", All).GetValue(context);
                string tagRules = string.Join("\n", rules.Select(r => r.Tag + ": " + r.Description));
                string history = (string)test["history_" + lang] ?? "";
                string reply = "Player: " + (string)test["player_" + lang] + "\nNPC: " + (string)test["npc_" + lang];
                string user = (string)prompts["UserPromptTemplate"];
                foreach (var item in new Dictionary<string, string> {
                    {"history", history}, {"npc_name", "NPC"}, {"reply", reply}, {"tag_rules", tagRules},
                    {"marriage_player_candidates", playerList}, {"marriage_target_candidates", targetList},
                    {"shared_item_list", ""}, {"player_item_list", ""}, {"debt_hint", ""}, {"runtime_context", ""} })
                    user = user.Replace("{" + item.Key + "}", item.Value);
                string system = ((string)prompts["SystemPrompt"]).Replace("{mood_rules}", "[ACTION:MOOD:NEUTRAL]");
                object[] messages = { new { role = "system", content = system }, new { role = "user", content = user } };
                PromptPackage package = (PromptPackage)typeof(LegacyConfiguredChatGateway).GetMethod("BuildPromptPackage", All).Invoke(null, new object[] { messages, 13053, model });
                var gateway = new LegacyConfiguredChatGateway(_ => key, 0f, false, false, false, false, (bool?)settings["ActionPostprocessApiThinkingEnabled"] ?? false, "high");
                var request = new LlmGenerateRequest(new TraceContext("marriage-test-" + Guid.NewGuid().ToString("N"), 1, 1, "synthetic-marriage", "1.4"), new LlmProviderSnapshot("configured-postprocess", endpoint, model, 120000, 13053), package, InteractionStage.Postprocess);
                LlmGenerateResult generated = gateway.GenerateAsync(request, CancellationToken.None).GetAwaiter().GetResult();
                string normalized = generated.Status == LlmResultStatus.Succeeded ? (string)Call("NormalizeMarriagePostprocessTags", host, generated.RawText, rules, context) : "";
                bool passed = generated.Status == LlmResultStatus.Succeeded && normalized == expected;
                JObject record = new JObject(); record["id"] = (string)test["id"]; record["language"] = lang;
                record["status"] = generated.Status.ToString(); record["error"] = generated.ErrorCode; record["raw"] = generated.RawText;
                record["normalized"] = normalized; record["expected"] = expected; record["passed"] = passed; results.Add(record);
                File.WriteAllText(Path.Combine(output, "live-results.json"), results.ToString(), new UTF8Encoding(false));
                Console.WriteLine(lang + " " + (string)test["id"] + " " + generated.Status + " " + (passed ? "PASS" : "FAIL") + " normalized=" + normalized);
                // Transport failure is not a semantic result; diagnose it before
                // sending the rest of the corpus. Semantic mismatches keep running.
                if (generated.Status != LlmResultStatus.Succeeded) return 1;
            }
        }
        Console.WriteLine("PASS actual DLL fixture/normalization checks=" + checks + "; native campaign not started; save/marriage mutation not tested");
        return live && results.Any(r => !(bool)r["passed"]) ? 1 : 0;
    }
}
