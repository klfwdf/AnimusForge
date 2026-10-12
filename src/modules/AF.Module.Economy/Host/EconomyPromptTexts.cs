using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

// Package selection supplies CN or EN at the same path, independently of UI language.
internal static class EconomyPromptTexts
{
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(
        () => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("EconomyPromptTexts.json"), Encoding.UTF8)));

    internal static string Get(string id) => Values.Value[id];

    private static readonly Lazy<Dictionary<string, string>> SourceProjection = new Lazy<Dictionary<string, string>>(() =>
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in Values.Value)
            if (EconomySourceTexts.TryGet(entry.Key, out string source))
                result[source] = entry.Value;
        return result;
    });

    // Used only for canonical labels whose original values also participate in alias resolution.
    internal static string ProjectSource(string source)
        => source != null && SourceProjection.Value.TryGetValue(source, out string text) ? text : source;

    internal static string ProjectPartyAssetName(string value)
    {
        if (value == null) return null;
        foreach (string key in new[] { "PartyAsset_Workshop", "PartyAsset_Caravan" })
        {
            string label = EconomySourceTexts.Get(key);
            if (value.StartsWith(label + EconomySourceTexts.Get("PartyAsset_OpenName"), StringComparison.Ordinal)
                && value.EndsWith(EconomySourceTexts.Get("PartyAsset_CloseName"), StringComparison.Ordinal))
                return Get(key) + value.Substring(label.Length);
        }
        return ProjectSource(value);
    }

    internal static string ProjectPrisonerSource(string source)
    {
        string suffix = EconomySourceTexts.Get("S777");
        return source != null && source.EndsWith(suffix, StringComparison.Ordinal)
            ? source.Substring(0, source.Length - suffix.Length) + Get("S777")
            : ProjectSource(source);
    }
}
