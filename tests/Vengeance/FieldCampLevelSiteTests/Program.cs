using AnimusForge;
using RichExecutions.Scene;
static class Program
{
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    static void Main()
    {
        int probes=0;
        FieldCampSiteSample sample=(float x,float y,bool detail,out float z)=>{probes++;z=x<100f?0.2f*x+0.1f*y:0f;return true;};
        var seeds=new[]{new FieldCampSiteSeed(0f,0f,0f,1f),new FieldCampSiteSeed(200f,0f,0f,1f)};
        Check(FieldCampSitePlanner.TryChoose(seeds,20f,-20f,20f,sample,_=>true,out var chosen,out var spread)&&chosen.X==200f,"reject nearest safe player hillside; choose a distant flat entire footprint");
        Check(spread<=1.5f&&probes<300,"whole-footprint search uses bounded coarse/fine probes");
        sample=(float x,float y,bool detail,out float z)=>{z=0.2f*x+0.1f*y;return true;};
        Check(!FieldCampSitePlanner.TryChoose(seeds[..1],20f,-20f,20f,sample,_=>true,out _,out _),"all-hillside scene fails explicitly instead of forcing a camp around its safe centre");
        sample=(float x,float y,bool detail,out float z)=>{z=(Math.Abs(x)>10f)?10f:0f;return true;};
        Check(FieldCampSitePlanner.TryChoose(new[]{new FieldCampSiteSeed(0f,0f,1f,0f)},10f,-20f,20f,sample,_=>true,out chosen,out _)&&Math.Abs(chosen.ForwardY)>0.9f,"second orientation fits a long flat corridor that the first camp orientation could not fit");
        sample=(float x,float y,bool detail,out float z)=>{z=(Math.Abs(x-5f)<0.01f&&Math.Abs(y)<0.01f)?3f:0f;return true;};
        Check(!FieldCampSitePlanner.TryChoose(seeds[..1],20f,-20f,20f,sample,_=>true,out _,out _),"fine footprint rejects a mound hidden between coarse samples");
        sample=(float x,float y,bool detail,out float z)=>{z=0f;return Math.Abs(x)<18f;};
        Check(!FieldCampSitePlanner.TryChoose(seeds[..1],20f,-20f,20f,sample,_=>true,out _,out _),"dry centre cannot override water/out-of-boundary footprint samples");
        sample=(float x,float y,bool detail,out float z)=>{z=float.NaN;return true;};
        Check(!FieldCampSitePlanner.TryChoose(seeds[..1],20f,-20f,20f,sample,_=>true,out _,out _),"non-finite terrain cannot be ranked as flat");
        sample=(float x,float y,bool detail,out float z)=>{z=0f;return true;};
        Check(!FieldCampSitePlanner.TryChoose(seeds[..1],20f,-20f,20f,sample,_=>false,out _,out _),"flat ground still requires real critical-area approval");
        Check(!FieldCampSitePlanner.TryChoose(seeds[..1],-1f,0f,0f,sample,_=>true,out _,out _),"invalid dimensions never create a site");
        var many=Enumerable.Range(0,1400).Select(i=>new FieldCampSiteSeed(i*100f,0f,0f,1f)).ToArray();probes=0;
        sample=(float x,float y,bool detail,out float z)=>{probes++;z=0f;return true;};
        Check(FieldCampSitePlanner.TryChoose(many,20f,-20f,20f,sample,_=>true,out _,out _),"large candidate list yields a complete valid site");
        Check(probes<=FieldCampSitePlanner.MaxSeeds*2*25+FieldCampSitePlanner.DetailedFinalists*81,"candidate input cannot cause an unbounded native-probe scan");
        Check(!FieldCampPreparationReadiness.CanParkOutside(true,false,11f,400f,9f),"give optional spectators their real walking opportunity");
        Check(FieldCampPreparationReadiness.CanParkOutside(true,false,12f,400f,9f),"late optional audience outside the clear site cannot force a 55-second cancellation");
        Check(!FieldCampPreparationReadiness.CanParkOutside(true,false,30f,64f,9f),"an actor inside the actual build footprint never gets waived");
        Check(!FieldCampPreparationReadiness.CanParkOutside(true,false,30f,90f,9f),"body margin at the edge of the build footprint remains protected");
        Check(FieldCampPreparationReadiness.CanParkOutside(false,false,1f,400f,9f),"a displaced resident is done clearing once physically safe, not after an irrelevant distant animation mark");
        Check(!FieldCampPreparationReadiness.CanParkOutside(true,true,30f,400f,9f),"actually arrived audience retains its borrowed role");
        Check(!FieldCampPreparationReadiness.CanParkOutside(true,false,30f,float.NaN,9f),"invalid physical evidence cannot waive an actor");
        var host=new TownExecutionMissionBehavior();var mission=host.Mission;host.RunRetire();
        Check(mission.Events.SequenceEqual(new[]{"finalize","death","host"}),"cancelled preparation snapshots mission and removes host last");
        Check(host.Mission==null&&!host.Retry&&mission.EndCalls==0,"native host removal can invalidate Mission without a null reference or camp exit");
        host=new TownExecutionMissionBehavior { ThrowCleanup=true };mission=host.Mission;
        for(int i=0;i<4;i++)host.RunRetire();
        Check(!host.Retry&&mission.EndCalls==0&&mission.Events.Count(x=>x=="finalize")==3,"cleanup failure stops bounded retries without terminating the parent camp");
        host=new TownExecutionMissionBehavior { CleanupReady=false };mission=host.Mission;host.RunRetire();
        Check(host.Retry&&!mission.Events.Contains("host")&&mission.EndCalls==0,"incomplete cleanup retains its owner for retry, without early detach");
        Console.WriteLine($"PASS {checks} production level-site/readiness/cancellation assertions (pure probes and fake native lifecycle; native game NOT RUN)");
    }
}
namespace RichExecutions.Diagnostics
{
    static class RexLog { internal static void Warning(string s){} internal static void Error(string s,Exception e){} }
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
        bool _cleanupComplete,_endMissionNextTick;
        internal bool ThrowCleanup,CleanupReady=true;internal bool Retry=>_endMissionNextTick;
        public TownExecutionMissionBehavior(){Mission=new FakeMission {Host=this};}
        void FinalizeSession(){Mission.Events.Add("finalize");if(ThrowCleanup)throw new InvalidOperationException();_cleanupComplete=CleanupReady;}
        internal void RunRetire(){_endMissionNextTick=false;RetireCancelledCampPreparation();}
    }
}
