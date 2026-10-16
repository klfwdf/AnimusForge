using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Request-only text projection. The caller captures the target's existing memory on the game thread.
internal static class PersonaRegenerationExperienceContext
{
    internal const int WindowDays = 10, ActionLimit = 8, HistoryReadLimit = 24, HistoryLimit = 8, DialogueLimit = 4;
    internal const int ActionChars = 2000, SummaryChars = 800, HistoryChars = 2400;

    internal static bool IsRecent(int day, int currentDay)
        => currentDay >= 0 && day >= 0 && day <= currentDay && day >= currentDay - WindowDays + 1;

    internal static bool IsSignificantAction(string kind, bool isMajor)
    {
        switch ((kind ?? "").Trim().ToLowerInvariant())
        {
            case "daily_behavior": case "army_create": case "army_join": case "army_leave":
            case "army_gather": case "army_disperse": case "siege_join": case "siege_leave":
            case "siege_start_attack": case "siege_start_defend": case "siege_end_attack": case "siege_end_defend":
                return false;
            case "map_event": case "map_event_aftermath": case "siege_complete":
            case "prisoner_taken_prisoner": case "prisoner_released_prisoner":
            case "prisoner_taken_captor": case "prisoner_released_captor":
            case "birth": case "marriage": case "hero_killed": case "hero_executed_victim": case "clan_member_killed":
            case "settlement_owner_changed_gain": case "settlement_owner_changed_loss": case "settlement_owner_changed_capture":
                return true;
            default: return isMajor;
        }
    }

    internal static string Build(string memoryId, int currentDay, string actions, string memoryDate, string summary, int memoryDay,
        IReadOnlyList<AnimusForgeDialogueHistoryEntry> history)
    {
        var context = new JObject();
        if (!string.IsNullOrWhiteSpace(actions)) context["recent_actions"] = Clip(actions, ActionChars, tail: true);
        if (IsRecent(memoryDay, currentDay) && !string.IsNullOrWhiteSpace(summary))
            context["latest_compressed_memory"] = new JObject
            {
                ["day"] = memoryDay, ["date"] = Clip(memoryDate, 100), ["text"] = Clip(summary, SummaryChars)
            };
        var selected = new List<(int Index, JObject Item)>();
        int remaining = HistoryChars;
        int dialogueCount = 0;
        // Confirmed actions get first use of the budget; dialogue remains a small, dated reference.
        for (int pass = 0; pass < 2; pass++)
        for (int i = (history?.Count ?? 0) - 1; i >= 0 && selected.Count < HistoryLimit; i--)
        {
            var entry = history[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.Text)
                || !string.Equals(entry.MemoryId, memoryId, StringComparison.OrdinalIgnoreCase)
                || !IsRecent(entry.GameDayIndex, currentDay)) continue;
            bool fact = entry.Kind == "afef_player" || entry.Kind == "afef_npc";
            bool dialogue = entry.Kind == "player" || entry.Kind == "npc";
            if (pass == 0 ? !fact : !dialogue || dialogueCount >= DialogueLimit) continue;
            var item = new JObject
            {
                ["day"] = entry.GameDayIndex, ["date"] = Clip(entry.GameDate, 100),
                ["speaker"] = Clip(entry.Speaker, 120), ["kind"] = Clip(entry.Kind, 40),
                ["text"] = Clip(entry.Text, 400)
            };
            int size = item.ToString(Formatting.None).Length;
            if (size > remaining) continue;
            remaining -= size;
            selected.Add((i, item));
            if (dialogue) dialogueCount++;
        }
        selected.Sort((left, right) => left.Index.CompareTo(right.Index));
        if (selected.Count > 0)
        {
            var turns = new JArray();
            foreach (var entry in selected) turns.Add(entry.Item);
            context["recent_dialogue"] = turns;
        }
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
