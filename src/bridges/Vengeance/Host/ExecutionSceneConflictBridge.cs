using System.Runtime.CompilerServices;
using RichExecutions.Scene;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal static class ExecutionSceneConflictBridge
{
    // Registered once at injection, read on the game thread. Weak Mission keys
    // avoid retaining ended scenes; lookups do not enumerate behaviors/agents.
    private static readonly ConditionalWeakTable<Mission, TownExecutionMissionBehavior> Controllers = new();

    internal static void Register(Mission mission, TownExecutionMissionBehavior controller)
    {
        if (mission == null || controller == null) return;
        Controllers.Remove(mission);
        Controllers.Add(mission, controller);
    }

    internal static bool IsSceneControlled(Mission mission) => mission != null
        && Controllers.TryGetValue(mission, out var controller) && controller.OwnsAfSceneControl;

    internal static bool BlocksConflict(Mission mission, Agent target)
        => mission != null && Controllers.TryGetValue(mission, out var controller)
            && (controller.OwnsAfSceneControl || controller.IsAfExecutionVictim(target));
}
