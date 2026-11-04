using AnimusForge;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
static class Program
{
    static int checks;static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
    static Mission Use(Scene scene){var m=new Mission{Scene=scene};Mission.Current=m;Logger.Lines.Clear();return m;}
    static bool Rejected(Action action){try{action();return false;}catch(InvalidOperationException){return true;}}
    static void Main()
    {
        try { Run(); }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.GetType().FullName);
            Console.Error.WriteLine(error.Message);
            Console.Error.WriteLine(error.StackTrace);
            Environment.ExitCode = 1;
        }
    }
    static void Run()
    {
        var layout=new FieldCampLayout();var scene=new Scene{Blocked=p=>Math.Abs(p.x)<1.1f&&p.y>17&&p.y<19};var m=Use(scene);
        FieldCampSite.InitializeGroundCache(scene,MatrixFrame.Identity,layout,1);
        Check(!FieldCampSite.TryValidateGroundedPassage(scene,new Vec3(0,12,0),new Vec3(0,30,0),.45f),"reproduce blocked straight entrance");
        Check(FieldCampSite.TryResolveGroundedPosition(scene,new Vec3(10,5,0),.35f,out _),"straight gate blockage keeps actor cache ready");
        Check(FieldCampSite.TryBuildGroundedRoute(scene,new Vec3(0,12,0),new Vec3(0,30,0),out var route,out bool covered)&&covered&&route.Length>2,"native terrain route detours around central fire obstruction");
        Check(!FieldCampSite.TryResolveGroundedPosition(scene,new Vec3(0,18,0),.35f,out _),"blocked exact slot remains forbidden");
        Check(FieldCampSite.TryFindNearbyGroundedPosition(scene,new Vec3(0,18,0),.35f,2,out var spawn)&&spawn.AsVec2.Distance(new Vec2(0,18))<=2.01f,"nearby spawn retains body clearance and bounded distance");
        Check(!FieldCampSite.TryFindNearbyGroundedPosition(scene,new Vec3(0,18,0),.35f,0,out _),"zero search cannot ignore collisions");
        Check(!FieldCampSite.TryFindNearbyGroundedPosition(scene,new Vec3(0,18,0),.35f,float.NaN,out _),"invalid search budget rejected");
        // An inner-margin start rounds onto an outside cache cell. Old code disabled all bounds.
        scene=new Scene();m=Use(scene);
        FieldCampSite.InitializeGroundCache(scene,MatrixFrame.Identity,layout,1);
        Check(FieldCampSite.TryBuildGroundedRoute(scene,new Vec3(35.9f,5,0),new Vec3(25,5,0),out route,out covered,stayInsideCamp:true),"margin-drift start can take a short connector back inside");
        Check(route.All(p=>Math.Abs(p.x)<layout.HalfX-.8f && p.y>layout.Back+.8f && p.y<layout.Front-.8f),"all issued resident route goals stay inside the fence margin");
        Vec3 previous=new Vec3(35.9f,5,0);
        foreach(var point in route){Check(previous.AsVec2.Distance(point.AsVec2)<=1.51f,"native route legs cannot choose a distant outside detour");previous=point;}
        Check(!FieldCampSite.TryBuildGroundedRoute(scene,new Vec3(10,5,0),new Vec3(36.2f,5,0),out _,out _,stayInsideCamp:true),"inside resident cannot request an outside-margin goal");
        layout.Props.Add(new FieldCampLayout.Prop{Kind=FieldCampLayout.PropKind.Fire,Position=new Vec2(10,5)});
        FieldCampSite.InitializeGroundCache(scene,MatrixFrame.Identity,layout,1);
        Check(!FieldCampSite.TryResolveGroundedPosition(scene,new Vec3(10,5,0),.4f,out _),"walk-through fire visual is still forbidden for spawn/seat");
        Check(!FieldCampSite.TryValidateGroundedPassage(scene,new Vec3(7,5,0),new Vec3(13,5,0),.15f),"direct walking through fire core is blocked");
        Check(FieldCampSite.TryResolveGroundedPosition(scene,new Vec3(12.25f,5,0),.4f,out _),"rest ring outside fire disc retains actor clearance");
        Check(FieldCampSite.TryBuildGroundedRoute(scene,new Vec3(7,5,0),new Vec3(13,5,0),out route,out _,stayInsideCamp:true),"resident may route around fire rather than losing the whole connected camp");
        Check(route.All(p=>p.AsVec2.Distance(new Vec2(10,5))>1.44f),"every short route waypoint stays clear of flames");
        layout.Props.Clear();
        scene=new Scene{Blocked=p=>p.y>21&&p.y<24};m=Use(scene);
        FieldCampSite.InitializeGroundCache(scene,MatrixFrame.Identity,layout,1);
        Check(FieldCampSite.TryResolveGroundedPosition(scene,new Vec3(10,5,0),.35f,out _),"disconnected gate does not suppress independent safe residents");
        Check(Logger.Lines.Any(s=>s.Contains("gate route unavailable")),"unavailable route is explicitly diagnosed");
        FieldCampSite.ReleaseCaches(scene);Check(!FieldCampSite.TryResolveGroundedPosition(scene,new Vec3(10,5,0),.35f,out _),"mission cleanup invalidates ground queries");
        scene=new Scene{Navigable=_=>false};m=Use(scene);
        Check(Rejected(()=>FieldCampSite.InitializeGroundCache(scene,MatrixFrame.Identity,layout,1)),"fully nonnavigable ground never authorizes actor spawning");
        scene=new Scene{Terrain=p=>p.x*.07f};m=Use(scene);
        var frame=FieldCampSite.Resolve(m,layout,out float scale);
        Check(frame.origin.IsValid&&scale==1,"ordinary seven percent field slope accepted across actual camp footprint");
        scene=new Scene{Terrain=p=>p.x*1f};m=Use(scene);
        frame=FieldCampSite.Resolve(m,layout,out _,out bool rough);
        Check(frame.origin.IsValid&&rough&&Logger.Lines.Any(s=>s.Contains("approximate; continuing generation")),"rough site is explicitly approximate, not falsely approved terrain");
        scene=new Scene{Water=1};m=Use(scene);
        frame=FieldCampSite.Resolve(m,layout,out _,out rough);
        Check(rough&&!FieldCampSite.TryFindPlayerStart(m,frame.origin,out _),"approximate underwater site cannot authorize an unsafe player spawn");
        Check(Rejected(()=>FieldCampSite.InitializeGroundCache(scene,MatrixFrame.Identity,layout,1)),"underwater cache cannot authorize resident spawning");
        scene=new Scene{Navigable=p=>p.x>160&&p.y>160};m=Use(scene);
        frame=FieldCampSite.Resolve(m,layout,out _);
        Check(frame.origin.x>160&&frame.origin.y>160,"distant valid scene corner survives flat unusable finalists");
        Check(scene.NavRejects>64,"selection actually searches beyond first 64 fine rejections");
        scene=new Scene();m=Use(scene);
        var tree=new GameEntity{Name="tree_oak",Pointer=(UIntPtr)1,GlobalBoxMin=new Vec3(-2,-2,0),GlobalBoxMax=new Vec3(2,2,23)};
        var rock=new GameEntity{Name="rock_cliff",Pointer=(UIntPtr)2,GlobalBoxMin=new Vec3(-2,-2,0),GlobalBoxMax=new Vec3(2,2,23)};
        scene.Entities.Add(tree);scene.Entities.Add(rock);frame=FieldCampSite.Resolve(m,layout,out _);
        FieldCampSite.ClearClutter(scene,MatrixFrame.Identity,layout,1);
        Check(tree.Hidden&&tree.Disabled&&!rock.Hidden,"tall tree can clear without disabling protected cliff");
        var handler=new FieldCampAgentHandler{Mission=m};NativeCalls.Events.Clear();
        handler.EarlyStart();handler.OnMissionTick(31);handler.OnMissionModeChange(MissionMode.Battle,false);
        Check(NativeCalls.Events.SequenceEqual(new[]{"stuck_report"}),"camp handler avoids native settlement weather passage and static face state");
        NativeCalls.Events.Clear();var character=new TaleWorlds.CampaignSystem.CharacterObject();var party=new TaleWorlds.CampaignSystem.Party.PartyBase();
        var agent=handler.SpawnResident(character,party,new Team(),new Vec3(3,4,5),new Vec2(0,1),true);
        Check(NativeCalls.Events.SequenceEqual(new[]{"spawn","action_set","navigator","behaviors","after","event"}),"resident native lifecycle ordering includes action set navigator callbacks and event");
        Check(m.LastBuild.Position.z==5&&m.LastBuild.Civilian,"native lifecycle retains validated terrain height and civilian equipment");
        Check(m.LastBuild.Data.Origin is TaleWorlds.CampaignSystem.AgentOrigins.PartyAgentOrigin po&&po.Immortal,"display-only soldier origin cannot write campaign casualties");
        var origin=new CaptiveOrigin();handler.SpawnResident(character,party,new Team(),new Vec3(3,4,5),new Vec2(0,1),true,origin);
        Check(ReferenceEquals(m.LastBuild.Data.Origin,origin),"mortal captive origin remains the authoritative casualty owner");
        var spawned=(SandBox.Missions.MissionLogics.LocationCharacterAgentSpawnedMissionEvent)TaleWorlds.Core.Game.Current.EventManager.Last;
        Check(ReferenceEquals(spawned.Resident.AgentData.Origin,origin),"native spawn event carries actual captive resident and origin");
        Console.WriteLine($"PASS {checks} linked production site/cache/selection/lifecycle assertions; fake engine, live game NOT RUN");
    }
    sealed class CaptiveOrigin:TaleWorlds.Core.IAgentOriginBase{}
}
