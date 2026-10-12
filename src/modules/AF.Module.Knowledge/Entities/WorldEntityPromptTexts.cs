using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class WorldEntityPromptTexts
{
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(
        () => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("WorldEntityPromptTexts.json"), Encoding.UTF8)));

    internal static string Get(string id, string englishFallback)
        => Values.Value.TryGetValue(id ?? "", out string text) ? text : englishFallback;
}
