using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class PromptComposerTexts
{
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(() =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("PromptComposerTexts.json"), Encoding.UTF8)));
    private static readonly Lazy<Dictionary<string, string>> Compatibility = new Lazy<Dictionary<string, string>>(() =>
    {
        using (var stream = typeof(PromptComposerTexts).Assembly.GetManifestResourceStream("AnimusForge.Prompt.Compatibility.json"))
        using (var reader = new StreamReader(stream, Encoding.UTF8))
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
    });
    private static readonly Regex Placeholder = new Regex(@"\{([A-Za-z_]+)\}", RegexOptions.Compiled);

    internal static string Get(string key) => Values.Value[key];
    // Kept as a compatibility seam for existing composers. Content edition
    // selects fixed Prompt resources, never the model's response language.
    internal static string ApplyOutputLanguage(string system) => system ?? string.Empty;
    private static readonly (string Source, string Target)[] OpeningSuffixes = { ("weekly_memory_compat_21d709ad6255", "opening_kingdom_title"), ("weekly_memory_compat_7443025bba34", "opening_kingdom_label") };

    // Request-boundary projection of exact known opening labels; user titles remain literal.
    internal static string ProjectOpeningTextForPrompt(string text)
    {
        string value = text ?? "";
        if (value == Raw("weekly_memory_compat_871fa8d0ce6a")) return Get("opening_world_title");
        if (value == Raw("weekly_memory_compat_2f59a5b54d55")) return Get("opening_world_label");
        foreach (var pair in OpeningSuffixes)
        {
            string suffix = Raw(pair.Item1);
            if (value.EndsWith(suffix, StringComparison.Ordinal))
                return Format(pair.Item2, ("KINGDOM", value.Substring(0, value.Length - suffix.Length)));
        }
        return value;
    }
    internal static string Raw(string key) => Compatibility.Value[key];
    internal static string Format(string key, params (string Name, object Value)[] variables)
        => FormatTemplate(Get(key), variables);
    internal static string FormatTemplate(string template, params (string Name, object Value)[] variables)
        => Placeholder.Replace(template, match =>
        {
            foreach (var variable in variables)
                if (variable.Name == match.Groups[1].Value)
                    return Convert.ToString(variable.Value, CultureInfo.CurrentCulture) ?? "";
            return match.Value;
        });
    internal static bool IsLabel(string value, string key)
        => string.Equals(value, Raw(key), StringComparison.Ordinal)
            || string.Equals(value, Get(key), StringComparison.OrdinalIgnoreCase);
    internal static bool StartsWith(string value, string key)
        => value.StartsWith(Raw(key), StringComparison.Ordinal)
            || Values.Value.TryGetValue(key, out string translated) && !string.IsNullOrEmpty(translated)
                && value.StartsWith(translated, StringComparison.OrdinalIgnoreCase);
}
