using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Semantic judgements below are explicit deterministic fixtures, not a claim of
// model accuracy. Real request, audit binding, publication and replay owners run.
internal static partial class MaterialCountryThirdReplay
{
    private sealed class CompletionEffects(WorldDiplomacyOrchestration owner) : IWorldDiplomacyCompletionEffects
    {
        public void RemoveJob(string id) => owner.RemoveJob(id);
        public void Log(string message) { }
    }
    private static void CompleteWriter(WorldDiplomacyOrchestration o, WorldDiplomacyJob job, JObject draft)
    {
        var effects=new CompletionEffects(o);
        WorldDiplomacyCompletionApplication.Complete(job,draft.ToString(),true,false,false,"",o.CurrentStorage,0,1,ref effects,o);
    }
    private static object Call(WorldDiplomacyOrchestration o,string method,params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(o,args)!;
    private class Host : EventStructureReplay.Host, IDiplomacyNarrativeActorHost, IWorldDiplomacyAnalysisPort, IDiplomacyConvergenceHost
    {
        public int WarTendency() => 50;
        public int DrawEndingRoute(int exclusiveMaximum) => exclusiveMaximum - 1;
        public new IReadOnlyDictionary<string,string> CaptureNarrativeActors(IReadOnlyCollection<string> ids) => new Dictionary<string,string> {
            ["ordinary_lord"]="ordinary lord",["will"]="Will",["may"]="May",["rose"]="Rose",["rob"]="Rob",
            ["anna"]="Anna",["ann"]="Ann",["same1"]="罗伯特",["same2"]="罗伯特",["short"]="罗伯",["dynamic"]="A {NAME} $1" };
        public override IWorldDiplomacyAnalysisPort AnalysisPort() => this;
        public WorldDiplomacyStorage Storage => Owner.CurrentStorage;
        public IWorldDiplomacyDocumentExecutionPort Execution => DocumentExecution();
        public int MaxAutomaticReplyDepth => 3;
        public string KingdomName(string id) => id;
    }
    private static (Host,WorldDiplomacyOrchestration) Owner(Host supplied = null)
    {
        var h=supplied ?? new Host { PublishEnabled=true,PlayerAwareExecution=true,OralRange=(0,3000),CurrentDayValue=40 };
        var o=new WorldDiplomacyOrchestration(h,new WorldDiplomacyRuntimeState());h.Owner=o;return(h,o);
    }
    private static void Complete(WorldDiplomacyOrchestration o,WorldDiplomacyJob job,JObject result) {
        CountryAdmissionReplay.BeforeLegacyConcept(o, job, result);
        Test.True(o.TryCompleteDiplomacyPipeline(job,new(){Success=true,Content=result.ToString()}),"real pipeline handles "+job.Kind);
    }
    private static void Reload(WorldDiplomacyOrchestration o) =>
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
    private static void Know(WorldDiplomacyOrchestration o,string party,string document) =>
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge,party,document,40);
    internal static JObject Audit(WorldDiplomacyJob job,params string[][] actualActors)
    {
        var required=(JArray)JObject.Parse(job.UserPrompt)["actor_review_required"]!;
        Test.True(required.Count==actualActors.Length,"review fixture explicitly covers every actual requested narrative");
        return new(){["approved"]=true,["story_review"]=StoryReview(job),["theme_review"]=ThemeReview(job),["narrative_safety"]=SafetyReview(job),["position_reviews"]=OwnerReviews(job),["actor_review"]=new JArray(required.Select((row,i)=>new JObject {
            ["index"]=row["index"]!.DeepClone(),["digest"]=row["digest"]!.DeepClone(),["complete"]=true,["hero_ids"]=new JArray(actualActors[i]) }))};
    }
    internal static JArray OwnerReviews(WorldDiplomacyJob job) => new JArray(
        (JObject.Parse(job.UserPrompt)["position_review_required"] as JArray ?? new()).OfType<JObject>()
        .Select(x => new JObject { ["kingdom"] = x["kingdom"]!.DeepClone(), ["ruler_hero_id"] = x["ruler_hero_id"]!.DeepClone(),
            ["digest"] = x["digest"]!.DeepClone(), ["consistent"] = true,
            ["reason"] = "Controlled semantic fixture: the supplied sovereign seeks this external position." }));
    internal static JObject SafetyReview(WorldDiplomacyJob job) => new() {
        ["digest"] = JObject.Parse(job.UserPrompt)["narrative_safety_review_required"]?["digest"]?.DeepClone() ?? "",
        ["player_agency_valid"] = true, ["continuity_valid"] = true, ["geography_valid"] = true,
        ["publication_order_valid"] = true,
        ["diplomatic_state_valid"] = true,
        ["world_identity_valid"] = true,
        ["event_record_basis_valid"] = true,
        ["world_book_basis"] = job.EventNarrativeReviewVersion < 4 ? new JArray() : new JArray(
            ((JObject.Parse(job.UserPrompt)["world_book_sources"] ?? JObject.Parse(job.UserPrompt)["sources"]) as JArray ?? new()).OfType<JObject>().Where(x => (string)x["source_kind"]! == "world_book_account")
                .Take(1).Select(x => new JObject { ["source_key"] = x["key"]!.DeepClone(),
                    ["quote"] = ((string)x["text"]!).Substring(0, Math.Min(100, ((string)x["text"]!).Length)) })),
        ["reason"] = "Controlled semantic fixture: no unsupported human action; the proposed narrative preserves this event's trigger and actual recorded developments."
    };
    internal static JObject ThemeReview(WorldDiplomacyJob job) => string.IsNullOrEmpty(job.EventExplorationTheme) ? null : new() {
        ["theme"] = JObject.Parse(job.EventSelectedCandidate)["theme"]?.DeepClone(), ["valid"] = true,
        ["reason"] = "Controlled semantic fixture: the historical claim remains the substantive motive of this draft."
    };
    internal static JObject StoryReview(WorldDiplomacyJob job)
    {
        if (job.StoryReviewVersion < 1 || job.EventSelectionStage is "player" or "oral") return null!;
        var request = JObject.Parse(job.UserPrompt);
        var draft = JObject.Parse(job.PrivacyDraft);
        var seed = JObject.Parse(job.EventSelectedCandidate);
        return new() { ["digest"] = request["story_review_required"]!["digest"]!.DeepClone(),
            ["motive_preserved"] = true, ["motive_reason"] = "Controlled semantic fixture preserves the supplied motive.",
            ["seed_motive_quote"] = seed["motive"]?.DeepClone() ?? "", ["draft_motive_quote"] = draft["background"]!.DeepClone(),
            ["comparisons"] = new JArray(((JArray)request["prior_events"]!).OfType<JObject>().Select(x => new JObject {
                ["event_id"] = x["RoundId"]!.DeepClone(), ["verdict"] = "distinct", ["prior_quote"] = x["EventBackground"]!.DeepClone(),
                ["draft_quote"] = draft["background"]!.DeepClone(), ["reason"] = "Controlled judgement; model semantic quality is not tested here." })) };
    }
    internal static JObject ContinuityReview(WorldDiplomacyOrchestration o, WorldDiplomacyJob job)
    {
        var evidence = Call(o,"ResponseContinuityEvidence",job) as JObject;
        if (evidence == null) return null!;
        // This is a provider judgement fixture, not a semantic classifier.
        string body=(string)JObject.Parse(job.PrivacyDraft)["body"]!;
        string source=(string)evidence["source_body"]!;
        return new(){["digest"]=evidence["digest"]!.DeepClone(),["source_id"]=evidence["source_id"]!.DeepClone(),
            ["consistent"]=true,["stance"]="acknowledged",["source_quote"]=source.Substring(0,Math.Min(500,source.Length)),
            ["draft_quote"]=body.Substring(0,Math.Min(500,body.Length)),["reason"]="Controlled fixture acknowledges the supplied source."};
    }
    internal static JObject Approved(WorldDiplomacyJob job) => new() { ["approved"] = true, ["story_review"] = StoryReview(job), ["theme_review"] = ThemeReview(job), ["narrative_safety"] = SafetyReview(job), ["position_reviews"] = OwnerReviews(job) };
    internal static bool ReviewedBoundary(WorldDiplomacyOrchestration o,WorldDiplomacyRound r,string stage,JObject draft,params string[][] actors)
    {
        var job=new WorldDiplomacyJob{JobId="actor-boundary-"+stage,Kind="event_boundary",RoundId=r.RoundId,AuthorKingdomId="a",ForcedIntent=stage};
        o.CurrentStorage.Jobs.Add(job);o.PrepareSharedRequest(job);Complete(o,job,draft);
        o.PrepareSharedRequest(job);Complete(o,job,Audit(job,actors));
        return o.CurrentStorage.EventAnnouncements.Any(x=>x.EventId==r.RoundId&&x.Stage==stage&&x.State is "public" or "applied");
    }
    internal static bool ReviewedConcept(WorldDiplomacyOrchestration o,WorldDiplomacyRound r,List<string> candidates,string sources,JObject draft,params string[][] actors)
    {
        var job=new WorldDiplomacyJob{JobId="actor-concept",Kind="event_concept",RoundId=r.RoundId,AuthorKingdomId="a",
            EventSelectionStage="expand",EventMaterialSnapshot=sources,EventSelectedCandidate="{}",EventComparison="{}",CandidateKingdomIds=candidates};
        o.CurrentStorage.Jobs.Add(job);o.PrepareSharedRequest(job);Complete(o,job,draft);
        if (job.Kind != "event_concept_audit")
        {
            Test.True(job.SuspendedReason.StartsWith("narrative_actor_identity_invalid", StringComparison.Ordinal),
                "unknown actor identity is now rejected by shared structural preflight, not a simulated semantic reviewer");
            Test.True(r.EventBackground!=(string)draft["background"]!,"early identity rejection cannot adopt the draft");
            return false;
        }
        o.PrepareSharedRequest(job);Complete(o,job,Audit(job,actors));
        return !o.CurrentStorage.Jobs.Contains(job) && r.EventBackground==(string)draft["background"]!;
    }
    private static JObject Position(string id) => new(){["kingdom"]=id,["agenda"]=id=="p"?"":"Recognition",["gain"]=id=="p"?"":"Public hearing",
        ["cost"]=id=="p"?"":"Joint review",["red_line"]=id=="p"?"":"No confession",["alternative"]=id=="p"?"":"Unresolved"};
    private static WorldDiplomacyDocument Publish(WorldDiplomacyOrchestration o,WorldDiplomacyJob job,string body,bool position,params string[] answers)
    {
        int publishedBefore=o.CurrentStorage.Documents.Count(x=>x.AuthorKingdomId==job.AuthorKingdomId&&x.IsReadyForPublication);
        o.PrepareSharedRequest(job);
        if(job.Kind=="privacy_material")Complete(o,job,new(){["safe"]=true,["public_context"]="The invitation and the current dispute are public. No treaty, war or transfer has been agreed."});
        o.PrepareSharedRequest(job);
        var draft=new JObject{["title"]="Our position",["body"]=body,
            ["actions"]=new JArray(new JObject{["target_kingdom_id"]=job.TargetKingdomId,["intent"]="statement",["commitment"]="none",["negotiation_move"]="state_position"}),
            ["requires_response"]=false,["round_participation"]="continue",["round_status"]="continue",["response_requests"]=new JArray(),
            ["answered_player_document_ids"]=new JArray(string.IsNullOrEmpty(job.SourceDocumentId)?Array.Empty<string>():new[]{job.SourceDocumentId})};
        o.CommitGeneratedDocument(job,draft.ToString());
        Complete(o,job,new(){["safe"]=true,["position_owner_valid"]=true,["negotiation_consistent"]=true,["response_continuity"]=ContinuityReview(o,job),["agenda_quote"]=position?body:"",
            ["agenda_answers"]=new JArray(answers.Select(x=>new JObject{["source"]=x,["quote"]=body}))});
        Test.True(o.CurrentStorage.Documents.Count(x=>x.AuthorKingdomId==job.AuthorKingdomId&&x.IsReadyForPublication)==publishedBefore+1,
            "new formal document actually published: "+JsonConvert.SerializeObject(new{job.Kind,job.SuspendedReason,job.PrivacyRewrites,job.SourceDocumentId,job.PlayerResponseSourceIds,job.PrivacyDraft}));
        return o.CurrentStorage.Documents.Last(x=>x.AuthorKingdomId==job.AuthorKingdomId&&x.IsReadyForPublication);
    }
    internal static void Run()
    {
        AuditLatency();
        DeclarationAuditStalls();
        PlayerNarrativeFollowup();
        PlayerOpening();
        ProgressionRepair();
        PipelineRecovery();
        OptionalTermsReplay();
        PublicationEvidenceReplay();
        RecipientSchedulingPriority();
        ContractCompletion();
        DeclarationAuditRepair(); InvitationLifecycle(); SemanticAudit(); ProgressAudit();
        Console.WriteLine("Repair3 "+(SmokeLocalizationEnvironment.EnglishPackage?"EN":"CN")+": invitation acceptance -> core expression -> response -> closure; immutable receipts; source-bound actor audit PASS. Real provider/game NOT_RUN.");
    }

    private static void ContractCompletion()
    {
        {
            var(_,o)=Owner();var r=o.EnsureActiveRound("a","b",false);Foundation(r);
            var j=new WorldDiplomacyJob{JobId="actor-field-type",Kind="event_boundary",RoundId=r.RoundId,AuthorKingdomId="a",ForcedIntent="started"};
            var draft=Draft("Will testified.","will");draft["narrative"]![0]!["hero_ids"]=new JArray(new JObject{["invalid"]="will"});
            o.CurrentStorage.Jobs.Add(j);o.PrepareSharedRequest(j);Complete(o,j,draft);o.PrepareSharedRequest(j);Complete(o,j,Audit(j,new[]{"will"}));
            Test.True(j.Kind=="event_boundary"&&j.PrivacySourceMaterial.Contains("narrative_actor_audit_mismatch")
                &&!o.CurrentStorage.EventAnnouncements.Any(),"non-string actor identities produce repair feedback without throwing or publishing");
        }
        // Recorded incident: a bare successful review used to discard the
        // opening, while a missing lore prefix kept retrying without a field.
        {
            var(h,o)=Owner();var r=o.EnsureActiveRound("a","b",false);Foundation(r);
            var j=new WorldDiplomacyJob{JobId="boundary-format",Kind="event_boundary",RoundId=r.RoundId,AuthorKingdomId="a",ForcedIntent="started"};
            o.CurrentStorage.Jobs.Add(j);o.PrepareSharedRequest(j);Complete(o,j,Draft("Will testified.","will"));
            string original=j.PrivacyDraft;
            for(int i=0;i<3;i++)o.TryCompleteDiplomacyPipeline(j,new(){Success=true,Content="approved:true\nactor_review:[]"});
            Test.True(j.Kind=="event_boundary_audit"&&j.PrivacyDraft==original&&j.PrivacyAuditRetries==3
                &&j.SuspendedReason.StartsWith("boundary_audit_invalid:")&&h.Writes==0,
                "recorded malformed boundary approval preserves draft with finite audit-only retries");
            Reload(o);j=o.CurrentStorage.Jobs.Single();r=o.ResolveRound(r.RoundId);
            Call(o,"TryResumeTechnicalRequest",j,true);o.PrepareSharedRequest(j);
            o.TryCompleteDiplomacyPipeline(j,new(){Success=true,IsOutputTruncated=true,Content="{\"approved\":"});
            Test.True(j.Kind=="event_boundary_audit"&&j.PrivacyDraft==original&&j.PrivacyAuditRetries==1,"truncated boundary audit also preserves the opening");
            o.PrepareSharedRequest(j);
            Test.True(j.Kind=="event_boundary_audit"&&j.PrivacyDraft==original&&j.SystemPrompt.Contains(DiplomacyPromptTexts.Get("EventBoundaryAuditOutput","")),
                "restored held audit resumes the same draft with actual package JSON contract");
            Complete(o,j,Audit(j,new[]{"will"}));
            Test.True(o.CurrentStorage.EventAnnouncements.Single().State=="public"&&h.Writes==1,
                "valid repaired boundary audit publishes effects once");
        }
        {
            var(_,o)=Owner();var r=o.EnsureActiveRound("a","b",false);
            JObject Party(string id,string target){var p=Position(id);p["source_keys"]=new JArray("f3b0ba5da09c104b");p["address_to"]=new JArray(target);return p;}
            var d=new JObject{["title"]="A sourced dispute",["background"]="A current public dispute",["initiator"]="a",
                ["material_keys"]=new JArray("lore:f3b0ba5da09c104b"),["participants"]=new JArray(Party("a","b"),Party("b","a")),
                ["party_review"]=new JArray(new[]{"a","b"}.Select(x=>new JObject{["kingdom"]=x,["included"]=true,["reason"]="Direct dispute"})),
                ["narrative"]=new JArray(new JObject{["kind"]="public_fact",["subject"]="a",["source"]="lore:f3b0ba5da09c104b",["text"]="The realms dispute a public claim."})};
            var j=new WorldDiplomacyJob{JobId="source-feedback",Kind="event_concept",RoundId=r.RoundId,AuthorKingdomId="a",EventSelectionStage="expand",
                EventSelectedCandidate="{}",EventComparison="{}",CandidateKingdomIds=new(){"a","b"},
                EventMaterialSnapshot="[{\"key\":\"lore:f3b0ba5da09c104b\",\"known_by\":\"public\",\"text\":\"A sourced claim.\",\"hero_ids\":[]}]"};
            o.CurrentStorage.Jobs.Add(j);Complete(o,j,d);
            Test.True(j.SuspendedReason.Contains("\"kingdom\":\"a\"")&&j.SuspendedReason.Contains("\"invalid_key\":\"f3b0ba5da09c104b\"")
                &&j.SuspendedReason.Contains("lore:f3b0ba5da09c104b")&&r.EventBackground=="",
                "source mismatch names the country, exact erroneous field and supplied full key; no guessed alias is adopted");
            foreach(var party in d["participants"]!)party["source_keys"]=new JArray("lore:f3b0ba5da09c104b");
            j.SuspendedReason="";Complete(o,j,d);o.PrepareSharedRequest(j);
            Test.True(j.Kind=="event_concept_audit"&&j.UserPrompt.Contains("sovereign_speakers")
                &&j.SystemPrompt.Contains(DiplomacyPromptTexts.Get("SovereignPositionContract","")),"corrected source reaches independent sovereign-ownership audit");
            Complete(o,j,new(){["approved"]=false,["rejection_category"]="position",["reason"]="a.agenda belongs to the dissident lord, not ruler_a"});
            Test.True(r.EventBackground==""&&j.SuspendedReason.Contains("dissident lord"),"ownership rejection cannot adopt a misassigned national position");
        }
        foreach(string decision in new[]{"accept","accept-observer","accept-withdraw","reject","missing","wrong-ruler","wrong-country","object-country","missing-quote","missing-quote-no-address"})
        {
            var(h,o)=Owner();var r=o.EnsureActiveRound("a","b",false);Foundation(r);
            r.InitiatorKingdomId="a";r.EventPhase="opening";
            foreach(var p in r.Participants){p.State="active";p.AgendaAddressTo=new(){p.KingdomId=="a"?"b":"a"};}
            string misassigned="阿尔凯娅与埃莉德要求国王卡拉多格削减税赋并交出地方行政权。";
            r.Participants.Single(x=>x.KingdomId=="a").Agenda=misassigned;
            Test.True(ReviewedBoundary(o,r,"started",Draft("Dissident lords demanded a hearing; no ruler agreed to surrender power.","ordinary_lord"),new[]{"ordinary_lord"}),"opening publishes the domestic conflict without making it sovereign consent");
            var j=o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate");o.PrepareSharedRequest(j);
            Complete(o,j,new(){["safe"]=true,["public_context"]="A stern attitude."});o.PrepareSharedRequest(j);
            Test.True(j.UserPrompt.Contains("Dissident lords demanded a hearing; no ruler agreed to surrender power.")
                &&j.SystemPrompt.Contains(DiplomacyPromptTexts.Get("SovereignPositionCorrection","")),
                "authoritative published opening survives lossy isolation and actual writer receives correction contract");
            if(decision=="accept-observer")
            {
                r.Participants.Single(x=>x.KingdomId=="b").State="observer";
                r.Participants.Single(x=>x.KingdomId=="a").AgendaExpressionDocumentId="prior-public-expression";
            }
            if(decision=="missing-quote-no-address")r.Participants.Single(x=>x.KingdomId=="a").AgendaAddressTo.Clear();
            string body="We reject foreign interference; we have not agreed to surrender power. We request that b stop funding the dissidents.";
            var corrected=Position(decision=="wrong-country"?"b":"a");corrected["agenda"]="Stop foreign interference and funding of dissidents.";
            if(decision=="object-country")corrected["kingdom"]=new JObject();
            corrected["ruler_hero_id"]=decision=="wrong-ruler"?"wrong":h.PartyRulerId("a");
            corrected["reason"]="The old agenda described the dissident lords' demand against their ruler, not the ruler's external position.";
            var d=new JObject{["title"]="Reply on interference",["body"]=body,["position_correction"]=corrected,
                ["actions"]=new JArray(new JObject{["target_kingdom_id"]="b",["intent"]="statement",["commitment"]="none",["negotiation_move"]="state_position"}),
                ["requires_response"]=false,["round_participation"]="continue",["round_status"]="continue",["response_requests"]=new JArray()};
            if(decision=="accept-withdraw"){d["round_participation"]="withdraw";d["actions"]![0]!["negotiation_move"]="end_negotiation";}
            CompleteWriter(o,j,d);o.PrepareSharedRequest(j);
            Test.True(j.SystemPrompt.Contains(DiplomacyPromptTexts.Get("SovereignPositionCorrectionAudit",""))&&j.UserPrompt.Contains("proposed_position_correction"),
                "independent audit receives original record, proposed correction and current sovereign identity");
            var audit=new JObject{["safe"]=true,["position_owner_valid"]=true,["negotiation_consistent"]=true,["response_continuity"]=ContinuityReview(o,j),["agenda_quote"]=decision.StartsWith("missing-quote")?"":body,["agenda_answers"]=new JArray()};
            if(decision!="missing")audit["position_correction_valid"]=decision!="reject";
            Complete(o,j,audit);
            bool accepted=decision.StartsWith("accept");
            Test.True(o.CurrentStorage.Documents.Any(x=>x.IsReadyForPublication)==accepted,"only independently approved, correctly bound, expressed correction publishes: "+decision);
            var party=r.Participants.Single(x=>x.KingdomId=="a");
            Test.True(party.Agenda==(accepted?(string)corrected["agenda"]!:misassigned),"failed/absent review cannot mutate national stance: "+decision);
            if(accepted)
            {
                var published=o.CurrentStorage.Documents.Single();
                if(decision=="accept-observer")Test.True(r.PlayerResponses.Any(x=>x.SourceDocumentId==published.DocumentId&&x.KingdomId=="b"&&x.RequestKind=="case_question"),"correcting an already expressed position registers its new observer response after publication");
                if(decision=="accept-withdraw")Test.True(party.State=="withdrawn","correction preserves actual published withdrawal");
                Test.True(party.AgendaExpressionDocumentId==published.DocumentId&&r.Participants.Count==2
                    &&o.CurrentStorage.EventAnnouncements.Single().Body.Contains("Dissident lords demanded"),"correction attaches to an actual declaration without rewriting history or participants");
                Reload(o);r=o.ResolveRound(r.RoundId);Test.True(r.Participants.Single(x=>x.KingdomId=="a").Agenda==(string)corrected["agenda"]!,"corrected stance persists through the existing round owner");
            }
        }
        {
            var(_,o)=Owner();var r=o.EnsureActiveRound("a","b",false);Foundation(r);
            var delivered=new WorldDiplomacyDocument{DocumentId="exact-delivered",RoundId=r.RoundId,AuthorKingdomId="b",TargetKingdomId="a",IsReadyForPublication=true,Title="Exact terms",Body="Only if both sides agree; no surrender, no transfer, otherwise refusal."};
            o.AddDocument(delivered);
            var j=new WorldDiplomacyJob{JobId="context",RoundId=r.RoundId,AuthorKingdomId="a",TargetKingdomId="b",SourceDocumentId=delivered.DocumentId};
            o.CurrentStorage.EventAnnouncements.Add(new(){Id="private-draft",EventId=r.RoundId,Stage="started",State="staged",Body="UNPUBLISHED SECRET"});
            Test.True(!((string)Call(o,"BuildPublicExecutionContext",j)).Contains(delivered.Body),"undelivered original never bypasses knowledge isolation");
            Know(o,"a",delivered.DocumentId);string context=(string)Call(o,"BuildPublicExecutionContext",j);
            Test.True(context.Contains(delivered.Body)&&!context.Contains("UNPUBLISHED SECRET"),"delivered conditions and negation survive exactly; unpublished event drafts remain excluded");
        }
        Console.WriteLine("Contract completion: sovereign correction/publication, malformed boundary audit, source diagnostics and lossless public context PASS; semantic judgments are controlled fixtures.");
    }
    private static void RecipientSchedulingPriority()
    {
        foreach (string legacy in new[] { "none", "queued", "running", "draft", "held" })
        {
            var (h,o)=Owner();
            var r=o.EnsureActiveRound("a","b",false);Foundation(r);
            r.EventPhase="discussing";r.EventBeginningId="priority-opening";
            o.CurrentStorage.EventAnnouncements.Add(new(){Id=r.EventBeginningId,EventId=r.RoundId,Stage="started",State="public",Acknowledged=true});
            foreach(var p in r.Participants){p.State="active";p.AgendaAddressTo=new(){p.KingdomId=="a"?"b":"a"};}
            var first=new WorldDiplomacyDocument{DocumentId="priority-source",RoundId=r.RoundId,AuthorKingdomId="a",TargetKingdomId="b",
                IsReadyForPublication=true,AnalysisStatus="success",Body="We seek recognition.",RequiresResponse=false,ResponseRequestsClassified=true};
            o.AddDocument(first);
            r.Participants.Single(x=>x.KingdomId=="a").AgendaExpressionDocumentId=first.DocumentId;
            for(int i=0;i<7;i++)
                WorldDiplomacyPropagationApplication.ReceiveCourt(o.CurrentStorage,first,"c"+i,h.CurrentDayValue,()=>false,()=>o.ProcessCourtArrival("c"+i,first));
            Call(o,"ReconcileEventQueue");
            Test.True(o.CurrentStorage.Jobs.Count==0&&r.PlayerCourtWork.Count==0,"seven incidental court receipts create knowledge only, no participation inquiries or pre-delivery core reply");
            Test.True(r.Participants.Count==2&&o.CurrentStorage.KingdomKnowledge.Count==7,"propagation preserves knowledge and the preselected participant set");
            WorldDiplomacyJob obsolete=null;WorldDiplomacyRequestSnapshot lease=null;
            if(legacy!="none")
            {
                for(int i=0;i<7;i++)r.PlayerCourtWork.Add(new(){KingdomId="c"+i,SourceDocumentId=first.DocumentId,Purpose="join_known_event",Status=i==0?"queued":"pending"});
                obsolete=new(){JobId="old-observer",Kind="player_decision",RoundId=r.RoundId,AuthorKingdomId="c0",SourceDocumentId=first.DocumentId,ForcedIntent="join_known_event"};
                if(legacy=="draft")
                {
                    var observer=WorldDiplomacyStructureRules.EnsureRoundParticipant(r,"c0","observer",false);
                    observer.PendingParticipationSourceId=first.DocumentId;
                    obsolete.Kind="privacy_audit";obsolete.ForcedIntent="";
                }
                o.CurrentStorage.Jobs.Add(obsolete);
                DiplomacyEventRules.Bind(r,obsolete);
                if(legacy=="held")
                {
                    obsolete.SuspendedReason="obsolete-review";r.TechnicalHoldReason=obsolete.SuspendedReason;
                    r.TechnicalRequestHoldReason=obsolete.SuspendedReason;r.TechnicalHoldStartedHour=h.CurrentDayValue*24-2;
                }
                if(legacy=="running")Test.True(o.RequestLeases.TryClaim(obsolete,1,1000,30000,false,out lease),"legacy in-flight request owns a real lease");
                Test.True(!o.CanDispatchDiplomacyJob(obsolete),"legacy unsolicited work cannot make another provider request");
            }
            WorldDiplomacyPropagationApplication.ReceiveCourt(o.CurrentStorage,first,"b",h.CurrentDayValue,()=>false,()=>o.ProcessCourtArrival("b",first));
            Call(o,"ReconcileEventQueue");
            var reply=o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate");
            Test.True(reply.AuthorKingdomId=="b"&&reply.SourceDocumentId==first.DocumentId,"delivered core recipient queues without advancing campaign time");
            Test.True(!r.PlayerCourtWork.Any(x=>x.Status is "pending" or "queued")&&!o.CurrentStorage.Jobs.Any(x=>x.AuthorKingdomId=="c0"),"old unsolicited decisions and uncommitted drafts retire without political refusal");
            if(obsolete!=null)
            {
                Test.True(DiplomacyEventRules.Matches(r,obsolete),"late completion retains a valid owner identity");
                Test.True(!o.AdmitCompletion(obsolete,new(){Success=true,Content="{}",OwnershipEpoch=r.OwnershipEpoch,
                    SchedulingOwner=r.SchedulingOwner,PlayerAuthorityRulerId=r.PlayerAuthorityRulerId,
                    RoundConversationRevision=r.ConversationRevision}),"late obsolete completion cannot revive admission or publish a draft");
            }
            if(legacy=="held")Test.True(string.IsNullOrEmpty(r.TechnicalHoldReason)&&r.TechnicalHoldStartedHour<0,
                "retired obsolete work releases only its own technical hold");
            if(lease!=null)
            {
                Test.True(o.RequestLeases.ContainsSpeech(r.RoundId),"retiring work does not release an actual transport lease early");
                Test.True(o.RequestLeases.TryRelease(obsolete.JobId,1,lease.Attempt),"actual completion releases the obsolete request lease");
            }
            WorldDiplomacyPropagationApplication.ReceiveCourt(o.CurrentStorage,first,"b",h.CurrentDayValue,()=>false,()=>o.ProcessCourtArrival("b",first));
            Call(o,"ReconcileEventQueue");Call(o,"ScheduleEventCourtWork",r);
            Test.True(o.CurrentStorage.Jobs.Count(x=>x.Kind=="generate")==1,"duplicate delivery and wakeup do not duplicate a response");
            Test.True(o.CurrentStorage.Documents.Single()==first,"retiring obsolete work preserves published history");
        }
    }
    internal static void DeclarationAuditRepair(string incidentPath = null)
    {
        (Host h, WorldDiplomacyOrchestration o, WorldDiplomacyRound r, WorldDiplomacyJob j, JObject draft) Fixture()
        {
            var(h,o)=Owner();o.CurrentStorage.MaterialCountrySchemaVersion=1;
            var r=o.EnsureActiveRound("a","b",false);Foundation(r);
            r.InitiatorKingdomId="a";r.EventPhase="opening";
            foreach(var p in r.Participants)p.AgendaAddressTo=new(){p.KingdomId=="a"?"b":"a"};
            Test.True(ReviewedBoundary(o,r,"started",Draft("The ordinary lord requested a hearing.","ordinary_lord"),new[]{"ordinary_lord"}),
                "real opening publication starts discussion");
            var j=o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate");
            o.PrepareSharedRequest(j);
            Complete(o,j,new(){["safe"]=true,["public_context"]="A public hearing over a disputed claim."});
            o.PrepareSharedRequest(j);
            var schema=JObject.Parse(DiplomacyPromptTexts.Get("AF_DIP_Contract_026",""));
            Test.True(schema["response_requests"] is JArray && schema["requires_response"]?.Type==JTokenType.Boolean
                && schema["round_participation"]!=null && schema["round_status"]!=null,"primary schema contains full lifecycle and response classification");
            var draft=new JObject{["title"]="Request for recognition",["body"]="We seek recognition through a joint review. Please answer our claim.",
                ["actions"]=new JArray(new JObject{["target_kingdom_id"]="b",["intent"]="statement",["commitment"]="none",["negotiation_move"]="state_position"}),
                ["requires_response"]=true,["round_participation"]="continue",["round_status"]="continue",
                ["response_requests"]=new JArray(new JObject{["kingdom"]="b",["kind"]="case_question",["quote"]="Please answer our claim."})};
            CompleteWriter(o,j,draft);
            return(h,o,r,j,draft);
        }
        foreach(var invalid in new[]{
            new JObject{["safe"]=true},
            new JObject{["safe"]=true,["position_owner_valid"]=true,["negotiation_consistent"]=true,["agenda_quote"]="Text from event background, not the declaration.",["agenda_answers"]=new JArray()},
            new JObject{["safe"]="true"},
            new JObject{["safe"]=true,["position_owner_valid"]=true,["negotiation_consistent"]=true,["agenda_quote"]=new JObject()},
            new JObject{["safe"]=true,["position_owner_valid"]=true,["negotiation_consistent"]=true,["agenda_quote"]="We seek recognition through a joint review.",["agenda_answers"]=new JArray("invalid row")}
        })
        {
            var(h,o,r,j,draft)=Fixture();string original=j.PrivacyDraft;
            Complete(o,j,invalid);
            Test.True(j.Kind=="privacy_audit"&&j.PrivacyDraft==original&&j.PrivacyRewrites==0&&j.PrivacyAuditRetries==1
                &&o.CurrentStorage.Documents.Count==0,"malformed audit preserves the original draft and cannot publish");
            Test.True(j.UserPrompt.Contains("validation_error")&&j.SystemPrompt.Contains("public_draft.body"),
                "actual audit repair request explains exact source/field requirement");
            string id=j.JobId;Reload(o);j=o.CurrentStorage.Jobs.Single(x=>x.JobId==id);r=o.ResolveRound(r.RoundId);
            Test.True(j.PrivacyAuditRetries==1&&j.PrivacyRepairFeedback.Contains("validation_error")&&j.PrivacyDraft==original,
                "save/load preserves same draft, diagnostic and finite audit budget");
            Complete(o,j,new(){["safe"]=true,["position_owner_valid"]=true,["negotiation_consistent"]=true,["agenda_quote"]="We seek recognition through a joint review.",["agenda_answers"]=new JArray()});
            var doc=o.CurrentStorage.Documents.Single(x=>x.IsReadyForPublication);
            Test.True(doc.Body==(string)draft["body"]!&&r.Participants.Single(x=>x.KingdomId=="a").AgendaExpressionDocumentId==doc.DocumentId,
                "corrected review publishes unchanged first declaration and records its core demand");
            Test.True(!WorldDiplomacyDocumentFactRules.HasKingdomKnowledge(o.CurrentStorage.KingdomKnowledge,"b",doc.DocumentId),
                "publication does not fake delivery");
            h.CurrentDayValue+=20;o.ProcessPropagationArrivals();
            Test.True(WorldDiplomacyDocumentFactRules.HasKingdomKnowledge(o.CurrentStorage.KingdomKnowledge,"b",doc.DocumentId),"actual propagation delivers first declaration");
            Call(o,"ScheduleUnexpressedPosition",r);
            var reply=o.CurrentStorage.Jobs.First(x=>x.Kind=="generate"&&x.AuthorKingdomId=="b");
            Publish(o,reply,"We reject your claim to recognition, but offer a joint hearing without confession.",true,doc.DocumentId);
            Test.True(r.Participants.Single(x=>x.KingdomId=="a").AgendaAnswerDocumentIds.Count==1,
                "real counterpart publication answers delivered core demand");
        }
        {
            var(_,o,_,j,draft)=Fixture();
            Complete(o,j,new(){["safe"]=true,["position_owner_valid"]=true,["negotiation_consistent"]=false,["repair_feedback"]="Address the requested joint review without claiming consent."});
            Test.True(j.Kind=="generate"&&j.PrivacyRewrites==1&&j.PrivacyRepairFeedback.Contains("negotiation_consistency_rejected"),
                "substantive rejection rewrites with accurate reason");
            o.PrepareSharedRequest(j);
            Test.True(j.UserPrompt.Contains("Address the requested joint review")&&!j.UserPrompt.Contains("上一份草稿未通过保密检查"),
                "writer receives real rejection rather than fabricated confidentiality failure");
            draft.Remove("response_requests");CompleteWriter(o,j,draft);
            Test.True(j.Kind=="privacy_repair"&&j.PrivacyRepairFeedback.Contains("response_request_classification_missing")
                &&o.CurrentStorage.Documents.Count==0&&o.CurrentStorage.Jobs.Contains(j),
                "real completion keeps the same repair job queued when preflight rejects missing classification");
        }
        {
            var(_,o,r,j,_)=Fixture();
            for(int i=0;i<3;i++)Complete(o,j,new(){["safe"]=true});
            Test.True(j.SuspendedReason.StartsWith("declaration_audit_invalid:position_owner_valid_boolean_missing")
                &&j.PrivacyRewrites==0&&o.CurrentStorage.Documents.Count==0,"finite audit retries hold without political refusal or leaked publication");
            j.SuspendedReason="privacy_rewrites_exhausted";j.TechnicalRecoveryAttempts=3;j.RetryAfterUtcTicks=0;j.NextRecoveryProbeUtcTicks=0;
            Call(o,"TryResumeTechnicalRequest",j,false);
            Test.True(j.Kind=="privacy_audit"&&j.SuspendedReason==""&&j.PrivacyDraft.Length>0&&j.PrivacyAuditRetries==0,
                "legacy exhausted audit can resume once under repaired contract using the same draft");
        }
        if (incidentPath != null)
        {
            var incidents=JArray.Parse(File.ReadAllText(incidentPath));
            foreach(var item in incidents.OfType<JObject>())
            {
                var(_,o,_,j,draft)=Fixture();
                var actual=(JObject)item["draft"]!;
                draft["title"]=actual["title"]!.DeepClone();draft["body"]=actual["body"]!.DeepClone();
                // Keep exact actual prose and review; only fixture authority IDs
                // and mechanically unrelated actions are supplied by the fake world.
                if(actual["response_requests"] is not JArray)draft.Remove("response_requests");
                else draft["response_requests"]=actual["response_requests"]!.DeepClone();
                j.Kind="generate";j.SystemPrompt=j.PrivacyDeclarationSystem;
                CompleteWriter(o,j,draft);
                if(actual["response_requests"] is not JArray)
                {
                    Test.True(j.Kind=="privacy_repair"&&o.CurrentStorage.Jobs.Contains(j)&&j.PrivacyRepairFeedback.Contains("response_request_classification_missing"),
                        "actual omitted classification is repaired before audit: "+item["time"]);
                    continue;
                }
                Complete(o,j,(JObject)item["audit"]!);
                string q=(string)item["audit"]!["agenda_quote"]??"";
                bool valid=item["audit"]!["negotiation_consistent"]?.Value<bool>()==true
                    &&q.Length>0&&((string)actual["body"]!).Contains(q,StringComparison.Ordinal);
                if(valid)Test.True(o.CurrentStorage.Documents.Count==1,"actual complete review publishes: "+item["time"]);
                else
                {
                    Test.True(j.Kind=="privacy_audit"&&j.PrivacyRewrites==0&&o.CurrentStorage.Documents.Count==0,
                        "actual defective review requests a same-draft repair: "+item["time"]);
                    Complete(o,j,new(){["safe"]=true,["position_owner_valid"]=true,["negotiation_consistent"]=true,["agenda_quote"]=(string)actual["body"]!,
                        ["agenda_answers"]=new JArray()});
                    Test.True(o.CurrentStorage.Documents.Single().Body==(string)actual["body"]!,
                        "controlled valid re-review publishes the exact original prose: "+item["time"]);
                }
            }
            Console.WriteLine("Actual incident draft/review pairs replayed="+incidents.Count+"; model correction responses are controlled fixtures.");
        }
    }
    private static void InvitationLifecycle()
    {
        foreach(string sender in new[]{"p","a"})
        foreach(string decision in new[]{"accept","decline","question","conditional","reserve"})
        foreach(bool deliverFirst in new[]{false,true})
        {
            var(h,o)=Owner();var r=o.EnsureActiveRound(sender,"c",sender=="p");string roundId=r.RoundId;
            r.PlayerAuthorityKingdomId="p";r.PlayerAuthorityRulerId="ruler_p";r.EventBeginningId=roundId+":started";r.RootDocumentId="invite";
            o.CurrentStorage.EventAnnouncements.Add(new(){Id=r.EventBeginningId,EventId=roundId,Stage="started",State="public",CanonicalHistoryRecorded=true});
            r.Participants.Single(x=>x.KingdomId=="c").State="observer";
            var invite=new WorldDiplomacyDocument{DocumentId="invite",RoundId=roundId,AuthorKingdomId=sender,TargetKingdomId="c",
                IsPlayerAuthored=sender=="p",IsReadyForPublication=true,AnalysisStatus="success",Body="Please join this negotiation.",Title="Invitation",
                ResponseRequestsClassified=true,ResponseRequests=new(){new(){KingdomId="c",Kind="invitation",Quote="Please join this negotiation."}}};
            o.AddDocument(invite);Know(o,"c",invite.DocumentId);
            if(sender=="p")Call(o,"RegisterPlayerResponseWork",invite,true);
            else {Call(o,"HandleEventPublication",invite);Call(o,"SchedulePlayerResponseWork",r);}
            Test.True(o.CurrentStorage.Jobs.Any(x=>x.Kind=="player_decision"),"invitation decision "+sender+"/"+decision+": "
                +JsonConvert.SerializeObject(new{r.State,r.EventSystemVersion,r.SchedulingOwner,r.PlayerResponses,jobs=o.CurrentStorage.Jobs.Select(x=>new{x.Kind,x.SuspendedReason,x.RoundId})}));
            var choice=o.CurrentStorage.Jobs.Single(x=>x.Kind=="player_decision");
            Complete(o,choice,new(){["participate"]=decision=="accept",["decision"]=decision,["reason"]="Current interests",["position"]=Position("c")});
            var first=Publish(o,o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"),
                decision=="accept"?"We accept participation without agreeing to a treaty or war.":"We "+decision+" participation for now; this is not acceptance.",false);
            var receipt=r.PlayerResponses.Single();string acceptance=first.DocumentId;
            Test.True(receipt.AnswerDocumentId==acceptance&&!receipt.AnswerDelivered,"first publication establishes an undelivered immutable answer");
            if(deliverFirst)
            {
                h.CurrentDayValue+=20;o.ProcessPropagationArrivals();Test.True(receipt.AnswerDelivered,"actual propagation delivers original answer");
                // Other informed courts may independently consider intervening.
                // Complete those real decisions as no new motive, not by deleting work.
                for(int i=0;i<16;i++)
                {
                    var incidental=o.CurrentStorage.Jobs.FirstOrDefault(x=>x.Kind=="player_decision"&&x.ForcedIntent=="join_known_event");
                    if(incidental==null)break;
                    Complete(o,incidental,new(){["participate"]=false,["reason"]="No additional stake in this hearing"});
                    Call(o,"ScheduleEventCourtWork",r);
                }
            }
            Reload(o);r=o.ResolveRound(roundId);receipt=r.PlayerResponses.Single();
            var c=r.Participants.Single(x=>x.KingdomId=="c");
            Test.True(c.State==(decision=="accept"?"active":"observer"),"all five decisions survive reload without false admission");
            var original=o.ResolveDocument(acceptance);Call(o,"HandleEventPublication",original);
            Test.True(receipt.AnswerDocumentId==acceptance&&receipt.AnswerDelivered==deliverFirst,"duplicate callback preserves original receipt and delivery");
            if(decision!="accept")
            {
                // Replay a later published historical clarification through the
                // owner: referencing a completed refusal cannot admit an observer.
                var later=new WorldDiplomacyDocument{DocumentId="later-clarification",RoundId=roundId,AuthorKingdomId="c",TargetKingdomId=sender,
                    SourceDocumentId="invite",Body="Our earlier answer remains unchanged.",IsReadyForPublication=true,Day=h.CurrentDayValue};
                o.AddDocument(later);Call(o,"HandleEventPublication",later);
                Test.True(c.State=="observer"&&receipt.AnswerDocumentId==acceptance&&receipt.AnswerDelivered==deliverFirst,
                    "completed nonacceptance reference preserves limited membership and original delivery receipt");
                continue;
            }
            Test.True((bool)Call(o,"ScheduleUnexpressedPosition",r),"accepted member still gets an actual core expression opportunity");
            var follow=o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate");
            Test.True(follow.SourceDocumentId=="invite","regression uses original invitation as known context");
            var expression=Publish(o,follow,"We seek public recognition and offer joint review, but will not confess wrongdoing; without agreement, the dispute remains unresolved.",true);
            string expressionId=expression.DocumentId;
            Test.True(c.AgendaExpressionDocumentId==expressionId&&receipt.AnswerDocumentId==acceptance&&receipt.AnswerDelivered==deliverFirst,
                "published core statement records its own identity without reacceptance or receipt overwrite");
            o.CurrentStorage.RoundSchedulingSchemaVersion=0;
            o.CurrentStorage.Documents.Reverse();
            Call(o,"RecoverRoundSchedulingAfterLoad");
            Test.True(receipt.AnswerDocumentId==acceptance&&receipt.AnswerDelivered==deliverFirst,
                "legacy scheduling recovery preserves an existing immutable receipt despite reordered documents");
            // A genuine second invitation gets its own receipt but cannot replace
            // an already admitted party's current negotiated position.
            var second=new WorldDiplomacyDocument{DocumentId="independent-invite",RoundId=roundId,AuthorKingdomId=sender,TargetKingdomId="c",
                IsPlayerAuthored=sender=="p",IsReadyForPublication=true,AnalysisStatus="success",Body="Please continue participating in the review.",
                ResponseRequestsClassified=true,ResponseRequests=new(){new(){KingdomId="c",Kind="invitation",Quote="Please continue participating in the review."}}};
            o.AddDocument(second);Know(o,"c",second.DocumentId);
            if(sender=="p")Call(o,"RegisterPlayerResponseWork",second,true);
            else {Call(o,"HandleEventPublication",second);Call(o,"SchedulePlayerResponseWork",r);}
            var newChoice=o.CurrentStorage.Jobs.Single(x=>x.Kind=="player_decision");
            Test.True(newChoice.ForcedIntent=="answer_invitation"&&newChoice.SourceDocumentId==second.DocumentId,"independent invitation dispatch has the exact new source");
            Complete(o,newChoice,new(){["participate"]=true,["decision"]="accept",["reason"]="Continue current role",["position"]=Position("c")});
            var nextAnswer=Publish(o,o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"),"We accept continuing this review; our demand and conditions remain.",false);
            Test.True(receipt.AnswerDocumentId==acceptance&&r.PlayerResponses.Single(x=>x.SourceDocumentId==second.DocumentId).AnswerDocumentId==nextAnswer.DocumentId,
                "independent new invitation retains a separate actual answer receipt: "+JsonConvert.SerializeObject(new{acceptance,actual=nextAnswer.DocumentId,receipts=r.PlayerResponses}));
            Test.True(c.Agenda=="Recognition"&&c.RedLine=="No confession"&&c.AgendaExpressionDocumentId==expressionId,
                "new invitation to an active member cannot restore obsolete entry position");
            // A new claim must answer a real opposing source; repeated replies
            // to the already answered invitation cannot replenish ordinary turns.
            var freshClaim=new WorldDiplomacyDocument { DocumentId="fresh-claim",RoundId=roundId,AuthorKingdomId=sender,TargetKingdomId="c",
                SourceDocumentId=acceptance,IsPlayerAuthored=sender=="p",IsReadyForPublication=true,AnalysisStatus="success",
                Body="We dispute your proposed review and request your revised position." };
            freshClaim.AnsweredNpcDocumentIds.Add(acceptance);
            o.AddDocument(freshClaim);Call(o,"HandleEventPublication",freshClaim);Know(o,"c",freshClaim.DocumentId);
            var revised=Position("c");revised["agenda"]="Revised demand for an independent review";revised["red_line"]="No payment before evidence";
            Test.True((bool)Call(o,"TryApplyEventProgress",r,new JObject{["outcome"]="continue",["reason"]="C revises its current position",
                ["next_actor"]="c",["source_document"]="fresh-claim",
                ["positions"]=new JArray(r.Participants.Select(x=>x.KingdomId=="c"?revised:Position(x.KingdomId)))},""),
                "production position-update owner accepts revised current position");
            Publish(o,o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"),
                "We now demand an independent review and will make no payment before the evidence is examined. Our earlier participation remains in force.",true);
            Test.True(c.Agenda=="Revised demand for an independent review"&&c.RedLine=="No payment before evidence"
                &&c.AgendaExpressionDocumentId==expressionId&&receipt.AnswerDocumentId==acceptance,
                "actual progress and the fresh corresponding claim preserve evolved position and original receipts");
            Know(o,sender,expressionId);
            if(sender=="p")
            {
                var response=new WorldDiplomacyDocument{DocumentId="player-substantive-answer",RoundId=roundId,AuthorKingdomId="p",TargetKingdomId="c",
                    SourceDocumentId=expressionId,IsPlayerAuthored=true,Body="We reject your claim to recognition and the proposed review. The dispute remains unresolved.",
                    AnalysisStatus="pending_analysis",Day=h.CurrentDayValue};
                o.AddDocument(response);o.PublishPlayerAuthoredDocumentImmediately(response);
                o.CommitAnalysis(new(){Kind="analyze",DocumentId=response.DocumentId,AuthorKingdomId="p"},new JObject{
                    ["status"]="success",["tone"]="firm",["confidence"]=1,["addressed_kingdom_ids"]=new JArray("c"),
                    ["actions"]=new JArray(new JObject{["target_kingdom_id"]="c",["intent"]="statement",["commitment"]="none",["requires_response"]=false}),
                    ["response_requests"]=new JArray(),["answered_npc_document_ids"]=new JArray(expressionId)}.ToString());
                Test.True(response.IsReadyForPublication&&response.AnalysisStatus=="success","actual player publication and analysis complete the response");
            }
            else
            {
                Test.True((bool)Call(o,"ScheduleUnexpressedPosition",r),"NPC counterpart schedules actual response");
                Publish(o,o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"),"We reject your claim to recognition and the proposed review. No agreement has been reached.",false,expressionId);
            }
            Test.True(c.AgendaAnswerDocumentIds.Count==1&&o.CurrentStorage.CountryParticipation.ContainsKey("c"),
                "invited member's actual expression and corresponding reply establish real participation");
            Reload(o);r=o.ResolveRound(roundId);receipt=r.PlayerResponses.Single(x=>x.SourceDocumentId=="invite");
            Test.True(receipt.AnswerDocumentId==acceptance&&receipt.AnswerDelivered==deliverFirst,"post-discussion reload preserves first answer and its delivery bit");
            Test.True(!(bool)Call(o,"ScheduleUnexpressedPosition",r),"completed position is not scheduled forever");
            Test.True(r.EventConvergence.Ending || (bool)Call(o,"TryApplyEventProgress",r,new JObject{["outcome"]="deferred",["reason"]="All actual positions were addressed; no agreement",
                ["positions"]=new JArray(r.Participants.Select(x=>Position(x.KingdomId)))},""),"accepted member completes actual discussion before legitimate closure");
            if(!deliverFirst){h.CurrentDayValue+=20;o.ProcessPropagationArrivals();Test.True(receipt.AnswerDelivered,"later propagation still targets original acceptance receipt");}
        }
    }
    private static JObject Draft(string text,params string[] heroes) => new(){["title"]="Petition hearing",["body"]=text,
        ["effects"]=new JArray(new JObject{["kind"]="ruler_relation",["kingdom"]="a",["other"]="b",["delta"]=-1,["days"]=0,["reason"]="Dispute"}),
        ["remove_effects"]=new JArray(),["confirmed_documents"]=new JArray(),["narrative"]=new JArray(new JObject{
            ["kind"]="adopted_npc_story",["subject"]="a",["source"]="adopted:hearing",["text"]=text,["hero_ids"]=new JArray(heroes)})};
    private static void Foundation(WorldDiplomacyRound r)
    {
        r.EventBackground="A petition hearing.";
        foreach(var p in r.Participants){p.Agenda="Recognition";p.PreferredOutcome="Review";p.ExpectedCost="Concession";p.RedLine="No confession";p.Alternative="Unresolved";}
    }
    private static void SemanticAudit()
    {
        var cases=new (string text,string[] declared,string[] actual,bool ok)[] {
            ("The ordinary lord asked, \"Will the council hear our petition?\"",new[]{"ordinary_lord"},new[]{"ordinary_lord"},true),
            ("WILL THE COUNCIL HEAR THE ORDINARY LORD?",new[]{"ordinary_lord"},new[]{"ordinary_lord"},true),
            ("May the ordinary lord attend?",new[]{"ordinary_lord"},new[]{"ordinary_lord"},true),
            ("Rose petals adorned the ordinary lord's petition.",new[]{"ordinary_lord"},new[]{"ordinary_lord"},true),
            ("Will submitted the ordinary lord's petition.",new[]{"ordinary_lord"},new[]{"ordinary_lord","will"},false),
            ("Will submitted the petition.",new[]{"will"},new[]{"will"},true),
            ("Anna contradicted Ann.",new[]{"anna"},new[]{"anna","ann"},false),
            ("罗伯特在听证会上作证。",new[]{"same2"},new[]{"same2"},true),
            ("罗伯特在听证会上作证。",new[]{"same1","same2"},new[]{"same2"},false),
            ("The source identifies the witness called Rowan as the ordinary lord.",new[]{"ordinary_lord"},new[]{"ordinary_lord"},true),
            ("A {NAME} $1 testified.",new[]{"dynamic"},new[]{"dynamic"},true),
            ("Will testified.",new[]{"not-a-current-actor"},new[]{"not-a-current-actor"},false),
            ("The player secretly testified.",new[]{"ruler_p"},new[]{"ruler_p"},false)
        };
        foreach(var test in cases)
        {
            var(h,o)=Owner();var r=o.EnsureActiveRound("a","b",false);
            var fact=(JObject)Draft(test.text,test.declared)["narrative"]![0]!;
            fact["source"]="lore:actors";
            JObject P(string id,string target){var p=Position(id);p["source_keys"]=new JArray("lore:actors");p["address_to"]=new JArray(target);return p;}
            var concept=new JObject{["title"]="Petition hearing",["background"]="A current petition prompts rival claims.",["initiator"]="a",
                ["material_keys"]=new JArray("lore:actors"),["participants"]=new JArray(P("a","b"),P("b","a")),["narrative"]=new JArray(fact),
                ["party_review"]=new JArray(new[]{"a","b"}.Select(x=>new JObject{["kingdom"]=x,["included"]=true,["reason"]="Source-supported dispute party"}))};
            string sources=new JArray(new JObject{["key"]="lore:actors",["known_by"]="public",
                ["text"]="The current source identifies the witness same2 rather than namesake same1, and records Rowan as an alias of ordinary_lord.",
                ["hero_ids"]=new JArray(h.CaptureNarrativeActors(new[]{"a","b"}).Keys)}).ToString();
            Test.True(ReviewedConcept(o,r,new(){"a","b"},sources,concept,test.actual)==test.ok,
                "actual concept writer/audit resolves ordinary words, aliases and namesakes before adoption: "+test.text);
        }
        foreach(var test in cases)
        foreach(string stage in new[]{"started","progress:1","ended"})
        {
            var(_,o)=Owner();var r=o.EnsureActiveRound("a","b",false);Foundation(r);var draft=Draft(test.text,test.declared);
            if(stage.StartsWith("progress")){o.AddDocument(new(){DocumentId="evidence",RoundId=r.RoundId,IsReadyForPublication=true});
                Know(o,"a","evidence");Know(o,"b","evidence");draft["progress_evidence"]=new JArray("evidence");}
            Test.True(ReviewedBoundary(o,r,stage,draft,test.actual)==test.ok,"independent actor semantics + actual boundary "+stage+": "+test.text);
            if(!test.ok){Test.True(o.CurrentStorage.EventAnnouncements.Count==0,"rejected actor meaning has no publication or personal memory");continue;}
            Reload(o);
            Test.True(o.CurrentStorage.EventAnnouncements.Single().Narrative.Single().Text==test.text
                && WorldDiplomacyMemoryApplication.Build(new(o.CurrentStorage,"a",new(),test.declared[0]),test.text,d=>d.ToString()).Contains(JsonConvert.ToString(test.text)),
                "actual actor recalls original text with normal JSON quoting: "+stage+"/"+test.text);
        }
        foreach(string failure in new[]{"missing","wrong-digest","wrong-id","missing-row","duplicate-row","incomplete","stale-draft","invalid-index"})
        {
            var(h,o)=Owner();var r=o.EnsureActiveRound("a","b",false);Foundation(r);
            var job=new WorldDiplomacyJob{JobId="bad-audit",Kind="event_boundary",RoundId=r.RoundId,AuthorKingdomId="a",ForcedIntent="started"};
            o.CurrentStorage.Jobs.Add(job);o.PrepareSharedRequest(job);Complete(o,job,Draft("Will testified.","will"));o.PrepareSharedRequest(job);
            var audit=Audit(job,new[]{"will"});
            if(failure=="missing")audit.Remove("actor_review");
            if(failure=="wrong-digest")audit["actor_review"]![0]!["digest"]="old";
            if(failure=="wrong-id")audit["actor_review"]![0]!["hero_ids"]=new JArray("ordinary_lord");
            if(failure=="missing-row")audit["actor_review"]=new JArray();
            if(failure=="duplicate-row")((JArray)audit["actor_review"]!).Add(audit["actor_review"]![0]!.DeepClone());
            if(failure=="incomplete")audit["actor_review"]![0]!["complete"]=false;
            if(failure=="stale-draft")job.PrivacyDraft=Draft("Will contradicted the account.","will").ToString();
            if(failure=="invalid-index")audit["actor_review"]![0]!["index"]=long.MaxValue;
            Complete(o,job,audit);
            bool rewrite=failure=="wrong-id";
            Test.True(h.Writes==0&&o.CurrentStorage.EventAnnouncements.Count==0
                &&job.Kind==(rewrite?"event_boundary":"event_boundary_audit")
                &&(rewrite?job.SemanticRepairAttempts==1:job.PrivacyAuditRetries==1),
                "semantic mismatch rewrites the narrative; malformed/stale review repairs the same draft before effects: "+failure);
            Reload(o);job=o.CurrentStorage.Jobs.Single();o.PrepareSharedRequest(job);
            if(rewrite){Complete(o,job,Draft("Will testified.","will"));o.PrepareSharedRequest(job);}
            Complete(o,job,Audit(job,new[]{"will"}));
            Test.True(o.CurrentStorage.EventAnnouncements.Single().State=="public","valid fresh review recovers after reload: "+failure);
        }
    }
    private static void ProgressAudit()
    {
        foreach(bool correct in new[]{false,true})
        {
            var(h,o)=Owner();var r=o.EnsureActiveRound("a","b",false);Foundation(r);
            o.AddDocument(new(){DocumentId="progress-evidence",RoundId=r.RoundId,IsReadyForPublication=true});
            Know(o,"a","progress-evidence");Know(o,"b","progress-evidence");
            var update=Draft("The ordinary lord asked, \"Will the council hear us?\"","ordinary_lord");
            update["progress_evidence"]=new JArray("progress-evidence");
            var progress=new JObject{["outcome"]="deferred",["reason"]="The testimony was heard; no agreement",
                ["closure"]=new JArray(new[]{"a","b"}.Select(id=>new JObject{["kingdom"]=id,["status"]="unresolved",
                    ["reason"]="The testimony leaves the disputed claim unresolved.",["sources"]=new JArray("progress-evidence")})),
                ["positions"]=new JArray(Position("a"),Position("b")),["effect_update"]=update};
            var job=new WorldDiplomacyJob{JobId="progress-audit",Kind="event_progress",RoundId=r.RoundId,AuthorKingdomId="a"};
            o.CurrentStorage.Jobs.Add(job);o.PrepareSharedRequest(job);Complete(o,job,progress);o.PrepareSharedRequest(job);
            Test.True(JObject.Parse(job.UserPrompt)["narrative_actors"]!["will"]!.Value<string>()=="Will",
                "actual progress audit gets identities, not an unaudited nested effect narrative");
            var review=correct?Audit(job,new[]{"ordinary_lord"}):new JObject{["approved"]=true};
            review["closure_consistent"]=true;Complete(o,job,review);
            Test.True(o.CurrentStorage.EventAnnouncements.Any(x=>x.Stage.StartsWith("progress:"))==correct
                && (h.Writes>0)==correct,"progress effect_update requires the same bound semantic audit");
        }
    }
}
