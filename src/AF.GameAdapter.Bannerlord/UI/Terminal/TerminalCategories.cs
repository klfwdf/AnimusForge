using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class TerminalCategories
{
    internal static readonly string All = Cache.Data.Categories[nameof(All)].LegacyValue;
    internal static readonly string War = Cache.Data.Categories[nameof(War)].LegacyValue;
    internal static readonly string Diplomacy = Cache.Data.Categories[nameof(Diplomacy)].LegacyValue;
    internal static readonly string Prompt = Cache.Data.Categories[nameof(Prompt)].LegacyValue;
    internal static readonly string Ai = Cache.Data.Categories[nameof(Ai)].LegacyValue;
    internal static readonly string Duel = Cache.Data.Categories[nameof(Duel)].LegacyValue;
    internal static readonly string Scene = Cache.Data.Categories[nameof(Scene)].LegacyValue;
    internal static readonly string Characters = Cache.Data.Categories[nameof(Characters)].LegacyValue;

    internal static string DisplayText(string legacyValue)
        => legacyValue != null && Cache.Data.DisplayByLegacy.TryGetValue(legacyValue, out string text)
            ? text : Cache.Data.Categories[nameof(All)].DisplayText;

    private sealed class Category
    {
        public string LegacyValue;
        public string DisplayText;
    }

    private sealed class Corpus
    {
        public int SchemaVersion;
        public Dictionary<string, Category> Categories;
        [JsonIgnore] internal Dictionary<string, string> DisplayByLegacy;
    }

    private static class Cache
    {
        internal static readonly Corpus Data = Load();
    }

    // Stable routing values are read once. Display strings retain their original
    // localization keys and English fallbacks; no language lookup occurs here.
    private static Corpus Load()
    {
        const string resource = "AnimusForge.Defaults.TerminalCategories.json";
        using (Stream stream = typeof(TerminalCategories).Assembly.GetManifestResourceStream(resource))
        {
            if (stream == null) throw new InvalidDataException("Missing terminal category resource.");
            using (var reader = new StreamReader(stream, new UTF8Encoding(false, true)))
            {
                Corpus data = JsonConvert.DeserializeObject<Corpus>(reader.ReadToEnd(),
                    new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error, MaxDepth = 8 });
                if (data == null || data.SchemaVersion != 1 || data.Categories == null || data.Categories.Count != 8)
                    throw new InvalidDataException("Invalid terminal category resource.");
                data.DisplayByLegacy = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (string key in new[] { nameof(All), nameof(War), nameof(Diplomacy), nameof(Prompt), nameof(Ai), nameof(Duel), nameof(Scene), nameof(Characters) })
                {
                    if (!data.Categories.TryGetValue(key, out Category item) || item == null ||
                        string.IsNullOrEmpty(item.LegacyValue) || string.IsNullOrEmpty(item.DisplayText) ||
                        !item.DisplayText.StartsWith("{=AF_UI_Terminal_", StringComparison.Ordinal))
                        throw new InvalidDataException("Missing terminal category: " + key);
                    data.DisplayByLegacy.Add(item.LegacyValue, item.DisplayText);
                }
                return data;
            }
        }
    }
}
