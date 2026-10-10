using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

// Compatibility input, not translated output. Read once on first projection;
// matching remains ordinal and existing Regex instances retain their lifetime.
internal static class EconomyDisplaySourceTexts
{
    internal static string Get(string key) => Cache.Values[key];

    private sealed class Corpus
    {
        public int SchemaVersion;
        public Dictionary<string, string> Values;
    }

    private static class Cache
    {
        internal static readonly Dictionary<string, string> Values = Load();
    }

    private static Dictionary<string, string> Load()
    {
        const string resource = "AnimusForge.Defaults.EconomyDisplaySources.json";
        using (Stream stream = typeof(EconomyDisplaySourceTexts).Assembly.GetManifestResourceStream(resource))
        {
            if (stream == null) throw new InvalidDataException("Missing economy display source resource.");
            using (var reader = new StreamReader(stream, new UTF8Encoding(false, true)))
            {
                Corpus data = JsonConvert.DeserializeObject<Corpus>(reader.ReadToEnd(),
                    new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error, MaxDepth = 8 });
                if (data == null || data.SchemaVersion != 1 || data.Values == null || data.Values.Count != 92)
                    throw new InvalidDataException("Invalid economy display source resource.");
                var result = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var pair in data.Values)
                {
                    if (string.IsNullOrEmpty(pair.Key) || string.IsNullOrEmpty(pair.Value))
                        throw new InvalidDataException("Empty economy display source entry.");
                    result.Add(pair.Key, pair.Value);
                }
                return result;
            }
        }
    }
}
