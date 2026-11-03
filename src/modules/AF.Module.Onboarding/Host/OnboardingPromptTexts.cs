using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class OnboardingPromptTexts
{
    private static readonly PromptLanguageFile<Dictionary<string, string>> Values = new PromptLanguageFile<Dictionary<string, string>>("OnboardingPromptTexts.json", path =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            path, Encoding.UTF8)));

    internal static string ConnectionTest => Values.Value["ConnectionTest"];
}
