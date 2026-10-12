using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace AnimusForge;

// Existing history and AFEF identities must not change when the player switches UI language.
internal static class DuelFactTexts
{
    private static readonly Lazy<Dictionary<string, string>> Values = new Lazy<Dictionary<string, string>>(
        () => JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("DuelFactTexts.json"), Encoding.UTF8)));
    private static readonly Regex Placeholder = new Regex(@"\{([A-Za-z_]+)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static string Get(string key) => Values.Value[key];
    internal static string Format(string key, params (string Name, object Value)[] variables)
        => Placeholder.Replace(Get(key), match =>
        {
            foreach (var variable in variables)
                if (variable.Name == match.Groups[1].Value)
                    return Convert.ToString(variable.Value, CultureInfo.CurrentCulture) ?? string.Empty;
            return match.Value;
        });
}
