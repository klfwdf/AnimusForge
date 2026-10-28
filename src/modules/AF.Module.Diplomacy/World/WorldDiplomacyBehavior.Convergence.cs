using TaleWorlds.Core;

namespace AnimusForge;

public sealed partial class WorldDiplomacyBehavior
{
    private sealed partial class OrchestrationHost : IDiplomacyConvergenceHost
    {
        public int WarTendency() => GetWarTendency();
        public int DrawEndingRoute(int exclusiveMaximum) => MBRandom.RandomInt(exclusiveMaximum);
    }
}
