using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Request-only text projection. The caller captures the target's existing memory on the game thread.
internal static class PersonaRegenerationExperienceContext
{
    internal const int ActionLimit = 16, HistoryLimit = 24;
    internal const int ActionChars = 4000, SummaryChars = 2000, HistoryChars = 6000;

    internal static string Build(string memoryId, string actions, string memoryDate, string summary,
        IReadOnlyList<AnimusForgeDialogueHistoryEntry> history)
    {
        var context = new JObject();
        if (!string.IsNullOrWhiteSpace(actions)) context["recent_actions"] = Clip(actions, ActionChars, tail: true);
        if (!string.IsNullOrWhiteSpace(summary))
            context["latest_compressed_memory"] = new JObject
            {
                ["date"] = Clip(memoryDate, 100), ["text"] = Clip(summary, SummaryChars)
            };
        var selected = new List<JObject>();
        int remaining = HistoryChars;
        for (int i = (history?.Count ?? 0) - 1; i >= 0 && selected.Count < HistoryLimit; i--)
        {
            var entry = history[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.Text)
                || !string.Equals(entry.MemoryId, memoryId, StringComparison.OrdinalIgnoreCase)) continue;
            var item = new JObject
            {
                ["day"] = entry.GameDayIndex, ["date"] = Clip(entry.GameDate, 100),
                ["speaker"] = Clip(entry.Speaker, 120), ["kind"] = Clip(entry.Kind, 40),
                ["text"] = Clip(entry.Text, 800)
            };
            int size = item.ToString(Formatting.None).Length;
            if (size > remaining) break;
            remaining -= size;
            selected.Add(item);
        }
        selected.Reverse();
        if (selected.Count > 0) context["recent_dialogue"] = new JArray(selected);
        return context.Count == 0 ? "" : context.ToString(Formatting.None);
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
