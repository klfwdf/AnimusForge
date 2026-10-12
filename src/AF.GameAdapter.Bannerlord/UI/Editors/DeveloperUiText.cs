using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// Player-facing text and literal display formatting for the UI and developer editors.
/// File names, folder names, identifiers, and exception text remain data/protocol
/// values; only the surrounding UI copy is resolved through Bannerlord language data.
/// </summary>
internal static class DeveloperUiText
{
    // Resolve the template once, then insert display data without re-parsing its braces.
    private static readonly System.Text.RegularExpressions.Regex LiteralValues = new System.Text.RegularExpressions.Regex(@"AFSWEEPLITERAL([0-9]+)END", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    internal static string T(string id, string englishFallback)
        => new TextObject("{=" + id + "}" + englishFallback).ToString();

    internal static string Format(string id, string englishFallback, params (string Name, object Value)[] variables)
        => FormatTemplate("{=" + id + "}" + englishFallback, variables);

    internal static string FormatTemplate(string template, params (string Name, object Value)[] variables)
    {
        var text = new TextObject(template);
        if (variables != null)
            for (int i = 0; i < variables.Length; i++)
                text.SetTextVariable(variables[i].Name, "AFSWEEPLITERAL" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "END");
        return LiteralValues.Replace(text.ToString(), match => variables[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)].Value?.ToString() ?? string.Empty);
    }

    internal static string F(string id, string englishFallback, params string[] variables)
    {
        var text = new TextObject("{=" + id + "}" + englishFallback);
        if (variables != null)
            for (int i = 0; i + 1 < variables.Length; i += 2)
                text.SetTextVariable(variables[i], "AFSWEEPLITERAL" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "END");
        return LiteralValues.Replace(text.ToString(), match => variables[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) + 1] ?? string.Empty);
    }
}
