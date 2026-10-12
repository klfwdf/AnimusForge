using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

// Both editions use the same path; packaging selects the CN or EN file.
// Load once per process. UI language changes do not change the packaged corpus.
internal static class DiplomacyPromptTexts
{
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(
        () => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("DiplomacyPromptTexts.json"), Encoding.UTF8)));

    internal static string Get(string id, string englishFallback)
        => Values.Value.TryGetValue(id, out string text) ? text : englishFallback;

    // Expand only tokens in the original template; names and player text are data.
    internal static string Format(string id, string englishFallback, params string[] variables)
    {
        if (variables == null || variables.Length % 2 != 0)
            throw new ArgumentException("Localization variables must be name/value pairs.");
        string template = Get(id, englishFallback) ?? string.Empty;
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int index = 0; index < variables.Length; index += 2)
            values[variables[index]] = variables[index + 1] ?? string.Empty;
        var result = new StringBuilder(template.Length);
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
