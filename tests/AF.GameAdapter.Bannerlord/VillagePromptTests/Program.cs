using System.Text.Json;
using System.Text.RegularExpressions;
using AnimusForge;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using V = AnimusForge.VillageAftermathBehavior;
int checks=0;
void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
bool HasDirective(string s)=>Regex.IsMatch(s,@"\[(VILLAGE_ACTION|VILLAGE_DISPOSE|ACTION|TOWN_ACTION|CASTLE_ACTION):",RegexOptions.IgnoreCase);
foreach(bool english in new[]{false,true}) {
 var text=TownPromptCatalogResources.Get(english);
 var foreign=VillageDispositionPromptProfile.BuildScenePrompt("village {mercy} [ACTION:bad]",false,true,true,text);
 var governor=VillageDispositionPromptProfile.BuildScenePrompt("test",false,false,true,text);
 var guest=VillageDispositionPromptProfile.BuildScenePrompt("test",true,false,false,text);
 Check(foreign.Contains(text.GetRuntimeText("VillageScene_Foreign"))&&!foreign.Contains(text.GetRuntimeText("VillageScene_FriendlyGovernor")),"foreign branch only");
 Check(governor.Contains(text.GetRuntimeText("VillageScene_FriendlyGovernor"))&&!governor.Contains(text.GetRuntimeText("VillageScene_Foreign")),"governor without invented victory");
 Check(guest.Contains(text.GetRuntimeText("VillageScene_FriendlyVisitor"))&&guest.Contains(text.GetRuntimeText("VillageScene_MercyApplied")),"guest and actual mercy");
 Check(!HasDirective(foreign+governor+guest),"main prose has no executable directive");
 Check(foreign.Contains("{mercy}")&&!foreign.Contains("[ACTION:bad]"),"inserted village name is not recursively parsed; tags stripped");
 foreach(var role in Enum.GetValues<VillageDispositionSpeakerRole>()) {
  Check(!HasDirective(VillageDispositionPromptProfile.BuildRoleContext(role,text)),"role prose no tags "+role);
  string post=VillageDispositionPromptProfile.BuildPostprocessContext("v",role,false,false,false,false,text);
  Check(post.Contains("FriendlyVisitor")&&post.Contains(role.ToString())&&post.Contains("False"),"postprocess facts bound "+role);
 }
 Check(VillageDispositionPromptProfile.BuildRoleContext((VillageDispositionSpeakerRole)999,text)==text.GetRuntimeText("VillageScene_RoleUnknown"),"undefined role fails closed");
 var session=new VillageDispositionDialogueSession();
 session.Begin("v",english,text);
 var source=session.GetOrCreate("v","a","A",VillageDispositionSpeakerRole.Villager,"AF persona","AF background");
 Check(source.Personality=="AF persona"&&source.Background=="AF background","supplied AF data wins");
 Check(ReferenceEquals(source,session.GetOrCreate("v","a","A",VillageDispositionSpeakerRole.Villager,"changed","changed")),"no reroll during visit");
 var ordinary=session.GetOrCreate("v","b","B",VillageDispositionSpeakerRole.Villager,"","");
 var soldier=session.GetOrCreate("v","c","C",VillageDispositionSpeakerRole.PlayerTroop,"","");
 Check(text.SpeakerVoiceFragments.Where(p=>p.Key.StartsWith("civilian_temperament_")).Any(p=>p.Value==ordinary.Personality),"civilian uses town temperament fragments");
 Check(text.SpeakerVoiceFragments.Where(p=>p.Key.StartsWith("soldier_temperament_")).Any(p=>p.Value==soldier.Personality),"soldier uses town temperament fragments");
 Check(ordinary.Rhythm.Length>0&&ordinary.Concern.Length>0&&ordinary.Background=="","local voice without invented life history");
 foreach(var role in new[]{VillageDispositionSpeakerRole.VillageNotable,VillageDispositionSpeakerRole.PlayerCompanion,VillageDispositionSpeakerRole.Other}) {
  var hero=session.GetOrCreate("v","hero"+role,"Hero",role,"","");
  Check(hero.Personality==""&&hero.Rhythm==""&&hero.Concern=="","no invented hero/notable persona "+role);
 }
 Check(session.GetOrCreate("elsewhere","a","x",VillageDispositionSpeakerRole.Villager,"","")==null,"other village rejected");
 Check(session.GetOrCreate("v","unknown","x",VillageDispositionSpeakerRole.Unknown,"","")==null,"unknown role rejected");
 var town=new TownOrdinarySpeakerVoiceSession();town.Begin("town");
 var facts=new TownOrdinarySpeakerVoiceFacts("town","town-speaker",TownDialogueRole.OrdinaryCivilian,"","");
 string before=town.BuildPromptContext(facts,text);
 string immutable=JsonSerializer.Serialize(text.SpeakerVoiceFragments);
 for(int i=0;i<30;i++){var p=session.GetOrCreate("v","npc"+i,"NPC",i%2==0?VillageDispositionSpeakerRole.Villager:VillageDispositionSpeakerRole.PlayerTroop,"","");Check(!p.Concern.Contains("城里")&&!p.Concern.Contains("刚占领")&&!p.Concern.Contains("newly occupied"),"village concern not town conquest "+i);}
 Check(JsonSerializer.Serialize(text.SpeakerVoiceFragments)==immutable&&town.BuildPromptContext(facts,text)==before,"village generation cannot mutate town catalog/session");
 Check(!HasDirective(session.BuildSpeakerPrompt(ordinary,VillageDispositionSpeakerRole.Villager,text)),"persona prose no action directives");
 Check(session.BuildSpeakerPrompt(soldier,VillageDispositionSpeakerRole.PlayerTroop,text).Contains(text.RoleInstructions["OrdinarySoldier"]),"actual town soldier voice/command policy reused");
 foreach(bool hostile in new[]{true,false})foreach(bool govern in new[]{true,false})foreach(var r in new[]{VillageDispositionSpeakerRole.Villager,VillageDispositionSpeakerRole.PlayerTroop,VillageDispositionSpeakerRole.PlayerCompanion}) {
  var p=session.GetOrCreate("v","length"+r,"NPC",r,"","");
  string compact=VillageDispositionPromptProfile.BuildScenePrompt("test",false,hostile,govern,text)+session.BuildSpeakerPrompt(p,r,text);
  Check(compact.Length<(english?2400:700),"compact scene/persona character budget "+r);
 }
 string allRules=string.Join("\n",VillageDispositionActionCatalog.All.Where(x=>x.HasDefinedEffect).Select(x=>x.Tag+" "+VillageDispositionPromptProfile.BuildActionDescription(x,english)));
 Check(allRules.Length<(english?1700:900),"shared conditions not repeated in every action");
 var cache=(TownOrdinarySpeakerVoiceSession)typeof(VillageDispositionDialogueSession).GetField("_ordinaryVoices",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(session)!;
 Check(cache.ProfileCount>0,"village actually delegates generation and storage to town session");
 session.End();Check(session.ProfileCount==0 && cache.ProfileCount==0 && !cache.IsActive,"departure clears village adapter AND shared town cache");
 Check(session.BuildSpeakerPrompt(source,VillageDispositionSpeakerRole.Villager,text)=="","stale profile rejected after exit");
 session.Begin("v",english,text);
 var reentered=session.GetOrCreate("v","a","A",VillageDispositionSpeakerRole.Villager,"","");
 Check(!ReferenceEquals(source,reentered)&&session.BuildSpeakerPrompt(source,VillageDispositionSpeakerRole.Villager,text)=="","same village reentry cannot revive old object");
 Check(town.ProfileCount==1,"village exit does not clear town state");
}
var englishFallback=TownPromptTextCatalog.CreateEnglishFallback();
foreach(var entry in TownPromptCatalogResources.Get(true).RuntimeText.Where(x=>x.Key.StartsWith("VillageScene_")))
 Check(entry.Value==englishFallback.GetRuntimeText(entry.Key),"actual EN fallback parity "+entry.Key);
Check(VillageDispositionPromptProfile.BuildScenePrompt("v",false,true,false,englishFallback).Contains("Dedicated GCCZ"),"actual fallback has new resources");
var character=new CharacterObject {HeroObject=new Hero()};V.Speaker=new Agent {Character=character};V.Role=VillageDispositionSpeakerRole.Villager;V.Live=true;
V.Active=false;Check(V.BuildRuntimePromptForExternal(null,character,7)=="","ordinary village/town/castle without admitted owner gets no village prompt");
V.Active=true;V.Foreign=true;Check(V.BuildRuntimePromptForExternal(character.HeroObject,character,7)=="foreign-main","actual scene target accepted");
Check(V.BuildRuntimePromptForExternal(null,null,-1)=="","courier without scene agent excluded even while village active");
Check(V.BuildRuntimePromptForExternal(new Hero(),character,7)=="","remote/mismatched hero excluded");
Check(V.BuildRuntimePromptForExternal(null,new CharacterObject(),7)=="","mismatched character excluded");
Check(V.BuildRuntimePromptForExternal(null,character,8)=="","other index excluded");
V.Live=false;Check(V.BuildRuntimePromptForExternal(null,character,7)=="","removed or stale speaker excluded");V.Live=true;
V.Foreign=false;Check(V.BuildRuntimePromptForExternal(null,character,7)=="friendly-main","friendly speaker routed without foreign prompt");
V.Active=false;Check(V.BuildRuntimePromptForExternal(null,character,7)=="","scene teardown blocks late capture");
Check(V.BuildCalls==2,"rejected contexts never call composers");
Console.WriteLine($"PASS {checks} village prompt/persona/scope checks; real core and prompt scope, native owner/resolver seams are fakes; no game/provider/save executed.");

if(args.Contains("--measure")) foreach(bool en in new[]{false,true}) {
 var t=TownPromptCatalogResources.Get(en);var ss=new VillageDispositionDialogueSession();ss.Begin("v",en,t);
 foreach(var mode in new[]{"foreign","governor","visitor"})foreach(var r in new[]{VillageDispositionSpeakerRole.Villager,VillageDispositionSpeakerRole.PlayerTroop,VillageDispositionSpeakerRole.PlayerCompanion}) {
  var pp=ss.GetOrCreate("v",r.ToString(),en?"Speaker":"说话者",r,"","");
  var scene=VillageDispositionPromptProfile.BuildScenePrompt(en?"Village":"村庄",false,mode=="foreign",mode=="governor",t);
  var voice=ss.BuildSpeakerPrompt(pp,r,t);
  var post=VillageDispositionPromptProfile.BuildPostprocessContext(en?"Village":"村庄",r,true,false,mode=="foreign",mode=="governor",t);
  Console.WriteLine("MEASURE "+JsonSerializer.Serialize(new{language=en?"en":"zh-CN",mode,role=r.ToString(),mainChars=scene.Length+1+voice.Length,postContextChars=post.Length}));
 }
 var rules=string.Join("\n",VillageDispositionActionCatalog.All.Where(x=>x.HasDefinedEffect).Select(x=>x.Tag+" "+VillageDispositionPromptProfile.BuildActionDescription(x,en)));
 Console.WriteLine("RULE_MEASURE "+JsonSerializer.Serialize(new{language=en?"en":"zh-CN",foreignAllRulesChars=rules.Length}));
}