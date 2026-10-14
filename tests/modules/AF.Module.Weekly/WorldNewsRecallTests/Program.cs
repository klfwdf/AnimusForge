using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AnimusForge;

int checks = 0;
void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; Console.WriteLine("PASS " + name); }
WorldNewsFact Fact(string key, string text, int day = 10, string nation = "A", int score = 65)
    => new WorldNewsFact { Key=key, Kind="battle", Sentence=text, Date="1084 spring", Day=day, Score=score,
        KingdomIds=new(){nation}, Group=key };
var facts = new List<WorldNewsFact>();
var index = new WorldNewsFactArchive(facts);
var old = Fact("old-siege","去年在阿尔戈隆的围城中，阿列克救出了俘虏。");
old.ParticipantIds.Add("witness");
index.Upsert(old);
for (int i=0;i<1500;i++) index.Upsert(Fact("fresh:"+i,"New event at unrelated fortress "+i,100+i));
var local = new WorldNewsObserver {HeroId="local",KingdomId="A",LocalKingdomId="A",Day=2000};
foreach(int yearDays in new[]{84,365,400}) {
 local.Day=10+yearDays;
 var request=index.Capture(local,"阿尔戈隆围城",1);
 Check(request.Candidates.Any(c=>c.Fact.Key=="old-siege"),"old fact retrieved after calendar year length "+yearDays);
 Check(request.Candidates.Count<=48,"candidate count bounded");
}
local.Day=2000;
var far = new WorldNewsObserver {HeroId="far",KingdomId="B",LocalKingdomId="B",Day=2000};
Check(!index.Capture(far,"阿尔戈隆围城",1).Candidates.Any(c=>c.Fact.Key=="old-siege"),"unpublished remote fact excluded before scoring");
far.HeroId="witness";
Check(index.Capture(far,"阿尔戈隆围城",1).Candidates.Any(c=>c.Fact.Key=="old-siege"&&c.Participant),"participant retains first-hand source after moving");
far.HeroId="far";
index.MarkPublished(new[]{"old-siege"});
Check(index.Capture(far,"阿尔戈隆围城",1).Candidates.Any(c=>c.Fact.Key=="old-siege"&&!c.Participant),"published major news is public, not first-hand");
var minor=Fact("minor","Remote small story",20,"A",35);minor.Published=true;index.Upsert(minor);
Check(!index.Capture(far,"Remote small story",1).Candidates.Any(c=>c.Fact.Key=="minor"),"published minor does not become worldwide knowledge");
var declaration=Fact("declaration:secret","Secret peace declaration");declaration.Kind="diplomatic_declaration";declaration.Published=true;declaration.ParticipantIds.Add("local");index.Upsert(declaration);
Check(!index.Capture(local,"Secret peace declaration",1).Candidates.Any(c=>c.Fact.Key==declaration.Key),"participant and publication cannot bypass diplomacy permission");
local.KnownDocuments=new HashSet<string>{"secret"};
Check(index.Capture(local,"Secret peace declaration",1).Candidates.Any(c=>c.Fact.Key==declaration.Key),"delivered document becomes eligible");
var legacyDeclaration=Fact("unknown-legacy-key","Legacy declaration");legacyDeclaration.Kind="DIPLOMATIC_DECLARATION";legacyDeclaration.Published=true;
Check(!WorldNewsFactArchive.Visible(legacyDeclaration,local),"legacy kind casing cannot bypass missing document provenance");
var future=Fact("future","future fortress",2001);index.Upsert(future);
Check(!index.Capture(local,"future fortress",1).Candidates.Any(c=>c.Fact.Key=="future"),"future fact rejected");
int count=facts.Count;
index.Upsert(Fact("old-siege","阿尔戈隆的围城已经确认修正。"));
Check(facts.Count==count&&facts.Single(f=>f.Key=="old-siege").Published,"same key update preserves publication and no duplicate");
Check(facts.Single(f=>f.Key=="old-siege").ParticipantIds.Contains("witness"),"missing legacy participant metadata cannot erase confirmed identity");
var detached=index.Capture(local,"阿尔戈隆",2);
string previous=detached.Candidates.Single(c=>c.Fact.Key=="old-siege").Fact.Sentence;
index.Upsert(Fact("old-siege","原始记录再次修订"));
Check(detached.Candidates.Single(c=>c.Fact.Key=="old-siege").Fact.Sentence==previous,"request snapshot detached from later edits");
var options=new JsonSerializerOptions{IncludeFields=true};
string json=JsonSerializer.Serialize(facts,options);
var loaded=JsonSerializer.Deserialize<List<WorldNewsFact>>(json,options);
var restored=new WorldNewsFactArchive(loaded);
Check(restored.Capture(local,"原始记录",3).Candidates.Any(c=>c.Fact.Key=="old-siege"),"JSON roundtrip retains historical facts and rebuilds index");
Check(loaded.Count==facts.Count,"no history age or count truncation at load");
var request2=new WorldNewsRecallRequest {Generation=42,Query="siege",Candidates=Enumerable.Range(0,48).Select(i=>new WorldNewsRecallCandidate {
 Fact=Fact("f"+i,"doc"+i),SearchText="doc"+i,LexicalScore=2 }).ToList()};
int embeds=0,reranked=0;
float[] Embed(string s) {embeds++;return new float[]{1,0};}
float[] Rerank(string q,IReadOnlyList<string> docs) {reranked=docs.Count;return docs.Select((d,i)=>(float)i).ToArray();}
var result=WorldNewsRecallPolicy.Retrieve(request2,Embed,Rerank);
Check(embeds==49&&reranked==12&&result.Selected.Count==4,"embedding/rerank/final budgets 48/12/4");
Check(result.Selected[0].Fact.Key=="f11"&&result.Mode=="embedding+rerank","cross-encoder ordering affects final selection");
Check(result.Generation==42,"generation preserved");
request2.Candidates.ForEach(c=>c.Fact.Group="same-event");
Check(WorldNewsRecallPolicy.Retrieve(request2,Embed,Rerank).Selected.Count==1,"same incident deduplicated");
var fallback=WorldNewsRecallPolicy.Retrieve(request2,_=>throw new Exception("unavailable"),null);
Check(fallback.Mode=="lexical_fallback"&&fallback.Selected.Count==1,"explicit lexical fallback retains only eligible candidates");
request2.Candidates.ForEach(c=>c.LexicalScore=0);
Check(WorldNewsRecallPolicy.Retrieve(request2,null,null).Selected.Count==0,"no lexical evidence never fabricates relevance");
Check(WorldNewsRecallPolicy.Retrieve(request2,_=>new float[]{1,0},(_,d)=>new float[]{float.NaN}).Selected.Count==1,"invalid reranker result uses prior semantic order safely");
Check(WorldNewsRecallPolicy.Cosine(new[]{float.NaN},new[]{1f})==-1,"invalid vectors rejected");
Check(WorldNewsFactArchive.Terms("A a the 阿尔戈隆").Contains("尔戈"),"Chinese bigram index supports substring names");
Check(!WorldNewsFactArchive.Visible(Fact("x","fact"),new WorldNewsObserver{Day=0,KingdomId="A"}),"negative relative date is not current knowledge");
Console.WriteLine("PASS "+checks+" world news archive and retrieval assertions");
