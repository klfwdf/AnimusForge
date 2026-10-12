using System;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>Localized text used by the MCM settings actions and connection-test notices.</summary>
internal static class DuelSettingsUiLocalization
{
    // Resolve the template once, then insert display data without re-parsing its braces.
    private static readonly System.Text.RegularExpressions.Regex LiteralValues = new System.Text.RegularExpressions.Regex(@"AFSWEEPLITERAL([0-9]+)END", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

	internal static string Text(string id, string englishFallback)
	{
		return new TextObject("{=" + id + "}" + englishFallback).ToString();
	}

	internal static string Format(string id, string englishFallback, params (string Name, object Value)[] variables)
    {
        var text = new TextObject("{=" + id + "}" + englishFallback);
        if (variables != null)
        for (int i = 0; i < variables.Length; i++)
            if (variables[i].Value is TextObject nested)
                text.SetTextVariable(variables[i].Name, nested);
            else
                text.SetTextVariable(variables[i].Name, "AFSWEEPLITERAL" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "END");
        return LiteralValues.Replace(text.ToString(), match => variables[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)].Value?.ToString() ?? string.Empty);
    }
}
