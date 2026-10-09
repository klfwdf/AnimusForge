using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// Player-facing text for the developer data editors and package import flows.
/// File names, folder names, identifiers, and exception text remain data/protocol
/// values; only the surrounding UI copy is resolved through Bannerlord language data.
/// </summary>
internal static class DeveloperUiText
{
    internal static string T(string id, string englishFallback)
        => new TextObject("{=" + id + "}" + englishFallback).ToString();

    internal static string F(string id, string englishFallback, params string[] variables)
    {
        var text = new TextObject("{=" + id + "}" + englishFallback);
        if (variables != null)
        {
            for (var i = 0; i + 1 < variables.Length; i += 2)
                text.SetTextVariable(variables[i], variables[i + 1] ?? string.Empty);
        }
        return text.ToString();
    }
}
