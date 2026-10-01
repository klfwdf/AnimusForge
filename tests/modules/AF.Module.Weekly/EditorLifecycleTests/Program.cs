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
var countries=ui.GetTerminalWeeklyReportBrowserCountries();Check(countries[0].IsWorld&&countries[0].CountryId=="world","world comes first");Check(countries.Count==2&&countries[1].CountryId=="k","orphan record kingdom remains browseable");Check(countries[0].Reports[0].EventId=="world-new","descending week order");Check(countries[0].Reports[1].BodyText=="short"&&!countries[0].Reports[1].HasFullReport&&countries[0].Reports[1].CreatedDay==0,"short fallback and clamp");Check(countries[0].Reports[0].BodyText=="full"&&countries[0].Reports[0].HasFullReport,"full report projection");
ui.OpenDevWeeklyEventMaterialPreviewGroupDetail(group,100);Check(ui.MaterialPage==2&&ReferenceEquals(ui.MaterialSelection,group),"material page clamp and source identity");var late=TaleWorlds.Core.MBInformationManager.Last.Confirm;SaveRuntimeGuard.Generation++;late(new(){new(group.Materials[0],"",null)});Check(ui.MaterialDetail==null,"late material navigation rejected");
ui.SynchronizeGeneration(2);Check(ui.MaterialSelection==null&&ui.MaterialPage==0,"load retires selections");
ui.ConfirmGenerateDevWeeklyReports();var inquiry=TaleWorlds.Library.InformationManager.Inquiry;Check(generated==0,"preconfirm does not generate");inquiry.Cancel();inquiry.Confirm();Check(generated==0&&returned==1,"cancel retires affirmative callback");
ui.ConfirmGenerateDevWeeklyReports();inquiry=TaleWorlds.Library.InformationManager.Inquiry;inquiry.Confirm();inquiry.Confirm();Check(generated==1,"repeated confirm generates once");
ui.ConfirmGenerateDevWeeklyReports();inquiry=TaleWorlds.Library.InformationManager.Inquiry;SaveRuntimeGuard.Generation++;inquiry.Confirm();Check(generated==1,"load retires generation confirmation");
Check(ui.TryShowWorldBulletinPanel(records[0],"issue"),"bulletin renderer consumed");var reading=DevWeeklyReportPopup.Reading;SaveRuntimeGuard.Generation++;reading();Check(awarded==0,"late reading callback does not award new save");
Check(ui.TryShowWorldBulletinPanel(records[0],"issue"),"bulletin second open");DevWeeklyReportPopup.Reading();Check(awarded==1,"reading award uses current capability");failBuild=true;Check(!ui.TryShowWorldBulletinPanel(records[0],"bad"),"bad panel preserves legacy fallback");
var preview=WeeklyEditorProjection.BuildWeeklyReportPromptPreviewText(port,group,"system","user");Check(preview.Contains("system")&&preview.Contains("user")&&preview.Contains("standard"),"prompt display source text retained");
Console.WriteLine($"PASS: {n} production Weekly editor/panel lifecycle assertions (game/render/record declarations fixture; no data commits).");
