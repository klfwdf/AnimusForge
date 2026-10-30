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
    private static readonly PromptLanguageFile<Dictionary<string, string>> Values = new PromptLanguageFile<Dictionary<string, string>>("PromptComposerTexts.json", path =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            path, Encoding.UTF8)));
    private static readonly BilingualTextTable Compatibility =
        BilingualTextTable.FromEmbedded(typeof(PromptComposerTexts).Assembly, "AnimusForge.Prompt.Compatibility.json");
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
        if (IsRaw(value, "weekly_memory_compat_871fa8d0ce6a")) return Get("opening_world_title");
        if (IsRaw(value, "weekly_memory_compat_2f59a5b54d55")) return Get("opening_world_label");
        foreach (var pair in OpeningSuffixes)
        {
            foreach (string suffix in RawVariants(pair.Item1))
                if (value.EndsWith(suffix, StringComparison.Ordinal))
                    return Format(pair.Item2, ("KINGDOM", value.Substring(0, value.Length - suffix.Length)));
        }
        return value;
    }
    internal static string Raw(string key) => Compatibility.Get(key);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string RawSource(string key) => Compatibility.Chinese(key);
    internal static string[] RawVariants(string key) => Compatibility.Variants(key);
    internal static bool IsRaw(string value, string key) => Compatibility.Is(value, key);
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
        => IsRaw(value, key)
            || string.Equals(value, Get(key), StringComparison.OrdinalIgnoreCase);
    internal static bool StartsWith(string value, string key)
        => Array.Exists(RawVariants(key), raw => value.StartsWith(raw, StringComparison.Ordinal))
            || Values.Value.TryGetValue(key, out string translated) && !string.IsNullOrEmpty(translated)
                && value.StartsWith(translated, StringComparison.OrdinalIgnoreCase);
}
