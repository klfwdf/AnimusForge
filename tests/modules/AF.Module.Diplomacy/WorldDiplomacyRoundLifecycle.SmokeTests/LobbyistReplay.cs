using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class LobbyistReplay
{
    internal sealed class Host : ConcurrentOralMigrationReplay.Host, IWorldDiplomacyLobbyistEvidence
    {
        internal bool Enabled = true, EvidenceThrows, HistoricalCase, IdThrows, CaptureThrows;
        internal List<DiplomacySponsorReceipt> Receipts = new();
        public override bool WorldDiplomacyEnabled() => Enabled;
        public override string PartyRulerId(string id)=>HistoricalCase&&id=="vlandia"?"lord_4_1":base.PartyRulerId(id);
        public override IReadOnlyList<string> AllKingdomIds()=>HistoricalCase?new[]{"vlandia","nord2","p"}:base.AllKingdomIds();
        public override string NewId(string prefix){if(IdThrows&&prefix=="diplomacy_round")throw new InvalidOperationException("id service");return base.NewId(prefix);}
        public IReadOnlyList<DiplomacySponsorReceipt> PlayerGoldReceipts(string ruler)
        { if(EvidenceThrows) throw new InvalidOperationException("receipt service"); return Receipts.Where(x=>x.Ruler==ruler).ToList(); }
    }
    internal sealed class DispatchState
    {
        internal Host Host;
        internal WorldDiplomacyOrchestration Owner;
        internal DialogueInteractionOrigin Origin;
        internal string Actor="a", Ruler="ruler_a";
        internal int Native, Bilateral;
    }
    internal readonly struct Source : IDiplomacyOralTagSource, IDiplomacyCommitmentTagSource,
        IDiplomacyCommitmentReviewSource, IDiplomacyLobbyistTagSource, IDiplomacyBilateralTagSource
    {
        readonly DispatchState s;
        internal Source(DispatchState state) => s=state;
        public bool HasSpeaker=>true;
        public bool IsAvailable=>true;
        public string SpeakerHeroId=>s.Ruler;
        public string SpeakerKingdomId=>s.Actor;
        public bool UseFormalCommitments=>s.Host.Enabled;
        public bool CaptureCommitmentReview(string tags)=>s.Host.CaptureThrows?throw new InvalidOperationException("legacy capture"):s.Owner.CaptureSpeechReview(s.Ruler,s.Actor,s.Origin,tags);
        public bool TryProcessLobbyist(string action,string payload)=>s.Owner.TryProcessLobbyistTag(s.Ruler,s.Actor,action,payload,s.Origin);
        public bool TryProcessBilateral(string action,string payload)
        {
            if(!s.Owner.IsBilateralConversation(s.Ruler,s.Actor)) return false;
            if(action=="MAKE_TRADE" && payload.StartsWith("a:p:")){s.Bilateral++;return true;}
            return action=="COMMIT" && DialogueTagPayload.TryParse(payload,out var p)&&s.Owner.OwnsDirectBilateralMatter(p,s.Actor,"p");
        }
        public string SubmitCommitment(string payload)=>s.Owner.SubmitOralDiplomaticCommitment(s.Ruler,s.Actor,payload,s.Origin);
        public string SubmitLegacyCommitment(string action,string payload)=>s.Owner.SubmitLegacyOralCommitment(s.Ruler,s.Actor,action,payload,s.Origin);
        public string ControlCommitment(string payload)=>"";
        public string DeclareWar(string p){s.Native++;return "";}
        public string MakePeace(string p){s.Native++;return "";}
        public string IndependentClanPeace(string p){s.Native++;return "";}
        public string FormAlliance(string p){s.Native++;return "";}
        public string BreakAlliance(string p){s.Native++;return "";}
        public string MakeTrade(string p){s.Native++;return "";}
        public string CancelTrade(string p){s.Native++;return "";}
        public void Log(string message){}
    }
    static DispatchState Fixture(string channel="native",bool monarch=false)
    {
        var h=new Host{PlayerSovereign=monarch?"p":"",PlayerAffiliation=monarch?"p":"a",WorldWar=true};
        var o=new WorldDiplomacyOrchestration(h,new WorldDiplomacyRuntimeState()); h.Owner=o;
        return new DispatchState{Host=h,Owner=o,Origin=new(channel,"turn1","session","I paid; proceed.","I will propose peace.")};
    }
    static void Dispatch(DispatchState s,string tags)
    {
        string text="Visible reply "+tags;
        DiplomacyOralTagApplication.Process(new Source(s),ref text);
        Test.True(text.Trim()=="Visible reply","tags never become speech");
    }
    static string Tag(string p)=>"[ACTION:DIPLOMACY:COMMIT:"+p+"]";
    static void Invoke(object o,string method,params object[] args)=>o.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(o,args);
    static string Peace="action=Peace;move=NewMatter;target=b";
    internal static void FullRun()
    {
        ReceiptProjection(); ContextAndMigration(); FinalBoundaries();
        foreach(string channel in new[]{"native","scene","courier"})
        {
            var s=Fixture(channel);
            Dispatch(s,"");
            Test.True(s.Owner.CurrentStorage.Jobs.Count==0 && s.Owner.CurrentStorage.DialogueArrangements.Count==0,"no tag cannot create or extract lobbyist action "+channel);
            // Actual 18:22:04 payload, after game-owned 18:21:39 receipt. Actor is
            // bound by the dispatch source; fixture maps a/b to vlandia/nord2.
            s.Host.Receipts.Add(DiplomacySponsorReceiptRules.Read("afef_player",ActualFact,"瓦兰迪亚壮年","德泰尔","ruler_a",s.Host.CurrentDay())!);
            Dispatch(s,Tag(Peace));
            var row=s.Owner.CurrentStorage.DialogueArrangements.Single();
            Test.True(row.IsLobbyistCommitment && row.ActorKingdomId=="a" && row.TargetKingdomId=="b" && row.Status=="accepted","real dispatcher registers fixed NPC commitment "+channel);
            Test.True(s.Owner.CurrentStorage.ConcurrentRounds.Count==1 && s.Owner.CurrentStorage.Jobs.Any(x=>x.DialogueArrangementId==row.ArrangementId),"deterministic event and declaration work "+channel);
            Test.True(!s.Owner.CurrentStorage.Jobs.Any(x=>x.Kind.StartsWith("oral_review")) && row.ConfirmedPlayerRulerId=="" && s.Host.Effects==0,"no extraction or invented consent/effect "+channel);
            Dispatch(s,Tag(Peace));
            var saved=JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(s.Owner.CurrentStorage))!;
            s.Owner.ReplaceStorage(saved); Dispatch(s,Tag(Peace));
            Test.True(saved.DialogueArrangements.Count==1 && saved.ConcurrentRounds.Count==1,"duplicate callback and load retain one commitment/case");
            var job=saved.Jobs.First(x=>x.DialogueArrangementId==row.ArrangementId);
            Test.True(!job.UserPrompt.Contains("I paid") && !job.UserPrompt.Contains("100000"),"private sponsorship absent from declaration input");
        }
        ConditionalAndClarification(); MixedAndLegacy(); RecoveryAndFairness(); EventAndPublication();
    }
    const string ActualFact="瓦兰迪亚壮年已经将 100000 第纳尔（合计总值约 100000 第纳尔） 交给 德泰尔。如果你正在和瓦兰迪亚壮年交易，并核对好了数量和价值，那么你现在可以将商量好的物品，第纳尔或人交给瓦兰迪亚壮年（本次交易动作记录#1）";
    static void ReceiptProjection()
    {
        var receipt=DiplomacySponsorReceiptRules.Read("afef_player",ActualFact,"瓦兰迪亚壮年","德泰尔","ruler_a",10);
        Test.True(receipt?.Gold==100000,"actual log-derived AFEF gold receipt parsed without total-value double count");
        foreach(string kind in new[]{"player","npc","afef_npc","system"})
            Test.True(DiplomacySponsorReceiptRules.Read(kind,ActualFact,"瓦兰迪亚壮年","德泰尔","ruler_a",10)==null,"quoted/spoofed AFEF never proves payment "+kind);
        Test.True(DiplomacySponsorReceiptRules.Read("afef_player",ActualFact.Replace("已经将","愿意将"),"瓦兰迪亚壮年","德泰尔","ruler_a",10)==null,"promise not payment");
        Test.True(DiplomacySponsorReceiptRules.Read("afef_player","Player has given 100000 denars (total value approx. 100000 denars) to Ruler. If you are trading with Player and have verified the quantity and value, you may now deliver the agreed items, denars, or people to Player (trade action record #1)","Player","Ruler","ruler_a",10)?.Gold==100000,"EN actual template receipt");
    }
    static void ConditionalAndClarification()
    {
        var s=Fixture(); string p=Peace+";condition=player_gold;sponsor_gold=100000";
        Dispatch(s,Tag(p)); var row=s.Owner.CurrentStorage.DialogueArrangements.Single();
        Test.True(row.Status=="waiting_condition"&&s.Owner.CurrentStorage.ConcurrentRounds.Count==0,"unpaid conditional promise retained without event/effect");
        s.Origin=new("courier","turn2","session","done","Agreed.");
        Dispatch(s,Tag(Peace+";continuation="+row.ArrangementId));
        Test.True(row.Status=="waiting_condition","short confirmation cannot drop payment");
        s.Owner.ObserveSponsorPayment("courier_gold_actual","ruler_a",100000);
        s.Owner.ObserveSponsorPayment("courier_gold_actual","ruler_a",100000);
        Dispatch(s,Tag(p+";continuation="+row.ArrangementId));
        Test.True(row.Status=="accepted"&&row.SponsorReceipt=="courier_gold_actual"&&row.Terms.DailyTribute==0,"cross-channel paid confirmation promotes same item without tribute or charge");
        Test.True(s.Owner.CurrentStorage.SponsorReceipts.Count==1&&s.Owner.CurrentStorage.DialogueArrangements.Count==1,"duplicate receipt and same task identity");
        Dispatch(s,Tag("action=Peace;move=NewMatter;target=c;condition=player_gold;sponsor_gold=100000;sponsor_receipt=courier_gold_actual"));
        Test.True(s.Owner.CurrentStorage.DialogueArrangements.Last().Status=="waiting_condition","one payment cannot fund another commitment");
        s=Fixture(); s.Host.OralEffectPortsEnabled=true; string missing=Peace+";payer=a;receiver=b;tribute=10";
        Dispatch(s,Tag(missing)); row=s.Owner.CurrentStorage.DialogueArrangements.Single();
        Test.True(row.Status=="waiting_clarification","missing period held");
        Dispatch(s,Tag(missing+";days=14;continuation="+row.ArrangementId));
        Test.True(row.Status=="accepted"&&row.Terms.DurationDays==14,"missing period completed in same item");
        s=Fixture(); Dispatch(s,Tag(Peace+";condition=clarify")); row=s.Owner.CurrentStorage.DialogueArrangements.Single();
        Dispatch(s,Tag(Peace+";condition=player_gold;sponsor_gold=100000;continuation="+row.ArrangementId));
        Test.True(row.Status=="waiting_condition"&&row.SponsorGold==100000,"clarification becomes supported condition, not locked forever");
    }
    static void MixedAndLegacy()
    {
        var s=Fixture(monarch:true);
        Dispatch(s,"[ACTION:DIPLOMACY:MAKE_TRADE:a:p:14]"+Tag(Peace));
        Test.True(s.Bilateral==1&&s.Owner.CurrentStorage.DialogueArrangements.Single().TargetKingdomId=="b","mixed direct and lobbyist dispatch separately");
        s=Fixture(); s.Owner.CurrentStorage.SpeechReviews.Add(new(){Id="old",Actor="a",Ruler="ruler_a",NpcText="conditional old",Status="conditional"});
        s.Owner.CurrentStorage.Jobs.Add(new(){JobId="old_job",Kind="oral_review_audit",DocumentId="old"});
        Invoke(s.Owner,"MaintainSpeechReviews");
        Test.True(s.Owner.CurrentStorage.SpeechReviews.Single().Status=="legacy_lobbyist_evidence"&&s.Owner.CurrentStorage.Jobs.Count==0,"legacy evidence retained with old execution authority retired");
        Dispatch(s,Tag(Peace));
        Test.True(s.Owner.CurrentStorage.DialogueArrangements.Count==1,"fresh COMMIT does not re-review old text");
        foreach(string bad in new[]{"action=Peace;move=NewMatter;target=a","action=Peace;move=NewMatter;target=b;actor=c","action=Peace;move=NewMatter;target=b;sponsor_gold=3"})
        {s=Fixture(); Dispatch(s,Tag(bad)); Test.True(s.Owner.CurrentStorage.ConcurrentRounds.Count==0,"invalid identity/condition rejected "+bad);}
        s=Fixture();s.Ruler="impostor"; Dispatch(s,Tag(Peace));Test.True(s.Owner.CurrentStorage.DialogueArrangements.Count==0,"actual current ruler required");
        s=Fixture();s.Host.Enabled=false; Dispatch(s,Tag(Peace+";condition=player_gold;sponsor_gold=100000"));
        Test.True(s.Native==0&&s.Owner.CurrentStorage.DialogueArrangements.Count==0,"AI-off never ignores new conditions");
        Dispatch(s,Tag(Peace));Test.True(s.Native==1,"AI-off legacy native fallback retained");
        s=Fixture();Dispatch(s,"[ACTION:DIPLOMACY:MAKE_PEACE:a:b:0:0]");Test.True(s.Owner.CurrentStorage.DialogueArrangements.Count==0,"native player tag never creates lobbyist commitment");
    }
    static void RecoveryAndFairness()
    {
        var s=Fixture();
        for(int n=0;n<6;n++) Dispatch(s,Tag("action=Peace;move=NewMatter;target=target"+n+";condition=player_gold;sponsor_gold="+(100+n)));
        s.Owner.ObserveSponsorPayment("sixth","ruler_a",105);
        // Explicit receipt IDs distinguish all amounts and avoid giving earlier
        // promises the sixth payment merely because it is large enough.
        foreach(var row in s.Owner.CurrentStorage.DialogueArrangements) row.LobbyistPayload+=";sponsor_receipt="+(row.SponsorGold==105?"sixth":"missing"+row.SponsorGold);
        Invoke(s.Owner,"MaintainDialogueArrangements");s.Host.CurrentDayValue++;
        Invoke(s.Owner,"MaintainDialogueArrangements");
        Test.True(s.Owner.CurrentStorage.DialogueArrangements.Last().Status=="accepted","more than four pending items receive fair daily recovery");
        s.Host.CurrentDayValue+=400;Invoke(s.Owner,"MaintainDialogueArrangements");
        Test.True(s.Owner.CurrentStorage.DialogueArrangements.Count(x=>x.Status=="waiting_condition")==5,"yearly cleanup cannot erase unfinished conditions");
        s=Fixture(monarch:true);s.Host.EvidenceThrows=true;
        Dispatch(s,Tag(Peace+";condition=player_gold;sponsor_gold=1")+"[ACTION:DIPLOMACY:MAKE_TRADE:a:p:14]");
        Test.True(s.Bilateral==1&&s.Owner.CurrentStorage.DialogueArrangements.Count==1,"receipt failure preserves current item and continues unrelated tag");
    }
    internal static void EventAndPublication()
    {
        foreach(string mode in new[]{"new","existing","unknown","cross_country","closed"})
        {
            var s=Fixture();s.Host.PublishEnabled=true;s.Host.OralEffectPortsEnabled=true;
            var prior=s.Owner.EnsureActiveRound("a",mode=="cross_country"?"c":"b",false);
            var source=new WorldDiplomacyDocument{DocumentId="public_case",RoundId=prior.RoundId,AuthorKingdomId="a",TargetKingdomId=mode=="cross_country"?"c":"b",IsReadyForPublication=true,Intent="statement",Body="A known border dispute."};
            s.Owner.CurrentStorage.Documents.Add(source);
            if(mode=="closed") prior.State="closed";
            string payload=Peace+(mode=="new"?"":";event="+(mode=="unknown"?"missing":prior.RoundId));
            Dispatch(s,Tag(payload));var row=s.Owner.CurrentStorage.DialogueArrangements.Single();
            if(mode is "unknown" or "cross_country" or "closed")
            {Test.True(row.Status=="waiting_clarification"&&s.Owner.CurrentStorage.ConcurrentRounds.Count==0,"invalid explicit case is held, never silently rebound "+mode);continue;}
            Test.True((row.RoundId==prior.RoundId)==(mode=="existing"),"explicit validated case or independent new decision "+mode);
            var writing=s.Owner.CurrentStorage.Jobs.Single(x=>x.DialogueArrangementId==row.ArrangementId);
            s.Owner.PrepareSharedRequest(writing);
            s.Owner.TryCompleteDiplomacyPipeline(writing,new LlmJobResult{Success=true,Content="{\"safe\":true,\"public_context\":\"The ruler decided to propose peace.\"}"});
            s.Owner.PrepareSharedRequest(writing);
            string draft=(string)typeof(WorldDiplomacyOrchestration).GetMethod("BuildCommittedDeclaration",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(s.Owner,new object[]{row})!;
            s.Owner.CommitGeneratedDocument(writing,draft);
            s.Owner.TryCompleteDiplomacyPipeline(writing,new LlmJobResult{Success=true,Content="{\"safe\":true,\"agreement_consistent\":true}"});
            var published=s.Owner.ResolveDocument(row.DocumentId);
            Test.True(published?.IsReadyForPublication==true && published.RequiresResponse && !published.ChangedDiplomaticState && s.Host.Effects==0,"peace publishes a real proposal and awaits target acceptance "+mode);
            Test.True(published.AuthorKingdomId=="a"&&published.TargetKingdomId=="b"&&!published.Body.Contains("paid"),"fixed parties and private source isolation");
            s.Owner.CommitGeneratedDocument(writing,draft);
            Test.True(s.Owner.CurrentStorage.Documents.Count(x=>x.DialogueArrangementId==row.ArrangementId)==1,"late generated result cannot republish");
            var round=s.Owner.ResolveRound(row.RoundId);
            var offer=round.PendingOffers.Single(x=>x.SourceDocumentId==published.DocumentId);
            var acceptance=new WorldDiplomacyDocument{DocumentId="target_accept_peace",RoundId=round.RoundId,AuthorKingdomId="b",TargetKingdomId="a",Intent="accept_peace",RespondingToOfferDocumentId=published.DocumentId,RespondingToOfferActionId=offer.SourceActionId};
            Test.True(s.Owner.TrySettleRelayOffer(acceptance)==WorldDiplomacyOfferOutcome.Applied&&s.Host.Effects==1,"only exact target acceptance applies the published peace proposal "+mode);
            s.Owner.TrySettleRelayOffer(acceptance);
            Test.True(s.Host.Effects==1,"duplicate target acceptance cannot execute peace twice "+mode);
        }
        foreach(string move in new[]{"AcceptProposal","RejectProposal"})
        {
            var s=Fixture();s.Host.PublishEnabled=true;s.Host.OralEffectPortsEnabled=true;
            var round=s.Owner.EnsureActiveRound("b","a",false);
            var source=new WorldDiplomacyDocument{DocumentId="proposal_b",RoundId=round.RoundId,AuthorKingdomId="b",TargetKingdomId="a",IsReadyForPublication=true,Intent="propose_peace"};
            s.Owner.CurrentStorage.Documents.Add(source);
            round.PendingOffers.Add(new(){SourceDocumentId=source.DocumentId,ProposerKingdomId="b",TargetKingdomId="a",Intent="propose_peace",Status="open"});
            AnimusForge.Refactor.Domain.WorldDiplomacyDocumentFactRules.RecordKingdomKnowledge(s.Owner.CurrentStorage.KingdomKnowledge,"a",source.DocumentId,12);
            string p="action=Peace;move="+move+";target=b";
            Dispatch(s,Tag(p));var row=s.Owner.CurrentStorage.DialogueArrangements.Single();
            Test.True(row.Status=="waiting_clarification","missing exact source held "+move);
            Dispatch(s,Tag(p+";source_document=proposal_b;continuation="+row.ArrangementId));
            Test.True(row.Status=="accepted"&&row.RoundId==round.RoundId&&row.SourceDocumentId=="proposal_b"&&s.Owner.CurrentStorage.ConcurrentRounds.Count==0,"later exact source completes same pending item "+move);
        }
    }
    internal struct ContextSource : IDiplomacyPostprocessContextSource, IDiplomacyOralPostprocessSource
    {
        internal bool Enabled, Independent;
        internal WorldDiplomacyOrchestration Owner;
        public bool HasSpeaker=>true;
        public bool UseFormalCommitments=>Enabled;
        public bool TryCaptureIndependentPeace(out DiplomacyIndependentPeaceContextSnapshot snapshot)
        {snapshot=new("clan",1,"ruler a");return Independent;}
        public DiplomacyConversationEligibilitySnapshot CaptureEligibility()=>new(true,false,false,true,false,true,false,false,true,false);
        public DiplomacyPostprocessKingdomSnapshot CaptureKingdoms()=>new(true,"a","A",false,false,"","",false,true,new[]{new DiplomacyKingdomSummary("b","B",false)});
        public string GetAnnexationHint()=>"";
        public bool ArePlayerAndNpcAtWar()=>true;
        public int CalculateDailyTribute(bool npcPays)=>0;
        public void LogFailure(string message)=>throw new Exception(message);
        public string NativeActionInstruction()=>"native-only";
        public string OralArrangementContext()=>Owner.BuildOralArrangementContext("ruler_a","a");
    }
    static void ContextAndMigration()
    {
        var s=Fixture();
        foreach(bool independent in new[]{false,true})
        {
            var source=new ContextSource{Enabled=true,Independent=independent,Owner=s.Owner};
            string post= DiplomacyPostprocessContextApplication.Build(ref source);
            Test.True(post.Contains("[ACTION:DIPLOMACY:COMMIT:")&&post.Contains(DiplomacyPromptTexts.Get("Lobbyist_Protocol","missing")),"actual postprocess consumer includes full lobbyist protocol, independent="+independent);
            if(independent) Test.True(post.Contains(DiplomacyPromptTexts.Get("AF_DIP_IndependentPeaceFacts","missing")),"independent peace facts coexist without replacing lobbyist facts");
            source.Enabled=false;post=DiplomacyPostprocessContextApplication.Build(ref source);
            Test.True(!post.Contains(DiplomacyPromptTexts.Get("Lobbyist_Protocol","missing")),"AI off doesn't acquire formal lobbyist protocol");
        }
        Test.True(!s.Owner.BuildOralArrangementContext("ruler_a","a").Contains(DiplomacyPromptTexts.Get("Lobbyist_Protocol","missing")),"main-reply context never instructs COMMIT output");
        foreach(string mode in new[]{"third","unknown","mixed","approved_complex"})
        {
            s=Fixture(monarch:true);
            string local="action=Annexation;move=NewMatter;target=p;receiving=a;joining=p";
            var evidence=new DiplomacySpeechReview{Id="old",Actor="a",Ruler="ruler_a",PlayerKingdom="p",PlayerRuler="ruler_p",NpcText="I agree",PlayerText="Yes",ExtractionContractVersion=4,
                TagHints=mode=="third"?Tag(Peace):mode=="mixed"?Tag(local)+Tag(Peace):"[ACTION:DIPLOMACY:REVIEW_REMAINING]",
                Approved=mode=="approved_complex"?new JObject{["unresolved"]=false,["commitments"]=new JArray(new JObject{["payload"]=local,["npc_quote"]="I agree",["player_quote"]="",["player_agreed"]=false})}.ToString():""};
            var oldJob=new WorldDiplomacyJob{JobId="old_job",Kind="oral_review_audit",DocumentId="old",PrivacyDraft=evidence.Approved};
            s.Owner.CurrentStorage.SpeechReviews.Add(evidence);s.Owner.CurrentStorage.Jobs.Add(oldJob);
            Invoke(s.Owner,"MaintainSpeechReviews");
            bool retained=mode is "mixed" or "approved_complex";
            Test.True((evidence.Status=="legacy_lobbyist_evidence")!=retained,"legacy classification preserves only proven self-country complex responsibility "+mode);
            if(!retained)
            {
                s.Owner.TryCompleteDiplomacyPipeline(oldJob,new LlmJobResult{Success=true,Content="{\"approved\":true}"});
                Test.True(s.Owner.CurrentStorage.DialogueArrangements.Count==0&&s.Owner.CurrentStorage.Jobs.Count==0,"retired late result cannot register "+mode);
            }
        }
        foreach(string invalid in new[]{";event=wrong",";days=14"})
        {
            s=Fixture(); Dispatch(s,Tag(Peace+invalid));var row=s.Owner.CurrentStorage.DialogueArrangements.Single();
            string correction=invalid.StartsWith(";event")?";event=":";days=0";
            Dispatch(s,Tag(Peace+correction+";continuation="+row.ArrangementId));
            Test.True(row.Status=="accepted","explicitly correct only program-rejected field "+invalid);
        }
        foreach(string sourceKind in new[]{"announcement","multi_action"})
        {
            s=Fixture();var round=s.Owner.EnsureActiveRound("a","b",false);
            if(sourceKind=="announcement") s.Owner.CurrentStorage.EventAnnouncements.Add(new(){Id="started",EventId=round.RoundId,State="public",Stage="started",Body="The border dispute opens."});
            else s.Owner.CurrentStorage.Documents.Add(new(){DocumentId="multi",RoundId=round.RoundId,AuthorKingdomId="a",TargetKingdomId="c",IsReadyForPublication=true,Actions=new(){new(){ActionId="b_action",Intent="statement",TargetKingdomId="b"}}});
            Dispatch(s,Tag(Peace+";event="+round.RoundId));
            Test.True(s.Owner.CurrentStorage.DialogueArrangements.Single().RoundId==round.RoundId,"known case source accepted "+sourceKind);
        }
    }
    static void FinalBoundaries()
    {
        var s=Fixture();s.Host.HistoricalCase=true;s.Host.PlayerAffiliation="vlandia";s.Actor="vlandia";s.Ruler="lord_4_1";
        string raw="action=Peace;move=NewMatter;target=nord2";
        string actual="[AFEF玩家行为补充] 瓦兰迪亚壮年已经将 100000 第纳尔（合计总值约 100000 第纳尔） 交给 德泰尔。如果你正在和瓦兰迪亚壮年交易，并核对好了数量和价值，那么你现在可以将商量好的物品，第纳尔或人交给瓦兰迪亚壮年（本次交易动作记录#1）";
        var receipt=DiplomacySponsorReceiptRules.Read("afef_player",actual.Substring("[AFEF玩家行为补充] ".Length),"瓦兰迪亚壮年","德泰尔",s.Ruler,s.Host.CurrentDay());
        Test.True(receipt!=null&&receipt.Gold==100000,"actual 18:21:39 typed receipt parsed after removing display-only log prefix, without inventing payment");s.Host.Receipts.Add(receipt);
        var legacy=new DiplomacySpeechReview{Id="real_legacy",Ruler=s.Ruler,Actor=s.Actor,PlayerKingdom="vlandia",PlayerRuler="",Status="conditional",TagHints=Tag(raw),NpcText="legacy_marker",ExtractionContractVersion=4};
        s.Owner.CurrentStorage.SpeechReviews.Add(legacy);Dispatch(s,Tag(raw));
        var arrangement=s.Owner.CurrentStorage.DialogueArrangements.Single();
        Test.True(arrangement.ActorKingdomId=="vlandia"&&arrangement.TargetKingdomId=="nord2"&&arrangement.RulerId=="lord_4_1"&&arrangement.Status=="accepted","actual 18:22:04 bare COMMIT binds real NPC authority and intended target");
        Test.True(arrangement.Terms.DailyTribute==0&&s.Host.WorldWar&&s.Host.Effects==0,"private 100000 neither national tribute nor instant peace");
        Test.True(legacy.Status=="continued_by_tag"&&legacy.Reason==arrangement.ArrangementId,"single exact legacy evidence receives explicit supersession link");
        s.Owner.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(s.Owner.CurrentStorage)));Invoke(s.Owner,"MaintainSpeechReviews");
        Test.True(s.Owner.CurrentStorage.SpeechReviews.Single().Status=="continued_by_tag"&&!s.Owner.BuildOralArrangementContext(s.Ruler,s.Actor).Contains("legacy_marker"),"reloaded continued evidence stays terminal and out of pending conversation");
        Test.True(!s.Owner.CurrentStorage.Jobs.Any(x=>x.Kind.StartsWith("oral_review")),"retired legacy queue never returns after load");
        s=Fixture();s.Host.IdThrows=true;Dispatch(s,Tag(Peace));var pending=s.Owner.CurrentStorage.DialogueArrangements.Single();
        Test.True(pending.Reason.StartsWith("registration_retry_")&&pending.Status=="waiting_clarification","event-registration exception remains durable on original item");
        s.Host.IdThrows=false;s.Host.CurrentDayValue++;Invoke(s.Owner,"MaintainDialogueArrangements");
        Test.True(pending.Status=="accepted"&&s.Owner.CurrentStorage.DialogueArrangements.Count==1,"bounded scheduled registration retry promotes original item");
        s=Fixture();Dispatch(s,Tag(Peace+";condition=clarify"));pending=s.Owner.CurrentStorage.DialogueArrangements.Single();
        for(int i=0;i<2;i++)Dispatch(s,Tag(Peace+";condition=none;continuation="+pending.ArrangementId));
        Test.True(pending.Status=="waiting_clarification"&&s.Owner.CurrentStorage.Jobs.Count==0,"unsupported private condition cannot be erased by repeated none confirmation");
        s=Fixture();s.Owner.ObserveSponsorPayment("payment_first","ruler_a",100000);s.Owner.ObserveSponsorPayment("payment_second","ruler_a",100000);
        string paid=Peace+";condition=player_gold;sponsor_gold=100000;sponsor_receipt=payment_first";Dispatch(s,Tag(paid));
        s.Origin=new("native","newturn","session","same request","same commitment");Dispatch(s,Tag(paid));
        Test.True(s.Owner.CurrentStorage.DialogueArrangements.Count==1,"later duplicate cannot allocate another payment condition or commitment");
        Dispatch(s,Tag("action=Peace;move=NewMatter;target=c;condition=player_gold;sponsor_gold=100000;sponsor_receipt=payment_second"));
        Test.True(s.Owner.CurrentStorage.DialogueArrangements.Count(x=>x.Status=="accepted")==2,"two distinct actual payments on same day can support two explicit commitments");
        s.Owner.CurrentStorage.DialogueArrangements.Clear();s.Host.CurrentDayValue+=400;s.Owner.ObserveSponsorPayment("new_payment","ruler_a",1);
        s.Owner.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(s.Owner.CurrentStorage)));
        s.Origin=new("native","after_archive","session","reuse old money","I agree");Dispatch(s,Tag(paid));
        Test.True(s.Owner.CurrentStorage.DialogueArrangements.Single().Status=="waiting_condition","durable payment claim survives arrangement archival, receipt pruning and reload");
        s=Fixture();s.Host.Receipts.Add(new(){Id="afef_gold_old",Ruler="ruler_a",Gold=100000,Day=s.Host.CurrentDay()});
        Dispatch(s,Tag(Peace+";condition=player_gold;sponsor_gold=100000;sponsor_receipt=afef_gold_old"));
        s.Owner.ObserveSponsorPayment("new_same_amount","ruler_a",100000);
        s.Host.Receipts.Add(new(){Id="afef_gold_new_rendering",Ruler="ruler_a",Gold=100000,Day=s.Host.CurrentDay()});
        Dispatch(s,Tag("action=Peace;move=NewMatter;target=c;condition=player_gold;sponsor_gold=100000;sponsor_receipt=new_same_amount"));
        Test.True(s.Owner.CurrentStorage.DialogueArrangements.Count(x=>x.Status=="accepted")==2,"old imported payment and later same-day same-amount actual transfer are distinct usable payments");
        Dispatch(s,Tag("action=Peace;move=NewMatter;target=p;condition=player_gold;sponsor_gold=100000;sponsor_receipt=afef_gold_new_rendering"));
        Test.True(s.Owner.CurrentStorage.DialogueArrangements.Last().Status=="waiting_condition"&&s.Owner.CurrentStorage.SponsorReceipts.Count==2,"new transfer's later AFEF projection is not a third spendable payment");
        s=Fixture();s.Host.Enabled=false;s.Owner.ObserveSponsorPayment("off","ruler_a",100000);s.Host.Enabled=true;s.Owner.ObserveSponsorPayment("not_ruler","companion",100000);
        Test.True(s.Owner.CurrentStorage.SponsorReceipts.Count==0,"receipt observer stays inactive for AI-off and non-ruler gifts");
        s=Fixture();s.Host.CaptureThrows=true;Dispatch(s,Tag(Peace));
        Test.True(s.Owner.CurrentStorage.DialogueArrangements.Single().Status=="accepted","failed legacy capture cannot suppress independent lobbyist item");
    }
}
