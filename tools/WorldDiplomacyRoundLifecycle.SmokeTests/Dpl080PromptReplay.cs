#nullable disable
using System.Text;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json.Linq;

// Production composition/repair code, with explicit synchronous world-fact substitutes.
internal static class Dpl080PromptReplay
{
    internal static void Run()
    {
        var fallback = JObject.Parse(WorldDiplomacyPromptComposer.BuildFallbackAnalysisJson(null, "b"));
        Test.True((string)fallback["intent"] == "statement" && (string)fallback["commitment"] == "non_binding" && !(bool)fallback["requires_response"], "080 degraded analysis never fabricates a binding action");
        Test.True((string)fallback["primary_target_kingdom_id"] == "b" && (double)fallback["confidence"] == 0, "080 fallback target and uncertainty");
        var world = new PromptWorldFixture();
        var round = world.Round;
        var doc = world.Document;
        string opening = WorldDiplomacyPromptComposer.BuildAutonomousOpeningPrompt(world, "a", "r", new() { "a", "missing", "dead", "vassal", "b" });
        Test.True(opening.Contains("candidate=b") && !opening.Contains("candidate=a") && !opening.Contains("candidate=dead") && !opening.Contains("candidate=vassal"), "080 opening excludes self/missing/eliminated/dependent kingdoms");
        Test.True(opening.Contains("上限（包括发起国）=3") && opening.Contains("战争判断="), "080 opening participant cap and war facts");
        Test.True(opening.IndexOf("author=a") < opening.IndexOf("candidate=b"), "080 author prefix before world candidates");
        Test.True(WorldDiplomacyPromptComposer.BuildAutonomousOpeningPrompt(world, null, "r", null) == "", "080 null author");
        foreach (int activity in new[] { 0, 1, 2 })
        {
            world.Activity = activity;
            string p = WorldDiplomacyPromptComposer.BuildGenerationPrompt(world, "a", "b", null, true, doc, true, "r", true, new() { "b" }, true);
            Test.True(p.Contains("来源公文ID：source") && p.Contains("不得附加、修改条款或另提和平方案"), "080 exact incoming peace source");
            Test.True(p.Contains("原案：来源=source|action=action-b") && p.Contains("target=b:peace=False"), "080 source action and forbidden counter-offer terms");
            Test.True(p.Contains("对象国迟迟没有回应") && p.Contains("related=b"), "080 reminder and relationship material");
            Test.True(p.Contains(activity == 0 ? "活跃程度为低" : activity == 2 ? "活跃程度为高" : "活跃程度为标准"), "080 activity modes");
        }
        Test.True(WorldDiplomacyPromptComposer.BuildGenerationPrompt(world,"a",null,null,true,null,false,"r",false,null,false)=="", "080 response requires target");
        Test.True(WorldDiplomacyPromptComposer.BuildGenerationPrompt(world,"a",null,null,false,null,false,"r",false,new(){"b"},false).Contains("candidate=b"), "080 untargeted opening dispatch");
        world.Events.Clear();
        string relay = WorldDiplomacyPromptComposer.BuildRelayConversationTurnPrompt(world, round, "a", "b", doc, true);
        Test.True(world.Events[0] == "prune" && relay.Contains("本篇优先任务：回应玩家王国宣言"), "080 prune before relay and player response priority");
        Test.True(relay.Contains("允许动作对象=b,c") && relay.Contains("最多接受一份") && relay.Contains("target=c:peace=True"), "080 stable target order and multi-peace constraint");
        Test.True(relay.Contains("当前已进入最后阶段") && relay.Contains("当前可提出和平方案的对象=c"), "080 relay timing and peace options");
        string plan = WorldDiplomacyPromptComposer.BuildRoundPlanPrompt(world, doc, new(){"b","missing"});
        Test.True(plan.Contains("【MODE=ROUND_PLAN】") && plan.Contains("candidate=b") && !plan.Contains("candidate=missing"), "080 round plan mode and candidates");
        Test.True(WorldDiplomacyPromptComposer.BuildRoundPlanSystemPrompt(world, round).StartsWith("stable-contract"), "080 stable system contract");
        world.Events.Clear();
        string analysis = WorldDiplomacyPromptComposer.BuildAnalysisPrompt(world, doc);
        Test.True(world.Events[0] == "prune" && analysis.Contains("【MODE=ANALYZE】") && analysis.Contains("公文正文："), "080 player analysis admission and protocol");
        Test.True(!analysis.Contains("- dead =") && !analysis.Contains("- a ="), "080 analysis excludes eliminated and author");
        Test.True(analysis.Contains("原样接受或明确拒绝") && analysis.Contains("来源=source"), "080 analysis original peace terms");
        var source = new WorldDiplomacyJob { JobId="job", Kind="generate", RoundId="r", SourceDocumentId="source", AuthorKingdomId="a", TargetKingdomId="b", SystemPrompt="stable-system", UserPrompt="original-user", MaxTokens=900, HistorySnapshotHash="hash", HistorySnapshotThroughSequence=7, HistoryThroughSequence=7, HistoryRevision=2, IsRelayTurn=true };
        var before = WorldDiplomacyPromptContractRules.BuildLlmMessagesForJob(source, world.BuildCanonicalHistoryBlock);
        WorldDiplomacyDraftRepairApplication.RejectGeneratedDraftBeforePublication(world, source, "rejected draft", "a", "b", "invalid_target", new JObject());
        Test.True(world.Enqueued.Count == 1 && world.Abandoned.Count == 0, "080 semantic repair once before abandonment");
        var repair = world.Enqueued[0];
        Test.True(repair.SemanticRepairAttempts == source.SemanticRepairAttempts+1 && repair.SourceDocumentId == "source" && repair.RoundId == "r", "080 repair source identity");
        Test.True(repair.PresentedLegalActionSignature == "legal-signature" && repair.HistorySnapshotThroughSequence == 7, "080 frozen history and legality signature");
        Test.True(repair.LlmMessages.Count == before.Count+2 && repair.LlmMessages[^2].Role == "assistant" && repair.LlmMessages[^2].Content == "rejected draft" && repair.LlmMessages[^1].Role == "user", "080 repair chain role order");
        for(int i=0;i<before.Count;i++) Test.True(repair.LlmMessages[i].Role==before[i].Role && repair.LlmMessages[i].Content==before[i].Content, "080 byte-preserved rejected prefix");
        source.SemanticRepairAttempts=WorldDiplomacyPromptContractRules.MaxGeneratedDraftRepairAttempts;
        WorldDiplomacyDraftRepairApplication.RejectGeneratedDraftBeforePublication(world, source,"bad","a","b","invalid",null);
        Test.True(world.Enqueued.Count==1 && world.Abandoned.Count==1, "080 repair bound");
        source.SemanticRepairAttempts=0; world.Authorized.Clear();
        WorldDiplomacyDraftRepairApplication.RejectGeneratedDraftBeforePublication(world,source,"bad","a","b","invalid",null);
        Test.True(world.Enqueued.Count==1 && world.Abandoned.Count==2, "080 no legal action abandons without enqueue");
        WorldDiplomacyDraftRepairApplication.RejectGeneratedDraftBeforePublication(world,source,"bad",null,"b",null,null);
        Test.True(world.Abandoned[^1]=="generated_party_missing", "080 missing author fails before correction");
    }
}

internal sealed class PromptWorldFixture : IWorldDiplomacyDraftRepairWorld
{
    internal readonly List<string> Events = new();
    internal readonly List<WorldDiplomacyJob> Enqueued = new();
    internal readonly List<string> Abandoned = new();
    internal readonly List<string> Authorized = new(){"b"};
    internal int Activity;
    internal readonly WorldDiplomacyRoundOffer Offer = new(){SourceDocumentId="source",SourceActionId="action-b",ProposerKingdomId="b",TargetKingdomId="a",Intent="propose_peace",Status="open",CreatedDay=1};
    internal readonly WorldDiplomacyRound Round;
    internal readonly WorldDiplomacyDocument Document = new(){DocumentId="source",RoundId="r",AuthorKingdomId="a",AuthorKingdomName="A",TargetKingdomId="b",Title="original offer",Body="original terms",IsPlayerAuthored=true};
    internal PromptWorldFixture() { Round=new(){RoundId="r",RoundTopic="topic",StartedDay=0,SoftEndDay=10,RelayRouteKingdomIds=new(){"a","c","b"},PendingOffers=new(){Offer},ExternalOpeningContext="external fact"}; }
    public string ResolveKingdom(string id) => id==null||id=="missing"||id=="dead" ? null : id;
    public bool IsEliminated(string id)=>id=="dead";
    public bool HasIndependentWorldDiplomacyAuthority(string id)=>id!="vassal";
    public IEnumerable<string> KingdomIds()=>new[]{"c","a","dead","b"};
    public bool IsAtWar(string author,string target)=>true;
    public WorldDiplomacyRound ResolveRound(string id)=>Round;
    public WorldDiplomacyDocument ResolveDocument(string id)=>id=="source" ? Document : null;
    public string KingdomName(string id)=>id??"unknown";
    public string RulerName(string id)=>"ruler-"+id;
    public int GetRoundParticipantLimit()=>3;
    public int GetActivityLevel()=>Activity;
    public int CurrentDay()=>9;
    public string GetCommonDiplomacyContract(WorldDiplomacyRound round)=>"stable-contract";
    public string BuildWorldDiplomacyVassalageSnapshot()=>"vassal-facts";
    public string BuildPolicySnapshot(string id)=>"policy-"+id;
    public string BuildGatheringSnapshot(IEnumerable<string> ids,int count)=>"gathering";
    public string BuildCompactRoundPlanCandidateLine(string author,string target,WorldDiplomacyRound round)=>"candidate="+target;
    public string BuildWarDecisionContext(string author,string target,bool includePeaceNegotiationTerms)=>"war-"+includePeaceNegotiationTerms;
    public void PruneInvalidOffers(WorldDiplomacyRound round)=>Events.Add("prune");
    public List<string> GetResultSettlementActionableTargets(WorldDiplomacyRound round,string author)=>new(){"b","c"};
    public List<string> BuildLegalDiplomaticActionIntents(WorldDiplomacyRound round,string author,string target)=>new(){"accept_peace","reject_peace"};
    public List<string> BuildLegalDiplomaticDeclarationIntents(WorldDiplomacyRound round,string author,string target,bool isRelayTurn,bool isExternalResponseOnly,WorldDiplomacyDocument responseSource)=>BuildLegalDiplomaticActionIntents(round,author,target);
    public Dictionary<string,List<string>> BuildLegalDiplomaticDeclarationIntentMap(WorldDiplomacyRound round,string author,List<string> ids,bool isRelayTurn,string resultSettlementSlotId,bool isExternalResponseOnly,WorldDiplomacyDocument responseSource)=>new(){["c"]=new(){"propose_peace"},["b"]=new(){"accept_peace","reject_peace"}};
    public WorldDiplomacyRoundOffer FindRequiredPeaceOfferResponse(WorldDiplomacyRound round,string author,string resultSettlementSlotId,bool isExternalResponseOnly,string sourceDocumentId,bool requireAnyOpenPeaceOffer)=>Offer;
    public bool HasCessionBoundMultiplePeaceAcceptanceOptions(WorldDiplomacyRound round,string author,Dictionary<string,List<string>> actions)=>true;
    public void AppendDiplomaticAuthorDecisionContext(StringBuilder sb,string author,string roundId)=>sb.AppendLine("author="+author);
    public void AppendDiplomaticTargetDecisionContext(StringBuilder sb,WorldDiplomacyRound round,string author,string target,bool includePeaceNegotiationTerms,IReadOnlyCollection<string> legalActions)=>sb.AppendLine("target="+target+":peace="+includePeaceNegotiationTerms);
    public void AppendRulerCaptivityDecisionContext(StringBuilder sb,string author,string target)=>sb.AppendLine("captivity");
    public void AppendOtherKingdomRelationshipContext(StringBuilder sb,string author,IEnumerable<string> ids)=>sb.AppendLine("related="+string.Join(",",ids));
    public void AppendRelayResponseSourceContext(StringBuilder sb,WorldDiplomacyRound round,string author,WorldDiplomacyDocument source,string requiredId)=>sb.AppendLine("relay-source="+requiredId);
    public void AppendDiplomaticThreatAnalysisContext(StringBuilder sb,string author)=>sb.AppendLine("threat-facts");
    public List<string> GetAuthorizedGenerationTargetIds(WorldDiplomacyJob source,WorldDiplomacyRound round,string author)=>Authorized;
    public string BuildBilateralState(string author,string target)=>"at-war";
    public string BuildGovernmentHardFact(string author)=>"government";
    public string BuildCurrentLegalDiplomaticOptions(WorldDiplomacyRound round,string author,List<string> ids,bool isRelayTurn,string resultSettlementSlotId,bool isExternalResponseOnly,WorldDiplomacyDocument source)=>"legal-options";
    public string BuildCanonicalHistoryBlock(long throughSequence)=>"current archive";
    public string NewId(string prefix)=>prefix+"-1";
    public string BuildGenerationLegalActionSignature(WorldDiplomacyJob job)=>"legal-signature";
    public void EnqueueJob(WorldDiplomacyJob job)=>Enqueued.Add(job);
    public void Log(string text)=>Events.Add("log:"+text);
    public void AbandonRejectedGeneration(WorldDiplomacyJob job,string author,string target,string reason)=>Abandoned.Add(reason);
}
