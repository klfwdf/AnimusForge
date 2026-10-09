using System;using System.Collections.Generic;using System.Linq;using System.Threading.Tasks;using AnimusForge;using static AnimusForge.MyBehavior;
namespace AnimusForge { public static class Logger { public static void Log(string area,string text){} } }
namespace AnimusForge { internal static class WorldBulletinPanelIllustrationBridge { internal static Func<bool> ShouldPreloadSelection; } }
internal static class Program {
 static int n;static void Check(bool ok,string label){if(!ok)throw new Exception(label);n++;}
 static string Body(string title="title")=>"[TITLE]"+title+"[SHORT]summary[REPORT]report[TAGS]STAB_FLAT";
 static string Block(string id,string body,string extra="")=>"[REPORT_BLOCK_BEGIN]\nreport_id="+id+"\n"+extra+body+"\n[REPORT_BLOCK_END]";
 static async Task TestBulletinDelayedCompletion()
 {
     foreach(string mode in new[]{"epoch","generation","disabled","success","battle-rejected"})
     {
         var records=new List<EventRecordEntry>();var api=new TaskCompletionSource<ApiCallResult>();
         int cancelled=0,notices=0;bool publishing=true;
         var owner=new WorldBulletinStateOwner();
         owner.Bind(new WorldBulletinPort {
             Enabled=()=>true,PublishingEnabled=()=>publishing,CurrentDay=()=>4,CurrentHour=()=>102,
             CurrentDate=()=>"卡拉迪亚1084年秋季21日",Render=x=>x,Focus=()=>new(),
             Records=()=>records,FindRecord=id=>records.FirstOrDefault(r=>r.EventId==id),
             ProductState=r=>r?.Summary??"",NotifyProductChanged=(before,after)=>{},NotifyTimeline=()=>{},
             ResolveKingdom=id=>id,QueueNotice=id=>notices++,PrepareIssue=id=>{},
             CallApi=(system,user)=>api.Task,CancelIllustration=plan=>cancelled++,Log=(area,text)=>{}
         });
         var state=owner.EnsureWorldBulletinState();state.World.WindowEndHour=102;
         state.Events.Add(new WorldBulletinEvent {Key="confirmed-fact",Kind="war_declared",Day=4,Hour=100,Score=100,Sentence="确定发生的事实。",Group="fact"});
         var selection=WorldBulletinPolicy.Select(state.Events,state.World,new(),102);
         var template=WorldBulletinPolicy.BuildTemplate(selection);
         if(mode=="battle-rejected") {
             selection.Major.Kind="battle";
             foreach(var fact in selection.MajorFacts)fact.Kind="battle";
         }
         long generation=SaveRuntimeGuard.CaptureGeneration();owner.InFlight=true;
         var pending=owner.RunWorldBulletinRequestAsync(102,generation,selection,template,"system","user",null);
         Check(records.Count==0&&owner.MainThreadActions.IsEmpty,"background request does not publish before result "+mode);
         if(mode=="epoch") {
             owner.RestartCollection(101);
             // Equal numeric windows do not identify the same collection after a restart.
             state.World.WindowEndHour=102;owner.InFlight=true;
         }
         if(mode=="generation")SaveRuntimeGuard.AdvanceGeneration("bulletin delayed save switch");
         if(mode=="disabled")publishing=false;
         api.SetResult(new ApiCallResult {Success=true,Content=mode=="battle-rejected"?"[TITLE]战报\n[MAJOR]双方三百人对五百人，阵亡八十人。\n[SHORT]阵亡八十人。":"{bad response"});await pending;
         Check(records.Count==0&&owner.MainThreadActions.Count==1,"completed worker only enqueues commit "+mode);
         owner.ProcessWorldBulletinMainThreadActions();
         if(mode=="epoch")Check(records.Count==0&&cancelled==1&&owner.InFlight&&state.World.WindowEndHour==102,"old epoch cannot publish or retire new equal-window request");
         if(mode=="generation")Check(records.Count==0&&notices==0,"old save generation cannot publish records or notices");
         if(mode=="disabled")Check(records.Count==0&&cancelled==1&&!owner.InFlight&&state.World.WindowEndHour<0,"disabled publishing closes window without records");
         if(mode=="success") {
             var issue=records.Single(r=>WeeklyReportArchivePolicy.IsBulletin(r.EventId));
             Check(issue.CreatedDay==4&&issue.CreatedDate=="卡拉迪亚1084年秋季21日"&&notices==1&&!owner.InFlight,"accepted commit uses current campaign calendar and releases one notice");
             Check(issue.Materials.Any(m=>m.SourceStableKeys.Contains("confirmed-fact")),"accepted commit retains factual source graph after provider parse failure");
             owner.CompleteWorldBulletin(102,generation,selection,template,null,null);
             Check(records.Count==1&&notices==1,"completed window cannot duplicate publication or notice");
         }
         if(mode=="battle-rejected") {
             var issue=records.Single(r=>WeeklyReportArchivePolicy.IsBulletin(r.EventId));
             Check(!issue.Summary.Contains("八十")&&!issue.Summary.Contains("三百")&&issue.Summary.Contains("确定发生的事实"),"battle numeric response falls back at actual publication boundary");
             Check(string.IsNullOrEmpty(issue.BulletinAnecdote),"rejected response does not enter NPC anecdote");
             Check(notices==1&&issue.Materials.Any(m=>m.SourceStableKeys.Contains("confirmed-fact")),"fallback keeps publication notice and confirmed source graph");
         }
     }
 }
 static void TestRegionalNewsAttachment()
 {
     int notices=0,changed=0,plans=0; double hour=312; int day=13;
     var records=new List<EventRecordEntry>();
     var owner=new WorldBulletinStateOwner();
     var port=new WorldBulletinPort {Records=()=>records,FindRecord=id=>records.FirstOrDefault(e=>e.EventId==id),
         CurrentDay=()=>day,CurrentHour=()=>hour,CurrentDate=()=>"date "+day,ProductState=e=>e?.Summary??"",
         NotifyTimeline=()=>changed++,NotifyProductChanged=(a,b)=>{},QueueNotice=id=>notices++,PrepareIssue=id=>plans++,
         ResolveKingdom=id=>id,Log=(a,b)=>{},NoticeTitle=e=>e.Title,PopupSubtitle=e=>e.CreatedDate,PopupBody=e=>e.Summary};
     owner.Bind(port); var state=owner.EnsureWorldBulletinState(); state.World.WindowEndHour=hour;
     WorldBulletinEvent Fact(string key,int score,int d,params string[] nations)=>new(){Key=key,Kind="war_declared",Score=score,Day=d,Hour=d*24,GameDate="date "+d,Sentence="fact "+key,KingdomIds=nations.ToList(),Group=key};
     state.Events.Add(Fact("lead",100,12,"A","B"));
     for(int i=0;i<6;i++) state.Events.Add(Fact("short"+i,40,12,"A"));
     state.Events.Add(Fact("cross",1,13,"A","B","a"));
     state.Events.Last().Detail="5 confirmed captives";
     state.Events.Add(Fact("other",1,13));
     state.Events.Add(Fact("previous-week",1,6,"A"));
     state.Events.Add(Fact("same-sentence-different-source",1,13,"Z"));state.Events.Last().Sentence=state.Events[0].Sentence;
     var selection=WorldBulletinPolicy.Select(state.Events,state.World,new(),hour);
     Check(selection.Minors.Count<=4&&selection.MajorFacts.Count>0,"one main story and at most four true shorts");
     Check(selection.WindowFacts.Count==11&&selection.ReportedKeys.Count>0,"snapshot contains all eligible scores and selected stable keys");
     string captured=selection.WindowFacts.Single(e=>e.Key=="cross").Sentence;
     state.Events.Single(e=>e.Key=="cross").Sentence="modified after snapshot";
     state.Events.Add(Fact("late-same-hour",100,13,"C"));
     state.Events.Add(new(){Key="late-next-hour",Hour=313,Day=13,Score=100,Kind="war_declared",Sentence="late next",KingdomIds=new(){"C"}});
     var template=WorldBulletinPolicy.BuildTemplate(selection);
     owner.PublishWorldBulletin(state.World,selection,template,null,null);
     var issue=records.Single(e=>WeeklyReportArchivePolicy.IsBulletin(e.EventId));
     var recent=records.Where(e=>WeeklyReportArchivePolicy.IsRecent(e.EventId)).ToList();
     var reported=selection.ReportedKeys;
     Check(recent.All(e=>e.Materials.All(m=>!m.SourceStableKeys.Any(reported.Contains))),"selected main and short source keys excluded from every recent record");
     var residual=selection.WindowFacts.Where(f=>!reported.Contains(f.Key)).ToList();
     foreach(var fact in residual) foreach(var nation in fact.KingdomIds.Distinct(StringComparer.OrdinalIgnoreCase).DefaultIfEmpty(WeeklyReportArchivePolicy.OtherKingdomId))
         Check(recent.Single(e=>e.EventId==WeeklyReportArchivePolicy.RecentId(nation,fact.Day/7)).Materials.Any(m=>m.SourceStableKeys.Contains(fact.Key)),"every rejected fact reaches its actual nations: "+fact.Key+" "+nation);
     Check(recent.Single(e=>e.EventId==WeeklyReportArchivePolicy.RecentId("A",1)).Summary.Contains(captured)&&!recent.Any(e=>e.Summary.Contains("modified after snapshot")),"publication uses detached facts not mutated game pool");
     Check(recent.Single(e=>e.EventId==WeeklyReportArchivePolicy.RecentId("A",1)).Summary.Contains("5 confirmed captives"),"rejected supplementary facts retained in near body");
     Check(recent.Single(e=>e.ScopeKingdomId=="Z").Materials.Single().SourceStableKeys.Contains("same-sentence-different-source"),"independent source key is not discarded merely because its sentence matches headline");
     Check(recent.Count(e=>e.ScopeKingdomId=="A")==2&&recent.All(e=>e.Materials.Count>0),"cross-week independent, same-week combined, no empty nations");
     Check(!recent.Any(e=>e.ScopeKingdomId=="C")&&state.World.DeferredFactKeys.SequenceEqual(new[]{"late-same-hour"}),"facts arriving during generation are not archived and same-hour facts carried durably");
     Check(notices==1&&plans==1&&changed==1,"only main issue notifies or prepares illustration");
     int before=records.Count;owner.PublishWorldBulletin(state.World,selection,template,null,null);
     Check(records.Count==before&&notices==1&&state.World.Sequence==1,"repeated completed publication is idempotent");
     var exported=owner.ExportJson(); owner.ImportJson(exported);state=owner.EnsureWorldBulletinState();
     Check(state.World.DeferredFactKeys.SequenceEqual(new[]{"late-same-hour"}),"pending snapshot boundary survives owner save/load");
     hour=336; day=14;state.World.WindowEndHour=hour;
     Check(WorldBulletinPolicy.FindPendingTriggerHour(state.Events,state.World,new(),hour)==312,"same-hour late headline can trigger next window");
     var next=WorldBulletinPolicy.Select(state.Events,state.World,new(),hour);
     Check(next.WindowFacts.Select(e=>e.Key).ToHashSet().SetEquals(new[]{"late-same-hour","late-next-hour"}),"next window contains late facts without replaying consumed sources");
     // Detached replay of the original window must not duplicate any country's materials.
     var duplicate=owner.BuildRegionalPublication(selection);
     Check(duplicate.All(e=>e.Materials.Count==recent.Single(old=>old.EventId==e.EventId).Materials.Count),"source-key dedup remains stable after load");
     var add=Fact("append",1,13,"A");next.WindowFacts.Add(add);
     owner.PublishWorldBulletin(state.World,next,WorldBulletinPolicy.BuildTemplate(next),null,null);
     Check(records.Single(e=>e.EventId==WeeklyReportArchivePolicy.RecentId("A",1)).Materials.Any(m=>m.SourceStableKeys.Contains("append")),"second issue immediately appends same-country same-week near facts");
     Check(state.World.DeferredFactKeys.Count==0,"consumed deferred keys cleared");
     Check(owner.BuildRegionalPublication(new(){Major=new(){Key="empty"}}).Count==0&&WorldBulletinPolicy.Select(Array.Empty<WorldBulletinEvent>(),new(){WindowEndHour=hour},new(),hour)==null,"no facts means no empty near records or fake main issue");
     var grouped=new List<WorldBulletinEvent>{Fact("group-lead",100,13,"A")};
     for(int i=0;i<7;i++) { var f=Fact("group-short"+i,40,13,"A");f.Group="one-minor";grouped.Add(f); }
     var groupedSelection=WorldBulletinPolicy.Select(grouped,new(){WindowEndHour=hour},new(),hour);
     Check(groupedSelection.Minors.Single().Events.Count==WorldBulletinPolicy.MaxMinorGroupSentences&&groupedSelection.ReportedKeys.Count==1+WorldBulletinPolicy.MaxMinorGroupSentences,"group short-news overflow is not falsely marked reported by a count-only summary");
     Check(owner.BuildRegionalPublication(groupedSelection).Single().Materials.Count(m=>m.SourceStableKeys.Any(k=>k.StartsWith("group-short")))==7-WorldBulletinPolicy.MaxMinorGroupSentences,"all facts omitted from a grouped short reach the national near record");
     var panel=owner.BuildWorldBulletinPanelData(issue,issue.EventId);
     Check(!panel.Minors.Any(m=>m.Value.Contains(captured))&&!panel.BodyText.Contains(captured),"original scroll excludes country residual facts");
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
 static async Task Main(){await TestBulletinDelayedCompletion();TestBulletinNoticeRecovery();var rules=new WeeklyGenerationRules(x=>x.Replace("{player}","Alice"));var world=new WeeklyEventMaterialPreviewGroup{GroupKind="world"};var king=new WeeklyEventMaterialPreviewGroup{GroupKind="kingdom",KingdomId="k"};var batch=new WeeklyReportBatchRequest{Groups=new(){world,king},SystemPrompt="sys",UserPrompt="usr",PromptPreview="preview"};
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
