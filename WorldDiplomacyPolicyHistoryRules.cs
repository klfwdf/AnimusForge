using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AnimusForge;

// Pure rules shared by save migration and offline regression tests. Never runs on a tick.
internal static class WorldDiplomacyPolicyHistoryRules
{
	internal static bool CanAdvanceCompression(long cutoff, long snapshotSequence, long snapshotTokens, long targetTokens)
		=> cutoff > snapshotSequence || snapshotTokens > targetTokens;

	internal static long NextEventRevision(IDictionary<string, string> fingerprints,
		IDictionary<string, long> revisions, string policyId, string fingerprint)
	{
		if (fingerprints.TryGetValue(policyId, out string previous)
			&& string.Equals(previous, fingerprint, StringComparison.Ordinal)) return 0L;
		revisions.TryGetValue(policyId, out long revision);
		return checked(Math.Max(0L, revision) + 1L);
	}

	internal static long SelectCompressionPrefix<T>(long snapshotSequence, long snapshotTokens,
		IEnumerable<T> orderedEntries, long inputBudget, Func<T, long> sequence, Func<T, long> tokens)
	{
		long cutoff = snapshotSequence;
		long used = Math.Max(0L, snapshotTokens);
		foreach (T entry in orderedEntries)
		{
			long nextTokens = Math.Max(0L, tokens(entry));
			if (used > inputBudget || nextTokens > inputBudget - used) break;
			used += nextTokens;
			cutoff = sequence(entry);
		}
		return cutoff;
	}

	private static readonly Regex Countdown = new Regex(
		@"｜状态：剩余 (?<remaining>[0-9]+)/(?<total>[0-9]+) 天",
		RegexOptions.CultureInvariant | RegexOptions.Compiled);

	internal static bool IsCountdownOnlyAdvance(string previous, string current)
	{
		string before = (previous ?? "").TrimEnd();
		string after = (current ?? "").TrimEnd();
		// Touch only the adapter's generated impact section, never the player's policy prose.
		int beforeStart = before.LastIndexOf("\n影响：", StringComparison.Ordinal);
		int afterStart = after.LastIndexOf("\n影响：", StringComparison.Ordinal);
		if (beforeStart < 0 || afterStart < 0) return false;
		int beforeEnd = before.IndexOf("\n机械效果：", beforeStart, StringComparison.Ordinal);
		int afterEnd = after.IndexOf("\n机械效果：", afterStart, StringComparison.Ordinal);
		if (beforeEnd < 0) beforeEnd = before.Length;
		if (afterEnd < 0) afterEnd = after.Length;
		string beforeImpact = before.Substring(beforeStart, beforeEnd - beforeStart);
		string afterImpact = after.Substring(afterStart, afterEnd - afterStart);
		MatchCollection oldClocks = Countdown.Matches(beforeImpact);
		MatchCollection newClocks = Countdown.Matches(afterImpact);
		if (oldClocks.Count == 0 || oldClocks.Count != newClocks.Count) return false;
		for (int i = 0; i < oldClocks.Count; i++)
		{
			if (!long.TryParse(oldClocks[i].Groups["remaining"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out long oldDays)
				|| !long.TryParse(newClocks[i].Groups["remaining"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out long newDays)
				|| newDays > oldDays) return false; // A renewed clock is a real change, even if its duration stayed the same.
		}
		string oldNormalized = before.Substring(0, beforeStart)
			+ Countdown.Replace(beforeImpact, "｜状态：生效中/${total} 天") + before.Substring(beforeEnd);
		string newNormalized = after.Substring(0, afterStart)
			+ Countdown.Replace(afterImpact, "｜状态：生效中/${total} 天") + after.Substring(afterEnd);
		return string.Equals(oldNormalized, newNormalized, StringComparison.Ordinal);
	}

	internal static List<T> CollapseCountdownCopies<T>(IEnumerable<T> orderedEntries,
		Func<T, string> policyIdentity, Func<T, string> text, out int removed, Func<T, T, bool> sameEventContext = null)
	{
		var previousByPolicy = new Dictionary<string, T>(StringComparer.Ordinal);
		var retained = new List<T>();
		removed = 0;
		foreach (T entry in orderedEntries)
		{
			string identity = policyIdentity(entry);
			if (string.IsNullOrWhiteSpace(identity)) { retained.Add(entry); continue; }
			bool duplicate = previousByPolicy.TryGetValue(identity, out T previous)
				&& (sameEventContext == null || sameEventContext(previous, entry))
				&& IsCountdownOnlyAdvance(text(previous), text(entry));
			// Compare to the last observation, not the retained first clock: 100 -> 90 -> 95 is a renewal.
			previousByPolicy[identity] = entry;
			if (duplicate) removed++; else retained.Add(entry);
		}
		return retained;
	}
}
