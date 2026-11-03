using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge.CoupSystem;

internal static class CoupPromptTexts
{
    private static readonly PromptLanguageFile<Dictionary<string, string>> Values = new PromptLanguageFile<Dictionary<string, string>>("CoupPromptTexts.json",
        path => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            path, Encoding.UTF8)));

    internal static string Get(string key) => Values.Value[key];
    internal static string Format(string key, params object[] values) => string.Format(Get(key), values);
}
