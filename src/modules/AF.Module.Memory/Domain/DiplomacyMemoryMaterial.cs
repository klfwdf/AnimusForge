using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge;

internal static class DiplomacyMemoryMaterial
{
    internal static bool IsRulerSnapshotCurrent(string capturedRulerId, string currentRulerId, bool alive)
    {
        if (string.IsNullOrWhiteSpace(capturedRulerId)) return true;
        return alive && !string.IsNullOrWhiteSpace(currentRulerId)
            && string.Equals(capturedRulerId, currentRulerId, StringComparison.OrdinalIgnoreCase);
    }
    private static readonly Regex Words = new(@"[A-Za-z0-9_]+|[\p{IsCJKUnifiedIdeographs}]+", RegexOptions.Compiled);
    private static HashSet<string> Terms(string text)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Words.Matches(text ?? ""))
        {
            string word = match.Value;
            if (word[0] >= 0x4e00)
                for (int i = 0; i + 1 < word.Length; i++) terms.Add(word.Substring(i, 2));
            else if (word.Length >= 3) terms.Add(word);
        }
        return terms;
    }
    internal static bool Relevant(string query, string text) => Terms(query).Overlaps(Terms(text));
    internal static bool Relevant(string query, string title, string summary, Func<string, float[]> embed)
    {
        if (Relevant(query, (title ?? "") + " " + (summary ?? ""))) return true;
        if (embed == null || string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(title)) return false;
        try
        {
            var a = embed(query); var b = embed(title);
            if (a == null || b == null || a.Length != b.Length) return false;
            double dot = 0, aa = 0, bb = 0;
            for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; aa += a[i] * a[i]; bb += b[i] * b[i]; }
            return aa > 0 && bb > 0 && dot / Math.Sqrt(aa * bb) >= 0.65;
        }
        catch { return false; }
    }
    internal static string BuildRelevantHistory(IReadOnlyList<ConversationMessage> messages, string query,
        int maximumMessages = 12, int maximumChars = 4800)
    {
        if (messages == null || maximumMessages <= 0 || maximumChars <= 0 || string.IsNullOrWhiteSpace(query)) return "";
        var terms = Terms(query); var groups = new List<(int order, int count, int score, string text)>();
        for (int i = 0; i < messages.Count; i++)
        {
            var message = messages[i]; if (message == null || string.IsNullOrWhiteSpace(message.Content)) continue;
            var group = new List<ConversationMessage> { message };
            if (message.Role == "user" && i + 1 < messages.Count && messages[i + 1]?.Role == "assistant"
                && (string.IsNullOrEmpty(message.PromptMemorySessionKey) || string.IsNullOrEmpty(messages[i + 1].PromptMemorySessionKey)
                    || message.PromptMemorySessionKey == messages[i + 1].PromptMemorySessionKey)
                && (message.GameDayIndex < 0 || messages[i + 1].GameDayIndex < 0 || message.GameDayIndex == messages[i + 1].GameDayIndex)) group.Add(messages[++i]);
            int score = group.Sum(x => Terms(x.Content).Count(terms.Contains)); if (score == 0) continue;
            string text = string.Join("\n", group.Select(x => MemoryTextResources.Prompt("MEM_Prompt_054")
                + (x.Role ?? "unknown") + ";event=" + x.EventSequence + ";day=" + x.GameDayIndex + ";hour=" + x.GameHour + ";speaker=" + x.SpeakerHeroId + MemoryTextResources.Prompt("AF_MEM_Prompt_SectionEnd") + x.Content.Trim()));
            // One complete turn is one paragraph in the shared budget, including
            // embedded paragraph breaks. Escaping does not change the saved source.
            text = text.Replace("\r", "").Replace("\n", "\\n");
            groups.Add((i, group.Count, score, text));
        }
        int used = 0, count = 0; var selected = new List<(int order, string text)>();
        foreach (var group in groups.OrderByDescending(x => x.score).ThenBy(x => x.order))
        {
            if (count + group.count > maximumMessages || used + group.text.Length + 2 > maximumChars) continue;
            used += group.text.Length + 2; count += group.count; selected.Add((group.order, group.text));
        }
        return string.Join("\n\n", selected.OrderBy(x => x.order).Select(x => x.text));
    }
    internal static void AppendSection(StringBuilder target, string text, int sectionLimit, int totalLimit)
    {
        if (target == null || string.IsNullOrWhiteSpace(text) || sectionLimit <= 0) return;
        string value = text.Trim();
        if (value.Length > sectionLimit || target.Length + value.Length + 2 * Environment.NewLine.Length > totalLimit) return;
        target.Append(value).AppendLine().AppendLine();
    }
}
