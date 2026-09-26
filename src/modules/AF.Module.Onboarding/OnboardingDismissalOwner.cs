using System;
using System.Collections.Generic;

namespace AnimusForge.Refactor.Modules;

internal sealed class OnboardingDismissalOwner<TStage>
{
    private TStage _pendingStage;
    private long _resumeAfterUtcTicks;
    private bool _hasPendingStage;

    internal void Reset()
    {
        _pendingStage = default(TStage);
        _resumeAfterUtcTicks = 0;
        _hasPendingStage = false;
    }

    internal bool TryClaim(TStage stage, long nowUtcTicks, out TStage resumeStage)
    {
        resumeStage = default(TStage);
        if (!_hasPendingStage || !EqualityComparer<TStage>.Default.Equals(_pendingStage, stage))
        {
            _pendingStage = stage;
            _resumeAfterUtcTicks = nowUtcTicks + TimeSpan.FromMilliseconds(150.0).Ticks;
            _hasPendingStage = true;
            return false;
        }
        if (nowUtcTicks < _resumeAfterUtcTicks)
            return false;
        resumeStage = _pendingStage;
        Reset();
        return true;
    }
}
