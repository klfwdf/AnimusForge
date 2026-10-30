using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class ConversationRuntimeTexts
{
    private static readonly BilingualTextTable RawValues =
        BilingualTextTable.FromEmbedded(typeof(ConversationRuntimeTexts).Assembly, "AnimusForge.Conversation.RuntimeCompatibility.json");
    private static readonly PromptLanguageFile<Dictionary<string, string>> PromptValues = new PromptLanguageFile<Dictionary<string, string>>("ConversationRuntimePrompts.json", path =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            path, Encoding.UTF8)));

    private static readonly System.Text.RegularExpressions.Regex Placeholder =
        new System.Text.RegularExpressions.Regex(@"\{([A-Z_]+)\}", System.Text.RegularExpressions.RegexOptions.Compiled);
    internal static string FormatPrompt(string key, params (string Name, object Value)[] variables)
        => Placeholder.Replace(Prompt(key), match =>
        {
            foreach (var variable in variables)
                if (variable.Name == match.Groups[1].Value)
                    return Convert.ToString(variable.Value, System.Globalization.CultureInfo.CurrentCulture) ?? "";
            return match.Value;
        });

    internal static string Raw(string key) => RawValues.Get(key);
    /// <summary>Chinese source value, for matching text written before the language switch.</summary>
    internal static string RawSource(string key) => RawValues.Chinese(key);
    internal static string[] RawVariants(string key) => RawValues.Variants(key);
    internal static bool IsRaw(string value, string key) => RawValues.Is(value, key);
    internal static string Prompt(string key) => PromptValues.Value[key];
}
