using System;

namespace AnimusForge.Refactor.Modules;

internal sealed class WeeklyReportPopupSessionOwner
{
    private DateTime _openedAtUtc;
    private readonly double _minimumDwellSeconds;
    private DateTime _suspendedAtUtc;
    private long _resumeInputGuardUntilUtcTicks;
    private bool _minimumDwellClaimed;
    private bool _pendingClose;

    internal WeeklyReportPopupSessionOwner(DateTime openedAtUtc, double minimumDwellSeconds)
    {
        _openedAtUtc = openedAtUtc;
        _minimumDwellSeconds = Math.Max(0.0, minimumDwellSeconds);
    }

    internal bool IsClosed { get; private set; }
    internal bool IsSuspended { get; private set; }
    internal bool CanProcessTick => !IsClosed && !IsSuspended;

    internal bool TryClaimMinimumDwell(DateTime nowUtc, bool hasCallback)
    {
        if (!CanProcessTick || _minimumDwellClaimed || !hasCallback || _minimumDwellSeconds <= 0.0
            || (nowUtc - _openedAtUtc).TotalSeconds < _minimumDwellSeconds)
            return false;
        _minimumDwellClaimed = true;
        return true;
    }

    internal bool Suspend(DateTime nowUtc)
    {
        if (IsClosed || IsSuspended)
            return false;
        IsSuspended = true;
        _suspendedAtUtc = nowUtc;
        return true;
    }

    internal bool Resume(DateTime nowUtc)
    {
        if (IsClosed || !IsSuspended)
            return false;
        if (_suspendedAtUtc != default(DateTime) && nowUtc > _suspendedAtUtc)
            _openedAtUtc = _openedAtUtc.Add(nowUtc - _suspendedAtUtc);
        _suspendedAtUtc = default(DateTime);
        IsSuspended = false;
        _resumeInputGuardUntilUtcTicks = nowUtc.AddMilliseconds(350.0).Ticks;
        return true;
    }

    internal bool CanHandleEscape(DateTime nowUtc)
        => CanProcessTick && nowUtc.Ticks >= _resumeInputGuardUntilUtcTicks;

    internal bool RequestClose()
    {
        if (IsClosed || _pendingClose)
            return false;
        _pendingClose = true;
        return true;
    }

    internal bool TryTakePendingClose()
    {
        if (!CanProcessTick || !_pendingClose)
            return false;
        _pendingClose = false;
        return true;
    }

    internal bool Close()
    {
        if (IsClosed)
            return false;
        IsClosed = true;
        IsSuspended = false;
        _suspendedAtUtc = default(DateTime);
        _pendingClose = false;
        return true;
    }
}
