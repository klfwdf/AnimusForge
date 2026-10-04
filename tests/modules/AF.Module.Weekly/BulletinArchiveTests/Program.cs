using AnimusForge;
using Newtonsoft.Json;
using static AnimusForge.MyBehavior;
int count=0;
void Check(bool result,string label){if(!result)throw new Exception(label);count++;Console.WriteLine("PASS "+label);}
string Clean(string x)=>(x??"").Trim();
List<EventRecordEntry> Sanitize(List<EventRecordEntry> x)=>WeeklyEventDataImportOwner.SanitizeEventRecordEntries(x,Clean,Clean,Clean);
void Normalize(List<EventRecordEntry> x)=>WeeklyEventDataImportOwner.NormalizeEventRecordEntriesInPlace(x,Clean,Clean,Clean);
var selection=new WorldBulletinSelection {
 Major=new(){KingdomIds=new(){" A ","b"}},MajorFacts=new(){new(){KingdomIds=new(){"C","D","a",null}}},
 Minors=new(){new(){Events=new(){new(){KingdomIds=new(){" E ","f","B",""}}}}}
};
var ids=WeeklyReportArchivePolicy.CaptureKingdomIds(selection);
Check(ids.SequenceEqual(new[]{"A","b","C","D","E","f"}),"major, combined facts and minor nations captured without 3-nation limit");
Check(WeeklyReportArchivePolicy.CaptureKingdomIds(null).Count==0,"null selection is safe");
var records=new List<EventRecordEntry>{
 new(){EventId="weekly_report:kingdom:A:1",Title="old kingdom weekly",EventKind="kingdom",ScopeKingdomId="A",WeekIndex=1,CreatedDay=7,Summary="old weekly body"},
 new(){EventId="weekly_report:kingdom:A:2:brief",Title="brief",EventKind="kingdom",ScopeKingdomId="A",WeekIndex=2,CreatedDay=14,Summary="brief body"},
 new(){EventId="weekly_report:world:1",Title="world weekly",EventKind="world",ScopeKingdomId="",WeekIndex=1,CreatedDay=7,Summary="world weekly"}
};
string bulletinId="weekly_report:world:bulletin:10:15";
var stateOwner=new WorldBulletinStateOwner{State=new()};
int notices=0;bool metadataAtNotify=false;
stateOwner._port=new(){Records=()=>records,FindRecord=id=>records.FirstOrDefault(x=>x.EventId==id),ProductState=x=>x?.Summary??"",CurrentDate=()=>"day 15",NotifyProductChanged=(previous,x)=>metadataAtNotify=x.BulletinKingdomIds.Count==6,NotifyTimeline=()=>notices++};
stateOwner.UpsertWorldBulletinRecord(bulletinId,"world","","new bulletin","new summary","new bulletin body",15,ids);
Check(records.Count==4&&notices==1&&metadataAtNotify,"one canonical issue with metadata before timeline notification");
var display=new WeeklyEditorDisplayPort {ResolveKingdomDisplay=id=>id};
List<WeeklyReportBrowserEntryData> Project(string kind,string id,IReadOnlyDictionary<string,List<string>> legacy=null)=>WeeklyEditorProjection.BuildWeeklyReportBrowserEntries(display,records,kind,id,legacy);
var a=Project("kingdom","a");
Check(a.Count==3&&a[0].EventId==bulletinId&&a[0].BodyText=="new bulletin body","kingdom shows latest related bulletin above retained weekly and brief");
Check(Project("kingdom","E").Single().EventId==bulletinId,"minor-only kingdom sees bulletin");
Check(Project("kingdom","f").Single().EventId==bulletinId,"sixth related kingdom sees bulletin");
Check(Project("kingdom","unrelated").Count==0,"unrelated kingdom excluded");
Check(Project("world","").Count==4&&records.Count==4,"world archive retained, no record copies");
Check(Project("kingdom","A").Any(x=>x.EventId.EndsWith(":brief")),"existing kingdom brief retained");
var old=new EventRecordEntry{EventId="weekly_report:world:bulletin:9:15",EventKind="world",ScopeKingdomId="",Title="zz old",WeekIndex=2,CreatedDay=15,Summary="previous body",BulletinKingdomIds=null};
records.Add(old);
stateOwner.State.Layouts.Add(new(){EventId=old.EventId,KingdomIds=new(){" a ","B","b"}});
var fallback=stateOwner.SnapshotLegacyBulletinKingdomAssociations();
Check(fallback[old.EventId].SequenceEqual(new[]{"a","B"}),"legacy layout snapshot normalizes IDs");
Check(Project("kingdom","A",fallback).Count==4,"old null metadata recovers known layout associations");
Check(Project("kingdom","A",fallback).Take(2).Select(x=>x.EventId).SequenceEqual(new[]{bulletinId,old.EventId}),"same-day issues sort 10 before 9, not by title");
stateOwner.State.Layouts.Clear();
Check(Project("kingdom","A",stateOwner.SnapshotLegacyBulletinKingdomAssociations()).Count==3,"durable association works after layout cache eviction");
Check(Project("world","").Any(x=>x.EventId==old.EventId),"unrecoverable old association remains in world archive");
var latest=records.First(x=>x.EventId==bulletinId);
var conflict=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase){{bulletinId,new(){"wrong"}}};
Check(!WeeklyReportArchivePolicy.Matches(latest,"kingdom","wrong",conflict),"durable metadata takes precedence over legacy fallback");
Check(!WeeklyReportArchivePolicy.Matches(records[2],"kingdom","A",new Dictionary<string,List<string>>{{records[2].EventId,new(){"A"}}}),"regular world weekly is not auto-associated");
latest.BulletinKingdomIds.Add(" a ");
var imported=Sanitize(records);
Check(imported.First(x=>x.EventId==bulletinId).BulletinKingdomIds.Count==6,"import sanitation preserves and deduplicates metadata");
Check(!ReferenceEquals(imported.First(x=>x.EventId==bulletinId).BulletinKingdomIds,latest.BulletinKingdomIds),"sanitized association list is detached");
Normalize(records);
Check(latest.BulletinKingdomIds.Count==6&&old.BulletinKingdomIds!=null,"in-place save/load normalization preserves IDs and repairs null");
var store=new Dictionary<string,object>();string saveJson="";
CampaignWeeklyRecordPersistenceAdapter.SaveRecords(new MemoryStore(store),records,ref saveJson,Normalize);
Check(store.ContainsKey("_eventRecordEntries_v1__af_chunk_count")&&saveJson=="","existing chunked save key unchanged and temporary JSON released");
var loaded=new List<EventRecordEntry>();
CampaignWeeklyRecordPersistenceAdapter.LoadRecords(new MemoryStore(store,true),ref loaded,ref saveJson,Normalize);
Check(loaded.Count==records.Count&&loaded.First(x=>x.EventId==bulletinId).Summary=="new bulletin body","actual save/load adapter roundtrip keeps canonical bodies");
Check(loaded.First(x=>x.EventId==bulletinId).BulletinKingdomIds.SequenceEqual(WeeklyReportArchivePolicy.CaptureKingdomIds(selection)),"actual chunk save/load roundtrip preserves all nations");
string exported=JsonConvert.SerializeObject(loaded);
var merged=new List<EventRecordEntry>();
WeeklyEventDataImportOwner.ApplyRecords(new(){HasEventRecordsFile=true,EventRecords=JsonConvert.DeserializeObject<List<EventRecordEntry>>(exported)},true,ref merged,Sanitize);
Check(merged.Single(x=>x.EventId==bulletinId).BulletinKingdomIds.Count==6,"exported JSON reimport keeps bulletin association");
var legacyJson="[{\"EventId\":\"weekly_report:world:bulletin:1:1\",\"Title\":\"legacy\",\"EventKind\":\"world\",\"Summary\":\"legacy body\"}]";
var legacyRead=JsonConvert.DeserializeObject<List<EventRecordEntry>>(legacyJson);Normalize(legacyRead);
Check(legacyRead.Single().Summary=="legacy body"&&legacyRead.Single().BulletinKingdomIds.Count==0,"old JSON without optional field stays readable and unchanged");
Check(WeeklyReportArchivePolicy.KindLabel(bulletinId)=="即时快报"&&WeeklyReportArchivePolicy.PeriodLabel(bulletinId,2).Contains("第 10 期"),"bulletin labeled by issue not week");
Check(WeeklyReportArchivePolicy.KindLabel(WeeklyReportArchivePolicy.RecentId("A",2))=="王国近况"&&WeeklyReportArchivePolicy.KindLabel("weekly_report:kingdom:A:1")=="周报档案","brief and weekly labels distinguish sources");
Check(WeeklyReportArchivePolicy.IssueNumber("weekly_report:world:bulletin:bad:15")==0&&WeeklyReportArchivePolicy.IssueNumber("weekly_report:world:bulletin:99999999999999999999:15")==0,"malformed and overflowing issue numbers are safe");
Check(WeeklyReportArchivePolicy.PeriodLabel("x:bulletin:bad",-1)=="即时快报","malformed issue label avoids false week");
var countries=new List<WeeklyReportBrowserCountryData>{
 WeeklyEditorProjection.BuildWeeklyReportBrowserCountryData(display,"world","","all archives",true,records,fallback),
 WeeklyEditorProjection.BuildWeeklyReportBrowserCountryData(display,"kingdom","A","Kingdom A",false,records,fallback),
 WeeklyEditorProjection.BuildWeeklyReportBrowserCountryData(display,"kingdom","E","Kingdom E",false,records,fallback)
};
MyBehavior.Instance=new(){Countries=countries};
usingVM(null,"all archives","default bulletin mode selects world latest issues");
usingVM("A","Kingdom A","explicit country selection honored");
MyBehavior.BulletinEnabled=false;usingVM(null,"Kingdom A","weekly-only mode keeps previous country default");MyBehavior.BulletinEnabled=true;
void usingVM(string selected,string expected,string label){var vm=new TerminalWeeklyReportBrowserPopupVM(countries,selected,()=>{});Check(vm.SelectedCountryNameText==expected,label);Check(vm.ReportItems.First().EventId==bulletinId,"actual VM latest issue sorted first");Check(!vm.ReportItems.First().ShowViewFullReport,"completed bulletin has no extra generation button");Check(vm.ReportItems.First().WeekText.Contains("第 10 期"),"actual row displays issue label");vm.OnFinalize();}
var switching=new TerminalWeeklyReportBrowserPopupVM(countries,null,()=>{});
switching.CountryItems.Single(x=>x.CountryId=="E").ExecuteSelect();
Check(switching.SelectedCountryNameText=="Kingdom E"&&switching.ReportItems.Single().BodyText=="new bulletin body","actual country click switches current bulletin body");
switching.CountryItems.Single(x=>x.CountryId=="world").ExecuteSelect();
Check(switching.SelectedCountryNameText=="all archives"&&switching.ReportItems.First().EventId==bulletinId,"switch back to world retains latest canonical issue");
switching.OnFinalize();
int closed=0;var empty=new TerminalWeeklyReportBrowserPopupVM(new(),null,()=>closed++);Check(empty.ShowEmptyState&&empty.ReportItems.Count==0,"empty archive safe");empty.ExecuteClose();Check(closed==1,"close callback unchanged");empty.OnFinalize();
var timeline=WorldMessageTimelineUi.Replay();
var row=timeline.Single(x=>x.WeeklyReportEventId==bulletinId);
Check(timeline.Count==records.Count,"actual timeline has one row per canonical ID");
Check(row.Countries.Select(x=>x.CountryId).ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(new[]{"world_weekly","A","E"}),"actual timeline unions related nation and world filters");
Check(row.BodyText=="new bulletin body"&&row.CategoryLabel=="即时快报"&&row.Sequence==10,"timeline retains canonical body and issue sequence");
Check(timeline.IndexOf(row)<timeline.FindIndex(x=>x.WeeklyReportEventId==old.EventId),"timeline keeps newest same-day issue first");
Console.WriteLine($"PASS: {count} bulletin archive/save/terminal/timeline assertions; game and renderer stubbed, no live-game acceptance.");

var archiveSources=new List<EventRecordEntry>{
 new(){EventId="weekly_report:world:bulletin:1:8",EventKind="world",CreatedDay=8,Summary="issue 1 complete",BulletinKingdomIds=new(){"existing"}},
 new(){EventId="weekly_report:world:bulletin:2:13",Title="issue 2",EventKind="world",CreatedDay=13,Summary="issue 2 complete"},
 new(){EventId="weekly_report:world:bulletin:3:15",EventKind="world",CreatedDay=15,Summary="future issue"},
 new(){EventId="weekly_report:kingdom:2:A:brief",EventKind="kingdom",ScopeKingdomId="A",WeekIndex=2,CreatedDay=14,Title="A news",Summary="A complete regional text\nsecond paragraph",TagText="INTERNAL_STAB"},
 new(){EventId="weekly_report:kingdom:2:B:brief",EventKind="kingdom",ScopeKingdomId="B",WeekIndex=2,CreatedDay=14,Title="B news",Summary="B complete regional text"},
 new(){EventId="weekly_report:world:1",EventKind="world",WeekIndex=1,CreatedDay=7,Summary="week 1 full"},
 new(){EventId="weekly_report:kingdom:C:1",EventKind="kingdom",ScopeKingdomId="C",WeekIndex=1,CreatedDay=7,Title="C news",ShortSummary="C old short-only body"},
 new(){EventId="weekly_report:kingdom:6:D:brief",EventKind="kingdom",ScopeKingdomId="D",WeekIndex=6,CreatedDay=42,Title="D orphan",Summary="D preserved body"},
 new(){EventId="weekly_report:kingdom:6:E:brief",EventKind="kingdom",ScopeKingdomId="E",WeekIndex=6,CreatedDay=42,Title="E orphan",Summary="E preserved body"}
};
string originalJson=JsonConvert.SerializeObject(archiveSources);
var associations=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase){{"weekly_report:world:bulletin:2:13",new(){"legacy"}}};
var consolidated=WeeklyReportArchivePolicy.BuildArchiveSnapshot(archiveSources,associations);
Check(consolidated.Count==9&&consolidated.All(x=>!WeeklyReportArchivePolicy.IsRegionalSummary(x)),"legacy briefs become independent national weekly recent records");
var parent=consolidated.Single(x=>x.EventId=="weekly_report:world:bulletin:2:13");
Check(parent.Materials.Count==0&&parent.BulletinKingdomIds.SequenceEqual(new[]{"legacy"}),"legacy country materials are not appended or guessed as canonical issue associations");
Check(WeeklyReportArchivePolicy.BodyWithRegionalNews(parent)=="issue 2 complete","issue body contains only original front-page text");
var nearA=consolidated.Single(x=>x.EventId==WeeklyReportArchivePolicy.RecentId("A",2));
Check(nearA.Summary.Contains("A complete regional text")&&nearA.Summary.Contains("second paragraph")&&nearA.Summary.Contains("历史近况")&&!nearA.Summary.Contains("INTERNAL_STAB"),"historical near facts complete and marked without action tags");
Check(consolidated.Single(x=>x.EventId==WeeklyReportArchivePolicy.RecentId("C",1)).Summary.Contains("C old short-only body"),"short-only old entry preserved in country recent");
Check(consolidated.Single(x=>x.EventId==WeeklyReportArchivePolicy.RecentId("D",6)).Summary.Contains("D preserved body"),"orphan old brief has own reading target");
Check(JsonConvert.SerializeObject(archiveSources)==originalJson,"legacy projection leaves every original record intact");
Check(JsonConvert.SerializeObject(WeeklyReportArchivePolicy.BuildArchiveSnapshot(archiveSources,associations))==JsonConvert.SerializeObject(consolidated),"reopening archives deterministic and non-accumulating");
parent.Materials.Add(new(){MaterialType=WeeklyReportArchivePolicy.RegionalMaterialType,KingdomId="A",Label="A第99999周近况",ActionDay=14,SnapshotText="A complete regional text\nsecond paragraph",SourceStableKeys=new(){archiveSources[3].EventId}});
parent.Materials.Add(new(){MaterialType=WeeklyReportArchivePolicy.RegionalMaterialType,KingdomId="A",Label="wrong title",ActionDay=14,SnapshotText="additional preserved historic fact"});
parent.Materials.Add(new(){MaterialType=WeeklyReportArchivePolicy.RegionalMaterialType,KingdomId="A",Label="wrong title",ActionDay=14,SnapshotText="second keyless preserved fact"});
var both=WeeklyReportArchivePolicy.BuildArchiveSnapshot(consolidated,associations);
var bothA=both.Single(e=>e.EventId==nearA.EventId);
Check(bothA.Materials.Count==3&&bothA.Summary.Contains("additional preserved historic fact")&&bothA.Summary.Contains("second keyless"),"standalone and attached same sources dedup while keyless historic facts preserved");
Check(!bothA.CreatedDate.Contains("99999")&&parent.Materials.Count==3,"legacy labels normalized without changing originals");
var noMaterials=new EventRecordEntry{EventId=WeeklyReportArchivePolicy.RecentId("F",4),EventKind="kingdom",ScopeKingdomId="F",WeekIndex=4,Summary="fallback historical near",CreatedDay=28};
Check(WeeklyReportArchivePolicy.BuildArchiveSnapshot(new(){noMaterials}).Single().Summary.Contains("fallback historical near"),"recent record lacking materials does not lose existing prose");
var mergedCountries=new List<WeeklyReportBrowserCountryData>{
 WeeklyEditorProjection.BuildWeeklyReportBrowserCountryData(display,"world","","all archives",true,both,associations),
 WeeklyEditorProjection.BuildWeeklyReportBrowserCountryData(display,"kingdom","A","Kingdom A",false,both,associations)
};
MyBehavior.Instance=new(){Countries=mergedCountries};
string encyclopedia=null;
var openVm=new TerminalWeeklyReportBrowserPopupVM(mergedCountries,"A",()=>{},link=>encyclopedia=link);
var openItem=openVm.ReportItems.Single();openVm.ListScrollPosition=47;
Check(openItem.EntryKind=="recent"&&openItem.OpenReportText=="阅读近况"&&!openItem.ShowViewFullReport,"recent card routes to independent reader");
openItem.ExecuteOpenReport();
Check(openVm.IsReading&&openVm.ReaderBodyText.Contains("second paragraph")&&MyBehavior.Instance.Opens==0,"near card reads full own body without opening front page");
openVm.ExecuteOpenEncyclopediaLink("hero:test");Check(encyclopedia=="hero:test","reader encyclopedia link callback retained");
openVm.ListScrollPosition=0; // Native hidden-list measurement may reset its scrollbar.
openVm.ExecuteReturnToList();Check(!openVm.IsReading&&openVm.ListScrollPosition==47,"reader back retains country, type, page and scroll");
openVm.OnFinalize();openItem.ExecuteOpenReport();Check(!openVm.IsReading&&MyBehavior.Instance.Opens==0,"finalized row callback invalidated");
var bulletinVm=new TerminalWeeklyReportBrowserPopupVM(mergedCountries,"world",()=>{});bulletinVm.ExecuteFilterBulletin();
var actualIssue=bulletinVm.ReportItems.Single(e=>e.EventId==parent.EventId);actualIssue.ExecuteOpenReport();
Check(MyBehavior.Instance.Opens==1&&MyBehavior.Instance.OpenedId==parent.EventId&&!bulletinVm.IsReading,"bulletin opens exact original scroll ID");
SaveRuntimeGuard.AdvanceGeneration("archive-load-test");actualIssue.ExecuteOpenReport();bulletinVm.ExecuteNextPage();
Check(MyBehavior.Instance.Opens==1,"loaded save invalidates old open and paging callbacks");bulletinVm.OnFinalize();
store=new();saveJson="";CampaignWeeklyRecordPersistenceAdapter.SaveRecords(new MemoryStore(store),both,ref saveJson,Normalize);
loaded=new();CampaignWeeklyRecordPersistenceAdapter.LoadRecords(new MemoryStore(store,true),ref loaded,ref saveJson,Normalize);
Check(loaded.Single(e=>e.EventId==nearA.EventId).Materials.Count==3,"country fact keys and bodies survive actual chunk save/load");
merged=new();WeeklyEventDataImportOwner.ApplyRecords(new(){HasEventRecordsFile=true,EventRecords=loaded},true,ref merged,Sanitize);
Check(merged.Single(e=>e.EventId==nearA.EventId).ScopeKingdomId=="A"&&merged.Single(e=>e.EventId==nearA.EventId).Materials.Count==3,"actual import keeps near national attribution and stable keys");
var year=new List<EventRecordEntry>();
for(int week=1;week<=52;week++) { year.Add(new(){EventId=$"weekly_report:world:bulletin:{week}:{week*7-1}",EventKind="world",CreatedDay=week*7-1,WeekIndex=week-1,Summary="issue "+week});
 for(int nation=0;nation<10;nation++) year.Add(new(){EventId=$"weekly_report:kingdom:{week}:K{nation}:brief",EventKind="kingdom",ScopeKingdomId="K"+nation,WeekIndex=week,CreatedDay=week*7,Summary=$"week {week} nation {nation}"}); }
var yearArchive=WeeklyReportArchivePolicy.BuildArchiveSnapshot(year);
Check(yearArchive.Count==572&&year.Count==572&&yearArchive.Count(e=>WeeklyReportArchivePolicy.IsRecent(e.EventId))==520,"52 weeks of historical facts retained as country-week near records without deleting raw records");
var yearCountries=new List<WeeklyReportBrowserCountryData>{WeeklyEditorProjection.BuildWeeklyReportBrowserCountryData(display,"world","","all",true,yearArchive)};
MyBehavior.Instance=new(){Countries=yearCountries};
var paged=new TerminalWeeklyReportBrowserPopupVM(yearCountries,"world",()=>{});
Check(paged.ReportItems.Count==12&&paged.PageText=="1 / 48","large archive binds exactly current 12-card page");
var retired=paged.ReportItems.First();paged.ExecuteNextPage();retired.ExecuteOpenReport();
Check(paged.PageText=="2 / 48"&&!paged.IsReading,"retired page callbacks invalidated");
paged.ListScrollPosition=63;paged.ReportItems.First().ExecuteOpenReport();paged.ExecuteReturnToList();
Check(paged.PageText=="2 / 48"&&paged.ListScrollPosition==63,"second-page back preserves position");
paged.ExecuteFilterBulletin();Check(paged.ReportItems.All(e=>e.EntryKind=="bulletin")&&paged.PageText=="1 / 5","type filtering changes actual entries and page count");
paged.ExecuteFilterRecent();Check(paged.ReportItems.All(e=>e.EntryKind=="recent")&&paged.PageText=="1 / 44","near filter includes independent country records");
paged.ExecuteFilterWeekly();Check(paged.ReportItems.Count==0&&paged.ShowEmptyState,"no synthetic empty cards in missing type");paged.OnFinalize();
foreach(int size in new[]{0,1,4,5,12,401}) {
 var data=new WorldBulletinPanelData{BodyText="full body",Minors=Enumerable.Range(0,size).Select(i=>new KeyValuePair<string,string>("news",i+":"+new string('x',700))).ToList()};
 var panel=new WorldBulletinPanelVM(data,16,()=>{},x=>{});data.Minors.Clear();
 Check(panel.LeftMinors.Count+panel.RightMinors.Count==size&&panel.HasMinors==(size>0),"existing scroll facts retained without paging or empty placeholders size="+size);
 Check(panel.LeftMinors.Concat(panel.RightMinors).All(e=>e.Text.Length>700),"long existing real shorts preserved size="+size);
}
Console.WriteLine($"PASS: {count} production archive/save/UI assertions; game, formatter and renderer stubbed, live acceptance NOT_RUN.");

var completion=new TaskCompletionSource<bool>();
var weeklyData=new WeeklyReportBrowserEntryData {EventId="weekly_report:kingdom:8:A",ArchiveKind="weekly",OpenTargetId="weekly_report:kingdom:8:A",Title="weekly title",BodyText="original weekly body",CreatedDay=56};
var weeklyCountries=new List<WeeklyReportBrowserCountryData>{new(){CountryId="A",DisplayName="A",Reports=new(){weeklyData}}};
MyBehavior.Instance=new(){Countries=weeklyCountries,FullReport=id=>completion.Task};
var reading=new TerminalWeeklyReportBrowserPopupVM(weeklyCountries,"A",()=>{});
reading.ExecuteFilterWeekly();reading.ListScrollPosition=35;reading.ReportItems.Single().ExecuteOpenReport();
Check(reading.IsReading&&reading.ReaderCanGenerateFull&&MyBehavior.Instance.Opens==0,"weekly card opens own saved prose and offers missing full generation");
reading.ExecuteReaderGenerateFull();Check(!reading.ReaderCanGenerateFull,"in-flight full report is not requested twice");
weeklyData.BodyText="completed full weekly body";weeklyData.HasFullReport=true;completion.SetResult(true);reading.Tick();
Check(reading.IsReading&&reading.ReaderBodyText.Contains("completed full weekly body")&&!reading.ReaderCanGenerateFull,"main-thread completion refreshes current reader without changing its type");
reading.ExecuteReturnToList();Check(reading.IsWeeklySelected&&reading.ListScrollPosition==35,"full generation preserves filter and list scroll");
reading.OnFinalize();reading.ExecuteClose();
int suspended=0;bool alive=true;var manager=TaleWorlds.CampaignSystem.Campaign.Current.EncyclopediaManager;
EncyclopediaEntityLinkNavigationCoordinator.Request("hero:expired-close",()=>suspended++,()=>{},()=>alive);
alive=false;EncyclopediaEntityLinkNavigationCoordinator.ProcessPending();
Check(manager.Opens==0&&suspended==0,"closing before deferred encyclopedia callback cancels the actual navigation");
long navGeneration=SaveRuntimeGuard.CaptureGeneration();
EncyclopediaEntityLinkNavigationCoordinator.Request("hero:expired-load",()=>suspended++,()=>{},()=>SaveRuntimeGuard.IsCurrentGeneration(navGeneration));
SaveRuntimeGuard.AdvanceGeneration("pending-navigation-load");EncyclopediaEntityLinkNavigationCoordinator.ProcessPending();
Check(manager.Opens==0&&suspended==0,"loading before deferred link callback cancels navigation");
EncyclopediaEntityLinkNavigationCoordinator.Request("event:hero:valid",()=>suspended++,()=>{},()=>true);
EncyclopediaEntityLinkNavigationCoordinator.ProcessPending();
Check(manager.Opens==1&&manager.Link=="hero:valid"&&suspended==1,"current encyclopedia navigation suspends once and opens normalized original target");
Console.WriteLine($"PASS: {count} final archive/save/UI/navigation assertions (native rendering and encyclopedia lifecycle stubbed).");
