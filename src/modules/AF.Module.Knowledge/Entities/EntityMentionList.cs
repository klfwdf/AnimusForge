using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

/// <summary>Pure mention-list preparation for world-entity retrieval (segmenting on 的/之, de-dupe, priority map).</summary>
internal static class EntityMentionList
{
	internal static List<string> BuildUnified(IEnumerable<string> mentions)
	{
		List<string> result = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		AddMentionList(result, seen, mentions);
		return result;
	}

	internal static HashSet<string> BuildActiveRuleIdSet(IEnumerable<string> activeRuleIds)
	{
		HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string ruleId in activeRuleIds ?? Enumerable.Empty<string>())
		{
			string text = (ruleId ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text))
			{
				result.Add(text);
			}
		}
		return result;
	}

	/// <summary>Mentions containing 的/之 are split into segments; each segment (and plain mentions) is trimmed and de-duplicated in order.</summary>
	internal static void AddMentionList(List<string> result, HashSet<string> seen, IEnumerable<string> values)
	{
		if (result == null || seen == null || values == null)
		{
			return;
		}
		foreach (string value in values)
		{
			string text = (value ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (text.IndexOf('的') < 0 && text.IndexOf('之') < 0)
			{
				AddMention(result, seen, text);
				continue;
			}
			int segmentStart = 0;
			for (int i = 0; i <= text.Length; i++)
			{
				if (i < text.Length && text[i] != '的' && text[i] != '之')
				{
					continue;
				}
				AddMention(result, seen, text.Substring(segmentStart, i - segmentStart));
				segmentStart = i + 1;
			}
		}
	}

	internal static void AddMention(List<string> result, HashSet<string> seen, string value)
	{
		string text = (value ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text) && seen.Add(text))
		{
			result.Add(text);
		}
	}

	internal static Dictionary<string, int> BuildPriority(List<string> mentions)
	{
		Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		if (mentions == null)
		{
			return result;
		}
		for (int i = 0; i < mentions.Count; i++)
		{
			string text = (mentions[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text) && !result.ContainsKey(text))
			{
				result[text] = i;
			}
		}
		return result;
	}

	internal static int GetPriority(Dictionary<string, int> mentionPriority, string mention)
	{
		if (mentionPriority != null && !string.IsNullOrWhiteSpace(mention) && mentionPriority.TryGetValue(mention.Trim(), out var value))
		{
			return value;
		}
		return int.MaxValue / 2;
	}

	internal static int CountNonBlank(List<string> values)
	{
		return values?.Count((string x) => !string.IsNullOrWhiteSpace(x)) ?? 0;
	}

	internal static string FormatForLog(IEnumerable<string> values)
	{
		List<string> names = (values ?? Enumerable.Empty<string>()).Select((string x) => (x ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Take(12).ToList();
		return names.Count == 0 ? "(none)" : string.Join("|", names);
	}
}
