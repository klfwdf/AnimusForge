using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

// Package selection supplies CN or EN at the same path, independently of UI language.
internal static class SocialPromptTexts
{
    private static readonly PromptLanguageFile<Dictionary<string, string>> Values = new PromptLanguageFile<Dictionary<string, string>>("SocialPromptTexts.json",
        path => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            path, Encoding.UTF8)));

    internal static string Get(string id) => Values.Value[id];
}
