using System;

namespace AnimusForge;

internal static class PlayerExperiencePrompt
{
    internal const string Begin = "[AF_PLAYER_EXPERIENCE]";
    internal const string End = "[/AF_PLAYER_EXPERIENCE]";

    internal static string Evidence(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        int start = text.IndexOf(Begin, StringComparison.Ordinal);
        if (start < 0) return "";
        start += Begin.Length;
        int end = text.IndexOf(End, start, StringComparison.Ordinal);
        return end < 0 ? "" : text.Substring(start, end - start).Trim();
    }

    internal static string RemoveRepeatedEvidence(string context, string opening)
    {
        string evidence = Evidence(opening);
        // The opening keeps the sole readable copy; all unrelated history remains unchanged.
        return string.IsNullOrEmpty(context) || string.IsNullOrEmpty(evidence) ? context : context.Replace(evidence, "");
    }
}
