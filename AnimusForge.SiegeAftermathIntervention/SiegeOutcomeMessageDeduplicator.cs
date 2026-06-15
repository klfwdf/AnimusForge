using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free per-outcome message de-duplication.
/// AF adapters decide how to display messages; this core only decides whether a key is new
/// within the current outcome track.
/// </summary>
public sealed class SiegeOutcomeMessageDeduplicator
{
    private readonly HashSet<string> _shownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private string _currentTrack = string.Empty;

    public string CurrentTrack => _currentTrack;

    public int ShownKeyCount => _shownKeys.Count;

    public void Reset()
    {
        _currentTrack = string.Empty;
        _shownKeys.Clear();
    }

    public bool ResetForTrack(string track)
    {
        string normalized = (track ?? string.Empty).Trim();
        if (string.Equals(_currentTrack, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        _currentTrack = normalized;
        _shownKeys.Clear();
        return true;
    }

    public bool ShouldShow(string key, string message)
    {
        string normalized = (key ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized) || string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return _shownKeys.Add(normalized);
    }
}
