using System.Runtime.CompilerServices;
using System.Threading;
using AnimusForge.Refactor.Contracts;

// Only the test control assembly has friend access. Production gains no friend assembly.
[assembly: InternalsVisibleTo("ModuleFrameworkControl")]

namespace AnimusForge.Refactor.Modules
{
    internal static class StubObservations
    {
        internal static int ServiceInitializations;
        internal static int GateCalls;
        internal static bool DisableSiege;
        internal static bool DisablePolicyWorld;
        internal static bool ThrowGate;
    }

    // This suite tests public DTO/runtime composition, not gameplay ports or Bannerlord.
    internal static class TeamModuleServices
    {
        static TeamModuleServices() { Interlocked.Increment(ref StubObservations.ServiceInitializations); }
        internal static object Policy { get; set; } = new object();
        internal static object Gathering { get; set; } = new object();
        internal static object Siege { get; set; } = new object();
    }
}

namespace AnimusForge.Refactor.Runtime
{
    internal static class FeatureBridgeRuntime
    {
        internal static FeatureBridgeDecision Evaluate(string id, int version)
        {
            Interlocked.Increment(ref Modules.StubObservations.GateCalls);
            if (Modules.StubObservations.ThrowGate) throw new InvalidOperationException("host-only gate failure");
            bool disabled = id == FeatureBridgeIds.ConversationSiege && Modules.StubObservations.DisableSiege
                || id == FeatureBridgeIds.PolicyWorldDiplomacy && Modules.StubObservations.DisablePolicyWorld;
            return new FeatureBridgeDecision(id,
                disabled ? FeatureBridgeDecisionStatus.Disabled : FeatureBridgeDecisionStatus.Allowed,
                FeatureBridgeFallback.NoOp, disabled ? "feature_bridge.disabled:" + id : "feature_bridge.allowed");
        }
    }
}
