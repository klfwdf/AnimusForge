using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace AnimusForge.SceneActions.Core
{
    // A package-level corpus, independent of the game's UI language. The host
    // supplies its resolved module path; Core never discovers or reads game state.
    public static class SceneActionPromptPackage
    {
        private static readonly object Sync = new object();
        private sealed class Corpus
        {
            internal bool UseChinese;
            internal readonly Dictionary<string, string> Strings =
                new Dictionary<string, string>(StringComparer.Ordinal);
        }

        private static volatile Corpus _corpus;
        internal static bool IsLoaded => _corpus != null;
        public static bool UseChinese => _corpus.UseChinese;
        public static string NativeConversationActionInstruction
            => _corpus.Strings["AF_SCENE_PACKAGE_NativeConversationActionInstruction"];
        public static string Text(string key) => _corpus.Strings["AF_SCENE_PACKAGE_" + key];

        public static void Load(string filePath)
        {
            if (_corpus != null) return;
            lock (Sync)
            {
                if (_corpus != null) return;
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
                _corpus = corpus;
            }
        }

        internal static Dictionary<string, string> ReadSection(string prefix)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> entry in _corpus.Strings)
                if (entry.Key.StartsWith(prefix, StringComparison.Ordinal))
                    result.Add(entry.Key.Substring(prefix.Length), entry.Value);
            return result;
        }
    }
}
