using System;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>Stable display text IDs for the PolicySystem player UI.</summary>
internal static class PolicyUiTexts
{
    internal static string Get(string id, string fallback)
        => new TextObject("{=" + id + "}" + fallback).ToString();

    internal static string Format(string id, string fallback, params string[] variables)
    {
        if (variables == null || variables.Length % 2 != 0)
            throw new ArgumentException("Localization variables must be name/value pairs.", nameof(variables));
        var text = new TextObject("{=" + id + "}" + fallback);
        for (int i = 0; i < variables.Length; i += 2)
            text.SetTextVariable(variables[i], variables[i + 1] ?? string.Empty);
        return text.ToString();
    }
}
