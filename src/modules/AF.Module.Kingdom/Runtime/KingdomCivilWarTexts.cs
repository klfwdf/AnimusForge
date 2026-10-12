using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class KingdomCivilWarTexts
{
    private static readonly Lazy<Dictionary<string, string>> Prompts = new Lazy<Dictionary<string, string>>(
        () => Load("KingdomCivilWarPromptTexts.json"));
    private static readonly Lazy<Dictionary<string, string>> Compatibility = new Lazy<Dictionary<string, string>>(
        () => Load("KingdomCivilWarCompatibility.json"));

    private static Dictionary<string, string> Load(string file)
        => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath(file), Encoding.UTF8));

    internal static string Prompt(string id) => Prompts.Value[id];
    internal static string Raw(string id) => Compatibility.Value[id];
}
