using System;
using System.Collections.Generic;
using System.IO;

namespace AnimusForge.SiegeAftermathIntervention;

public static class SiegeAftermathCompatibilityTexts
{
    private static readonly Lazy<Dictionary<string, string>> Cached = new Lazy<Dictionary<string, string>>(Load);

    public static string Raw(string key)
    {
        if (key != null && Cached.Value.TryGetValue(key, out string val))
        {
            return val;
        }
        return key ?? string.Empty;
    }

    private static Dictionary<string, string> Load()
    {
        string current = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 12 && !string.IsNullOrEmpty(current); i++)
        {
            string candidate = Path.Combine(current, "content", "modules", "AnimusForge.SiegeAftermathIntervention", "ModuleData", "SiegeAftermathCompatibility.json");
            if (File.Exists(candidate))
            {
                try
                {
                    return Parse(File.ReadAllText(candidate));
                }
                catch { }
            }
            string candidate2 = Path.Combine(current, "ModuleData", "SiegeAftermathCompatibility.json");
            if (File.Exists(candidate2))
            {
                try
                {
                    return Parse(File.ReadAllText(candidate2));
                }
                catch { }
            }
            current = Path.GetDirectoryName(current);
        }

        try
        {
            var asm = typeof(SiegeAftermathCompatibilityTexts).Assembly;
            using (var stream = asm.GetManifestResourceStream("AnimusForge.Defaults.SiegeAftermathCompatibility.json"))
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return Parse(reader.ReadToEnd());
                    }
                }
            }
        }
        catch { }

        return new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private static Dictionary<string, string> Parse(string json)
    {
        return CompatibilityFlatJson.Parse(json);
    }
}
