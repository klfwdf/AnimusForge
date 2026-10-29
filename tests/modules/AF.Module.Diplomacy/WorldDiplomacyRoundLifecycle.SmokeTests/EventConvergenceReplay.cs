using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class EventConvergenceReplay
{
    private static object Call(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args)!;
    private sealed class Host : EventStructureReplay.Host, IDiplomacyConvergenceHost, IWorldDiplomacyThreatMechanicsHost
    {
        internal int Tendency = 50, Draw = -1, Draws;
        internal readonly List<string> Logs = new();
        internal readonly LiveDiplomacyFixReplay.Prestige Standing = new();
        public int WarTendency() => Tendency;
        public int DrawEndingRoute(int max) { Draws++; return Draw < 0 ? max - 1 : Draw % max; }
        public override void Log(string text) => Logs.Add(text);
        public override string ValidateOpenThreatWorldEligibility(WorldDiplomacyThreat t) =>
            t.IssuerKingdomId == UnavailableParty || t.TargetKingdomId == UnavailableParty ? "lost_authority" : "";
        public IWorldDiplomacyPrestigePort Prestige() => Standing;
        public IWorldDiplomacyThreatBindingPort ThreatBinding() => new Binding(this);
        public override IWorldDiplomacyThreatSettlementPort ThreatSettlement() => new ThreatSettlementReplay.Port(Owner.CurrentStorage);
    }
    private sealed class Binding : IWorldDiplomacyThreatBindingPort
    {
        private readonly Host h;
        internal Binding(Host host) { h = host; }
        public WorldDiplomacyRound ResolveRound(string id) => h.Owner.ResolveRound(id);
        public bool IsPolicyActive(string p, string o, string a) => false;
        public string RepresentativeId(string id) => id;
        public int CurrentDay() => h.CurrentDayValue;
        public string NewId(string kind) => h.NewId(kind);
        public int EscalationPrestigeReward => 3;
        public int WarPrestigeReward => 5;
        public void Log(string text) => h.Log(text);
    }
    private static (Host h, WorldDiplomacyOrchestration o, WorldDiplomacyRound r) Fixture(string first = "a", string second = "b")
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true, OralRange = (0, 3000) };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        var r = o.EnsureActiveRound(first, second, false); r.EventPhase = "discussing";
        foreach (var p in r.Participants) p.State = "active";
        r.EventBeginningId = r.RoundId + ":started";
        o.CurrentStorage.EventAnnouncements.Add(new() { Id = r.EventBeginningId, EventId = r.RoundId, Stage = "started", State = "public", Title = "Dispute", Body = "The dispute is unresolved." });
        return (h, o, r);
    }
    private static WorldDiplomacyDocument Publish(WorldDiplomacyOrchestration o, WorldDiplomacyRound r,
        string id, string author, string target, string source = "", string intent = "statement", bool changed = false,
        bool response = false, bool settle = true, string body = "Our position remains unchanged.", bool agenda = false)
    {
        var d = new WorldDiplomacyDocument { DocumentId = id, RoundId = r.RoundId, AuthorKingdomId = author,
            TargetKingdomId = target, AddressedKingdomIds = new() { target }, SourceDocumentId = source,
            IsPlayerAuthored = author == "p", AnalysisStatus = "success", IsReadyForPublication = true,
            Intent = intent, ChangedDiplomaticState = changed, RequiresResponse = response,
            CreatedUtcTicks = o.CurrentStorage.Documents.Count + 1, Day = 1, Title = "Disputed interests", Body = body,
            Commitment = WorldDiplomacyIntentVocabulary.DefaultCommitmentForIntent(intent) };
        if (source.Length > 0) d.AuditedAgendaAnswers.Add(source);
        if (agenda) d.AuditedAgendaQuote = body;
        if (source.Length > 0 && d.IsPlayerAuthored) d.AnsweredNpcDocumentIds.Add(source);
        foreach (var t in o.CurrentStorage.DiplomaticThreats.Where(x => x.StageRoundId == r.RoundId && x.Status == "open"))
        {
            if (t.TargetKingdomId == author) { d.PresentedThreatDocumentIds.Add(t.StageDocumentId); if (intent == "comply_ultimatum") d.RespondingToThreatDocumentId = t.StageDocumentId; }
            if (t.IssuerKingdomId == author && t.TargetDecision == "noncomplied") d.PresentedThreatFollowThroughDocumentIds.Add(t.StageDocumentId);
        }
        o.AddDocument(d);
        foreach (string k in new[] { "a", "b", "c", "p" }) WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge, k, id, 1);
        if (settle) { o.ProcessDiplomaticThreatDocument(d, author, target); o.SettleThreatDeclaration(d); }
        o.HandleRoundDocumentProcessed(d);
        return d;
    }
    private static void Advance(WorldDiplomacyOrchestration o, WorldDiplomacyRound r) => Call(o, "AdvanceEventEnding", r);
    private static void ClearCompletedGeneration(WorldDiplomacyOrchestration o, string author)
    { foreach (var j in o.CurrentStorage.Jobs.Where(x => x.AuthorKingdomId == author && x.Kind == "generate").ToList()) o.RemoveJob(j.JobId); }
    internal static void Run()
    {
        PlayerThreatReplyRepair();
        WithdrawnThreatAuthor(false); WithdrawnThreatAuthor(true);
        Dependencies(); ExecutionRecovery(); ExecutionRecovery(true); RecoveryGuards();
        UnexpressedCoreAgenda();
        Counter(); MultiCountry(); Weights(); Deadlock(); Threat(false, false); Threat(true, false); Threat(true, true);
        WarningRetreat(); ExistingUltimatum(); Agreement(false); Agreement(true); AgreementFailure(); Unavailable();
        SelectedAgreement(); PlayerWaiting(); NecessaryAnswers(); Compliance(); FinalGuards(); EndingRegistration(); LegacyClosing(); HistoricalResistance();
        Test.True(DiplomacyPromptTexts.Get("EventEndingContract", "").Contains("response_requests"), "real CN/EN reader loads ending contract");
        Console.WriteLine("Convergence: source pairs, publication, scheduler, three routes, execution failures and save recovery PASS; model/live NOT_RUN.");
    }
    private static void WithdrawnThreatAuthor(bool war)
    {
        var (h,o,r)=Fixture();
        Publish(o,r,"withdrawn-ultimatum","a","b",intent:"ultimatum");
        Publish(o,r,"withdrawn-refusal","b","a","withdrawn-ultimatum");
        r.Participants.Single(x=>x.KingdomId=="a").State="withdrawn";
        Call(o,"BeginEventEnding",r,""); Advance(o,r);
        var job=o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate" && x.AuthorKingdomId=="a");
        var draft=JObject.Parse("{\"title\":\"Final response\",\"body\":\"We withdraw our demand.\",\"actions\":[],\"response_requests\":[],\"round_participation\":\"withdraw\",\"round_status\":\"continue\"}");
        draft["actions"]=new JArray(new JObject { ["action_id"]="action_1", ["target_kingdom_id"]="b",
            ["intent"]=war?"declare_war":"statement", ["commitment"]=war?"binding":"non_binding", ["negotiation_move"]="state_position",
            ["responding_to_threat_document_id"]=war?"withdrawn-ultimatum":"" });
        WorldDiplomacyEnvelopeJsonRules.NormalizeGeneratedDiplomaticEnvelopeShape(job,draft);
        o.TryGetGeneratedIntentLegalityViolation(job,draft,"a","b",out _,out string reason);
        Test.True(string.IsNullOrEmpty(reason),
            "queued final threat obligation remains eligible after ordinary participation withdrawal: "+reason);
        var foreignDraft=(JObject)draft.DeepClone();
        ((JArray)foreignDraft["actions"]!).Add(new JObject { ["action_id"]="action_2", ["target_kingdom_id"]="c", ["intent"]="statement" });
        Test.True(o.TryGetGeneratedIntentLegalityViolation(job,foreignDraft,"a","b",out _,out reason)
            && reason=="author_is_not_current_event_participant","bound duty cannot authorize a second unrelated country action");
        bool Bound() => (bool)Call(o,"HasBoundEndingJob",r,job,"a");
        Test.True(Bound(),"real queued final decision binds original threat refusal");
        job.TargetKingdomId="c"; Test.True(!Bound(),"different counterparty cannot inherit ending authority"); job.TargetKingdomId="b";
        job.SourceDocumentId="withdrawn-ultimatum"; Test.True(!Bound(),"issuer must answer actual refusal, not its own ultimatum"); job.SourceDocumentId="withdrawn-refusal";
        var threat=o.CurrentStorage.DiplomaticThreats.Single();
        threat.Status="breached"; Test.True(!Bound(),"settled duty grants no further speech"); threat.Status="open";
        h.UnavailableParty="a"; Test.True(!Bound(),"lost sovereign authority still blocks final decision"); h.UnavailableParty="";
        r.Participants.Single(x=>x.KingdomId=="a").State="unavailable";
        Test.True(!Bound(),"unavailable participant cannot be revived as an ending actor");
        r.Participants.Single(x=>x.KingdomId=="a").State="withdrawn";
        r.EventConvergence.Ending=false; Test.True(!Bound(),"ordinary conversation cannot use ending exception"); r.EventConvergence.Ending=true;
        job.SuspendedReason="author_is_not_current_event_participant"; job.TechnicalRecoveryAttempts=3;
        job.RecoveryConfiguration=(string)Call(o,"SpeechConfiguration"); job.RecoveryRevision=r.ConversationRevision;
        job.RecoveryKnowledgeRevision=DiplomacyEventRules.KnowledgeRevision(r,job.AuthorKingdomId);
        job.TargetKingdomId="c";
        Call(o,"TryResumeTechnicalRequest",job,false);
        Test.True(job.SuspendedReason!="" && job.TechnicalRecoveryAttempts==3,"unbound exhausted hold does not receive a fresh retry budget");
        job.TargetKingdomId="b"; job.NextRecoveryProbeUtcTicks=0;
        Call(o,"TryResumeTechnicalRequest",job,false);
        Test.True(job.SuspendedReason=="" && job.TechnicalRecoveryAttempts==0,
            "old exhausted participation hold resumes only for a verified original ending duty");
        o.PrepareSharedRequest(job);
        job.PrivacyApproved=true;
        o.CommitGeneratedDocument(job,draft.ToString());
        Test.True(o.CurrentStorage.Documents.Any(x=>x.DocumentId==job.DocumentId && x.IsReadyForPublication)
            && threat.Status==(war?"enforced":"breached"),"real commit publishes the final decision and settles the existing ultimatum: "
                +job.DocumentId+" / "+o.ResolveDocument(job.DocumentId)?.IsReadyForPublication+" / "+threat.Status);
        int documentCount=o.CurrentStorage.Documents.Count;
        o.CommitGeneratedDocument(job,draft.ToString());
        Test.True(o.CurrentStorage.Documents.Count==documentCount,"duplicate final completion publishes no second document");
    }
    private static void UnexpressedCoreAgenda()
    {
        var (h,o,r) = Fixture();
        r.Participants.Add(new() { KingdomId="c", State="active", Agenda="Return the captive before opening trade", AgendaAddressTo=new(){"a"} });
        TwoRounds(o,r);
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
        r=o.ResolveRound(r.RoundId); Advance(o,r);
        Test.True(r.EventPhase != "closing" && o.CurrentStorage.Jobs.Any(x=>x.AuthorKingdomId=="c" && x.Kind=="generate"),
            "third admitted country retains its first core expression after two rounds and reload");
        ClearCompletedGeneration(o,"c");
        Publish(o,r,"c-core","c","a","r2",body:"Return the captive before opening trade.",agenda:true);
        Advance(o,r);
        Test.True(r.EventConvergence.RequiredSources.Contains("c-core") && o.CurrentStorage.Jobs.Any(x=>x.AuthorKingdomId=="a" && x.SourceDocumentId=="c-core"),
            "core expression becomes a finite required answer, not a new ordinary round");
        ClearCompletedGeneration(o,"a"); Publish(o,r,"c-answer","a","c","c-core"); Advance(o,r);
        Test.True(r.EventPhase=="closing" && r.EventConvergence.CompletedRounds==2 && h.Draws==1,
            "necessary answer completes without an extra answer-to-answer or route reroll");
        var (lostHost,lostOwner,lostRound)=Fixture();
        lostRound.Participants.Add(new(){KingdomId="c",State="active",Agenda="A claim",AgendaAddressTo=new(){"a"}});
        TwoRounds(lostOwner,lostRound); lostHost.UnavailableParty="c"; Advance(lostOwner,lostRound);
        Test.True(lostRound.EventPhase=="closing","loss of sovereign authority cannot permanently block closure on an unspeakable agenda");
        var (_,oldOwner,oldRound)=Fixture(); TwoRounds(oldOwner,oldRound);
        oldRound.Participants.Add(new(){KingdomId="c",State="active",Agenda="A previously admitted claim",AgendaAddressTo=new(){"a"}});
        oldRound.EventPhase="closing";
        oldOwner.CurrentStorage.Jobs.Add(new(){JobId="obsolete-ending",RoundId=oldRound.RoundId,Kind="event_boundary",ForcedIntent="ended"});
        Advance(oldOwner,oldRound);
        Test.True(oldRound.EventPhase=="discussing" && oldOwner.CurrentStorage.Jobs.All(x=>x.JobId!="obsolete-ending")
            && oldOwner.CurrentStorage.Jobs.Any(x=>x.AuthorKingdomId=="c"),"old un-published closure resumes its missed core position without retaining a stale ending job");
    }
    private static JObject RecoveryDraft(WorldDiplomacyDocument d) => new JObject {
        ["title"] = d.Title, ["body"] = d.Body, ["requires_response"] = false, ["response_requests"] = new JArray(),
        ["round_participation"] = "continue", ["round_status"] = "continue", ["tone"] = "hostile",
        ["answered_player_document_ids"] = new JArray(),
        ["actions"] = new JArray(new JObject { ["target_kingdom_id"] = d.TargetKingdomId, ["intent"] = "declare_war",
            ["commitment"] = "binding", ["dependency_action_ids"] = new JArray(),
            ["responding_to_threat_document_id"] = d.Actions[0].RespondingToThreatDocumentId,
            ["responding_to_threat_action_id"] = d.Actions[0].RespondingToThreatActionId }) };

    private static void Dependencies()
    {
        JArray Rows(params string[][] dependencies) => new JArray(dependencies.Select(x => new JObject {
            ["target_kingdom_id"]="b", ["intent"]="declare_war", ["commitment"]="binding", ["dependency_action_ids"]=new JArray(x) }));
        foreach(var bad in new[]{ Rows(new[]{"action_1"}), Rows(new[]{"action_2"},new[]{"action_1"}),
            Rows(new[]{"action_99"}), Rows(new[]{"missing#action_1"}) })
        {
            Test.True(!DiplomacyConditionalExecutionRules.Validate(bad,_=>null,out var why)&&why.StartsWith("dependency_"),"invalid dependency graph rejected before effects: "+why);
            Test.True(WorldDiplomacyGenerationValidationRules.TryGetGeneratedIntentLegalityViolation(new(),new(){["actions"]=bad},
                (_,_) => (false,"b",""),_=>null,out _,out why),"real shared admission rejects graph even if action is legal");
        }
        var source=new WorldDiplomacyDocument{DocumentId="source",IsReadyForPublication=true,Actions=new(){new(){ActionId="action_1"}}};
        Test.True(DiplomacyConditionalExecutionRules.Validate(Rows(new[]{"action_2"},Array.Empty<string>()),_=>null,out _),"acyclic reciprocal prerequisites remain represented");
        Test.True(DiplomacyConditionalExecutionRules.Validate(Rows(new[]{"source#action_1"}),_=>source,out _),"exact published external action accepted");
        source.IsReadyForPublication=false;
        Test.True(!DiplomacyConditionalExecutionRules.Validate(Rows(new[]{"source#action_1"}),_=>source,out _),"unpublished source cannot authorize dependency");
        var(h,o,r)=Fixture();var doc=Publish(o,r,"source","a","b");
        var j=new WorldDiplomacyJob{JobId="bad-draft",Kind="generate",RoundId=r.RoundId,AuthorKingdomId="a",TargetKingdomId="b",
            PrivacySourceMaterial="public context",SystemPrompt="rules"};
        o.CurrentStorage.Jobs.Add(j);
        o.CommitGeneratedDocument(j,new JObject{["title"]="Declaration",["body"]="We declare war.",["actions"]=Rows(new[]{"action_1"})}.ToString());
        Test.True(j.Kind=="generate"&&j.PrivacyRepairFeedback.Contains("dependency_self_reference")&&h.Effects==0
            &&o.CurrentStorage.Documents.Count==1,"real pre-audit pipeline sends self dependency to repair without publishing");
        doc.Actions=new(){new(){ActionId="action_1",Intent="ultimatum",TargetKingdomId="b"}};
        var actual=Rows(new[]{"source#action_1"});
        Test.True(!DiplomacyConditionalExecutionRules.Validate(actual,_=>doc,out var threatError)
            && threatError.StartsWith("dependency_threat_is_context_not_prerequisite"),"live external-ultimatum dependency is rejected, not just self-reference");
        o.CommitGeneratedDocument(j,new JObject{["title"]="Decision on the ultimatum",["body"]="We declare war.",["actions"]=actual}.ToString());
        Test.True(j.PrivacyRepairFeedback.Contains("dependency_threat_is_context_not_prerequisite") && h.Effects==0
            && o.CurrentStorage.Documents.Count==1,"live failure shape enters repair before false war publication or effects");
        var corrected=Rows(Array.Empty<string>()); corrected[0]["responding_to_threat_document_id"]="source";
        corrected[0]["responding_to_threat_action_id"]="action_1";
        Test.True(DiplomacyConditionalExecutionRules.Validate(corrected,_=>doc,out _)
            && DiplomacyConditionalExecutionRules.ValidateImmediateWar(corrected,out _),"a correctly bound unconditional war is eligible rather than deferred");
        doc.Actions[0].Intent="propose_trade";
        Test.True(DiplomacyConditionalExecutionRules.Validate(actual,_=>doc,out _)
            && !DiplomacyConditionalExecutionRules.ValidateImmediateWar(actual,out _),"a real conditional war must be reworded before publication, not forcibly made unconditional");
    }

    private static WorldDiplomacyDocument HeldWar(WorldDiplomacyOrchestration o, WorldDiplomacyRound r)
    {
        // Earlier invalid self-dependency receipt; the external ultimatum case
        // is covered separately below using its exact published source action.
        Publish(o,r,"original-ultimatum","a","b",intent:"ultimatum");
        Publish(o,r,"refusal","b","a","original-ultimatum");
        var d=new WorldDiplomacyDocument{DocumentId="failed-war",RoundId=r.RoundId,AuthorKingdomId="a",TargetKingdomId="b",
            SourceDocumentId="refusal",Title="Declaration of war",Body="The ultimatum was rejected. We declare war.",
            PresentedThreatFollowThroughDocumentIds=new(){"original-ultimatum"},
            Actions=new(){new(){ActionId="action_1",Intent="declare_war",Commitment="binding",TargetKingdomId="b",
                DependencyActionIds=new(){"action_1"},RespondingToThreatDocumentId="original-ultimatum"}}};
        o.AddDocument(d);
        // Persisted old-version failure receipt. The current executor rejects
        // this shape before publication, so migration must start from saved data.
        d.Intent="declare_war"; d.Commitment="binding"; d.IsReadyForPublication=true;
        d.Actions[0].ExecutionState="deferred_conditions";
        o.HandleRoundDocumentProcessed(d);
        foreach(string k in new[]{"a","b"})WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(o.CurrentStorage.KingdomKnowledge,k,d.DocumentId,1);
        return d;
    }

    private static void ExecutionRecovery(bool externalUltimatum = false)
    {
        var(h,o,r)=Fixture();var original=HeldWar(o,r);string body=original.Body;
        if (externalUltimatum)
        {
            o.ResolveDocument("original-ultimatum").Actions = new() { new() { ActionId="action_1", Intent="ultimatum", TargetKingdomId="b" } };
            original.Actions[0].DependencyActionIds = new() { "original-ultimatum#action_1" };
            original.Actions[0].RespondingToThreatActionId = "action_1";
        }
        string originalDependency = original.Actions[0].DependencyActionIds.Single();
        Test.True(original.Actions[0].ExecutionState=="deferred_conditions"&&h.Effects==0&&r.EventConvergence.TechnicalFailure.StartsWith("war_execution_failed"),"persisted old nonexecution receipt restores its hold");
        Advance(o,r);var job=o.CurrentStorage.Jobs.Single(x=>x.EndingRecoveryDocumentId==original.DocumentId);
        Test.True((bool)Call(o,"CanDispatchPlayerResponse",job),"recovery of own published artifact is dispatchable without inventing a reply request");
        Test.True(r.EventConvergence.ExecutionRecoveryAttempts==1&&h.Draws==0,"held receipt queues original obligation once without reroll");
        for(int i=0;i<3;i++){r.EventConvergence.NextExecutionRecoveryUtcTicks=0;Advance(o,r);}
        Test.True(o.CurrentStorage.Jobs.Count(x=>x.EndingRecoveryDocumentId==original.DocumentId)==1&&r.EventConvergence.ExecutionRecoveryAttempts==1,"repeated scheduler passes never duplicate recovery job");
        string saved=JsonConvert.SerializeObject(o.CurrentStorage);o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(saved)!);
        r=o.ResolveRound(r.RoundId);original=o.ResolveDocument(original.DocumentId);job=o.CurrentStorage.Jobs.Single(x=>x.EndingRecoveryDocumentId==original.DocumentId);
        Test.True(r.EventConvergence.ExecutionRecoveryAttempts==1,"retry budget persists across reload");
        o.PrepareSharedRequest(job);
        o.TryCompleteDiplomacyPipeline(job,new(){Success=true,Content=new JObject{["safe"]=true,["public_context"]=job.UserPrompt,["attitude_summary"]=""}.ToString()});
        o.PrepareSharedRequest(job);
        Test.True(job.UserPrompt.Contains(body)&&job.UserPrompt.Contains("recovery_consistent"),"real recovery request carries immutable original and bilingual recovery contract");
        var draft=RecoveryDraft(original);o.CommitGeneratedDocument(job,draft.ToString());
        Test.True(job.Kind=="privacy_audit"&&job.UserPrompt.Contains("recovery_consistent"),"corrected artifact still requires independent review");
        o.TryCompleteDiplomacyPipeline(job,new(){Success=true,Content=new JObject{["safe"]=true,["position_owner_valid"]=true,
            ["negotiation_consistent"]=true,["recovery_consistent"]=true,["agenda_quote"]="",["agenda_answers"]=new JArray()}.ToString()});
        Test.True(h.Effects==1,"corrected declaration executes exactly once through original action owner: "+job.Kind+" "+job.SuspendedReason+" "+job.PrivacyRepairFeedback+" "+string.Join(";",h.Logs.TakeLast(4)));
        Test.True(original.Body==body&&!original.ChangedDiplomaticState&&original.Actions[0].DependencyActionIds.Single()==originalDependency,"original failed artifact is preserved unchanged");
        var corrected=o.CurrentStorage.Documents.Single(x=>x.EndingRecoveryDocumentId==original.DocumentId);
        Test.True(corrected.ChangedDiplomaticState&&corrected.Actions[0].ExecutionState=="applied"&&r.EventConvergence.TechnicalFailure==""
            &&r.EventConvergence.ActualWar,"new confirmed receipt releases only its own ending hold");
        h.CurrentDayValue+=10;o.ProcessPropagationArrivals();
        Advance(o,r);Test.True(r.EventPhase=="closing"&&h.Draws==0,"repaired threat settles and advances to ending without new political route: "+r.EventPhase+" "+string.Join(",",o.CurrentStorage.Jobs.Select(x=>x.Kind))+" "+string.Join(",",o.CurrentStorage.DiplomaticThreats.Select(x=>x.Status)));
        saved=JsonConvert.SerializeObject(o.CurrentStorage);o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(saved)!);r=o.ResolveRound(r.RoundId);Advance(o,r);
        Test.True(h.Effects==1,"reload after success never repeats effect");
    }

    private static void RecoveryGuards()
    {
        var (externalHost, externalOwner, externalRound) = Fixture();
        var externalWar = HeldWar(externalOwner, externalRound);
        externalOwner.ResolveDocument("original-ultimatum").Actions = new() { new() { ActionId="action_1", Intent="ultimatum", TargetKingdomId="b" } };
        externalWar.Actions[0].DependencyActionIds = new() { "original-ultimatum#action_1" };
        Advance(externalOwner, externalRound); Advance(externalOwner, externalRound);
        Test.True(externalOwner.CurrentStorage.Jobs.Count(x=>x.EndingRecoveryDocumentId==externalWar.DocumentId)==1
            && externalRound.EventConvergence.ExecutionRecoveryAttempts==1 && externalHost.Effects==0,
            "saved external-ultimatum dependency queues one audited repair without automatic war");
        foreach(string kind in new[]{"uncertain","legacy","partial","player","valid_condition","limit","owner_changed"})
        {
            var(h,o,r)=Fixture();var d=HeldWar(o,r);
            if(kind=="uncertain")d.Actions[0].ExecutionState="uncertain";
            if(kind=="legacy")d.Actions[0].ExecutionState="";
            if(kind=="partial")d.Actions.Add(new(){Intent="cancel_trade",ChangedDiplomaticState=true});
            if(kind=="player")d.IsPlayerAuthored=true;
            if(kind=="valid_condition")d.Actions[0].DependencyActionIds=new(){"failed-war#action_1"};
            if(kind=="limit")r.EventConvergence.ExecutionRecoveryAttempts=3;
            if(kind=="owner_changed")d.AuthorRulerId="former_ruler";
            Advance(o,r);
            Test.True(!o.CurrentStorage.Jobs.Any(x=>!string.IsNullOrEmpty(x.EndingRecoveryDocumentId))&&h.Effects==0
                &&r.EventConvergence.TechnicalFailure!="","unsafe recovery remains held: "+kind);
        }
        var(host,owner,round)=Fixture();var original=HeldWar(owner,round);Advance(owner,round);
        var job=owner.CurrentStorage.Jobs.Single(x=>x.EndingRecoveryDocumentId==original.DocumentId);
        var changed=RecoveryDraft(original);changed["body"]="We withdraw the threat.";
        object[] args={job,changed,""};var method=typeof(WorldDiplomacyOrchestration).GetMethod("ValidateEndingRecoveryDraft",BindingFlags.Instance|BindingFlags.NonPublic)!;
        Test.True(!(bool)method.Invoke(owner,args)!,"recovery cannot change public political decision");
        original.Actions[0].ExecutionState="uncertain";args=new object[]{job,RecoveryDraft(original),""};
        Test.True(!(bool)method.Invoke(owner,args)!,"receipt becoming uncertain during model work blocks late completion");
        foreach(string state in new[]{"not_attempted","not_applied"})
        {
            var(h,o,r)=Fixture();var d=HeldWar(o,r);d.Actions[0].DependencyActionIds.Clear();d.Actions[0].ExecutionState=state;
            Advance(o,r);Test.True(o.CurrentStorage.Jobs.Any(x=>x.EndingRecoveryDocumentId==d.DocumentId)&&h.Effects==0,
                "confirmed nonexecution queues audited repair rather than direct effect: "+state);
        }
        var(ah,ao,ar)=Fixture();Publish(ao,ar,"offer","a","b");
        var acceptance=Publish(ao,ar,"acceptance","b","a","offer","accept_trade");
        acceptance.Actions=new(){new(){ActionId="action_1",Intent="accept_trade",RespondingToOfferDocumentId="offer",RespondingToOfferActionId="action_1",ExecutionState="uncertain"}};
        ar.PendingOffers.Add(new(){SourceDocumentId="offer",SourceActionId="action_1",Status="partially_executed"});
        Call(ao,"HoldEndingExecution",ar,"agreement_execution_failed:acceptance");ar.EventConvergence.Ending=true;
        Advance(ao,ar);Test.True(ar.EventConvergence.TechnicalFailure!=""&&ah.Effects==0,"partial agreement cannot be retried by ending owner");
        ar.PendingOffers[0].Status="accepted";ar.EventConvergence.NextExecutionRecoveryUtcTicks=0;Advance(ao,ar);
        Test.True(ar.EventConvergence.TechnicalFailure!="","accepted status alone cannot clear unknown original action receipt");
        acceptance.Actions[0].ExecutionState="applied";acceptance.Actions[0].ChangedDiplomaticState=true;
        ar.EventConvergence.NextExecutionRecoveryUtcTicks=0;Advance(ao,ar);
        Test.True(ar.EventConvergence.TechnicalFailure==""&&ar.EventConvergence.Result=="action_executed"&&ah.Effects==0,
            "only exact offer and action receipts resolved by original owner release agreement hold without replay");
    }
    private static void Counter()
    {
        var (_, o, r) = Fixture("p", "b"); r.StartedDay = 20;
        var root = Publish(o, r, "player-before-case", "p", "b"); root.Day = 3;
        Test.True(r.EventConvergence.CompletedRounds == 0, "pre-event initiating declaration starts first claim without inventing answer");
        var reply = Publish(o, r, "first-reply", "b", "p", root.DocumentId);
        Test.True(r.EventConvergence.CompletedRounds == 1 && !r.EventConvergence.Ending, "player pre-creation claim and first NPC answer count round one");
        for (int i = 0; i < 8; i++) o.HandleRoundDocumentProcessed(reply);
        Test.True(r.EventConvergence.CompletedRounds == 1, "duplicate publication receipts do not count again");
        var draft = new WorldDiplomacyDocument { DocumentId = "draft", AuthorKingdomId = "p", SourceDocumentId = reply.DocumentId };
        DiplomacyConvergenceRules.Record(r.EventConvergence, draft, root.DocumentId, new[] { reply.DocumentId });
        Test.True(r.EventConvergence.Receipts.Count == 2, "unpublished rewrite and retry excluded");
        Publish(o, r, "followup", "p", "b", reply.DocumentId);
        Test.True(r.EventConvergence.CompletedRounds == 1, "second claim alone cannot fabricate completed second round");
        Publish(o, r, "second-reply", "b", "p", "followup");
        Test.True(r.EventConvergence.CompletedRounds == 2 && r.EventConvergence.Ending, "second corresponding answer enters ending at publication");
        var restored = JsonConvert.DeserializeObject<WorldDiplomacyRound>(JsonConvert.SerializeObject(r))!;
        Test.True(restored.EventConvergence.CompletedRounds == 2 && restored.EventConvergence.Ending, "round and phase survive round JSON save/load");
        var legacy = new DiplomacyEventConvergence();
        foreach (var d in o.CurrentStorage.Documents.OrderBy(x=>x.CreatedUtcTicks)) DiplomacyConvergenceRules.Record(legacy, d, root.DocumentId, new[] { d.SourceDocumentId });
        Test.True(legacy.CompletedRounds == 2, "old published source chain reconstructs without creation-time cutoff");
        int boundedCount=legacy.Receipts.Count;
        for(int i=0;i<100;i++) DiplomacyConvergenceRules.Record(legacy,new(){DocumentId="late-legacy-"+i,
            AuthorKingdomId="p",IsReadyForPublication=true},root.DocumentId,new[]{"second-reply"});
        Test.True(legacy.Receipts.Count==boundedCount,"legacy recovery stops collecting ordinary receipts after two completed pairs");
    }
    private static void MultiCountry()
    {
        var (_, o, r) = Fixture(); r.Participants.Add(new() { KingdomId = "c", State = "observer" });
        Publish(o,r,"root","a","b"); Publish(o,r,"b-answer","b","a","root");
        Publish(o,r,"c-answer","c","a","root");
        Test.True(r.EventConvergence.CompletedRounds == 1, "multiple countries answering same claim do not create second round");
        Publish(o,r,"b-repeat","b","a","root");
        Test.True(r.EventConvergence.CompletedRounds == 1, "new wording on same source never advances claim lineage");
        Publish(o,r,"second-claim","a","b","b-answer"); Publish(o,r,"second-answer","b","a","second-claim");
        int count = r.EventConvergence.Receipts.Count;
        Publish(o,r,"late-observer","c","a","second-answer");
        Test.True(r.EventConvergence.Receipts.Count == count && r.EventConvergence.CompletedRounds == 2, "late country and new claim cannot extend allowance");
    }
    private static void Weights()
    {
        foreach (int t in new[] { 0, 49, 50, 51, 100 })
        {
            var w = DiplomacyConvergenceRules.Weights(t,0,0,100,100,0,false,false,false,true,true);
            int expected = t < 50 ? 50 : t == 50 ? 60 : 70;
            Test.True(w.threat == expected && w.agreement == 90 - expected && w.deadlock == 10, "exact three tier base weights " + t);
            var counts = Enumerable.Range(0,100).GroupBy(x => DiplomacyConvergenceRules.Select(w,x)).ToDictionary(g=>g.Key,g=>g.Count());
            Test.True(counts["threat"] == expected && counts["agreement"] == 90-expected && counts["deadlock"] == 10, "exhaustive draw distribution " + t);
            foreach (int relation in new[]{-100,100}) foreach(int wars in new[]{0,20})
            { var adjusted=DiplomacyConvergenceRules.Weights(t,relation,relation,10000,1,wars,true,true,true,true,true);
              Test.True(Math.Abs(adjusted.threat-expected)<=12 && adjusted.threat+adjusted.agreement+adjusted.deadlock==100,"bounded modifiers preserve base dominance"); }
        }
        var none=DiplomacyConvergenceRules.Weights(100,-100,-100,1000,1,0,false,false,false,false,false);
        Test.True(none==(0,0,10)&&DiplomacyConvergenceRules.Select(none,9)=="deadlock","illegal routes excluded before draw");
    }
    private static void TwoRounds(WorldDiplomacyOrchestration o, WorldDiplomacyRound r)
    { Publish(o,r,"root","a","b");Publish(o,r,"r1","b","a","root");Publish(o,r,"c2","a","b","r1");Publish(o,r,"r2","b","a","c2"); }
    private static void Deadlock()
    {
        var(h,o,r)=Fixture();TwoRounds(o,r);
        o.CurrentStorage.Jobs.Add(new(){JobId="stale-progress",Kind="event_progress",RoundId=r.RoundId});
        o.TryCompleteDiplomacyPipeline(o.CurrentStorage.Jobs.Single(), new(){Success=true,Content="{\"outcome\":\"continue\"}"});
        Test.True(o.CurrentStorage.Jobs.Count==0,"late model continuation cannot override ending");
        Advance(o,r);Test.True(r.EventConvergence.Route=="deadlock"&&r.EventPhase=="closing"&&h.Draws==1,"stalemate directly schedules ending, no final speeches");
        Test.True(o.CurrentStorage.Jobs.All(x=>x.Kind=="event_boundary"),"boundary announcement is distinct from actual case closure");
        Test.True(r.State=="active","case not closed before ending publication");
        var saved=JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
        o.ReplaceStorage(saved);r=o.ResolveRound(r.RoundId);Advance(o,r);
        Test.True(h.Draws==1 && r.EventConvergence.Route=="deadlock","reload never redraws ending route");
        var job=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_boundary");o.PrepareSharedRequest(job);
        var boundary=new JObject{["title"]="Unresolved dispute",["body"]="The courts retain their positions; the exchange has ended.",
            ["effects"]=new JArray(),["remove_effects"]=new JArray(),["confirmed_documents"]=new JArray(),["narrative"]=new JArray()};
        o.TryCompleteDiplomacyPipeline(job,new(){Success=true,Content=boundary.ToString()});o.PrepareSharedRequest(job);
        o.TryCompleteDiplomacyPipeline(job,new(){Success=true,Content=MaterialCountryThirdReplay.Approved(job).ToString()});
        Test.True(r.State=="closed"&&o.CurrentStorage.EventAnnouncements.Any(x=>x.EventId==r.RoundId&&x.Stage=="ended"&&x.State=="public"),"audited neutral ending publishes and truly closes case");
    }
    private static void Threat(bool war, bool fail)
    {
        var(h,o,r)=Fixture();h.Draw=0;TwoRounds(o,r);Advance(o,r);
        Test.True(r.EventConvergence.Route=="threat"&&h.Draws==1,"program selects threat once");
        string a=r.EventConvergence.Actor,b=r.EventConvergence.Target;
        ClearCompletedGeneration(o,a);Publish(o,r,"warning",a,b,"r2","warning",response:true);
        var t=o.CurrentStorage.DiplomaticThreats.Single();
        Advance(o,r);Test.True(o.CurrentStorage.Jobs.Any(x=>x.AuthorKingdomId==b),"threat target actual response is queued");
        ClearCompletedGeneration(o,b);Publish(o,r,"reject-warning",b,a,"warning");Advance(o,r);
        ClearCompletedGeneration(o,a);Publish(o,r,"ultimatum",a,b,"reject-warning","ultimatum",response:true);
        Test.True(t.Stage=="ultimatum","warning refusal continues same threat to ultimatum");Advance(o,r);
        ClearCompletedGeneration(o,b);Publish(o,r,"reject-ultimatum",b,a,"ultimatum");Advance(o,r);
        ClearCompletedGeneration(o,a);Publish(o,r,"final",a,b,"reject-ultimatum",war?"declare_war":"statement",changed:war&&!fail);
        Advance(o,r);
        if(fail) Test.True(r.EventPhase!="closing"&&r.EventConvergence.TechnicalFailure.StartsWith("war_execution_failed")&&!t.ReputationPenaltyApplied,"failed war is technical hold, never retreat or success");
        else Test.True(r.EventPhase=="closing"&&(war?t.Status=="enforced":t.Status=="breached")&&r.EventConvergence.ActualWar==war,"threat settlement leads all the way to event ending");
        Test.True(h.Draws==1,"threat progression never redraws");
    }
    private static void WarningRetreat()
    {
        var(_,o,r)=Fixture();Publish(o,r,"w","a","b",intent:"warning");Publish(o,r,"no","b","a","w");
        Publish(o,r,"retreat","a","b","no");Advance(o,r);
        Test.True(r.EventConvergence.Ending&&r.EventPhase=="closing"&&o.CurrentStorage.DiplomaticThreats.Single().ReputationPenaltyAmount==10,"early warning retreat settles once and ends before two rounds");
        Publish(o,r,"restart","a","b","retreat","warning");
        Test.True(o.CurrentStorage.DiplomaticThreats.Count==1,"settled threat cannot reopen same dispute");
    }
    private static void ExistingUltimatum()
    {
        var(h,o,r)=Fixture();Publish(o,r,"u","a","b",intent:"ultimatum");Publish(o,r,"no","b","a","u");
        Call(o,"BeginEventEnding",r,"");Advance(o,r);
        Test.True(h.Draws==0&&r.EventConvergence.Route=="existing_threat"&&o.BuildLegalDiplomaticActionIntents(r,"a","b").Contains("declare_war")
            &&!o.BuildLegalDiplomaticActionIntents(r,"a","b").Contains("warning"),"existing ultimatum continues original stage without warning restart or draw");
    }
    private static void Agreement(bool accept)
    {
        var(h,o,r)=Fixture();Publish(o,r,"proposal","a","b",intent:"propose_trade");
        r.PendingOffers.Add(new(){SourceDocumentId="proposal",SourceActionId="a1",ProposerKingdomId="a",TargetKingdomId="b",Intent="propose_trade",Status="open"});
        Call(o,"BeginEventEnding",r,"");Advance(o,r);ClearCompletedGeneration(o,"b");
        var offer=r.PendingOffers.Single();offer.Status=accept?"accepted":"rejected";
        Publish(o,r,"answer","b","a","proposal",accept?"accept_trade":"reject_trade",changed:accept);Advance(o,r);
        Test.True(h.Draws==0&&r.EventPhase=="closing"&&r.EventConvergence.Result==(accept?"action_executed":"agreement_not_reached"),"existing agreement "+(accept?"execution":"refusal")+" ends without redraw");
    }
    private static void AgreementFailure()
    {
        var(_,o,r)=Fixture();Publish(o,r,"root","a","b");r.PendingOffers.Add(new(){Status="partially_executed"});
        Publish(o,r,"failed","b","a","root","accept_trade",changed:true);Advance(o,r);
        Test.True(r.EventPhase!="closing"&&r.EventConvergence.TechnicalFailure.StartsWith("agreement_execution_failed"),"partial agreement execution cannot be called successful closure");
    }
    private static void Unavailable()
    {
        var(h,o,r)=Fixture();Publish(o,r,"u","a","b",intent:"ultimatum");Call(o,"BeginEventEnding",r,"");
        h.UnavailableParty="b";Call(o,"ReconcileEventEligibility",r);Advance(o,r);
        Test.True(o.CurrentStorage.DiplomaticThreats.Single().Status=="invalidated"&&r.EventPhase=="closing","lost authority retires existing threat without extra country turns");
    }

    private static void SelectedAgreement()
    {
        var(h,o,r)=Fixture();r.Participants[0].Agenda="Reopen the caravan trade route";TwoRounds(o,r);
        // Choose the first agreement bucket after modifiers (without a model selecting it).
        h.Draw=60;Advance(o,r);
        Test.True(r.EventConvergence.Route=="agreement"&&r.EventConvergence.Intent=="propose_alliance","common enemy prefers legal alliance within agreement route");
        string actor=r.EventConvergence.Actor,target=r.EventConvergence.Target;
        var allowed=o.BuildLegalDiplomaticActionIntents(r,actor,target);
        Test.True(allowed.Contains("propose_alliance")&&!allowed.Contains("warning")&&!allowed.Contains("ultimatum")&&!allowed.Contains("propose_trade"),"chosen proposal type constrains actual legal consumer");
        ClearCompletedGeneration(o,actor);Publish(o,r,"withdraw-final",actor,target,"r2");Advance(o,r);
        Test.True(r.EventConvergence.Result=="agreement_not_reached"&&r.EventPhase=="closing"&&h.Draws==1,"final proposer can withdraw without new threat or reroll");
    }
    private static void PlayerWaiting()
    {
        var(h,o,r)=Fixture("p","b");Publish(o,r,"root","p","b");Publish(o,r,"r1","b","p","root",response:true);
        int count=r.EventConvergence.CompletedRounds;Advance(o,r);
        Test.True(count==1&&!r.EventConvergence.Ending&&r.PlayerNegotiationWaits.Any(x=>x.RequiresAnswer),"human silence retains existing wait and never invents a second round");
        Publish(o,r,"p2","p","b","r1");Publish(o,r,"r2","b","p","p2");
        int revision=r.ConversationRevision;
        for(int i=0;i<12;i++)Publish(o,r,"extra"+i,"p","b",body:"I still reject interference.");
        Test.True(r.ConversationRevision==revision&&r.EventConvergence.CompletedRounds==2,"additional player statements retain history without invalidating the finite ending");
        o.CurrentStorage.Jobs.Add(new(){JobId="new-unrelated-analysis",Kind="player_lifecycle",RoundId=r.RoundId,DocumentId="extra11"});
        Advance(o,r);
        Test.True(r.EventPhase=="closing"&&h.Draws==1,"unrelated newly queued analysis cannot starve the ending scheduler");
    }
    private static void NecessaryAnswers()
    {
        var(_,o,r)=Fixture();Publish(o,r,"root","a","b");Publish(o,r,"r1","b","a","root");
        r.Participants.Add(new(){KingdomId="c",State="observer"});
        r.PlayerResponses.Add(new(){SourceDocumentId="root",KingdomId="c",OriginalRoundId=r.RoundId,RequestKind="case_question",Status="pending"});
        Publish(o,r,"c2","a","b","r1");Publish(o,r,"r2","b","a","c2");Advance(o,r);
        Test.True(r.EventPhase!="closing"&&r.EventConvergence.RequiredSources.Contains("root"),"existing limited observer answer survives two-round transition");
        Publish(o,r,"limited","c","a","root");Advance(o,r);
        Test.True(r.Participants.Single(x=>x.KingdomId=="c").State=="observer"&&r.EventPhase=="closing","necessary observer answer completes without creating observer agenda or new round");
    }
    private static void Compliance()
    {
        var(_,o,r)=Fixture();Publish(o,r,"warning","a","b",intent:"warning");Call(o,"BeginEventEnding",r,"");
        Test.True(o.BuildLegalDiplomaticActionIntents(r,"b","a").Contains("comply_ultimatum"),"actual final action presentation includes compliance");
        Publish(o,r,"compliance","b","a","warning","comply_ultimatum");Advance(o,r);
        Test.True(o.CurrentStorage.DiplomaticThreats.Single().Status=="complied"&&r.EventPhase=="closing","compliance execution settles original threat and ends event");
    }
    private static void FinalGuards()
    {
        var(h,o,r)=Fixture();TwoRounds(o,r);h.Draw=0;Advance(o,r);
        var job=o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate");
        o.PrepareSharedRequest(job);
        if(job.Kind=="privacy_material") o.TryCompleteDiplomacyPipeline(job,new(){Success=true,Content="{\"safe\":true,\"public_context\":\"The published dispute remains unresolved.\"}"});
        o.PrepareSharedRequest(job);
        string contract=DiplomacyPromptTexts.Get("EventEndingContract","");
        Test.True(job.SystemPrompt.Contains(contract)&&job.UserPrompt.Contains("\"Route\":\"threat\""),
            "actual final declaration request contains selected route and real package contract");
        object[] invalid={job,new JObject{["intent"]="propose_trade",["response_requests"]=new JArray()},""};
        Test.True(!(bool)typeof(WorldDiplomacyOrchestration).GetMethod("ValidateEndingDraft",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(o,invalid)!,"model cannot replace threat route with agreement");
        object[] extraReply={job,new JObject{["actions"]=new JArray(new JObject{["intent"]="warning"},new JObject{["intent"]="statement",["requires_response"]=true}),["response_requests"]=new JArray()},""};
        Test.True(!(bool)typeof(WorldDiplomacyOrchestration).GetMethod("ValidateEndingDraft",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(o,extraReply)!,"per-action reply flag cannot add an extra final speech");
        var root=o.ResolveDocument("root");
        Test.True(!(bool)Call(o,"IsFreshOrdinaryContinuation",r,"b",root),"already answered source is not another ordinary continuation");
        var restored=JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
        o.ReplaceStorage(restored);r=o.ResolveRound(r.RoundId);Advance(o,r);
        Test.True(h.Draws==1&&r.EventConvergence.Route=="threat","selected route persists while final generation is pending");
        job=o.CurrentStorage.Jobs.Single(x=>x.AuthorKingdomId==r.EventConvergence.Actor);
        job.PrivacyDraft="{\"title\":\"Warning\",\"body\":\"Our final demand remains.\",\"actions\":[]}";
        Call(o,"PrepareDeclarationAudit",job);
        Test.True(job.SystemPrompt.Contains(contract)&&job.UserPrompt.Contains("\"Route\":\"threat\""),
            "independent declaration auditor sees the same persisted route and bilingual contract");
    }
    private static void EndingRegistration()
    {
        var(_,o,r)=Fixture();TwoRounds(o,r);
        Publish(o,r,"extra-human-threat","p","b","r2","warning");
        Test.True(o.CurrentStorage.DiplomaticThreats.Count==0,"extra human statement cannot register a new threat chain in the ending case");
        var counter=new WorldDiplomacyDocument { DocumentId="counter",RoundId=r.RoundId,AuthorKingdomId="b",TargetKingdomId="a",
            Intent="propose_trade",RespondingToOfferDocumentId="original",RespondingToOfferActionId="terms" };
        r.PendingOffers.Add(new(){SourceDocumentId="original",SourceActionId="terms",ProposerKingdomId="a",TargetKingdomId="b",Intent="propose_trade",Status="open"});
        o.TrySettleRelayOffer(counter);
        Test.True(r.PendingOffers.Count==1&&r.PendingOffers[0].Status=="rejected"&&!counter.ChangedDiplomaticState,
            "exact structured counterconditions reject original terms without another final offer or invented agreement");
        counter.DocumentId="unbound";counter.RespondingToOfferDocumentId="";counter.RespondingToOfferActionId="";
        r.PendingOffers[0].Status="open";o.TrySettleRelayOffer(counter);
        Test.True(r.PendingOffers.Count==1&&r.PendingOffers[0].Status=="open","unbound human prose cannot invent rejection of an existing offer");
    }
    private static void LegacyClosing()
    {
        var(h,o,r)=Fixture();Publish(o,r,"legacy-warning","a","b",intent:"warning");
        r.EventPhase="closing";r.EventOutcome="deadlocked";
        o.CurrentStorage.Jobs.Add(new(){JobId="legacy-end",RoundId=r.RoundId,Kind="event_boundary",ForcedIntent="ended"});
        Advance(o,r);
        Test.True(r.EventPhase=="discussing"&&r.EventConvergence.Route=="existing_threat"&&h.Draws==0
            &&!o.CurrentStorage.Jobs.Any(x=>x.JobId=="legacy-end"),"legacy uncommitted ending resumes original threat instead of publishing false closure");
        object[] publish={r,"ended",new JObject(),""};
        Test.True(!(bool)typeof(WorldDiplomacyOrchestration).GetMethod("TryPublishEventBoundary",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(o,publish)!
            &&(string)publish[3]=="event_ending_original_obligation_unfinished","late ending callback cannot publish over an unresolved original duty");
    }
    private static void HistoricalResistance()
    {
        var fixture=JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"event-convergence-actual.json")));
        foreach(var scenario in fixture["scenarios"]!.OfType<JObject>())
        {
            var(h,o,r)=Fixture();var drafts=scenario["samples"]!.Select(x=>(JObject)x["original_draft"]!).ToList();
            Test.True(drafts.Count>0,"historical hostile evidence exists: "+scenario["scenario"]);
            for(int i=0;i<4;i++)
            {
                var draft=drafts[i%drafts.Count];string intent=(string)draft["actions"]![0]!["intent"]!;
                Publish(o,r,"historical-"+i,i%2==0?"a":"b",i%2==0?"b":"a",i==0?"":"historical-"+(i-1),intent,
                    body:(string)draft["body"]!);
            }
            Advance(o,r);
            Test.True(r.EventConvergence.CompletedRounds==2&&r.EventPhase=="closing","hostile real draft prose cannot create endless ordinary exchange: "+scenario["scenario"]);
            Test.True(o.CurrentStorage.Documents.All(x=>x.Body==drafts[0]["body"]!.Value<string>()||drafts.Any(d=>(string)d["body"]==x.Body)),"recorded prose retained verbatim");
            Console.WriteLine("Historical prose replay "+scenario["scenario"]+": route="+r.EventConvergence.Route+", actual_war="+r.EventConvergence.ActualWar+"; controlled source bindings, no provider call.");
        }
    }
}
