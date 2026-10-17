using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Request-only text projection. The caller captures the target's existing memory on the game thread.
internal static class PersonaRegenerationExperienceContext
{
    internal const int WindowDays = 10, SummaryChars = 800;

    internal static bool IsRecent(int day, int currentDay)
        => currentDay >= 0 && day >= 0 && day <= currentDay && day >= currentDay - WindowDays + 1;

    internal static string Build(int currentDay, string memoryDate, string summary, int memoryDay)
    {
        if (!IsRecent(memoryDay, currentDay) || string.IsNullOrWhiteSpace(summary)) return "";
        return new JObject
        {
            ["latest_compressed_memory"] = new JObject
            {
                ["day"] = memoryDay, ["date"] = Clip(memoryDate, 100), ["text"] = Clip(summary, SummaryChars)
            }
        }.ToString(Formatting.None);
    }

    private static string Clip(string value, int limit, bool tail = false)
    {
        string text = (value ?? "").Trim();
        if (text.Length <= limit) return text;
        // Preserve complete UTF-16 characters at an excerpt boundary.
        int start = tail ? text.Length - limit + 1 : 0;
        if (tail && char.IsLowSurrogate(text[start])) start++;
        int count = tail ? text.Length - start : limit - 1;
        if (!tail && char.IsHighSurrogate(text[count - 1])) count--;
        return tail ? "…" + text.Substring(start) : text.Substring(0, count) + "…";
    }
}
