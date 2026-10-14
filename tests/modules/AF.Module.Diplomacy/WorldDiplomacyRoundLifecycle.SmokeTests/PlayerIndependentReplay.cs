using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class PlayerIndependentReplay
{
    private static object Invoke(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args)!;
    private static WorldDiplomacyRound Round(string id, string owner = "player") => new() {
        RoundId = id, SchedulingOwner = owner, OwnershipEpoch = 1, State = "active", InitiatorKingdomId = "a",
        PlayerAuthorityKingdomId = owner == "player" ? "p" : "", PlayerAuthorityRulerId = owner == "player" ? "ruler_p" : "",
        RelayRouteKingdomIds = new() { "a", "p" }, Participants = new() { new() { KingdomId = "a", State = "active" }, new() { KingdomId = "p", State = "active" } } };
    private static WorldDiplomacyDocument Doc(string id, string round, string author, string target = "p") => new() {
        DocumentId = id, RoundId = round, AuthorKingdomId = author, TargetKingdomId = target,
        IsPlayerAuthored = author == "p", IsReadyForPublication = true, Day = 10, Title = "Notice", Body = "Public proposal",
        Intent = "statement", RequiresResponse = true, AddressedKingdomIds = new() { target }, AnalysisStatus = "success" };
    private static LlmJobResult Result(WorldDiplomacyJob job, WorldDiplomacyRound round, string content) => new() {
        JobId = job.JobId, Success = true, Content = content, RequestAttempt = job.RequestAttempt,
        OwnershipEpoch = job.OwnershipEpoch, SchedulingOwner = job.SchedulingOwner,
        PlayerAuthorityRulerId = job.PlayerAuthorityRulerId, RoundConversationRevision = round.ConversationRevision };

    internal static void Run()
    {
        BudgetAndLeases(); HandoffAndRecovery(); NoticesAndSilence(); Privacy(); Conditions(); EventCadenceAndWithdrawal(); ConfirmedOralWithPrivacy(); AuthorityChangeAndQuietAgenda(); ScopedResponsesAndRefusals(); DeadlineAndQueuedAudit();
        Console.WriteLine("Player independent chain: production owners and protocol fakes; live game/provider/.sav NOT-RUN");
        PlayerSaveReplay.Run();
    }

    private static void BudgetAndLeases()
    {
        var budget = new DiplomacyRequestBudget(); budget.AdvanceDay(10);
        for (int i = 0; i < 500; i++) Test.True(budget.TryAdmit(true), "player chain has no daily ceiling");
        Test.True(budget.NonPlayerStarted == 0 && budget.Started == 0 && budget.PlayerStarted == 500, "player HTTP attempts do not charge ordinary bank");
        for (int i = 0; i < 12; i++) Test.True(budget.TryAdmit(false), "ordinary bank retains all twelve starts without reserve");
        Test.True(!budget.TryAdmit(false) && budget.TryAdmit(true), "ordinary exhaustion cannot starve player lane");
        var restored = JsonConvert.DeserializeObject<DiplomacyRequestBudget>(JsonConvert.SerializeObject(budget))!;
        Test.True(restored.PlayerStarted == 501 && restored.NonPlayerStarted == 12, "lane statistics persist separately");
        budget.AdvanceDay(11); Test.True(budget.PlayerStarted == 0 && budget.TryAdmit(false), "new day resets both statistics");
        var oldBank=JsonConvert.DeserializeObject<DiplomacyRequestBudget>("{\"day\":12,\"started\":12,\"nonPlayerStarted\":8}")!;
        oldBank.AdvanceDay(12);
        Test.True(oldBank.NonPlayerStarted==8 && oldBank.PlayerStarted==4 && oldBank.Snapshot().SchemaVersion==1,
            "old completed requests migrate to their respective counters without refunding or discarding sends");
        var leases = new WorldDiplomacyRequestLeaseCoordinator(3);
        var first = new WorldDiplomacyJob { JobId = "first", RoundId = "one", Kind = "privacy_material" };
        Test.True(leases.TryClaim(first, 1, 100, 1000, true, out _), "privacy work holds event speech lease");
        Test.True(!leases.TryClaim(new() { JobId = "conflict", RoundId = "one", Kind = "generate" }, 1, 100, 1000, true, out _), "no conflicting event speech during isolation");
        Test.True(leases.TryClaim(new() { JobId = "normal", RoundId = "two", Kind = "generate" }, 1, 100, 1000, false, out _), "ordinary requests retain a concurrent opportunity");
        Test.True(leases.TryClaim(new() { JobId = "player", RoundId = "three", Kind = "generate" }, 1, 100, 1000, true, out _) && leases.IsFull, "bounded total concurrency");
        leases.Reset();
    }

    private static void HandoffAndRecovery()
    {
        var (h,o) = ConcurrentOralMigrationReplay.Fixture(); var storage=o.CurrentStorage;
        var round=Round("one","normal"); round.RoundTopic="Original dispute";
        round.Participants[0].Agenda="Recorded future intention";
        round.PendingOffers.Add(new() { SourceDocumentId="original",SourceActionId="action_1",Intent="propose_peace",ProposerKingdomId="a",TargetKingdomId="p" });
        storage.ConcurrentRounds.Add(round); storage.Documents.Add(Doc("original","one","a"));
        var other=Round("other","normal"); storage.ConcurrentRounds.Add(other);
        foreach (string kind in new[]{"generate","analyze","round_plan","round_compress"})
        { var job=new WorldDiplomacyJob { JobId=kind,Kind=kind,RoundId="one",AuthorKingdomId="a",TargetKingdomId="p"};DiplomacyPlayerEventRules.Bind(round,job);storage.Jobs.Add(job); }
        var unrelated=new WorldDiplomacyJob { JobId="unrelated",Kind="generate",RoundId="other" };storage.Jobs.Add(unrelated);
        var old=storage.Jobs[0]; var oldResult=Result(old,round,"{}");
        Test.True(o.RequestLeases.TryClaim(old,1,100,100,true,out var lease),"old speech can be in flight at handoff");
        Invoke(o,"TransferEvent",round,true,"test_insertion");
        Test.True(round.SchedulingOwner=="player" && round.OwnershipEpoch==2 && round.PendingOffers[0].SourceDocumentId=="original", "same event takes ownership without losing source terms");
        Test.True(storage.Jobs.Where(x=>x.RoundId=="one").All(x=>x.OwnershipEpoch==2 && x.JobId!=x.Kind),"all job kinds rekey at epoch transition");
        Test.True(unrelated.JobId=="unrelated" && o.RequestLeases.ContainsSpeech("one"),"other tasks stay intact; old lease retained until real completion");
        Test.True(lease.Cancellation.IsCancellationRequested,"handoff revokes unsent transport admission for this event only");
        Test.True(!o.AdmitCompletion(old,oldResult),"old result cannot commit after event handoff");
        int epoch=round.OwnershipEpoch; Invoke(o,"TransferEvent",round,true,"repeat");Test.True(round.OwnershipEpoch==epoch,"repeated insertion is idempotent");
        string context=(string)Invoke(o,"BuildPlayerEventContinuity",round,"a");
        Test.True(context.Contains("original") && context.Contains("Recorded future intention") && context.Contains("Original dispute"),"successor actually reads clauses, recorded intentions and topic");
        var copy=JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(storage))!;
        Test.True(copy.ConcurrentRounds[0].SchedulingOwner=="player" && copy.ConcurrentRounds[0].OwnershipEpoch==2 && copy.ConcurrentRounds[0].PendingOffers.Count==1,"handoff ownership and proposals survive storage roundtrip");
        var oral=Round("oral");oral.RelayRouteKingdomIds=new(){"a","b"};oral.PlayerAuthorityKingdomId="";oral.PlayerAuthorityRulerId="";oral.EventSourceType="dialogue_commitment";
        storage.ConcurrentRounds.Add(oral);Invoke(o,"InvalidateDialogueIndex");Test.True(o.IsPlayerSchedulingJobForDispatch(new(){RoundId="oral",Kind="analyze",AuthorKingdomId="b"}),"entire oral A-to-B event is player work without player signatory");
    }

    private sealed class Sink : IWorldDiplomacyNotificationSink
    {
        internal int Hour=245, Shown; internal bool Enabled=true, Available=true, Register=true, Throw;
        public bool MapNotificationsEnabled=>Enabled;
        public bool CanPublishMapNotification()=>Available;
        public bool EnsureMapNotificationRegistered()=>Register;
        public string KingdomName(string id)=>id;
        public string FormatCampaignDate(int day)=>day.ToString();
        public int CurrentDay=>Hour/24; public double CurrentHour=>Hour;
        public void RecordShownTime(WorldDiplomacyDocument document, double hour) { }
        public string PlayerKingdomId=>"p";
        public void ShowRumor(string text) { }
        public void ShowNotice(AnimusForge.Refactor.Contracts.WorldDiplomacyNotice notice) { if(Throw)throw new Exception("widget unavailable"); Shown++; LastMap=notice.ShowOnMap; }
        internal bool LastMap;
        public void Log(string text) { }
    }
    private static void NoticesAndSilence()
    {
        var storage=new WorldDiplomacyStorage(); var doc=Doc("incoming","one","a");doc.HasReachedPlayerCourt=true;doc.IsRead=true;storage.Documents.Add(doc);
        var sink=new Sink();var notices=new WorldDiplomacyNotificationApplication();var now=DateTime.UtcNow;
        sink.Available=false;notices.Poll(storage,now,sink);Test.True(doc.ActualPlayerNoticeHour<0,"arrival without display never starts clock");
        sink.Available=true;sink.Throw=true;notices.Poll(storage,now.AddSeconds(2),sink);Test.True(doc.ActualPlayerNoticeHour<0 && !doc.FormalNoticeShown,"failed widget remains retryable without timestamp");
        sink.Throw=false;sink.Enabled=false;notices.Poll(storage,now.AddSeconds(4),sink);
        Test.True(sink.Shown==1 && !sink.LastMap && doc.ActualPlayerNoticeHour==245,"disabled right notice produces text even for pre-read incoming request");
        sink.Enabled=true;sink.Hour=270;notices.Poll(storage,now.AddSeconds(6),sink);Test.True(sink.Shown==1 && doc.ActualPlayerNoticeHour==245,"switch/read cannot renotify or reset clock");
        var wait=new WorldDiplomacyPlayerWait { SourceDocumentId=doc.DocumentId,NotifiedHour=doc.ActualPlayerNoticeHour };
        Test.True(!DiplomacyPlayerEventRules.SilenceDue(wait,316) && DiplomacyPlayerEventRules.SilenceDue(wait,317),"exact three game days from actual display hour");
        wait.NotifiedHour=245.75;
        Test.True(!DiplomacyPlayerEventRules.SilenceDue(wait,317.74) && DiplomacyPlayerEventRules.SilenceDue(wait,317.75),"fractional actual display time cannot expire nearly one hour early");
        wait.NotifiedHour=245;
        wait.AnswerDocumentId="published_not_delivered";Test.True(!DiplomacyPlayerEventRules.SilenceDue(wait,1000),"public player reply stops silence before NPC knowledge");
        var (h,o)=ConcurrentOralMigrationReplay.Fixture();var round=Round("one");o.CurrentStorage.ConcurrentRounds.Add(round);o.CurrentStorage.Documents.Add(doc);
        round.PlayerNegotiationWaits.Add(wait);wait.AnswerDocumentId="";
        var reply=Doc("reply","provisional","p","a");reply.SourceDocumentId=doc.DocumentId;o.CurrentStorage.Documents.Add(reply);
        reply.AnsweredNpcDocumentIds.Add(doc.DocumentId);
        Invoke(o,"RegisterPublishedPlayerAnswer",reply);Test.True(wait.AnswerDocumentId=="reply","actual publication command stops relevant waiting");
        var closure=new WorldDiplomacyJob{JobId="closing",RoundId="one",Kind="generate",SilenceSourceDocumentId=doc.DocumentId};DiplomacyPlayerEventRules.Bind(round,closure);
        Test.True(!o.AdmitCompletion(closure,Result(closure,round,"{}")),"in-flight silent closing discarded after relevant player publication");
        var unrelated=Doc("unrelated","other","p","c");Invoke(o,"RegisterPublishedPlayerAnswer",unrelated);Test.True(wait.AnswerDocumentId=="reply","unrelated declaration does not change this wait");
        var legacy=Round("legacy");legacy.PlayerNegotiationWaits.Clear();o.CurrentStorage.ConcurrentRounds.Add(legacy);var old=Doc("legacy_doc","legacy","a");old.FormalNoticeShown=true;old.IsNotified=true;o.CurrentStorage.Documents.Add(old);
        Invoke(o,"InvalidateDialogueIndex");
        Invoke(o,"NormalizePlayerEventState");Test.True(!old.FormalNoticeShown && old.ActualPlayerNoticeHour<0,"legacy notice boolean is not proof of display; renotify once");
    }

    private static void Privacy()
    {
        var (h,o)=ConcurrentOralMigrationReplay.Fixture();var round=Round("privacy");o.CurrentStorage.ConcurrentRounds.Add(round);
        var job=new WorldDiplomacyJob { JobId="privacy_job",RoundId=round.RoundId,Kind="generate",AuthorKingdomId="a",TargetKingdomId="p",PersonalMemoryRulerId="ruler_a",UserPrompt="Known public insult. SECRET_PRIVATE_ROMANCE",SystemPrompt="declaration rules" };
        DiplomacyPlayerEventRules.Bind(round,job);o.CurrentStorage.Jobs.Add(job);
        Invoke(o,"PrepareDiplomacyPrivacy",job);Test.True(job.Kind=="privacy_material" && job.UserPrompt.Contains("SECRET_PRIVATE_ROMANCE"),"protected raw material is isolated in specialist request");
        o.TryCompleteDiplomacyPipeline(job,Result(job,round,"{\"safe\":true,\"public_context\":\"Known public insult; exact legal action IDs\",\"attitude_summary\":\"Reserved attitude\"}"));
        Test.True(job.Kind=="generate" && job.PrivacyPublicContext.Contains("Known public insult"),"ordinary public insult remains available after isolation");
        job.UserPrompt="SECRET_PRIVATE_ROMANCE";Invoke(o,"PrepareDiplomacyPrivacy",job);
        var messages=WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(job,_=>"").ToString();
        Test.True(!messages.Contains("SECRET_PRIVATE_ROMANCE") && messages.Contains("Reserved attitude"),"actual declaration messages contain non-leaking attitude only");
        for(int i=0;i<3;i++)
        {
            Invoke(o,"DeferDraftForPrivacyCheck",job,"{\"title\":\"leak\",\"body\":\"SECRET_PRIVATE_ROMANCE\"}");
            o.TryCompleteDiplomacyPipeline(job,Result(job,round,"{\"safe\":false}"));
        }
        Test.True(job.SuspendedReason=="privacy_rewrites_exhausted" && o.CurrentStorage.Documents.Count==0,"finite confidentiality rewrites suspend without declaration or political refusal");
        Test.True(!round.PlayerNegotiationWaits.Any(),"protected draft never starts silence time");
        var saved=JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
        Test.True(saved.Jobs[0].SuspendedReason==job.SuspendedReason && saved.Jobs[0].PrivacyDraft.Contains("SECRET_PRIVATE_ROMANCE"),"held work and failure reason survive roundtrip for recovery");
    }

    private static void Conditions()
    {
        var proposal=new WorldDiplomacyDocument{DocumentId="terms_v1",Actions=new(){new(){ActionId="action_1",Intent="propose_peace",DependencyActionIds=new(){"action_2"}},new(){ActionId="action_2",Intent="propose_trade"}}};
        var acceptance=new List<WorldDiplomacyDocumentAction>{new(){ActionId="pay",Intent="accept_peace",RespondingToOfferDocumentId="terms_v1",RespondingToOfferActionId="action_1"},new(){ActionId="independent",Intent="propose_alliance"}};
        var deferred=DiplomacyConditionalExecutionRules.DeferredActions(acceptance,id=>id==proposal.DocumentId?proposal:null);
        Test.True(deferred.Contains("pay") && !deferred.Contains("independent"),"partial assent cannot execute favorable dependent half; independent proposal remains eligible");
        var linked=new List<WorldDiplomacyDocumentAction>{new(){ActionId="cession",DependencyActionIds=new(){"payment"}},new(){ActionId="payment"},new(){ActionId="independent"}};
        deferred=DiplomacyConditionalExecutionRules.DeferredActions(linked,_=>null);Test.True(deferred.SetEquals(new[]{"cession","payment"}),"coupled group is deferred symmetrically without splitting exchange");
        var safePeace=new[]{new WorldDiplomacyDocumentAction{ActionId="peace",PeaceTerms=new(){DailyTribute=100,CessionSettlementId="castle"}}};
        Test.True(DiplomacyConditionalExecutionRules.DeferredActions(safePeace,_=>null).Count==0,"existing single peace action with cession and tribute retains coordinated executor");
        var clone=new WorldDiplomacyDocumentExecutionCommand.ActionInput(linked[0]).Materialize();linked[0].DependencyActionIds.Clear();
        Test.True(clone.DependencyActionIds.Count==1,"execution command freezes dependencies against callback mutation");
        var port=new DocumentExecutionReplay.Port{PlayerKingdomId="p"};var orchestration=new DocumentExecutionReplay.Orch(port);
        var doc=new WorldDiplomacyDocument { DocumentId="exchange",RoundId="r",AuthorKingdomId="p",IsPlayerAuthored=true,
            Actions=new(){new(){ActionId="one",TargetKingdomId="a",Intent="declare_war",Commitment="binding",DependencyActionIds=new(){"two"}},
                new(){ActionId="two",TargetKingdomId="b",Intent="declare_war",Commitment="binding"},
                new(){ActionId="independent",TargetKingdomId="c",Intent="declare_war",Commitment="binding"}} };
        WorldDiplomacyDocumentExecutionApplication.ProcessAnalyzedDocument(port,orchestration,doc,"declare_war","binding",false,"firm",1,out var receipts);
        Test.True(port.Effects==1 && receipts.Count==3 && !receipts[0].EffectAttempted && !receipts[1].EffectAttempted && receipts[2].EffectAttempted,
            "real execution owner defers both coupled effects while executing independent authorized action");
        Test.True(doc.Actions[0].ExecutionState=="deferred_conditions" && !doc.Actions[0].ChangedDiplomaticState,"deferred conditions persist without false success");
    }

    private static void EventCadenceAndWithdrawal()
    {
        var (h,o)=ConcurrentOralMigrationReplay.Fixture();var first=Round("npc_active");first.NpcInitiatedPlayerEvent=true;o.CurrentStorage.ConcurrentRounds.Add(first);
        o.CurrentStorage.ConcurrentRounds.Add(Round("player_started_a"));o.CurrentStorage.ConcurrentRounds.Add(Round("player_started_b"));
        Test.True(DiplomacyPlayerEventRules.HasNpcOpening(o.CurrentStorage.ConcurrentRounds),"player initiated cases coexist with the sole NPC opening");
        Invoke(o,"TrySchedulePlayerAgenda");Test.True(o.CurrentStorage.ConcurrentRounds.Count==3,"independent major development cannot force another proactive case");
        var closed=Doc("exit","npc_active","a");closed.RequiresResponse=false;closed.RoundParticipation="withdraw";o.CurrentStorage.Documents.Add(closed);
        o.HandleRoundDocumentProcessed(closed);Test.True(first.Participants[0].State=="withdrawn" && first.Participants[1].State=="active","one country withdrawal preserves other participants");
        var third=new WorldDiplomacyRoundParticipant{KingdomId="c",State="active"};first.Participants.Add(third);
        Test.True(first.State=="active" && first.Participants.Last().KingdomId=="c","multi-country event keeps same identity after one exit");
        Test.True(!WorldDiplomacyLiveRoundRules.IsOrdinary(first),"player events never occupy ordinary opening admission slots");
        var ordinary=Round("ordinary","normal");Test.True(WorldDiplomacyLiveRoundRules.IsOrdinary(ordinary),"ordinary NPC opening remains ordinary");
        var playerJob=new WorldDiplomacyJob { RoundId=first.RoundId,Priority=95 };var normalJob=new WorldDiplomacyJob{RoundId=ordinary.RoundId,Priority=20};o.CurrentStorage.ConcurrentRounds.Add(ordinary);
        o.RecordLaneDispatch(normalJob);Test.True(o.LaneDispatchPriority(playerJob,12)>o.LaneDispatchPriority(normalJob,12),"after normal send player lane gets opportunity");
        o.RecordLaneDispatch(playerJob);Test.True(o.LaneDispatchPriority(normalJob,12)>o.LaneDispatchPriority(playerJob,12),"continuous player backlog does not starve ordinary lane");
        Invoke(o,"RegisterPlayerAgendaCandidate","major_fact","b","p");
        Test.True(o.CurrentStorage.PlayerAgendaCandidates.Single().KingdomId=="b" && first.NpcInitiatedPlayerEvent,"independent major issue retained as candidate without a new active event");
        var candidateCopy=JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
        Test.True(candidateCopy.PlayerAgendaCandidates[0].SourceKeys.Contains("major_fact"),"agenda candidate survives save boundary");
    }

    private struct CompletionSource : IWorldDiplomacyCompletionSource
    {
        internal WorldDiplomacyOrchestration Owner; internal Queue<LlmJobResult> Results;
        public long RuntimeGeneration=>42;
        public bool IsSaveRuntimeStale(long generation)=>false;
        public WorldDiplomacyStorage Storage=>Owner.CurrentStorage;
        public int CurrentHour=>288; public int FailedServiceCooldownHours=>1;
        public bool TryDequeue(out LlmJobResult result)=>Results.TryDequeue(out result);
        public void ReleaseLease(string job,long generation) { }
        public void LogUsage(WorldDiplomacyJob job,LlmJobResult result) { }
        public void RemoveJob(string id)=>Owner.RemoveJob(id);
        public void Log(string text) { }
    }
    private static void CompleteActual(WorldDiplomacyOrchestration o, WorldDiplomacyJob job,string reply)
    {
        Test.True(o.RequestLeases.TryClaim(job,42,1000,1000,true,out var request),"real application claims each pipeline attempt");
        var result=new LlmJobResult { JobId=request.JobId,RuntimeGeneration=request.RuntimeGeneration,RequestAttempt=request.Attempt,
            OwnershipEpoch=request.OwnershipEpoch,SchedulingOwner=request.SchedulingOwner,PlayerAuthorityRulerId=request.PlayerAuthorityRulerId,
            RoundConversationRevision=request.ConversationRevision,Success=true,Content=reply };
        var source=new CompletionSource{Owner=o,Results=new(new[]{result})};
        WorldDiplomacyCompletionApplication.Run(ref source,o);
    }
    private static void ConfirmedOralWithPrivacy()
    {
        var (h,o)=ConcurrentOralMigrationReplay.Fixture();h.PublishEnabled=true;h.PlayerAwareExecution=true;h.OralEffectPortsEnabled=true;
        o.SubmitOralDiplomaticCommitment("ruler_a","a","action=Trade;move=NewMatter;target=p;days=14",
            new AnimusForge.DiplomacyDialogue.DialogueInteractionOrigin("native","privacy-oral","session","双方同意贸易十四天。","我同意开放贸易十四天。"));
        var item=o.CurrentStorage.DialogueArrangements.Single();var job=o.CurrentStorage.Jobs.Single();
        o.PrepareSharedRequest(job);Test.True(job.Kind=="privacy_material","oral raw conversation enters specialist isolation before public generation");
        CompleteActual(o,job,"{\"safe\":true,\"public_context\":\"Both current rulers explicitly agreed to 14-day trade between a and p; fixed intent accept_trade and duration_days=14.\",\"attitude_summary\":\"cooperative\"}");
        o.PrepareSharedRequest(job);
        Test.True(job.Kind=="generate" && !WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(job,_=>"").ToString().Contains("PRIVATE_MEMORY"),"actual oral declaration messages exclude private memories");
        CompleteActual(o,job,OralDeclarationReplay.Draft(item,"accept_trade"));
        Test.True(job.Kind=="privacy_audit" && o.CurrentStorage.Documents.Count==0 && h.Effects==0,"successful writing still cannot publish or act before audit");
        o.PrepareSharedRequest(job);CompleteActual(o,job,"{\"safe\":true}");
        var document=o.ResolveDocument(item.DocumentId);
        Test.True(item.Status=="executed" && h.TradeDuration==14 && document?.IsReadyForPublication==true,"audited explicit oral agreement still fulfills at NPC publication without another player acceptance");
        int effects=h.Effects;
        var duplicate=Result(job,o.ResolveRound(item.RoundId),OralDeclarationReplay.Draft(item,"accept_trade"));duplicate.RuntimeGeneration=42;
        var source=new CompletionSource{Owner=o,Results=new(new[]{duplicate})};WorldDiplomacyCompletionApplication.Run(ref source,o);
        Test.True(h.Effects==effects && o.CurrentStorage.Documents.Count==1,"duplicate pipeline completion cannot republish or reexecute oral result");
    }

    private sealed class AuthorityHost : ConcurrentOralMigrationReplay.Host
    {
        internal string Realm="p";
        public override string PlayerKingdomId()=>Realm;
        public override string PartyRulerId(string id)=>id==Realm?"ruler_p":id=="p"?"successor_p":base.PartyRulerId(id);
        public override bool IsPlayerParty(string id)=>id==Realm;
        public override bool IsPlayerAffiliatedParty(string id)=>id==Realm;
    }
    private static void AuthorityChangeAndQuietAgenda()
    {
        var h=new AuthorityHost();var o=new WorldDiplomacyOrchestration(h,new WorldDiplomacyRuntimeState());h.Owner=o;
        var old=Round("former_realm");var retained=Round("new_realm_case");retained.Participants.Add(new(){KingdomId="b",State="active"});
        old.PendingOffers.Add(new(){SourceDocumentId="national_treaty",Status="accepted"});
        o.CurrentStorage.ConcurrentRounds.Add(old);o.CurrentStorage.ConcurrentRounds.Add(retained);
        var job=new WorldDiplomacyJob{JobId="old_request",Kind="analyze",RoundId=old.RoundId,AuthorKingdomId="p"};DiplomacyPlayerEventRules.Bind(old,job);o.CurrentStorage.Jobs.Add(job);
        h.Realm="b";Invoke(o,"RefreshPlayerAuthority");
        Test.True(old.SchedulingOwner=="normal" && WorldDiplomacyLiveRoundRules.IsOrdinary(old) && old.PendingOffers[0].Status=="accepted","former player realm returns to ordinary owner while established national facts survive");
        Test.True(retained.SchedulingOwner=="player" && retained.PlayerAuthorityKingdomId=="b" && retained.OwnershipEpoch==3,"case involving new player realm remains player-owned after authority handoff");
        Test.True(job.JobId!="old_request" && job.PlayerAuthorityRulerId=="","old national authority cannot retain request identity");
        var quiet=Round("quiet_agenda");quiet.NpcInitiatedPlayerEvent=true;o.CurrentStorage.ConcurrentRounds.Add(quiet);Invoke(o,"InvalidateDialogueIndex");
        var decision=new WorldDiplomacyJob{JobId="quiet",Kind="player_decision",RoundId=quiet.RoundId,AuthorKingdomId="a",TargetKingdomId="b",ForcedIntent="opening"};DiplomacyPlayerEventRules.Bind(quiet,decision);o.CurrentStorage.Jobs.Add(decision);
        o.TryCompleteDiplomacyPipeline(decision,Result(decision,quiet,"{\"participate\":false,\"reason\":\"No current motive\"}"));
        Test.True(quiet.State!="active" && o.CurrentStorage.Documents.Count==0 && !o.CurrentStorage.Jobs.Contains(decision),"interval expiry permits quiet with no forced declaration or fabricated political refusal");
    }

    private static void ScopedResponsesAndRefusals()
    {
        var (h,o)=ConcurrentOralMigrationReplay.Fixture();var round=Round("scope");o.CurrentStorage.ConcurrentRounds.Add(round);
        WorldDiplomacyDocument a=Doc("demand_a","scope","a"), b=Doc("demand_b","scope","b");o.CurrentStorage.Documents.AddRange(new[]{a,b});
        var wa=new WorldDiplomacyPlayerWait{SourceDocumentId=a.DocumentId,NotifiedHour=100};var wb=new WorldDiplomacyPlayerWait{SourceDocumentId=b.DocumentId,NotifiedHour=101};
        round.PlayerNegotiationWaits.AddRange(new[]{wa,wb});
        var unrelated=Doc("unrelated_advisory","new_event","p","c");unrelated.SourceDocumentId=a.DocumentId;
        Invoke(o,"RegisterPublishedPlayerAnswer",unrelated);Test.True(wa.AnswerDocumentId=="" && wb.AnswerDocumentId=="","reply shortcut alone cannot clear unrelated matters");
        var relevant=Doc("relevant","new_event","p","a");relevant.AnsweredNpcDocumentIds.Add(a.DocumentId);Invoke(o,"RegisterPublishedPlayerAnswer",relevant);
        Test.True(wa.AnswerDocumentId=="relevant" && wb.AnswerDocumentId=="" && wb.NotifiedHour==101,"analyzed relevance answers only the particular source without resetting another country's deadline");
        Test.True(!o.CurrentStorage.KingdomKnowledge.Any(x=>x.KingdomId=="a" && x.DocumentIds.Contains("relevant")),"publication acknowledgement never leaks undelivered player reply content to NPC court");
        var closing=Doc("public_refusal","scope","a");closing.Body="No more repetition of the unchanged border claim.";closing.RequiresResponse=false;
        Invoke(o,"RecordDisputeRefusal",round,closing);round.State="closed";
        Test.True((bool)Invoke(o,"HasActiveDisputeRefusal","a"),"temporary dispute scope survives completion of its original event");
        var copy=JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;o.ReplaceStorage(copy);
        Test.True((bool)Invoke(o,"HasActiveDisputeRefusal","a") && ((string)Invoke(o,"BuildDisputeRefusalContext","a")).Contains("unchanged border claim"),"new event generation actually reads the persisted prior refusal scope");
        var newRound=Round("new_scope");o.CurrentStorage.ConcurrentRounds.Add(newRound);Invoke(o,"InvalidateDialogueIndex");
        newRound.PlayerResponses.Add(new(){SourceDocumentId="repeat",KingdomId="a",Status="pending"});
        var decision=new WorldDiplomacyJob{JobId="repeat_review",Kind="player_decision",RoundId=newRound.RoundId,AuthorKingdomId="a",SourceDocumentId="repeat",ForcedIntent="reconsider_temporarily_refused_dispute"};DiplomacyPlayerEventRules.Bind(newRound,decision);o.CurrentStorage.Jobs.Add(decision);
        o.TryCompleteDiplomacyPipeline(decision,Result(decision,newRound,"{\"participate\":false,\"same_dispute_repeat\":true,\"reason\":\"same unchanged dispute\"}"));
        Test.True(newRound.PlayerResponses[0].Status=="declined_repeat" && !o.CurrentStorage.Documents.Any(x=>x.DocumentId=="repeat_review"),"model-confirmed repetition may stay quiet after an actual prior closing declaration");
        h.CurrentDayValue+=3;Test.True(!(bool)Invoke(o,"HasActiveDisputeRefusal","a"),"three-day refusal ends at boundary without permanent suppression");
    }

    private sealed class ClockHost : ConcurrentOralMigrationReplay.Host
    {
        internal double Hour;
        public override double CurrentCampaignHours()=>Hour;
    }
    private static void DeadlineAndQueuedAudit()
    {
        var h=new ClockHost{Hour=245.75};var o=new WorldDiplomacyOrchestration(h,new WorldDiplomacyRuntimeState());h.Owner=o;
        var round=Round("deadline");o.CurrentStorage.ConcurrentRounds.Add(round);var doc=Doc("real_notice","deadline","a");o.CurrentStorage.Documents.Add(doc);
        var wait=new WorldDiplomacyPlayerWait{SourceDocumentId=doc.DocumentId};round.PlayerNegotiationWaits.Add(wait);Invoke(o,"IndexPlayerWait",round,wait);
        o.RecordActualPlayerNotice(doc,245.75);o.RecordActualPlayerNotice(doc,250);
        var heap=(System.Collections.IDictionary)typeof(WorldDiplomacyOrchestration).GetField("_silenceDeadlines",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o)!;
        Test.True(wait.NotifiedHour==245.75 && heap.Count==1,"real display callback creates one precise deadline, repeated receipt cannot reset it");
        h.Hour=317.74;Invoke(o,"ProcessDuePlayerSilence");Test.True(heap.Count==1 && o.CurrentStorage.Jobs.Count==0,"deadline queue ignores future work without scanning events");
        o.CurrentStorage.Jobs.Add(new(){JobId="busy",Kind="generate",RoundId=round.RoundId});h.Hour=317.75;Invoke(o,"ProcessDuePlayerSilence");
        Test.True(wait.NotifiedHour==245.75 && heap.Count==1 && o.CurrentStorage.Documents.Count==1,"busy transport postpones processing but never restarts waiting or fabricates refusal");
        var (host,owner)=ConcurrentOralMigrationReplay.Fixture();host.PublishEnabled=true;host.PlayerAwareExecution=true;host.OralEffectPortsEnabled=true;
        owner.SubmitOralDiplomaticCommitment("ruler_a","a","action=Trade;move=NewMatter;target=p;days=14",new AnimusForge.DiplomacyDialogue.DialogueInteractionOrigin("native","audit-refresh","session","同意14日贸易","同意14日贸易"));
        var item=owner.CurrentStorage.DialogueArrangements.Single();var job=owner.CurrentStorage.Jobs.Single();owner.PrepareSharedRequest(job);
        var oralRound=owner.ResolveRound(item.RoundId);
        owner.TryCompleteDiplomacyPipeline(job,Result(job,oralRound,"{\"safe\":true,\"public_context\":\"Fixed trade agreement\",\"attitude_summary\":\"cooperative\"}"));owner.PrepareSharedRequest(job);
        Invoke(owner,"DeferDraftForPrivacyCheck",job,OralDeclarationReplay.Draft(item,"accept_trade"));oralRound.ConversationRevision++;
        owner.PrepareSharedRequest(job);Test.True(job.Kind=="privacy_material" && job.PrivacyDraft=="" && host.TradeDuration==0,"queued audit from an older conversation cannot borrow a fresh revision and publish old draft");
    }
}
