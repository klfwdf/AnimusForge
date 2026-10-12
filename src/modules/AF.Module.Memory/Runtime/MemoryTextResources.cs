using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class MemoryTextResources
{
    private static readonly Lazy<Dictionary<string, string>> Prompts = new Lazy<Dictionary<string, string>>(() =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("MemoryPromptTexts.json"), Encoding.UTF8)));
    private static readonly Lazy<Dictionary<string, string>> Sources = new Lazy<Dictionary<string, string>>(() =>
    {
        using (Stream stream = typeof(MemoryTextResources).Assembly.GetManifestResourceStream("AnimusForge.Memory.SourceTexts.json"))
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
    });

    internal static string Prompt(string id) => Prompts.Value[id];
    internal static string Raw(string id) => Sources.Value[id];
    internal static string FormatRaw(string id, params object[] values) => string.Format(Raw(id), values);
    internal static string FormatPrompt(string id, params object[] values) => string.Format(Prompt(id), values);
}
