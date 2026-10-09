using System;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// Player-facing copy used by the memory editor. Stored memory text, protocol
/// markers, and selection identifiers stay outside this catalog.
/// </summary>
internal static class MemoryEditorUiText
{
    internal static string T(string id, string englishFallback)
        => new TextObject("{=" + id + "}" + englishFallback).ToString();

    internal static string F(string id, string englishFallback, params string[] variables)
    {
        var text = new TextObject("{=" + id + "}" + englishFallback);
        if (variables != null)
        {
            for (int i = 0; i + 1 < variables.Length; i += 2)
            {
                text.SetTextVariable(variables[i], variables[i + 1] ?? string.Empty);
            }
        }
        return text.ToString();
    }

    internal static string WithHero(string id, string englishFallback, string hero)
        => F(id, englishFallback, "HERO", hero ?? "NPC");
}
