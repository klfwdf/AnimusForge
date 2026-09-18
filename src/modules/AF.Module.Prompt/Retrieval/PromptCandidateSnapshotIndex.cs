using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// Only keys and expiry metadata live here. The compatibility adapter retains game payloads.
internal sealed class PromptCandidateSnapshotIndex
{
    private sealed class Entry
    {
        internal DateTime CreatedUtc;
        internal long Sequence;
    }

    private readonly int _maxKeys;
    private readonly TimeSpan _maxAge;
    private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
    private long _nextSequence;

    internal PromptCandidateSnapshotIndex(int maxKeys, TimeSpan maxAge)
    {
        _maxKeys = maxKeys;
        _maxAge = maxAge;
    }

    // Caller holds its payload lock across publication and removal of returned keys.
    internal IReadOnlyList<string> Publish(string key, DateTime now)
    {
        _entries[key] = new Entry { CreatedUtc = now, Sequence = ++_nextSequence };
        var removed = _entries.Where(x => x.Value.CreatedUtc < now - _maxAge).Select(x => x.Key).ToList();
        foreach (string expired in removed) _entries.Remove(expired);
        if (_entries.Count > _maxKeys)
        {
            var overflow = _entries.OrderBy(x => x.Value.CreatedUtc).ThenBy(x => x.Value.Sequence)
                .Take(_entries.Count - _maxKeys).Select(x => x.Key).ToList();
            foreach (string keyToRemove in overflow) _entries.Remove(keyToRemove);
            removed.AddRange(overflow);
        }
        return removed;
    }

    internal bool IsFresh(string key, DateTime now)
    {
        if (!_entries.TryGetValue(key, out Entry entry)) return false;
        if (now - entry.CreatedUtc <= _maxAge) return true;
        _entries.Remove(key);
        return false;
    }

    internal int Count => _entries.Count;
}
