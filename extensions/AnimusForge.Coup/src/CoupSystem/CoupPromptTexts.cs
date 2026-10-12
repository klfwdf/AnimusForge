using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge.CoupSystem;

internal static class CoupPromptTexts
{
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(
        () => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("CoupPromptTexts.json"), Encoding.UTF8)));

    internal static string Get(string key) => Values.Value[key];
    internal static string Format(string key, params object[] values) => string.Format(Get(key), values);
}
