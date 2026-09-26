using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge;

internal sealed class PromptCandidateDescriptor
{
    internal int Index { get; }
    internal IReadOnlyList<string> Aliases { get; }
    internal int EqualityGroup { get; }

    internal PromptCandidateDescriptor(int index, IReadOnlyList<string> aliases, int equalityGroup = -1)
    {
        Index = index;
        Aliases = aliases ?? Array.Empty<string>();
        EqualityGroup = equalityGroup < 0 ? index : equalityGroup;
    }
}

// Descriptors contain only detached aliases and indices; game aliases are captured by the caller.
internal static class PromptCandidateSelection
{
    private const float MatchThreshold = 0.66f;

    private sealed class Match
    {
        internal int Index;
        internal int Priority;
        internal float Score;
    }

    internal static IReadOnlyList<int> SelectIndices(IReadOnlyList<PromptCandidateDescriptor> candidates, IReadOnlyList<string> terms, int limit, bool fillWithFallback)
    {
        if (candidates == null || candidates.Count == 0 || limit <= 0) return Array.Empty<int>();
        var matches = new List<Match>();
        if (terms != null && terms.Count > 0)
        {
            var priority = BuildMentionPriority(terms);
            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate == null) continue;
                var aliases = BuildDistinctAliases(candidate.Aliases);
                if (aliases.Count == 0) continue;
                float bestScore = 0f;
                int bestPriority = int.MaxValue;
                int positiveAliasMatches = 0;
                foreach (string term in terms)
                {
                    if (string.IsNullOrWhiteSpace(term)) continue;
                    float termBest = 0f;
                    foreach (string alias in aliases)
                    {
                        float score = CalculateFuzzyScore(term, alias);
                        if (score > termBest) termBest = score;
                        if (score >= MatchThreshold) positiveAliasMatches++;
                    }
                    if (termBest > bestScore)
                    {
                        bestScore = termBest;
                        bestPriority = priority.TryGetValue(term, out int value) ? value : int.MaxValue;
                    }
                }
                if (bestScore >= MatchThreshold)
                {
                    matches.Add(new Match
                    {
                        Index = i,
                        Priority = bestPriority,
                        Score = bestScore + Math.Min(0.18f, positiveAliasMatches * 0.02f)
                    });
                }
            }
        }
        var selected = matches.OrderByDescending(x => x.Score).ThenBy(x => x.Priority).ThenBy(x => candidates[x.Index].Index)
            .Take(limit).Select(x => x.Index).ToList();
        if (selected.Count == 0 && fillWithFallback)
        {
            return candidates.Take(limit).Select((_, index) => index).ToList();
        }
        if (fillWithFallback && selected.Count < limit)
        {
            var selectedGroups = new HashSet<int>(selected.Select(x => candidates[x].EqualityGroup));
            for (int i = 0; i < candidates.Count && selected.Count < limit; i++)
            {
                if (candidates[i] != null && selectedGroups.Add(candidates[i].EqualityGroup)) selected.Add(i);
            }
        }
        return selected;
    }

    private static List<string> BuildDistinctAliases(IEnumerable<string> aliases)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string alias in aliases ?? Enumerable.Empty<string>())
        {
            string text = (alias ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(text) && seen.Add(text)) result.Add(text);
        }
        return result;
    }

    private static Dictionary<string, int> BuildMentionPriority(IReadOnlyList<string> terms)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < terms.Count; i++)
        {
            string text = (terms[i] ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(text) && !result.ContainsKey(text)) result[text] = i;
        }
        return result;
    }

    private static float CalculateFuzzyScore(string mention, string alias)
    {
        string left = NormalizeFuzzyText(mention);
        string right = NormalizeFuzzyText(alias);
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return 0f;
        if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase)) return 1f;
        if (left.Contains(right) || right.Contains(left))
        {
            int min = Math.Min(left.Length, right.Length);
            int max = Math.Max(left.Length, right.Length);
            float ratio = max <= 0 ? 0f : (float)min / max;
            return Math.Max(0.78f, Math.Min(0.96f, 0.82f + ratio * 0.14f));
        }
        float tokenScore = CalculateTokenOverlapScore(left, right);
        if (tokenScore > 0f) return tokenScore;
        if (HasCjk(left) || HasCjk(right)) return CalculateCjkOverlapScore(left, right);
        int maxLen = Math.Max(left.Length, right.Length);
        if (maxLen <= 0) return 0f;
        int distance = LevenshteinDistance(left, right, 64);
        if (distance < 0) return 0f;
        return Math.Max(0f, 1f - (float)distance / maxLen);
    }

    private static string NormalizeFuzzyText(string value)
    {
        string text = (value ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(text)) return "";
        var builder = new StringBuilder(text.Length);
        foreach (char ch in text)
        {
            if (char.IsLetterOrDigit(ch) || IsCjk(ch)) builder.Append(ch);
            else if (char.IsWhiteSpace(ch) || ch == '_' || ch == '-' || ch == '/' || ch == '\\') builder.Append(' ');
        }
        return Regex.Replace(builder.ToString(), "\\s+", " ").Trim();
    }

    private static float CalculateTokenOverlapScore(string left, string right)
    {
        string[] leftTokens = left.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string[] rightTokens = right.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (leftTokens.Length == 0 || rightTokens.Length == 0) return 0f;
        var leftSet = new HashSet<string>(leftTokens, StringComparer.OrdinalIgnoreCase);
        var rightSet = new HashSet<string>(rightTokens, StringComparer.OrdinalIgnoreCase);
        int overlap = leftSet.Count(x => rightSet.Contains(x));
        if (overlap <= 0) return 0f;
        float precision = (float)overlap / Math.Max(1, rightSet.Count);
        float recall = (float)overlap / Math.Max(1, leftSet.Count);
        return Math.Max(precision, recall) >= 0.5f ? Math.Max(precision, recall) : 0f;
    }

    private static float CalculateCjkOverlapScore(string left, string right)
    {
        var leftChars = new HashSet<char>(left.Where(IsCjk));
        var rightChars = new HashSet<char>(right.Where(IsCjk));
        if (leftChars.Count == 0 || rightChars.Count == 0) return 0f;
        int overlap = leftChars.Count(x => rightChars.Contains(x));
        if (overlap <= 0) return 0f;
        float score = (float)overlap / Math.Min(leftChars.Count, rightChars.Count);
        return score >= 0.66f ? score : 0f;
    }

    private static bool HasCjk(string value) => !string.IsNullOrWhiteSpace(value) && value.Any(IsCjk);
    private static bool IsCjk(char ch) => (ch >= '\u3400' && ch <= '\u9fff') || (ch >= '\uf900' && ch <= '\ufaff');

    private static int LevenshteinDistance(string left, string right, int maxLength)
    {
        if (left == null || right == null || left.Length > maxLength || right.Length > maxLength) return -1;
        var d = new int[left.Length + 1, right.Length + 1];
        for (int i = 0; i <= left.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= right.Length; j++) d[0, j] = j;
        for (int i = 1; i <= left.Length; i++)
        {
            for (int j = 1; j <= right.Length; j++)
            {
                int cost = left[i - 1] == right[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[left.Length, right.Length];
    }
}
