using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Game and transport leaves are fakes; adoption, drafting/audit, publication,
// reply dispatch, reload and memory are the production owners.
internal static class MaterialCountryRepairReplay
{
    private static object Call(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args)!;
    private sealed class Host : EventStructureReplay.Host, IDiplomacyNarrativeActorHost
    {
        internal Dictionary<string,string> Names = new() { ["ordinary_lord"]="ordinary lord", ["rob"]="Rob",
            ["ann"]="Ann", ["anna"]="Anna", ["rob2"]="Rob", ["rose"]="Rose", ["cn"]="罗伯特", ["short_cn"]="罗伯",
            ["accent"]="Émile" };
        public new IReadOnlyDictionary<string,string> CaptureNarrativeActors(IReadOnlyCollection<string> ids) => Names;
    }
    private static (Host h, WorldDiplomacyOrchestration o) Owner()
    {
        var h = new Host { PublishEnabled=true, PlayerAwareExecution=true, OralRange=(0,3000), CurrentDayValue=40 };
        var o = new WorldDiplomacyOrchestration(h,new WorldDiplomacyRuntimeState()); h.Owner=o; return(h,o);
    }
    private static void Reload(WorldDiplomacyOrchestration o) =>
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
    private static JObject Position(string id,string target="a") => new() { ["kingdom"]=id,["agenda"]="Recognition",
        ["gain"]="Review",["cost"]="Concession",["red_line"]="No confession",["alternative"]="Unresolved",
        ["source_keys"]=new JArray("lore:test"),["address_to"]=new JArray(target) };
    private static JObject Concept(bool third=false, bool limited=false, bool excludedTarget=false) => new() {
        ["title"]="Witness dispute",["background"]="A and B dispute testimony; a present hearing requires clarification.",
        ["initiator"]="a",["material_keys"]=new JArray("lore:test"),
        ["participants"]=third ? new JArray(Position("a","c"),Position("b"),Position("c"))
            : new JArray(Position("a",limited || excludedTarget ? "c" : "b"),Position("b")),
        ["narrative"]=new JArray(new JObject { ["kind"]="allegation",["subject"]="a",["source"]="lore:test",["text"]="The testimony is disputed." }),
        ["party_review"]=new JArray(new[]{"a","b","c"}.Select(k=>new JObject { ["kingdom"]=k,["included"]=k!="c" || third,
            ["reply_only"]=k=="c" && limited,["reason"]=k=="c" && limited ? "Clarify testimony without joining the dispute" : "Source-supported stake or exclusion" })) };
    private static string Sources => new JArray(new JObject { ["key"]="lore:test",["known_by"]="public",
        ["text"]="Current witness identities and disputed testimony",["hero_ids"]=new JArray("ordinary_lord","rob","rob2","ann","anna","rose","cn","short_cn","accent") }).ToString();
    private static bool Adopt(WorldDiplomacyOrchestration o, WorldDiplomacyRound r, JObject concept) =>
        (bool)Call(o,"TryAdoptEventConcept",r,new List<string>{"a","b","c"},Sources,concept,"");
    private static JObject Boundary() => new() { ["title"]="Witness dispute",["body"]="The courts dispute the testimony.",
        ["effects"]=new JArray(new JObject { ["kind"]="ruler_relation",["kingdom"]="a",["other"]="b",["delta"]=-1,["days"]=0,["reason"]="Disputed testimony" }),
        ["remove_effects"]=new JArray(),["confirmed_documents"]=new JArray(),["narrative"]=new JArray() };
    private static void Publish(WorldDiplomacyOrchestration o,WorldDiplomacyJob job) =>
        typeof(EventStructureReplay).GetMethod("PublishDeclaration",BindingFlags.Static|BindingFlags.NonPublic)!
            .Invoke(null,new object?[]{o,job,job.TargetKingdomId,"statement",false,null,null,false});
    private static void Know(WorldDiplomacyOrchestration o,string country,string document) =>
        WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge,country,document,40);
    internal static void Run()
    {
        AgendaEligibility(); ParticipationOrder(); ActorNames(); PackageConsumers();
        Console.WriteLine("Repair2 PASS: admitted/limited agenda queues and publication, monotone chronological participation, CN/EN actor identity boundaries; live/provider NOT_RUN.");
    }
    private static void AgendaEligibility()
    {
        foreach (bool three in new[]{false,true})
        {
            var(_,o)=Owner();var r=o.EnsureActiveRound("a","b",false);
            Test.True(Adopt(o,r,Concept(third:three)), "valid two/three-country concept accepted");
            Test.True(r.Participants.Count== (three?3:2), "excluded candidate is not silently enrolled");
            Test.True((bool)Call(o,"TryPublishEventBoundary",r,"started",Boundary(),""),"opening actually publishes");
            Call(o,"StartEventDiscussion",r);var job=o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate");
            Test.True(job.TargetKingdomId==(three?"c":"b"),"first actual draft addresses the approved core target");
            Publish(o,job);
            Test.True(o.CurrentStorage.Documents.Any(x=>x.IsReadyForPublication&&x.TargetKingdomId==(three?"c":"b")),
                "admitted agenda reaches actual declaration publication");
        }
        {
            var(_,o)=Owner();var r=o.EnsureActiveRound("a","b",false);
            Test.True(!Adopt(o,r,Concept(excludedTarget:true)) && r.EventBackground=="",
                "excluded core addressee rejected atomically before a stranded event exists");
        }
        {
            var(h,o)=Owner();var r=o.EnsureActiveRound("a","b",false);
            h.UnavailableParty="c";
            Test.True(!Adopt(o,r,Concept(limited:true)),"lost authority cannot be adopted even as a limited respondent");
        }
        {
            var(h,o)=Owner();var r=o.EnsureActiveRound("a","b",false);
            Test.True(Adopt(o,r,Concept()),"authority-loss case adopts normally");
            Call(o,"TryPublishEventBoundary",r,"started",Boundary(),"");
            Call(o,"StartEventDiscussion",r);Publish(o,o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"));
            var expression=o.CurrentStorage.Documents.Single(x=>x.IsReadyForPublication);
            Know(o,"b",expression.DocumentId);h.UnavailableParty="a";Call(o,"ReconcileEventEligibility",r);
            Test.True(!(bool)Call(o,"ScheduleUnexpressedPosition",r),"no reply is queued to a former sovereign");
            Test.True((bool)Call(o,"TryApplyEventProgress",r,new JObject { ["outcome"]="deferred",["reason"]="Original claimant lost independent authority",
                ["positions"]=new JArray(Position("a","b"),Position("b")) },""),
                "objective loss of original claimant cannot leave an impossible core-answer close guard");
        }
        foreach(bool loseAuthority in new[]{false,true})
        {
            var(h,o)=Owner();var r=o.EnsureActiveRound("a","b",false);
            Test.True(Adopt(o,r,Concept(limited:true)), "explicit limited answer arrangement adopted");
            string eventId=r.RoundId;
            Test.True(r.Participants.Single(x=>x.KingdomId=="c").State=="observer"
                && r.Participants.Single(x=>x.KingdomId=="c").Agenda=="","limited respondent has no national position or membership");
            Test.True((bool)Call(o,"TryPublishEventBoundary",r,"started",Boundary(),""),"limited arrangement opening publishes");
            Call(o,"StartEventDiscussion",r);Publish(o,o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"));
            var expression=o.CurrentStorage.Documents.Single(x=>x.IsReadyForPublication && x.AuthorKingdomId=="a");
            Test.True(r.PlayerResponses.Any(x=>x.KingdomId=="c"&&x.SourceDocumentId==expression.DocumentId&&x.Status=="pending"),
                "published core expression creates a durable limited response obligation");
            Test.True(!(bool)Call(o,"ScheduleUnexpressedPosition",r),"limited answer cannot draft before court knowledge");
            Reload(o);r=o.ResolveRound(eventId);Know(o,"c",expression.DocumentId);
            if(loseAuthority)
            {
                h.UnavailableParty="c";Call(o,"ReconcileEventEligibility",r);
                Test.True(!(bool)Call(o,"ScheduleUnexpressedPosition",r)
                    && r.PlayerResponses.Single(x=>x.KingdomId=="c").Status=="unavailable",
                    "actual authority loss retires the original obligation without transferring it");
                // B has not expressed its position, so close protection must still apply.
                Test.True(!(bool)Call(o,"TryApplyEventProgress",r,new JObject { ["outcome"]="deferred",["reason"]="Authority changed",
                    ["positions"]=new JArray(Position("a","c"),Position("b")) },""),"other available core positions still protect closing");
                continue;
            }
            Call(o,"SchedulePlayerResponseWork",r);
            var reply=o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate");
            Test.True(reply.AuthorKingdomId=="c" && reply.IsExternalResponseOnly,"actual reply scheduler preserves limited role across reload");
            Publish(o,reply);
            Test.True(r.Participants.Single(x=>x.KingdomId=="c").State=="observer"
                && r.Participants.Single(x=>x.KingdomId=="a").AgendaAnswerDocumentIds.Count==1
                && o.CurrentStorage.CountryParticipation.ContainsKey("a") && !o.CurrentStorage.CountryParticipation.ContainsKey("c"),
                "limited published answer satisfies A's real opportunity without admitting/counting C");
            var answer=o.CurrentStorage.Documents.Single(x=>x.IsReadyForPublication && x.AuthorKingdomId=="c");
            Know(o,"b",answer.DocumentId);
            Test.True((bool)Call(o,"ScheduleUnexpressedPosition",r),"remaining B position schedules normally");
            Publish(o,o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"));
            var b=o.CurrentStorage.Documents.Single(x=>x.IsReadyForPublication && x.AuthorKingdomId=="b");
            Know(o,"a",b.DocumentId);
            Test.True((bool)Call(o,"ScheduleUnexpressedPosition",r),"B's core answer is still required");
            Publish(o,o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"));
            Test.True(r.EventConvergence.Ending || (bool)Call(o,"TryApplyEventProgress",r,new JObject { ["outcome"]="deferred",["reason"]="All positions answered; no agreement",
                ["positions"]=new JArray(Position("a","c"),Position("b")) },""),
                "case can close after actual core answers without inventing a position for observer C");
        }
    }
    private static void ParticipationOrder()
    {
        foreach(bool reverse in new[]{false,true})
        {
            var(_,o)=Owner();
            var records=new[]{("old-position",10),("same-a",30),("same-z",30)};
            if(reverse)Array.Reverse(records);
            foreach(var record in records)
            {
                var r=o.EnsureActiveRound("a","b",false);
                var a=r.Participants.Single(x=>x.KingdomId=="a");a.Agenda="Hearing";a.AgendaAddressTo=new(){"b"};
                a.AgendaExpressionDocumentId="";a.AgendaAnswerDocumentIds.Clear();
                var expression=new WorldDiplomacyDocument { DocumentId=record.Item1,RoundId=r.RoundId,AuthorKingdomId="a",TargetKingdomId="b",
                    Day=record.Item2,Body="We demand a hearing.",AuditedAgendaQuote="We demand a hearing.",IsReadyForPublication=true };
                o.AddDocument(expression);Call(o,"HandleEventPublication",expression);
                var answer=new WorldDiplomacyDocument { DocumentId="answer-"+record.Item1,RoundId=r.RoundId,AuthorKingdomId="b",
                    TargetKingdomId="a",Day=40,Body="We refuse the hearing.",AuditedAgendaAnswers=new(){expression.DocumentId},IsReadyForPublication=true };
                o.AddDocument(answer);Call(o,"HandleEventPublication",answer);Call(o,"HandleEventPublication",answer);
                Test.True(a.AgendaAnswerDocumentIds.Count==1,"duplicate publication retains one answer receipt");
                Reload(o);
                // Start a distinct event for the next independently answered expression.
                o.ResolveRound(r.RoundId).State="closed";
            }
            var last=o.CurrentStorage.CountryParticipation["a"];
            Test.True(last.LastExpressionDay==30&&last.LastDocumentId=="same-z","late/interleaved answers and same-day reorder preserve canonical latest opportunity");
            o.CurrentStorage.CountryParticipation["b"]=new(){LastExpressionDay=20};
            o.CurrentStorage.CountryParticipation["c"]=new(){LastExpressionDay=25};
            var selected=(List<string>)Call(o,"SelectMaterialInvestigations",(object)new[]{"a","b","c"});
            Test.True(selected.SequenceEqual(new[]{"b","c"}),"subsequent absence selection uses expression dates, not answer arrival");
            // Exercise old-schema replay through the same comparator; records are already audited and published.
            o.CurrentStorage.MaterialCountrySchemaVersion=0;Reload(o);
            Test.True(o.CurrentStorage.CountryParticipation["a"].LastDocumentId=="same-z","legacy replay cannot regress or change same-day ordering");
        }
    }
    private static void ActorNames()
    {
        var cases=new (string text,string[] ids,bool pass)[] {
            ("The ordinary lord reported a robbery at the hearing.",new[]{"ordinary_lord"},true),
            ("The ordinary lord will rob the caravan if ignored.",new[]{"ordinary_lord"},true),
            ("The ordinary lord presented a rose at the hearing.",new[]{"ordinary_lord"},true),
            ("Anna reported the forgery.",new[]{"anna"},true),
            ("ANNA reported the forgery.",new[]{"anna"},true),
            ("罗伯特在听证会上提交了证词。",new[]{"cn"},true),
            ("Rob submitted false testimony.",new[]{"rob"},true),
            ("Rob submitted false testimony.",new[]{"rob2"},true),
            ("Émile submitted testimony.",new[]{"accent"},true),
            ("The ordinary lord disputed Rob's testimony.",new[]{"ordinary_lord"},false),
            ("Ann and Anna submitted testimony.",new[]{"anna"},false),
            ("罗伯特在听证会上提交了证词。",new[]{"ordinary_lord"},false),
            ("Émile submitted testimony.",new[]{"ordinary_lord"},false),
            ("The ordinary lord testified.",new[]{"invented-id"},false)
        };
        foreach(var test in cases)
        {
            var(_,o)=Owner();var r=o.EnsureActiveRound("a","b",false);var concept=Concept();
            var fact=new JObject { ["kind"]="adopted_npc_story",["subject"]="a",["source"]="lore:test",
                ["text"]=test.text,["hero_ids"]=new JArray(test.ids) };
            concept["narrative"]=new JArray(fact);
            string[] actual = test.text=="The ordinary lord disputed Rob's testimony." ? new[]{"ordinary_lord","rob"}
                : test.text=="Ann and Anna submitted testimony." ? new[]{"ann","anna"}
                : test.text=="罗伯特在听证会上提交了证词。" ? new[]{"cn"}
                : test.text=="Émile submitted testimony." ? new[]{"accent"} : test.ids;
            Test.True(MaterialCountryThirdReplay.ReviewedConcept(o,r,new(){"a","b","c"},Sources,concept,actual)==test.pass,
                "concept actor validation via independent meaning audit: "+test.text);
            foreach(string stage in new[]{"started","progress:1","ended"})
            {
                var(_,boundaryOwner)=Owner();var round=boundaryOwner.EnsureActiveRound("a","b",false);
                round.EventBackground="The testimony is disputed.";
                foreach(var p in round.Participants){p.Agenda="Review";p.PreferredOutcome="Recognition";p.ExpectedCost="Concession";p.RedLine="No confession";p.Alternative="Unresolved";}
                var draft=Boundary();draft["narrative"]=new JArray(fact.DeepClone());
                if(stage.StartsWith("progress"))
                {
                    boundaryOwner.AddDocument(new(){DocumentId="evidence",RoundId=round.RoundId,IsReadyForPublication=true});
                    Know(boundaryOwner,"a","evidence");Know(boundaryOwner,"b","evidence");
                    draft["progress_evidence"]=new JArray("evidence");
                }
                bool accepted=MaterialCountryThirdReplay.ReviewedBoundary(boundaryOwner,round,stage,draft,actual);
                Test.True(accepted==test.pass,"boundary actor validation "+stage+": "+test.text);
                if(!accepted)continue;
                Reload(boundaryOwner);
                var notice=boundaryOwner.CurrentStorage.EventAnnouncements.Single();
                Test.True(notice.Narrative.Single().HeroIds.SequenceEqual(test.ids),"only verified actor identities persist");
                Test.True(WorldDiplomacyMemoryApplication.Build(new(boundaryOwner.CurrentStorage,"a",new(),test.ids[0]),test.text,d=>d.ToString()).Contains(test.text),
                    "accepted original-language personal narrative reaches actual memory reader");
            }
        }
    }
    private static void PackageConsumers()
    {
        var(_,o)=Owner();var r=o.EnsureActiveRound("a","b",false);
        var concept=Concept(limited:true);
        concept["narrative"]=new JArray(new JObject { ["kind"]="adopted_npc_story",["subject"]="a",["source"]="lore:test",
            ["text"]="The ordinary lord reported a robbery at the hearing.",["hero_ids"]=new JArray("ordinary_lord") });
        var job=new WorldDiplomacyJob { JobId="repair-concept",Kind="event_concept",RoundId=r.RoundId,
            AuthorKingdomId="a",CandidateKingdomIds=new(){"a","b","c"},EventSelectionStage="expand",
            EventMaterialSnapshot=Sources,EventSelectedCandidate="{}",EventComparison="{}" };
        o.CurrentStorage.Jobs.Add(job);
        void Complete(WorldDiplomacyJob work,JObject output)
        {
            Test.True(o.TryCompleteDiplomacyPipeline(work,new LlmJobResult { Success=true,Content=output.ToString() }),
                "production pipeline consumes "+work.Kind);
        }
        var package=JObject.Parse(File.ReadAllText(AnimusForgeModulePaths.GetModuleDataFilePath("DiplomacyPromptTexts.json")));
        r.PlayerResponses.Add(new(){KingdomId="c",SourceDocumentId="existing_limited_question"});
        CountryAdmissionReplay.FreezeFixture(o,job,new[]{"a","b"});
        foreach (string ui in new[]{"简体中文","English"})
        {
            Test.True(TaleWorlds.Localization.MBTextManager.ChangeLanguage(ui),"real UI language switches independently");
            o.PrepareSharedRequest(job);
            Test.True(job.SystemPrompt.Contains((string)package["EventConceptWriter"]!)
                && job.SystemPrompt.Contains((string)package["CountryAdmissionContract"]!),"selected package writer reaches request under "+ui);
            Test.True(JObject.Parse(job.UserPrompt)["narrative_actors"]!["rob"]!.Value<string>()=="Rob",
                "writer receives real actor identity registry");
        }
        Complete(job,concept);o.PrepareSharedRequest(job);
        Test.True(job.SystemPrompt.Contains((string)package["EventConceptAudit"]!)
            && JObject.Parse(job.UserPrompt)["proposed_event"]!.ToString().Contains("robbery"),
            "actual package audit sees unchanged prose and limited-response design");
        Complete(job,MaterialCountryThirdReplay.Audit(job,new[]{"ordinary_lord"}));
        Test.True(r.EventPhase=="opening","concept writer and independent audit reach adoption");
        var opening=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_boundary");
        o.PrepareSharedRequest(opening);
        Test.True(opening.SystemPrompt.Contains((string)package["EventBoundaryWriter"]!),"actual package boundary writer consumed");
        Complete(opening,Boundary());o.PrepareSharedRequest(opening);
        Test.True(opening.SystemPrompt.Contains((string)package["EventBoundaryAudit"]!),"actual package boundary audit consumed");
        Complete(opening,MaterialCountryThirdReplay.Approved(opening));
        Test.True(o.CurrentStorage.EventAnnouncements.Single().Narrative.Single().HeroIds.SequenceEqual(new[]{"ordinary_lord"}),
            "real audited opening preserves actor association despite unrelated Rob");
        Publish(o,o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"));
        var expression=o.CurrentStorage.Documents.Single(x=>x.IsReadyForPublication);
        Know(o,"c",expression.DocumentId);Call(o,"SchedulePlayerResponseWork",r);
        Publish(o,o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"));
        Reload(o);
        Test.True(WorldDiplomacyMemoryApplication.Build(new(o.CurrentStorage,"a",new(),"ordinary_lord"),"robbery",d=>d.ToString()).Contains("reported a robbery"),
            "actual package concept/audit/generation/publication/reload/memory chain");
        Console.WriteLine("Repair2 actual package="+(SmokeLocalizationEnvironment.EnglishPackage?"EN":"CN")
            +" / UI=Chinese+English; deterministic model responses only.");
    }
}
