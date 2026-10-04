using System;using System.Collections.Generic;using System.Linq;using System.Threading.Tasks;using AnimusForge;using static AnimusForge.MyBehavior;
namespace AnimusForge { public static class Logger { public static void Log(string area,string text){} } }
namespace AnimusForge { internal static class WorldBulletinPanelIllustrationBridge { internal static Func<bool> ShouldPreloadSelection; } }
internal static class Program {
 static int n;static void Check(bool ok,string label){if(!ok)throw new Exception(label);n++;}
 static string Body(string title="title")=>"[TITLE]"+title+"[SHORT]summary[REPORT]report[TAGS]STAB_FLAT";
 static string Block(string id,string body,string extra="")=>"[REPORT_BLOCK_BEGIN]\nreport_id="+id+"\n"+extra+body+"\n[REPORT_BLOCK_END]";
 static void TestRegionalNewsAttachment()
 {
     int changed=0;
     var records=new List<EventRecordEntry>();
     var issue=new EventRecordEntry{EventId="weekly_report:world:bulletin:1:13",EventKind="world",CreatedDay=13,Summary="【大事件】Original major\n【其他消息】Original minor"};
     var port=new WorldBulletinPort{Records=()=>records,EligibleKingdoms=()=>new(){new("A","Nation A"),new("B","Nation B")},NotifyTimeline=()=>changed++,Log=(a,b)=>{},ResolveKingdom=id=>id,NoticeTitle=e=>e.Title,PopupSubtitle=e=>e.CreatedDate,PopupBody=e=>e.Summary};
     port.CurrentDay=()=>0;
     var owner=new WorldBulletinStateOwner();owner.Bind(port);
     var state=owner.EnsureWorldBulletinState();state.World.Sequence=1;state.TrackingStartDay=0;
     state.Events.Add(new(){Key="a",KingdomIds=new(){"A"},Day=12,Sentence="A confirmed event",Score=30});
     owner.WriteWorldBulletinKingdomBriefs(state,14);
     Check(state.LastKingdomWeek<2&&changed==0,"no issue does not consume regional week");
     records.Add(issue);owner.WriteWorldBulletinKingdomBriefs(state,14);
     Check(records.Count==1&&issue.Materials.Count==2&&state.LastKingdomWeek==2&&changed==1,"new regional messages attach to issue without creating archive rows");
     Check(issue.BulletinKingdomIds.ToHashSet().SetEquals(new[]{"A","B"}),"attached nations join issue archive associations");
     Check(WeeklyReportArchivePolicy.BodyWithRegionalNews(issue).Contains("A confirmed event"),"new regional facts are available in full issue body");
     var panel=owner.BuildWorldBulletinPanelData(issue,issue.EventId);
     Check(panel.BodyText=="Original major"&&panel.Minors.Count==3&&panel.Minors.Any(m=>m.Value.Contains("A confirmed event")),"original panel preserves major and contains all attached messages");
     owner.WriteWorldBulletinKingdomBriefs(state,14);
     Check(issue.Materials.Count==2&&changed==1,"same-week hourly calls do not duplicate regional messages");
     records.Clear();owner.ResetTransient();owner.WriteWorldBulletinKingdomBriefs(state,21);
     Check(state.LastKingdomWeek==2&&changed==1,"missing next issue does not discard next week regional news");
 }
 static void TestBulletinNoticeRecovery()
 {
     TestRegionalNewsAttachment();
     var notices = new WeeklyNoticeStateOwner();
     var records = new Dictionary<string, EventRecordEntry>();
     var owner = new WorldBulletinStateOwner();
     var callbacks = new List<Action>();
     var port = new WorldBulletinPort {
         CurrentDay=()=>10, FindRecord=id=>records.TryGetValue(id,out var e)?e:null,
         QueueNotice=id=>notices.Queue(id), Log=(a,b)=>{},
         AwaitIllustration=(plan,release)=>{callbacks.Add(release);return true;}
     };
     owner.Bind(port); owner.EnsureWorldBulletinState();
     string id="weekly_report:world:bulletin:1:10";
     records[id]=new(){EventId=id,EventKind="world"};
     owner.QueueNoticeAfterIllustration(id,new(){Identity="first"});
     Check(notices.Unread.Count==0 && owner.State.PendingNoticeEventIds.SequenceEqual(new[]{id}),"pending illustration notice is persisted before release");
     string saved=owner.ExportJson();
     var loaded=new WorldBulletinStateOwner(); loaded.Bind(port);
     SaveRuntimeGuard.AdvanceGeneration("load_notice_test");
     loaded.ImportJson(saved); loaded.ResetTransient();
     callbacks[0]();
     Check(notices.Unread.Count==0 && loaded.State.PendingNoticeEventIds.Count==1,"old generation callback cannot publish into loaded campaign");
     loaded.ResetTransient();
     Check(loaded.MainThreadActions.Count==1,"repeated load reset rebuilds recovery without duplicates");
     loaded.ProcessWorldBulletinMainThreadActions();
     Check(notices.Unread.SequenceEqual(new[]{id}) && loaded.State.PendingNoticeEventIds.Count==0,"load transfers saved pending notice to durable unread queue");
     loaded.ResetTransient(); loaded.ProcessWorldBulletinMainThreadActions();
     Check(notices.Unread.Count==1,"repeated recovery cannot duplicate unread");
     // Existing unread notice survives another load even after its pending marker is removed.
     var unreadSaved=notices.Unread.ToList(); notices=new(){Unread=unreadSaved};
     loaded.ImportJson(loaded.ExportJson());loaded.ResetTransient();loaded.ProcessWorldBulletinMainThreadActions();
     Check(notices.Unread.SequenceEqual(new[]{id}),"subsequent save restores unread without pending marker");
     notices.MarkRead(id); loaded.ResetTransient();loaded.ProcessWorldBulletinMainThreadActions();
     Check(notices.Unread.Count==0,"read notice is not resurrected");
     callbacks.Clear();owner.QueueNoticeAfterIllustration(id,new()); callbacks[0]();callbacks[0]();
     Check(notices.Unread.Count==1&&owner.State.PendingNoticeEventIds.Count==0,"live release transfers once and removes marker");
     notices.MarkRead(id);
     port.AwaitIllustration=(plan,release)=>false;owner.QueueNoticeAfterIllustration(id,new());
     Check(notices.Unread.Count==1&&owner.State.PendingNoticeEventIds.Count==0,"no pending artwork queues immediately");
     notices.MarkRead(id);port.AwaitIllustration=(plan,release)=>throw new Exception("bridge");owner.QueueNoticeAfterIllustration(id,new());
     Check(notices.Unread.Count==1&&owner.State.PendingNoticeEventIds.Count==0,"bridge failure falls back to immediate notice");
     notices.MarkRead(id);port.QueueNotice=x=>throw new Exception("queue");
     try{owner.QueueNoticeAfterIllustration(id,null);}catch(Exception){}
     Check(owner.State.PendingNoticeEventIds.Contains(id),"queue exception preserves recovery marker");
     port.QueueNotice=x=>notices.Queue(x);owner.ResetTransient();owner.ProcessWorldBulletinMainThreadActions();
     Check(notices.Unread.Count==1&&owner.State.PendingNoticeEventIds.Count==0,"next reset can recover a failed queue transfer");
     notices.MarkRead(id);owner.State.PendingNoticeEventIds.Add(id);records.Remove(id);owner.ResetTransient();owner.ProcessWorldBulletinMainThreadActions();
     Check(notices.Unread.Count==0&&owner.State.PendingNoticeEventIds.Count==0,"deleted record clears pending marker without notification");
     owner.ImportJson("{\"World\":{\"Sequence\":1}}"); owner.ResetTransient();owner.EnsureWorldBulletinState();
     Check(owner.State.PendingNoticeEventIds.Count==0&&owner.MainThreadActions.IsEmpty,"old save with no pending field loads without replaying archive");
     for(int i=0;i<7;i++){string next=id+i;records[next]=new(){EventId=next};owner.State.PendingNoticeEventIds.Add(next);}
     owner.ResetTransient();owner.ProcessWorldBulletinMainThreadActions();
     Check(notices.Unread.Count==4&&owner.State.PendingNoticeEventIds.Count==3,"recovery shares four action per tick budget");
     owner.ProcessWorldBulletinMainThreadActions();
     Check(notices.Unread.Count==7&&owner.State.PendingNoticeEventIds.Count==0,"remaining recoveries drain next tick");
     notices=new(); owner.State.PendingNoticeEventIds.Add(id);owner.ResetRuntime("new_game_created");owner.ProcessWorldBulletinMainThreadActions();
     Check(owner.State==null&&notices.Unread.Count==0&&owner.MainThreadActions.IsEmpty,"new game does not recover prior campaign notices");
 }
 static async Task Main(){TestBulletinNoticeRecovery();var rules=new WeeklyGenerationRules(x=>x.Replace("{player}","Alice"));var world=new WeeklyEventMaterialPreviewGroup{GroupKind="world"};var king=new WeeklyEventMaterialPreviewGroup{GroupKind="kingdom",KingdomId="k"};var batch=new WeeklyReportBatchRequest{Groups=new(){world,king},SystemPrompt="sys",UserPrompt="usr",PromptPreview="preview"};
 Check(rules.TryParseWeeklyBatchResponse(Block("world",Body()),batch,out var blocks,out var missing,out var error)&&missing.SequenceEqual(new[]{"kingdom:k"}),"partial block retains missing");
 Check(rules.TryParseWeeklyBatchResponse(Block("world","invalid")+Block("world",Body())+Block("kingdom:k",Body()),batch,out blocks,out missing,out error)&&missing.Count==0&&blocks.Count==3,"valid duplicate wins expected identity");
 Check(!rules.TryParseWeeklyBatchResponse(Block("unknown",Body()),batch,out blocks,out missing,out error)&&missing.Count==2,"unexpected identity not accepted");
 Check(!rules.TryParseWeeklyBatchResponse(Block("kingdom:k",Body(),"kind=kingdom\nkingdom_id=other\n"),batch,out blocks,out missing,out error),"kingdom mismatch rejected");
 Check(!rules.TryParseWeeklyBatchResponse(Block("world",Body(),"mode=title_short_tags_only\n"),batch,out blocks,out missing,out error),"mode mismatch rejected");
 var brief=new WeeklyEventMaterialPreviewGroup{GroupKind="world",OutputMode=WeeklyReportOutputMode.TitleShortTagsOnly};Check(!rules.TryParseWeeklyReportResponse(Body(),brief,0,out _,out _,out _,out _),"brief forbids report");
 Check(rules.TryParseWeeklyReportResponse("[TITLE]{player} Calradia[SHORT]summary[TAGS]STAB_UP_1",brief,0,out var title,out var shortText,out var report,out var tag)&&title=="Alice 大陆"&&report==""&&tag=="STAB_UP_1","brief renders and normalizes");
 Check(!WeeklyGenerationRules.TryValidateWeeklyReportTagText("STAB_UP_1 STAB_DOWN_1",out _,out _,out _),"conflicting tag fails");Check(WeeklyGenerationRules.TryValidateWeeklyReportTagText("prose STAB_UP_2 prose STAB_UP_2",out _,out var stable,out _)&&stable=="STAB_UP_2","duplicate same tag retained");
 Check(rules.TryParseWeeklyFullOnDemandReportResponse("[TITLE]full[REPORT]body",out _,out shortText,out report)&&shortText=="body","on demand short fallback");
 var delays=new List<int>();int calls=0;var owner=new WeeklyGenerationAttemptOwner(rules);var port=new WeeklyGenerationAttemptPort{CallGroup=(sys,user)=>Task.FromResult(new ApiCallResult{Success=true,Content=Body()}),CallBatch=(sys,user,gen,first)=>Task.FromResult(++calls==1?new ApiCallResult{IsRateLimit=true,RetryAfterSeconds=75,ErrorMessage="rate"}:new ApiCallResult{Success=true,Content=Block("world",Body())+Block("kingdom:k",Body())}),Delay=ms=>{delays.Add(ms);return Task.CompletedTask;},Log=(area,text)=>{},LogExchange=(label,prompt,response)=>{}};
 var result=await owner.GenerateWeeklyReportBatchWithRetriesAsync(batch,2,0,"batch",port);Check(result.Success&&result.AttemptsUsed==2&&calls==2,"retry succeeds");Check(delays.SequenceEqual(new[]{75000})&&!result.IsRateLimit&&result.RetryAfterSeconds==null,"latest metadata resets with server delay");
 calls=0;delays.Clear();port.CallBatch=(sys,user,gen,first)=>{calls++;return Task.FromResult(new ApiCallResult{Success=true,Content=Block("world",Body())});};result=await owner.GenerateWeeklyReportBatchWithRetriesAsync(batch,2,0,"batch",port);Check(!result.Success&&result.Blocks.Count==1&&result.MissingReportIds.SequenceEqual(new[]{"kingdom:k"})&&calls==2,"partial retries remain inspectable");Check(delays.SequenceEqual(new[]{1200}),"ordinary retry delay");
 var generation=SaveRuntimeGuard.CurrentGeneration;port.CallBatch=(sys,user,gen,first)=>{SaveRuntimeGuard.AdvanceGeneration("test");return Task.FromResult(new ApiCallResult{Success=true,Content=Block("world",Body())+Block("kingdom:k",Body())});};result=await owner.GenerateWeeklyReportBatchWithRetriesAsync(batch,3,generation,"batch",port);Check(!result.Success&&result.MissingReportIds.Count==2,"stale after API refuses success");
 var unprepared=new WeeklyReportBatchRequest{Groups=batch.Groups};calls=0;port.CallBatch=(a,b,c,e)=>{calls++;return Task.FromResult(new ApiCallResult());};result=await owner.GenerateWeeklyReportBatchWithRetriesAsync(unprepared,3,0,"batch",port);Check(!result.Success&&calls==0&&result.MissingReportIds.Count==2,"unprepared cannot launch");
 var groupResult=await owner.GenerateWeeklyReportGroupWithRetriesAsync(world,0,0,6,1,"sys","usr","preview","world",port);Check(groupResult.Success&&groupResult.PromptPreview=="preview","group old entry preserved");
 var notice=new WeeklyNoticeStateOwner();var records=new Dictionary<string,EventRecordEntry>();int lookups=0,pops=0;var np=new WeeklyNoticePort{FindRecord=id=>{lookups++;return records.TryGetValue(id,out var e)?e:null;},Publish=e=>pops++,IsBulletin=WorldBulletinStateOwner.IsWorldBulletinEventId,NearestKingdom=ids=>"near",Log=(a,b)=>{}};
 Check(notice.PublishPending(np)==0&&lookups==0,"idle notice tick performs no source scan");for(int i=0;i<12;i++){string id="weekly_report:world:"+i;records[id]=new(){EventId=id,EventKind="world"};notice.Queue(id);notice.Queue(id);}Check(notice.Unread.Count==12&&notice.PublishPending(np)==8&&pops==8,"notice dedup and eight pending budget");Check(notice.PublishPending(np)==4&&pops==12,"pending cursor continues next tick");notice.ResetShown();np.Publish=e=>throw new Exception("notification");try{notice.PublishPending(np);throw new Exception("swallowed");}catch(Exception e){Check(e.Message=="notification","notification failure propagates");}np.Publish=e=>pops++;Check(notice.PublishPending(np,1)==1&&pops==13,"failed publication remains pending");
 records.Clear();records["weekly_report:k:far"]=new(){EventId="weekly_report:k:far",EventKind="kingdom",WeekIndex=2,ScopeKingdomId="far"};records["weekly_report:k:near"]=new(){EventId="weekly_report:k:near",EventKind="kingdom",WeekIndex=2,ScopeKingdomId="near"};records["weekly_report:world:bulletin:1"]=new(){EventId="weekly_report:world:bulletin:1",EventKind="world"};records["weekly_report:kingdom:bulletin:1"]=new(){EventId="weekly_report:kingdom:bulletin:1",EventKind="kingdom"};notice=new();notice.Unread=records.Keys.ToList();notice.NormalizeUnreadWeeklyReportNoticesForCurrentPolicy(np);Check(notice.Unread.SequenceEqual(new[]{"weekly_report:k:near","weekly_report:world:bulletin:1"}),"legacy notices keep nearest kingdom and world bulletin only");
 bool enabled=true;int day=21,auto=0;var wb=new WorldBulletinStateOwner();wb.Bind(new(){Enabled=()=>enabled,PublishingEnabled=()=>false,CurrentDay=()=>day,CurrentHour=()=>day*24,Render=x=>x,Focus=()=>new(),Log=(a,b)=>{},AutoWeek=()=>auto,SetAutoWeek=x=>auto=x});Check(wb.CaptureWorldBulletinEvent("minor","one",1,"line\ntext",false,"g","detail","K","k")&&!wb.CaptureWorldBulletinEvent("minor","one",1,"again",false,"g","", "K"),"bulletin capture tail dedup");Check(wb.State.Events[0].Sentence=="line text"&&wb.State.Events[0].KingdomIds.Count==1,"bulletin canonical text and kingdom identity");enabled=false;Check(!wb.CaptureWorldBulletinEvent("minor","two",1,"x",false,"",""),"disabled bulletin capture no state write");Check(wb.TryRecordCoupOutcomeForBulletin("off",true,"x","","k","k"),"disabled coup callback remains handled");enabled=true;wb.TryRecordCoupOutcomeForBulletin("same",true,"coup","","k","k");for(int i=0;i<70;i++)wb.CaptureWorldBulletinEvent("minor","tail"+i,1,"x",false,"","");int before=wb.State.Events.Count;Check(wb.TryRecordCoupOutcomeForBulletin("same",true,"coup","","k","k")&&wb.State.Events.Count==before,"coup retry dedup searches retained set beyond tail");wb.OnWorldBulletinHourlyTick();Check(auto==3&&wb.LastPruneDay==day&&!wb.InFlight,"hourly updates cursor with publishing disabled");wb.ImportJson("{broken");Check(wb.State==null&&wb.ExportJson()=="{broken","unreadable save raw preserved");wb.EnsureWorldBulletinState();Check(wb.State.PreservedUnreadableState=="{broken"&&wb.ExportJson().Contains("{broken"),"unreadable payload retained through new state export");wb.ResetRuntime("new_game_created");Check(wb.State==null&&wb.CorruptRaw==null&&wb.LatestEventId=="","new game resets domain runtime and save state");
 Console.WriteLine("F4_WEEKLY_GENERATION_RESULT pass="+n+" fail=0 LIVE=NOT_RUN PROVIDER=NOT_RUN"); }
}
