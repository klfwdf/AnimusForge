using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace AnimusForge.SceneActions.Core
{
    // Fixed scene prompt corpus in both languages. The host supplies the module path and a selector that
    // follows the game's text language; Core never reads game state itself. Each corpus loads once.
    public static class SceneActionPromptPackage
    {
        private static readonly object Sync = new object();
        private sealed class Corpus
        {
            internal bool UseChinese;
            internal readonly Dictionary<string, string> Strings =
                new Dictionary<string, string>(StringComparer.Ordinal);
        }

        private static volatile Corpus _chinese;
        private static volatile Corpus _english;
        private static volatile bool _loaded;
        private static Func<bool> _preferChinese;
        internal static bool IsLoaded => _loaded;
        // Falls back to the other edition only when one file is absent from the module.
        private static Corpus Current
            => (_preferChinese == null || _preferChinese() ? _chinese ?? _english : _english ?? _chinese);
        public static bool UseChinese => Current.UseChinese;
        public static string NativeConversationActionInstruction
            => Current.Strings["AF_SCENE_PACKAGE_NativeConversationActionInstruction"];
        public static string Text(string key) => Current.Strings["AF_SCENE_PACKAGE_" + key];

        /// <summary>Installs the language selector. Without one the Chinese corpus is used.</summary>
        public static void SetLanguageSelector(Func<bool> preferChinese) => _preferChinese = preferChinese;

        /// <summary>Loads <paramref name="filePath"/> and its <c>.en</c> sibling; each is filed by its own language attribute.</summary>
        public static void Load(string filePath)
        {
            if (_loaded) return;
            lock (Sync)
            {
                if (_loaded) return;
                string sibling = Path.Combine(Path.GetDirectoryName(filePath) ?? "",
                    Path.GetFileNameWithoutExtension(filePath) + ".en" + Path.GetExtension(filePath));
                Corpus chinese = null, english = null;
                foreach (string path in new[] { filePath, sibling })
                {
                    if (!ReferenceEquals(path, filePath) && !File.Exists(path)) continue;
                    Corpus corpus = Read(path);
                    if (corpus.UseChinese ? chinese != null : english != null)
                        throw new InvalidDataException("Scene prompt package contains two copies of one edition: " + path);
                    if (corpus.UseChinese) chinese = corpus; else english = corpus;
                }
                _chinese = chinese;
                _english = english;
                _loaded = true;
            }
        }

        private static Corpus Read(string filePath)
        {
            var document = new XmlDocument { XmlResolver = null };
            using (XmlReader reader = XmlReader.Create(filePath, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 1024 * 1024
            })) document.Load(reader);
            string language = document.DocumentElement.GetAttribute("language");
            if (language != "zh-CN" && language != "en")
                throw new InvalidDataException("Unsupported scene prompt package language: " + language);
            var corpus = new Corpus { UseChinese = language == "zh-CN" };
            foreach (XmlElement entry in document.SelectNodes("/base/strings/string"))
                corpus.Strings.Add(entry.GetAttribute("id"), entry.GetAttribute("text"));
            return corpus;
        }

        internal static bool HasCorpus(bool chinese) => chinese ? _chinese != null : _english != null;

        internal static Dictionary<string, string> ReadSection(string prefix, bool chinese)
        {
            Corpus corpus = chinese ? _chinese : _english;
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> entry in corpus.Strings)
                if (entry.Key.StartsWith(prefix, StringComparison.Ordinal))
                    result.Add(entry.Key.Substring(prefix.Length), entry.Value);
            return result;
        }
    }
}
