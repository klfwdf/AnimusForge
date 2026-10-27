using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnimusForge;

internal sealed class ConversationContextOptions
{
    internal bool Enabled = true;
    internal int RecentTurns = 12, RecentChars = 20000, InputChars = 100000, ContextTokens = 128000;
}

// Request projections only. Sources have already passed the owning NPC's visibility filter.
internal static class ConversationContextWindow
{
    internal const int RecallChars = 6000, RecallGroups = 3;
    internal sealed class Turn
    {
        internal readonly List<ConversationMessage> Messages = new List<ConversationMessage>();
        internal int Chars;
        internal string Text => string.Join("\n", Messages.Select(x => x.Content));
    }

    internal static List<Turn> Group(IEnumerable<ConversationMessage> source)
    {
        var turns = new List<Turn>();
        foreach (var message in source ?? Enumerable.Empty<ConversationMessage>())
        {
            if (message == null || string.IsNullOrWhiteSpace(message.Content) || message.Role == "system") continue;
            if (turns.Count == 0 || message.Role == "user") turns.Add(new Turn());
            Turn turn = turns[turns.Count - 1];
            turn.Messages.Add(message); turn.Chars += message.Content.Length;
        }
        return turns;
    }

    // Content-defined window anchor: about one turn in AnchorModulus is a boundary.
    // The window only shrinks forward, never past its configured turn/char limits.
    internal const int AnchorModulus = 3, MaxAnchorShift = 4;

    internal static int RecentStart(List<Turn> turns, ConversationContextOptions options)
    {
        int start = turns.Count, chars = 0;
        while (start > 0 && turns.Count - start < options.RecentTurns)
        {
            int size = turns[start - 1].Chars;
            // The most recent turn is never silently cut, even if it alone exceeds the budget.
            if (start < turns.Count && (long)chars + size > options.RecentChars) break;
            chars += size; start--;
        }
        return AnchorStart(turns, start);
    }

    // Sliding the oldest kept turn by one on every request changes the first history
    // message, so provider prefix caches stop after system+persona. Starting at the next
    // content-defined boundary keeps the same first turn across several requests. Boundaries
    // depend on turn content, not indexes, so upstream truncation does not move them.
    // Cost: hashes at most MaxAnchorShift+1 turn heads per request.
    internal static int AnchorStart(List<Turn> turns, int start)
    {
        if (start <= 0) return start; // nothing evicted yet: the prefix is already stable
        // Small windows may give up at most a third of their turns to the anchor.
        int shift = Math.Min(MaxAnchorShift, (turns.Count - start) / 3);
        int last = Math.Min(start + shift, turns.Count - 2); // keep at least two turns
        for (int i = start; i <= last; i++)
            if (IsAnchor(turns[i])) return i;
        return start;
    }

    private static bool IsAnchor(Turn turn)
    {
        string text = turn.Messages.Count == 0 ? "" : turn.Messages[0].Content ?? "";
        uint hash = 2166136261;
        foreach (char c in text) { hash ^= c; hash *= 16777619; }
        return hash % AnchorModulus == 0;
    }

    internal static HashSet<string> Terms(string text)
    {
        text = (text ?? "").ToLowerInvariant();
        if (text.Length > 1200) text = text.Substring(text.Length - 1200);
        var terms = new HashSet<string>(StringComparer.Ordinal);
        var word = new StringBuilder();
        for (int i = 0; i <= text.Length; i++)
        {
            char c = i < text.Length ? text[i] : ' ';
            if (c >= '\u2e80' && c <= '\u9fff')
            {
                if (word.Length > 1) terms.Add(word.ToString()); word.Clear();
                if (i + 1 < text.Length && text[i + 1] >= '\u2e80' && text[i + 1] <= '\u9fff')
                    terms.Add(text.Substring(i, 2));
            }
            else if (char.IsLetterOrDigit(c)) word.Append(c);
            else { if (word.Length > 1) terms.Add(word.ToString()); word.Clear(); }
        }
        return terms;
    }

    internal static double Score(string text, HashSet<string> query)
    {
        if (query.Count == 0) return 0;
        var terms = Terms(text);
        return (double)query.Count(terms.Contains) / query.Count;
    }

    internal static List<ConversationMessage> Select(IEnumerable<ConversationMessage> source, string query, ConversationContextOptions options)
        => Select(source, query, options, out _);

    // Same membership as Select. Earlier relevant turns and day recall are also returned in
    // `recall` (chronological source order) so callers can move them after the stable history
    // prefix; the main result keeps system facts plus the recent window only when recall is split.
    internal static List<ConversationMessage> SelectWindow(IEnumerable<ConversationMessage> source, string query,
        ConversationContextOptions options, out List<ConversationMessage> recall)
    {
        var selected = Select(source, query, options, out HashSet<ConversationMessage> recent);
        recall = new List<ConversationMessage>();
        if (recent == null) return selected;
        var window = new List<ConversationMessage>(selected.Count);
        foreach (var message in selected)
        {
            if (message.Role == "system" || recent.Contains(message)) window.Add(message);
            else recall.Add(message);
        }
        return window;
    }

    private static List<ConversationMessage> Select(IEnumerable<ConversationMessage> source, string query,
        ConversationContextOptions options, out HashSet<ConversationMessage> recent)
    {
        recent = null;
        var messages = (source ?? Enumerable.Empty<ConversationMessage>()).Where(x => x != null).ToList();
        if (options == null || !options.Enabled) return messages;
        var turns = Group(messages.Where(x => !x.PromptIsRecall));
        int start = RecentStart(turns, options);
        recent = new HashSet<ConversationMessage>(turns.Skip(start).SelectMany(x => x.Messages));
        var selected = new HashSet<ConversationMessage>(recent);
        var terms = Terms(query);
        int recallChars = 0, recallCount = 0;
        // Prioritize the already-ranked recall from the complete saved day over a
        // second retrieval pass limited to the recent persistent/scene projection.
        foreach (var turn in Group(messages.Where(x => x.PromptIsRecall)))
        {
            if (recallCount >= RecallGroups || turn.Chars > RecallChars - recallChars) continue;
            foreach (var message in turn.Messages) selected.Add(message);
            recallChars += turn.Chars; recallCount++;
        }
        foreach (var candidate in turns.Take(start).Select((turn, index) => new { Turn = turn, Index = index, Score = Score(turn.Text, terms) })
            .Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenByDescending(x => x.Index))
        {
            if (recallCount >= RecallGroups) break;
            if (candidate.Turn.Chars > RecallChars - recallChars) continue;
            foreach (var message in candidate.Turn.Messages) selected.Add(message);
            recallChars += candidate.Turn.Chars; recallCount++;
        }
        return messages.Where(x => x.Role == "system" || selected.Contains(x)).ToList();
    }
}
