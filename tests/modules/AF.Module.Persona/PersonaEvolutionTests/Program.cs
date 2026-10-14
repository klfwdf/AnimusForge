using AnimusForge;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
int count=0;
void Check(bool value,string name){if(!value)throw new Exception(name);count++;}
Hero HeroOf(string id)=>new(){StringId=id,Name=id,EncyclopediaText="A cautious landholder."};
WorldBulletinEvent Event(string id,string hero,string kind="settlement_siege")=>new(){Key=id,Kind=kind,Hour=0,GameDate="day0",Sentence="A captured B's castle.",Detail="B was the previous owner.",Participants=new(){new(){HeroId=hero}}};
var ids=NpcPersonaTextRules.AllowedTraitIdsForPrompt;
foreach(string lang in new[]{"",".en"})
{
    PersonaPromptTexts.Values=JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(Path.Combine(args[0],"content/modules/AF.Module.Persona/ModuleData/PersonaPromptTexts"+lang+".json")));
    var host=new MyBehavior(); MyBehavior.Instance=host;DuelSettings.Current=new();MyBehavior.NewsEnabled=true;
    Hero a=HeroOf("a"), b=HeroOf("b"), parent=HeroOf("parent"), outsider=HeroOf("outsider");
    b.Father=parent;parent.Children.Add(b);
    MemoryEntityIdentityBannerlordAdapter.Heroes=new(){{"a",a},{"b",b},{"parent",parent},{"outsider",outsider}};
    var p=host.Profile("b");p.Description="original";p.VoiceId="voice";p.Values.Add("protect family");
    int calls=ConfiguredChatApplicationAdapter.Calls;
    host.Tick();Check(ConfiguredChatApplicationAdapter.Calls==calls,"idle no call");
    int settingsReads=DuelSettings.Reads;
    var idleTimer=System.Diagnostics.Stopwatch.StartNew();
    long idleBytes=GC.GetAllocatedBytesForCurrentThread();
    for(int idle=0;idle<250000;idle++)host.Tick(false);
    idleBytes=GC.GetAllocatedBytesForCurrentThread()-idleBytes;
    idleTimer.Stop();
    Check(DuelSettings.Reads==settingsReads&&ConfiguredChatApplicationAdapter.Calls==calls,"250000 idle checks read no settings and request no tokens");
    Console.WriteLine("IDLE 250000 checks ms="+idleTimer.Elapsed.TotalMilliseconds+" allocatedBytes="+idleBytes+"; managed fixture, not live game timing");
    host.Setting(false);host.Capture(Event("disabled","b"));host.Tick();Check(host.Queued==0,"off no candidates");
    host.Setting(true);host.Capture(Event("irrelevant","b","war_declared"));Check(host.Queued==0,"irrelevant event filtered");
    host.Capture(Event("one","b"));host.Capture(Event("one","b"));host.Capture(Event("two","b"));
    Check(host.Queued==2 && p.Evolution.Pending.Count==2,"self and parent, merged duplicate events; no global heroes");
    MyBehavior.NotifyPersonaEvolutionSettingChanged(new DuelSettings());
    Check(host.Queued==2 && p.Evolution.Pending.Count==2,"loading a separate settings preset cannot retire runtime work");
    Check(host.Profile("outsider").Evolution==null,"unrelated hero never enqueued");
    host.Hour=.5;host.Tick();Check(ConfiguredChatApplicationAdapter.Calls==calls,"one hour merge wait");
    host.Tick();host.Hour=1;host.Tick();
    Check(ConfiguredChatApplicationAdapter.Calls==calls+1,"start one request");
    Check(JObject.Parse(ConfiguredChatApplicationAdapter.Input)["events"].Count()==2,"one NPC request merges facts");
    host.Tick(false);Check(ConfiguredChatApplicationAdapter.Calls==calls+1,"inflight limits concurrency");
    await host.Finish("{\"changed\":true,\"motivations\":[\"Recover the family castle\"],\"change_reason\":\"Loss of the castle changed his priorities.\"}");
    Check(p.Motivations.Single()=="Recover the family castle"&&p.Description=="original"&&p.VoiceId=="voice"&&p.Values.Single()=="protect family","patch writes only requested fields");
    Check(p.RecentChangeReason.Contains("castle")&&p.Evolution.Pending.Count==0,"reason saved and facts consumed");
    host.Tick(false);Check(ConfiguredChatApplicationAdapter.Calls==calls+1,"real time throttle");
    host.Tick();await host.Finish("{\"changed\":false}");Check(string.IsNullOrEmpty(host.Profile("parent").RecentChangeReason),"unchanged parent need not seek revenge");
    host.Tick();host.Tick();Check(host.Queued==0,"queue drains");
    host.Capture(Event("one","b"));Check(host.Queued==0,"completed event not repeated");
    host.Capture(Event("three","b"));host.Tick();p.Motivations=new(){"player edit"};
    await host.Finish("{\"changed\":true,\"motivations\":[\"old overwrite\"],\"change_reason\":\"old\"}");
    Check(p.Motivations.Single()=="player edit","inflight result cannot overwrite manual edit");
    host.Tick();await host.Finish("{\"changed\":false}");host.Tick();host.Tick();
    host.Capture(Event("four","b"));host.Tick();host.Setting(false);host.Setting(true);
    await host.Finish("{\"changed\":true,\"motivations\":[\"disabled overwrite\"],\"change_reason\":\"old\"}");
    Check(p.Motivations.Single()=="player edit"&&p.Evolution.Pending.Count==0&&host.Queued==0,"off/on retires in-flight epoch");
    host.Capture(Event("five","b"));host.Tick();SaveRuntimeGuard.Generation++;
    await host.Finish("{\"changed\":true,\"motivations\":[\"wrong save\"],\"change_reason\":\"old\"}");
    Check(p.Motivations.Single()=="player edit","old save callback discarded");
    host.Reload();host.Tick();await host.Finish("{\"changed\":false}");
    Check(p.Evolution.Pending.Count==0,"save requeue handles pending once");
    host.Setting(false);host.Setting(true);
    host.Capture(Event("bad","b"));host.Tick();await host.Finish("{\"changed\":true,\"traits\":[{\"id\":\"invalid\",\"strength\":99}],\"change_reason\":\"bad\"}");
    Check(p.Evolution.Pending.Count==1&&p.Evolution.Attempts==1,"bad result leaves profile and permits one retry");
    host.Tick();await host.Finish("{\"changed\":false}"); // other candidate fairly rotates
    host.Tick();await host.Finish("invalid");
    Check(p.Evolution.Pending.Count==0&&p.Motivations.Single()=="player edit","second failure stops retry and retains original");
}
var profile=new MyBehavior.NpcPersonaProfile{Description="body",VoiceId="voice",Motivations=new(){"old revenge"},SpeechStyle="harsh",RecentChangeReason="previous"};
foreach(string invalid in new[]{
 "{\"changed\":true,\"description\":\"overwrite\",\"change_reason\":\"x\"}",
 "{\"changed\":true,\"motivations\":null,\"change_reason\":\"x\"}",
 "{\"changed\":true,\"traits\":[{\"id\":\"bold\",\"strength\":4}],\"change_reason\":\"x\"}",
 "{\"changed\":true,\"motivations\":[\"a\",\"b\",\"c\",\"d\"],\"change_reason\":\"x\"}",
 "{\"changed\":true,\"speech_style\":42,\"change_reason\":\"x\"}",
 "{\"changed\":true,\"motivations\":[\"new\"]}",
 "{\"changed\":true,\"motivations\":[\"new\"],\"motivations\":[],\"change_reason\":\"x\"}",
 "{\"changed\":true,\"traits\":[{\"id\":\"bold\",\"strength\":2},{\"id\":\"bold\",\"strength\":1}],\"change_reason\":\"x\"}"})
 Check(!PersonaEvolutionPolicy.TryParse(invalid,ids,out _),"reject malformed/unknown patch atomically");
Check(PersonaEvolutionPolicy.TryParse("{\"changed\":true,\"motivations\":[],\"speech_style\":\"\",\"change_reason\":\"The feud ended.\"}",ids,out var clear),"explicit clear valid");
Check(PersonaEvolutionPolicy.Apply(profile,clear)&&profile.Motivations.Count==0&&profile.SpeechStyle==""&&profile.Description=="body","resolved motivation can disappear");
Check(PersonaEvolutionPolicy.TryParse("{\"changed\":true,\"traits\":[{\"id\":\"cautious\",\"strength\":3}],\"change_reason\":\"Defeat taught caution.\"}",ids,out var traits)&&PersonaEvolutionPolicy.Apply(profile,traits)&&profile.Traits.Single().Strength==3,"trait strength change");
var original=PersonaEvolutionPolicy.Snapshot(profile);profile.Description=new string('中',3000);
var request=PersonaEvolutionPolicy.RequestPersona(PersonaEvolutionPolicy.Snapshot(profile));
Check(profile.Description.Length==3000&&((string)request["description"]).Length==1000,"request budget preserves stored full input");
Check(!PersonaEvolutionPolicy.IsCurrent(profile,original),"snapshot detects body edit");
profile.SchemaVersion=3;profile.Evolution=new();
for(int i=0;i<10;i++)PersonaEvolutionPolicy.Enqueue(profile.Evolution,new(){Key="event"+i,Hour=0,Facts=new string('中',1000),Connection="child"});
Check(PersonaEvolutionPolicy.TakeBatch(profile.Evolution,1).Length==4,"batch cap four");
var state=new PersonaProfileStateOwner();state.Save("test",profile,(_,_)=>{});
var persistence=new CampaignPersonaPersistenceAdapter(state);var store=new Store{IsSaving=true};
persistence.Save(store,(_,_)=>true);
var raw=(Dictionary<string,string>)store.Values["_npcPersonaProfiles_v1"];
Check(raw.Values.All(x=>System.Text.Encoding.UTF8.GetByteCount(x)<=12000),"profile evolution uses bounded UTF8 chunks");
store.IsSaving=false;state.ResetForCurrentSave();persistence.Load(store);
var loaded=state.Get("test",false);
Check(loaded.Description==profile.Description&&loaded.RecentChangeReason==profile.RecentChangeReason&&loaded.Evolution.Pending.Count==10&&loaded.Evolution.Seen.Count==10,"full profile pending/reason roundtrip through actual persistence adapter");
var legacy=JsonConvert.DeserializeObject<MyBehavior.NpcPersonaProfile>("{\"Description\":\"legacy\",\"SchemaVersion\":3}");
Check(legacy.Description=="legacy"&&legacy.Evolution==null&&legacy.RecentChangeReason==null,"old profile JSON retains defaults");
var imported=JsonConvert.DeserializeObject<MyBehavior.NpcPersonaProfile>(JsonConvert.SerializeObject(profile));
var importedProfiles=new Dictionary<string,MyBehavior.NpcPersonaProfile>();
PersonaImportOwner.ApplySingleProfile(ref importedProfiles,"test",imported);
Check(imported.Evolution==null&&imported.RecentChangeReason==profile.RecentChangeReason,"external persona import keeps content but drops another campaign's pending work");
Console.WriteLine("PASS "+count+" persona evolution assertions; actual policy, host, DTO, owner and save adapter; game/provider/dispatcher are fixtures.");
