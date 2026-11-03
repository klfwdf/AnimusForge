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
    private static readonly BilingualTextTable Table = BilingualTextTable.FromModuleData("DuelFactTexts.json");
    private static readonly Regex Placeholder = new Regex(@"\{([A-Za-z_]+)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static string Get(string id) => Table.Get(id);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string Source(string id) => Table.Chinese(id);
    internal static string[] Variants(string id) => Table.Variants(id);
    internal static bool Is(string value, string id) => Table.Is(value, id);
    /// <summary>List/clause separator for composed facts in the current language.</summary>
    internal static string Comma => PromptLanguage.IsChinese ? "\uFF0C" : ", ";
    /// <summary>Space between a name and an appended word: none in Chinese, one in English.</summary>
    internal static string Gap => PromptLanguage.IsChinese ? "" : " ";
    internal static string Format(string key, params (string Name, object Value)[] variables)
        => Placeholder.Replace(Get(key), match =>
        {
            foreach (var variable in variables)
                if (variable.Name == match.Groups[1].Value)
                    return Convert.ToString(variable.Value, CultureInfo.CurrentCulture) ?? string.Empty;
            return match.Value;
        });
}
