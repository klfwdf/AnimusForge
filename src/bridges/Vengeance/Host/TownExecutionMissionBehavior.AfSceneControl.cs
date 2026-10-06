using RichExecutions.Core;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

// AF-only projection of the real execution owner. The shared standalone
// implementation and its state machine do not acquire AF conflict policy.
public sealed partial class TownExecutionMissionBehavior
{
    internal bool OwnsAfSceneControl => !_cleanupComplete
        && (State != ExecutionSessionState.Aftermath
            || !_playerExecutionStateRestored
            || !_aftermathConversationsRestored
            || !_aftermathSessionReleased);

    // A frozen executed victim can remain active after ordinary town control
    // resumes. Reference identity also prevents cross-Mission index reuse.
    internal bool IsAfExecutionVictim(Agent agent) => !_cleanupComplete
        && agent != null && ReferenceEquals(agent, _victimAgent);
}
