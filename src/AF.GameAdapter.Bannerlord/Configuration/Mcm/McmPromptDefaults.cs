using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

// Current defaults follow the content package; exact source identities only recognize old settings.
internal static class McmPromptDefaults
{
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(() =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("McmPromptDefaults.json"), new UTF8Encoding(false, true))));
    private static readonly Lazy<Dictionary<string, string>> SourceValues = new Lazy<Dictionary<string, string>>(() =>
    {
        using (Stream stream = typeof(McmPromptDefaults).Assembly.GetManifestResourceStream("AnimusForge.Mcm.PromptSourceCompatibility.json"))
        {
            if (stream == null) throw new InvalidDataException("Missing MCM prompt source compatibility resource.");
            using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false, true)))
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
        }
    });

    internal static string Get(string key) => Values.Value[key];
    internal static string Raw(string key) => SourceValues.Value[key];
}
