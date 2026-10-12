using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace AnimusForge.SceneActions.Core
{
    internal static class SceneActionCatalogText
    {
        internal sealed class Entry
        {
            internal string Name { get; }
            internal string Description { get; }
            internal string[] Positive { get; }
            internal string[] Negative { get; }

            internal Entry(Dictionary<string, string> strings, string key)
            {
                Name = strings[key + "_Name"];
                Description = strings[key + "_Description"];
                Positive = strings[key + "_Positive"].Split('\n');
                Negative = strings[key + "_Negative"].Split('\n');
            }
        }

        private sealed class Catalog
        {
            internal readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
            internal string Format;
            internal string Examples;
            internal string Separator;
            internal Dictionary<string, string> Strings;
        }

        // Each language is parsed once; independent nested caches prevent cycles
        // while the immutable V3/V4 action contract initializes its legacy fields.
        private static class ChineseCache { internal static readonly Catalog Value = Load("zh-CN"); }
        private static class EnglishCache { internal static readonly Catalog Value = Load("en"); }
        private static class PackageCache { internal static readonly Catalog Value = Build(SceneActionPromptPackage.ReadSection("AF_SCENE_CATALOG_")); }

        private static Catalog For(bool useChinese) => SceneActionPromptPackage.IsLoaded && useChinese == SceneActionPromptPackage.UseChinese
            ? PackageCache.Value : useChinese ? ChineseCache.Value : EnglishCache.Value;
        internal static Entry Get(string key, bool useChinese) => For(useChinese).Entries[key];
        internal static string Prompt(string key, bool useChinese) => For(useChinese).Strings["Prompt_" + key];

        internal static string FormatEntry(string key, bool useChinese, bool includeExamples)
        {
            Catalog catalog = For(useChinese);
            Entry entry = catalog.Entries[key];
            string line = string.Format(CultureInfo.InvariantCulture, catalog.Format, key, entry.Name, entry.Description);
            return includeExamples
                ? line + string.Format(CultureInfo.InvariantCulture, catalog.Examples,
                    string.Join(catalog.Separator, entry.Positive), string.Join(catalog.Separator, entry.Negative))
                : line;
        }

        private static Catalog Load(string language)
        {
            string resource = "AnimusForge.SceneActions.Catalog." + language + ".xml";
            using (Stream stream = typeof(SceneActionCatalogText).Assembly.GetManifestResourceStream(resource))
            {
                if (stream == null) throw new InvalidDataException("Missing scene action text resource: " + language);
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersInDocument = 1024 * 1024
                };
                var document = new XmlDocument { XmlResolver = null };
                using (XmlReader reader = XmlReader.Create(stream, settings)) document.Load(reader);
                var strings = new Dictionary<string, string>(StringComparer.Ordinal);
                const string prefix = "AF_SCENE_CATALOG_";
                foreach (XmlNode node in document.SelectNodes("/base/strings/string"))
                {
                    var element = (XmlElement)node;
                    string id = element.GetAttribute("id");
                    string value = element.GetAttribute("text");
                    if (!id.StartsWith(prefix, StringComparison.Ordinal) || value.Length == 0)
                        throw new InvalidDataException("Invalid scene action text entry.");
                    strings.Add(id.Substring(prefix.Length), value);
                }
                return Build(strings);
            }
        }

        private static Catalog Build(Dictionary<string, string> strings)
        {
                var catalog = new Catalog
                {
                    Format = strings["Format"].Replace("{KEY}", "{0}").Replace("{NAME}", "{1}").Replace("{DESCRIPTION}", "{2}"),
                    Examples = strings["Examples"].Replace("{POSITIVE}", "{0}").Replace("{NEGATIVE}", "{1}"),
                    Separator = strings["Separator"], Strings = strings
                };
                foreach (string key in strings.Keys)
                    if (key.EndsWith("_Name", StringComparison.Ordinal))
                    {
                        string action = key.Substring(0, key.Length - "_Name".Length);
                        catalog.Entries.Add(action, new Entry(strings, action));
                    }
                if (catalog.Entries.Count != 27) throw new InvalidDataException("Incomplete scene action text catalog.");
                return catalog;
        }
    }
}
