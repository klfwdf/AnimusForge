using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class MaterialCountryCompletionReplay
{
    private static object Call(WorldDiplomacyOrchestration o,string n,params object[] a) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(o,a)!;
    private sealed class Host : EventStructureReplay.Host, IDiplomacyMaterialVersionHost
    {
        internal Dictionary<string,string> Versions = new();
        public string CaptureMaterialVersion(string id) => Versions.GetValueOrDefault(id,"initial");
    }
    private static (Host,WorldDiplomacyOrchestration) Owner()
    {
        var h = new Host { PublishEnabled=true,PlayerAwareExecution=true,OralRange=(0,3000),CurrentDayValue=40 };
        var o = new WorldDiplomacyOrchestration(h,new WorldDiplomacyRuntimeState()); h.Owner=o; return(h,o);
    }
    private static void Reload(WorldDiplomacyOrchestration o) =>
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
    private static void Complete(WorldDiplomacyOrchestration o,WorldDiplomacyJob j,JObject result) {
        CountryAdmissionReplay.BeforeLegacyConcept(o, j, result);
        o.TryCompleteDiplomacyPipeline(j,new LlmJobResult {Success=true,Content=result.ToString()});
    }
    internal static void Run()
    {
        MaterialSupplyReplay.Run(); Investigations(); BoundaryActors(); Memory(); Contacts(); ContactPublication(); AgendaAndSideQuestions();
        MaterialCountryRepairReplay.Run();
        MaterialCountryThirdReplay.Run();
        Console.WriteLine("Completion regression: fair exploration, boundary actor identity, personal relevance, independent contacts, core agenda and side work PASS; live/model NOT_RUN.");
    }
    private static void Investigations()
    {
        var(h,o)=Owner(); string[] countries={"a","b","c"}; int day=1;
        foreach(string k in countries)o.CurrentStorage.CountryParticipation[k]=new(){LastExpressionDay=day++};
        List<string> Select()=>(List<string>)Call(o,"SelectMaterialInvestigations",(object)countries);
        void Record(List<string> ids)=>Call(o,"RecordMaterialInvestigation",new WorldDiplomacyJob{EventInvestigationKingdoms=ids});
        var first=Select();Test.True(first.SequenceEqual(new[]{"a","b"}),"oldest absence investigated first");Record(first);
        Reload(o);var next=Select();Test.True(next.SequenceEqual(new[]{"c"}),"unchanged a/b cannot starve unvisited c across reload");Record(next);
        Test.True(Select().Count==0,"unchanged investigation not repeatedly selected");
        h.Versions["b"]="changed";Test.True(Select().SequenceEqual(new[]{"b"}),"changed source version becomes eligible immediately");Record(Select());
        h.CurrentDayValue+=7;Test.True(Select().SequenceEqual(new[]{"a","c"}),"finite revisit uses least recently inspected sequence");
        o.CurrentStorage.CountryParticipation["a"]=new(){LastExpressionDay=47};Record(Select());
        Test.True(Select().SequenceEqual(new[]{"b"}),"actual participation never prevents later material investigation");
        var(h2,o2)=Owner();o2.TryScheduleNormalRound();var job=o2.CurrentStorage.Jobs.Single(x=>x.Kind=="event_concept");
        o2.PrepareSharedRequest(job);Complete(o2,job,new(){["candidates"]=new JArray(),["reason"]="No qualified current story"});
        Test.True(job.MaterialSearchPass==1&&job.SuspendedReason==""&&job.EventMaterialSnapshot=="","normal empty pool starts one fresh exploration without technical retry");
        o2.PrepareSharedRequest(job);Complete(o2,job,new(){["candidates"]=new JArray(),["reason"]="Still no qualified current story"});
        Test.True(o2.ResolveRound(job.RoundId).State=="active" && job.MaterialNextSearchDay==0&&job.MaterialSearchPass==2&&o2.CurrentStorage.EventAnnouncements.Count==0&&o2.CurrentStorage.NpcPlayerContactCount==0,
            "ordinary no-result reaches final bounded batch without publication or contact");
        var(_,o3)=Owner();o3.TryScheduleNormalRound();var compared=o3.CurrentStorage.Jobs.Single(x=>x.Kind=="event_concept");o3.PrepareSharedRequest(compared);
        Complete(o3,compared,new(){["candidates"]=new JArray(new JObject{["id"]="weak",["kingdoms"]=new JArray("a","b"),["material_keys"]=new JArray("lore:succession"),
            ["conflict"]="Contested oath",["motive"]="Recognition",["new_development"]="Uncertain account",["theme"]="history", ["theme_reason"]="Conflicting historical accounts motivate the current demand for recognition", ["npc_creation"]="None",["negotiation_space"]="Review"})});
        Complete(o3,compared,new(){["assessments"]=new JArray(new JObject{["id"]="weak",["qualified"]=false,["duplicate_of"]="",["strength"]="Attributed account",
            ["weakness"]="No current stake",["novelty_reason"]="No meaningful new motive",["urgency_reason"]="None"}),["best_equivalent"]=new JArray(),["urgent"]=new JArray(),["comparison_reason"]="All candidates fail current-interest quality"});
        Test.True(compared.MaterialSearchPass==1&&compared.SuspendedReason==""&&compared.EventCandidatePool==""
            &&o3.ResolveRound(compared.RoundId).EventSelectionEvidence.Contains("All candidates fail"),"all-unqualified comparison refreshes material instead of retrying invalid output or forcing publication");
    }
    private static JObject Boundary(string text,string id="ordinary_lord") => new(){
        ["title"]="Witness account",["body"]="The lord submitted testimony.",
        ["effects"]=new JArray(new JObject{["kind"]="ruler_relation",["kingdom"]="a",["other"]="b",["delta"]=-1,["days"]=0,["reason"]="The dispute strains relations."}),
        ["remove_effects"]=new JArray(),["confirmed_documents"]=new JArray(),["narrative"]=new JArray(new JObject{
            ["kind"]="adopted_npc_story",["subject"]="a",["source"]="adopted:testimony",["text"]=text,["hero_ids"]=new JArray(id)})};
    private static void Foundation(WorldDiplomacyRound r)
    {
        r.EventBackground="The lord's testimony is disputed.";
        foreach(var p in r.Participants){p.Agenda="recognition";p.PreferredOutcome="hearing";p.ExpectedCost="review";p.RedLine="no confession";p.Alternative="unresolved";}
    }
    private static void BoundaryActors()
    {
        var(_,guard)=Owner();
        var inventedPlayer=new JObject{["kind"]="adopted_npc_story",["hero_ids"]=new JArray("ruler_p")};
        Test.True(!(bool)Call(guard,"TryReadNarrativeActors",inventedPlayer,new HashSet<string>{"ruler_p"},new List<string>(),""),
            "NPC creation cannot bind invented personal experience to the player even when a source includes their real ID");
        foreach(string stage in new[]{"started","progress:1","ended"})
        {
            var(h,o)=Owner();var r=o.EnsureActiveRound("a","b",false);Foundation(r);
            var draft=Boundary("The ordinary lord submitted a forged witness statement.");
            if(stage.StartsWith("progress"))
            {
                o.AddDocument(new(){DocumentId="evidence",RoundId=r.RoundId,IsReadyForPublication=true});draft["progress_evidence"]=new JArray("evidence");
                foreach(string k in new[]{"a","b"})WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge,k,"evidence",h.CurrentDayValue);
            }
            Test.True((bool)Call(o,"TryPublishEventBoundary",r,stage,draft,""),"actual boundary publication: "+stage);
            var notice=o.CurrentStorage.EventAnnouncements.Single();
            Test.True(notice.Narrative.Single().HeroIds.SequenceEqual(new[]{"ordinary_lord"}),"actor ID survives every boundary path: "+stage);
            int writes=h.Writes;Call(o,"TryPublishEventBoundary",r,stage,draft,"");
            Test.True(h.Writes==writes,"retry uses frozen receipt without replaying effect: "+stage);
            if(stage=="progress:1")
            {
                notice.State="pending";WorldDiplomacyMemoryApplication.Invalidate(o.CurrentStorage);
                Test.True(!WorldDiplomacyMemoryApplication.Build(new(o.CurrentStorage,"a",new(),"ordinary_lord"),"forged witness",d=>d.ToString()).Contains("forged witness"),"pending receipt is not a personal fact");
                Call(o,"TryPublishEventBoundary",r,stage,draft,"");
                Test.True(WorldDiplomacyMemoryApplication.Build(new(o.CurrentStorage,"a",new(),"ordinary_lord"),"forged witness",d=>d.ToString()).Contains("forged witness")&&h.Writes==writes,
                    "completing an existing staged receipt invalidates memory index without repeating its effect");
            }
            Reload(o);Test.True(o.CurrentStorage.EventAnnouncements.Single().Narrative.Single().HeroIds.Count==1,"actor identity survives reload: "+stage);
            Test.True(WorldDiplomacyMemoryApplication.Build(new(o.CurrentStorage,"a",new(),"ordinary_lord"),"forged witness",d=>d.ToString()).Contains("forged witness"),"adopted actor fact reaches recall: "+stage);
            if(stage=="progress:1")Test.True(!WorldDiplomacyMemoryApplication.Build(new(o.CurrentStorage,"c",new(),"unrelated"),"forged witness",d=>d.ToString()).Contains("forged witness"),"limited progress fact does not leak to uninformed outsider");
        }
        foreach(string bad in new[]{"missing","unknown","misclassified"})
        {
            var(_,o)=Owner();var r=o.EnsureActiveRound("a","b",false);Foundation(r);var draft=Boundary("The ordinary lord submitted testimony.");
            var fact=(JObject)draft["narrative"]![0]!;
            if(bad=="unknown")fact["hero_ids"]=new JArray("invented_id");else fact.Remove("hero_ids");
            if(bad=="misclassified")fact["actor_scope"]="collective";
            Test.True(!MaterialCountryThirdReplay.ReviewedBoundary(o,r,"started",draft,new[]{"ordinary_lord"})
                &&o.CurrentStorage.EventAnnouncements.Count==0,"invalid named actor cannot publish after independent audit: "+bad);
        }
    }
    private static void Memory()
    {
        var(_,o)=Owner();for(int i=0;i<5;i++)o.CurrentStorage.EventAnnouncements.Add(new(){Id="notice-"+i,EventId="event-"+i,State="public",Day=i+1,Title="Hearing",
            Narrative=new(){new(){Id=i==0?"target-old":"recent-"+i,Kind="adopted_npc_story",Public=true,HeroIds=new(){"ordinary_lord"},Source=i==0?"adopted:amber":"adopted:hearing",
                Text=(i==0?"amber oath ":"oath ")+new string('x',1450)}}});
        foreach(bool reload in new[]{false,true})
        {
            if(reload)Reload(o);
            foreach(string query in new[]{"amber oath","amber pledge","target-old","adopted:amber"})
            {
                string s=WorldDiplomacyMemoryApplication.Build(new(o.CurrentStorage,"a",new(),"ordinary_lord"),query,d=>d.ToString());
                Test.True(s.Contains("target-old")&&s.Contains(new string('x',1450))&&s.Length<=7000,"older most relevant whole experience survives long similar newer facts: "+query+" reload="+reload);
            }
        }
    }
    private static void Contacts()
    {
        var(h,o)=Owner();var r=o.EnsureActiveRound("a","p",false);
        WorldDiplomacyDocument Invite(string id,string author)=>new(){DocumentId=id,AuthorKingdomId=author,TargetKingdomId="p",RoundId=r.RoundId,IsReadyForPublication=true,
            ResponseRequests=new(){new(){KingdomId="p",Kind="invitation",Quote="Join the hearing."}}};
        h.CurrentDayValue=10;var first=Invite("first","a");o.AddDocument(first);Call(o,"RecordPublishedPlayerContact",r,first);
        h.CurrentDayValue=20;var second=Invite("second","b");o.AddDocument(second);Call(o,"RecordPublishedPlayerContact",r,second);
        Test.True(o.CurrentStorage.NpcPlayerContactCount==2&&o.CurrentStorage.LastNpcPlayerContactDay==20,"two independent invitations in same case count twice");
        Reload(o);r=o.ResolveRound(r.RoundId);var repeated=Invite("reminder","b");repeated.PlayerContactId="second";Call(o,"RecordPublishedPlayerContact",r,repeated);
        Test.True(o.CurrentStorage.NpcPlayerContactCount==2,"reloaded reminder preserves contact identity");
        var job=new WorldDiplomacyJob{JobId="third",RoundId=r.RoundId,AuthorKingdomId="a",TargetKingdomId="p"};
        var draft=new JObject{["body"]="Join a separate arbitration.",["response_requests"]=new JArray(new JObject{["kingdom"]="p",["kind"]="invitation",["quote"]="Join a separate arbitration."})};
        Test.True((bool)Call(o,"DeferNewPlayerContact",job,draft.ToString())&&job.Kind=="contact_wait"&&job.ContactNotBeforeDay==25&&job.SuspendedReason=="","new invitation waits without technical failure");
        job=JsonConvert.DeserializeObject<WorldDiplomacyJob>(JsonConvert.SerializeObject(job))!;
        Test.True(!o.CanDispatchDiplomacyJob(job)&&job.DeferredContactDraft==draft.ToString(),"waiting draft survives save and is not dispatched early");
        h.CurrentDayValue=25;Test.True(!(bool)Call(o,"DeferNewPlayerContact",job,draft.ToString()),"same invitation becomes eligible after contact interval");
        h.CurrentDayValue=21;job.SourceDocumentId="required";r.PlayerResponses.Add(new(){SourceDocumentId="required",KingdomId="a"});
        Test.True(!(bool)Call(o,"DeferNewPlayerContact",job,draft.ToString()),"required response never enters new-contact wait");
        var(_,other)=Owner();var waiting=other.EnsureActiveRound("a","c",false);
        var unsent=new WorldDiplomacyDocument{DocumentId="unsent-invitation",RoundId=waiting.RoundId,AuthorKingdomId="a",TargetKingdomId="c",IsReadyForPublication=true,
            ResponseRequests=new(){new(){KingdomId="c",Kind="invitation",Quote="Please join this case."}}};
        other.AddDocument(unsent);Call(other,"HandleEventPublication",unsent);waiting.EventPhase="closing";waiting.EventOutcome="deferred";other.CloseRound("event_deferred",waiting);
        Test.True(waiting.State=="active"&&waiting.PlayerResponses.Single().Status=="pending"&&!other.CurrentStorage.Jobs.Any(x=>x.Kind=="player_decision"),
            "NPC invitation blocks premature closure before delivery without pretending recipient already knows it");
    }
    private static void AgendaAndSideQuestions()
    {
        var(_,o)=Owner();var r=o.EnsureActiveRound("a","b",false);Foundation(r);var a=r.Participants.Single(x=>x.KingdomId=="a");a.AgendaAddressTo.Add("b");
        var greeting=new WorldDiplomacyDocument{DocumentId="greeting",RoundId=r.RoundId,AuthorKingdomId="a",TargetKingdomId="b",Body="Greetings.",IsReadyForPublication=true};
        o.AddDocument(greeting);Call(o,"HandleEventPublication",greeting);
        Test.True(a.AgendaExpressionDocumentId==""&&!o.CurrentStorage.CountryParticipation.ContainsKey("a"),"unrelated published words do not fulfill a core agenda");
        var expression=new WorldDiplomacyDocument{DocumentId="position",RoundId=r.RoundId,AuthorKingdomId="a",TargetKingdomId="b",Body="We demand recognition through a hearing.",AuditedAgendaQuote="We demand recognition through a hearing.",Day=40,IsReadyForPublication=true};
        o.AddDocument(expression);Call(o,"HandleEventPublication",expression);
        Test.True(a.AgendaExpressionDocumentId=="position"&&!o.CurrentStorage.CountryParticipation.ContainsKey("a"),"expression without corresponding negotiation is not completed participation");
        var answer=new WorldDiplomacyDocument{DocumentId="answer",RoundId=r.RoundId,AuthorKingdomId="b",TargetKingdomId="a",Body="We will hear the evidence but reserve recognition.",AuditedAgendaAnswers=new(){"position"},IsReadyForPublication=true};
        o.AddDocument(answer);Call(o,"HandleEventPublication",answer);
        Test.True(a.AgendaAnswerDocumentIds.Contains("answer")&&o.CurrentStorage.CountryParticipation["a"].LastDocumentId=="position","substantive audited answer completes participation");
        var r2=o.EnsureActiveRound("c","p",true);r2.PlayerResponses.Add(new(){KingdomId="c",SourceDocumentId="side",RequestKind="side_question"});
        r2.PlayerCourtWork.Add(new(){KingdomId="c",SourceDocumentId="side",Status="queued"});
        o.CurrentStorage.Jobs.Add(new(){JobId="side-job",RoundId=r2.RoundId,Kind="generate",SourceDocumentId="side",AuthorKingdomId="c",PlayerResponseSourceIds=new(){"side"}});
        Test.True(!(bool)Call(o,"EventHasPendingWork",r2,null),"side-only response job and court work do not indirectly block conclusion");
        r2.PlayerResponses.Add(new(){KingdomId="c",SourceDocumentId="core",RequestKind="case_question"});
        Test.True((bool)Call(o,"EventHasPendingWork",r2,null),"core obligation still blocks conclusion");
        r2.PlayerResponses.RemoveAll(x=>x.SourceDocumentId=="core");
        r2.PlayerResponses.Single().OriginalRoundId=r2.RoundId;
        o.AddDocument(new(){DocumentId="side",RoundId=r2.RoundId,AuthorKingdomId="p",TargetKingdomId="c",IsPlayerAuthored=true,
            IsReadyForPublication=true,AnalysisStatus="success",Body="Where is the envoy?",ResponseRequestsClassified=true,
            ResponseRequests=new(){new(){KingdomId="c",Kind="side_question",Quote="Where is the envoy?"}}});
        r2.EventPhase="closing";r2.EventOutcome="deferred";
        o.CloseRound("event_deferred",r2);
        var follow=o.CurrentStorage.ConcurrentRounds.Single(x=>x.EventSourceType=="player_followup");
        Test.True(r2.State=="closed"&&follow.PlayerResponses.Single().SourceDocumentId=="side"&&o.ResolveDocument("side").RoundId==r2.RoundId,
            "closing keeps exact side-question source and transfers the outstanding answer without republishing the question");
        Test.True((bool)Call(o,"EventBoundaryComplete",follow,"started")&&(bool)Call(o,"EventBoundaryComplete",follow,"ended"),
            "carried answer requires no fabricated event announcement or effect");
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge,"c","side",40);
        Call(o,"SchedulePlayerResponseWork",follow);
        var reply=o.CurrentStorage.Jobs.Single(x=>x.RoundId==follow.RoundId&&x.Kind=="generate");
        o.PrepareSharedRequest(reply);Complete(o,reply,new(){["safe"]=true,["public_context"]="The envoy's current whereabouts are unknown. Answer the original question honestly."});o.PrepareSharedRequest(reply);
        var response=new JObject{["title"]="Envoy whereabouts",["body"]="We do not know the envoy's current whereabouts.",
            ["actions"]=new JArray(new JObject{["target_kingdom_id"]="p",["intent"]="statement",["commitment"]="none",["negotiation_move"]="state_position"}),
            ["requires_response"]=false,["round_participation"]="continue",["round_status"]="continue",["response_requests"]=new JArray(),["answered_player_document_ids"]=new JArray("side")};
        o.CommitGeneratedDocument(reply,response.ToString());Complete(o,reply,new(){["safe"]=true,["position_owner_valid"]=true,["negotiation_consistent"]=true,["response_continuity"]=MaterialCountryThirdReplay.ContinuityReview(o,reply)});
        Test.True(follow.PlayerResponses.Single().Status=="answered"&&o.ResolveDocument(follow.PlayerResponses.Single().AnswerDocumentId).IsReadyForPublication,
            "transferred side question actually receives a formal published answer after original case closure");
        Test.True(o.CurrentStorage.EventAnnouncements.All(x=>x.EventId!=follow.RoundId),"side answer creates no new political event or boundary effect");
    }

    private static void ContactPublication()
    {
        var(h,o)=Owner();h.CurrentDayValue=11;var r=o.EnsureActiveRound("a","p",false);
        r.EventBeginningId=r.RoundId+":started";
        o.CurrentStorage.EventAnnouncements.Add(new(){Id=r.EventBeginningId,EventId=r.RoundId,Stage="started",State="public",CanonicalHistoryRecorded=true});
        o.CurrentStorage.LastNpcPlayerContactDay=10;o.CurrentStorage.NpcPlayerContactCount=1;
        o.EnqueueGeneration("a","p",null,false,null,90,roundId:r.RoundId);
        var j=o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate");
        o.PrepareSharedRequest(j);Complete(o,j,new(){["safe"]=true,["public_context"]="Public proposal for a new hearing."});o.PrepareSharedRequest(j);
        var draft=new JObject{["title"]="New invitation",["body"]="We invite you to a separate hearing about the disputed testimony.",
            ["actions"]=new JArray(new JObject{["target_kingdom_id"]="p",["intent"]="statement",["commitment"]="none",["negotiation_move"]="state_position"}),
            ["round_participation"]="continue",["round_status"]="continue",["requires_response"]=true,
            ["response_requests"]=new JArray(new JObject{["kingdom"]="p",["kind"]="invitation",["quote"]="We invite you to a separate hearing about the disputed testimony."})};
        o.CommitGeneratedDocument(j,draft.ToString());Complete(o,j,new(){["safe"]=true,["position_owner_valid"]=true,["negotiation_consistent"]=true,["response_continuity"]=MaterialCountryThirdReplay.ContinuityReview(o,j)});
        Test.True(j.Kind=="contact_wait"&&o.CurrentStorage.Documents.Count==0&&o.CurrentStorage.NpcPlayerContactCount==1,"audited new invitation remains unpublished during cooldown");
        string id=j.JobId,contact=j.PlayerContactId;Reload(o);j=o.CurrentStorage.Jobs.Single(x=>x.JobId==id);r=o.ResolveRound(r.RoundId);
        Test.True(j.Kind=="contact_wait"&&j.DeferredContactDraft.Contains("separate hearing"),"actual waiting job persists through owner reload");
        h.CurrentDayValue=15;o.PrepareSharedRequest(j);
        Test.True(j.Kind=="privacy_material","due invitation refreshes current privacy material");
        Complete(o,j,new(){["safe"]=true,["public_context"]="Current public facts and unchanged invitation."});
        Test.True(j.Kind=="privacy_audit"&&j.PrivacyDraft==draft.ToString(),"same stored invitation is independently reaudited rather than regenerated");
        Complete(o,j,new(){["safe"]=true,["position_owner_valid"]=true,["negotiation_consistent"]=true,["response_continuity"]=MaterialCountryThirdReplay.ContinuityReview(o,j)});
        var published=o.CurrentStorage.Documents.Single();
        Test.True(published.IsReadyForPublication&&published.PlayerContactId==contact&&o.CurrentStorage.NpcPlayerContactCount==2
            &&o.CurrentStorage.LastNpcPlayerContactDay==15,"deferred invitation actually publishes and counts once at publication: "+JsonConvert.SerializeObject(new{published.IsReadyForPublication,published.PlayerContactId,contact,published.MechanicalResult,published.AnalysisStatus,published.EventExecutionPending,o.CurrentStorage.NpcPlayerContactCount,o.CurrentStorage.LastNpcPlayerContactDay,j.Kind,j.SuspendedReason}));
        Call(o,"RecordPublishedPlayerContact",r,published);Reload(o);h.CurrentDayValue=25;o.ProcessPropagationArrivals();
        Test.True(o.CurrentStorage.NpcPlayerContactCount==2,"retry reload and delivery cannot count the same published invitation again");
    }
}
