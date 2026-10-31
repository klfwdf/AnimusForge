using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

// Fixed prompt text follows the game language (zh-HANS/zh-HANT -> Chinese, all others -> English).
// The actual candidate DLL composers run in one process while PromptLanguage is flipped; every input is
// ASCII so any Han character in an English result comes from fixed text assembled in C#.
internal static class GameLanguageRequestChecks
{
    const BindingFlags AllStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    const BindingFlags AllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static readonly Regex Han = new Regex(@"[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]", RegexOptions.CultureInvariant);
    static string _gameLanguage = "English";

    // Exact arity first; otherwise the single overload whose remaining parameters are optional.
    static object Call(Type type, string name, params object[] args)
    {
        MethodInfo[] named = type.GetMethods(AllStatic).Where(m => m.Name == name).ToArray();
        MethodInfo method = named.SingleOrDefault(m => m.GetParameters().Length == args.Length)
            ?? named.Single(m => m.GetParameters().Length > args.Length && m.GetParameters().Skip(args.Length).All(p => p.IsOptional));
        ParameterInfo[] parameters = method.GetParameters();
        // Same arity: pass the caller array so out/ref results stay visible to the caller.
        if (parameters.Length == args.Length) return method.Invoke(null, args);
        object[] values = new object[parameters.Length];
        Array.Copy(args, values, args.Length);
        for (int i = args.Length; i < values.Length; i++) values[i] = parameters[i].DefaultValue;
        return method.Invoke(null, values);
    }

    // Installs the language source used by the product; returns a setter for the simulated game language.
    internal static Action<string> Install(Assembly product, string initialLanguage)
    {
        _gameLanguage = initialLanguage;
        Type language = product.GetType("AnimusForge.PromptLanguage", true);
        language.GetMethod("Install", AllStatic).Invoke(null, new object[] {
            (Func<string>)(() => _gameLanguage),
            (Func<string, bool>)(id => id == "简体中文" || id == "繁體中文") });
        return id => { _gameLanguage = id; language.GetMethod("Refresh", AllStatic).Invoke(null, null); };
    }

    // Fixed prompt file the product reads for the current game language (X.en.json beside X.json).
    internal static string FixedFile(Assembly product, string root, string fileName)
    {
        string path = Path.Combine(root, "ModuleData", fileName);
        string english = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".en" + Path.GetExtension(path));
        return !IsChinese(product) && File.Exists(english) ? english : path;
    }

    internal static bool IsChinese(Assembly product)
        => (bool)product.GetType("AnimusForge.PromptLanguage", true).GetProperty("IsChinese", AllStatic).GetValue(null);

    internal static void Run(Assembly product, string root)
    {
        string restore = _gameLanguage;
        Action<string> setLanguage = Install(product, restore);
        var failures = new List<string>();
        int checks = 0;
        void Check(bool ok, string label) { checks++; if (!ok) failures.Add(label); }
        const string literal = "literal {NAME} $1 ID=lord_S9_m; days=14; gold=-200";

        // Exact persisted/protocol identities kept on purpose (diplomacy contract validator markers).
        string[] stableMarkers = new string[0];
        using (var stream = product.GetManifestResourceStream("AnimusForge.Diplomacy.PromptCompatibility.json"))
            if (stream != null)
                using (var reader = new StreamReader(stream))
                {
                    JObject compatibility = JObject.Parse(reader.ReadToEnd());
                    stableMarkers = Enumerable.Range(0, 5).Select(i => (string)compatibility["AF_DIP_Compatibility_" + i.ToString("000")])
                        .Where(s => !string.IsNullOrEmpty(s)).ToArray();
                }
        string Fixed(string text)
        {
            string value = text ?? "";
            foreach (string marker in stableMarkers) value = value.Replace(marker, "");
            return value;
        }
        string Sample(string text)
        {
            Match match = Han.Match(text);
            int start = Math.Max(0, match.Index - 40);
            return text.Substring(start, Math.Min(120, text.Length - start)).Replace("\n", " ");
        }

        Type memory = product.GetType("AnimusForge.Refactor.Runtime.MemorySummaryRules", true);
        Type persona = product.GetType("AnimusForge.NpcPersonaTextRules", true);
        Type diplomacy = product.GetType("AnimusForge.Refactor.Domain.WorldDiplomacyPromptContractRules", true);
        Type diplomacyTexts = product.GetType("AnimusForge.DiplomacyPromptTexts", true);
        Func<string, string, string> localize = (key, fallback) => (string)Call(diplomacyTexts, "Get", key, fallback);
        Type weekly = product.GetType("AnimusForge.WeeklyPromptMaterialOwner", true);
        Type owner = product.GetType("AnimusForge.MainPromptMessageAssemblyOwner", true);
        Type llm = product.GetType("AnimusForge.LlmPromptTexts", true);
        Type composer = product.GetType("AnimusForge.PromptComposerTexts", true);

        // Each entry assembles one real system prompt (or request block) for the current language.
        var composers = new List<KeyValuePair<string, Func<string>>> {
            new KeyValuePair<string, Func<string>>("daily memory", () => (string)Call(memory, "DailySystemPrompt", 999, 3, "Player", literal)),
            new KeyValuePair<string, Func<string>>("major memory", () => (string)Call(memory, "MajorSystemPrompt", 280, literal)),
            new KeyValuePair<string, Func<string>>("overview memory", () => (string)Call(memory, "OverviewSystemPrompt", 280, literal)),
            new KeyValuePair<string, Func<string>>("persona native", () => (string)Prop(Call(persona, "BuildNative", literal, literal, true, literal, ""), "System")),
            new KeyValuePair<string, Func<string>>("persona promoted", () => (string)Prop(Call(persona, "BuildPromoted", literal, literal, literal, "lord_S9_m", literal, literal, literal, literal, literal, literal, literal), "System")),
            new KeyValuePair<string, Func<string>>("rebellion naming", () => (string)Call(product.GetType("AnimusForge.RebellionNamingRules", true), "BuildSystemPrompt", literal)),
            new KeyValuePair<string, Func<string>>("world bulletin", () => (string)Call(product.GetType("AnimusForge.WorldBulletinPolicy", true), "BuildSystemPrompt", 2, 3, literal)),
            new KeyValuePair<string, Func<string>>("diplomacy canonical history", () => (string)Call(diplomacy, "BuildCanonicalHistorySystemPrompt", literal, 100, 400, localize)),
            new KeyValuePair<string, Func<string>>("diplomacy analysis", () => (string)Call(diplomacy, "BuildAnalysisSystemPrompt", literal, localize)),
            new KeyValuePair<string, Func<string>>("diplomacy round compression", () => (string)Call(diplomacy, "BuildRoundCompressionSystemPrompt", localize)),
            new KeyValuePair<string, Func<string>>("diplomacy generation", () => (string)Call(diplomacy, "BuildGenerationSystemPrompt", literal, 300, 1200, localize)),
            new KeyValuePair<string, Func<string>>("diplomacy declare mode", () => (string)Call(diplomacy, "BuildDeclareModePrompt", literal, localize)),
            new KeyValuePair<string, Func<string>>("weekly report", () => (string)Call(weekly, "BuildWeeklyReportSystemPrompt", null, Call(weekly, "GetWeeklyReportPromptProfile", 200), literal)),
            new KeyValuePair<string, Func<string>>("courier reply", () => Messages(Call(owner, "BuildCourierReplyMessages", "Alda", "Player", literal, "", "", "", null, "", "", "", "", "", "", "", ""))),
            new KeyValuePair<string, Func<string>>("courier inbound", () => Messages(Call(owner, "BuildInboundNpcLetterMessages", "Alda", "Player", literal, "", "", "", null, "", "", "", "", "", "", "", 220, "private letter", ""))),
            new KeyValuePair<string, Func<string>>("llm fixed texts", () => (string)Call(llm, "Get", "AF_LLM_PROMPT_ContentLanguage", "zh-CN")),
            new KeyValuePair<string, Func<string>>("composer courier system", () => (string)Call(composer, "Get", "main_courier_reply_system")),
        };

        var english = new Dictionary<string, string>();
        setLanguage("English");
        Check(!IsChinese(product), "English game language selects English fixed prompts");
        foreach (var entry in composers)
        {
            string text;
            try { text = entry.Value(); }
            catch (Exception error) { failures.Add(entry.Key + " threw " + (error.InnerException ?? error).Message); continue; }
            english[entry.Key] = text ?? "";
            string audited = Fixed(text);
            Check(!Han.IsMatch(audited), "EN " + entry.Key + " has Chinese fixed text: ..." + (Han.IsMatch(audited) ? Sample(audited) : "") + "...");
        }

        setLanguage("繁體中文");
        Check(IsChinese(product), "Traditional Chinese game language selects Chinese fixed prompts");
        int switched = 0;
        foreach (var entry in composers)
        {
            string text;
            try { text = entry.Value() ?? ""; }
            catch (Exception error) { failures.Add(entry.Key + " threw under Chinese " + (error.InnerException ?? error).Message); continue; }
            if (english.TryGetValue(entry.Key, out string previous) && previous != text) switched++;
        }
        // Prompts built only from ASCII inputs must actually change; a frozen static cache would not.
        Check(switched >= composers.Count - 2, "fixed prompts follow a language switch without restart (switched=" + switched + "/" + composers.Count + ")");

        setLanguage("简体中文");
        Check(IsChinese(product), "Simplified Chinese game language selects Chinese fixed prompts");
        setLanguage("Deutsch");
        Check(!IsChinese(product), "any other game language selects English fixed prompts");
        setLanguage(restore);

        Console.WriteLine("Game-language request checks: " + checks + " actual DLL checks; failures=" + failures.Count + "; no model request.");
        foreach (string failure in failures) Console.WriteLine("FAIL " + failure);
        if (failures.Count > 0) throw new Exception("Game-language request checks failed.");
    }

    static object Prop(object value, string name) => value.GetType().GetProperty(name, AllInstance).GetValue(value);

    static string Messages(object list)
    {
        var parts = new List<string>();
        foreach (object message in (IEnumerable)list)
            parts.Add((string)message.GetType().GetProperty("content").GetValue(message));
        return string.Join("\n", parts);
    }
}
