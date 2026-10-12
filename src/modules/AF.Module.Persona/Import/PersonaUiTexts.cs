using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.Localization;

namespace AnimusForge;

internal static class PersonaUiTexts
{
    internal static string Text(string id, string englishFallback)
        => new TextObject("{=" + id + "}" + englishFallback).ToString();

    internal static string Format(string id, string englishFallback, params (string Name, string Value)[] variables)
    {
        TextObject text = new TextObject("{=" + id + "}" + englishFallback);
        List<(string Marker, string Value)> replacements = new List<(string Marker, string Value)>();
        int index = 0;
        foreach (var variable in variables ?? Array.Empty<(string Name, string Value)>())
        {
            string marker = "AF_PERSONA_TEXT_VAR_" + index++ + "_END";
            text.SetTextVariable(variable.Name, marker);
            replacements.Add((marker, variable.Value ?? ""));
        }
        string resolved = text.ToString();
        StringBuilder result = new StringBuilder(resolved.Length);
        for (int offset = 0; offset < resolved.Length;)
        {
            bool replaced = false;
            foreach (var replacement in replacements)
            {
                if (offset + replacement.Marker.Length <= resolved.Length
                    && string.Compare(resolved, offset, replacement.Marker, 0, replacement.Marker.Length, StringComparison.Ordinal) == 0)
                {
                    result.Append(replacement.Value);
                    offset += replacement.Marker.Length;
                    replaced = true;
                    break;
                }
            }
            if (!replaced)
                result.Append(resolved[offset++]);
        }
        return result.ToString();
    }
}
