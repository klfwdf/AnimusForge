using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

public partial class DuelSettings
{
    // These exact historical values identify untouched old configurations. They
    // are compatibility inputs, not current prompts or translated presentation.
    private static class LegacySettingsPrompts
    {
        private const string ResourceName = "AnimusForge.Defaults.LegacySettingsPrompts.json";
        private static readonly Dictionary<string, string> Values = Load();

        private sealed class Corpus
        {
            public int SchemaVersion;
            public Dictionary<string, string> Values;
        }

        internal static string Get(string key) => Values[key];

        private static Dictionary<string, string> Load()
        {
            using (Stream stream = typeof(DuelSettings).Assembly.GetManifestResourceStream(ResourceName))
            {
                if (stream == null) throw new InvalidDataException("Missing historical settings prompt resource.");
                using (var reader = new StreamReader(stream, new UTF8Encoding(false, true)))
                {
                    Corpus corpus = JsonConvert.DeserializeObject<Corpus>(reader.ReadToEnd(),
                        new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error, MaxDepth = 8 });
                    if (corpus == null || corpus.SchemaVersion != 1 || corpus.Values == null || corpus.Values.Count != 56)
                        throw new InvalidDataException("Invalid historical settings prompt corpus.");
                    foreach (var pair in corpus.Values)
                        if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrEmpty(pair.Value))
                            throw new InvalidDataException("Empty historical settings prompt entry.");
                    return new Dictionary<string, string>(corpus.Values, StringComparer.Ordinal);
                }
            }
        }
    }
}
