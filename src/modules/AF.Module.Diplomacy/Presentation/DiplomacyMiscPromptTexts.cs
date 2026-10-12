using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class DiplomacyMiscPromptTexts
{
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(
        () => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("DiplomacyMiscPromptTexts.json"), Encoding.UTF8)));

    internal static string Get(string id, string englishFallback)
        => Values.Value.TryGetValue(id, out string text) ? text : englishFallback;

    internal static string GetRequired(string id)
    {
        if (Values.Value.TryGetValue(id, out string text) && !string.IsNullOrWhiteSpace(text))
            return text;
        throw new InvalidDataException("Missing required diplomacy prompt text: " + id);
    }
}
