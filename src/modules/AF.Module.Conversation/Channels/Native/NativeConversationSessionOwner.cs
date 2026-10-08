using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AnimusForge;

// Transient Native state only. Memory remains the authoritative persistent history owner.
// A captured key is never recomputed for a tentative event after a request starts.
internal sealed class NativeConversationSessionOwner
{
    private readonly object _gate = new object();
    private readonly Dictionary<string, List<AnimusForgeDialogueHistoryEntry>> _history = new Dictionary<string, List<AnimusForgeDialogueHistoryEntry>>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _recordedDialog = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<ConversationMessage>> _pendingFacts = new Dictionary<string, List<ConversationMessage>>(StringComparer.OrdinalIgnoreCase);
    private readonly Func<long> _nextEventSequence;
    private readonly int _maxConversationLines;

    internal NativeConversationSessionOwner(Func<long> nextEventSequence, int maxConversationLines)
    {
        _nextEventSequence = nextEventSequence ?? throw new ArgumentNullException(nameof(nextEventSequence));
        _maxConversationLines = maxConversationLines;
    }

    internal void ClearAll() { lock (_gate) { _history.Clear(); _recordedDialog.Clear(); _pendingFacts.Clear(); } }
    internal void CloseInput(bool clearHistory) { lock (_gate) { _recordedDialog.Clear(); if (clearHistory) _history.Clear(); } }
    internal void ClearPendingFacts() { lock (_gate) _pendingFacts.Clear(); }
    internal void Clear(string key, int dayIndex)
    {
        lock (_gate)
        {
            if (dayIndex < 0) { _history.Remove(key); _recordedDialog.Remove(key); return; }
            if (!_history.TryGetValue(key, out var entries) || entries == null) return;
            entries.RemoveAll(entry => entry != null && entry.GameDayIndex == dayIndex);
            if (entries.Count == 0) _history.Remove(key);
        }
    }
    internal bool HasHistory(string key) { lock (_gate) return _history.TryGetValue(key, out var entries) && entries != null && entries.Count > 0; }
    internal void Append(string key, AnimusForgeDialogueHistoryEntry entry)
    {
        lock (_gate)
        {
            if (!_history.TryGetValue(key, out var entries) || entries == null) _history[key] = entries = new List<AnimusForgeDialogueHistoryEntry>();
            entries.Add(CloneNativeConversationHistoryEntry(entry));
            TrimNativeConversationSessionHistory(entries);
        }
    }
    internal void RollbackPlayerEvent(string key, long sequence)
    {
        lock (_gate)
        {
            if (!_history.TryGetValue(key, out var entries) || entries == null) return;
            entries.RemoveAll(entry => entry != null && entry.EventSequence == sequence && string.Equals(entry.Kind,"player",StringComparison.OrdinalIgnoreCase));
            if (entries.Count == 0) _history.Remove(key);
        }
    }
    internal void MarkDialog(string key, string line) { lock (_gate) _recordedDialog[key] = line; }
    internal bool TryMarkDialog(string key, string line)
    {
        lock (_gate)
        {
            if (_recordedDialog.TryGetValue(key, out var existing) && string.Equals(existing,line,StringComparison.Ordinal)) return false;
            _recordedDialog[key] = line; return true;
        }
    }
    internal string LatestNpcUtterance(string key, ConversationSpeechTextOptions options = default)
    {
        lock (_gate)
        {
            if (!_history.TryGetValue(key,out var entries) || entries == null) return "";
            for (int i=entries.Count-1; i>=0; i--)
            {
                var entry=entries[i];
                if (entry == null || !string.Equals((entry.Kind ?? "").Trim(),"npc",StringComparison.OrdinalIgnoreCase)) continue;
                string text=ConversationSpeechTextRules.NormalizeNativeConversationVisibleTextKey(entry.Text, options);
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
            return "";
        }
    }
    internal bool IsLastNpcLine(string key, string line)
    {
        lock (_gate)
        {
            if (!_history.TryGetValue(key, out var entries) || entries == null) return false;
            var last = entries.LastOrDefault(x => x != null && string.Equals((x.Kind ?? "").Trim(), "npc", StringComparison.OrdinalIgnoreCase));
            return last != null && string.Equals(ConversationSpeechTextRules.NormalizeNativeConversationHistoryTextForPostprocess(last.Text), line, StringComparison.Ordinal);
        }
    }
    internal List<AnimusForgeDialogueHistoryEntry> GetTail(string key, int limit)
    {
        lock (_gate)
            return !_history.TryGetValue(key,out var entries) || entries == null ? new List<AnimusForgeDialogueHistoryEntry>() : entries.Skip(Math.Max(0,entries.Count-limit)).Select(CloneNativeConversationHistoryEntry).ToList();
    }
    internal List<AnimusForgeDialogueHistoryEntry> Snapshot(string key, int normalBudget)
    {
        lock (_gate)
        {
            if (!_history.TryGetValue(key,out var value) || value == null) return new List<AnimusForgeDialogueHistoryEntry>();
            int normalCount=0;
            var keepIndexes=new HashSet<int>();
            for(int i=value.Count-1;i>=0;i--)
            {
                var entry=value[i];
                if(entry != null && string.Equals((entry.Kind ?? "").Trim(),"fact",StringComparison.OrdinalIgnoreCase)) { keepIndexes.Add(i); continue; }
                if(normalCount<normalBudget) { keepIndexes.Add(i); normalCount++; }
            }
            var result=new List<AnimusForgeDialogueHistoryEntry>();
            for(int i=0;i<value.Count;i++) if(keepIndexes.Contains(i)) result.Add(CloneNativeConversationHistoryEntry(value[i]));
            return result;
        }
    }
    internal void QueueFact(string key, ConversationMessage message)
    {
        lock (_gate)
        {
            if (!_pendingFacts.TryGetValue(key,out var list) || list == null) _pendingFacts[key]=list=new List<ConversationMessage>();
            list.Add(CloneMessage(message));
            while (list.Count > 12) list.RemoveAt(0);
        }
    }
    internal List<ConversationMessage> ConsumeFacts(string key)
    {
        lock (_gate)
        {
            if(!_pendingFacts.TryGetValue(key,out var list) || list == null || list.Count==0) return new List<ConversationMessage>();
            _pendingFacts.Remove(key);
            return list.Where(x => x != null).Select(CloneMessage).ToList();
        }
    }
    private static ConversationMessage CloneMessage(ConversationMessage x)
    {
        return x == null ? null : new ConversationMessage { EventSequence=x.EventSequence,GameDayIndex=x.GameDayIndex,GameDate=x.GameDate,GameHour=x.GameHour,Scene=x.Scene,Role=x.Role,Content=x.Content,SpeakerName=x.SpeakerName,SpeakerAgentIndex=x.SpeakerAgentIndex,SpeakerHeroId=x.SpeakerHeroId,TargetAgentIndex=x.TargetAgentIndex,TargetName=x.TargetName,TargetHeroId=x.TargetHeroId,PlayerDistanceMeters=x.PlayerDistanceMeters,VisibleAgentIndices=new List<int>(x.VisibleAgentIndices ?? new List<int>()),VisibleHeroIds=new List<string>(x.VisibleHeroIds ?? new List<string>()) };
    }
    internal void AppendDiagnostics(StringBuilder output)
    {
        if(!Monitor.TryEnter(_gate)) { output.Append("nativeHistory=busy"); return; }
        try { output.Append("nativeHistoryKeys=").Append(_history.Count).Append(" nativeHistoryEntries=").Append(_history.Values.Sum(x=>x?.Count ?? 0)).Append(" nativeDedupKeys=").Append(_recordedDialog.Count); }
        finally { Monitor.Exit(_gate); }
    }
    internal string SyncDay(string key, int dayIndex, IEnumerable<AnimusForgeDialogueHistoryEntry> previousEntries, IEnumerable<AnimusForgeDialogueHistoryEntry> currentEntries, ConversationSpeechTextOptions options = default, bool completeDaySnapshot = true)
    {
        // UI line edits supply a delta, not an authoritative replacement for the whole day.
			List<AnimusForgeDialogueHistoryEntry> oldSnapshot = CloneNativeConversationHistoryEntriesForDailyMemoryEdit(previousEntries, dayIndex);
			List<AnimusForgeDialogueHistoryEntry> newSnapshot = CloneNativeConversationHistoryEntriesForDailyMemoryEdit(currentEntries, dayIndex);
			List<AnimusForgeDialogueHistoryEntry> removed = BuildNativeConversationHistoryEditDelta(oldSnapshot, newSnapshot, options);
			List<AnimusForgeDialogueHistoryEntry> added = BuildNativeConversationHistoryEditDelta(newSnapshot, oldSnapshot, options);
			if (removed.Count == 0 && added.Count == 0)
			{
				return "";
			}

			int replacedCount = 0;
			int removedCount = 0;
			int addedCount = 0;
			bool rebuilt = false;
			lock (_gate)
			{
				_history.TryGetValue(key, out var existing);
				List<AnimusForgeDialogueHistoryEntry> working = existing ?? new List<AnimusForgeDialogueHistoryEntry>();
				bool hadAffectedDayEntries = working.Any((AnimusForgeDialogueHistoryEntry x) => x != null && x.GameDayIndex == dayIndex);
				bool exactMatchFailed = false;
				int pairedCount = Math.Min(removed.Count, added.Count);
				List<Tuple<int, AnimusForgeDialogueHistoryEntry>> replacements = new List<Tuple<int, AnimusForgeDialogueHistoryEntry>>();
				List<int> removalIndexes = new List<int>();
				List<int> missingReplacementIndexes = new List<int>();
				HashSet<int> reservedIndexes = new HashSet<int>();

				if (hadAffectedDayEntries)
				{
					for (int i = 0; i < pairedCount; i++)
					{
						int index = FindNativeConversationHistoryEntryForDailyMemoryEdit(working, removed[i], dayIndex, reservedIndexes, options);
						if (index < 0)
						{
							exactMatchFailed = true;
							missingReplacementIndexes.Add(i);
							continue;
						}
						reservedIndexes.Add(index);
						replacements.Add(Tuple.Create(index, added[i]));
					}

					for (int i = pairedCount; i < removed.Count; i++)
					{
						int index = FindNativeConversationHistoryEntryForDailyMemoryEdit(working, removed[i], dayIndex, reservedIndexes, options);
						if (index < 0)
						{
							exactMatchFailed = true;
							continue;
						}
						reservedIndexes.Add(index);
						removalIndexes.Add(index);
					}
				}

				if (exactMatchFailed && completeDaySnapshot)
				{
					working = RebuildNativeConversationSessionHistoryDayForDailyMemoryEdit(existing, newSnapshot, dayIndex);
					rebuilt = true;
					replacedCount = 0;
					removedCount = 0;
					addedCount = newSnapshot.Count;
				}
				else
				{
					foreach (Tuple<int, AnimusForgeDialogueHistoryEntry> replacement in replacements)
					{
						long eventSequence = working[replacement.Item1].EventSequence;
						CopyNativeConversationHistoryEntryForDailyMemoryEdit(working[replacement.Item1], replacement.Item2);
						working[replacement.Item1].EventSequence = eventSequence > 0L ? eventSequence : _nextEventSequence();
						replacedCount++;
					}
					foreach (int index in removalIndexes.OrderByDescending((int x) => x))
					{
						working.RemoveAt(index);
						removedCount++;
					}
					// A missing old row may have been trimmed. Add its edited replacement without
					// deleting any unrelated rows; deletion of an absent row is a no-op.
					foreach (int index in missingReplacementIndexes)
					{
						AnimusForgeDialogueHistoryEntry entry = CloneNativeConversationHistoryEntry(added[index]);
						entry.EventSequence = _nextEventSequence();
						working.Add(entry);
						addedCount++;
					}
					int firstAddedIndex = hadAffectedDayEntries ? pairedCount : 0;
					for (int i = firstAddedIndex; i < added.Count; i++)
					{
						AnimusForgeDialogueHistoryEntry entry = CloneNativeConversationHistoryEntry(added[i]);
						entry.EventSequence = _nextEventSequence();
						working.Add(entry);
						addedCount++;
					}
				}

				TrimNativeConversationSessionHistory(working);
				if (working.Count == 0)
				{
					_history.Remove(key);
				}
				else
				{
					_history[key] = working;
				}
			}
        return "replaced=" + replacedCount + " removed=" + removedCount + " added=" + addedCount + " rebuilt=" + rebuilt + " completeDaySnapshot=" + completeDaySnapshot;
    }
private AnimusForgeDialogueHistoryEntry CloneNativeConversationHistoryEntry(AnimusForgeDialogueHistoryEntry entry)
	{
		if (entry == null)
		{
			return new AnimusForgeDialogueHistoryEntry();
		}
		return new AnimusForgeDialogueHistoryEntry
		{
			GameDayIndex = entry.GameDayIndex,
			GameDate = entry.GameDate ?? "",
			GameHour = entry.GameHour,
			Scene = entry.Scene ?? "",
			Speaker = entry.Speaker ?? "",
			TargetAgentIndex = entry.TargetAgentIndex,
			TargetName = entry.TargetName ?? "",
			Text = entry.Text ?? "",
			Kind = entry.Kind ?? "",
			EventSequence = entry.EventSequence
		};
	}

private List<AnimusForgeDialogueHistoryEntry> CloneNativeConversationHistoryEntriesForDailyMemoryEdit(IEnumerable<AnimusForgeDialogueHistoryEntry> entries, int dayIndex)
	{
		return (entries ?? Enumerable.Empty<AnimusForgeDialogueHistoryEntry>())
			.Where((AnimusForgeDialogueHistoryEntry x) => x != null && x.GameDayIndex == dayIndex && !string.IsNullOrWhiteSpace(x.Text))
			.Select(CloneNativeConversationHistoryEntry)
			.ToList();
	}

private List<AnimusForgeDialogueHistoryEntry> BuildNativeConversationHistoryEditDelta(IEnumerable<AnimusForgeDialogueHistoryEntry> source, IEnumerable<AnimusForgeDialogueHistoryEntry> target, ConversationSpeechTextOptions options)
	{
		Dictionary<string, int> targetCounts = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (AnimusForgeDialogueHistoryEntry entry in target ?? Enumerable.Empty<AnimusForgeDialogueHistoryEntry>())
		{
			string fingerprint = BuildNativeConversationHistoryDailyMemoryEditFingerprint(entry, options);
			if (!targetCounts.ContainsKey(fingerprint))
			{
				targetCounts[fingerprint] = 0;
			}
			targetCounts[fingerprint]++;
		}
		List<AnimusForgeDialogueHistoryEntry> result = new List<AnimusForgeDialogueHistoryEntry>();
		foreach (AnimusForgeDialogueHistoryEntry entry in source ?? Enumerable.Empty<AnimusForgeDialogueHistoryEntry>())
		{
			string fingerprint = BuildNativeConversationHistoryDailyMemoryEditFingerprint(entry, options);
			if (targetCounts.TryGetValue(fingerprint, out var count) && count > 0)
			{
				targetCounts[fingerprint] = count - 1;
				continue;
			}
			result.Add(CloneNativeConversationHistoryEntry(entry));
		}
		return result;
	}

private string BuildNativeConversationHistoryDailyMemoryEditFingerprint(AnimusForgeDialogueHistoryEntry entry, ConversationSpeechTextOptions options)
	{
		if (entry == null)
		{
			return "";
		}
		return BuildNativeConversationHistoryDailyMemoryEditCoreKey(entry, options)
			+ "\u001f" + (entry.Speaker ?? "").Trim().ToLowerInvariant()
			+ "\u001f" + entry.GameHour
			+ "\u001f" + (entry.Scene ?? "").Trim().ToLowerInvariant()
			+ "\u001f" + entry.TargetAgentIndex
			+ "\u001f" + (entry.TargetName ?? "").Trim().ToLowerInvariant();
	}

private string BuildNativeConversationHistoryDailyMemoryEditCoreKey(AnimusForgeDialogueHistoryEntry entry, ConversationSpeechTextOptions options)
	{
		if (entry == null)
		{
			return "";
		}
		return (entry.Kind ?? "").Trim().ToLowerInvariant() + "\u001f" + ConversationSpeechTextRules.NormalizeNativeConversationVisibleTextKey(entry.Text, options);
	}

private int FindNativeConversationHistoryEntryForDailyMemoryEdit(List<AnimusForgeDialogueHistoryEntry> entries, AnimusForgeDialogueHistoryEntry expected, int dayIndex, HashSet<int> reservedIndexes, ConversationSpeechTextOptions options)
	{
		string expectedFingerprint = BuildNativeConversationHistoryDailyMemoryEditFingerprint(expected, options);
		for (int i = 0; i < (entries?.Count ?? 0); i++)
		{
			AnimusForgeDialogueHistoryEntry candidate = entries[i];
			if ((reservedIndexes == null || !reservedIndexes.Contains(i)) && candidate != null && candidate.GameDayIndex == dayIndex && string.Equals(BuildNativeConversationHistoryDailyMemoryEditFingerprint(candidate, options), expectedFingerprint, StringComparison.Ordinal))
			{
				return i;
			}
		}
		string expectedKey = BuildNativeConversationHistoryDailyMemoryEditCoreKey(expected, options);
		for (int i = 0; i < (entries?.Count ?? 0); i++)
		{
			AnimusForgeDialogueHistoryEntry candidate = entries[i];
			if ((reservedIndexes == null || !reservedIndexes.Contains(i)) && candidate != null && candidate.GameDayIndex == dayIndex && string.Equals(BuildNativeConversationHistoryDailyMemoryEditCoreKey(candidate, options), expectedKey, StringComparison.Ordinal))
			{
				return i;
			}
		}
		return -1;
	}

private void CopyNativeConversationHistoryEntryForDailyMemoryEdit(AnimusForgeDialogueHistoryEntry target, AnimusForgeDialogueHistoryEntry source)
	{
		if (target == null || source == null)
		{
			return;
		}
		target.GameDayIndex = source.GameDayIndex;
		target.GameDate = source.GameDate ?? "";
		target.GameHour = source.GameHour;
		target.Scene = source.Scene ?? "";
		target.Speaker = source.Speaker ?? "";
		target.TargetAgentIndex = source.TargetAgentIndex;
		target.TargetName = source.TargetName ?? "";
		target.Text = source.Text ?? "";
		target.Kind = source.Kind ?? "";
	}

private List<AnimusForgeDialogueHistoryEntry> RebuildNativeConversationSessionHistoryDayForDailyMemoryEdit(IEnumerable<AnimusForgeDialogueHistoryEntry> existing, IEnumerable<AnimusForgeDialogueHistoryEntry> replacement, int dayIndex)
	{
		List<AnimusForgeDialogueHistoryEntry> result = (existing ?? Enumerable.Empty<AnimusForgeDialogueHistoryEntry>())
			.Where((AnimusForgeDialogueHistoryEntry x) => x != null && x.GameDayIndex != dayIndex)
			.ToList();
		foreach (AnimusForgeDialogueHistoryEntry source in replacement ?? Enumerable.Empty<AnimusForgeDialogueHistoryEntry>())
		{
			AnimusForgeDialogueHistoryEntry entry = CloneNativeConversationHistoryEntry(source);
			entry.EventSequence = _nextEventSequence();
			result.Add(entry);
		}
		return result;
	}

private void TrimNativeConversationSessionHistory(List<AnimusForgeDialogueHistoryEntry> entries)
	{
		if (entries == null || entries.Count <= _maxConversationLines)
		{
			return;
		}
		int conversationCount = 0;
		for (int i = 0; i < entries.Count; i++)
		{
			if (!string.Equals((entries[i]?.Kind ?? "").Trim(), "fact", StringComparison.OrdinalIgnoreCase))
			{
				conversationCount++;
			}
		}
		int removeCount = conversationCount - _maxConversationLines;
		if (removeCount <= 0)
		{
			return;
		}
		int writeIndex = 0;
		for (int readIndex = 0; readIndex < entries.Count; readIndex++)
		{
			AnimusForgeDialogueHistoryEntry entry = entries[readIndex];
			bool isFact = string.Equals((entry?.Kind ?? "").Trim(), "fact", StringComparison.OrdinalIgnoreCase);
			if (!isFact && removeCount > 0)
			{
				removeCount--;
				continue;
			}
			entries[writeIndex++] = entry;
		}
		if (writeIndex < entries.Count)
		{
			entries.RemoveRange(writeIndex, entries.Count - writeIndex);
		}
	}

    internal static int ResolveHistoryLineLimit(int requested, int maximum, int configured) => requested > 0 ? Math.Max(1, Math.Min(maximum, requested)) : configured;

internal static List<ConversationMessage> ProjectHistoryMessages(List<AnimusForgeDialogueHistoryEntry> entries, string npcName, int targetAgentIndex, IReadOnlyDictionary<int, float> distances)
	{
		List<ConversationMessage> list = new List<ConversationMessage>();
		try
		{

			string targetName = (npcName ?? "").Trim();
			string npcSpeakerName = string.IsNullOrWhiteSpace(targetName) ? "NPC" : targetName;
			for (int i = 0; i < entries.Count; i++)
			{
				AnimusForgeDialogueHistoryEntry entry = entries[i];
				string text = (entry?.Text ?? "").Replace("\r", "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				string kind = (entry?.Kind ?? "").Trim().ToLowerInvariant();
				string speaker = string.IsNullOrWhiteSpace(entry?.Speaker) ? "" : entry.Speaker.Trim();
				if (kind == "fact")
				{
					string factLine = ConversationSpeechTextRules.NormalizeNativeConversationFactLineForPrompt(text, speaker);
					if (!string.IsNullOrWhiteSpace(factLine))
					{
						list.Add(new ConversationMessage
						{
							EventSequence = entry.EventSequence,
							GameDayIndex = entry.GameDayIndex,
							GameDate = entry.GameDate ?? "",
							GameHour = entry.GameHour,
							Scene = entry.Scene ?? "",
							Role = "system",
							Content = factLine,
							SpeakerName = "系统",
							SpeakerAgentIndex = -1
						});
					}
					continue;
				}
				if (kind == "npc")
				{
					list.Add(new ConversationMessage
					{
						EventSequence = entry.EventSequence,
						GameDayIndex = entry.GameDayIndex,
						GameDate = entry.GameDate ?? "",
						GameHour = entry.GameHour,
						Scene = entry.Scene ?? "",
						Role = "assistant",
						Content = text,
						SpeakerName = string.IsNullOrWhiteSpace(speaker) ? npcSpeakerName : speaker,
						SpeakerAgentIndex = targetAgentIndex
					});
					continue;
				}
				list.Add(new ConversationMessage
				{
					EventSequence = entry.EventSequence,
					GameDayIndex = entry.GameDayIndex,
					GameDate = entry.GameDate ?? "",
					GameHour = entry.GameHour,
					Scene = entry.Scene ?? "",
					Role = "user",
					Content = text,
					SpeakerName = "你",
					SpeakerAgentIndex = -1,
					TargetAgentIndex = entry.TargetAgentIndex >= 0 ? entry.TargetAgentIndex : targetAgentIndex,
					TargetName = string.IsNullOrWhiteSpace(entry.TargetName) ? targetName : entry.TargetName.Trim(),
					PlayerDistanceMeters = distances != null && distances.TryGetValue(entry.TargetAgentIndex >= 0 ? entry.TargetAgentIndex : targetAgentIndex, out var distance) ? distance : -1f
				});
			}
		}
		catch
		{
		}
		return list;
	}
}
