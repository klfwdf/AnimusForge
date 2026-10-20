using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

/// <summary>
/// Pure line-level rules for the per-NPC dialogue history: scene-session tagging, AFEF prefixing,
/// NPC-utterance prefixing, single-use fact expiry and the 260-line trim/regroup. Day containers
/// are the host's persisted DialogueDay; this owner works on (day, date, line) tuples and rebuilds
/// through the supplied factory so save identity is untouched.
/// </summary>
internal static class DialogueHistoryLedger
{
	internal const int MaxLines = 260;
	internal const string SceneSessionMarkerPrefix = "[AF_SCENE_SESSION:";
	internal static readonly string AfefPlayerPrefix = MemoryTextResources.Raw("MEM_Raw_018");
	internal static readonly string AfefNpcPrefix = MemoryTextResources.Raw("MEM_Raw_006");
	internal static readonly string SceneShoutPrefix = MemoryTextResources.Raw("MEM_Raw_036");

    // A fixed six-entry protocol table, initialized once. Natural-language bodies are opaque.
    private static readonly (string Prefix, bool Npc)[] FactPrefixes = {
        (AfefPlayerPrefix, false), (AfefNpcPrefix, true),
        ("[AFEF PLAYER_ACTION]", false), ("[AFEF NPC_ACTION]", true),
        (MemoryTextResources.Raw("AF_MEM_Compat_PlayerFactBracket"), false),
        (MemoryTextResources.Raw("AF_MEM_Compat_NpcFactBracket"), true)
    };
    internal static bool TryReadAfefPrefix(string text, out bool npc, out int prefixLength)
    {
        if (!string.IsNullOrEmpty(text))
            foreach (var marker in FactPrefixes)
                if (text.StartsWith(marker.Prefix, StringComparison.Ordinal))
                { npc = marker.Npc; prefixLength = marker.Prefix.Length; return true; }
        npc = false; prefixLength = 0; return false;
    }


	internal static string TagSceneSession(string line, int sceneSessionId)
	{
		string text = (line ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || sceneSessionId < 0)
		{
			return text;
		}
		return $"{SceneSessionMarkerPrefix}{sceneSessionId}] {text}";
	}

	internal static bool TryStripSceneSessionMarker(string line, out string stripped, out int sceneSessionId)
	{
		stripped = (line ?? "").Trim();
		sceneSessionId = -1;
		if (string.IsNullOrWhiteSpace(stripped) || !stripped.StartsWith(SceneSessionMarkerPrefix, StringComparison.Ordinal))
		{
			return false;
		}
		int end = stripped.IndexOf(']');
		if (end <= SceneSessionMarkerPrefix.Length)
		{
			return false;
		}
		string digits = stripped.Substring(SceneSessionMarkerPrefix.Length, end - SceneSessionMarkerPrefix.Length).Trim();
		if (!int.TryParse(digits, out sceneSessionId))
		{
			sceneSessionId = -1;
			return false;
		}
		stripped = stripped.Substring(end + 1).TrimStart();
		return true;
	}

	/// <summary>An extra fact is stored with the player AFEF prefix unless it already carries an AFEF prefix.</summary>
	internal static string NormalizeAfefFact(string extraFact)
	{
		string text = (extraFact ?? "").Trim();
        // Keep the original on-disk identity while accepting all known transport aliases.
        if (TryReadAfefPrefix(text, out bool npc, out int length))
            return (npc ? AfefNpcPrefix : AfefPlayerPrefix) + text.Substring(length);
		return AfefPlayerPrefix + " " + text;
	}

	/// <summary>NPC text is stored as "Name: text" unless it is a scene shout line.</summary>
	internal static string NormalizeNpcLine(string npcName, string aiText)
	{
		string text = (aiText ?? "").Trim();
		return text.StartsWith(SceneShoutPrefix, StringComparison.Ordinal) ? text : (npcName + ": " + text);
	}

	internal static bool IsSingleUseNpcFactLine(string line, Func<string, bool> isFirstMeetingBody)
	{
		string text = (line ?? "").Trim();
		TryStripSceneSessionMarker(text, out text, out _);
		if (!TryReadAfefPrefix(text, out bool npc, out int prefixLength) || !npc)
		{
			return false;
		}
		string body = text.Substring(prefixLength).Trim();
		if (string.IsNullOrWhiteSpace(body))
		{
			return false;
		}
		if (isFirstMeetingBody != null && isFirstMeetingBody(body))
		{
			return true;
		}
		if (body.StartsWith(MemoryTextResources.Raw("MEM_Raw_037"), StringComparison.Ordinal) && body.EndsWith(MemoryTextResources.Raw("MEM_Raw_038"), StringComparison.Ordinal))
		{
			return true;
		}
		return body.StartsWith(MemoryTextResources.Raw("MEM_Raw_039"), StringComparison.Ordinal) && body.Contains(MemoryTextResources.Raw("MEM_Raw_040")) && body.EndsWith(MemoryTextResources.Raw("MEM_Raw_041"), StringComparison.Ordinal);
	}

	/// <summary>
	/// Walking from the newest line backwards, single-use NPC fact lines that appear before (i.e.
	/// older than) any meaningful direct conversation line are removed. Returns the kept lines in
	/// original order, or null when nothing was removed.
	/// </summary>
	internal static List<(int Day, string Date, string Line)> ExpireSingleUseFacts(IReadOnlyList<(int Day, string Date, string Line)> lines, Func<string, bool> isSingleUseFact, Func<string, bool> isMeaningfulConversation)
	{
		if (lines == null || lines.Count == 0)
		{
			return null;
		}
		bool removed = false;
		bool seenConversation = false;
		var kept = new List<(int, string, string)>(lines.Count);
		for (int i = lines.Count - 1; i >= 0; i--)
		{
			var item = lines[i];
			if (seenConversation && isSingleUseFact(item.Line))
			{
				removed = true;
				continue;
			}
			kept.Add(item);
			if (isMeaningfulConversation(item.Line))
			{
				seenConversation = true;
			}
		}
		if (!removed)
		{
			return null;
		}
		kept.Reverse();
		return kept;
	}

	/// <summary>Flatten non-blank lines with their day/date in stored order.</summary>
	internal static List<(int Day, string Date, string Line)> Flatten<TDay>(IEnumerable<TDay> days, Func<TDay, int> dayIndex, Func<TDay, string> date, Func<TDay, List<string>> lines) where TDay : class
	{
		var result = new List<(int, string, string)>();
		foreach (TDay day in days ?? Enumerable.Empty<TDay>())
		{
			if (day == null) continue;
			List<string> dayLines = lines(day);
			if (dayLines == null) continue;
			foreach (string line in dayLines)
			{
				if (!string.IsNullOrWhiteSpace(line))
				{
					result.Add((dayIndex(day), date(day), line));
				}
			}
		}
		return result;
	}

	/// <summary>Keep only the newest <paramref name="maxLines"/> entries.</summary>
	internal static List<(int Day, string Date, string Line)> TrimToNewest(List<(int Day, string Date, string Line)> lines, int maxLines)
	{
		if (lines == null) return new List<(int, string, string)>();
		return lines.Count > maxLines ? lines.Skip(lines.Count - maxLines).ToList() : lines;
	}

	/// <summary>Regroup flat lines into day containers (first-seen order), creating days through the factory.</summary>
	internal static List<TDay> Regroup<TDay>(IEnumerable<(int Day, string Date, string Line)> lines, Func<int, string, TDay> createDay, Func<TDay, int> dayIndex, Func<TDay, List<string>> dayLines) where TDay : class
	{
		var result = new List<TDay>();
		foreach (var entry in lines ?? Enumerable.Empty<(int, string, string)>())
		{
			TDay day = null;
			for (int i = 0; i < result.Count; i++)
			{
				if (dayIndex(result[i]) == entry.Day) { day = result[i]; break; }
			}
			if (day == null)
			{
				day = createDay(entry.Day, entry.Date);
				result.Add(day);
			}
			dayLines(day).Add(entry.Line);
		}
		return result;
	}
}
