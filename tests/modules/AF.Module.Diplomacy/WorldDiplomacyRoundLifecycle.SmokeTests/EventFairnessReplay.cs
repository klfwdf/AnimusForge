using AnimusForge;
using Newtonsoft.Json;

internal static class EventFairnessReplay
{
    internal static void Run()
    {
        var h=new FakeOrchestrationHost();
        var o=new WorldDiplomacyOrchestration(h,new WorldDiplomacyRuntimeState());
        var s=o.CurrentStorage;
        WorldDiplomacyJob Add(string id,string lane,int priority=90)
        {
            var r=new WorldDiplomacyRound { RoundId=id, State="active", BirthLane=lane, SchedulingOwner=lane, EventSystemVersion=1 };
            s.ConcurrentRounds.Add(r);
            var j=new WorldDiplomacyJob { JobId=id+"-job", RoundId=id, Kind="event_boundary", Priority=priority };
            DiplomacyEventRules.Bind(r,j);s.Jobs.Add(j);return j;
        }
        var n=Add("normal","normal",1000);
        var p=Add("player","player",1000);
        var a=Add("oral-a","oral",1);
        var b=Add("oral-b","oral",1000);
        var selected=new List<string>();
        for(int i=0;i<12;i++)
        {
            var j=WorldDiplomacyJobSelectionView.For(s).Select(s,24,"",_=>true,x=>o.LaneDispatchPriority(x,1));
            selected.Add(j!.RoundId);o.RecordLaneDispatch(j);
        }
        Test.True(selected.Take(3).Select(x=>x.Split('-')[0]).SequenceEqual(new[]{"normal","player","oral"}),"all three busy lanes receive a turn despite priority differences");
        Test.True(selected.Count(x=>x=="oral-a")==2 && selected.Count(x=>x=="oral-b")==2,"low-priority oral event is not starved by another event's repeated high-priority work");
        s.Jobs.Remove(p);selected.Clear();
        for(int i=0;i<4;i++) { var j=WorldDiplomacyJobSelectionView.For(s).Select(s,24,"",_=>true,x=>o.LaneDispatchPriority(x,1));selected.Add(o.RequestLane(j!));o.RecordLaneDispatch(j!); }
        Test.True(selected.Count(x=>x=="normal")==2 && selected.Count(x=>x=="oral")==2,"empty lane does not block available lanes");
        s.RequestBudget.AdvanceDay(1);
        for(int i=0;i<DiplomacyRequestBudget.DailyLimit;i++) Test.True(s.RequestBudget.TryAdmit("normal"),"ordinary HTTP accounting");
        Test.True(!s.RequestBudget.TryAdmit("normal") && s.RequestBudget.TryAdmit("oral") && s.RequestBudget.TryAdmit("player"),"ordinary budget does not hold accepted oral or player work behind it");
        var restored=JsonConvert.DeserializeObject<DiplomacyRequestBudget>(JsonConvert.SerializeObject(s.RequestBudget.Snapshot()))!;
        Test.True(restored.NonPlayerStarted==12 && restored.OralStarted==1 && restored.PlayerStarted==1,"three independent HTTP counters survive snapshot and reload");
        restored.AdvanceDay(2);
        Test.True(restored.NonPlayerStarted==0 && restored.OralStarted==0 && restored.PlayerStarted==0,"day reset is atomic across lanes");
        var legacy=new DiplomacyRequestBudget { SchemaVersion=0,Started=10,NonPlayerStarted=6,Day=1 };
        var migrated=legacy.Snapshot();
        Test.True(migrated.PlayerStarted==4 && migrated.NonPlayerStarted==6 && migrated.OralStarted==0,"legacy accounting is preserved without fabricating or refunding oral costs");
        s.RequestBudget.AdvanceDay(2);s.OralServiceRetryAfterUtcTicks=DateTime.UtcNow.AddMinutes(1).Ticks;
        Test.True(!o.CanDispatchDiplomacyJob(a) && o.CanDispatchDiplomacyJob(n),"oral transport backoff does not freeze ordinary cases");
        s.OralServiceRetryAfterUtcTicks=0;
        Test.True(o.RequestLeases.TryClaim(a,1,1000,1000,false,out var oral) && oral.RequestLane=="oral","detached request captures original oral lane");
        var same=new WorldDiplomacyJob { JobId="same",RoundId=a.RoundId,Kind="analyze" };
        Test.True(!o.RequestLeases.TryClaim(same,1,1000,1000,false,out _) && !o.CanDispatchDiplomacyJob(a),"boundary and semantic work cannot race in one event");
        Test.True(o.RequestLeases.TryClaim(n,1,1000,1000,false,out _) && o.RequestLeases.TryClaim(p,1,1000,1000,true,out _),"independent events may use remaining slots");
        Test.True(!o.RequestLeases.TryClaim(b,1,1000,1000,false,out _) && o.RequestLeases.Count==3,"combined transport concurrency stays bounded at three");
        Test.True(!o.RequestLeases.TryRelease(a.JobId,1,oral.Attempt+1) && o.RequestLeases.TryRelease(a.JobId,1,oral.Attempt),"only the matching attempt releases its slot");
        o.RequestLeases.Reset();
    }
}
