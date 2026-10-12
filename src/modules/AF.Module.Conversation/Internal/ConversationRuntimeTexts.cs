using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class ConversationRuntimeTexts
{
    private static readonly Lazy<Dictionary<string, string>> RawValues = new Lazy<Dictionary<string, string>>(() =>
    {
        using (var stream = typeof(ConversationRuntimeTexts).Assembly.GetManifestResourceStream("AnimusForge.Conversation.RuntimeCompatibility.json"))
        using (var reader = new StreamReader(stream, Encoding.UTF8))
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
    });
    private static readonly Lazy<Dictionary<string, string>> PromptValues = new Lazy<Dictionary<string, string>>(() =>
        JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(
            AnimusForgeModulePaths.GetModuleDataFilePath("ConversationRuntimePrompts.json"), Encoding.UTF8)));

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

    internal static string Raw(string key) => RawValues.Value[key];
    internal static string Prompt(string key) => PromptValues.Value[key];
}
