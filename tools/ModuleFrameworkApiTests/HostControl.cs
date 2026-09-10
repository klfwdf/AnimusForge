using AnimusForge.Api.V1;
using AnimusForge.Refactor.Modules;

namespace ModuleFramework.TestControl;

// Test-only host operations; a real sub-MOD only uses AnimusForge.Api.V1.
public static class HostControl
{
    public static int ServiceInitializations => StubObservations.ServiceInitializations;
    public static int GateCalls => StubObservations.GateCalls;
    public static bool Initialize() => ModuleFrameworkRuntime.Initialize(out _);
    public static void Shutdown() => ModuleFrameworkRuntime.Shutdown();
    public static void SetGates(bool siegeDisabled, bool policyWorldDisabled, bool throwGate = false)
    {
        StubObservations.DisableSiege = siegeDisabled;
        StubObservations.DisablePolicyWorld = policyWorldDisabled;
        StubObservations.ThrowGate = throwGate;
    }
    public static void SetPolicyAdapterPresent(bool present)
        => TeamModuleServices.Policy = present ? new object() : null;

    public static AfFrameworkSnapshot CreateDetachedSnapshotThenMutateSources()
    {
        var publicCaps = new List<AfCapabilityInfo> { AfApi.GetCapability(AfCapabilityIds.CatalogRead) };
        var internalCaps = new List<AfModuleCapabilityInfo>
        {
            new AfModuleCapabilityInfo("test.dialogue", 1, AfModuleCapabilityState.Available, "test.bound")
        };
        var modules = new List<AfModuleInfo> { new AfModuleInfo("test.module", 1, internalCaps) };
        var result = new AfFrameworkSnapshot(AfFrameworkState.Ready, "test.snapshot", publicCaps, modules);
        publicCaps.Clear(); internalCaps.Clear(); modules.Clear();
        return result;
    }
}
