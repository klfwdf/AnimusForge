using System;

namespace AnimusForge.Refactor.Modules;

internal sealed class OnboardingSessionOwner
{
    private bool _startupNoticeShown;
    private bool _startupNoticePending;
    private long _startupNoticeDueTicks;
    private bool _welcomeShown;
    private bool _welcomePending;
    private long _welcomeDueTicks;
    private bool _actionPostprocessShown;
    private bool _actionPostprocessPending;
    private long _actionPostprocessDueTicks;

    internal void MarkStartupNotice(long nowTicks)
    {
        _startupNoticePending = true;
        _startupNoticeDueTicks = nowTicks + TimeSpan.FromSeconds(1.0).Ticks;
    }

    internal void MarkWelcome(long nowTicks)
    {
        _welcomePending = true;
        _welcomeDueTicks = nowTicks + TimeSpan.FromSeconds(2.0).Ticks;
    }

    internal void MarkActionPostprocess(long nowTicks)
    {
        _actionPostprocessPending = true;
        _actionPostprocessDueTicks = nowTicks + TimeSpan.FromSeconds(3.0).Ticks;
    }

    internal bool TryClaimStartupNotice(long nowTicks, bool gameStarted)
    {
        if (!gameStarted || !_startupNoticePending || _startupNoticeShown || nowTicks < _startupNoticeDueTicks)
            return false;
        _startupNoticePending = false;
        _startupNoticeShown = true;
        return true;
    }

    internal bool TryClaimWelcome(long nowTicks, bool gameStarted, bool setupDone)
    {
        if (!gameStarted || setupDone || !_welcomePending || _welcomeShown || nowTicks < _welcomeDueTicks)
            return false;
        _welcomePending = false;
        _welcomeShown = true;
        return true;
    }

    internal bool TryClaimActionPostprocess(long nowTicks, bool gameStarted, bool setupDone)
    {
        if (!gameStarted || !setupDone || !_actionPostprocessPending || _actionPostprocessShown
            || nowTicks < _actionPostprocessDueTicks)
            return false;
        _actionPostprocessPending = false;
        _actionPostprocessShown = true;
        return true;
    }

    internal void ResetWelcomeShown() => _welcomeShown = false;
    internal void CancelWelcome() => _welcomePending = false;
}
