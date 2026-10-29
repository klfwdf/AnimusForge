using System.Reflection;
using AnimusForge;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

internal static class PlayerLifecycleReplay
{
    private sealed class GenerationHost : ConcurrentOralMigrationReplay.Host
    {
        public override string ResolveKingdomIdOrNull(string id) => string.IsNullOrWhiteSpace(id) ? null : id;
        public override string ResolveKingdomNameOrEmpty(string id) => id;
        public override int MaxDiplomaticActionsPerDocument() => 8;
        public override IWorldDiplomacyPeaceAdmissionPort PeaceAdmission() => new PeaceAdmissionReplay.Port {War=false};

    }
    private static object Call(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(o, args)!;
    internal static WorldDiplomacyRound Case(string id) => new() { RoundId=id, State="active", SchedulingOwner="player", EventSystemVersion=1, BirthLane="player",
        NpcInitiatedPlayerEvent=true, InitiatorKingdomId="a", PlayerAuthorityKingdomId="p", PlayerAuthorityRulerId="ruler_p",
        Participants=new() { new() {KingdomId="a",State="active"}, new() {KingdomId="p",State="active"} } };
    internal static void Run()
    {
        var (h,o)=ConcurrentOralMigrationReplay.Fixture();
        var r=Case("real_aserai_case"); r.RootDocumentId="real_aserai_notice";o.CurrentStorage.ConcurrentRounds.Add(r);
        var doc=new WorldDiplomacyDocument { DocumentId=r.RootDocumentId,RoundId=r.RoundId,AuthorKingdomId="a",TargetKingdomId="p",
            AddressedKingdomIds=new(){"p"},IsReadyForPublication=true,Intent="statement",Commitment="non_binding",
            RoundParticipation="continue",RoundStatus="continue",RequiresResponse=false,Body="Please respond to this call for restraint." };
        o.CurrentStorage.Documents.Add(doc);
        o.ProcessRoundLifecycle();
        Test.True(r.State=="active","a published continuing statement must retain the unique NPC slot before court arrival, even with no queued work");
        Test.True(!o.CurrentStorage.CompletedRounds.Contains(r),"absence of tasks is not an explicit political conclusion");
        GeneratedStatementAndFiniteClassification();
        SemanticReplyRouting();
        OldAutomaticClosureRecovery();
        MultiCountryClosure();
        RoutingBackpressureAndSave();
        Console.WriteLine("Player lifecycle real-output regression passed; public fixture model decisions are synthetic, live provider/game/sav NOT-RUN.");
    }

    private static LlmJobResult Result(WorldDiplomacyJob job, string value) => new() { JobId=job.JobId,Success=true,Content=value };
    // This is the observed public Aserai envelope shape, with realm IDs projected to the test host's a/p identities.
    private static JObject ObservedDraft() => JObject.Parse("""
        {"title":"关于维持边境稳定与和平底线的声明","body":"阿塞莱王庭向西帝国政府致意。鉴于两国领土接壤且边境关系总体谨慎，阿塞莱苏丹温吉德秉持审慎稳健之国策，特此发表本项正式声明。当前双边虽维持和平，然边境防务事关重大，容不得丝毫疏忽。阿塞莱在此阐明立场，要求西帝国贵族与加里俄斯统治下的西帝国当局保持应有的克制与边界分寸，切实尊重双方现有的领土划分与和平格局。王庭无意挑起事端，但也绝不容忍任何威胁边境安宁的试探。希望西帝国政府对本国之呼吁予以审慎回应，共同维护地区稳定，避免因误判而导致不必要的战略摩擦。","actions":[{"target_kingdom_id":"p","intent":"statement","commitment":"non_binding","negotiation_move":"state_position","peace_terms":{}}],"mentioned_kingdom_ids":["p"],"tone":"firm","round_plan":{"topic":"重申边境分寸与和平底线","selected_kingdom_ids":["p"]},"international_reputation_delta":1,"international_reputation_reason":"公开阐明立场"}
        """);

    private static void GeneratedStatementAndFiniteClassification()
    {
        var h=new GenerationHost { PublishEnabled=true,PlayerAwareExecution=true,OralRange=(0,3000) };
        var o=new WorldDiplomacyOrchestration(h,new WorldDiplomacyRuntimeState());h.Owner=o;
        var r=Case("observed_generation");o.CurrentStorage.ConcurrentRounds.Add(r);
        var job=new WorldDiplomacyJob { JobId="observed",Kind="generate",RoundId=r.RoundId,AuthorKingdomId="a",TargetKingdomId="p",PrivacyApproved=true };
        DiplomacyEventRules.Bind(r,job);o.CurrentStorage.Jobs.Add(job);
        string raw=ObservedDraft().ToString();o.CommitGeneratedDocument(job,raw);job.PrivacyApproved=false;
        Test.True(job.Kind=="player_lifecycle" && o.CurrentStorage.Documents.Count==0,"observed missing metadata is privately classified before any public artifact or effect");
        o.PrepareSharedRequest(job);
        string messages=WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(job,_=>"").ToString();
        Test.True(messages.Contains("requires_response") && messages.Contains("round_participation") && messages.Contains("public_declaration"),"actual classification messages include strict protocol and the preserved draft");
        o.TryCompleteDiplomacyPipeline(job,Result(job,"{\"requires_response\":true,\"round_participation\":\"continue\",\"round_status\":\"continue\"}"));
        Test.True(o.CurrentStorage.Documents.Count==1,"classified publication expected; jobs="+JsonConvert.SerializeObject(o.CurrentStorage.Jobs.Select(x=>new{x.Kind,x.SuspendedReason,x.SemanticRepairAttempts,x.UserPrompt})));
        var published=o.CurrentStorage.Documents.Single();
        Test.True(published.IsReadyForPublication && published.RequiresResponse && published.PlayerLifecycleClassified,"player statement response obligation survives existing semantic validation and authority execution");
        Test.True(published.Body==ObservedDraft()["body"]!.Value<string>() && r.PlayerNegotiationWaits.Single().SourceDocumentId==published.DocumentId,"classification changes metadata only and registers the concrete incoming source");
        h.CurrentDayValue+=10;o.ProcessRoundLifecycle();
        Test.True(r.State=="active" && r.PlayerNegotiationWaits[0].NotifiedHour<0,"long pre-arrival delay cannot silently finish the event or start a deadline");
        var other=Case("finite_missing");o.CurrentStorage.ConcurrentRounds.Add(other);
        var bad=new WorldDiplomacyJob { JobId="bad_metadata",Kind="player_lifecycle",RoundId=other.RoundId,AuthorKingdomId="a",PrivacyDraft=raw };
        DiplomacyEventRules.Bind(other,bad);o.CurrentStorage.Jobs.Add(bad);
        for(int i=0;i<2;i++)o.TryCompleteDiplomacyPipeline(bad,Result(bad,"{\"requires_response\":\"false\"}"));
        Test.True(bad.LifecycleClassificationAttempts==2 && bad.SuspendedReason!="" && other.State=="active","invalid metadata has finite attempts then technical hold, never political closure");
        Test.True(o.CurrentStorage.Documents.Count==1 && h.Effects==0,"technical classification failure cannot fabricate documents or mechanics");
    }

    private static void SemanticReplyRouting()
    {
        var (h,o)=ConcurrentOralMigrationReplay.Fixture();var r=Case("same_dispute");r.RootDocumentId="incoming";o.CurrentStorage.ConcurrentRounds.Add(r);
        var source=new WorldDiplomacyDocument { DocumentId="incoming",RoundId=r.RoundId,AuthorKingdomId="a",TargetKingdomId="p",IsReadyForPublication=true,IsRead=true,Body="Please explain your border policy.",Day=1 };
        o.CurrentStorage.Documents.Add(source);r.PlayerNegotiationWaits.Add(new(){SourceDocumentId=source.DocumentId,NotifiedHour=5});
        var provisional=Case("provisional");provisional.NpcInitiatedPlayerEvent=false;provisional.DialogueArrangementId="player_manual_declaration";o.CurrentStorage.ConcurrentRounds.Add(provisional);
        var reply=new WorldDiplomacyDocument { DocumentId="player_question",RoundId=provisional.RoundId,AuthorKingdomId="p",TargetKingdomId="a",IsPlayerAuthored=true,IsReadyForPublication=true,Intent="statement",AnalysisStatus="success",PlayerAnalysisCommitted=true,Body="What is the intention of your announcement? Please explain.",Day=2 };
        provisional.RootDocumentId=reply.DocumentId;o.CurrentStorage.Documents.Add(reply);
        var followup=new WorldDiplomacyDocument { DocumentId="already_published_followup",RoundId=provisional.RoundId,AuthorKingdomId="a",TargetKingdomId="p",IsReadyForPublication=true,SourceDocumentId=reply.DocumentId };
        o.CurrentStorage.Documents.Add(followup);
        provisional.PendingOffers.Add(new(){SourceDocumentId=followup.DocumentId,SourceActionId="action_1",Status="open"});
        var pending=new WorldDiplomacyJob { JobId="old_pending",RoundId=provisional.RoundId,Kind="generate",AuthorKingdomId="a",TargetKingdomId="p",IsRunning=true };
        DiplomacyEventRules.Bind(provisional,pending);o.CurrentStorage.Jobs.Add(pending);
        o.RequestLeases.TryClaim(pending,1,100,1000,true,out var lease);
        Call(o,"QueuePlayerReplyRoutingCheck",reply);var route=o.CurrentStorage.Jobs.Single(x=>x.Kind=="player_reply_routing");o.PrepareSharedRequest(route);
        Test.True(route.UserPrompt.Contains(source.DocumentId) && route.UserPrompt.Contains(reply.Body),"routing specialist sees the actual public question and eligible incoming artifact");
        o.TryCompleteDiplomacyPipeline(route,Result(route,"{\"answered_npc_document_ids\":[\"incoming\"]}"));
        Test.True(reply.RoundId==r.RoundId && r.NpcInitiatedPlayerEvent && provisional.State=="closed","actual semantic answer merges the whole declaration into the original case and retains the unique NPC slot");
        Test.True(r.PlayerNegotiationWaits[0].AnswerDocumentId==reply.DocumentId && reply.PlayerReplyRoutingRechecked,"publication stops only the matched source's silence wait");
        Test.True(h.Effects==0 && reply.PlayerAnalysisCommitted,"source attribution cannot replay committed mechanical actions");
        Test.True(followup.RoundId==r.RoundId && r.PendingOffers.Single().Status=="open","already published followup and its valid clauses migrate with the complete event");
        Test.True(pending.JobId!="old_pending" && pending.RoundId==r.RoundId && lease.Cancellation.IsCancellationRequested,"old pending work is rekeyed and its old transport cancellation is signaled");
        Test.True(!o.CurrentStorage.KingdomKnowledge.Any(x=>x.KingdomId=="a" && x.DocumentIds.Contains(reply.DocumentId)),"routing never delivers the player reply prematurely to NPC court");
        var multiple=Case("multiple");multiple.NpcInitiatedPlayerEvent=false;multiple.DialogueArrangementId="player_manual_declaration";o.CurrentStorage.ConcurrentRounds.Add(multiple);
        var unrelated=new WorldDiplomacyDocument{DocumentId="new_dispute",RoundId=multiple.RoundId,AuthorKingdomId="p",TargetKingdomId="a",IsPlayerAuthored=true,IsReadyForPublication=true,Body="A completely different question.",AnalysisStatus="success"};o.CurrentStorage.Documents.Add(unrelated);
        Call(o,"InvalidateDialogueIndex");Call(o,"QueuePlayerReplyRoutingCheck",unrelated);var empty=o.CurrentStorage.Jobs.Single(x=>x.Kind=="player_reply_routing");
        o.TryCompleteDiplomacyPipeline(empty,Result(empty,"{\"answered_npc_document_ids\":[]}"));
        Test.True(unrelated.RoundId==multiple.RoundId,"same target alone cannot force a new dispute into the existing case");
    }

    private static void OldAutomaticClosureRecovery()
    {
        var (h,o)=ConcurrentOralMigrationReplay.Fixture();var r=Case("legacy_early_close");r.State="closed";r.CloseReason="player_negotiation_finished";r.RootDocumentId="old_notice";
        var second=Case("legacy_battania");second.State="closed";second.CloseReason=r.CloseReason;second.RootDocumentId="second_notice";second.StartedDay=1;
        var state=new WorldDiplomacyStorage{PlayerDiplomacySchemaVersion=1};state.CompletedRounds.AddRange(new[]{r,second});
        var transactional=Case("expired_transaction");transactional.State="closed";transactional.CloseReason=r.CloseReason;transactional.RootDocumentId="expired_offer";
        transactional.PendingOffers.Add(new(){SourceDocumentId="expired_offer",Status="expired"});state.CompletedRounds.Add(transactional);
        var explicitEnd=Case("explicit_end");explicitEnd.State="closed";explicitEnd.CloseReason=r.CloseReason;explicitEnd.RootDocumentId="explicit_final";state.CompletedRounds.Add(explicitEnd);
        state.Documents.Add(new(){DocumentId="explicit_final",RoundId=explicitEnd.RoundId,AuthorKingdomId="a",IsReadyForPublication=true,RoundParticipation="withdraw",RoundStatus="resolved"});
        foreach(var item in new[]{r,second})state.Documents.Add(new(){DocumentId=item.RootDocumentId,RoundId=item.RoundId,AuthorKingdomId="a",TargetKingdomId="p",IsReadyForPublication=true,RoundParticipation="continue",RoundStatus="continue",ActualPlayerNoticeHour=1,FormalNoticeShown=true,IsNotified=true});
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(state))!);
        r=o.ResolveRound(r.RoundId);var job=o.CurrentStorage.Jobs.Single(x=>x.Kind=="player_lifecycle");
        Test.True(r.State=="active" && o.CurrentStorage.PlayerLifecycleRecoveryRoundIds.Count==1 && o.CurrentStorage.ConcurrentRounds.Count(x=>x.NpcInitiatedPlayerEvent)==1,"load recovery restores the earliest erroneous closure, preserving later artifacts without two active NPC openings");
        o.TryCompleteDiplomacyPipeline(job,Result(job,"{\"requires_response\":true,\"round_participation\":\"continue\",\"round_status\":\"continue\"}"));
        var doc=o.ResolveDocument(job.DocumentId);
        Test.True(doc.ActualPlayerNoticeHour<0 && !doc.FormalNoticeShown && r.PlayerNegotiationWaits.Single().NotifiedHour<0,"recovered obligation requires a fresh actual notification with no retrospective penalty");
        Test.True(o.CurrentStorage.Documents.Count==3 && h.Effects==0,"recovery never republishes the source or replays its actions");
        var copied=JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
        o.ReplaceStorage(copied);Test.True(o.CurrentStorage.ConcurrentRounds.Count(x=>x.NpcInitiatedPlayerEvent)==1,"a second save/load cannot recover the same closed identity twice");
        Test.True(o.CurrentStorage.CompletedRounds.Any(x=>x.RoundId==transactional.RoundId) && o.CurrentStorage.CompletedRounds.Any(x=>x.RoundId==explicitEnd.RoundId),"migration never revives expired transactional offers or explicitly concluded diplomacy");
    }

    private static void MultiCountryClosure()
    {
        var (h,o)=ConcurrentOralMigrationReplay.Fixture();var r=Case("multiple_parties");r.RootDocumentId="final_a";r.Participants[0].State="withdrawn";r.Participants.Add(new(){KingdomId="b",State="active"});
        o.CurrentStorage.ConcurrentRounds.Add(r);o.CurrentStorage.Documents.Add(new(){DocumentId=r.RootDocumentId,RoundId=r.RoundId,AuthorKingdomId="a",IsReadyForPublication=true,RoundParticipation="withdraw",RoundStatus="resolved"});
        o.ProcessRoundLifecycle();Test.True(r.State=="active","one country leaving does not finish the continuing counterpart");
        r.Participants.Last().State="withdrawn";h.CurrentDayValue++;o.ProcessRoundLifecycle();
        Test.True(r.State=="active" && r.EventEndingId=="","withdrawal alone cannot fabricate a conclusion or skip actual ending effects");
    }

    private static void RoutingBackpressureAndSave()
    {
        var (h,o)=ConcurrentOralMigrationReplay.Fixture();var sourceCase=Case("capacity_source");sourceCase.RootDocumentId="capacity_incoming";
        var provisional=Case("capacity_reply");provisional.NpcInitiatedPlayerEvent=false;provisional.DialogueArrangementId="player_manual_declaration";
        o.CurrentStorage.ConcurrentRounds.AddRange(new[]{sourceCase,provisional});
        o.CurrentStorage.Documents.Add(new(){DocumentId=sourceCase.RootDocumentId,RoundId=sourceCase.RoundId,AuthorKingdomId="a",TargetKingdomId="p",IsReadyForPublication=true,IsRead=true});
        var reply=new WorldDiplomacyDocument{DocumentId="capacity_published",RoundId=provisional.RoundId,AuthorKingdomId="p",TargetKingdomId="a",IsPlayerAuthored=true,IsReadyForPublication=true,AnalysisStatus="success",PlayerAnalysisCommitted=true};
        provisional.RootDocumentId=reply.DocumentId;o.CurrentStorage.Documents.Add(reply);
        for(int i=0;i<24;i++)o.CurrentStorage.Jobs.Add(new(){JobId="occupied_"+i,RoundId=sourceCase.RoundId,Kind="generate",SchedulingOwner="player"});
        Call(o,"QueuePlayerReplyRoutingCheck",reply);
        Test.True(reply.PlayerReplyRoutingPending && !o.CurrentStorage.Jobs.Any(x=>x.Kind=="player_reply_routing"),"full queue retains the published source as pending attribution instead of silently dropping it");
        var loaded=JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
        o.ReplaceStorage(loaded);reply=o.ResolveDocument(reply.DocumentId);o.CurrentStorage.Jobs.RemoveAt(0);
        Call(o,"MaintainPlayerReplyRoutingBacklog");var routing=o.CurrentStorage.Jobs.Single(x=>x.Kind=="player_reply_routing");
        Test.True(routing.DocumentId==reply.DocumentId && reply.PlayerReplyRoutingPending,"bounded backlog resumes persisted attribution when capacity becomes available");
        o.TryCompleteDiplomacyPipeline(routing,Result(routing,"{\"answered_npc_document_ids\":[\"capacity_incoming\"]}"));
        Test.True(reply.RoundId==sourceCase.RoundId && !reply.PlayerReplyRoutingPending,"resumed attribution completes the original handoff without republishing the player artifact");
    }
}
