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
