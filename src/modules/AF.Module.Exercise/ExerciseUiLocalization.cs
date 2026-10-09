using System;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>Player-visible Exercise, troop inspection, and settlement-entry text.</summary>
internal static class ExerciseUiLocalization
{
    internal static string Text(string id, string englishFallback)
        => new TextObject("{=" + id + "}" + englishFallback).ToString();

    internal static TextObject Object(string id, string englishFallback)
        => new TextObject("{=" + id + "}" + englishFallback);

    internal static string Text(string id, string englishFallback, params (string Name, object Value)[] variables)
    {
        TextObject text = Object(id, englishFallback);
        if (variables != null)
        {
            foreach ((string name, object value) in variables)
            {
                text.SetTextVariable(name, value?.ToString() ?? string.Empty);
            }
        }
        return text.ToString();
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
