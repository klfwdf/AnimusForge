using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class LlmSourceTexts
{
    private const string ResourceName = "AnimusForge.Llm.RuntimeSources.json";
    private static readonly BilingualTextTable Table =
        BilingualTextTable.FromEmbedded(typeof(LlmSourceTexts).Assembly, ResourceName);
    internal static string Get(string id) => Table.Get(id);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string Source(string id) => Table.Chinese(id);
    internal static string[] Variants(string id) => Table.Variants(id);
    internal static bool Is(string value, string id) => Table.Is(value, id);
    /// <summary>English edition (the Chinese source when the entry is language-invariant).</summary>
    internal static string English(string id) => Table.English(id);


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

}
