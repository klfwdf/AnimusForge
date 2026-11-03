using System;
using RichExecutions.Diagnostics;

namespace RichExecutions.Scene;

public sealed partial class TownExecutionMissionBehavior
{
    // This request owns an execution, not the parent camp mission's lifetime.
    private void RetireCancelledCampExecution()
    {
        // Preserve the existing mission-exit commit owner after a lethal event.
        if (_campaignCommitPending) return;
        if (++_fieldCampPreparationCleanupAttempts > 3)
        {
            RexLog.Warning("Camp execution cleanup exhausted bounded retries; manual leave remains available.");
            return;
        }
        try
        {
            FinalizeSession();
            if (!_cleanupComplete) { _endMissionNextTick = true; return; }
            var mission = Mission;
            var deathLogic = _deathLogic;
            if (mission is null) return;
            if (deathLogic is not null) mission.RemoveMissionBehavior(deathLogic);
            mission.RemoveMissionBehavior(this);
        }
        catch (Exception exception)
        {
            _endMissionNextTick = true;
            RexLog.Error("Camp execution cleanup will retry without closing the camp.", exception);
        }
    }
}
