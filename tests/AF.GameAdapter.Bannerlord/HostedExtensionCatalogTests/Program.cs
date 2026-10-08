using System;
using System.Linq;
using System.Threading.Tasks;
using AnimusForge;
using AnimusForge.Refactor.Modules;
using AnimusForge.Api.Internal;
using AnimusForge.Api.V1;
using RichExecutions.Core;
using TaleWorlds.CampaignSystem;
internal static class Program
{
    private static int checks;
    private static bool deferCoup;
    private static void Check(bool ok,string label) { if(!ok)throw new Exception("FAIL "+label);checks++; }
    private static AfFrameworkSnapshot Snapshot() => AfV1SnapshotProjection.Create(ModuleFrameworkRuntime.CaptureSnapshot(),Array.Empty<AfCapabilityInfo>());
    private static AfModuleCapabilityInfo Cap(string id) => Snapshot().Modules.Single(m=>m.Id==id).Capabilities.Single();
    private static void State(string id,AfModuleCapabilityState state,string label) => Check(Cap(id).State==state,label);
    private static void Fresh()
    {
        TestLeaves.Failure=null;IntegratedModuleHost.Shutdown();VengeanceRuntimeBridge.Shutdown();ModuleFrameworkRuntime.Shutdown();
        TestLeaves.UiResources=true;VengeanceIntegration.Enabled=true;AnimusForge.Refactor.Runtime.FeatureBridgeRuntime.Disabled=false;
        AnimusForge.CoupSystem.CoupGuards.MissionProtectionAvailable=true;AnimusForge.CoupSystem.SettlementEntryTroopSelectionBehavior.IsAvailable=true;AnimusForge.CoupSystem.CoupRebellionBridge.IsAvailable=true;
        Check(ModuleFrameworkRuntime.Initialize(out _),"framework initialized");TestLeaves.Calls.Clear();
    }
    private static void StartAll(){VengeanceRuntimeBridge.Initialize();IntegratedModuleHost.Start();IntegratedModuleHost.InstallDialoguePresentation();}
    private static void CheckCoup(AfModuleCapabilityState state,string name){if(!deferCoup)State(HostedExtensionCatalog.Coup,state,name);}
    public static int Main(string[] args)
    {
        deferCoup=args.Contains("--defer-coup");
        string[] ids=deferCoup?new[]{HostedExtensionCatalog.Illustrator,HostedExtensionCatalog.DialogueUi,HostedExtensionCatalog.Vengeance}
            :new[]{HostedExtensionCatalog.Illustrator,HostedExtensionCatalog.DialogueUi,HostedExtensionCatalog.Coup,HostedExtensionCatalog.Vengeance};
        Check(!ModuleFrameworkRuntime.ReportHostedExtensionState(HostedExtensionCatalog.Illustrator,InternalModuleRuntimeState.Ready,"before.init"),"cannot report before init");
        Fresh();Check(Snapshot().Modules.Count==4+ids.Length
            && Snapshot().Modules.Select(m=>m.Id).OrderBy(id=>id,StringComparer.Ordinal).SequenceEqual(
                new[]{"af.team.policy","af.team.gathering","af.team.siege","af.team.diplomacy"}.Concat(ids).OrderBy(id=>id,StringComparer.Ordinal))
            && Snapshot().Modules.Single(m=>m.Id=="af.team.diplomacy").Capabilities.Select(c=>c.Id).OrderBy(id=>id,StringComparer.Ordinal).SequenceEqual(
                new[]{"af.team.diplomacy.dialogue","af.team.diplomacy.policy","af.team.diplomacy.world"}),
            "four existing ports with exact Diplomacy capabilities plus hosted catalog");
        foreach(string id in ids){State(id,AfModuleCapabilityState.NotInitialized,"not automatically ready: "+id);Check(Cap(id).Id==id+".host"&&!Cap(id).IsExternallyCallable,"metadata is not an executor: "+id);}
        Check(!ModuleFrameworkRuntime.ReportHostedExtensionState("af.team.policy",InternalModuleRuntimeState.Failed,"bad"),"host cannot alter original port");
        Check(!ModuleFrameworkRuntime.ReportHostedExtensionState("unknown",InternalModuleRuntimeState.Ready,"bad"),"unknown cannot be registered by event");
        var before=Snapshot();VengeanceRuntimeBridge.Initialize();IntegratedModuleHost.Start();
        State(HostedExtensionCatalog.Illustrator,AfModuleCapabilityState.Available,"illustrator actual start");
        State(HostedExtensionCatalog.DialogueUi,AfModuleCapabilityState.NotInitialized,"UI waits for installation");
        State(HostedExtensionCatalog.Vengeance,AfModuleCapabilityState.Available,"vengeance actual claim");CheckCoup(AfModuleCapabilityState.Available,"coup actual start");
        Check(Cap(HostedExtensionCatalog.DialogueUi).ReasonCode=="module.awaiting_presentation","deferred UI reason");
        Check(before.Modules.Where(m=>ids.Contains(m.Id)).All(m=>m.Capabilities[0].State==AfModuleCapabilityState.NotInitialized),"old snapshot immutable");
        IntegratedModuleHost.InstallDialoguePresentation();State(HostedExtensionCatalog.DialogueUi,AfModuleCapabilityState.Available,"presentation installed");
        IntegratedModuleHost.InstallDialoguePresentation();Check(TestLeaves.Calls.Count(c=>c=="ui.presentation")==1,"presentation duplicate is idempotent");
        VengeanceRuntimeBridge.Initialize();Check(TestLeaves.Calls.Count(c=>c=="vengeance.address")==1,"vengeance duplicate does not subscribe twice");
        State(HostedExtensionCatalog.Vengeance,AfModuleCapabilityState.Available,"duplicate claim preserves readiness");
        VengeanceRuntimeBridge.RegisterCampaign(new CampaignGameStarter());IntegratedModuleHost.RegisterCampaign(new CampaignGameStarter());
        Check(Cap(HostedExtensionCatalog.Illustrator).ReasonCode=="capability.available","real campaign result");
        Check(Cap(HostedExtensionCatalog.Vengeance).ReasonCode=="capability.available","real vengeance campaign result");
        Check(TestLeaves.Calls.Count(c=>c=="register.IllustratorCampaignBehavior")==1,"single illustrator registration");
        Check(TestLeaves.Calls.Count(c=>c=="register.RichExecutionCampaignBehavior")==1,"single execution registration");
        if(!deferCoup)Check(Cap(HostedExtensionCatalog.Coup).ReasonCode=="capability.available","real coup campaign result");
        AnimusForge.Refactor.Runtime.FeatureBridgeRuntime.Disabled=true;
        State("af.team.siege",AfModuleCapabilityState.Unavailable,"existing siege gate retained");
        foreach(string id in ids)State(id,AfModuleCapabilityState.Available,"unrelated gate does not suppress extension: "+id);
        AnimusForge.Refactor.Runtime.FeatureBridgeRuntime.Disabled=false;
        Parallel.For(0,128,_=>{var s=Snapshot();if(s.Modules.Count!=4+ids.Length||s.Modules.Any(m=>m.Capabilities[0].State!=AfModuleCapabilityState.Available))throw new Exception("parallel snapshot");});checks++;
        IntegratedModuleHost.Shutdown();VengeanceRuntimeBridge.Shutdown();
        foreach(string id in ids)State(id,AfModuleCapabilityState.Unavailable,"host stop: "+id);
        var stoppedHost=Snapshot();ModuleFrameworkRuntime.Shutdown();
        Check(!ModuleFrameworkRuntime.ReportHostedExtensionState(HostedExtensionCatalog.Illustrator,InternalModuleRuntimeState.Ready,"late"),"late report cannot resurrect stopped directory");
        Check(Snapshot().State==AfFrameworkState.Stopped&&Snapshot().Modules.All(m=>m.Capabilities[0].State==AfModuleCapabilityState.Unavailable),"all unavailable after framework stop");
        Check(stoppedHost.State==AfFrameworkState.Ready,"captured state detached from shutdown");
        Fresh();TestLeaves.Failure="illustrator.start";StartAll();
        State(HostedExtensionCatalog.Illustrator,AfModuleCapabilityState.Unavailable,"swallowed illustrator init failure is visible");
        State(HostedExtensionCatalog.DialogueUi,AfModuleCapabilityState.Available,"one failure does not stop UI");
        State(HostedExtensionCatalog.Vengeance,AfModuleCapabilityState.Available,"one failure does not stop execution");
        TestLeaves.Failure=null;IntegratedModuleHost.RegisterCampaign(new CampaignGameStarter());
        State(HostedExtensionCatalog.Illustrator,AfModuleCapabilityState.Unavailable,"campaign callback cannot hide startup failure");
        Fresh();TestLeaves.Failure="illustrator.patch";StartAll();State(HostedExtensionCatalog.Illustrator,AfModuleCapabilityState.Unavailable,"patch exception cannot publish ready");
        Fresh();TestLeaves.UiResources=false;StartAll();State(HostedExtensionCatalog.DialogueUi,AfModuleCapabilityState.Unavailable,"missing UI resources");
        State(HostedExtensionCatalog.Illustrator,AfModuleCapabilityState.Available,"UI fallback leaves illustrator");
        Fresh();TestLeaves.Failure="ui.presentation";StartAll();State(HostedExtensionCatalog.DialogueUi,AfModuleCapabilityState.Unavailable,"caught presentation error not false ready");
        Check(!Cap(HostedExtensionCatalog.DialogueUi).ReasonCode.Contains("secret"),"no exception detail in public status");
        Fresh();TestLeaves.Failure="ui.wheel";StartAll();State(HostedExtensionCatalog.DialogueUi,AfModuleCapabilityState.Available,"optional wheel fallback preserves installed core UI");
        Fresh();VengeanceIntegration.Enabled=false;StartAll();State(HostedExtensionCatalog.Vengeance,AfModuleCapabilityState.Unavailable,"disabled execution claim not ready");
        VengeanceRuntimeBridge.RegisterCampaign(new CampaignGameStarter());State(HostedExtensionCatalog.Vengeance,AfModuleCapabilityState.Unavailable,"inactive campaign no-op");
        Fresh();TestLeaves.Failure="vengeance.address";bool threw=false;try{VengeanceRuntimeBridge.Initialize();}catch(InvalidOperationException){threw=true;}
        Check(threw&&!VengeanceIntegration.IsEmbeddedHostActive,"claim cleanup and original exception preserved");State(HostedExtensionCatalog.Vengeance,AfModuleCapabilityState.Unavailable,"execution initialization failed");
        Fresh();StartAll();TestLeaves.Failure="vengeance.presets";VengeanceRuntimeBridge.RegisterCampaign(new CampaignGameStarter());
        State(HostedExtensionCatalog.Vengeance,AfModuleCapabilityState.Unavailable,"execution setup failure visible");
        Check(!TestLeaves.Calls.Contains("register.RichExecutionCampaignBehavior"),"setup failed before adding behavior");
        TestLeaves.Failure=null;VengeanceRuntimeBridge.RegisterCampaign(new CampaignGameStarter());State(HostedExtensionCatalog.Vengeance,AfModuleCapabilityState.Available,"later real campaign registration recovers");
        Fresh();StartAll();TestLeaves.Failure="register.RichExecutionCampaignBehavior";VengeanceRuntimeBridge.RegisterCampaign(new CampaignGameStarter());
        State(HostedExtensionCatalog.Vengeance,AfModuleCapabilityState.Unavailable,"execution registration failure visible");
        State(HostedExtensionCatalog.Illustrator,AfModuleCapabilityState.Available,"execution registration failure isolated");
        Fresh();StartAll();TestLeaves.Failure="register.IllustratorCampaignBehavior";IntegratedModuleHost.RegisterCampaign(new CampaignGameStarter());
        State(HostedExtensionCatalog.Illustrator,AfModuleCapabilityState.Unavailable,"illustrator registration failure visible");
        Check(TestLeaves.Calls.Contains("register.CoupCampaignBehavior"),"other registration continues after failure");
        TestLeaves.Failure=null;IntegratedModuleHost.RegisterCampaign(new CampaignGameStarter());State(HostedExtensionCatalog.Illustrator,AfModuleCapabilityState.Available,"real subsequent campaign can recover");
        Fresh();StartAll();TestLeaves.Failure="ui.shutdown";IntegratedModuleHost.Shutdown();
        foreach(string id in ids.Where(x=>x!=HostedExtensionCatalog.Vengeance))State(id,AfModuleCapabilityState.Unavailable,"shutdown continues after UI error: "+id);
        if(!deferCoup)
        {
            foreach(Action breakProbe in new Action[] {
                ()=>AnimusForge.CoupSystem.CoupGuards.MissionProtectionAvailable=false,
                ()=>AnimusForge.CoupSystem.SettlementEntryTroopSelectionBehavior.IsAvailable=false,
                ()=>AnimusForge.CoupSystem.CoupRebellionBridge.IsAvailable=false })
            {
                Fresh();breakProbe();StartAll();CheckCoup(AfModuleCapabilityState.Unavailable,"missing required coup compatibility probe");
            }
            Fresh();TestLeaves.Failure="coup.entry";StartAll();CheckCoup(AfModuleCapabilityState.Unavailable,"coup start failure not ready");
            State(HostedExtensionCatalog.Illustrator,AfModuleCapabilityState.Available,"coup failure isolated");
            TestLeaves.Failure=null;IntegratedModuleHost.RegisterCampaign(new CampaignGameStarter());CheckCoup(AfModuleCapabilityState.Unavailable,"coup campaign cannot conceal start failure");
            Fresh();StartAll();TestLeaves.Failure="register.CoupCampaignBehavior";IntegratedModuleHost.RegisterCampaign(new CampaignGameStarter());CheckCoup(AfModuleCapabilityState.Unavailable,"coup campaign failure visible");
            TestLeaves.Failure=null;IntegratedModuleHost.RegisterCampaign(new CampaignGameStarter());CheckCoup(AfModuleCapabilityState.Available,"coup campaign can recover");
        }
        Fresh();Check(ModuleFrameworkRuntime.Initialize(out _),"repeat init idempotent");Check(Snapshot().Modules.Count==4+ids.Length,"no duplicate directory entries");
        foreach(string id in ids)State(id,AfModuleCapabilityState.NotInitialized,"reload needs actual startup: "+id);
        Console.WriteLine($"PASS {checks} hosted-extension lifecycle checks; fake engine/patch/resource leaves; live NOT_RUN");return 0;
    }
}
