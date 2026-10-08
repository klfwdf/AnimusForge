using System;
using System.Linq;
using AnimusForge;
internal sealed class LegacyState : PatienceRecord { }
internal static class Program {
static void Check(bool ok,string label) { if(!ok) throw new Exception(label); }
static void Main() {
var owner=new PatienceOwner<LegacyState>();
Check(PatienceRules.MaxFromRelation(100)==80 && PatienceRules.MaxFromRelation(-100)==12,"max bounds");
Check(owner.Snapshot("hero:a",30,0)==30,"initial");
var x=owner.Apply("hero:a",30,0,PatienceMood.Neutral); Check(x.Before==30&&x.After==29,"neutral");
owner.Apply("hero:a",30,0,PatienceMood.Neutral); x=owner.Apply("hero:a",30,0,PatienceMood.Neutral); Check(x.After==26,"third round penalty");
x=owner.OverrideNeutral("hero:a",30,0,PatienceMood.Joy); Check(x.Delta==3&&x.After==29,"neutral correction");
Check(owner.Snapshot("hero:a",30,.25f)==30,"fractional recovery");
Check(owner.Snapshot("hero:a",10,.25f)==10,"cap shrink");
for(int i=0;i<10;i++) owner.Apply("hero:a",10,.25f,PatienceMood.Annoyed);
x=owner.Apply("hero:a",10,.25f,PatienceMood.Neutral); Check(x.Before==0&&x.After==0,"floor");
Check(PatienceRules.RelationDelta(PatienceMood.Delighted,95)==0&&PatienceRules.RelationDelta(PatienceMood.Annoyed,-95)==0,"relation saturation");
Check(owner.Snapshot("unnamed:a",30,1)==30,"identity separation");
var saved=owner.SaveSnapshot(); var copy=new PatienceOwner<LegacyState>(); copy.Replace(saved); Check(copy.Snapshot("hero:a",10,.25f)==0,"round trip");
copy.Clear(); Check(copy.Snapshot("hero:a",10,.25f)==10,"retire");
string t=" hi [ACTION:MOOD:joy][ACTION:MOOD:bored] "; Check(PatienceRules.StripMood(ref t)==PatienceMood.Bored&&t=="hi","last tag wins");
int[] expected={-1,2,1,-3,-2};int[] rounds={1,0,0,1,1};
for(int i=0;i<5;i++){int n=0;Check(PatienceRules.ComputePatienceDelta((PatienceMood)i,ref n)==expected[i]&&n==rounds[i],"all mood delta "+i);}
Check(PatienceRules.HeroRelationEffect(PatienceMood.Neutral,0,0,false)==-1&&PatienceRules.HeroRelationEffect(PatienceMood.Neutral,0,0,true)==0,"zero correction relation once");
Check(PatienceRules.ComputeRoyalDomainConversationLoyaltyDelta(PatienceMood.Delighted)==4&&PatienceRules.ComputeRoyalDomainConversationLoyaltyDelta(PatienceMood.Joy)==2,"loyalty values preserved");
Check(PatienceRules.HeroKey(" A ")=="hero:a"&&PatienceRules.UnnamedKey(" A ","other")=="unnamed:a"&&PatienceRules.UnnamedKey(""," A ")=="name:a","legacy key identities");
var loaded=new System.Collections.Generic.Dictionary<string,LegacyState>{{"hero:old",new LegacyState {Value=0,LastDay=0,NoInterestRounds=7,ExhaustedRefusalCount=3}}};
copy.Replace(loaded);Check(copy.Snapshot("hero:old",30,.5f)==2,"half-day recovery");var savedOld=copy.SaveSnapshot()["hero:old"];Check(savedOld.NoInterestRounds==7&&savedOld.ExhaustedRefusalCount==0,"fractional day does not decay rounds; positive resets refusal");
Check(copy.Snapshot("hero:old",30,2)==8,"cross-day recovery");Check(copy.SaveSnapshot()["hero:old"].NoInterestRounds==6,"floor elapsed-day decay");
var detached=copy.SaveSnapshot();detached["hero:old"].Value=99;Check(copy.Snapshot("hero:old",30,2)==8,"save snapshot not live duplicate state");
// Four legacy JSON field names/defaults are independent of the legacy nested record's location.
var legacyJson="{\"Value\":5.5,\"LastDay\":7,\"NoInterestRounds\":3,\"ExhaustedRefusalCount\":2}";
var jsonOptions=new System.Text.Json.JsonSerializerOptions {IncludeFields=true};
var legacy=System.Text.Json.JsonSerializer.Deserialize<LegacyState>(legacyJson,jsonOptions);
Check(legacy.Value==5.5f&&legacy.LastDay==7&&legacy.NoInterestRounds==3&&legacy.ExhaustedRefusalCount==2,"legacy four-field JSON fixture");
var absent=System.Text.Json.JsonSerializer.Deserialize<LegacyState>("{}",jsonOptions);Check(absent.NoInterestRounds==0&&absent.ExhaustedRefusalCount==0,"missing JSON fields default");
var responseOwner=new PatienceOwner<LegacyState>();
var app=new PatienceResponseApplication(responseOwner.Apply,responseOwner.OverrideNeutral);
var input=new PatienceResponseInput("hero:response",10,0,0,true);
for(int i=0;i<10;i++) responseOwner.Apply(input.Key,10,0,PatienceMood.Annoyed);
int relation=0,love=0,royal=0; string response="hello";
var result=app.Apply(input,ref response,0,false,_=>royal++, (delta,_)=>relation+=delta,(delta,_)=>love+=delta);
Check(result.Applied&&relation==-1&&royal==0,"ordinary exhausted hero applies relation once");
response="hello"; result=app.Apply(input,ref response,0,true,_=>royal++, (delta,_)=>relation+=delta,(delta,_)=>love+=delta);
Check(!result.Applied&&relation==-1&&royal==0,"neutral correction early return");
response="hello [ACTION:MOOD:joy]";
result=app.Apply(input,ref response,0,true,_=>{Check(responseOwner.Snapshot(input.Key,10,0)==0,"royal before corrected state");royal++;},(delta,_)=>relation+=delta,(delta,_)=>love+=delta);
Check(result.Applied&&result.RelationDelta==0&&royal==1&&response=="hello","positive correction no exhausted penalty");
response="hello [ACTION:MOOD:joy]";
result=app.Apply(new PatienceResponseInput("",30,0,0,false),ref response,0,true,_=>royal++,null,null);
Check(!result.Applied&&royal==2,"unnamed empty correction still royal first");
response="hello [ACTION:MOOD:unknown]";
result=app.Apply(new PatienceResponseInput("unnamed:x",30,0,0,false),ref response,0,false,null,null,null);
Check(result.Mood==PatienceMood.Neutral&&response=="hello","unknown tag is not a fabricated mood");
response="hello [ACTION:MOOD:joy]";bool failed=false;
try { app.Apply(input,ref response,0,false,null,null,(_,_)=>throw new InvalidOperationException("love leaf")); } catch(InvalidOperationException) { failed=true; }
Check(failed,"private love leaf failure is not swallowed");
Console.WriteLine("PASS PatienceResponseApplication original owner, effect order, correction, unknown and failure");
RelationLevelPolicyCases.Run();
GameAdapterCases.Run();
Console.WriteLine("PASS PatienceOwner production rules/state/identity/save/reset");
} }

namespace TaleWorlds.CampaignSystem {
 public class Hero {public static Hero MainHero=new();public string StringId="npc",Name="NPC";public Clan Clan,CompanionOf;public bool IsPlayerCompanion,IsPrisoner,IsLord;public int Relation;public int GetRelation(Hero player)=>Relation;}
 public class Clan {public static Clan PlayerClan=new();}
 public class CharacterObject {}
 public class Campaign {public static Campaign Current=new();public Models Models=new();public ConversationManager ConversationManager=new();}
 public class Models {public LoyaltyModel SettlementLoyaltyModel=new();}public class LoyaltyModel {public float MaximumLoyaltyInSettlement=100;}
 public class ConversationManager {public CharacterObject OneToOneConversationCharacter;}
}
namespace TaleWorlds.CampaignSystem.Settlements {public class Town {public float Loyalty=50;}public class Settlement {public static Settlement CurrentSettlement;public Town Town=new();public TaleWorlds.CampaignSystem.Clan OwnerClan;public string StringId="town",Name="Town";}}
namespace TaleWorlds.CampaignSystem.Party {public class MobileParty {public static MobileParty MainParty;public TaleWorlds.CampaignSystem.Settlements.Settlement CurrentSettlement;}}
namespace TaleWorlds.CampaignSystem.Actions {public static class ChangeRelationAction {public static bool Throw;public static void ApplyRelationChangeBetweenHeroes(TaleWorlds.CampaignSystem.Hero player,TaleWorlds.CampaignSystem.Hero target,int delta){if(Throw)throw new InvalidOperationException("relation leaf");target.Relation+=delta;}}}
namespace TaleWorlds.Library {public static class MBMath {public static float ClampFloat(float v,float min,float max)=>Math.Clamp(v,min,max);}}
namespace AnimusForge {
 internal class MyBehavior {internal struct PatienceSnapshot {public string Key,DisplayName;public int Relation,Trust,PublicTrust,PrivateLove,Max;public float Current;public string PatienceLevel,RelationLevel,TrustLevel,PublicTrustLevel,PrivateLoveLevel;}}
 internal class RewardSystemBehavior {internal static RewardSystemBehavior Instance=new();internal static string GetTrustLevelText(int v)=>v.ToString();internal int GetPublicTrust(TaleWorlds.CampaignSystem.Hero h)=>7;internal int GetEffectiveTrust(TaleWorlds.CampaignSystem.Hero h)=>9;internal bool TryGetSettlementMerchantKind(TaleWorlds.CampaignSystem.CharacterObject c,out string kind){kind="merchant";return true;}internal int GetSettlementLocalPublicTrust(TaleWorlds.CampaignSystem.Settlements.Settlement s)=>3;internal int GetSettlementSharedPublicTrust(TaleWorlds.CampaignSystem.Settlements.Settlement s)=>4;internal int GetSettlementMerchantEffectiveTrust(TaleWorlds.CampaignSystem.Settlements.Settlement s,string kind)=>9;}
 internal class RomanceSystemBehavior {internal static RomanceSystemBehavior Instance=new();internal static bool RelationUsesLove;internal bool Throw;internal int Love;internal static string GetPrivateLoveLevelText(int v)=>v.ToString();internal static bool IsPlayerCompanionOrFamily(TaleWorlds.CampaignSystem.Hero h)=>false;internal static bool TryGetPrivateLoveAsPlayerRelation(TaleWorlds.CampaignSystem.Hero h,out int value){value=Instance.Love;return RelationUsesLove;}internal int GetPrivateLove(TaleWorlds.CampaignSystem.Hero h)=>Love;internal void AdjustPrivateLove(TaleWorlds.CampaignSystem.Hero h,int delta,string source){if(Throw)throw new InvalidOperationException("love leaf");Love+=delta;}}
 internal static class AnimusForgeQuickInfo {internal static void Show(string message){}}
 internal static class Logger {internal static int Observations;internal static void Log(string a,string b){}internal static void Obs(string a,string b,System.Collections.Generic.Dictionary<string,object> facts){Observations++;}internal static void Metric(string name){}}
}
internal static class GameAdapterCases {
 static int checks;static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
 internal static void Run(){
 var owner=new PatienceOwner<LegacyState>();var app=new PatienceResponseApplication(owner.Apply,owner.OverrideNeutral);int sync=0;var adapter=new PatienceResponseBannerlordAdapter(owner.Snapshot,()=>0,r=>"relation:"+r,t=>sync++,app);var hero=new TaleWorlds.CampaignSystem.Hero();
 var snapshot=adapter.GetHeroSnapshot(hero);Check(snapshot.Key=="hero:npc"&&snapshot.PublicTrust==7&&snapshot.Trust==9,"actual hero projection uses sole owner and trust leaves");
 for(int i=0;i<100;i++)owner.Apply(snapshot.Key,snapshot.Max,0,PatienceMood.Annoyed);string text="hello";adapter.ApplyHero(hero,ref text,true,false);Check(hero.Relation==-1&&owner.Snapshot(snapshot.Key,snapshot.Max,0)==0,"actual exhausted game relation ordinary once");
 int observations=Logger.Observations;text="hello";adapter.ApplyHero(hero,ref text,true,true);Check(hero.Relation==-1&&Logger.Observations==observations,"actual neutral correction no effect or observation");
 TaleWorlds.CampaignSystem.Settlements.Settlement.CurrentSettlement=new(){OwnerClan=TaleWorlds.CampaignSystem.Clan.PlayerClan};text="hello [ACTION:MOOD:joy]";adapter.ApplyHero(hero,ref text,true,true);Check(sync==1&&text=="hello"&&TaleWorlds.CampaignSystem.Settlements.Settlement.CurrentSettlement.Town.Loyalty==52,"actual corrected royal loyalty leaf and stripped reply");
 hero.IsLord=true;text="hello [ACTION:MOOD:joy]";adapter.ApplyHero(hero,ref text,true,true);Check(sync==1,"actual royal lord denial");hero.IsLord=false;
 TaleWorlds.CampaignSystem.Actions.ChangeRelationAction.Throw=true;observations=Logger.Observations;text="hello [ACTION:MOOD:annoyed]";adapter.ApplyHero(hero,ref text,false,false);Check(Logger.Observations==observations+1,"actual relation exception preserved as caught leaf");TaleWorlds.CampaignSystem.Actions.ChangeRelationAction.Throw=false;
 RomanceSystemBehavior.Instance.Throw=true;observations=Logger.Observations;bool failed=false;text="hello [ACTION:MOOD:joy]";try{adapter.ApplyHero(hero,ref text,false,false);}catch(InvalidOperationException){failed=true;}Check(failed&&Logger.Observations==observations,"actual private love failure prevents false observed success");RomanceSystemBehavior.Instance.Throw=false;
 text="hello [ACTION:MOOD:joy]";adapter.ApplyHero(null,ref text,false,false);Check(text=="hello"&&Logger.Observations==observations,"actual null hero ABI only strips tag");
 RomanceSystemBehavior.RelationUsesLove=true;RomanceSystemBehavior.Instance.Love=0;hero.Relation=11;text="hello [ACTION:MOOD:joy]";adapter.ApplyHero(hero,ref text,false,false);Check(hero.Relation==11&&RomanceSystemBehavior.Instance.Love>0,"actual private-love relation uses single romance leaf instead of Hero relation");Check(adapter.GetHeroSnapshot(hero).Relation==RomanceSystemBehavior.Instance.Love,"actual private-love relation snapshot projection");RomanceSystemBehavior.RelationUsesLove=false;
 var unnamed=adapter.GetUnnamedSnapshot("a","NPC"," display ");Check(unnamed.Key=="unnamed:a"&&unnamed.DisplayName=="display","actual unnamed identity projection");
 int oldSync=sync;text="hello [ACTION:MOOD:joy]";adapter.ApplyUnnamed("","",ref text,true);Check(sync==oldSync+1,"actual unnamed empty correction retains royal-first semantics");
 Check(typeof(PatienceResponseBannerlordAdapter).GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).All(f=>!f.FieldType.Name.Contains("Dictionary")),"actual game adapter has no social state warehouse");
 int exited=0;var diagnostics=new ConversationDiagnosticsCaptureAdapter(()=>true,()=>exited++,sb=>sb.Append(" sceneHistory=ok"),()=>2,(out int count,out bool running)=>{count=3;running=true;return true;});var diagnostic=ConversationDiagnosticsCaptureAdapter.Snapshot(sb=>sb.Append("nativeHistory=ok"),()=>diagnostics);Check(exited==1&&diagnostic.Contains("sceneHistory=ok")&&diagnostic.Contains("mainThreadActions=2")&&diagnostic.Contains("sceneSpeechQueue=3")&&diagnostic.Contains("sceneSpeechWorker=1"),"actual diagnostic snapshot composes only supplied leaves");
 diagnostics=new ConversationDiagnosticsCaptureAdapter(()=>false,()=>throw new Exception("must not exit untaken lock"),sb=>throw new Exception("must not read locked history"),()=>throw new Exception("queue leaf"),(out int count,out bool running)=>{count=0;running=false;return false;});diagnostic=ConversationDiagnosticsCaptureAdapter.Snapshot(sb=>throw new Exception("native leaf"),()=>diagnostics);Check(diagnostic.Contains("nativeHistory=unavailable")&&diagnostic.Contains("sceneHistory=busy")&&diagnostic.Contains("mainThreadActions=unavailable")&&diagnostic.Contains("sceneSpeechQueue=busy"),"actual diagnostics keeps nonblocking busy and independent catch boundaries");
 diagnostic=ConversationDiagnosticsCaptureAdapter.Snapshot(sb=>{},()=>throw new Exception("composition lookup"));Check(diagnostic.Contains("sceneHistory=unavailable"),"actual diagnostic lookup remains inside original catch boundary");
 diagnostics=new ConversationDiagnosticsCaptureAdapter(()=>true,()=>exited++,sb=>throw new Exception("history read"),()=>0,(out int count,out bool running)=>{count=0;running=false;return true;});ConversationDiagnosticsCaptureAdapter.Snapshot(sb=>{},()=>diagnostics);Check(exited==2,"actual diagnostic read failure still releases taken lock");
 Console.WriteLine("PASS "+checks+" PatienceResponseBannerlordAdapter actual game leaves/order/failure/snapshot");
 }
}

internal static class RelationLevelPolicyCases {
 static void Check(bool ok,string name){if(!ok)throw new Exception("relation policy: "+name);}
 internal static void Run(){
 string[] labels={"死敌","敌对","厌恶","疏离","冷漠","中立","熟络","友好","亲近","至交"};
 for(int relation=-150;relation<=150;relation++){
  int clamped=Math.Clamp(relation,-100,100);int expected=Math.Min(10,(clamped+100)/20+1);
  Check(PatienceRules.ToTenLevelIndexByRelation(relation)==expected,"integer partition boundary "+relation);
  Check(PatienceRules.GetRelationLevelIndex(relation)==expected,"index entry parity "+relation);
  Check(PatienceRules.GetRelationLevelText(relation)==labels[expected-1],"ten-level text parity "+relation);
 }
 Check(PatienceRules.GetRelationLevelIndex(int.MinValue)==1&&PatienceRules.GetRelationLevelText(int.MinValue)=="死敌","minimum integer clamp");
 Check(PatienceRules.GetRelationLevelIndex(int.MaxValue)==10&&PatienceRules.GetRelationLevelText(int.MaxValue)=="至交","maximum integer clamp");
 var owner=new PatienceOwner<LegacyState>();var adapter=new PatienceResponseBannerlordAdapter(owner.Snapshot,()=>0,PatienceRules.GetRelationLevelText,_=>{},new PatienceResponseApplication(owner.Apply,owner.OverrideNeutral));
 var hero=new TaleWorlds.CampaignSystem.Hero{Relation=-1};Check(adapter.GetHeroSnapshot(hero).RelationLevel=="冷漠","actual Hero snapshot cached pure delegate");hero.Relation=0;Check(adapter.GetHeroSnapshot(hero).RelationLevel=="中立","actual neutral Hero snapshot");Check(adapter.GetUnnamedSnapshot("relation-policy","NPC").RelationLevel=="中立","actual Unnamed snapshot uses same rule");
 Console.WriteLine("PASS RelationLevel policy 301 integer inputs/ten texts/extreme clamps/actual Hero-Unnamed snapshot delegate");
 }
}
