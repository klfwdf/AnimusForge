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
        var orch = new PromptOrch(world);
        var round = world.Round;
        var doc = world.Document;
        string opening = WorldDiplomacyPromptComposer.BuildAutonomousOpeningPrompt(world, orch, "a", "r", new() { "a", "missing", "dead", "vassal", "b" });
        Test.True(opening.Contains("- b=") && !opening.Contains("- a=") && !opening.Contains("- dead=") && !opening.Contains("- vassal="), "080 opening excludes self/missing/eliminated/dependent kingdoms");
        Test.True(opening.Contains("上限（包括发起国）=3") && opening.Contains("战争判断="), "080 opening participant cap and war facts");
        Test.True(opening.IndexOf("发文国：a") < opening.IndexOf("- b="), "080 author prefix before world candidates");
        Test.True(WorldDiplomacyPromptComposer.BuildAutonomousOpeningPrompt(world, orch, null, "r", null) == "", "080 null author");
        foreach (int activity in new[] { 0, 1, 2 })
        {
            world.Activity = activity;
            string p = WorldDiplomacyPromptComposer.BuildGenerationPrompt(world, orch, "a", "b", null, true, doc, true, "r", true, new() { "b" }, true);
            Test.True(p.Contains("来源公文ID：source") && p.Contains("不得附加、修改条款或另提和平方案"), "080 exact incoming peace source");
            Test.True(p.Contains("原案：来源=source|action=action-b") && p.Contains("【对象决策硬事实】b】"), "080 source action and target hard-fact section");
            Test.True(p.Contains("对象国迟迟没有回应") && p.Contains("- c="), "080 reminder and other-kingdom relationship snapshot");
            Test.True(p.Contains(activity == 0 ? "活跃程度为低" : activity == 2 ? "活跃程度为高" : "活跃程度为标准"), "080 activity modes");
        }
        Test.True(WorldDiplomacyPromptComposer.BuildGenerationPrompt(world, orch,"a",null,null,true,null,false,"r",false,null,false)=="", "080 response requires target");
        Test.True(WorldDiplomacyPromptComposer.BuildGenerationPrompt(world, orch,"a",null,null,false,null,false,"r",false,new(){"b"},false).Contains("- b="), "080 untargeted opening dispatch");
        world.Events.Clear();
        string relay = WorldDiplomacyPromptComposer.BuildRelayConversationTurnPrompt(world, orch, round, "a", "b", doc, true);
        Test.True(world.Events[0] == "prune" && relay.Contains("本篇优先任务：回应玩家王国宣言"), "080 prune before relay and player response priority");
        Test.True(relay.Contains("允许动作对象=b,c") && relay.Contains("最多接受一份") && relay.Contains("【对象决策硬事实】c】"), "080 stable target order and multi-peace constraint");
        Test.True(relay.Contains("当前已进入最后阶段") && relay.Contains("当前可提出和平方案的对象=c"), "080 relay timing and peace options");
        string plan = WorldDiplomacyPromptComposer.BuildRoundPlanPrompt(world, orch, doc, new(){"b","missing"});
        Test.True(plan.Contains("【MODE=ROUND_PLAN】") && plan.Contains("- b=") && !plan.Contains("- missing"), "080 round plan mode and candidates");
        Test.True(WorldDiplomacyPromptComposer.BuildRoundPlanSystemPrompt(world, round).StartsWith("stable-contract"), "080 stable system contract");
        world.Events.Clear();
        string analysis = WorldDiplomacyPromptComposer.BuildAnalysisPrompt(world, orch, doc);
        Test.True(world.Events[0] == "prune" && analysis.Contains("【MODE=ANALYZE】") && analysis.Contains("公文正文："), "080 player analysis admission and protocol");
        Test.True(!analysis.Contains("- dead =") && !analysis.Contains("- a ="), "080 analysis excludes eliminated and author");
        Test.True(analysis.Contains("原样接受或明确拒绝") && analysis.Contains("来源=source"), "080 analysis original peace terms");
        var source = new WorldDiplomacyJob { JobId="job", Kind="generate", RoundId="r", SourceDocumentId="source", AuthorKingdomId="a", TargetKingdomId="b", SystemPrompt="stable-system", UserPrompt="original-user", MaxTokens=900, HistorySnapshotHash="hash", HistorySnapshotThroughSequence=7, HistoryThroughSequence=7, HistoryRevision=2, IsRelayTurn=true };
        var before = WorldDiplomacyPromptContractRules.BuildLlmMessagesForJob(source, world.BuildCanonicalHistoryBlock);
        WorldDiplomacyDraftRepairApplication.RejectGeneratedDraftBeforePublication(world, orch, source, "rejected draft", "a", "b", "invalid_target", new JObject());
        Test.True(world.Enqueued.Count == 1 && world.Abandoned.Count == 0, "080 semantic repair once before abandonment");
        var repair = world.Enqueued[0];
        Test.True(repair.SemanticRepairAttempts == source.SemanticRepairAttempts+1 && repair.SourceDocumentId == "source" && repair.RoundId == "r", "080 repair source identity");
        Test.True(repair.PresentedLegalActionSignature == "legal-signature" && repair.HistorySnapshotThroughSequence == 7, "080 frozen history and legality signature");
        Test.True(repair.LlmMessages.Count == before.Count+2 && repair.LlmMessages[^2].Role == "assistant" && repair.LlmMessages[^2].Content == "rejected draft" && repair.LlmMessages[^1].Role == "user", "080 repair chain role order");
        for(int i=0;i<before.Count;i++) Test.True(repair.LlmMessages[i].Role==before[i].Role && repair.LlmMessages[i].Content==before[i].Content, "080 byte-preserved rejected prefix");
        source.SemanticRepairAttempts=WorldDiplomacyPromptContractRules.MaxGeneratedDraftRepairAttempts;
        WorldDiplomacyDraftRepairApplication.RejectGeneratedDraftBeforePublication(world, orch, source,"bad","a","b","invalid",null);
        Test.True(world.Enqueued.Count==1 && world.Abandoned.Count==1, "080 repair bound");
        source.SemanticRepairAttempts=0; world.Authorized.Clear();
        WorldDiplomacyDraftRepairApplication.RejectGeneratedDraftBeforePublication(world, orch, source,"bad","a","b","invalid",null);
        Test.True(world.Enqueued.Count==1 && world.Abandoned.Count==2, "080 no legal action abandons without enqueue");
        WorldDiplomacyDraftRepairApplication.RejectGeneratedDraftBeforePublication(world, orch, source,"bad",null,"b",null,null);
        Test.True(world.Abandoned[^1]=="generated_party_missing", "080 missing author fails before correction");
    }
}


    internal sealed class PromptOrch : FakeOrchestration
    {
        private readonly PromptWorldFixture _w;
        internal PromptOrch(PromptWorldFixture w) { _w = w; }
        public override void PruneInvalidOffers(WorldDiplomacyRound round) => _w.PruneInvalidOffers(round);
        public override List<string> GetResultSettlementActionableTargetIds(WorldDiplomacyRound round, string authorId) => _w.GetResultSettlementActionableTargets(round, authorId);
        public override List<string> BuildLegalDiplomaticActionIntents(WorldDiplomacyRound round, string authorId, string targetId) => _w.BuildLegalDiplomaticActionIntents(round, authorId, targetId);
        public override List<string> BuildLegalDiplomaticDeclarationIntents(WorldDiplomacyRound round, string authorId, string targetId, bool isRelayTurn, string resultSettlementSlotId, bool isExternalResponseOnly, WorldDiplomacyDocument responseSource) => _w.BuildLegalDiplomaticDeclarationIntents(round, authorId, targetId, isRelayTurn, isExternalResponseOnly, responseSource);
        public override Dictionary<string, List<string>> BuildLegalDiplomaticDeclarationIntentMap(WorldDiplomacyRound round, string authorId, List<string> ids, bool isRelayTurn, string resultSettlementSlotId, bool isExternalResponseOnly, WorldDiplomacyDocument responseSource) => _w.BuildLegalDiplomaticDeclarationIntentMap(round, authorId, ids, isRelayTurn, resultSettlementSlotId, isExternalResponseOnly, responseSource);
        public override bool HasCessionBoundMultiplePeaceAcceptanceOptions(WorldDiplomacyRound round, string authorId, IReadOnlyDictionary<string, List<string>> legalActionsByTarget) => _w.HasCessionBoundMultiplePeaceAcceptanceOptions(round, authorId, legalActionsByTarget?.ToDictionary(x => x.Key, x => x.Value));
        public override List<string> BuildPotentialDiplomaticActionIntents(string firstId, string secondId) => new() { "accept_peace", "reject_peace" };
        public override List<string> GetAuthorizedGenerationTargetIds(WorldDiplomacyJob source, WorldDiplomacyRound round, string authorId) => _w.GetAuthorizedGenerationTargetIds(source, round, authorId);
        public override string BuildCurrentLegalDiplomaticOptions(WorldDiplomacyRound round, string authorId, IEnumerable<string> targetIds, bool isRelayTurn, string resultSettlementSlotId, bool isExternalResponseOnly, WorldDiplomacyDocument responseSource) => _w.BuildCurrentLegalDiplomaticOptions(round, authorId, targetIds?.ToList(), isRelayTurn, resultSettlementSlotId, isExternalResponseOnly, responseSource);
        public override string BuildCanonicalHistoryBlock(long throughSequence) => _w.BuildCanonicalHistoryBlock(throughSequence);
        public override string BuildGenerationLegalActionSignature(WorldDiplomacyJob job) => _w.BuildGenerationLegalActionSignature(job);
        public override void EnqueueJob(WorldDiplomacyJob job) => _w.EnqueueJob(job);
        public override void AbandonRejectedGeneration(WorldDiplomacyJob job, string authorId, string targetId, string reason) => _w.Abandoned.Add(reason);
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
    public IReadOnlyList<string> CurrentWarKingdomIds(string authorId)=>new List<string>{"b","c"};
    public IReadOnlyList<string> IndependentKingdomIds()=>new List<string>{"c","a","b"};
    public bool IsAtWar(string author,string target)=>true;
    public WorldDiplomacyRound ResolveRound(string id)=>Round;
    public WorldDiplomacyDocument ResolveDocument(string id)=>id=="source" ? Document : null;
    public IReadOnlyList<WorldDiplomacyDocument> Documents()=>new List<WorldDiplomacyDocument>{Document};
    public IReadOnlyList<WorldDiplomacyThreat> DiplomaticThreats()=>new List<WorldDiplomacyThreat>();
    public string KingdomName(string id)=>id??"unknown";
    public string RulerName(string id)=>"ruler-"+id;
    public int GetRoundParticipantLimit()=>3;
    public int GetActivityLevel()=>Activity;
    public int CurrentDay()=>9;
    public int DaysPerYear()=>84;
    public int RecentBattleRetentionDays()=>14;
    public int NegativeReputationFactRetentionDays()=>21;
    public string FormatCampaignDate(int day)=>"day"+day;
    public string GetCommonDiplomacyContract(WorldDiplomacyRound round)=>"stable-contract";
    public string BuildWorldDiplomacyVassalageSnapshot()=>"vassal-facts";
    public string BuildPolicySnapshot(string id)=>"policy-"+id;
    public string BuildGatheringSnapshot(IEnumerable<string> ids,int count)=>"gathering";
    public IReadOnlyList<string> CessionCandidates(string cedingId,string receivingId,float cessionScore)=>new List<string>();
    public WarSituationSnapshot WarSituation(string authorId,string targetId)=>new(){IsAtWar=true,WarDays=12,AuthorStrength=100f,TargetStrength=80f,AuthorProgress=10f,TargetProgress=5f,AuthorOtherWars=0,TargetOtherWars=1,AuthorPeacePressure=10f,TargetPeacePressure=20f,AuthorSuggestedTribute=100,TargetSuggestedTribute=200,AuthorCessionScore=0.3f,TargetCessionScore=0.4f};
    public WorldDiplomacyRealmRelationProfile RelationProfile(string authorId,string targetId)=>new();
    public WorldDiplomacyBorderRelation BorderRelation(string authorId,string targetId)=>new();
    public int RulerRelation(string authorId,string targetId)=>3;
    public int CulturalClaimCount(string authorId,string targetId)=>2;
    public int NationalPrestige(string kingdomId)=>50;
    public int InternationalReputation(string kingdomId)=>50;
    public int WarPressure(string authorId,string targetId)=>10;
    public string RulerVoiceContext(string kingdomId)=>"voice-"+kingdomId;
    public string RealmInstitutionalVoiceContext(string kingdomId)=>"realm-"+kingdomId;
    public string AuthorRulerFamilyContext(string kingdomId)=>"family-"+kingdomId;
    public string BilateralRulerFamilyContext(string authorId,string targetId)=>"bilateral-family";
    public string RecentNativeSignalContext(string authorId,string targetId)=>"native-"+authorId+"-"+targetId;
    public string RecentBilateralBattleContext(string authorId,string targetId)=>"battle-"+authorId+"-"+targetId;
    public WorldDiplomacyRulerCaptivity AuthorRulerCaptivity(string authorId)=>new();
    public void PruneInvalidOffers(WorldDiplomacyRound round)=>Events.Add("prune");
    public List<string> GetResultSettlementActionableTargets(WorldDiplomacyRound round,string author)=>new(){"b","c"};
    public List<string> BuildLegalDiplomaticActionIntents(WorldDiplomacyRound round,string author,string target)=>new(){"accept_peace","reject_peace"};
    public List<string> BuildLegalDiplomaticDeclarationIntents(WorldDiplomacyRound round,string author,string target,bool isRelayTurn,bool isExternalResponseOnly,WorldDiplomacyDocument responseSource)=>BuildLegalDiplomaticActionIntents(round,author,target);
    public Dictionary<string,List<string>> BuildLegalDiplomaticDeclarationIntentMap(WorldDiplomacyRound round,string author,List<string> ids,bool isRelayTurn,string resultSettlementSlotId,bool isExternalResponseOnly,WorldDiplomacyDocument responseSource)=>new(){["c"]=new(){"propose_peace"},["b"]=new(){"accept_peace","reject_peace"}};
    public WorldDiplomacyRoundOffer FindRequiredPeaceOfferResponse(WorldDiplomacyRound round,string author,string resultSettlementSlotId,bool isExternalResponseOnly,string sourceDocumentId,bool requireAnyOpenPeaceOffer)=>Offer;
    public bool HasCessionBoundMultiplePeaceAcceptanceOptions(WorldDiplomacyRound round,string author,Dictionary<string,List<string>> actions)=>true;
    public List<string> GetAuthorizedGenerationTargetIds(WorldDiplomacyJob source,WorldDiplomacyRound round,string author)=>Authorized;
    public bool IsAlly(string author,string target)=>false;
    public bool HasTradeAgreement(string author,string target)=>false;
    public string BuildGovernmentHardFact(string author)=>"government";
    public string BuildCurrentLegalDiplomaticOptions(WorldDiplomacyRound round,string author,List<string> ids,bool isRelayTurn,string resultSettlementSlotId,bool isExternalResponseOnly,WorldDiplomacyDocument source)=>"legal-options";
    public string BuildCanonicalHistoryBlock(long throughSequence)=>"current archive";
    public string NewId(string prefix)=>prefix+"-1";
    public string BuildGenerationLegalActionSignature(WorldDiplomacyJob job)=>"legal-signature";
    public void EnqueueJob(WorldDiplomacyJob job)=>Enqueued.Add(job);
    public void Log(string text)=>Events.Add("log:"+text);
    public void AbandonRejectedGeneration(WorldDiplomacyJob job,string author,string target,string reason)=>Abandoned.Add(reason);
}
