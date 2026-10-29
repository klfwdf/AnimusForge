using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialSupplyReplay
{
    private static object AuditCall(WorldDiplomacyOrchestration owner, string method, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(owner,args)!;

    internal static void AuditGapRegressions()
    {
        var (h,o,j)=Start(); var round=o.ResolveRound(j.RoundId);
        const string outcome="Tax claim withdrawn; trade opens ONLY after release of the captive. Neither court admitted guilt.";
        o.CurrentStorage.EventAnnouncements.Add(new(){EventId="prior",Stage="started",State="public",Title="Prior dispute",Body="A tax demand challenged a conditional trade agreement.",Day=1});
        o.CurrentStorage.EventAnnouncements.Add(new(){EventId="prior",Stage="ended",State="public",Body=outcome,Day=2});
        o.CurrentStorage.CompletedRounds.Add(new(){RoundId="prior",EventBackground="A tax demand challenged a conditional trade agreement.",EventOutcome="resolved",EventOutcomeReason="A diplomatic action was executed.",LastActivityDay=2});
        var stories=JArray.FromObject(AuditCall(o,"ReadPriorEventStories",round));
        Test.True((string)stories.Single()["EventOutcomeReason"]! == outcome,"public ending preserves exact conditional outcome over generic completed status");
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
        round=o.ResolveRound(j.RoundId); j=o.CurrentStorage.Jobs.Single(x=>x.JobId==j.JobId);
        o.CurrentStorage.CompletedRounds.Clear();
        Test.True((string)JArray.FromObject(AuditCall(o,"ReadPriorEventStories",round)).Single()["EventOutcomeReason"]! == outcome,
            "public case result remains readable after save/load and completed display-cache eviction");

        Complete(o,j,new(){["candidates"]=new JArray(Candidate())}); Complete(o,j,Compare());
        AuditCall(o,"ContinueMaterialSearch",j,round,false,false);
        var next=Candidate(); next["conflict"]="A new witness disputes the location of the original hearing";
        Complete(o,j,new(){["candidates"]=new JArray(next)}); Complete(o,j,Compare());
        Test.True(o.CurrentStorage.MaterialLeads.Count==2 && o.CurrentStorage.MaterialLeads.Count(x=>x.Status=="failed_unused")==1
            && o.CurrentStorage.MaterialLeads.Single(x=>x.Status=="selected_unpublished").Candidate.Contains("new witness"),
            "same local candidate ID in another search batch cannot alias the failed old lead");
        j.EventSelectionStage="compare"; AuditCall(o,"CompleteMaterialSelection",j,round,Compare());
        Test.True(o.CurrentStorage.MaterialLeads.Count==2,"same comparison callback is idempotent within its source batch");

        // Force complete-request pressure after capture, including audit stage.
        foreach(string stage in new[]{"candidates","compare","expand"})
        {
            j.EventSelectionStage=stage; j.Kind="event_concept"; j.SystemPrompt=""; j.SuspendedReason="";
            var sources=new JArray(new JObject{["key"]="world:current",["text"]="Current world"},
                new JObject{["key"]="lore:succession",["text"]="Complete selected evidence"},
                new JObject{["key"]="optional",["text"]=new string('x',140000)});
            j.EventMaterialSnapshot=sources.ToString();
            j.UserPrompt=new JObject{["sources"]=sources,["prior_events"]=stories.DeepClone()}.ToString();
            AuditCall(o,"FinishMaterialRequest",j);
            var actual=JObject.Parse(j.UserPrompt);
            Test.True(JToken.DeepEquals(actual["prior_events"],stories) && !j.UserPrompt.Contains(new string('x',2000)),
                "budget sheds optional whole sources before complete prior motives/outcomes: "+stage);
        }
        j.EventSelectionStage="expand"; j.SystemPrompt=""; j.SuspendedReason="";
        j.UserPrompt=new JObject{["sources"]=new JArray(new JObject{["key"]="lore:succession",["text"]=new string('x',140000)}),["prior_events"]=stories.DeepClone()}.ToString();
        AuditCall(o,"FinishMaterialRequest",j);
        Test.True(j.SuspendedReason.Contains("required_evidence_budget_exceeded") && j.RequestPreparationPending
            && JToken.DeepEquals(JObject.Parse(j.UserPrompt)["prior_events"],stories),"required evidence overflow holds rather than silently erasing dedup evidence: "+j.SuspendedReason+" input="+h.EstimateTokens(j.UserPrompt));

        StoryCoverage(); ResponseContinuity();
        Console.WriteLine("PASS audit gaps: exact outcome, source-budget preservation, batch identity, evidence-bound diversity and denial-response guards; semantic judgements are controlled fixtures.");
    }

    private static void StoryCoverage()
    {
        var (_,o,j)=Start(); o.PrepareSharedRequest(j);
        j.Kind="event_concept_audit"; j.EventSelectionStage="expand";
        j.EventSelectedCandidate=Candidate().ToString();
        j.PrivacyDraft=new JObject{["background"]="A new witness requests a hearing to clarify the oath without reopening the withdrawn tax claim."}.ToString();
        j.UserPrompt=new JObject{["sources"]=JArray.Parse(j.EventMaterialSnapshot),["prior_events"]=new JArray(new JObject{
            ["RoundId"]="old",["EventBackground"]="The court demanded taxes and alleged foreign interference.",
            ["EventOutcomeReason"]="The tax demand was withdrawn."})}.ToString();
        AuditCall(o,"FinishMaterialRequest",j);
        var good=MaterialCountryThirdReplay.StoryReview(j);
        string latestReason = "";
        bool Validate(JObject review) { object[] args={j,new JObject{["story_review"]=review},"",false}; bool valid = (bool)AuditCall(o,"ValidateStoryReview",args); latestReason = (string)args[2]; return valid; }
        Test.True(Validate(good),"grounded old-material new-motive review can pass exact evidence binding");
        foreach(string defect in new[]{"missing","wrong-case","fabricated-quote","stale","duplicate","revisit-without-change"})
        {
            var bad=(JObject)good.DeepClone();var row=(JObject)bad["comparisons"]![0]!;
            if(defect=="missing") ((JArray)bad["comparisons"]!).Clear();
            if(defect=="wrong-case") row["event_id"]="not-offered";
            if(defect=="fabricated-quote") row["prior_quote"]="The foreign court admitted guilt.";
            if(defect=="stale") bad["digest"]="old-draft";
            if(defect=="duplicate") row["verdict"]="duplicate";
            if(defect=="revisit-without-change") row["verdict"]="revisit";
            Test.True(!Validate(bad),"independent cross-case audit rejects "+defect);
        }
        var revisit=(JObject)good.DeepClone(); revisit["comparisons"]![0]!["verdict"]="revisit";
        revisit["comparisons"]![0]!["new_development_quote"]="A new witness requests a hearing";
        Test.True(Validate(revisit),"a substantiated revisit is not forbidden by material identity or theme");
        var wrongNested = (JObject)good.DeepClone();
        wrongNested["comparisons"]![0]!["draft_motive_quote"] = wrongNested["comparisons"]![0]!["draft_quote"]!.DeepClone();
        ((JObject)wrongNested["comparisons"]![0]!).Remove("draft_quote");
        Test.True(!Validate(wrongNested) && latestReason.Contains("draft_quote_not_draft_motive_quote"),
            "observed nested field mismatch receives specific same-review correction, not a generic invalid error");
        var request = JObject.Parse(j.UserPrompt); var prior = (JArray)request["prior_events"]!;
        foreach (string id in new[] { "old-2", "old-3" }) { var copy = (JObject)prior[0]!.DeepClone(); copy["RoundId"] = id; prior.Add(copy); }
        j.UserPrompt = request.ToString(); j.SystemPrompt = ""; AuditCall(o, "FinishMaterialRequest", j);
        var missingCase = MaterialCountryThirdReplay.StoryReview(j);
        ((JArray)missingCase["comparisons"]!).RemoveAt(2);
        Test.True(!Validate(missingCase) && latestReason.Contains("missing=old-3") && latestReason.Contains("expected_count=3"),
            "NPC novelty review still requires all three supplied cases and identifies the exact omitted case");
    }

    private static void ResponseContinuity()
    {
        var (_,o,j)=Start(); j.AuthorKingdomId="b"; j.TargetKingdomId="a"; j.SourceDocumentId="denial";
        o.AddDocument(new(){DocumentId="denial",RoundId=j.RoundId,AuthorKingdomId="a",TargetKingdomId="b",IsReadyForPublication=true,
            Body="We refuse to intervene and deny sheltering the rebels."});
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge,"b","denial",1);
        j.PrivacyDraft=new JObject{["body"]="You deny sheltering them, but our envoy reports seeing their banner at your gate; we seek an explanation."}.ToString();
        AuditCall(o,"PrepareDeclarationAudit",j);
        var evidence=(JObject)AuditCall(o,"ResponseContinuityEvidence",j);
        var review=new JObject{["digest"]=evidence["digest"]!.DeepClone(),["source_id"]="denial",["consistent"]=true,
            ["stance"]="disputed",["source_quote"]="deny sheltering the rebels",["draft_quote"]="You deny sheltering them, but our envoy reports seeing their banner at your gate",
            ["reason"]="The denial is acknowledged and the allegation attributed to an envoy rather than established as fact."};
        object[] args={j,new JObject{["response_continuity"]=review},"",false};
        Test.True((bool)AuditCall(o,"ValidateResponseContinuity",args),"reasoned disagreement need not accept a political denial as truth");
        review["consistent"]=false; review["reason"]="Draft ignored the denial and repeated an ungrounded allegation as an established fact.";
        o.TryCompleteDiplomacyPipeline(j,new(){Success=true,Content=new JObject{["safe"]=true,["position_owner_valid"]=true,["negotiation_consistent"]=true,["response_continuity"]=review}.ToString()});
        Test.True(j.Kind=="generate" && j.PrivacyRewrites==1 && !j.PrivacyApproved && o.CurrentStorage.Documents.Count==1,
            "independent continuity rejection actually routes to bounded rewrite instead of publication: "+j.Kind+":"+j.PrivacyRepairFeedback);
    }
}
