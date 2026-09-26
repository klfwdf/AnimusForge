using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge;

/// <summary>Normalized/tokenized view of a name used by the fuzzy matcher.</summary>
internal sealed class FuzzyTextProfile
{
	public string Raw;
	public string Normalized;
	public List<string> Tokens;
}

/// <summary>
/// Pure fuzzy name matching for world-entity retrieval: normalization (letters/digits/CJK only, lower-cased),
/// containment / prefix / Levenshtein / short-CJK near-name / token-overlap scoring clamped to 0..1.
/// </summary>
internal static class EntityNameMatcher
{
	internal static FuzzyTextProfile BuildProfile(string value)
	{
		string raw = (value ?? "").Trim();
		return new FuzzyTextProfile { Raw = raw, Normalized = Normalize(raw), Tokens = SplitTokens(raw) };
	}

	internal static List<FuzzyTextProfile> BuildAliasProfiles(IEnumerable<string> aliases)
	{
		List<FuzzyTextProfile> result = new List<FuzzyTextProfile>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string alias in aliases ?? Enumerable.Empty<string>())
		{
			string text = (alias ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && seen.Add(text))
			{
				result.Add(BuildProfile(text));
			}
		}
		return result;
	}

	internal static string Normalize(string value)
	{
		string text = (value ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		StringBuilder sb = new StringBuilder(text.Length);
		foreach (char c in text)
		{
			if (char.IsLetterOrDigit(c) || IsCjk(c))
			{
				sb.Append(c);
			}
		}
		return sb.ToString();
	}

	internal static bool IsCjk(char c)
	{
		return (c >= 0x4e00 && c <= 0x9fff) || (c >= 0x3400 && c <= 0x4dbf) || (c >= 0xf900 && c <= 0xfaff);
	}

	internal static bool IsExactNameMatch(string mention, string name)
	{
		string normalizedMention = Normalize(mention);
		return !string.IsNullOrWhiteSpace(normalizedMention) && string.Equals(normalizedMention, Normalize(name), StringComparison.OrdinalIgnoreCase);
	}

	internal static float BestScore(string mention, IEnumerable<string> aliases)
	{
		return BestScore(BuildProfile(mention), BuildAliasProfiles(aliases));
	}

	internal static float BestScore(FuzzyTextProfile mention, IEnumerable<FuzzyTextProfile> aliases)
	{
		float best = 0f;
		foreach (FuzzyTextProfile alias in aliases ?? Enumerable.Empty<FuzzyTextProfile>())
		{
			best = Math.Max(best, Score(mention, alias));
		}
		return best;
	}

	internal static float Score(string left, string right)
	{
		return Score(BuildProfile(left), BuildProfile(right));
	}

	internal static float Score(FuzzyTextProfile left, FuzzyTextProfile right)
	{
		string a = left?.Normalized ?? "";
		string b = right?.Normalized ?? "";
		if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
		{
			return 0f;
		}
		if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
		{
			return 1f;
		}
		float best = 0f;
		int minLen = Math.Min(a.Length, b.Length);
		int maxLen = Math.Max(a.Length, b.Length);
		if (minLen >= 2 && (a.Contains(b) || b.Contains(a)))
		{
			best = Math.Max(best, 0.86f + 0.12f * ((float)minLen / Math.Max(1, maxLen)));
		}
		if (minLen >= 3 && (a.StartsWith(b, StringComparison.OrdinalIgnoreCase) || b.StartsWith(a, StringComparison.OrdinalIgnoreCase)))
		{
			best = Math.Max(best, 0.82f + 0.1f * ((float)minLen / Math.Max(1, maxLen)));
		}
		int distance = LevenshteinDistance(a, b);
		best = Math.Max(best, ShortCjkNearNameScore(a, b, distance));
		float distanceScore = 1f - ((float)distance / Math.Max(1, maxLen));
		best = Math.Max(best, distanceScore);
		best = Math.Max(best, TokenOverlapScore(left, right));
		return Math.Max(0f, Math.Min(1f, best));
	}

	internal static float ShortCjkNearNameScore(string a, string b, int distance)
	{
		try
		{
			if (distance != 1 || string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
			{
				return 0f;
			}
			int minLen = Math.Min(a.Length, b.Length);
			int maxLen = Math.Max(a.Length, b.Length);
			if (minLen < 3 || maxLen > 6 || !IsAllCjkText(a) || !IsAllCjkText(b))
			{
				return 0f;
			}
			if (a.Length == b.Length)
			{
				int same = 0;
				for (int i = 0; i < a.Length; i++)
				{
					if (a[i] == b[i])
					{
						same++;
					}
				}
				if (same >= minLen - 1)
				{
					return maxLen <= 3 ? 0.82f : 0.86f;
				}
			}
			if (maxLen == minLen + 1 && IsOrderedSubsequence(a.Length <= b.Length ? a : b, a.Length <= b.Length ? b : a))
			{
				return 0.80f;
			}
		}
		catch
		{
		}
		return 0f;
	}

	internal static bool IsAllCjkText(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		foreach (char c in value)
		{
			if (!IsCjk(c))
			{
				return false;
			}
		}
		return true;
	}

	internal static bool IsOrderedSubsequence(string shortText, string longText)
	{
		if (string.IsNullOrWhiteSpace(shortText) || string.IsNullOrWhiteSpace(longText))
		{
			return false;
		}
		int j = 0;
		for (int i = 0; i < longText.Length && j < shortText.Length; i++)
		{
			if (shortText[j] == longText[i])
			{
				j++;
			}
		}
		return j == shortText.Length;
	}

	internal static float TokenOverlapScore(string left, string right)
	{
		return TokenOverlapScore(BuildProfile(left), BuildProfile(right));
	}

	internal static float TokenOverlapScore(FuzzyTextProfile left, FuzzyTextProfile right)
	{
		List<string> a = left?.Tokens ?? new List<string>();
		List<string> b = right?.Tokens ?? new List<string>();
		if (a.Count == 0 || b.Count == 0)
		{
			return 0f;
		}
		HashSet<string> setA = new HashSet<string>(a, StringComparer.OrdinalIgnoreCase);
		HashSet<string> setB = new HashSet<string>(b, StringComparer.OrdinalIgnoreCase);
		int intersection = setA.Count((string x) => setB.Contains(x));
		int union = setA.Count + setB.Count - intersection;
		return union <= 0 ? 0f : (0.65f + 0.25f * ((float)intersection / union));
	}

	internal static List<string> SplitTokens(string value)
	{
		return Regex.Matches((value ?? "").ToLowerInvariant(), "[\\p{L}\\p{Nd}]+", RegexOptions.CultureInvariant).Cast<Match>().Select((Match x) => x.Value).Where((string x) => x.Length > 1).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	internal static int LevenshteinDistance(string a, string b)
	{
		int n = a.Length;
		int m = b.Length;
		int[] previous = new int[m + 1];
		int[] current = new int[m + 1];
		for (int j = 0; j <= m; j++)
		{
			previous[j] = j;
		}
		for (int i = 1; i <= n; i++)
		{
			current[0] = i;
			for (int j = 1; j <= m; j++)
			{
				int cost = a[i - 1] == b[j - 1] ? 0 : 1;
				current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
			}
			int[] temp = previous;
			previous = current;
			current = temp;
		}
		return previous[m];
	}
}
