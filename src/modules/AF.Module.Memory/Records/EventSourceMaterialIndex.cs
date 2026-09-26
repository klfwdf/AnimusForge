using System;
using System.Collections.Generic;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Owns the derived event-material index policy and its source binding, not the
/// persisted material records. Callers publish a complete Build result, then Bind.
/// Uses existing serial owner writes; this is not a concurrent collection.
/// </summary>
internal sealed class EventSourceMaterialIndex<T> where T : class
{
    private readonly Func<T, int> _day;
    private readonly Func<T, string> _stableKey;
    private readonly Func<int, string, string> _buildKey;
    private List<T> _source;
    private Dictionary<string, T> _map;
    private int _count;
    private List<T>.Enumerator _structureProbe;

    internal EventSourceMaterialIndex(Func<T, int> day, Func<T, string> stableKey,
        Func<int, string, string> buildKey)
    {
        _day = day ?? throw new ArgumentNullException(nameof(day));
        _stableKey = stableKey ?? throw new ArgumentNullException(nameof(stableKey));
        _buildKey = buildKey ?? throw new ArgumentNullException(nameof(buildKey));
    }

    internal bool IsCurrent(List<T> source, Dictionary<string, T> map)
    {
        if (map == null || !ReferenceEquals(_map, map)
            || !ReferenceEquals(_source, source) || _count != (source?.Count ?? 0)) return false;
        if (source == null) return true;
        // Even an exhausted List enumerator detects same-count replacements.
        try { _structureProbe.MoveNext(); return true; }
        catch (InvalidOperationException) { return false; }
    }

    internal Dictionary<string, T> Build(List<T> source)
    {
        var rebuilt = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        if (source != null)
        {
            foreach (T item in source)
            {
                if (item == null) continue;
                string text = (_stableKey(item) ?? "").Trim();
                int day = _day(item);
                string key = _buildKey(day, text);
                if (!string.IsNullOrWhiteSpace(text)) rebuilt[key] = item;
                // Preserve AF's named last-wins and nonnegative-day blank first-wins.
                // Negative named days still use the original caller's key policy.
                else if (day >= 0 && !rebuilt.ContainsKey(key)) rebuilt.Add(key, item);
            }
        }
        return rebuilt;
    }

    internal void Bind(List<T> source, Dictionary<string, T> map)
    {
        _source = source;
        _map = map;
        _count = source?.Count ?? 0;
        _structureProbe = source == null ? default(List<T>.Enumerator) : source.GetEnumerator();
    }
}
