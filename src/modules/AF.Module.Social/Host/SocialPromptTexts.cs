using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

// Package selection supplies CN or EN at the same path, independently of UI language.
internal static class SocialPromptTexts
{
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(
        () => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("SocialPromptTexts.json"), Encoding.UTF8)));

    internal static string Get(string id) => Values.Value[id];
}
