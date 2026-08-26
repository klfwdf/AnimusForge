using System;
using System.Collections.Generic;

namespace AnimusForge.SceneActions.Core
{
    /// <summary>
    /// Compares generated battle speeches without depending on a model or the
    /// Bannerlord runtime. This runs only when a speech is generated, never on
    /// the Mission tick hot path.
    /// </summary>
    public static class BattleSpeechDiversityV1
    {
        public const double DefaultSimilarityThreshold = 0.78d;
        public const int MinimumComparableCharacters = 12;

        public static string NormalizeForComparison(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var buffer = new System.Text.StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char value = char.ToLowerInvariant(text[i]);
                if (char.IsLetterOrDigit(value) ||
                    (value >= '\u3400' && value <= '\u9fff') ||
                    (value >= '\uf900' && value <= '\ufaff'))
                {
                    buffer.Append(value);
                }
            }
            return buffer.ToString();
        }

        public static double Similarity(string left, string right)
        {
            string a = NormalizeForComparison(left);
            string b = NormalizeForComparison(right);
            if (a.Length == 0 || b.Length == 0)
            {
                return 0d;
            }
            if (string.Equals(a, b, StringComparison.Ordinal))
            {
                return 1d;
            }
            if (a.Length < MinimumComparableCharacters ||
                b.Length < MinimumComparableCharacters)
            {
                return 0d;
            }

            int commonSubstring = LongestCommonSubstring(a, b);
            int lcs = LongestCommonSubsequence(a, b);
            double sequenceScore = (double)lcs / Math.Min(a.Length, b.Length);
            double substringScore = (double)commonSubstring / Math.Min(a.Length, b.Length);
            double bigramScore = DiceBigrams(a, b);
            return Math.Max(sequenceScore, Math.Max(substringScore, bigramScore));
        }

        public static bool IsTooSimilar(
            string candidate,
            IEnumerable<string> previous,
            out double highestSimilarity,
            double threshold = DefaultSimilarityThreshold)
        {
            highestSimilarity = 0d;
            if (string.IsNullOrWhiteSpace(candidate) || previous == null)
            {
                return false;
            }
            foreach (string value in previous)
            {
                double score = Similarity(candidate, value);
                if (score > highestSimilarity)
                {
                    highestSimilarity = score;
                }
                if (score >= threshold)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsExactRepeat(
            string candidate,
            IEnumerable<string> previous,
            out double similarity)
        {
            similarity = 0d;
            string normalizedCandidate = NormalizeForComparison(candidate);
            if (normalizedCandidate.Length == 0 || previous == null)
            {
                return false;
            }
            foreach (string value in previous)
            {
                string normalizedPrevious = NormalizeForComparison(value);
                if (normalizedPrevious.Length == 0)
                {
                    continue;
                }
                if (string.Equals(
                        normalizedCandidate,
                        normalizedPrevious,
                        StringComparison.Ordinal))
                {
                    similarity = 1d;
                    return true;
                }
            }
            return false;
        }

        public static string BuildAvoidanceInstruction(
            IEnumerable<string> previous,
            int maximumEntries = 3,
            int maximumCharactersPerEntry = 90)
        {
            if (previous == null || maximumEntries <= 0)
            {
                return string.Empty;
            }
            var snippets = new List<string>();
            foreach (string value in previous)
            {
                string normalized = NormalizeForComparison(value);
                if (normalized.Length == 0)
                {
                    continue;
                }
                if (normalized.Length > maximumCharactersPerEntry)
                {
                    normalized = normalized.Substring(0, maximumCharactersPerEntry);
                }
                snippets.Add(normalized);
                if (snippets.Count >= maximumEntries)
                {
                    break;
                }
            }
            if (snippets.Count == 0)
            {
                return string.Empty;
            }
            return "同一演讲者最近已经使用过以下正文片段（只作避让参考，不得复用其开头、句式、" +
                   "连续短语或号召，也不要只替换几个词）：" + string.Join(" / ", snippets) +
                   "。这次必须改用明显不同的切入点、节奏和号召。";
        }

        private static double DiceBigrams(string a, string b)
        {
            if (a.Length < 2 || b.Length < 2)
            {
                return 0d;
            }
            var left = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < a.Length - 1; i++)
            {
                left.Add(a.Substring(i, 2));
            }
            var right = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < b.Length - 1; i++)
            {
                right.Add(b.Substring(i, 2));
            }
            int common = 0;
            foreach (string value in left)
            {
                if (right.Contains(value))
                {
                    common++;
                }
            }
            return (2d * common) / (left.Count + right.Count);
        }

        private static int LongestCommonSubstring(string a, string b)
        {
            int[] previous = new int[b.Length + 1];
            int best = 0;
            for (int i = 1; i <= a.Length; i++)
            {
                int diagonal = 0;
                for (int j = 1; j <= b.Length; j++)
                {
                    int saved = previous[j];
                    if (a[i - 1] == b[j - 1])
                    {
                        previous[j] = diagonal + 1;
                        best = Math.Max(best, previous[j]);
                    }
                    else
                    {
                        previous[j] = 0;
                    }
                    diagonal = saved;
                }
            }
            return best;
        }

        private static int LongestCommonSubsequence(string a, string b)
        {
            int[] row = new int[b.Length + 1];
            for (int i = 1; i <= a.Length; i++)
            {
                int diagonal = 0;
                for (int j = 1; j <= b.Length; j++)
                {
                    int saved = row[j];
                    row[j] = a[i - 1] == b[j - 1]
                        ? diagonal + 1
                        : Math.Max(row[j], row[j - 1]);
                    diagonal = saved;
                }
            }
            return row[b.Length];
        }
    }
}
