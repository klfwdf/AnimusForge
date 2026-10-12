using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class AdapterPromptCaptureTexts
{
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(() =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("AdapterPromptCapture.json"), Encoding.UTF8)));
    private static readonly Lazy<Dictionary<string, string>> Compatibility = new Lazy<Dictionary<string, string>>(() =>
    {
        using (var stream = typeof(AdapterPromptCaptureTexts).Assembly.GetManifestResourceStream("AnimusForge.AdapterPromptCapture.Compatibility.json"))
        using (var reader = new StreamReader(stream, Encoding.UTF8))
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
    });
    private static readonly Regex Placeholder = new Regex(@"\{([A-Z_]+)\}", RegexOptions.Compiled);
    private static readonly Lazy<Dictionary<string, string>> ProjectionKeys = new Lazy<Dictionary<string, string>>(() =>
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in Compatibility.Value)
            if (entry.Key.StartsWith("Project_", StringComparison.Ordinal)) result[entry.Value] = entry.Key;
            else if (entry.Key.StartsWith("Alias_Project_", StringComparison.Ordinal)) result[entry.Value] = entry.Key.Substring("Alias_".Length);
        return result;
    });

    internal static string PcapText(string key) => Values.Value[key];
    internal static string PcapText(string key, string englishFallback)
        => Values.Value.TryGetValue(key, out string text) ? text : englishFallback;
    internal static string PcapCompat(string key) => Compatibility.Value[key];
    internal static string PcapProjectKnown(string value)
        => value != null && ProjectionKeys.Value.TryGetValue(value, out string key) ? Values.Value[key] : value;
    internal static string PcapFormat(string key, string englishFallback, params (string Name, object Value)[] variables)
        => Placeholder.Replace(PcapText(key, englishFallback), match =>
        {
            foreach (var variable in variables)
                if (variable.Name == match.Groups[1].Value)
                    return Convert.ToString(variable.Value, CultureInfo.CurrentCulture) ?? "";
            return match.Value;
        });
}
