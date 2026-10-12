using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TaleWorlds.Localization;

namespace AnimusForge;

internal static class AdapterRecordTexts
{
    private static class Cache
    {
        internal static readonly Dictionary<string, string> Values = Load();

        private static Dictionary<string, string> Load()
        {
            using (Stream stream = typeof(AdapterRecordTexts).Assembly.GetManifestResourceStream("AnimusForge.Adapter.RecordCompatibility.json"))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
        }
    }

    // Content language is selected by the package, independently of the UI.
    // Load only when transient material grammar is requested, never during raw capture.
    private static readonly Lazy<Dictionary<string, string>> PromptValues = new Lazy<Dictionary<string, string>>(() =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("AdapterRecordPromptTexts.json"), Encoding.UTF8)));
    private static readonly Regex Placeholder = new Regex(@"\{([A-Z_]+)\}", RegexOptions.Compiled);

    internal static string Prompt(string id) => PromptValues.Value[id];

    internal static string PromptActionKind(string actionKind, string original)
        => PromptValues.Value.TryGetValue("ActionKind_" + (actionKind ?? "").Trim().ToLowerInvariant(), out string value)
            ? value : original;

    // Only newly composed current-action text reaches this boundary. Saved history
    // is not rewritten, and inserted names are not searched or reinterpreted.
    internal static string CurrentActionActor(string text, string actor)
    {
        if (string.IsNullOrWhiteSpace(actor)) return text;
        if (text.StartsWith("You have ", StringComparison.Ordinal)) return actor + " has " + text.Substring(9);
        if (text.StartsWith("You are ", StringComparison.Ordinal)) return actor + " is " + text.Substring(8);
        return text;
    }

    internal static string PromptFormat(string id, params (string Name, string Value)[] variables)
        => Placeholder.Replace(Prompt(id), match =>
        {
            foreach (var variable in variables)
                if (variable.Name == match.Groups[1].Value) return variable.Value ?? "";
            return match.Value;
        });

    internal static string Raw(string id) => Cache.Values[id];

    internal static string T(string id, string fallback)
        => new TextObject("{=" + id + "}" + fallback).ToString();

}
