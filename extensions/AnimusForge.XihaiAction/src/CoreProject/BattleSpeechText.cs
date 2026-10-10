using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace AnimusForge.SceneActions.Core
{
    public static class BattleSpeechText
    {
        private sealed class Catalog
        {
            internal readonly Dictionary<string, string> Strings = new Dictionary<string, string>(StringComparer.Ordinal);
            internal readonly Dictionary<string, string[]> Lists = new Dictionary<string, string[]>(StringComparer.Ordinal);
        }

        private static class Chinese { internal static readonly Catalog Value = Load("zh-CN"); }
        private static class English { internal static readonly Catalog Value = Load("en"); }
        private static Catalog For(bool useChinese) => useChinese ? Chinese.Value : English.Value;
        internal static string Text(string key, bool useChinese) => For(useChinese).Strings[key];
        internal static string[] List(string key, bool useChinese) => For(useChinese).Lists[key + "_List"];
        public static string PlayerNameReplacement(bool useChinese) => Text("PlayerNameReplacement", useChinese);
        public static string FactLabel(string key, bool useChinese) => Text("FactLabel_" + key, useChinese);

        private static readonly string[] FactLabelKeys =
        {
            "UnknownBattleType", "UnknownPhase", "Siege", "SallyOut", "Field", "Battle", "Deployment", "Combat"
        };

        internal static string ProjectFactLabel(string value, bool useChinese)
        {
            // Eight frozen labels, evaluated only while building a prompt. Unknown
            // external labels remain intact; political/cultural names are separate.
            foreach (string key in FactLabelKeys)
                if (string.Equals(value, FactLabel(key, true), StringComparison.Ordinal) ||
                    string.Equals(value, FactLabel(key, false), StringComparison.Ordinal))
                    return FactLabel(key, useChinese);
            return value ?? string.Empty;
        }

        private static Catalog Load(string language)
        {
            using (Stream stream = typeof(BattleSpeechText).Assembly.GetManifestResourceStream(
                "AnimusForge.SceneActions.Speech." + language + ".xml"))
            {
                if (stream == null) throw new InvalidDataException("Missing battle speech text resource: " + language);
                var document = new XmlDocument { XmlResolver = null };
                using (XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1024 * 1024
                })) document.Load(reader);
                var catalog = new Catalog();
                const string prefix = "AF_SCENE_SPEECH_";
                foreach (XmlNode node in document.SelectNodes("/base/strings/string"))
                {
                    var element = (XmlElement)node;
                    string id = element.GetAttribute("id");
                    string text = element.GetAttribute("text");
                    if (!id.StartsWith(prefix, StringComparison.Ordinal) || text.Length == 0)
                        throw new InvalidDataException("Invalid battle speech text entry.");
                    string key = id.Substring(prefix.Length);
                    catalog.Strings.Add(key, text);
                    if (key.EndsWith("_List", StringComparison.Ordinal)) catalog.Lists.Add(key, text.Split('\n'));
                }
                if (catalog.Strings.Count == 0) throw new InvalidDataException("Empty battle speech text catalog.");
                return catalog;
            }
        }
    }
}
