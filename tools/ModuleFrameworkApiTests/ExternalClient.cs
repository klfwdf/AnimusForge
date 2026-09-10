using System.Collections.Generic;
using System.Reflection;
using AnimusForge.Api.V1;
using ModuleFramework.TestControl;

// This is a different assembly. It can inspect only the public V1 contract; host control
// is a separate test fixture used instead of loading Bannerlord or a user's save.
static class Program
{
    private static int checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL " + name);
        checks++;
    }
    private static void Immutable<T>(IReadOnlyList<T> source, string name)
    {
        Check(source is IList<T>, name + " read-only wrapper");
        var list = (IList<T>)source;
        Check(list.IsReadOnly, name + " IsReadOnly");
        try { list.Clear(); throw new Exception("FAIL mutable " + name); }
        catch (NotSupportedException) { checks++; }
        if (list.Count > 0)
        {
            try { list[0] = list[0]; throw new Exception("FAIL item mutable " + name); }
            catch (NotSupportedException) { checks++; }
        }
    }
    private static AfModuleCapabilityInfo Capability(AfFrameworkSnapshot snapshot, string module)
        => snapshot.Modules.Single(item => item.Id == module).Capabilities.Single();

    static void Main()
    {
        Check(HostControl.ServiceInitializations == 0 && HostControl.GateCalls == 0, "host initially untouched");
        AfFrameworkSnapshot before = AfApi.GetSnapshot();
        Check(before.State == AfFrameworkState.NotInitialized, "before load NotInitialized");
        Check(before.ReasonCode == "framework.not_initialized" && before.Modules.Count == 0, "before load empty catalog");
        Check(before.ContractVersion == 1 && AfApi.ContractVersion == 1, "V1 contract identity");
        Check(before.PublicCapabilities.Count == 7, "seven declared public capabilities");
        Check(before.PublicCapabilities.Count(x => x.State == AfCapabilityState.Available) == 1, "only catalog callable");
        Check(AfApi.GetCapability(AfCapabilityIds.CatalogRead).State == AfCapabilityState.Available, "catalog query before load supported");
        string[] unsupported = { AfCapabilityIds.NativeSubmit, AfCapabilityIds.SceneSubmit,
            AfCapabilityIds.CourierSubmit, AfCapabilityIds.ActionExecute, AfCapabilityIds.MemoryWrite,
            AfCapabilityIds.ExtensionRegister };
        foreach (string id in unsupported)
        {
            AfCapabilityInfo capability = AfApi.GetCapability(id);
            Check(capability.Id == id && capability.State == AfCapabilityState.NotSupported
                && capability.ReasonCode == "api.not_supported_in_v1", "explicit unsupported " + id);
        }
        foreach (string id in new[] { null, "", "  ", new string('a', 129) })
        {
            var result = AfApi.GetCapability(id);
            Check(result.Id == "" && result.State == AfCapabilityState.InvalidRequest, "invalid ID fail closed");
        }
        foreach (int version in new[] { -1, 0, 2, int.MaxValue })
            Check(AfApi.GetCapability(AfCapabilityIds.CatalogRead, version).State == AfCapabilityState.VersionMismatch,
                "exact contract version " + version);
        Check(AfApi.GetCapability(" af.api.catalog.read").State == AfCapabilityState.UnknownCapability, "no whitespace alias");
        Check(AfApi.GetCapability("AF.API.CATALOG.READ").State == AfCapabilityState.UnknownCapability, "case sensitive ID");
        Check(AfApi.GetCapability(new string('a', 128)).State == AfCapabilityState.UnknownCapability, "bounded unknown ID");
        Check(HostControl.ServiceInitializations == 0 && HostControl.GateCalls == 0, "public reads do not initialize adapters or gates");
        Immutable(before.Modules, "before-load module list");
        Immutable(before.PublicCapabilities, "public capabilities");

        Check(HostControl.Initialize(), "initialize succeeds");
        Check(HostControl.ServiceInitializations == 1, "services constructed once");
        AfFrameworkSnapshot ready = AfApi.GetSnapshot();
        Check(ready.State == AfFrameworkState.Ready && ready.ReasonCode == "framework.adapters_bound", "ready means adapter binding only");
        Check(ready.Modules.Count == 3, "three composed team modules");
        Check(ready.Modules.Select(x => x.Id).Order().SequenceEqual(new[] { "af.team.gathering", "af.team.policy", "af.team.siege" }), "stable module identities");
        foreach (AfModuleInfo module in ready.Modules)
        {
            Check(module.ContractVersion == 1 && module.Capabilities.Count == 1, "module version and selected seam only");
            Check(module.Capabilities[0].Id == module.Id + ".dialogue", "stable capability identity");
            Check(module.Capabilities[0].State == AfModuleCapabilityState.Available, "bound adapter available");
            Check(!module.Capabilities[0].IsExternallyCallable, "internal port not public executor");
            Immutable(module.Capabilities, "module capabilities");
        }
        Immutable(ready.Modules, "ready module list");
        Check(HostControl.Initialize(), "duplicate load is idempotent");
        Check(AfApi.GetSnapshot().Modules.Count == 3 && HostControl.ServiceInitializations == 1, "no duplicate registrations");
        HostControl.SetGates(true, true);
        AfFrameworkSnapshot gated = AfApi.GetSnapshot();
        Check(Capability(gated, "af.team.siege").State == AfModuleCapabilityState.Unavailable, "disabled siege bridge unavailable");
        Check(Capability(gated, "af.team.policy").State == AfModuleCapabilityState.Available, "policy not globally disabled by diplomacy bridge");
        Check(Capability(gated, "af.team.gathering").State == AfModuleCapabilityState.Available, "unrelated gathering unaffected");
        Check(gated.State == AfFrameworkState.Ready, "bridge gate not framework load failure");
        Check(Capability(ready, "af.team.siege").State == AfModuleCapabilityState.Available, "old snapshot is immutable observation");
        HostControl.SetGates(false, false, true);
        Check(Capability(AfApi.GetSnapshot(), "af.team.siege").State == AfModuleCapabilityState.Unavailable, "gate exception fails closed");
        HostControl.SetGates(false, false);
        Parallel.For(0, 256, _ =>
        {
            AfFrameworkSnapshot snapshot = AfApi.GetSnapshot();
            if (snapshot.State != AfFrameworkState.Ready || snapshot.Modules.Count != 3
                || snapshot.Modules.Any(m => m.Capabilities[0].State != AfModuleCapabilityState.Available))
                throw new Exception("concurrent query corrupted snapshot");
        });
        checks++;

        HostControl.Shutdown(); HostControl.Shutdown();
        AfFrameworkSnapshot stopped = AfApi.GetSnapshot();
        Check(stopped.State == AfFrameworkState.Stopped && stopped.ReasonCode == "framework.stopped", "shutdown idempotent");
        Check(stopped.Modules.Count == 3 && stopped.Modules.All(m => m.Capabilities.All(c =>
            c.State == AfModuleCapabilityState.Unavailable && c.ReasonCode == "framework.stopped")), "stopped retains descriptions not availability");
        int gateCalls = HostControl.GateCalls;
        AfApi.GetSnapshot();
        Check(HostControl.GateCalls == gateCalls, "stopped queries do not evaluate gates");
        Check(before.State == AfFrameworkState.NotInitialized && before.Modules.Count == 0, "old pre-init snapshot unchanged");
        Check(ready.State == AfFrameworkState.Ready, "old ready snapshot unchanged after shutdown");
        Check(HostControl.Initialize(), "reload supported");
        Check(AfApi.GetSnapshot().Modules.Count == 3 && HostControl.ServiceInitializations == 1, "reload uses same stateless adapters");
        Check(stopped.State == AfFrameworkState.Stopped, "old stopped snapshot unchanged on reload");

        HostControl.Shutdown(); HostControl.SetPolicyAdapterPresent(false);
        Check(!HostControl.Initialize(), "missing adapter cannot report ready");
        AfFrameworkSnapshot failed = AfApi.GetSnapshot();
        Check(failed.State == AfFrameworkState.Degraded && failed.ReasonCode == "framework.initialization_failed", "bounded failure reason");
        Check(failed.Modules.Count == 0, "failure clears previous ready catalog");
        HostControl.SetPolicyAdapterPresent(true);
        Check(!HostControl.Initialize(), "same-load failure not silently retried");
        HostControl.Shutdown();
        Check(HostControl.Initialize(), "explicit reload recovers missing adapter");

        AfFrameworkSnapshot detached = HostControl.CreateDetachedSnapshotThenMutateSources();
        Check(detached.PublicCapabilities.Count == 1 && detached.Modules.Count == 1
            && detached.Modules[0].Capabilities.Count == 1, "DTO constructors defensively copy source lists");
        Type[] dtoTypes = { typeof(AfCapabilityInfo), typeof(AfModuleCapabilityInfo), typeof(AfModuleInfo), typeof(AfFrameworkSnapshot) };
        foreach (Type type in dtoTypes)
        {
            Check(type.IsSealed && type.GetConstructors().Length == 0, "DTO sealed with no public constructors " + type.Name);
            Check(type.GetProperties().All(p => p.GetSetMethod(true) == null), "DTO no setters " + type.Name);
            Check(type.GetFields(BindingFlags.Public | BindingFlags.Instance).Length == 0, "DTO no mutable public fields " + type.Name);
            foreach (PropertyInfo property in type.GetProperties())
            {
                Type t = property.PropertyType;
                Check(t.Assembly == typeof(int).Assembly || t.Namespace == "AnimusForge.Api.V1"
                    || t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)
                        && t.GetGenericArguments()[0].Namespace == "AnimusForge.Api.V1", "public DTO excludes game/internal types");
            }
        }
        string[] apiMethods = typeof(AfApi).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(m => m.Name).Order().ToArray();
        Check(apiMethods.SequenceEqual(new[] { "GetCapability", "GetSnapshot" }), "no undeclared public execution path");
        Check(typeof(AfApi).Assembly != typeof(Program).Assembly, "external client is a separate assembly");
        Console.WriteLine($"PASS {checks} public API assertions; 256 concurrent reads; actual source-linked V1 contracts/runtime.");
        Console.WriteLine("NOT TESTED: Bannerlord host, live saves, economy, gameplay ports, request submission, external DLL load order.");
    }
}
