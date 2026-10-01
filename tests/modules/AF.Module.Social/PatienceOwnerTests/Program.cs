using System;
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
Console.WriteLine("PASS PatienceOwner production rules/state/identity/save/reset");
} }
