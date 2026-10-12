using System;
using System.Globalization;
using System.Text.RegularExpressions;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>Stable display text IDs for the PolicySystem player UI.</summary>
internal static class PolicyUiTexts
{
    private static readonly Regex LiteralValues = new Regex(@"AFPOLICYUILITERAL([0-9]+)END", RegexOptions.CultureInvariant);
    internal static string Get(string id, string fallback)
        => new TextObject("{=" + id + "}" + fallback).ToString();

    internal static string Format(string id, string fallback, params string[] variables)
    {
        if (variables == null || variables.Length % 2 != 0)
            throw new ArgumentException("Localization variables must be name/value pairs.", nameof(variables));
        var text = new TextObject("{=" + id + "}" + fallback);
        for (int i = 0; i < variables.Length; i += 2)
            text.SetTextVariable(variables[i], "AFPOLICYUILITERAL" + i.ToString(CultureInfo.InvariantCulture) + "END");
        return LiteralValues.Replace(text.ToString(), match =>
            variables[int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) + 1] ?? string.Empty);
    }
}
