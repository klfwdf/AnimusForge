using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

// Prompt language follows the selected content package, independently of UI language.
internal static class PolicyMiscTexts
{
    private static readonly PromptLanguageFile<Dictionary<string, string>> Values = new PromptLanguageFile<Dictionary<string, string>>("PolicyMiscPromptTexts.json",
        path => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            path, Encoding.UTF8)));

    internal static string Get(string id, string englishFallback)
        => Values.Value.TryGetValue(id ?? "", out string text) ? text : englishFallback;
}
