using System;

namespace AnimusForge.SiegeAftermathIntervention;

public enum TownDirectAftermathKind
{
    None = 0,
    Plunder = 1,
    Massacre = 2,
}

public enum TownDirectAftermathPhase
{
    Inactive = 0,
    AwaitingResolution = 1,
    AwaitingLootClose = 2,
    AwaitingEncounterFinish = 3,
}

/// <summary>
/// Owns the mutually exclusive direct plunder or massacre aftermath flow without runtime side effects.
/// </summary>
public sealed class TownDirectAftermathFlowState
{
    private TownDirectAftermathKind _kind;
    private TownDirectAftermathPhase _phase;
    private bool _messageClaimed;
    private string _lastDeferKey = string.Empty;

    public TownDirectAftermathKind Kind => _kind;

    public TownDirectAftermathPhase Phase => _phase;

    public bool IsPending => _kind != TownDirectAftermathKind.None
        && _phase != TownDirectAftermathPhase.Inactive;

    public bool HasOpenedLootScreen => _phase == TownDirectAftermathPhase.AwaitingLootClose
        || _phase == TownDirectAftermathPhase.AwaitingEncounterFinish;

    public bool IsWaitingForLootClose => _phase == TownDirectAftermathPhase.AwaitingLootClose;

    public bool IsAwaitingEncounterFinish => _phase == TownDirectAftermathPhase.AwaitingEncounterFinish;

    public bool IsPendingFor(TownDirectAftermathKind kind)
    {
        return IsPending && kind != TownDirectAftermathKind.None && _kind == kind;
    }

    public bool Queue(TownDirectAftermathKind kind)
    {
        if (kind == TownDirectAftermathKind.None)
        {
            return false;
        }

        _kind = kind;
        _phase = TownDirectAftermathPhase.AwaitingResolution;
        _messageClaimed = false;
        _lastDeferKey = string.Empty;
        return true;
    }

    public bool TryBeginLootScreen(TownDirectAftermathKind kind)
    {
        if (!IsPendingFor(kind) || _phase != TownDirectAftermathPhase.AwaitingResolution)
        {
            return false;
        }

        _phase = TownDirectAftermathPhase.AwaitingLootClose;
        _lastDeferKey = string.Empty;
        return true;
    }

    public bool TryMarkLootScreenClosed(TownDirectAftermathKind kind)
    {
        if (!IsPendingFor(kind) || _phase != TownDirectAftermathPhase.AwaitingLootClose)
        {
            return false;
        }

        _phase = TownDirectAftermathPhase.AwaitingEncounterFinish;
        return true;
    }

    public bool TryRecoverLootScreenOpenFailure(TownDirectAftermathKind kind)
    {
        if (!IsPendingFor(kind) || _phase != TownDirectAftermathPhase.AwaitingLootClose)
        {
            return false;
        }

        _phase = TownDirectAftermathPhase.AwaitingResolution;
        return true;
    }

    public bool TryClaimMessage(TownDirectAftermathKind kind)
    {
        if (!IsPendingFor(kind) || _messageClaimed)
        {
            return false;
        }

        _messageClaimed = true;
        return true;
    }

    public bool TrySetDeferKey(TownDirectAftermathKind kind, string key)
    {
        if (!IsPendingFor(kind))
        {
            return false;
        }

        string normalized = (key ?? string.Empty).Trim();
        if (string.Equals(_lastDeferKey, normalized, StringComparison.Ordinal))
        {
            return false;
        }

        _lastDeferKey = normalized;
        return true;
    }

    public bool Complete(TownDirectAftermathKind kind)
    {
        if (!IsPendingFor(kind))
        {
            return false;
        }

        Reset();
        return true;
    }

    public bool ResetIfKind(TownDirectAftermathKind kind)
    {
        if (!IsPendingFor(kind))
        {
            return false;
        }

        Reset();
        return true;
    }

    public void Reset()
    {
        _kind = TownDirectAftermathKind.None;
        _phase = TownDirectAftermathPhase.Inactive;
        _messageClaimed = false;
        _lastDeferKey = string.Empty;
    }
}
