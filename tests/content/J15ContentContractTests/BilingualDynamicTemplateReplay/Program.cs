using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Localization;

internal static class Program
{
    internal static string PackageRoot;
    private static int checks;
    private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();
    private static readonly List<object> Negatives = new List<object>();
    private static readonly List<object> EngineBoundary = new List<object>();
    private static int restoredNativeSlots;
    private static int nativeUnchanged;
    private static object cacheEvidence;
    private static string category;
    private const string Literal = "名 {SPEAKER}/{TARGET_ID}/{player_gold}/{npcName}/$1";

    private static void Check(bool value, string label)
    {
        checks++;
        Counts[category] = Counts.TryGetValue(category, out int count) ? count + 1 : 1;
        if (!value) throw new InvalidOperationException(category + ": " + label);
    }

    // Independent literal scanner: only the original template is visited; inserted
    // strings are appended without scanning. Tests do not call production formatter
    // to compute their expected dynamic result.
    private static string Expand(string template, Dictionary<string, string> values)
    {
        var result = new StringBuilder();
        for (int i = 0; i < template.Length;)
        {
            if (template[i] == '{')
            {
                int close = template.IndexOf('}', i + 1);
                if (close >= 0)
                {
                    string name = template.Substring(i + 1, close - i - 1);
                    if (values.TryGetValue(name, out string value))
                    { result.Append(value ?? ""); i = close + 1; continue; }
                }
            }
            result.Append(template[i++]);
        }
        return result.ToString();
    }

    // Names are captured string leaves so the replay can feed literal brace data
    // into the formatter. Raw engine TextObject name parsing is tested separately.
    private static string Text(string value) => value;
    private static TownAmbientPlayerContext Player(string name, string title = "Title", string status = "Status",
        string clan = "Clan", string kingdom = "Realm", bool formal = false)
        => new TownAmbientPlayerContext(name, title, status, clan, kingdom, 1234, 12.5f, 3, formal, null);
    private static Settlement Town(string name = "Town", string owner = "Owner", float loyalty = 70f)
        => new Settlement { Name = Text(name), OwnerClan = new Clan { Leader = new Hero { Name = Text(owner) } }, Town = new Town { Loyalty = loyalty, Prosperity = 4567 } };
    private static NpcDataPacket Npc(string name = "NPC")
        => new NpcDataPacket { Name = name, CultureId = "Culture", RoleDesc = "Role", Age = 24 };

    private static void Register(string path, string language)
    {
        var document = new XmlDocument();
        document.LoadXml("<LanguageData id=\"" + language + "\" name=\"" + language
            + "\" supported_iso=\"" + (language == "English" ? "en" : "zh-CN")
            + "\" under_development=\"false\" text_processor=\"TaleWorlds.Localization.TextProcessor.LanguageProcessors.EnglishTextProcessor\"><LanguageFile xml_path=\""
            + Path.GetFileName(path) + "\" /></LanguageData>");
        Type data = typeof(TextObject).Assembly.GetType("TaleWorlds.Localization.LanguageData", true);
        data.GetMethod("LoadFromXml", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { document, Path.GetDirectoryName(path) });
    }

    private static void NativeProbe(string root)
    {
        category = "native-preformat-boundary";
        string baseDir = Path.Combine(root, "content/modules/AF.Module.Conversation/ModuleData/Languages");
        Register(Path.Combine(baseDir, "afconv_ambient_strings.xml"), "English");
        Register(Path.Combine(baseDir, "CNs/afconv_ambient_strings-zh-CN.xml"), "简体中文");
        foreach (string language in new[] { "English", "简体中文" })
        {
            Check(MBTextManager.ChangeLanguage(language), "real engine probe switch");
            string raw = new TextObject("{speaker}/{UNKNOWN}/{player_gold}/$1").ToString();
            string protectedText = new TextObject("{speaker}/{UNKNOWN}/{player_gold}/$1")
                .SetTextVariable("speaker", "{speaker}").SetTextVariable("UNKNOWN", "{UNKNOWN}")
                .SetTextVariable("player_gold", "{player_gold}").ToString();
            string rawName = new TextObject("Name " + Literal).ToString();
            const string id = "AF_CONV_AMBIENT_ruler_citizen_01_TEXT";
            string translated = LocalizedTextManager.GetTranslatedText(MBTextManager.ActiveTextLanguage, id);
            var xml = new XmlDocument(); xml.Load(Path.Combine(baseDir, language == "English" ? "afconv_ambient_strings.xml" : "CNs/afconv_ambient_strings-zh-CN.xml"));
            string rawXml = xml.SelectSingleNode("//string[@id='" + id + "']").Attributes["text"].Value;
            Check(language == "English" ? translated == null || translated == rawXml : translated == rawXml, "public native translation API returns raw named template; ACTUAL=" + (translated ?? "null") + "; EXPECTED=" + rawXml);
            string missing = LocalizedTextManager.GetTranslatedText(MBTextManager.ActiveTextLanguage, "AF_REPLAY_MISSING_ID");
            Check(missing == null, "public native API unknown ID returns null; ACTUAL=" + (missing ?? "null"));
            EngineBoundary.Add(new { language, rawNamedTemplate = raw, localNativeAttributeTemplate = protectedText, rawTextObjectName = rawName, capturedNameLeaf = "Name " + Literal,
                publicApiRawTemplate = translated, missingId = missing });
        }
        File.WriteAllText("engine-boundary.json", JsonConvert.SerializeObject(EngineBoundary, Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);
    }

    private static void Annexation()
    {
        category = "annexation";
        Fixture.Eligible = true;
        Fixture.Player = new Kingdom { StringId = "player", Name = Text("PlayerRealm") };
        Fixture.Target = new Kingdom { StringId = "target", Name = Text("TargetRealm") };
        Fixture.Speaker = new Hero { StringId = "speaker", Name = Text("Ruler") };
        var hero = Fixture.Speaker;
        string template = DiplomacyMiscPromptTexts.GetRequired("Annexation_NegotiationFact");
        string current = CurrentAnnexation.BuildRuntimeAnnexationInstructionForExternal(hero);
        Check(current == BeforeAnnexation.BuildRuntimeAnnexationInstructionForExternal(hero), "ordinary values preserve complete negotiation template");
        var expected = new Dictionary<string, string> {
            ["PLAYER_KINGDOM"] = "PlayerRealm", ["SPEAKER"] = "Ruler", ["TARGET_KINGDOM"] = "TargetRealm", ["TARGET_ID"] = "target" };
        Check(current == Expand(template, expected), "actual CN/EN resource rendered exactly");

        foreach (string value in new[] { Literal, "{SPEAKER}", "{TARGET_ID}", "{player_gold}", "{npcName}", "$1", "{UNKNOWN}", "{speaker}", "" })
        {
            Fixture.Player.Name = Text("Player " + value);
            Fixture.Target.Name = Text("Target " + value);
            Fixture.Target.StringId = "id " + value;
            Fixture.Speaker.Name = Text("Ruler " + value);
            expected["PLAYER_KINGDOM"] = "Player " + value;
            expected["TARGET_KINGDOM"] = "Target " + value;
            expected["TARGET_ID"] = "id " + value;
            expected["SPEAKER"] = "Ruler " + value;
            current = CurrentAnnexation.BuildRuntimeAnnexationInstructionForExternal(hero);
            Check(current == Expand(template, expected), "names/ID remain literal: " + value + "\nACTUAL " + current + "\nEXPECTED " + Expand(template, expected) + "\nNATIVE " + Fixture.Player.Name.ToString());
        }
        Fixture.Player.Name = Text("Realm " + Literal);
        Fixture.Target.Name = Text("Target"); Fixture.Target.StringId = "id42"; Fixture.Speaker.Name = Text("Ruler");
        string fixedText = CurrentAnnexation.BuildRuntimeAnnexationInstructionForExternal(hero);
        string oldText = BeforeAnnexation.BuildRuntimeAnnexationInstructionForExternal(hero);
        Check(fixedText.Contains("Realm " + Literal), "fixed annexation preserves player kingdom literal");
        Check(!oldText.Contains("Realm " + Literal) && oldText != fixedText, "before negative control reproduces annexation corruption");
        Negatives.Add(new { method = "BuildRuntimeAnnexationInstructionForExternal", input = Literal, before = oldText, after = fixedText });

        foreach (string value in new[] { (string)null, "", " ", "Ordinary" })
        {
            Fixture.Player.Name = Text(value); Fixture.Target.Name = Text(value); Fixture.Speaker.Name = Text(value); Fixture.Target.StringId = value;
            Check(CurrentAnnexation.BuildRuntimeAnnexationInstructionForExternal(hero) == BeforeAnnexation.BuildRuntimeAnnexationInstructionForExternal(hero), "old fallback/null/empty display and ID: " + (value ?? "null"));
        }
        var character = new CharacterObject { HeroObject = hero, StringId = "character" };
        CurrentAnnexation.BuildRuntimeAnnexationInstructionForExternal(null, character);
        Check(ReferenceEquals(Fixture.LastHero, hero), "character HeroObject fallback retained");
        CurrentAnnexation.BuildRuntimeAnnexationInstructionForExternal(hero, new CharacterObject { HeroObject = new Hero() });
        Check(ReferenceEquals(Fixture.LastHero, hero), "explicit hero wins");
        Fixture.Eligible = false;
        Check(CurrentAnnexation.BuildRuntimeAnnexationInstructionForExternal(null, character) == "" && Fixture.LastLog == "runtime.instruction.skip", "controlled ineligible returns empty and skips instruction");
        Check(BeforeAnnexation.BuildRuntimeAnnexationInstructionForExternal(null, character) == "", "before also skips controlled ineligible");
        Fixture.Eligible = true;
        const string unknown = "Unregistered {SPEAKER}/{npcName}/$1";
        Check(DiplomacyMiscPromptTexts.Get("Replay_Absent_Key", unknown) == unknown, "actual diplomacy unknown key preserves fallback literal");
    }

    private static IEnumerable<string> AmbientStrings(JObject data)
    {
        foreach (JObject line in data["Lines"])
        {
            if (line["Text"]?.Type == JTokenType.String) yield return (string)line["Text"];
            foreach (JToken text in line["TextVariants"] as JArray ?? new JArray())
                if (text.Type == JTokenType.String) yield return (string)text;
        }
    }

    private static void Ambient(string root, string language)
    {
        category = "ambient-native-registration";
        string baseDir = Path.Combine(root, "content/modules/AF.Module.Conversation/ModuleData/Languages");
        Register(Path.Combine(baseDir, "afconv_ambient_strings.xml"), "English");
        Register(Path.Combine(baseDir, "CNs/afconv_ambient_strings-zh-CN.xml"), "简体中文");
        var npc = Npc(); var town = Town(); var player = Player("Player");
        var lines = JObject.Parse(File.ReadAllText(Path.Combine(PackageRoot, "TownAmbientDialogue.json")));
        foreach (string uiLanguage in new[] { language == "cn" ? "简体中文" : "English", language == "cn" ? "English" : "简体中文", language == "cn" ? "简体中文" : "English" })
        {
            category = "ambient-native-registration";
            Check(MBTextManager.ChangeLanguage(uiLanguage), "real TextObject UI language switch " + uiLanguage);
            category = "ambient-packaged-ordinary";
            foreach (string text in AmbientStrings(lines))
            {
                string actual = CurrentAmbient.Call(text, npc, town, "market", player);
                Check(actual == BeforeAmbientRawBoundary.Call(text, npc, town, "market", player), "whole actual ambient Text/variant old formatter parity under controlled raw-localization leaf");
                if (actual == BeforeAmbient.Call(text, npc, town, "market", player)) nativeUnchanged++;
                else restoredNativeSlots++;
            }
            string expectedGreeting = uiLanguage == "English" ? "Good day" : "日安";
            category = "ambient-literal";
            // Exact expected native strings are sourced directly from the current XML.
            var xml = new XmlDocument(); xml.Load(Path.Combine(baseDir, uiLanguage == "English" ? "afconv_ambient_strings.xml" : "CNs/afconv_ambient_strings-zh-CN.xml"));
            expectedGreeting = xml.SelectSingleNode("//string[@id='AF_CONV_AMBIENT_TimeGreeting_Day']").Attributes["text"].Value;
            Check(CurrentAmbient.Call("{time_greeting}", npc, town, "market", player) == expectedGreeting, "actual ambient XML selected in real native engine; ACTUAL=" + CurrentAmbient.Call("{time_greeting}", npc, town, "market", player) + "; EXPECTED=" + expectedGreeting + "; TEXT_OBJECT=" + new TextObject("{time_greeting}").ToString());
            const string pattern = "speaker={speaker}|town={town}|owner={owner}|gold={player_gold}|unknown={SPEAKER}/{TARGET_ID}/{npcName}/$1";
            foreach (string value in new[] { Literal, "{player_gold}", "{player_name}", "{town}", "{npcName}", "$1", "{UNKNOWN}", "{PLAYER_GOLD}", "" })
            {
                string name = "NPC " + value;
                string actual = CurrentAmbient.Call(pattern, Npc(name), town, "market", player);
                string expected = "speaker=" + name + "|town=Town|owner=Owner|gold=1,234|unknown={SPEAKER}/{TARGET_ID}/{npcName}/$1";
                Check(actual == expected, "dynamic NPC name stays literal through real TextObject: " + value);
                Check(CurrentAmbient.Call("{player_name}", npc, town, "market", Player("Player " + value)) == ("Player " + value).Trim(), "dynamic player name literal: " + value);
                Check(CurrentAmbient.Call("{player_title}", npc, town, "market", Player("Player", "Title " + value)) == ("Title " + value).Trim(), "dynamic title literal");
                Check(CurrentAmbient.Call("{town}", npc, Town("Town " + value), "market", player) == ("Town " + value).Trim(), "captured settlement name literal");
                Check(CurrentAmbient.Call("{owner}", npc, Town("Town", "Owner " + value), "market", player) == ("Owner " + value).Trim(), "captured owner name literal");
                Check(CurrentAmbient.Call("{culture}|{role}|{scene}", new NpcDataPacket { Name = "NPC", CultureId = value, RoleDesc = value }, town, value, player) == value + "|" + value + "|" + value, "dynamic culture/role/scene literal");
                Check(CurrentAmbient.Call("{player_name}", npc, town, "market", Player("Player", "Title " + value, formal: true)) == ("Title " + value).Trim(), "formal title address literal");
                Check(CurrentAmbient.Call("{player_status}|{player_clan}|{player_kingdom}", npc, town, "market", Player("Player", status: value, clan: value, kingdom: value)) == value + "|" + value + "|" + value, "dynamic status/clan/kingdom literal");
            }
            string fixedText = CurrentAmbient.Call("{speaker} / {player_gold}", Npc(Literal), town, "market", player);
            string oldText = BeforeAmbientRawBoundary.Call("{speaker} / {player_gold}", Npc(Literal), town, "market", player);
            Check(fixedText == Literal + " / 1,234", "new ambient preserves later token in NPC name");
            Check(oldText != fixedText && !oldText.Contains("{player_gold}"), "before negative control reproduces ambient corruption");
            if (uiLanguage == (language == "cn" ? "简体中文" : "English") && !Negatives.Any(x => JsonConvert.SerializeObject(x).Contains("\"method\":\"Render\"")))
                Negatives.Add(new { method = "Render", input = Literal, uiLanguage, before = oldText, unmodifiedBeforeNative = BeforeAmbient.Call("{speaker} / {player_gold}", Npc(Literal), town, "market", player), after = fixedText, controlledLeaf = "raw template localization only" });
            foreach (string text in new[] { (string)null, "", " ", "ordinary", "{UNKNOWN}", "{Speaker}|{PLAYER_GOLD}|{npcName}", "{0}|$1", "\"quote\"", "prefix「spoken」tail" })
                Check(CurrentAmbient.Call(text, npc, town, null, null) == BeforeAmbientRawBoundary.Call(text, npc, town, null, null), "unknown/empty/quotation/default context old formatter parity under raw boundary");
            foreach (float loyalty in new[] { 0f, 24f, 25f, 59f, 60f, 100f })
                foreach (int hour in new[] { 0, 5, 7, 8, 16, 17, 19, 20, 23 })
                {
                    CampaignTime.Hour = hour;
                    var fallbackNpc = new NpcDataPacket();
                    var fallbackTown = Town(null, null, loyalty);
                    var fallbackPlayer = new TownAmbientPlayerContext(null, null, null, null, null, -1, -1, -1, true, null);
                    Check(CurrentAmbient.Call("{speaker}|{town}|{owner}|{culture}|{role}|{scene}|{tax_mood}|{time_greeting}|{time_period}|{player_name}|{player_title}|{player_status}|{player_clan}|{player_kingdom}|{player_gold}|{player_renown}|{player_tier}", fallbackNpc, fallbackTown, null, fallbackPlayer)
                        == BeforeAmbientRawBoundary.Call("{speaker}|{town}|{owner}|{culture}|{role}|{scene}|{tax_mood}|{time_greeting}|{time_period}|{player_name}|{player_title}|{player_status}|{player_clan}|{player_kingdom}|{player_gold}|{player_renown}|{player_tier}", fallbackNpc, fallbackTown, null, fallbackPlayer), "actual fallbacks/time/tax boundary ordinary parity");
                }
            CampaignTime.Hour = 12;
            Check(CurrentAmbient.Call("{town}|{owner}|{loyalty}|{prosperity}", npc, new Settlement { Village = new Village { Bound = town } }, "market", player)
                == BeforeAmbientRawBoundary.Call("{town}|{owner}|{loyalty}|{prosperity}", npc, new Settlement { Village = new Village { Bound = town } }, "market", player), "village bound owner/town resolution parity");
            category = "ambient-native-raw-helper";
            foreach (string plain in new[] { (string)null, "", " ", "plain {UNKNOWN}/$1", "{SPEAKER}/{TARGET_ID}", "{=unclosed", "prefix {=AF_CONV_AMBIENT_ruler_citizen_01_TEXT}fallback" })
                Check(CurrentAmbient.Localize(plain) == plain, "plain/null/unclosed/non-prefix template unchanged");
            foreach (string id in new[] { "!", "*", "AF_REPLAY_MISSING_ID", "af_conv_ambient_ruler_citizen_01_text" })
            {
                const string fallback = "{speaker} / {UNKNOWN} / $1";
                string template = "{=" + id + "}" + fallback;
                string translated = uiLanguage == "English" || id == "!" || id == "*" ? fallback
                    : LocalizedTextManager.GetTranslatedText(uiLanguage, id) ?? fallback;
                Check(CurrentAmbient.Localize(template) == translated, "native raw helper exact ID/fallback rule: " + id);
                Check(CurrentAmbient.Call(template, npc, town, "market", player)
                    == BeforeAmbientRawBoundary.Call(template, npc, town, "market", player), "unknown ID/explicit nonlocalizable marker ordinary old formatter parity");
                Check(CurrentAmbient.Call(template, Npc(Literal), town, "market", player)
                    == Expand(translated, new Dictionary<string,string> { ["speaker"] = Literal }).Trim(), "unknown ID/explicit nonlocalizable marker dynamic value literal");
            }
            const string resourceId = "AF_CONV_AMBIENT_ruler_citizen_01_TEXT";
            string actualRaw = xml.SelectSingleNode("//string[@id='" + resourceId + "']").Attributes["text"].Value;
            Check(CurrentAmbient.Localize("{=" + resourceId + "}" + (uiLanguage == "English" ? actualRaw : "unused fallback")) == actualRaw,
                "actual native CN raw translation and English fallback retain placeholders");
        }
        category = "cached-package-readers";
        int reads = AnimusForgeModulePaths.Reads;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            CurrentAmbient.Call("{speaker} / {player_gold}", Npc(Literal), town, "market", player);
            CurrentRuntime.Call("{playerName}/{npcName}", new Dictionary<string,string> { ["playerName"] = Literal, ["npcName"] = "Ruler" });
            CurrentAnnexation.BuildRuntimeAnnexationInstructionForExternal(Fixture.Speaker);
        }
        watch.Stop();
        Check(AnimusForgeModulePaths.Reads == reads, "warm repeated production methods do not reopen package readers");
        cacheEvidence = new { repetitions = 1000, methodsPerRepetition = 3, readsBefore = reads, readsAfter = AnimusForgeModulePaths.Reads, elapsedMilliseconds = watch.Elapsed.TotalMilliseconds,
            limitation = "single-process replay timing; no Tick, live-game allocation, renderer or actual IO throughput benchmark" };
    }

    private static IEnumerable<string> Strings(JToken token)
    {
        if (token.Type == JTokenType.String) { yield return (string)token; yield break; }
        foreach (JToken child in token.Children())
            foreach (string value in Strings(child)) yield return value;
    }

    private static void Runtime()
    {
        category = "runtime-actual-resources-ordinary";
        foreach (string name in new[] { "RuleBehaviorPrompts.json", "PromptComposerTexts.json", "ConversationRuntimePrompts.json" })
            foreach (string template in Strings(JToken.Parse(File.ReadAllText(Path.Combine(PackageRoot, name)))))
            {
                var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match match in Regex.Matches(template, "\\{([^{}]*)\\}"))
                    if (!string.IsNullOrWhiteSpace(match.Groups[1].Value)) tokens[match.Groups[1].Value] = "ordinary";
                Check(CurrentRuntime.Call(template, tokens) == BeforeRuntime.Call(template, tokens), "actual package ordinary template parity");
            }
        category = "runtime-literal";
        // This order mirrors BuildRewardRuntimeTokens: playerName precedes npcName.
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["playerName"] = Literal, ["npcName"] = "Ruler", ["trustCurrent"] = "80", ["trustIndex"] = "2", ["trustLevel"] = "trusted" };
        var rules = JObject.Parse(File.ReadAllText(Path.Combine(PackageRoot, "RuleBehaviorPrompts.json")));
        string realTemplate = Strings(rules).First(x => x.Contains("{playerName}"));
        string fixedText = CurrentRuntime.Call(realTemplate, values);
        string oldText = BeforeRuntime.Call(realTemplate, values);
        Check(fixedText == Expand(realTemplate, new Dictionary<string,string>(values, StringComparer.Ordinal)), "actual package playerName template dynamic literal");
        Check(fixedText.Contains(Literal) && !oldText.Contains(Literal) && oldText != fixedText, "before negative control reproduces runtime NPC-name corruption");
        Negatives.Add(new { method = "ApplyRuntimeTemplate", input = Literal, actualTemplate = realTemplate, before = oldText, after = fixedText });
        foreach (string value in new[] { Literal, "{SPEAKER}", "{TARGET_ID}", "{player_gold}", "{npcName}", "$1", "{UNKNOWN}", "{PLAYERNAME}", "", (string)null })
        {
            var tokens = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) { ["playerName"] = value, ["npcName"] = "NPC", ["SPEAKER"] = "Speaker", ["TARGET_ID"] = "ID", ["player_gold"] = "100" };
            const string template = "{playerName}|{npcName}|{SPEAKER}|{TARGET_ID}|{player_gold}|{unknown}|{PLAYERNAME}|$1";
            Check(CurrentRuntime.Call(template, tokens) == Expand(template, new Dictionary<string,string>(tokens, StringComparer.Ordinal)), "case-sensitive literal inserted value: " + value);
            var reverse = new Dictionary<string,string>(); foreach (var pair in tokens.Reverse()) reverse[pair.Key] = pair.Value;
            Check(CurrentRuntime.Call(template, reverse) == CurrentRuntime.Call(template, tokens), "result independent of dictionary insertion order");
        }
        foreach (string template in new[] { (string)null, "", " ", "unknown {X}/$1", "{playerName}/{PLAYERNAME}/{npcName}", "{}/{ }/{0}/{snake_key}/{a-b}/{ a }" })
            foreach (Dictionary<string,string> tokens in new[] { null, new Dictionary<string,string>(), new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) { ["playerName"] = "Alice", ["npcName"] = null, [""] = "ignored", [" "] = "ignored", ["0"] = "zero", ["snake_key"] = "snake", ["a-b"] = "dash", [" a "] = "spaced" } })
                Check(CurrentRuntime.Call(template, tokens) == BeforeRuntime.Call(template, tokens), "old unknown/case/null/whitespace/key grammar retained");
        const string custom = "Custom {npcName}: {NOT_REGISTERED} {0} $1";
        Check(CurrentRuntime.Call(custom, new Dictionary<string,string> { ["npcName"] = Literal }) == "Custom " + Literal + ": {NOT_REGISTERED} {0} $1", "unknown custom template remains supported");

        category = "actual-prompt-composer";
        Check(PromptComposerTexts.FormatTemplate("{SPEAKER}|{TARGET_ID}|{player_gold}|{npcName}|{UNKNOWN}|{speaker}",
            ("SPEAKER", Literal), ("TARGET_ID", Literal), ("player_gold", Literal), ("npcName", Literal))
            == Literal + "|" + Literal + "|" + Literal + "|" + Literal + "|{UNKNOWN}|{speaker}", "production composer one-pass and exact case");
        Check(PromptComposerTexts.FormatTemplate("{SPEAKER}/{UNKNOWN}", ("SPEAKER", (object)null)) == "/{UNKNOWN}", "composer null value empty and unknown untouched");
        category = "actual-fixed-package-readers";
        var composer = JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(Path.Combine(PackageRoot, "PromptComposerTexts.json")));
        Check(PromptComposerTexts.Get("identity_mercenary") == composer["identity_mercenary"], "actual lazy composer package reader");
        Check(PromptComposerTexts.Format("identity_mercenary", ("NAME", Literal))
            == Expand(composer["identity_mercenary"], new Dictionary<string,string> { ["NAME"] = Literal }), "actual composer package Format keeps dynamic name literal");
        var conversation = JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(Path.Combine(PackageRoot, "ConversationRuntimePrompts.json")));
        Check(ConversationRuntimeTexts.Prompt("letter_fallback_greeting") == conversation["letter_fallback_greeting"], "actual lazy conversation package reader");
        Check(ConversationRuntimeTexts.FormatPrompt("letter_fallback_greeting", ("PLAYER", Literal))
            == Expand(conversation["letter_fallback_greeting"], new Dictionary<string,string> { ["PLAYER"] = Literal }), "actual conversation package FormatPrompt keeps dynamic name literal");
    }

    internal static void Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.SetError(new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false)) { AutoFlush = true });
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            PackageRoot = args[0];
            NativeProbe(args[1]);
            if (args.Length > 4 && args[4] == "native-only")
            {
                File.WriteAllText(args[2] + "-result.json", JsonConvert.SerializeObject(new { api = args[3], package = args[2], assertions = checks, nativePreformatBoundary = EngineBoundary, negativeControls = Negatives }, Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);
                Console.WriteLine("PASS native probe " + args[3] + "/" + args[2] + ": " + checks + " assertions");
                return;
            }
            Annexation(); Runtime(); Ambient(args[1], args[2]);
            var result = new { api = args[3], package = args[2], assertions = checks, categories = Counts,
                negativeControls = Negatives, nativePreformatBoundary = EngineBoundary, engine = new { localization = typeof(TextObject).Assembly.FullName, location = typeof(TextObject).Assembly.Location },
                ambientLines = ((JArray)JObject.Parse(File.ReadAllText(Path.Combine(PackageRoot, "TownAmbientDialogue.json")))["Lines"]).Count,
                nativeRestoredSlotCases = restoredNativeSlots, nativeUnchangedCases = nativeUnchanged,
                ordinaryAmbientComparison = "exact before formatter with one controlled raw-localization leaf; unmodified before Render separately recorded", cachedReaders = cacheEvidence };
            File.WriteAllText(args[2] + "-result.json", JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);
            Console.WriteLine("PASS " + args[3] + "/" + args[2] + ": " + checks + " assertions; 3 before negative controls; actual package readers and real TextObject.");
        }
        catch (Exception error)
        {
            for (Exception current = error; current != null; current = current.InnerException)
                Console.Error.WriteLine(current.GetType().FullName + ": " + current.Message);
            Environment.Exit(1);
        }
    }
}

namespace AnimusForge
{
    // Controlled leaf for the BEFORE formatter only. The current whole method
    // must independently resolve localization through production code.
    internal static class ReplayNativeRawTemplate
    {
        internal static string Capture(string text)
        {
            if (text == null || !text.StartsWith("{=", StringComparison.Ordinal)) return text ?? "";
            int close = text.IndexOf('}');
            if (close < 0) return text;
            string fallback = text.Substring(close + 1);
            return MBTextManager.ActiveTextLanguage == "English" ? fallback
                : LocalizedTextManager.GetTranslatedText(MBTextManager.ActiveTextLanguage, text.Substring(2, close - 2)) ?? fallback;
        }
    }
    // Controlled input graph and captured name strings only. Native TextObject
    // in ambient rendering/native probes is never stubbed, and all
    // formatting, display-name fallbacks, player-title/status mappings, spoken
    // extraction, village-bound town resolution and time-band methods are exact.
    internal sealed class Hero { internal string Name; internal string StringId; }
    internal sealed class Kingdom { internal string Name; internal string StringId; }
    internal sealed class CharacterObject { internal Hero HeroObject; internal string StringId; }
    internal sealed class Clan { internal Hero Leader; }
    internal sealed class Settlement { internal string Name; internal Clan OwnerClan; internal Town Town; internal Village Village; }
    internal sealed class Village { internal Settlement Bound; }
    internal sealed class Town { internal float Loyalty; internal float Prosperity; }
    internal readonly struct CampaignTime { internal static int Hour = 12; internal static CampaignTime Now => new CampaignTime(); internal int GetHourOfDay => Hour; }
    internal static class Fixture { internal static bool Eligible; internal static Kingdom Player; internal static Kingdom Target; internal static Hero Speaker; internal static Hero LastHero; internal static string LastLog; }
    internal static class AnimusForgeModulePaths { internal static int Reads; internal static string GetModuleDataFilePath(string name) { Reads++; return Path.Combine(Program.PackageRoot, name); } }
}
