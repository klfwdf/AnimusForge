using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Owns the explicit lifetime boundary for scene-local town dialogue memory.
/// Named AF character persistence remains outside this session.
/// </summary>
public sealed class TownSceneMemorySession
{
    private readonly object _sync = new object();
    private readonly TownSceneMemoryStore _memory;
    private string _settlementId = string.Empty;
    private bool _isActive;

    public TownSceneMemorySession(int capacity)
    {
        _memory = new TownSceneMemoryStore(capacity);
    }

    public bool IsActive
    {
        get
        {
            lock (_sync)
            {
                return _isActive;
            }
        }
    }

    public string SettlementId
    {
        get
        {
            lock (_sync)
            {
                return _settlementId;
            }
        }
    }

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _isActive ? _memory.Count : 0;
            }
        }
    }

    public bool Begin(string settlementId)
    {
        string normalizedSettlementId = NormalizeSettlementId(settlementId);
        if (normalizedSettlementId.Length == 0)
        {
            EndScene();
            return false;
        }

        lock (_sync)
        {
            _memory.Reset();
            _settlementId = normalizedSettlementId;
            _isActive = true;
            return true;
        }
    }

    public bool IsActiveFor(string settlementId)
    {
        string normalizedSettlementId = NormalizeSettlementId(settlementId);
        lock (_sync)
        {
            return _isActive
                && normalizedSettlementId.Length > 0
                && string.Equals(_settlementId, normalizedSettlementId, StringComparison.OrdinalIgnoreCase);
        }
    }

    public bool TryRecord(string entry)
    {
        lock (_sync)
        {
            return _isActive && _memory.TryRecord(entry);
        }
    }

    public IReadOnlyList<string> Snapshot()
    {
        lock (_sync)
        {
            return _isActive
                ? _memory.Snapshot()
                : Array.Empty<string>();
        }
    }

    public bool EndScene()
    {
        lock (_sync)
        {
            bool ended = _isActive || _memory.Count > 0 || _settlementId.Length > 0;
            _memory.Reset();
            _settlementId = string.Empty;
            _isActive = false;
            return ended;
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _memory.Reset();
            _settlementId = string.Empty;
            _isActive = false;
        }
    }

    private static string NormalizeSettlementId(string settlementId)
    {
        return string.IsNullOrWhiteSpace(settlementId)
            ? string.Empty
            : settlementId.Trim();
    }
}
