using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Owns the bounded, scene-local GCCZ event timeline.
/// Persistence for named AF characters remains outside this store.
/// </summary>
public sealed class TownSceneMemoryStore
{
    private readonly object _sync = new object();
    private readonly List<string> _events = new List<string>();
    private readonly int _capacity;
    private int _sequence;

    public TownSceneMemoryStore(int capacity)
    {
        _capacity = Math.Max(1, capacity);
    }

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _events.Count;
            }
        }
    }

    public int Sequence
    {
        get
        {
            lock (_sync)
            {
                return _sequence;
            }
        }
    }

    public bool TryRecord(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
        {
            return false;
        }

        string normalizedEntry = entry.Trim();
        lock (_sync)
        {
            if (_events.Count > 0
                && string.Equals(
                    SiegeInterventionMemoryEventFormatter.StripSequencePrefix(_events[_events.Count - 1]),
                    normalizedEntry,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            _sequence++;
            _events.Add(_sequence + "." + normalizedEntry);
            while (_events.Count > _capacity)
            {
                _events.RemoveAt(0);
            }

            return true;
        }
    }

    public IReadOnlyList<string> Snapshot()
    {
        lock (_sync)
        {
            return Array.AsReadOnly(_events.ToArray());
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _events.Clear();
            _sequence = 0;
        }
    }
}
