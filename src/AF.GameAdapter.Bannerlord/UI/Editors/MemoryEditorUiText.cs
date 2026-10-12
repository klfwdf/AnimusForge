using System;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// Player-facing copy used by the memory editor. Stored memory text, protocol
/// markers, and selection identifiers stay outside this catalog.
/// </summary>
internal static class MemoryEditorUiText
{
    // Resolve the template once, then insert display data without re-parsing its braces.
    private static readonly System.Text.RegularExpressions.Regex LiteralValues = new System.Text.RegularExpressions.Regex(@"AFSWEEPLITERAL([0-9]+)END", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    internal static string T(string id, string englishFallback)
        => new TextObject("{=" + id + "}" + englishFallback).ToString();

    internal static string F(string id, string englishFallback, params string[] variables)
    {
        var text = new TextObject("{=" + id + "}" + englishFallback);
        if (variables != null)
            for (int i = 0; i + 1 < variables.Length; i += 2)
                text.SetTextVariable(variables[i], "AFSWEEPLITERAL" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "END");
        return LiteralValues.Replace(text.ToString(), match => variables[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) + 1] ?? string.Empty);
    }

    internal static string WithHero(string id, string englishFallback, string hero)
        => F(id, englishFallback, "HERO", hero ?? "NPC");
}
