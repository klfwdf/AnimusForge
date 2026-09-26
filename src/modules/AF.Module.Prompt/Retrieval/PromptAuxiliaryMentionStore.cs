using System;
using System.Collections.Generic;

namespace AnimusForge;

// Cross-request auxiliary mention state; ambient latest mentions have a separate request scope.
internal sealed class PromptAuxiliaryMentionStore
{
    private readonly object _gate = new object();
    private readonly Dictionary<string, MentionedWorldEntities> _values =
        new Dictionary<string, MentionedWorldEntities>(StringComparer.Ordinal);
    private readonly Queue<string> _order = new Queue<string>();
    private readonly int _capacity;

    internal PromptAuxiliaryMentionStore(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    internal void Publish(string key, MentionedWorldEntities entities)
    {
        if (string.IsNullOrWhiteSpace(key) || entities == null || entities.IsEmpty) return;
        lock (_gate)
        {
            if (!_values.TryGetValue(key, out var existing) || existing == null)
            {
                existing = new MentionedWorldEntities();
                _values[key] = existing;
                _order.Enqueue(key);
            }
            existing.Merge(entities);
            while (_values.Count > _capacity && _order.Count > 0)
            {
                string oldest = _order.Dequeue();
                if (!string.Equals(oldest, key, StringComparison.Ordinal)) _values.Remove(oldest);
            }
        }
    }

    internal MentionedWorldEntities Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        lock (_gate)
            return _values.TryGetValue(key, out var value) ? value?.Clone() : null;
    }
}
