using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

// Canonical facts, protocol aliases and historical matching text are invariant across editions.
// The corpus is read once; callers retain the original values and formatting.
internal static class EconomySourceTexts
{
    private static class Cache
    {
        internal static readonly Dictionary<string, string> Values = Load();

        private static Dictionary<string, string> Load()
        {
            using (Stream stream = typeof(EconomySourceTexts).Assembly.GetManifestResourceStream("AnimusForge.Defaults.EconomySourceTexts.json"))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
        }
    }

    internal static string Get(string id) => Cache.Values[id];
    internal static bool TryGet(string id, out string text) => Cache.Values.TryGetValue(id, out text);
}
