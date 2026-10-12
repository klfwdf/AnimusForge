using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

// Canonical facts, protocol aliases and historical matching text are invariant across editions.
// The corpus is read once; callers retain the original values and formatting.
internal static class SocialSourceTexts
{
    private static class Cache
    {
        internal static readonly Dictionary<string, string> Values = Load();

        private static Dictionary<string, string> Load()
        {
            using (Stream stream = typeof(SocialSourceTexts).Assembly.GetManifestResourceStream("AnimusForge.Defaults.SocialSourceTexts.json"))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
        }
    }

    internal static string Get(string id) => Cache.Values[id];
}
