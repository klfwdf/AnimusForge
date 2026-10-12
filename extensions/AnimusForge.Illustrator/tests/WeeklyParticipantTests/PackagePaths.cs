using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

        internal static string Raw(string key) => RawValues.Value.TryGetValue(key, out string v) ? v : key;
        internal static string Prompt(string id, string fallback = "", params (string Name, string Value)[] variables)
        {
            string res = fallback;
            if (variables != null)
                foreach (var (n, v) in variables)
                    res = res.Replace("{" + n + "}", v ?? "");
            return res;
        }
        internal static string Title(string id, string fallback) => fallback;
    }
}

namespace TaleWorlds.Localization
{
    internal sealed class TextObject
    {
        private readonly string _text;
        internal TextObject(string text) => _text = text ?? "";
        public override string ToString() => _text.StartsWith("{=") && _text.IndexOf('}') >= 0 ? _text.Substring(_text.IndexOf('}') + 1) : _text;
    }
}