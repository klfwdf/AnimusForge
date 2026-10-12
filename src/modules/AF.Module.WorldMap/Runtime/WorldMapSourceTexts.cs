using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Queue identities and saved facts remain invariant across UI and content languages.
internal static class WorldMapSourceTexts
{
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(() =>
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var source = JObject.Parse(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("WorldMapCompatibility.json"), Encoding.UTF8));
        foreach (var section in source.Properties())
            if (section.Value is JObject entries)
                foreach (var entry in entries.Properties())
                    result.Add(section.Name + "." + entry.Name, (string)entry.Value);
        return result;
    });

    internal static string Get(string key) => Values.Value[key];
}
