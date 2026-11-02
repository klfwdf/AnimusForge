using AnimusForge;
using RichExecutions.Scene;
static class Program
{
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
    static bool Terrain(Func<int,int,float> height)
    {
        var profile=new FieldCampSurfaceProfile();var heights=new float[81];
        for(int x=0;x<9;x++)for(int y=0;y<9;y++)
        {int i=x*9+y;heights[i]=height(x,y);if(!profile.Add(heights[i],x==0?float.NaN:heights[i-9],y==0?float.NaN:heights[i-1],5f,5f))return false;}
        return true;
    }
    static void Main()
    {
        Check(Terrain((x,y)=>0f),"whole flat footprint accepted");
        Check(Terrain((x,y)=>x*0.15f+y*0.1f),"gentle whole footprint accepted");
        Check(!Terrain((x,y)=>x*1.0f),"hillside grade rejected even with a safe centre");
        Check(Terrain((x,y)=>x*0.4f),"gentle field across the whole camp is not rejected by the old global spread cap");
        Check(!Terrain((x,y)=>x==4&&y==4?2f:0f),"interior mound rejected");
        Check(!Terrain((x,y)=>float.NaN),"NaN never classified as flat");
        Check(!Terrain((x,y)=>float.PositiveInfinity),"infinite terrain rejected");
        Check(FieldCampPlacementRules.TryTentScale(5f,6f,1f,3f,4f,out float scale)&&Math.Abs(scale-0.6f)<0.001f,"real tent fits parcel by actual scale");
        Check(!FieldCampPlacementRules.TryTentScale(20f,30f,1f,3f,4f,out _),"unusable miniature tent rejected");
        Check(!FieldCampPlacementRules.TryTentScale(float.NaN,3f,1f,3f,4f,out _),"invalid prefab size rejected");
        Check(!FieldCampPlacementRules.IsPhysicallyClear(9.3f*9.3f,9f),"actor body overlapping footprint is not waived");
        Check(FieldCampPlacementRules.IsPhysicallyClear(9.7f*9.7f,9f),"actually cleared actor may hold outside");
        Check(!FieldCampPlacementRules.IsPhysicallyClear(float.NaN,9f),"unknown location cannot grant readiness");
        var host=new TownExecutionMissionBehavior();var mission=host.Mission;host.RunRetire();
        Check(mission.Events.SequenceEqual(new[]{"finalize","death","host"}),"cache mission and remove controller last");
        Check(host.Mission==null&&!host.Retry&&mission.EndCalls==0,"native owner invalidation never closes camp");
        host=new(){ThrowCleanup=true};mission=host.Mission;
        for(int i=0;i<4;i++)host.RunRetire();
        Check(!host.Retry&&mission.EndCalls==0&&mission.Events.Count(x=>x=="finalize")==3,"bounded cleanup failure never ends camp");
        host=new(){CleanupReady=false};mission=host.Mission;host.RunRetire();
        Check(host.Retry&&!mission.Events.Contains("host")&&mission.EndCalls==0,"incomplete cleanup retains owner and retries");
        host.CleanupReady=true;host.RunRetire();Check(mission.Events.Contains("host"),"transient cleanup can succeed next tick");
        host=new(){PendingCommit=true};mission=host.Mission;host.RunRetire();
        Check(mission.Events.Count==0&&host.Mission==mission&&mission.EndCalls==0,"pending lethal settlement keeps mission-exit owner without early commit or exit");
        Console.WriteLine($"PASS {checks} production camp geometry/cancellation assertions; fake native lifecycle, live game NOT RUN");
    }
}
namespace RichExecutions.Diagnostics
{
    static class RexLog{internal static void Warning(string s){}internal static void Error(string s,Exception e){}}
}
namespace RichExecutions.Scene
{
    public sealed class FakeMission
    {
        internal List<string> Events=new();internal int EndCalls;internal TownExecutionMissionBehavior Host;
        internal void RemoveMissionBehavior(object owner){if(owner==Host){Events.Add("host");Host.Mission=null;}else Events.Add("death");}
        internal void EndMission()=>EndCalls++;
    }
    public sealed partial class TownExecutionMissionBehavior
    {
        internal FakeMission Mission;object _deathLogic=new();int _fieldCampPreparationCleanupAttempts;
        bool _cleanupComplete,_endMissionNextTick,_campaignCommitPending;
        internal bool ThrowCleanup,CleanupReady=true;internal bool Retry=>_endMissionNextTick;
        internal bool PendingCommit{set=>_campaignCommitPending=value;}
        public TownExecutionMissionBehavior(){Mission=new(){Host=this};}
        void FinalizeSession(){Mission.Events.Add("finalize");if(ThrowCleanup)throw new InvalidOperationException();_cleanupComplete=CleanupReady;}
        internal void RunRetire(){_endMissionNextTick=false;RetireCancelledCampExecution();}
    }
}
