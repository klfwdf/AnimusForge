using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace TaleWorlds.Localization;

// Controlled resource lookup, not Bannerlord's native language engine.
public sealed class TextObject
{
    public readonly string Value;
    private readonly Dictionary<string, string> _variables = new();
    private static readonly Dictionary<string, string> Resources = new();

    public TextObject(string value, Dictionary<string, object> attributes = null) { Value = value ?? ""; }

    public static void UseLanguage(bool chinese)
    {
        Resources.Clear();
        foreach (string path in Directory.EnumerateFiles(AppContext.BaseDirectory, chinese ? "*-zh.xml" : "*-en.xml"))
        {
            foreach (var node in XDocument.Load(path).Descendants("string"))
                Resources[(string)node.Attribute("id")] = (string)node.Attribute("text");
        }
    }

    public void SetTextVariable(string name, object value) { _variables[name] = value?.ToString() ?? ""; }

    public override string ToString()
    {
        string text = Value;
        if (text.StartsWith("{=", StringComparison.Ordinal))
        {
            int end = text.IndexOf('}');
            if (end >= 0)
            {
                string id = text.Substring(2, end - 2);
                text = Resources.TryGetValue(id, out var localized) ? localized : text.Substring(end + 1);
            }
        }
        foreach (var variable in _variables) text = text.Replace("{" + variable.Key + "}", variable.Value);
        return text;
    }
}
