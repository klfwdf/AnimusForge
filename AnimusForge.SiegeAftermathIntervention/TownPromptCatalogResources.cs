using System;
using System.IO;
using System.Text;
#if NETFRAMEWORK
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

namespace AnimusForge.SiegeAftermathIntervention;

// Legacy bool APIs choose an explicit language. The AF host passes its fixed package
// catalog directly through the new overloads. Each disk/embedded edition loads once.
public static class TownPromptCatalogResources
{
    private static readonly Lazy<TownPromptTextCatalog> Chinese = new Lazy<TownPromptTextCatalog>(() => Load(false), true);
    private static readonly Lazy<TownPromptTextCatalog> English = new Lazy<TownPromptTextCatalog>(() => Load(true), true);

    public static TownPromptTextCatalog Get(bool isEnglish) => isEnglish ? English.Value : Chinese.Value;

    private static TownPromptTextCatalog Load(bool isEnglish)
    {
        string fileName = isEnglish ? "GcczTownPrompt.en.json" : "GcczTownPrompt.zh-CN.json";
        string directory = Path.GetDirectoryName(typeof(TownPromptCatalogResources).Assembly.Location);
        for (int depth = 0; depth < 12 && !string.IsNullOrEmpty(directory); depth++)
        {
            string installed = Path.Combine(directory, "ModuleData", fileName);
            string checkout = Path.Combine(directory, "content", "modules", "AnimusForge.SiegeAftermathIntervention", "ModuleData", fileName);
            string path = File.Exists(installed) ? installed : (File.Exists(checkout) ? checkout : null);
            if (path != null)
            {
                try { return Deserialize(File.ReadAllText(path, Encoding.UTF8), isEnglish); }
                catch (Exception) when (isEnglish) { return TownPromptTextCatalog.CreateEnglishFallback(); }
            }
            directory = Path.GetDirectoryName(directory);
        }
        using Stream stream = typeof(TownPromptCatalogResources).Assembly.GetManifestResourceStream("AnimusForge.Defaults." + fileName);
        if (stream != null)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            try { return Deserialize(reader.ReadToEnd(), isEnglish); }
            catch (Exception) when (isEnglish) { return TownPromptTextCatalog.CreateEnglishFallback(); }
        }
        if (isEnglish) return TownPromptTextCatalog.CreateEnglishFallback();
        throw new FileNotFoundException("Chinese GCCZ prompt templates are missing.", fileName);
    }

    private static TownPromptTextCatalog Deserialize(string json, bool isEnglish)
    {
#if NETFRAMEWORK
        TownPromptTextCatalog parsed = JsonConvert.DeserializeObject<TownPromptTextCatalog>(json);
#else
        TownPromptTextCatalog parsed = JsonSerializer.Deserialize<TownPromptTextCatalog>(json);
#endif
        string language = isEnglish ? "en" : "zh-CN";
        if (parsed == null || (parsed.Language != language && !(isEnglish && parsed.Language == "en-US")))
            throw new InvalidDataException("GCCZ template edition does not match the requested language: " + language);
        return TownPromptTextCatalog.Resolve(parsed);
    }
}
