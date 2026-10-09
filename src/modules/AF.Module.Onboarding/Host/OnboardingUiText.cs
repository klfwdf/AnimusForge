using System;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>Player-facing onboarding and API setup copy. Protocol payloads remain in their original source.</summary>
internal static class OnboardingUiText
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
