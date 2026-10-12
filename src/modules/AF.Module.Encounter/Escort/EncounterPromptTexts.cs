using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class EncounterPromptTexts
{
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(Load);

    private static Dictionary<string, string> Load()
    {
        string path = AnimusForgeModulePaths.GetModuleDataFilePath("EncounterPromptTexts.json");
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path, Encoding.UTF8));
    }

    internal static string Get(string key) => Values.Value[key];

    internal static string BuildPromptInstruction(string heroName, string tag)
        => Regex.Replace(Get("SceneExecutionPromptInstruction"), "\\{(NAME|TAG)\\}",
            match => match.Groups[1].Value == "TAG" ? tag ?? string.Empty
                : string.IsNullOrWhiteSpace(heroName) ? Get("DefaultPrisonerName") : heroName);

    internal static string BuildPostprocessRule(string heroName)
        => Get("SceneExecutionPostprocessRule")
            .Replace("{NAME}", string.IsNullOrWhiteSpace(heroName) ? Get("DefaultPrisonerName") : heroName);
}
