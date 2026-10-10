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
}
