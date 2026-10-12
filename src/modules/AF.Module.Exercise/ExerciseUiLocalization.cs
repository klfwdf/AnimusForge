using System;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>Player-visible Exercise, troop inspection, and settlement-entry text.</summary>
internal static class ExerciseUiLocalization
{
    // Resolve the template once, then insert display data without re-parsing its braces.
    private static readonly System.Text.RegularExpressions.Regex LiteralValues = new System.Text.RegularExpressions.Regex(@"AFSWEEPLITERAL([0-9]+)END", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    internal static string Text(string id, string englishFallback)
        => new TextObject("{=" + id + "}" + englishFallback).ToString();

    internal static TextObject Object(string id, string englishFallback)
        => new TextObject("{=" + id + "}" + englishFallback);

    internal static string Text(string id, string englishFallback, params (string Name, object Value)[] variables)
    {
        var text = Object(id, englishFallback);
        if (variables != null)
        for (int i = 0; i < variables.Length; i++)
            if (variables[i].Value is TextObject nested)
                text.SetTextVariable(variables[i].Name, nested);
            else
                text.SetTextVariable(variables[i].Name, "AFSWEEPLITERAL" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "END");
        return LiteralValues.Replace(text.ToString(), match => variables[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)].Value?.ToString() ?? string.Empty);
    }

    internal static TextObject Object(string id, string englishFallback, params (string Name, object Value)[] variables)
    {
        TextObject text = Object(id, englishFallback);
        if (variables != null)
        {
            foreach ((string name, object value) in variables)
            {
                text.SetTextVariable(name, value?.ToString() ?? string.Empty);
            }
        }
        return text;
    }
}
