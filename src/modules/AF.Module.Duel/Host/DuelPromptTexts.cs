using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class DuelPromptTexts
{
    private sealed class Payload
    {
        public List<string> DuelInstructionsHero { get; set; }
        public List<string> DuelInstructionsNonHero { get; set; }
    }

    private static readonly Lazy<Payload> Values = new Lazy<Payload>(Load);

    private static Payload Load()
    {
        string path = AnimusForgeModulePaths.GetModuleDataFilePath("DuelPromptTexts.json");
        return JsonConvert.DeserializeObject<Payload>(File.ReadAllText(path, Encoding.UTF8));
    }

    internal static IEnumerable<string> GetHeroInstructions() => Values.Value?.DuelInstructionsHero ?? Enumerable.Empty<string>();
    internal static IEnumerable<string> GetNonHeroInstructions() => Values.Value?.DuelInstructionsNonHero ?? Enumerable.Empty<string>();
}
