using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

// Raw facts and parser vocabulary are invariant. Only the prompt corpus follows package selection.
// Each resource is loaded once; material loops only perform dictionary lookups.
internal static class WeeklyMaterialTexts
{
    private static readonly Lazy<Dictionary<string, string>> RawValues = new Lazy<Dictionary<string, string>>(() =>
    {
        using (var stream = typeof(WeeklyMaterialTexts).Assembly.GetManifestResourceStream("AnimusForge.Weekly.MaterialCompatibility.json"))
        using (var reader = new StreamReader(stream, Encoding.UTF8))
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
    });
    private static readonly Lazy<Dictionary<string, string>> PromptValues = new Lazy<Dictionary<string, string>>(() =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("WeeklyMaterialPrompts.json"), Encoding.UTF8)));

    internal static string Raw(string key) => RawValues.Value[key];
    internal static string Prompt(string key) => PromptValues.Value[key];

    private static readonly Lazy<Dictionary<string, string>> SourceProjection = new Lazy<Dictionary<string, string>>(() =>
    {
        var projection = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in PromptValues.Value)
            if (RawValues.Value.TryGetValue(entry.Key, out string source)) projection[source] = entry.Value;
        return projection;
    });

    // Project derived parser labels only at the prompt boundary; facts and grouping keys retain their source values.
    internal static string ProjectRaw(string source)
        => source != null && SourceProjection.Value.TryGetValue(source, out string text) ? text : source;
}
