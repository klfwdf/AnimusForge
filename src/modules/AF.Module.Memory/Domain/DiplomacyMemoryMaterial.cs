using System;
using System.Collections.Generic;
using System.Text;

namespace AnimusForge;

internal static class DiplomacyMemoryMaterial
{
    internal static bool IsRulerSnapshotCurrent(string capturedRulerId, string currentRulerId, bool alive)
    {
        // Old jobs without this optional field contain no new private material.
        if (string.IsNullOrWhiteSpace(capturedRulerId)) return true;
        return alive && !string.IsNullOrWhiteSpace(currentRulerId)
            && string.Equals(capturedRulerId, currentRulerId, StringComparison.OrdinalIgnoreCase);
    }

    internal static string BuildRecentHistory(IReadOnlyList<ConversationMessage> messages,
        int maximumMessages = 12, int maximumChars = 4800, int sectionLimit = 800)
    {
        if (messages == null || maximumMessages <= 0 || maximumChars <= 0 || sectionLimit <= 0) return "";
        List<string> sections = new List<string>(Math.Min(maximumMessages, messages.Count));
        int used = 0;
        // Reserve the latest exchange first, then render selected messages in time order.
        for (int i = messages.Count - 1; i >= Math.Max(0, messages.Count - maximumMessages); i--)
        {
            ConversationMessage message = messages[i];
            if (message == null || string.IsNullOrWhiteSpace(message.Content)) continue;
            string heading = "【近期交涉 role=" + (message.Role ?? "unknown") + "】\n";
            int available = Math.Min(sectionLimit, maximumChars - used - Environment.NewLine.Length);
            if (available <= heading.Length) break;
            string section = Truncate(heading + message.Content.Trim(), available);
            sections.Add(section);
            used += section.Length + Environment.NewLine.Length;
        }
        StringBuilder result = new StringBuilder(used);
        for (int i = sections.Count - 1; i >= 0; i--) result.AppendLine(sections[i]);
        return result.ToString();
    }

    internal static void AppendSection(StringBuilder target, string text, int sectionLimit, int totalLimit)
    {
        if (target == null || string.IsNullOrWhiteSpace(text) || sectionLimit <= 0) return;
        int remaining = totalLimit - target.Length - Environment.NewLine.Length;
        if (remaining <= 0) return;
        string value = text.Trim();
        int count = SafeLength(value, Math.Min(value.Length, Math.Min(sectionLimit, remaining)));
        if (count > 0) target.Append(value, 0, count).AppendLine();
    }

    private static string Truncate(string value, int length) =>
        value.Substring(0, SafeLength(value, Math.Min(value.Length, length)));

    private static int SafeLength(string value, int length) =>
        length > 0 && length < value.Length && char.IsHighSurrogate(value[length - 1])
            && char.IsLowSurrogate(value[length]) ? length - 1 : length;
}
