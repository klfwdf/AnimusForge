using System;
using RichExecutions.Diagnostics;
namespace RichExecutions.Scene;
public sealed partial class TownExecutionMissionBehavior
{
    // Cancelled field-camp preparation never owns the parent mission's exit.
    private void RetireCancelledCampPreparation()
    {
        if (++_fieldCampPreparationCleanupAttempts > 3)
        {
            RexLog.Warning("Cancelled camp preparation cleanup exhausted its bounded retry; camp remains open for manual leave.");
            return;
        }
        try
        {
            FinalizeSession();
            if (!_cleanupComplete) { _endMissionNextTick = true; return; }
            var mission = Mission;
            var deathLogic = _deathLogic;
            if (mission is null) return;
            // Remove this host LAST: native removal invalidates this.Mission.
            if (deathLogic is not null) mission.RemoveMissionBehavior(deathLogic);
            mission.RemoveMissionBehavior(this);
        }
        catch (Exception exception)
        {
            _endMissionNextTick = true;
            RexLog.Error("Could not retire the cancelled camp preparation; bounded cleanup will retry without ending camp.", exception);
        }
    }
}
