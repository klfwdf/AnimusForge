using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class TauntSourceTexts
{
    private const string ResourceName = "AnimusForge.Taunt.RuntimeSources.json";
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(Load, true);

    internal static string Get(string id) => Values.Value[id];

    internal static string Format(string id, params (string Name, string Value)[] variables)
    {
        string template = Get(id);
        Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var variable in variables ?? Array.Empty<(string Name, string Value)>())
            values[variable.Name ?? ""] = variable.Value ?? "";
        StringBuilder result = new StringBuilder(template.Length);
        for (int index = 0; index < template.Length;)
        {
            if (template[index] == '{')
            {
                int end = template.IndexOf('}', index + 1);
                if (end > index && values.TryGetValue(template.Substring(index + 1, end - index - 1), out string value))
                {
                    result.Append(value);
                    index = end + 1;
                    continue;
                }
            }
            result.Append(template[index++]);
        }
        return result.ToString();
    }

    private static Dictionary<string, string> Load()
    {
        using (Stream stream = typeof(TauntSourceTexts).Assembly.GetManifestResourceStream(ResourceName))
        using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false, true)))
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
    }
}
