using System;
using System.Collections.Generic;
using AnimusForge;
internal static class Program {
static void Check(bool ok,string label) {if(!ok) throw new Exception(label);}
static void Main() {
var f=new RebellionClanFacts { Exists=true, KingdomExists=true, InKingdom=true, LeaderExists=true, LeaderAlive=true, TownCount=1,RelationToKing=-5, Renown=10000,Tier=3,Strength=10000,SameKingCulture=true };
Check(RebellionRules.CandidateNote(f,false)=="","eligible"); Check(RebellionRules.CandidateScore(f)==900+420+420+140+7.5f+8,"score caps");
f.RelationToKing=-4; Check(RebellionRules.CandidateNote(f,false)!=""&&RebellionRules.CandidateNote(f,true)=="","forced qualification");
int calls=0; float Random(){calls++;return .25f;}
Check(!RebellionRules.PassChance(false,0,Random,out _)&&calls==0,"zero lazy");
Check(RebellionRules.PassChance(true,0,Random,out _)&&calls==0,"forced lazy");
Check(!RebellionRules.PassChance(false,.25f,Random,out _)&&calls==1,"strict probability boundary");
Check(RebellionRules.FollowerEligible(-5,10)&&!RebellionRules.FollowerEligible(-5,9),"follower threshold");
uint bg,icon; int colorCalls=0; RebellionRules.SelectColors(new List<uint>(),new HashSet<uint>(),10,10,true,n=>{colorCalls++;return 0;},out bg,out icon); Check(bg==10&&icon==4289374890U&&colorCalls==0,"empty palette lazy");
RebellionRules.SelectColors(new List<uint>{0xff000000,0xffffffff},new HashSet<uint>(),0xff000000,0xff000000,true,n=>{colorCalls++;return 0;},out bg,out icon); Check(bg==0xffffffff&&icon==0xff000000&&colorCalls==1,"color selection");
Check(RebellionRules.IsKnownOrLegacy("new_kingdom1",false,true)&&!RebellionRules.IsKnownOrLegacy("other",false,true),"legacy identity");
Check(!RebellionRules.CanDiscontinue(true,false,true,false,true,false)&&RebellionRules.CanDiscontinue(true,false,false,false,true,false),"landless player guard");
Check(RebellionNamingRules.TryParse("[NAME] Alpha [SHORT] A [LORE] lore",out var name,out _,out _)&&name=="Alpha","naming parser");
Check(!RebellionNamingRules.TryParse("Alpha",out _,out _,out _),"malformed naming");
// Stable ties preserve input order even when case-insensitive names compare equal.
var order=RebellionRules.Sort(new[]{"b","A","a"},_=>true,_=>1f,v=>v);
Check(string.Join(",",order)=="A,a,b","stable tie ordering");
int attempts=0,waits=0;var delays=new List<int>();
var result=RebellionNamingOwner.Generate(()=>System.Threading.Tasks.Task.FromResult(++attempts==1
 ? new RebellionNamingAttempt {IsRateLimit=true,RetryAfterSeconds=3,ErrorMessage="rate"}
 : new RebellionNamingAttempt {Success=true,Content="[NAME] Alpha [SHORT] A [LORE] lore"}),_=>false,
 (reason,content,body)=>reason,null,null,()=>2000,3,delay=>{waits++;delays.Add(delay);});
Check(result.Success&&result.AttemptsUsed==2&&attempts==2&&waits==1&&delays[0]==3000,"retry rate-limit ordering");
attempts=0;result=RebellionNamingOwner.Generate(()=>System.Threading.Tasks.Task.FromResult(new RebellionNamingAttempt {Success=true,Content="[NAME] Alpha [SHORT] A [LORE] lore"}),_=>true,
 (reason,content,body)=>reason,(_,_,_)=>attempts++,null,()=>1,2,_=>{});
Check(!result.Success&&result.AttemptsUsed==2&&attempts==2,"duplicate naming exhausted");
var pending=new System.Threading.Tasks.TaskCompletionSource<RebellionNamingAttempt>();
result=RebellionNamingOwner.Generate(()=>pending.Task,_=>false,(reason,content,body)=>reason,null,null,()=>1,1,_=>{},1);
Check(!result.Success&&result.AttemptsUsed==1&&result.FailureReason.Contains("60"),"timeout is failure not success");
pending.SetResult(new RebellionNamingAttempt {Success=true,Content="[NAME] Late [SHORT] L [LORE] late"});
Check(!result.Success,"late attempt cannot mutate returned outcome");
var ids=new RebelKingdomIdentityOwner();ids.Mark("new_kingdom1");ids.Mark("NEW_KINGDOM1");Check(ids.Snapshot().Length==1&&ids.IsKnown("NEW_KINGDOM1"),"identity normalization");
ids.Replace(new[]{"old"});Check(!ids.IsKnown("new_kingdom1")&&ids.IsKnown("OLD"),"identity load replacement");ids.Remove("old");Check(ids.Snapshot().Length==0,"identity retirement");
// Every eligibility preflight branch remains a rejection; facts and live effects are separate.
foreach(string field in new[]{"PlayerClan","RulingClan","Eliminated","Bandit","Minor","Rebel","Mercenary","LeaderChild","LeaderPrisoner","KingdomEliminated"}) {
 var valid=new RebellionClanFacts {Exists=true,KingdomExists=true,InKingdom=true,LeaderExists=true,LeaderAlive=true,TownCount=1,RelationToKing=-5};
 typeof(RebellionClanFacts).GetField(field,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(valid,true);
 Check(RebellionRules.CandidateNote(valid,false)!="","preflight flag "+field);
}
foreach(string field in new[]{"Exists","KingdomExists","InKingdom","LeaderExists","LeaderAlive"}) {
 var valid=new RebellionClanFacts {Exists=true,KingdomExists=true,InKingdom=true,LeaderExists=true,LeaderAlive=true,TownCount=1,RelationToKing=-5};
 typeof(RebellionClanFacts).GetField(field,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(valid,false);
 Check(RebellionRules.CandidateNote(valid,false)!="","missing fact "+field);
}
var noLand=new RebellionClanFacts {Exists=true,KingdomExists=true,InKingdom=true,LeaderExists=true,LeaderAlive=true};Check(RebellionRules.CandidateNote(noLand,true)!="","force still requires land");
Check(RebellionRules.FollowerPreflightNote(true,true,true)!=""&&RebellionRules.FollowerLeaderNote(true,false)!="","follower guards");
Check(RebellionNamingRules.NormalizeName(new string('x',30),24).Length==24,"name length cap");
var prompt=RebellionNamingRules.BuildUserPrompt(new RebellionNamingFacts {WeekIndex=1,ClanName="clan-marker",WorldWeekly="world-marker",KingdomWeekly="kingdom-marker",ExistingNames="existing-marker"});
Check(prompt.Contains("clan-marker")&&prompt.Contains("world-marker")&&prompt.Contains("kingdom-marker")&&prompt.Contains("existing-marker"),"detached prompt facts");
Console.WriteLine("PASS Rebellion production eligibility/score/random/followers/colors/legacy/landless/naming");
} }
