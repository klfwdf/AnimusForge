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
Check(Project("world","").Count==2&&records.Count==4,"world archive retained, no record copies");
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
Check(WeeklyReportArchivePolicy.KindLabel("weekly_report:kingdom:A:2:brief")=="王国局势提要"&&WeeklyReportArchivePolicy.KindLabel("weekly_report:kingdom:A:1")=="周报档案","brief and weekly labels distinguish sources");
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
Check(consolidated.Count==5&&consolidated.All(x=>!WeeklyReportArchivePolicy.IsRegionalSummary(x)),"legacy regional entries consolidate into issue rows, not standalone summaries");
var parent=consolidated.Single(x=>x.CreatedDay==13);
Check(parent.Materials.Count==2&&parent.BulletinKingdomIds.ToHashSet().SetEquals(new[]{"A","B","legacy"}),"two nations attach to latest covered issue and preserve layout associations");
Check(consolidated.Single(x=>x.CreatedDay==15).Materials.Count==0&&consolidated.Single(x=>x.CreatedDay==8).Materials.Count==0,"regional news does not attach to future or earlier replaced issue");
string combinedBody=WeeklyReportArchivePolicy.BodyWithRegionalNews(parent);
Check(combinedBody.Contains("issue 2 complete")&&combinedBody.Contains("A complete regional text second paragraph")&&combinedBody.Contains("B complete regional text")&&!combinedBody.Contains("INTERNAL_STAB"),"combined issue includes all visible text, never internal tags");
var weekOne=consolidated.Single(x=>x.EventId=="weekly_report:world:1");
Check(WeeklyReportArchivePolicy.BodyWithRegionalNews(weekOne).Contains("C old short-only body")&&weekOne.BulletinKingdomIds.SequenceEqual(new[]{"C"}),"short-only weekly merges into exact matching world week");
var orphan=consolidated.Single(x=>x.EventId=="weekly_report:world:bulletin:archive:6");
Check(orphan.Materials.Count==2&&WeeklyReportArchivePolicy.BodyWithRegionalNews(orphan).Contains("D preserved body")&&WeeklyReportArchivePolicy.BodyWithRegionalNews(orphan).Contains("E preserved body"),"orphan summaries share one complete browseable edition per week");
Check(JsonConvert.SerializeObject(archiveSources)==originalJson,"archive projection never modifies original legacy records");
Check(JsonConvert.SerializeObject(WeeklyReportArchivePolicy.BuildArchiveSnapshot(archiveSources,associations))==JsonConvert.SerializeObject(consolidated),"repeated archive opens are deterministic without accumulating messages");
WeeklyReportArchivePolicy.AttachRegionalNews(parent,WeeklyReportArchivePolicy.RegionalMaterial(archiveSources[3]));
Check(parent.Materials.Count==2,"repeated regional source replaces instead of duplicates");
var mergedCountries=new List<WeeklyReportBrowserCountryData>{
 WeeklyEditorProjection.BuildWeeklyReportBrowserCountryData(display,"world","","all archives",true,consolidated,associations),
 WeeklyEditorProjection.BuildWeeklyReportBrowserCountryData(display,"kingdom","A","Kingdom A",false,consolidated,associations)
};
MyBehavior.Instance=new(){Countries=mergedCountries};
var openVm=new TerminalWeeklyReportBrowserPopupVM(mergedCountries,"A",()=>{});
var openItem=openVm.ReportItems.Single();
Check(openItem.ShowOpenReport&&openItem.OpenReportText=="打开快报"&&!openItem.ShowViewFullReport,"completed issue has reopen button instead of regenerate");
openItem.ExecuteOpenReport();
Check(MyBehavior.Instance.Opens==1&&MyBehavior.Instance.OpenedId==parent.EventId,"actual VM reopens selected canonical issue ID");
openVm.OnFinalize();openItem.ExecuteOpenReport();
Check(MyBehavior.Instance.Opens==1,"retired archive rejects cached row callback");
var staleVm=new TerminalWeeklyReportBrowserPopupVM(mergedCountries,"A",()=>{});
SaveRuntimeGuard.AdvanceGeneration("archive-load-test");staleVm.ReportItems.Single().ExecuteOpenReport();
Check(MyBehavior.Instance.Opens==1,"save load rejects old archive open callbacks");staleVm.OnFinalize();
var mergedTimeline=WorldMessageTimelineUi.Replay();
Check(mergedTimeline.Count==5&&mergedTimeline.Single(x=>x.WeeklyReportEventId==parent.EventId).BodyText.Contains("A complete regional text"),"timeline consumes one merged issue per ID with regional body");
store=new();saveJson="";
CampaignWeeklyRecordPersistenceAdapter.SaveRecords(new MemoryStore(store),new(){parent},ref saveJson,Normalize);
loaded=new();CampaignWeeklyRecordPersistenceAdapter.LoadRecords(new MemoryStore(store,true),ref loaded,ref saveJson,Normalize);
Check(loaded.Single().Materials.Count==2&&WeeklyReportArchivePolicy.BodyWithRegionalNews(loaded.Single())==combinedBody,"actual chunked save/load retains attached regional text");
merged=new();WeeklyEventDataImportOwner.ApplyRecords(new(){HasEventRecordsFile=true,EventRecords=loaded},true,ref merged,Sanitize);
Check(WeeklyReportArchivePolicy.BodyWithRegionalNews(merged.Single())==combinedBody,"actual import sanitation retains attached messages");
Console.WriteLine($"PASS: {count} total archive/save/UI assertions including consolidation and reopen.");
var year=new List<EventRecordEntry>();
for(int week=1;week<=52;week++)
{
    year.Add(new(){EventId=$"weekly_report:world:bulletin:{week}:{week*7-1}",EventKind="world",CreatedDay=week*7-1,WeekIndex=week-1,Summary="issue "+week});
    for(int nation=0;nation<10;nation++)
        year.Add(new(){EventId=$"weekly_report:kingdom:{week}:K{nation}:brief",EventKind="kingdom",ScopeKingdomId="K"+nation,WeekIndex=week,CreatedDay=week*7,Summary=$"week {week} nation {nation}"});
}
var yearArchive=WeeklyReportArchivePolicy.BuildArchiveSnapshot(year);
Check(yearArchive.Count==52&&yearArchive.All(e=>e.Materials.Count==10)&&year.Count==572,"52 weeks and 520 regional summaries become 52 issues without deleting original data");
Console.WriteLine($"PASS: {count} final archive/save/UI assertions, including year-long archive consolidation.");
var countryA=WeeklyEditorProjection.BuildWeeklyReportBrowserEntries(display,consolidated,"kingdom","A",associations).Single();
var countryB=WeeklyEditorProjection.BuildWeeklyReportBrowserEntries(display,consolidated,"kingdom","B",associations).Single();
Check(countryA.BodyText.Contains("A complete regional text")&&!countryA.BodyText.Contains("B complete regional text")&&!countryA.BodyText.Contains("issue 2 complete"),"country A shows only its own attached news");
Check(countryA.Title=="A news"&&countryB.Title=="B news"&&countryA.BodyText!=countryB.BodyText,"country titles and bodies differ without duplicating issue records");
Check(WeeklyEditorProjection.BuildWeeklyReportBrowserEntries(display,consolidated,"world","",associations).First(x=>x.EventId==parent.EventId).BodyText==combinedBody,"world archive retains complete original issue and all national news");
foreach(int size in new[]{0,1,4,5,12,401})
{
    var data=new WorldBulletinPanelData { BodyText="full body",Minors=Enumerable.Range(0,size).Select(i=>new KeyValuePair<string,string>("news",i+":"+new string('x',700))).ToList() };
    int closes=0;string link=null;
    var vm=new WorldBulletinPanelVM(data,16,()=>closes++,x=>link=x);
    data.Minors.Clear(); // Opening freezes the page source.
    var seen=new List<string>();
    vm.ExecutePreviousMinorPage();
    Check(!vm.CanPreviousMinorPage&&vm.HasMinors==(size>0)&&vm.ShowMinorPagination==(size>4),"page start and empty states size="+size);
    do {
        Check(vm.LeftMinors.Count<=2&&vm.RightMinors.Count<=2&&vm.LeftMinors.Count+vm.RightMinors.Count<=4,"bounded page size="+size);
        seen.AddRange(vm.LeftMinors.Concat(vm.RightMinors).Select(x=>x.Text));
        if(!vm.CanNextMinorPage)break;
        vm.MinorScrollPosition=45;
        vm.ExecuteNextMinorPage();
        Check(vm.MinorScrollPosition==0,"page change resets scroll size="+size);
    }while(true);
    string last=vm.MinorPageText;vm.ExecuteNextMinorPage();
    Check(vm.MinorPageText==last&&seen.Count==size&&seen.Distinct().Count()==size&&seen.All(x=>x.Length>700),"all messages once, no long-text truncation and last-page clamp size="+size);
    for(int i=0;i<size+1;i++)vm.ExecutePreviousMinorPage();
    Check(!vm.CanPreviousMinorPage&&vm.MinorPageText.StartsWith("1 /"),"backwards paging stops at first size="+size);
    if(size>0) {vm.LeftMinors[0].ExecuteOpenEncyclopediaLink("hero:test");Check(link=="hero:test","page keeps encyclopedia command size="+size);}
    vm.ExecuteClose();Check(closes==1,"close callback retained size="+size);
}
Console.WriteLine($"PASS: {count} final production archive/projection/panel assertions; game, formatter and renderer remain stubbed.");
