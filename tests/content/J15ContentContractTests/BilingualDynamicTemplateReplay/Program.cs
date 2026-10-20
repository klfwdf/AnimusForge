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
            Annexation(); Runtime();
            var result = new { api = args[3], package = args[2], assertions = checks, categories = Counts,
                negativeControls = Negatives, nativePreformatBoundary = EngineBoundary, engine = new { localization = typeof(TextObject).Assembly.FullName, location = typeof(TextObject).Assembly.Location },
                cachedReaders = cacheEvidence };
            File.WriteAllText(args[2] + "-result.json", JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented), Encoding.UTF8);
            Console.WriteLine("PASS " + args[3] + "/" + args[2] + ": " + checks + " assertions; 2 before negative controls; actual package readers and real TextObject.");
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
    // Controlled input names; production Annexation and Runtime methods are extracted unchanged.
    internal sealed class CharacterObject { internal Hero HeroObject; internal string StringId; }
    internal sealed class Hero { internal string Name; internal string StringId; }
    internal sealed class Kingdom { internal string Name; internal string StringId; }
    internal static class Fixture { internal static bool Eligible; internal static Kingdom Player; internal static Kingdom Target; internal static Hero Speaker; internal static Hero LastHero; internal static string LastLog; }
    internal static class AnimusForgeModulePaths { internal static int Reads; internal static string GetModuleDataFilePath(string name) { Reads++; return Path.Combine(Program.PackageRoot, name); } }
}
