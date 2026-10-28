using System.Reflection;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Production owners and package readers, controlled provider judgements.
internal static partial class MaterialSupplyReplay
{
    private sealed class Host : EventStructureReplay.Host, IDiplomacyEventMaterialHost, IWorldDiplomacyRealmVoicePreparation
    {
        internal bool VoiceReady = true;
        internal int VoicePreparations;
        public bool PrepareRealmVoice(string kingdomId) { VoicePreparations++; return VoiceReady; }
        internal readonly List<(int Pass, string[] Countries)> Searches = new();
        internal bool DeferCapture, FailCapture;
        internal bool RecordedSemanticFixture;
        public override int EstimateTokens(string text)
        {
            if(!RecordedSemanticFixture)return base.EstimateTokens(text);
            // The archived incident exercises semantic/field rejection, not the
            // adversarial one-character-per-token budget used by Budget(). Model
            // the runtime's ASCII word grouping while counting non-ASCII fully.
            int count=0,word=0;
            foreach(char ch in text??"")
            {
                if(ch<=127&&char.IsLetterOrDigit(ch)){word++;continue;}
                count+=(word+3)/4;word=0;if(!char.IsWhiteSpace(ch))count++;
            }
            return count+(word+3)/4;
        }
        public new string CaptureEventSources(IReadOnlyCollection<string> ids, IReadOnlyCollection<string> excluded, int searchPass = 0, string topic = "")
        {
            Searches.Add((searchPass, ids.ToArray()));
            if (FailCapture) throw new InvalidOperationException("controlled capture failure");
            if (DeferCapture) return null!;
            return base.CaptureEventSources(ids, excluded, searchPass, topic);
        }
    }
    private static (Host h, WorldDiplomacyOrchestration o, WorldDiplomacyJob j) Start()
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralRange = (0,3000), CurrentDayValue = 40 };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        o.TryScheduleNormalRound();
        return (h,o,o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept"));
    }
    private static void Complete(WorldDiplomacyOrchestration o, WorldDiplomacyJob j, JObject result)
    { CountryAdmissionReplay.BeforeLegacyConcept(o, j, result);
        o.PrepareSharedRequest(j);
        Test.True(o.TryCompleteDiplomacyPipeline(j, new() { Success = true, Content = result.ToString() }), "material owner completes " + j.Kind);
    }
    private static JObject Candidate(string id = "domestic") => new() {
        ["id"]=id, ["kingdoms"]=new JArray("b"), ["material_keys"]=new JArray("lore:succession"),
        ["conflict"]="A lord disputes his ruler's recognition of an old oath", ["motive"]="A proposed renewal makes it relevant",
        ["new_development"]="The lord proposes a new hearing", ["theme"]="history", ["theme_reason"]="Conflicting historical accounts motivate the current demand for recognition", ["npc_creation"]="A grounded NPC petition, not a past fact",
        ["negotiation_space"]="The other court may clarify its old promise without joining the dispute" };
    private static JObject Compare() => new() {
        ["assessments"]=new JArray(new JObject { ["id"]="domestic", ["qualified"]=true, ["duplicate_of"]="",
            ["strength"]="Personal conflict and an answerable request",["weakness"]="The claim remains disputed",
            ["novelty_reason"]="New petition",["urgency_reason"]="No deadline" }),
        ["best_equivalent"]=new JArray("domestic"),["urgent"]=new JArray(),["comparison_reason"]="Grounded negotiation potential" };
    private static JObject Position(string id, string target) => new() {
        ["kingdom"]=id,["agenda"]="Clarify the oath",["gain"]="Recognition",["cost"]="A hearing",
        ["red_line"]="No false confession",["alternative"]="Leave the claim unresolved",
        ["source_keys"]=new JArray("lore:succession"),["address_to"]=new JArray(target) };
    internal static void Run()
    {
        PlayerMaterialOptimizationReplay.Run();
        ThemeExploration();
        AuditGapRegressions();
        PlanningAuditBudget();
        PlanningPerformanceRepairs(); RealmVoiceAndProgressBudget(); AsyncEmbeddingReplay.Run(); PostDeploymentStalls(); AdmissionHoldRecovery();
        SingleSeedToPublication(); Continuation(); Repairs(); Budget(); RecordedFailures();
        ConceptCorrection(); RecordedConceptFailures();
        Console.WriteLine("PASS material supply: production selection/expansion/audit/publication, paced wider research, feedback and input budget; provider semantics NOT_RUN");
    }
    private static void SingleSeedToPublication()
    {
        foreach (bool limited in new[] {false,true})
        {
            var(h,o,j)=Start();
            Complete(o,j,new(){["candidates"]=new JArray(Candidate())});
            Test.True(j.EventSelectionStage=="compare" && j.SuspendedReason=="","single-country material is not rejected");
            Complete(o,j,Compare());
            if(limited)o.ResolveRound(j.RoundId).PlayerResponses.Add(new(){KingdomId="a",SourceDocumentId="fixture_existing_question"});
            var parties=new JArray(Position("b","a"));
            if(!limited)parties.Add(Position("a","b"));
            var concept=new JObject {
                ["title"]="The proposed hearing",["background"]="A domestic petition raises the other court's responsibility for an old oath.",
                ["initiator"]="b",["material_keys"]=new JArray("lore:succession"),
                ["participants"]=parties,["narrative"]=new JArray(new JObject {
                    ["kind"]="allegation",["subject"]="b",["source"]="lore:succession",["text"]="The old oath is disputed." }),
                ["party_review"]=new JArray(j.CandidateKingdomIds.Select(id=>new JObject {
                    ["kingdom"]=id,["included"]=id=="b"||(!limited&&id=="a"),
                    ["reply_only"]=limited&&id=="a",["reason"]=id=="a"?"The other court may clarify its undertaking":"Source-based standing" })) };
            Complete(o,j,concept); o.PrepareSharedRequest(j);
            if (!limited)
            {
                var missingStory=MaterialCountryThirdReplay.Approved(j); missingStory.Remove("story_review");
                Complete(o,j,missingStory);
                Test.True(j.Kind=="event_concept_audit" && o.CurrentStorage.EventAnnouncements.Count==0
                    && j.MaterialRepairFeedback.Contains("story_review"), "actual concept adoption requires independent story evidence, not merely a valid theme label");
                var missingReview=MaterialCountryThirdReplay.Approved(j);missingReview.Remove("theme_review");
                string draft=j.PrivacyDraft;
                Complete(o,j,missingReview);
                Test.True(j.Kind=="event_concept_audit" && j.PrivacyDraft==draft && o.CurrentStorage.EventAnnouncements.Count==0,
                    "missing theme audit repeats review of same draft without publishing or changing direction");
            }
            Complete(o,j,MaterialCountryThirdReplay.Approved(j));
            var r=o.ResolveRound(j.RoundId);
            Test.True(r.EventPhase=="opening" && r.Participants.Count(x=>x.State=="active")==(limited?1:2),"frozen direct parties plus existing reply-only obligation: "+j.SuspendedReason);
            var boundary=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_boundary");
            Complete(o,boundary,new(){
                ["title"]="The hearing opens",["body"]="The courts acknowledge a disputed undertaking.",
                ["effects"]=new JArray(new JObject{["kind"]="ruler_relation",["kingdom"]="b",["other"]="a",["delta"]=-1,["days"]=0,["reason"]="The dispute strains relations."}),
                ["remove_effects"]=new JArray(),["confirmed_documents"]=new JArray(),["narrative"]=new JArray() });
            o.PrepareSharedRequest(boundary); Complete(o,boundary,MaterialCountryThirdReplay.Approved(boundary));
            Test.True(o.CurrentStorage.EventAnnouncements.Any(x=>x.EventId==r.RoundId&&x.Stage=="started"),"domestic seed actually reaches published opening");
            Test.True(o.CurrentStorage.EventAnnouncements.Single(x=>x.EventId==r.RoundId&&x.Stage=="started").Theme=="history",
                "formal receipt counts reviewed story theme, even when exploration sampled another direction");
            Test.True(h.Writes>0,"approved opening uses actual effect owner");
            if(limited)Test.True(r.Participants.Single(x=>x.KingdomId=="a").State=="observer","limited answer does not become substantive membership");
        }
    }
    private static void Continuation()
    {
        var(h,o,j)=Start();
        for(int pass=0;pass<2;pass++)
        {
            Complete(o,j,new(){["candidates"]=new JArray(),["reason"]="No current answerable dispute in this batch"});
            Test.True(j.MaterialSearchPass==pass+1&&o.ResolveRound(j.RoundId).State=="active","finite investigation retains its owner");
            Test.True(o.CanDispatchDiplomacyJob(j),"next bounded batch does not wait a whole campaign day");
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            j=o.CurrentStorage.Jobs.Single(x=>x.JobId==j.JobId);
            Test.True(j.MaterialSearchPass==pass+1&&o.CanDispatchDiplomacyJob(j),"reload preserves consumed search budget");
            o.PrepareSharedRequest(j);
            var req=JObject.Parse(j.UserPrompt);
            Test.True((bool)req["grounded_creation"]!&&(int)req["search_pass"]!>0,"actual request activates grounded NPC development");
        }
        Test.True(h.Searches.Select(x=>x.Pass).Distinct().Count()==3,"three distinct bounded evidence batches");
        Complete(o,j,new(){["candidates"]=new JArray(),["reason"]="All grounded directions exhausted"});
        Test.True(o.CurrentStorage.CompletedRounds.Any(x=>x.RoundId==j.RoundId&&x.CloseReason=="unpublished_material_search_exhausted")
            && !o.CurrentStorage.Jobs.Any(x=>x.RoundId==j.RoundId),"empty research releases its slot after three batches");
        o.TryScheduleNormalRound();
        Test.True(!o.CurrentStorage.Jobs.Any(x=>x.Kind=="event_concept"),"paused-time exhaustion cannot create a new paid loop");
        Test.True(o.CurrentStorage.EventAnnouncements.Count==0&&o.CurrentStorage.CountryParticipation.Count==0,"empty research does not pretend to publish or grant participation");
        var(_,retry,rj)=Start();
        Complete(retry,rj,new(){["candidates"]=new JArray(Candidate())});Complete(retry,rj,Compare());
        Complete(retry,rj,new(){["no_negotiation"]=true,["reason"]="No grounded external respondent after expansion"});
        Test.True(rj.EventSelectionStage=="candidates"&&rj.MaterialSearchPass==1,"unworkable expansion changes material without forcing a second country");
    }
    private static void Repairs()
    {
        var(_,o,j)=Start();var bad=Candidate("bad");bad["material_keys"]=new JArray("not-supplied");
        Complete(o,j,new(){["candidates"]=new JArray(bad,Candidate())});
        Test.True(j.EventSelectionStage=="compare"&&JArray.Parse(j.EventCandidatePool).Count==1,"invalid sibling cannot discard a valid domestic seed");
        var(h,o2,j2)=Start();bad=Candidate();bad.Remove("motive");
        Complete(o2,j2,new(){["candidates"]=new JArray(bad)});
        o2.PrepareSharedRequest(j2);
        Test.True(j2.MaterialRepairFeedback.Contains("motive")&&JObject.Parse(j2.UserPrompt)["repair_feedback"]!.ToString().Contains("motive"),"concrete failing field reaches actual correction request");
        // Three local contract failures leave no published state and start a new
        // evidence batch within the finite search budget, not the same invalid request forever.
        for(int i=0;i<2;i++)o2.CommitFailedJob(j2,"event_candidate_evidence_invalid:motive_missing");
        Test.True(j2.SuspendedReason==""&&j2.EventMaterialSnapshot==""&&j2.MaterialNextSearchDay==0&&j2.MaterialSearchPass==1,"exhausted local repair broadens immediately within its finite budget");
        Test.True(o2.CanDispatchDiplomacyJob(j2),"bounded technical replacement is immediately dispatchable");
        var(_,guard,gj)=Start();var invalid=Candidate();invalid["kingdoms"]=new JArray("invented_realm");
        Complete(guard,gj,new(){["candidates"]=new JArray(invalid)});
        Test.True(gj.SuspendedReason.Contains("kingdoms_must")&&gj.EventSelectionStage=="candidates","single-country allowance does not permit unknown identity");
    }
    private static void Budget()
    {
        var(h,o,j)=Start();
        h.MaterialOverride=new JArray(Enumerable.Range(0,32).Select(i=>new JObject{
            ["key"]="lore:"+i,["known_by"]="public",["text"]=new string('x',1500)+" not an executed promise."})).ToString();
        o.PrepareSharedRequest(j);
        Test.True(JArray.Parse(j.EventMaterialSnapshot).Count<32,"source capture budgets serialized complete records");
        Test.True(JObject.Parse(j.UserPrompt)["world"]==null,"world snapshot is not duplicated");
        var messages=WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(j,_=>"");
        bool failed=false;
        Test.True(WorldDiplomacyLlmDispatchApplication.EnsureRequestFitsInputBudget(j,messages,32000,10000,h.EstimateTokens,_=>"",_=>false,(_,_)=>failed=true,()=>{},_=>{})&&!failed,
            "actual dispatch gate admits bounded request");
        var sources=JArray.Parse(j.EventMaterialSnapshot);
        Test.True(sources.OfType<JObject>().Where(x=>(string)x["key"]!="world:current").All(x=>((string)x["text"]!).EndsWith("not an executed promise.")),"whole source negations preserved");
    }
    private static void RecordedFailures()
    {
        // Optional local incident evidence; core regressions above are portable.
        string path=Path.Combine("artifacts","diplomacy-supply-repair-20261021","incident-requests.json");
        if(!File.Exists(path))return;
        foreach(var item in JArray.Parse(File.ReadAllText(path)).OfType<JObject>())
        {
            var(h,o,j)=Start();
            var request=(JObject)item["request"]!;var result=(JObject)item["result"]!;
            j.CandidateKingdomIds=((JArray)request["candidates"]!).Values<string>().ToList()!;
            j.EventMaterialSnapshot=request["sources"]!.ToString(Formatting.None);
            j.EventSelectionStage=request["candidate_pool"] is JArray?"compare":"candidates";
            if(request["candidate_pool"] is JArray pool)j.EventCandidatePool=pool.ToString();
            j.UserPrompt=request.ToString();j.SystemPrompt=DiplomacyPromptTexts.Get(j.EventSelectionStage=="compare"?"EventCandidateCompare":"EventCandidateWriter","");
            Test.True(o.TryCompleteDiplomacyPipeline(j,new(){Success=true,Content=result.ToString()}),"recorded model response consumed");
            Test.True(j.SuspendedReason==""&&j.EventSelectionStage is "compare" or "expand","recorded single-country outputs pass appropriate stage");
            o.PrepareSharedRequest(j);
            bool failed=false;
            var messages=WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(j,_=>"");
            Test.True(WorldDiplomacyLlmDispatchApplication.EnsureRequestFitsInputBudget(j,messages,32000,10000,h.EstimateTokens,_=>"",_=>false,(_,_)=>failed=true,()=>{},_=>{})&&!failed,
                "recorded large world request fits next real stage after eliminating duplicate and irrelevant evidence; stage="+j.EventSelectionStage
                    +" input="+(h.EstimateTokens(j.SystemPrompt)+h.EstimateTokens(j.UserPrompt)));
        }
    }

    private static void Resume(WorldDiplomacyOrchestration o, WorldDiplomacyJob j)
    {
        j.RetryAfterUtcTicks=0; j.NextRecoveryProbeUtcTicks=0;
        typeof(WorldDiplomacyOrchestration).GetMethod("TryResumeTechnicalRequest",BindingFlags.Instance|BindingFlags.NonPublic)!
            .Invoke(o,new object[]{j,false});
    }

    private static void ConceptCorrection()
    {
        var(h,o,j)=Start();
        Complete(o,j,new(){["candidates"]=new JArray(Candidate())});
        h.MaterialOverride=new JArray(new JObject { ["key"]="lore:external",["known_by"]="a",
            ["text"]="The other court has an old recognition undertaking, not an executed transfer." },new JObject {
            ["key"]="persona:b",["known_by"]="b",["text"]="The court disputes recognition of the oath." }).ToString();
        Complete(o,j,Compare());
        CountryAdmissionReplay.FreezeFixture(o,j,new[]{"a","b"});
        Test.True(JArray.Parse(j.EventMaterialSnapshot).Any(x=>(string)x["key"]=="lore:external"),"selection supplements external evidence once");
        Test.True(JObject.Parse(j.UserPrompt)["sources"]!.Any(x=>(string)x["key"]=="lore:envoy")
            && JObject.Parse(j.UserPrompt)["sources"]!.Any(x=>(string)x["key"]=="lore:external"),
            "expansion retains existing other-country dossiers and new external evidence");
        int reads=h.MaterialReads;
        var concept=new JObject {
            ["title"]="A grounded external hearing",["background"]="A domestic petition seeks clarification of another court's undertaking.",
            ["initiator"]="b",["material_keys"]=new JArray("lore:succession","lore:external"),
            ["participants"]=new JArray(Position("b","a"),Position("a","b")),
            ["party_review"]=new JArray(j.CandidateKingdomIds.Select(id=>new JObject {
                ["kingdom"]=id,["included"]=id=="b"||id=="a",["reply_only"]=false,["reason"]="Actual standing reviewed" })),
            ["narrative"]=new JArray(new JObject { ["kind"]="allegation",["subject"]="b",["source"]="lore:succession",["text"]="The oath is disputed." }) };
        var bad=(JObject)concept.DeepClone(); bad["participants"][0]["address_to"]=new JArray();
        Complete(o,j,bad);
        Test.True(j.Kind=="event_concept"&&j.SuspendedReason.Contains("address_to"),"empty recipient is rejected before paid semantic audit");
        Test.True(o.ResolveRound(j.RoundId).EventPhase=="forming"&&o.CurrentStorage.EventAnnouncements.Count==0,"preflight has no publication effects");
        Resume(o,j);o.PrepareSharedRequest(j);
        var retry=JObject.Parse(j.UserPrompt);
        Test.True(retry["repair_feedback"]!.ToString().Contains("address_to")&&JToken.DeepEquals(JObject.Parse(retry["rejected_draft"]!.ToString())["participants"],bad["participants"]),
            "real recovery retains failed draft and exact field diagnostic in next request");
        bad=(JObject)concept.DeepClone();((JArray)bad["participants"]!).RemoveAt(1);
        Complete(o,j,bad);
        Test.True(j.SuspendedReason.Contains("admission_participants"),"missing frozen country receives concrete diagnostic before audit");
        Resume(o,j);Complete(o,j,concept);
        Test.True(j.Kind=="event_concept_audit","corrected full structure reaches semantic audit");
        Test.True(h.MaterialReads==reads,"writer retries and audit reuse frozen supplemental evidence");
        o.PrepareSharedRequest(j); Complete(o,j,MaterialCountryThirdReplay.Approved(j));
        var boundary=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_boundary");
        Complete(o,boundary,new(){["title"]="The external hearing opens",["body"]="The court requests clarification.",
            ["effects"]=new JArray(new JObject{["kind"]="ruler_relation",["kingdom"]="b",["other"]="a",
                ["delta"]=-1,["days"]=0,["reason"]="The disputed oath strains relations."}),
            ["remove_effects"]=new JArray(),["confirmed_documents"]=new JArray(),["narrative"]=new JArray()});
        o.PrepareSharedRequest(boundary); Complete(o,boundary,MaterialCountryThirdReplay.Approved(boundary));
        Test.True(o.CurrentStorage.EventAnnouncements.Any(x=>x.EventId==j.RoundId),"corrected draft proceeds through audit to actual published opening");

        var(_,ao,aj)=Start();
        Complete(ao,aj,new(){["candidates"]=new JArray(Candidate())});Complete(ao,aj,Compare());
        concept["material_keys"]=new JArray("lore:succession");
        Complete(ao,aj,concept);
        const string detail="source contradiction: the oath is conditional; preserve the condition and identify who may disclose it";
        Complete(ao,aj,new(){["approved"]=false,["rejection_category"]="fact",["repair_feedback"]=detail});
        Test.True(aj.MaterialRepairFeedback.Contains(detail),"semantic rejection retains reviewer explanation");
        Resume(ao,aj);ao.PrepareSharedRequest(aj);
        Test.True(JObject.Parse(aj.UserPrompt)["repair_feedback"]!.ToString().Contains(detail)
            && JObject.Parse(aj.UserPrompt)["rejected_draft"] is JObject,"semantic correction and exact rejected draft reach writer");
        string writer=DiplomacyPromptTexts.Get("EventConceptWriter","");
        Test.True(!writer.Contains("至少两个参与国")&&!writer.Contains("At least two participants"),"package writer has no obsolete minimum-country instruction");
        int shapeStart=writer.IndexOf("{\"title\"",StringComparison.Ordinal);
        Test.True(shapeStart>=0&&!writer.Substring(shapeStart,Math.Min(1500,writer.Length-shapeStart)).Contains("\"party_review\""),
            "main writer JSON example does not delegate country admission to model");
    }

    private static void RecordedConceptFailures()
    {
        string path=Path.Combine("artifacts","diplomacy-concept-repair-20261021","incident-concepts.json");
        if(!File.Exists(path))return;
        int writers=0,rejections=0;
        foreach(var item in JArray.Parse(File.ReadAllText(path)).OfType<JObject>())
        {
            var(h,o,j)=Start();h.RecordedSemanticFixture=true;
            var req=(JObject)item["request"]!;var result=(JObject)item["result"]!;
            j.CandidateKingdomIds=((JArray)req["candidates"]!).Values<string>().ToList()!;
            j.EventMaterialSnapshot=req["sources"]!.ToString(Formatting.None);
            j.EventSelectedCandidate=req["selected_candidate"]!.ToString();
            j.EventSelectionStage="expand";
            if(result["participants"] is JArray)
            {
                // These historical malformed single-country drafts now stop at
                // admission before the writer. Exercise their original field
                // validator directly, without inventing another country or a
                // player obligation just to bypass the current admission gate.
                object[] args={o.ResolveRound(j.RoundId),j.CandidateKingdomIds,j.EventMaterialSnapshot,result,"",false};
                bool valid=(bool)typeof(WorldDiplomacyOrchestration).GetMethod("CheckEventConcept",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(o,args)!;
                Test.True(!valid&&((string)args[4]).Contains("address_to"),"actual archived empty-recipient draft still fails the production field validator: "+args[4]);
                Test.True(o.CurrentStorage.EventAnnouncements.Count==0,"archived field-validation replay has no publication side effects");
                writers++;
            }
            else if((bool?)result["approved"]==false)
            {
                string feedback=(string)typeof(WorldDiplomacyOrchestration).GetMethod("EventAuditFeedback",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{result})!;
                Test.True(feedback.Contains((string)result["repair_feedback"]!),"actual archived audit explanation survives the production feedback reader");
                rejections++;
            }
        }
        Test.True(writers==6&&rejections==2,"all six actual drafts and both detailed rejections replayed");
    }
}
