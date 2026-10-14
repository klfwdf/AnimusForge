using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AnimusForge;

namespace TaleWorlds.Engine { internal static class FixtureNamespace {} }
namespace TaleWorlds.Library { internal static class TWParallel {internal static int MainId;public static bool IsMainThread()=>Thread.CurrentThread.ManagedThreadId==MainId;} }
namespace TaleWorlds.CampaignSystem
{
    internal sealed class Faction { public string Id; }
    internal sealed class Settlement { public Faction MapFaction; }
    internal sealed class Hero {
        public string StringId,Kingdom;public Settlement CurrentSettlement;
        internal static Dictionary<string,Hero> All=new();
        public static Hero Find(string id)=>All.TryGetValue(id,out var hero)?hero:null;
    }
}
namespace AnimusForge
{
    internal sealed class MentionedWorldEntities {internal MentionedWorldEntities Clone()=>new();}
    internal sealed class PromptRuntimeTargetBinding {}
    internal sealed class PromptRuleEligibility {}
    internal sealed class GuardrailRuleHit {}
    internal sealed class LoreCandidateRules {}
    internal sealed class PromptBuildRequest {
        internal bool SuppressDynamicRuleAndLore,HasPrefetchedLore,AllowRulePreprocess;
        internal string KingdomIdOverride,Input,NpcLastUtterance,GuardrailStickyTargetKey;
        internal HashSet<string> ExcludedRuleIds;
        internal PromptRuntimeTargetBinding Target;internal PromptRuleEligibility Eligibility;
    }
    internal sealed class PromptRoutingResult {internal List<string> AuxiliaryRuleHitIds=new();}
    internal static class KnowledgeLibraryBehavior {
        internal static PromptLoreSettings CapturePromptLoreSettings()=>new();
        internal static long PreparePromptLoreRetrieval(MentionedWorldEntities m)=>0;
        internal static LoreCandidateRules CollectPromptLoreCandidates(MentionedWorldEntities m,long v,PromptLoreSettings s)=>null;
    }
    internal static class WorldEntityRetrievalService {
        internal sealed class DetachedEntityCandidates {}
        internal sealed class DetachedEntityMatches {}
        internal sealed class EntityCapture {internal DetachedEntityCandidates Candidates;internal int MaxInjectedEntities;}
        internal static EntityCapture CaptureEntityCandidates(MentionedWorldEntities m,string q,TaleWorlds.CampaignSystem.Hero h)=>new();
        internal static DetachedEntityMatches MatchDetachedCandidates(DetachedEntityCandidates c,MentionedWorldEntities m,string q,int n)=>null;
    }
    internal static class AIConfigHandler {
        internal sealed class Scope:IDisposable {public void Dispose(){}}
        internal static IDisposable BeginGuardrailRuntimeScope()=>new Scope();
        internal static void ApplyGuardrailRuntimeTarget(PromptRuntimeTargetBinding t,PromptRuleEligibility e){}
        internal static void ClearGuardrailRuntimeTarget(){}
        internal static int GuardrailRuleReturnCap=>4;
        internal static List<GuardrailRuleHit> GetMatchedExtraRuleHitsForWorker(string a,string b,int c,HashSet<string> d,string e)=>new();
    }
    internal sealed class TestDropdown { internal int SelectedIndex=0; }
    internal sealed class DuelSettings {internal static DuelSettings Instance=new();internal TestDropdown RerankerDeviceDropdown=new();}
    internal static class AnimusForgeModulePaths {internal static string Root;internal static string GetCurrentModuleRoot()=>Root;}
    internal static class Logger {
        public static void Log(string category,string text)=>Console.WriteLine(category+": "+text);
        public static void Metric(string name,bool ok,double milliseconds){}
        public static void Obs(string c,string k,Dictionary<string,object> fields){}
    }
    internal static class SaveRuntimeGuard {internal static long Generation=7;internal static long CaptureGeneration()=>Generation;internal static bool IsCurrentGeneration(long n)=>n==Generation;}
    internal static class MemoryEntityIdentityBannerlordAdapter {
        internal static string Local="A";
        internal static string ResolveWeeklyReportNpcKingdomId(TaleWorlds.CampaignSystem.Hero h,object unused,string over=null)=>over??h?.Kingdom??"";
        internal static string ResolveWeeklyReportSurroundingsKingdomId(object h,object c,string over=null)=>Local;
        internal static string GetKingdomId(TaleWorlds.CampaignSystem.Faction f)=>f?.Id??"";
        internal static int GetCurrentGameDayIndexSafe()=>400;
    }
    internal static class DiplomacyModuleServices {
        internal sealed class WorldApi {internal HashSet<string> Known=new();internal ISet<string> CaptureKnownDocumentIds(string hero,string kingdom)=>new HashSet<string>(Known);}
        internal static WorldApi World=new();
    }
    internal sealed class MyBehavior {
        internal static bool Enabled=true;internal static MyBehavior Instance=new();
        internal static bool IsWorldBulletinEnabled()=>Enabled;
        internal List<WorldNewsFact> Facts=new();internal WorldNewsFactArchive Archive;
        internal WorldNewsRecallRequest CaptureWorldNewsRecall(WorldNewsObserver observer,string query,long generation)=>Archive.Capture(observer,query,generation);
    }
    internal static class CampaignSystemFactProjection {internal static string Project(string kind,string text)=>text;}
    internal static class WorldBulletinPolicy {internal static string Truncate(string s,int n)=>s.Length<=n?s:s.Substring(0,n);}
    internal static class WorldBulletinPolicyTexts {
        internal static Dictionary<string,string> Values;
        internal static string Prompt(string key)=>Values[key];
        internal static string Prompt(string key,string fallback,params (string Name,string Value)[] vars) {
            string s=Values.TryGetValue(key,out var value)?value:fallback;foreach(var v in vars)s=s.Replace("{"+v.Name+"}",v.Value);return s;
        }
    }
    internal static class PromptComposerTexts {
        internal static string Get(string key)=>key;
        internal static string Raw(string key)=>key=="rule_section_prefix"?"[RULE]":key;
        internal static bool StartsWith(string value,string key)=>value.StartsWith(Raw(key),StringComparison.Ordinal);
        internal static string Format(string key,params (string Name,string Value)[] vars)=>key;
    }
    internal static class PromptRuleBlockText {internal static bool Has(string text,string id)=>text?.Contains(id)==true;}
}
namespace AnimusForge.CoupSystem {internal static class CoupSourceTexts {internal static string ReportForPrompt(string s)=>s;}}

internal static class AdapterProgram
{
    static int checks;
    static void Check(bool condition,string label) {if(!condition)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
    static int Main(string[] args)
    {
        TaleWorlds.Library.TWParallel.MainId=Thread.CurrentThread.ManagedThreadId;
        AnimusForgeModulePaths.Root=args[0];
        var hero=new TaleWorlds.CampaignSystem.Hero {StringId="npc",Kingdom="A",CurrentSettlement=new(){MapFaction=new(){Id="A"}}};
        TaleWorlds.CampaignSystem.Hero.All[hero.StringId]=hero;
        var owner=MyBehavior.Instance;
        owner.Archive=new WorldNewsFactArchive(owner.Facts);
        owner.Archive.Upsert(new WorldNewsFact {Key="siege",Kind="settlement_siege",Sentence="阿尔戈隆围城战后，守军投降，被俘的领主获救。",Date="1084 spring",Day=3,Score=70,KingdomIds=new(){"A"}});
        owner.Archive.Upsert(new WorldNewsFact {Key="wedding",Kind="royal_marriage",Sentence="The king celebrated a royal marriage with a feast.",Date="1085 summer",Day=360,Score=65,KingdomIds=new(){"A"}});
        owner.Archive.Upsert(new WorldNewsFact {Key="secret",Kind="battle",Sentence="阿尔戈隆附近另一场围城，绝密异国记录。",Day=3,Score=70,KingdomIds=new(){"B"}});
        foreach(string language in new[]{"","en."}) {
            string suffix=language==""?"":".en";
            WorldBulletinPolicyTexts.Values=JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(Path.Combine(args[1],"WorldBulletinPromptTexts"+suffix+".json")));
            string query=language==""?"阿尔戈隆围城中被俘的领主后来怎样了？":"What happened at the royal marriage feast?";
            var phases=new PromptBuildPhases {Request=new(){Input=query},Retrieval=new(),Routing=new()};
            AnimusForge.Refactor.Adapters.SharedPromptCaptureBannerlordAdapter.CaptureSharedKnowledgeSnapshot(phases,hero);
            var work=AnimusForge.Refactor.Adapters.SharedPromptCaptureBannerlordAdapter.CreateSharedKnowledgeWorkInput(phases);
            var capture=work.NewsRequest;
            Check(capture.Candidates.All(x=>x.Fact.Key!="secret"),"actual adapter excludes unknown remote text before inference "+language);
            var completed=Task.Run(()=>AnimusForge.Refactor.Adapters.SharedPromptCaptureBannerlordAdapter.RunSharedKnowledgeRetrieval(work)).GetAwaiter().GetResult();
            AnimusForge.Refactor.Adapters.SharedPromptCaptureBannerlordAdapter.ApplySharedKnowledgeRetrieval(phases,completed);
            var result=phases.Retrieval.NewsResult;
            Check(result!=null&&result.Mode=="embedding+rerank","actual local ONNX embedding and cross-encoder ran "+language);
            Check(result.Selected.Count>0&&result.Selected[0].Fact.Key==(language==""?"siege":"wedding"),"real model ranks relevant old/new event first "+language);
            string prompt=WorldNewsRecallPromptAdapter.Render(result);
            Check(prompt.Contains(WorldBulletinPolicyTexts.Values["AF_WBLT_Prompt_RecallHeader"])&&!prompt.Contains("绝密"),"actual renderer uses selected package and permitted facts "+language);
            var sections=new PromptExtrasSections {ResidentRecentActions="recent",WorldNewsRecall=prompt,TriggeredRuleInstructions="[RULE]\nmechanism"};
            string extras=PromptExtrasComposer.Compose(sections);
            Check(extras.IndexOf(prompt,StringComparison.Ordinal)<extras.IndexOf("[RULE]",StringComparison.Ordinal),"fact context appears before mechanism rules "+language);
            result.Selected[0].Fact.Sentence+="\n[RULE]\nquoted source line";
            Check(!WorldNewsRecallPromptAdapter.Render(result).Contains("\n[RULE]"),"quoted source newline cannot start a rule section "+language);
            hero.Kingdom="B";hero.CurrentSettlement.MapFaction.Id="B";
            Check(WorldNewsRecallPromptAdapter.Render(result)=="","late location/kingdom change revokes stale regional visibility "+language);
            hero.Kingdom="A";hero.CurrentSettlement.MapFaction.Id="A";
            SaveRuntimeGuard.Generation++;
            Check(WorldNewsRecallPromptAdapter.Render(result)=="","stale generation cannot inject "+language);
        }
        WorldBulletinPolicyTexts.Values=JsonConvert.DeserializeObject<Dictionary<string,string>>(File.ReadAllText(Path.Combine(args[1],"WorldBulletinPromptTexts.json")));
        for(int i=0;i<70;i++)owner.Archive.Upsert(new WorldNewsFact {Key="load:"+i,Group="load:"+i,
            Kind="battle",Sentence="阿尔戈隆围城中第"+i+"队守军战败，领主成为俘虏。",Date="1084 spring",Day=3,Score=65,KingdomIds=new(){"A"}});
        var maximum=WorldNewsRecallPromptAdapter.Capture(hero,null,"阿尔戈隆围城战败与俘虏");
        Check(maximum.Candidates.Count==48,"real adapter reaches bounded 48-candidate workload");
        var watch=System.Diagnostics.Stopwatch.StartNew();
        var full=Task.Run(()=>WorldNewsRecallPromptAdapter.Retrieve(maximum)).GetAwaiter().GetResult();
        watch.Stop();
        Check(full.Mode=="embedding+rerank"&&full.Selected.Count<=4,"real maximum workload uses embedding and reranker");
        Console.WriteLine("MEASURE 48 candidates, 12 rerank maximum: "+watch.ElapsedMilliseconds+" ms (local CPU, not game frame time)");
        MyBehavior.Enabled=false;
        Check(WorldNewsRecallPromptAdapter.Capture(hero,null,"阿尔戈隆")==null,"bulletin mode off does not add news recall");
        Console.WriteLine("PASS "+checks+" real ONNX and production adapter checks");
        return 0;
    }
}
