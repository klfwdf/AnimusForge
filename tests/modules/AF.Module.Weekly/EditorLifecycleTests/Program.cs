using AnimusForge;
using static AnimusForge.MyBehavior;
using AnimusForge.Refactor.Runtime;
int n=0;void Check(bool ok,string label){if(!ok)throw new Exception(label);n++;}
var group=new WeeklyEventMaterialPreviewGroup{GroupKind="world",Title="materials",Materials=Enumerable.Range(0,33).Select(i=>new EventMaterialReference{MaterialType="misc",Label="item"+i}).ToList()};
var batch=new WeeklyReportBatchRequest {WeekIndex=1,Groups=new(){group}};
var records=new List<EventRecordEntry>{new(){EventId="world-old",EventKind="world",WeekIndex=1,CreatedDay=-1,ShortSummary=" short ",Title=""},new(){EventId="world-new",EventKind="world",WeekIndex=3,CreatedDay=4,Summary=" full ",Title="new"},new(){EventId="kingdom",EventKind="kingdom",ScopeKingdomId="k",WeekIndex=2,Title="kingdom"}};
int generated=0,awarded=0,returned=0;bool failBuild=false;
var port=new WeeklyEditorPort {
 CaptureGeneration=SaveRuntimeGuard.CaptureGeneration,IsCurrent=g=>g==SaveRuntimeGuard.Generation,
 PromptProfileLabel=()=>"standard",ResolveKingdomDisplay=id=>"Kingdom "+id,ResolveHeroDisplay=id=>id,ResolveSettlementDisplay=id=>id,
 BuildDevSummaryPreview=(text,max)=>text??"",TranslateEventMaterialTypeForDev=kind=>kind,
 AppendDevNpcActionField=(sb,label,value)=>sb.AppendLine(label+":"+value),GetEventAndRebellionApiMaxTokens=()=>100,
 BuildWeeklyReportGroupReportId=g=>g.GroupKind,
 EventRecords=()=>records,WorldOpeningSummary=()=>"opening",EnsureWeekZeroOpeningSummaryEvents=sanitize=>{},SanitizeEventRecordEntries=x=>x,
 GetDevEditableKingdoms=()=>new(),BuildWeeklyEventMaterialPreviewGroups=()=>new(){group},OrderWeeklyReportGenerationGroups=groups=>groups,
 BuildWeeklyReportBatchRequests=(groups,week,start,end)=>new(){batch},GetCurrentGameDayIndexSafe=()=>3,
 GetKingdomIdsByPlayerProximity=ids=>ids.ToList(),GetWeeklyReportRequestsPerMinute=()=>1,GetWeeklyReportBatchSize=()=>4,
 OpenDevEventEditorMenu=()=>returned++,GenerateDevWeeklyReportsAsync=()=>{generated++;return Task.CompletedTask;},
 OrderWeeklyPreviewMaterials=materials=>materials,TranslateEventKindForDev=kind=>kind,
 BuildBulletinPanel=(entry,id)=>failBuild?throw new Exception("bad panel"):new(){EventId=id},AwardReadingXp=id=>awarded++
};
var ui=new WeeklyReportEditorController(port);ui.SynchronizeGeneration(1);
var countries=ui.GetTerminalWeeklyReportBrowserCountries();Check(countries[0].IsWorld&&countries[0].CountryId=="world","world comes first");Check(countries.Count==2&&countries[1].CountryId=="k","orphan record kingdom remains browseable");Check(countries[0].Reports[0].EventId=="world-new","descending week order");Check(countries[0].Reports.Single(e=>e.EventId=="world-old").BodyText=="short"&&!countries[0].Reports.Single(e=>e.EventId=="world-old").HasFullReport&&countries[0].Reports.Single(e=>e.EventId=="world-old").CreatedDay==0,"short fallback and clamp");Check(countries[0].Reports[0].BodyText=="full"&&countries[0].Reports[0].HasFullReport,"full report projection");
ui.OpenDevWeeklyEventMaterialPreviewGroupDetail(group,100);Check(ui.MaterialPage==2&&ReferenceEquals(ui.MaterialSelection,group),"material page clamp and source identity");var late=TaleWorlds.Core.MBInformationManager.Last.Confirm;SaveRuntimeGuard.Generation++;late(new(){new(group.Materials[0],"",null)});Check(ui.MaterialDetail==null,"late material navigation rejected");
ui.SynchronizeGeneration(2);Check(ui.MaterialSelection==null&&ui.MaterialPage==0,"load retires selections");
ui.ConfirmGenerateDevWeeklyReports();var inquiry=TaleWorlds.Library.InformationManager.Inquiry;Check(generated==0,"preconfirm does not generate");inquiry.Cancel();inquiry.Confirm();Check(generated==0&&returned==1,"cancel retires affirmative callback");
ui.ConfirmGenerateDevWeeklyReports();inquiry=TaleWorlds.Library.InformationManager.Inquiry;inquiry.Confirm();inquiry.Confirm();Check(generated==1,"repeated confirm generates once");
ui.ConfirmGenerateDevWeeklyReports();inquiry=TaleWorlds.Library.InformationManager.Inquiry;SaveRuntimeGuard.Generation++;inquiry.Confirm();Check(generated==1,"load retires generation confirmation");
Check(ui.TryShowWorldBulletinPanel(records[0],"issue"),"bulletin renderer consumed");var reading=DevWeeklyReportPopup.Reading;SaveRuntimeGuard.Generation++;reading();Check(awarded==0,"late reading callback does not award new save");
Check(ui.TryShowWorldBulletinPanel(records[0],"issue"),"bulletin second open");DevWeeklyReportPopup.Reading();Check(awarded==1,"reading award uses current capability");failBuild=true;Check(!ui.TryShowWorldBulletinPanel(records[0],"bad"),"bad panel preserves legacy fallback");
var preview=WeeklyEditorProjection.BuildWeeklyReportPromptPreviewText(port,group,"system","user");Check(preview.Contains("system")&&preview.Contains("user")&&preview.Contains("standard"),"prompt display source text retained");

int retries=0, fresh=0, exits=0, persisted=0, markedWeek=0, repair=0;
var settings=new DuelSettings { WeeklyReportRequestsPerMinute=8 };
var pending=new TaskCompletionSource<WeeklyReportGenerationResult>();
port.ReadSettings=()=>settings; port.PersistSettings=x=>persisted++;
port.OpenApiRepairFlow=()=>{repair++;return true;}; port.ExitCurrentGameFromWeeklyReportGate=()=>exits++;
port.IsMainThreadAndHostCurrent=()=>true;
port.GenerateRetry=context=>{retries++;return pending.Task;}; port.MarkAutoGeneratedWeek=week=>markedWeek=week;
port.CollectFreshMaterials=(start,end)=>new(){group};port.BuildGroupMap=groups=>groups.ToDictionary(g=>g.GroupKind);
port.CreateFreshRetryContext=(old,groups)=>{fresh++;return new(){Groups=groups,WeekIndex=old.WeekIndex};};
ui.SynchronizeGeneration(SaveRuntimeGuard.Generation);
WeeklyReportRetryContext Context(bool rpm=false)=>new(){WeekIndex=7,Groups=new(){group},IsRequestsPerMinuteLimit=rpm,IsAutoGeneration=true};
ui.QueueWeeklyReportFailurePopup(Context(),true);var failure=TaleWorlds.Library.InformationManager.Inquiry;
Check(retries==0&&ui.UiStage==WeeklyReportUiStage.Failure,"failure does not retry before confirmation");
failure.Confirm();failure.Confirm();Check(retries==1&&ui.ManualRetryInProgress,"manual retry exactly once");
var progress=TaleWorlds.Library.InformationManager.Inquiry; progress.Cancel();
Check(!ui.ManualRetryInProgress&&ui.UiStage==WeeklyReportUiStage.Failure,"cancel retires manual version and returns to failure");
pending.SetResult(new(){Completed=true}); await Task.Delay(5);
Check(!ui.PendingManualRetryResult&&markedWeek==0,"cancelled async success cannot publish or mark auto week");
ui.QueueWeeklyReportFailurePopup(Context(),true);failure=TaleWorlds.Library.InformationManager.Inquiry;
SaveRuntimeGuard.Generation++;failure.Confirm();Check(retries==1,"load rejects failure callback");
ui.SynchronizeGeneration(SaveRuntimeGuard.Generation);Check(ui.RetryContext==null&&ui.UiStage==WeeklyReportUiStage.None&&!ui.ReopenAfterApiConfig,"load clears single UI state");
ui.QueueWeeklyReportFailurePopup(Context(true),true);failure=TaleWorlds.Library.InformationManager.Inquiry;failure.Confirm();
var rpmInput=TaleWorlds.Library.InformationManager.Text;rpmInput.Confirm("not a number");
Check(persisted==0,"invalid RPM reopens input without persistence");rpmInput=TaleWorlds.Library.InformationManager.Text;rpmInput.Confirm("100");rpmInput.Confirm("2");
Check(settings.WeeklyReportRequestsPerMinute==20&&persisted==1&&retries==2,"RPM clamps and persists captured settings once before retry");
ui.ProcessPendingWeeklyReportManualRetryResult();Check(ui.RetryContext==null&&ui.UiStage==WeeklyReportUiStage.None&&markedWeek==7,"success consumes pending result and clears failure");
pending=new();ui.QueueWeeklyReportFailurePopup(Context(),true);TaleWorlds.Library.InformationManager.Inquiry.Confirm();
SaveRuntimeGuard.Generation++;pending.SetResult(new(){Completed=true});await Task.Delay(5);
Check(!ui.PendingManualRetryResult&&markedWeek==7,"late save generation async result rejected without UI tick");
ui.SynchronizeGeneration(SaveRuntimeGuard.Generation);pending=new();ui.QueueWeeklyReportFailurePopup(Context(),true);TaleWorlds.Library.InformationManager.Inquiry.Confirm();
var same=ui.RetryContext;pending.SetResult(new(){BlockedByChangedRecord=true});await Task.Delay(5);ui.ProcessPendingWeeklyReportManualRetryResult();
Check(ReferenceEquals(ui.RetryContext,same)&&same.RequiresFreshMaterials&&!ui.ManualRetryInProgress,"changed record keeps context identity and requests fresh materials");
TaleWorlds.Library.InformationManager.Inquiry.Confirm();Check(fresh==1&&retries==5,"fresh confirmation selects required current groups then typed generation");
ui.CancelWeeklyReportManualRetryAndReturn();ui.OpenWeeklyReportApiRepairFlow();Check(repair==1&&ui.ReopenAfterApiConfig&&ui.UiStage==WeeklyReportUiStage.None,"API repair defers same context reopen");
ui.ReopenAfterApiConfigUtcTicks=0;ui.ProcessWeeklyReportUiResume();Check(!ui.ReopenAfterApiConfig&&ui.UiStage==WeeklyReportUiStage.Failure,"tick resumes failure only after inquiry delay");
ui.ShowWeeklyReportFailurePopup(true);var replaced=TaleWorlds.Library.InformationManager.Inquiry;ui.ShowWeeklyReportFailurePopup(true);replaced.Cancel();Check(repair==1,"replaced dialog callback cannot reopen API config");
Console.WriteLine($"PASS: {n} production Weekly editor/panel lifecycle assertions (game/render/record declarations fixture; no data commits).");

// Real controller wiring: both live and vanished related nations need archive tabs.
int archiveStart=n,snapshots=0;
records.Add(new(){EventId="weekly_report:world:bulletin:8:30",EventKind="world",ScopeKingdomId="",Title="bulletin",Summary="current issue body",CreatedDay=30,WeekIndex=4,BulletinKingdomIds=new(){"k","minor-only","vanished"}});
records.Add(new(){EventId="weekly_report:world:bulletin:7:29",EventKind="world",ScopeKingdomId="",Title="legacy bulletin",Summary="legacy body",CreatedDay=29,WeekIndex=4});
port.BulletinKingdomAssociations=()=>{snapshots++;return new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase){{"weekly_report:world:bulletin:7:29",new(){"legacy-vanished","k"}}};};
port.GetDevEditableKingdoms=()=>new(){new(){StringId="k"},new(){StringId="unrelated"}};
var archive=ui.GetTerminalWeeklyReportBrowserCountries();
Check(snapshots==1,"legacy layout snapshot built once per archive open");
Check(archive.Count(x=>x.CountryId=="k")==1,"live and recorded kingdom tabs do not duplicate");
Check(archive.Single(x=>x.CountryId=="k").Reports.First().BodyText=="current issue body","controller wires latest related bulletin to kingdom");
Check(archive.Single(x=>x.CountryId=="minor-only").Reports.Count==1,"minor-only kingdom archive discovered");
Check(archive.Single(x=>x.CountryId=="vanished").Reports.Count==1,"vanished kingdom retained by durable metadata");
Check(archive.Single(x=>x.CountryId=="legacy-vanished").Reports.Count==1,"old layout-only vanished kingdom discovered");
Check(archive.Single(x=>x.CountryId=="unrelated").Reports.Count==0,"unrelated live kingdom not contaminated");
Check(archive[0].IsWorld&&archive[0].Reports.First().BodyText=="current issue body","world canonical archive stays first and current");
Console.WriteLine($"PASS: {n-archiveStart} production controller bulletin association assertions, {n} total editor assertions.");

failBuild=false;
Check(ui.OpenArchivedReport("weekly_report:world:bulletin:8:30"),"archive reopens existing issue");
Check(DevWeeklyReportPopup.OpenedId=="weekly_report:world:bulletin:8:30"&&DevWeeklyReportPopup.Seconds==0&&DevWeeklyReportPopup.Reading==null&&awarded==1,"archive uses exact original panel without wait or reading reward");
Check(ui.OpenArchivedReport("world-new")&&DevWeeklyReportPopup.Body.Trim()=="full","ordinary full weekly reopens saved body");
Check(!ui.OpenArchivedReport("missing"),"missing archive does not open unrelated report");
failBuild=true;
Check(ui.OpenArchivedReport("weekly_report:world:bulletin:8:30")&&DevWeeklyReportPopup.Body=="current issue body","panel build failure falls back to complete saved text");
records.Add(new(){EventId="weekly_report:kingdom:4:orphan:brief",EventKind="kingdom",ScopeKingdomId="orphan",WeekIndex=4,CreatedDay=28,Title="orphan news",Summary="regional complete text"});
var mergedArchive=ui.GetTerminalWeeklyReportBrowserCountries();
Check(mergedArchive.All(c=>c.Reports.All(r=>!r.EventId.EndsWith(":brief"))),"controller no longer lists standalone regional briefs");
var orphanIssue=mergedArchive.Single(c=>c.CountryId=="orphan").Reports.Single();
Check(ui.OpenArchivedReport(orphanIssue.EventId)&&DevWeeklyReportPopup.Body.Contains("regional complete text"),"orphan weekly consolidation remains reopenable without discarding text");
Console.WriteLine($"PASS: {n} total production editor assertions including archive reopen/fallback.");
