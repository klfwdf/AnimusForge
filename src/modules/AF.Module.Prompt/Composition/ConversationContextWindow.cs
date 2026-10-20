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
        return start;
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
    {
        var messages = (source ?? Enumerable.Empty<ConversationMessage>()).Where(x => x != null).ToList();
        if (options == null || !options.Enabled) return messages;
        var turns = Group(messages.Where(x => !x.PromptIsRecall));
        int start = RecentStart(turns, options);
        var selected = new HashSet<ConversationMessage>(turns.Skip(start).SelectMany(x => x.Messages));
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
