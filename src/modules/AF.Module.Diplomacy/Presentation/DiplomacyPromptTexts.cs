using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

// Both editions use the same path; packaging selects the CN or EN file.
// Load once per process. UI language changes do not change the packaged corpus.
internal static class DiplomacyPromptTexts
{
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(
        () => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("DiplomacyPromptTexts.json"), Encoding.UTF8)));

    internal static string Get(string id, string englishFallback)
        => Values.Value.TryGetValue(id, out string text) ? text : englishFallback;
}
