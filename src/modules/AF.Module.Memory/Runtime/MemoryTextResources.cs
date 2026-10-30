using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class MemoryTextResources
{
    private static readonly PromptLanguageFile<Dictionary<string, string>> Prompts = new PromptLanguageFile<Dictionary<string, string>>("MemoryPromptTexts.json", path =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            path, Encoding.UTF8)));
    private static readonly BilingualTextTable Sources =
        BilingualTextTable.FromEmbedded(typeof(MemoryTextResources).Assembly, "AnimusForge.Memory.SourceTexts.json");

    internal static string Prompt(string id) => Prompts.Value[id];
    internal static string Raw(string key) => Sources.Get(key);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string RawSource(string key) => Sources.Chinese(key);
    internal static string[] RawVariants(string key) => Sources.Variants(key);
    internal static bool IsRaw(string value, string key) => Sources.Is(value, key);
    internal static string FormatRaw(string id, params object[] values) => string.Format(Raw(id), values);
    internal static string FormatPrompt(string id, params object[] values) => string.Format(Prompt(id), values);
}
