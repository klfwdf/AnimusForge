using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace AnimusForge.SceneActions.Core
{
    // Input compatibility vocabulary is language-independent: both languages must
    // remain recognizable regardless of the currently selected display language.
    internal static class SceneActionVocabulary
    {
        private const string ResourceName = "AnimusForge.Defaults.SceneActionVocabulary.xml";

        internal static string[] List(string key)
        {
            return Cache.Lists[key];
        }

        private static class Cache
        {
            internal static readonly Dictionary<string, string[]> Lists = Load();
        }

        // Initialized once, on first use; no file access or parsing in match loops.
        private static Dictionary<string, string[]> Load()
        {
            using (Stream stream = typeof(SceneActionVocabulary).Assembly.GetManifestResourceStream(ResourceName))
            {
                if (stream == null)
                    throw new InvalidDataException("Missing scene action vocabulary resource.");
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersInDocument = 1024 * 1024
                };
                var document = new XmlDocument { XmlResolver = null };
                using (XmlReader reader = XmlReader.Create(stream, settings))
                    document.Load(reader);
                XmlElement root = document.DocumentElement;
                if (root == null || root.Name != "vocabulary" || root.GetAttribute("version") != "1")
                    throw new InvalidDataException("Unsupported scene action vocabulary schema.");
                var lists = new Dictionary<string, string[]>(StringComparer.Ordinal);
                foreach (XmlNode node in root.ChildNodes)
                {
                    if (!(node is XmlElement list) || list.Name != "list")
                        throw new InvalidDataException("Invalid scene action vocabulary list.");
                    string id = list.GetAttribute("id");
                    if (string.IsNullOrWhiteSpace(id) || lists.ContainsKey(id))
                        throw new InvalidDataException("Missing or duplicate scene action vocabulary id.");
                    var tokens = new List<string>();
                    foreach (XmlNode child in list.ChildNodes)
                    {
                        if (!(child is XmlElement token) || token.Name != "token" || string.IsNullOrEmpty(token.InnerText))
                            throw new InvalidDataException("Invalid scene action vocabulary token.");
                        tokens.Add(token.InnerText);
                    }
                    if (tokens.Count == 0)
                        throw new InvalidDataException("Empty scene action vocabulary list.");
                    lists.Add(id, tokens.ToArray());
                }
                return lists;
            }
        }
    }
}
