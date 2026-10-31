using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

/// <summary>
/// A runtime text table kept in Chinese (<c>X.json</c>) and English (<c>X.en.json</c>) with the same keys.
/// <see cref="Get"/> returns the current game language, so text composed now is written in that language.
/// An English value of <see cref="SameAsSource"/> marks a language-invariant identity (legacy saved value,
/// vanilla game string, parser token) that keeps the Chinese source in both languages.
/// Matching code that reads text written earlier, by the model or by the player must accept both
/// languages: use <see cref="Is"/>, <see cref="Variants"/> or <see cref="Chinese"/> instead of <see cref="Get"/>.
/// Each edition is parsed once on first use; reads are dictionary lookups.
/// </summary>
internal sealed class BilingualTextTable
{
    internal const string SameAsSource = "=";

    private readonly Lazy<Dictionary<string, string>> _chinese;
    private readonly Lazy<Dictionary<string, string>> _english;
    private readonly Lazy<Dictionary<string, string[]>> _variants;

    private BilingualTextTable(Func<Dictionary<string, string>> chinese, Func<Dictionary<string, string>> english)
    {
        _chinese = new Lazy<Dictionary<string, string>>(() => chinese() ?? new Dictionary<string, string>(StringComparer.Ordinal), true);
        _english = new Lazy<Dictionary<string, string>>(() => english() ?? new Dictionary<string, string>(StringComparer.Ordinal), true);
        _variants = new Lazy<Dictionary<string, string[]>>(BuildVariants, true);
    }

    /// <summary>Embedded resource pair: <paramref name="logicalName"/> and the same name with <c>.en.json</c>.</summary>
    internal static BilingualTextTable FromEmbedded(Assembly assembly, string logicalName)
        => new BilingualTextTable(
            () => ReadEmbedded(assembly, logicalName, required: true),
            () => ReadEmbedded(assembly, EnglishName(logicalName), required: false));

    /// <summary>ModuleData file pair: <paramref name="fileName"/> and its <c>.en.json</c> sibling.</summary>
    internal static BilingualTextTable FromModuleData(string fileName, Func<string, Dictionary<string, string>> parse = null)
    {
        Func<string, Dictionary<string, string>> reader = parse ?? ParseFlat;
        return new BilingualTextTable(
            () => reader(File.ReadAllText(AnimusForgeModulePaths.GetModuleDataFilePath(fileName), Encoding.UTF8)),
            () =>
            {
                string path = PromptLanguage.EnglishSibling(AnimusForgeModulePaths.GetModuleDataFilePath(fileName));
                return path != null && File.Exists(path) ? reader(File.ReadAllText(path, Encoding.UTF8)) : null;
            });
    }

    /// <summary>Caller-supplied loaders (for resources that need a custom reader).</summary>
    internal static BilingualTextTable FromLoaders(Func<Dictionary<string, string>> chinese, Func<Dictionary<string, string>> english)
        => new BilingualTextTable(chinese, english);

    /// <summary>Value in the current game language; an English entry that is absent or <c>=</c> keeps the Chinese source.</summary>
    internal string Get(string key) => PromptLanguage.IsChinese ? Chinese(key) : English(key);

    internal bool TryGet(string key, out string text)
    {
        if (!_chinese.Value.TryGetValue(key, out string chinese)) { text = null; return false; }
        text = PromptLanguage.IsChinese ? chinese : EnglishOr(key, chinese);
        return true;
    }

    internal string Chinese(string key) => _chinese.Value[key];

    internal string English(string key) => EnglishOr(key, _chinese.Value[key]);

    internal bool ContainsKey(string key) => _chinese.Value.ContainsKey(key);

    /// <summary>True when <paramref name="value"/> equals the entry in either language (ordinal).</summary>
    internal bool Is(string value, string key)
    {
        if (value == null) return false;
        string chinese = Chinese(key);
        return string.Equals(value, chinese, StringComparison.Ordinal)
            || string.Equals(value, EnglishOr(key, chinese), StringComparison.Ordinal);
    }

    /// <summary>The distinct Chinese and English values of an entry, Chinese first. Cached; do not modify.</summary>
    internal string[] Variants(string key) => _variants.Value[key];

    private readonly System.Collections.Concurrent.ConcurrentDictionary<(string Key, System.Text.RegularExpressions.RegexOptions Options), System.Text.RegularExpressions.Regex[]> _regexes =
        new System.Collections.Concurrent.ConcurrentDictionary<(string, System.Text.RegularExpressions.RegexOptions), System.Text.RegularExpressions.Regex[]>();

    /// <summary>Compiled regexes for every variant of a pattern entry, built once per key and options. Do not modify.</summary>
    internal System.Text.RegularExpressions.Regex[] Regexes(string key, System.Text.RegularExpressions.RegexOptions options = System.Text.RegularExpressions.RegexOptions.None)
        => _regexes.GetOrAdd((key, options), entry => Array.ConvertAll(Variants(entry.Key),
            pattern => new System.Text.RegularExpressions.Regex(pattern, entry.Options | System.Text.RegularExpressions.RegexOptions.Compiled)));

    private Dictionary<string, string[]> BuildVariants()
    {
        var result = new Dictionary<string, string[]>(_chinese.Value.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> entry in _chinese.Value)
        {
            string english = EnglishOr(entry.Key, entry.Value);
            result[entry.Key] = string.Equals(entry.Value, english, StringComparison.Ordinal)
                ? new[] { entry.Value } : new[] { entry.Value, english };
        }
        return result;
    }

    /// <summary>Current-language template with {NAME} placeholders replaced literally (values are never re-parsed).</summary>
    internal string Format(string key, params (string Name, string Value)[] variables) => Fill(Get(key), variables);

    /// <summary>Replaces {NAME} placeholders in <paramref name="template"/>; inserted values are not scanned again.</summary>
    internal static string Fill(string template, params (string Name, string Value)[] variables)
    {
        template = template ?? string.Empty;
        if (variables == null || variables.Length == 0 || template.IndexOf('{') < 0) return template;
        var result = new StringBuilder(template.Length + 32);
        for (int index = 0; index < template.Length;)
        {
            if (template[index] == '{')
            {
                int end = template.IndexOf('}', index + 1);
                if (end > index)
                {
                    string name = template.Substring(index + 1, end - index - 1);
                    bool found = false;
                    foreach (var variable in variables)
                        if (string.Equals(variable.Name, name, StringComparison.Ordinal)) { result.Append(variable.Value ?? string.Empty); found = true; break; }
                    if (found) { index = end + 1; continue; }
                }
            }
            result.Append(template[index++]);
        }
        return result.ToString();
    }

    /// <summary>All Chinese entries (for building value-to-key indexes over both languages).</summary>
    internal IReadOnlyDictionary<string, string> ChineseEntries => _chinese.Value;

    private string EnglishOr(string key, string chinese)
        => _english.Value.TryGetValue(key, out string english) && english != null
            && !string.Equals(english, SameAsSource, StringComparison.Ordinal) ? english : chinese;

    private static string EnglishName(string logicalName)
        => logicalName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? logicalName.Substring(0, logicalName.Length - 5) + ".en.json" : logicalName + ".en";

    private static Dictionary<string, string> ReadEmbedded(Assembly assembly, string logicalName, bool required)
    {
        using (Stream stream = assembly.GetManifestResourceStream(logicalName))
        {
            if (stream == null)
            {
                if (required) throw new InvalidDataException("Missing embedded text table: " + logicalName);
                return null;
            }
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return ParseFlat(reader.ReadToEnd());
        }
    }

    private static Dictionary<string, string> ParseFlat(string json)
    {
        var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(json ?? "");
        return values == null ? null : new Dictionary<string, string>(values, StringComparer.Ordinal);
    }

    /// <summary>Parser for tables grouped as <c>{"Section": {"Key": "text"}}</c>; keys become <c>Section.Key</c>.</summary>
    internal static Dictionary<string, string> ParseSections(string json)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (JProperty section in JObject.Parse(json ?? "{}").Properties())
            if (section.Value is JObject entries)
                foreach (JProperty entry in entries.Properties())
                    result.Add(section.Name + "." + entry.Name, (string)entry.Value);
        return result;
    }
}
