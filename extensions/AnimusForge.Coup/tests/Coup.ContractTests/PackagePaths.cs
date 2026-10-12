using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace AnimusForge
{
    internal static class AnimusForgeModulePaths
    {
        internal static string GetModuleDataFilePath(string file)
            => Path.Combine(AppContext.BaseDirectory, "ModuleData", Path.GetFileName(file));
    }

    internal static class WorldBulletinPolicyTexts
    {
        private static readonly Lazy<Dictionary<string, string>> RawValues = new Lazy<Dictionary<string, string>>(() =>
        {
            string root = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(root) && !File.Exists(Path.Combine(root, "AnimusForge.sln")) && !Directory.Exists(Path.Combine(root, "content")))
            {
                string parent = Path.GetDirectoryName(root);
                if (parent == root) break;
                root = parent;
            }
            string p = Path.Combine(root, "content", "modules", "AF.Module.Weekly", "ModuleData", "WorldBulletinCompatibility.json");
            if (!File.Exists(p)) return new Dictionary<string, string>();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(p, Encoding.UTF8));
        });

        internal static readonly Lazy<Dictionary<string, string>> ChineseStrings = new Lazy<Dictionary<string, string>>(() =>
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            string root = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(root) && !File.Exists(Path.Combine(root, "AnimusForge.sln")) && !Directory.Exists(Path.Combine(root, "content")))
            {
                string parent = Path.GetDirectoryName(root);
                if (parent == root) break;
                root = parent;
            }
            string[] paths = {
                Path.Combine(root, "content", "modules", "AF.Module.Weekly", "ModuleData", "Languages", "CNs", "afweekly_bulletin_strings-zh-CN.xml"),
                Path.Combine(root, "content", "modules", "AnimusForge.Coup", "ModuleData", "Languages", "CNs", "afcoup_strings-zh-CN.xml"),
                Path.Combine(root, "content", "modules", "AnimusForge.Coup", "ModuleData", "Languages", "CNs", "coup_strings-zh-CN.xml")
            };
            foreach (var p in paths)
            {
                if (File.Exists(p))
                {
                    var doc = XDocument.Load(p);
                    foreach (var el in doc.Descendants("string"))
                        dict[(string)el.Attribute("id")] = (string)el.Attribute("text");
                }
            }
            return dict;
        });

        internal static bool EnglishMode;

        internal static string Raw(string key) => RawValues.Value.TryGetValue(key, out string v) ? v : key;
        internal static string Prompt(string id, string fallback = "", params (string Name, string Value)[] variables)
        {
            string res = fallback;
            if (variables != null)
                foreach (var (n, v) in variables)
                    res = res.Replace("{" + n + "}", v ?? "");
            return res;
        }
        internal static string Title(string id, string fallback)
        {
            if (EnglishMode) return fallback;
            return ChineseStrings.Value.TryGetValue(id, out string cn) ? cn : fallback;
        }
    }
}

namespace TaleWorlds.Localization
{
    internal sealed class TextObject
    {
        private string _text;
        internal TextObject(string text)
        {
            _text = text ?? string.Empty;
            if (_text.StartsWith("{=") && _text.IndexOf('}') >= 0)
            {
                int end = _text.IndexOf('}');
                string id = _text.Substring(2, end - 2);
                string fallback = _text.Substring(end + 1);
                string cn;
                if (!AnimusForge.WorldBulletinPolicyTexts.EnglishMode && AnimusForge.WorldBulletinPolicyTexts.ChineseStrings.Value.TryGetValue(id, out cn))
                    _text = cn;
                else
                    _text = fallback;
            }
        }
        public TextObject SetTextVariable(string name, string value)
        {
            _text = _text.Replace("{" + name + "}", value ?? string.Empty);
            return this;
        }
        public TextObject SetTextVariable(string name, int value) => SetTextVariable(name, value.ToString());
        public override string ToString() => _text;
    }
}
