using System.Collections.Generic;
using System.Text.Json;
using AnimusForge.Api.Internal;
using AnimusForge.Api.V1;
using AnimusForge.Refactor.Modules;

namespace ModuleFramework.TestControl;

internal static class SnapshotBoundaryChecks
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL snapshot boundary " + name); checks++; }
        IReadOnlyList<AfCapabilityInfo> publicCaps = AfApi.GetSnapshot().PublicCapabilities;
        void Same(string state)
        {
            var before = OriginalFrameworkRuntime.GetSnapshot(publicCaps);
            var actual = AfApi.GetSnapshot();
            Check(JsonSerializer.Serialize(before) == JsonSerializer.Serialize(actual), "old/new complete DTO " + state);
        }
        OriginalFrameworkRuntime.Shutdown(); ModuleFrameworkRuntime.Shutdown();
        Check(OriginalFrameworkRuntime.Initialize(out _) && ModuleFrameworkRuntime.Initialize(out _), "both roots initialized");
        Same("ready");
        foreach (bool disabled in new[]{true,false})
        {
            StubObservations.DisableSiege=disabled; Same("siege gate " + disabled);
        }
        StubObservations.ThrowGate=true; Same("gate throws"); StubObservations.ThrowGate=false;
        ModuleFrameworkSnapshot frozen=ModuleFrameworkRuntime.CaptureSnapshot();
        int calls=StubObservations.GateCalls;
        string rendered=JsonSerializer.Serialize(AfV1SnapshotProjection.Create(frozen,publicCaps));
        Check(StubObservations.GateCalls==calls,"projection never evaluates gate");
        ModuleFrameworkRuntime.Shutdown(); OriginalFrameworkRuntime.Shutdown(); Same("stopped");
        Check(JsonSerializer.Serialize(AfV1SnapshotProjection.Create(frozen,publicCaps))==rendered,"capture detached from shutdown");
        calls=StubObservations.GateCalls; AfApi.GetSnapshot();
        Check(calls==StubObservations.GateCalls,"stopped query does not evaluate gate");
        TeamModuleServices.Policy=null;
        Check(!OriginalFrameworkRuntime.Initialize(out _) && !ModuleFrameworkRuntime.Initialize(out _),"both degraded"); Same("degraded");
        TeamModuleServices.Policy=new object(); OriginalFrameworkRuntime.Shutdown(); ModuleFrameworkRuntime.Shutdown();
        OriginalFrameworkRuntime.Initialize(out _); ModuleFrameworkRuntime.Initialize(out _); Same("reload");
        Check(JsonSerializer.Serialize(AfV1SnapshotProjection.Create(frozen,publicCaps))==rendered,"capture detached from reload");
        var definition=new InternalModuleDefinition("test.module",3,Array.Empty<InternalCapabilityDefinition>());
        var caps=new List<InternalCapabilityStatus>{new InternalCapabilityStatus("test.cap","test.module",2,2,InternalCapabilityState.Available,"test.available")};
        var module=new ModuleBindingSnapshot(definition,caps); caps.Clear();
        var modules=new List<ModuleBindingSnapshot>{module};
        var captured=new ModuleFrameworkSnapshot(ModuleFrameworkLifecycleState.Ready,"test.ready",modules); modules.Clear();
        Check(captured.Modules.Count==1 && captured.Modules[0].Capabilities.Count==1,"constructor copies caller-owned containers");
        try { ((IList<ModuleBindingSnapshot>)captured.Modules).Clear(); Check(false,"mutable modules"); } catch(NotSupportedException) { checks++; }
        try { ((IList<InternalCapabilityStatus>)module.Capabilities)[0]=null; Check(false,"mutable capabilities"); } catch(NotSupportedException) { checks++; }
        foreach (InternalCapabilityState state in Enum.GetValues<InternalCapabilityState>())
        {
            var status=new InternalCapabilityStatus("test.cap","test.module",2,2,state,"test.reason");
            var snap=new ModuleFrameworkSnapshot(ModuleFrameworkLifecycleState.Ready,"test.ready",new[]{new ModuleBindingSnapshot(definition,new[]{status})});
            var mapped=AfV1SnapshotProjection.Create(snap,publicCaps).Modules[0].Capabilities[0];
            AfModuleCapabilityState expected=state switch {
                InternalCapabilityState.Available=>AfModuleCapabilityState.Available,
                InternalCapabilityState.RegistrationOpen or InternalCapabilityState.ModuleNotInitialized=>AfModuleCapabilityState.NotInitialized,
                InternalCapabilityState.UnknownCapability or InternalCapabilityState.Blocked or InternalCapabilityState.VersionMismatch=>AfModuleCapabilityState.InvalidRegistration,
                _=>AfModuleCapabilityState.Unavailable };
            Check(mapped.State==expected && mapped.ContractVersion==2 && mapped.ReasonCode=="test.reason","exhaustive capability map " + state);
        }
        foreach(var state in Enum.GetValues<ModuleFrameworkLifecycleState>())
        {
            var snap=new ModuleFrameworkSnapshot(state,"test.state",Array.Empty<ModuleBindingSnapshot>());
            Check(AfV1SnapshotProjection.Create(snap,publicCaps).State.ToString()==state.ToString(),"explicit state map " + state);
        }
        var future=new ModuleFrameworkSnapshot((ModuleFrameworkLifecycleState)123,"future",Array.Empty<ModuleBindingSnapshot>());
        Check(AfV1SnapshotProjection.Create(future,publicCaps).State==AfFrameworkState.Degraded,"unknown internal lifecycle fails closed");
        Parallel.For(0,128,i=> {
            var snap=ModuleFrameworkRuntime.CaptureSnapshot();
            if(JsonSerializer.Serialize(AfV1SnapshotProjection.Create(snap,publicCaps))!=JsonSerializer.Serialize(AfApi.GetSnapshot()))
                throw new Exception("FAIL snapshot boundary parallel projection");
        }); checks++;
        return checks;
    }
}
