using System.Reflection;
using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;

internal static class BilateralReplay
{
    private sealed class Host : ConcurrentOralMigrationReplay.Host
    {
        internal bool Enabled = true, FailId;
        public override bool WorldDiplomacyEnabled() => Enabled;
        public override string NewId(string prefix) => FailId && prefix == "diplomacy_round"
            ? throw new InvalidOperationException("injected_round_failure") : base.NewId(prefix);
    }
    private static (Host h, WorldDiplomacyOrchestration o) Fixture()
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o; return (h,o);
    }
    private static object? Invoke(WorldDiplomacyOrchestration o,string name,params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(o,args);
    private static DialogueInteractionOrigin Origin(string channel="native",string id="turn") => new(channel,id,"session","I agree", "I agree");
    private static DiplomacyBilateralResult Result(string state="applied", bool playerActor=false) => new() {
        State=state, Actor=playerActor?"p":"a", Target=playerActor?"a":"p", Terms=new(){DurationDays=14},Diagnostic=state };
    private const string Marker="[ACTION:DIPLOMACY:REVIEW_REMAINING]";
    private static void Complete(WorldDiplomacyOrchestration o,WorldDiplomacyJob job,JObject json) =>
        o.TryCompleteDiplomacyPipeline(job,new LlmJobResult {Success=true,Content=json.ToString()});
    internal static void Run()
    {
        ParsingAndReceipts(); SourceBinding(); MissingSourceBinding();
        foreach(string channel in new[]{"native","scene","courier"})
        {
            var(h,o)=Fixture(); int effects=0; var origin=Origin(channel);
            Test.True(o.CaptureSpeechReview("ruler_a","a",origin,""),"bilateral owns missing tag boundary "+channel);
            Test.True(o.CurrentStorage.Jobs.Count==0,"missing bilateral tag never queues another semantic extraction");
            o.ExecuteBilateral("ruler_a","a",DialogueDiplomaticAction.Trade,origin,()=>{effects++;return Result();});
            var item=o.CurrentStorage.DialogueArrangements.Single();
            o.ControlOralDiplomaticCommitment("ruler_a","a","arrangement="+item.ArrangementId+";state=cancelled;reason=private change",origin);
            Test.True(item.Status=="accepted"&&o.CurrentStorage.Jobs.Count==1,"executed factual result cannot be cancelled by commitment control");
            Test.True(effects==1 && item.DirectExecutionState=="applied" && item.Terms.DurationDays==14,"real receipt stored before publication");
            Test.True(item.MemoryReceipts.Count==1 && h.Facts.Count==1,"shared AFEF memory delivered once "+channel);
            Test.True(o.CurrentStorage.ConcurrentRounds.Count==1 && o.CurrentStorage.Jobs.Count==1,"one event and declaration task");
            o.ExecuteBilateral("ruler_a","a",DialogueDiplomaticAction.Trade,origin,()=>{effects++;return Result();});
            var saved=JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
            o.ReplaceStorage(saved); o.ExecuteBilateral("ruler_a","a",DialogueDiplomaticAction.Trade,origin,()=>{effects++;return Result();});
            Test.True(effects==1,"repeat and save/load cannot repeat an effect");
            item=saved.DialogueArrangements.Single();
            saved.Jobs.Clear(); item.GenerationFailureAttempts=3; Invoke(o,"MaintainDialogueArrangements");
            var doc=saved.Documents.Single();
            Test.True(doc.IsReadyForPublication && doc.Intent=="statement" && !doc.RequiresResponse,"result publishes without proposal acceptance");
            Test.True(h.TradeExecutions==0 && item.Status=="executed","announcement never calls treaty executor");
            o.ProcessAnalyzedDocument(doc,"accept_trade","acceptance",true,"neutral",1);
            Test.True(h.TradeExecutions==0,"duplicate callback cannot turn result announcement into effect");
        }
        foreach(string state in new[]{"rejected","partial","unknown"})
        {
            var(h,o)=Fixture(); int effects=0;
            o.ExecuteBilateral("ruler_a","a",DialogueDiplomaticAction.Peace,Origin(),()=>{effects++;return Result(state);});
            o.ExecuteBilateral("ruler_a","a",DialogueDiplomaticAction.Peace,Origin(),()=>{effects++;return Result();});
            Test.True(effects==1 && o.CurrentStorage.ConcurrentRounds.Count==0 && o.CurrentStorage.Jobs.Count==0,"no success announcement or replay for "+state);
        }
        {
            var(h,o)=Fixture(); int effects=0; h.FailId=true;
            try{o.ExecuteBilateral("ruler_a","a",DialogueDiplomaticAction.Trade,Origin(),()=>{effects++;return Result();});}catch(InvalidOperationException){}
            h.FailId=false; Invoke(o,"MaintainDialogueArrangements"); Invoke(o,"MaintainDialogueArrangements");
            Test.True(effects==1 && o.CurrentStorage.ConcurrentRounds.Count==1 && o.CurrentStorage.Jobs.Count==1 && h.Facts.Count==1,"post-effect failure recovers only fact/case/publication once");
        }
        foreach(string legacy in new[]{"third_party","complex","terminated","exact_pending"})
        {
            var(h,o)=Fixture(); int effects=0;
            o.CurrentStorage.SpeechReviews.Add(new(){Id="old",Ruler="ruler_a",Actor="a",PlayerKingdom="p",PlayerRuler="ruler_p",Status="technical_hold"});
            o.CurrentStorage.DialogueArrangements.Add(new(){ArrangementId="old",ActorKingdomId="a",TargetKingdomId=legacy=="third_party"?"b":"p",
                Action=legacy=="complex"?DialogueDiplomaticAction.Annexation:DialogueDiplomaticAction.Trade,Status=legacy=="terminated"?"published":"accepted"});
            o.ExecuteBilateral("ruler_a","a",DialogueDiplomaticAction.Trade,Origin(),()=>{effects++;return Result();});
            Test.True(effects==(legacy=="exact_pending"?0:1),"old matter isolation "+legacy);
        }
        {
            var(h,o)=Fixture(); int effects=0; h.Enabled=false;
            o.ExecuteBilateral("ruler_a","a",DialogueDiplomaticAction.Trade,Origin(),()=>{effects++;return Result();});
            Test.True(effects==0 && o.CurrentStorage.ConcurrentRounds.Count==0,"AI off adds no event or direct AI work");
            h.Enabled=true;h.PlayerSovereign="";
            Test.True(!o.IsBilateralConversation("ruler_a","a"),"non-ruler retains persuasion");
            Test.True(o.CaptureSpeechReview("ruler_a","a",Origin(),"") && o.CurrentStorage.Jobs.Count==1,"non-ruler untagged review retained");
        }
        foreach(string move in new[]{"NewMatter","AcceptProposal","RejectProposal"})
        {
            DialogueTagPayload.TryParse("action=Trade;move="+move+";target=p"+(move=="NewMatter"?"":";source_document=d"),out var parsed);
            Test.True(DiplomacyBilateralRouting.OwnsMatter(parsed,"a","p")== (move!="RejectProposal"),"operation capability routing "+move);
        }
        foreach(string action in new[]{"Peace","Alliance","Trade","DeclareWar","BreakAlliance","CancelTrade","Annexation","Tributary","Garrison","Vassal"})
        foreach(string target in new[]{"p","b"})
        {
            DialogueTagPayload.TryParse("action="+action+";move=NewMatter;target="+target,out var parsed);
            Test.True(DiplomacyBilateralRouting.OwnsMatter(parsed,"a","p")== (target=="p" && Array.IndexOf(new[]{"Peace","Alliance","Trade","DeclareWar","BreakAlliance","CancelTrade"},action)>=0),"matrix "+action+target);
        }
        foreach(bool legacy in new[]{false,true})
        {
            var(h,o)=Fixture();o.CaptureSpeechReview("ruler_a","a",Origin(),Marker);
            var item=o.CurrentStorage.SpeechReviews.Single();
            if(legacy){ item.ExcludeDirectBilateral=false;item.ExtractionContractVersion=3; o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!); }
            var job=o.CurrentStorage.Jobs.Single();o.PrepareSharedRequest(job);
            Test.True(job.SystemPrompt.Contains(DiplomacyPromptTexts.Get("Bilateral_ReviewExclusion","MISSING")),"mixed extraction gets scope exclusion "+legacy);
            var extraction=new JObject { ["unresolved"]=false,["commitments"]=new JArray(
                new JObject{["payload"]="action=Trade;move=NewMatter;target=p;days=14",["npc_quote"]="I agree",["player_quote"]="I agree",["player_agreed"]=true},
                new JObject{["payload"]="action=Trade;move=NewMatter;target=b;days=14",["npc_quote"]="I agree",["player_quote"]="",["player_agreed"]=false})};
            Complete(o,job,extraction);Complete(o,job,new JObject{["approved"]=true});
            if(job.Kind=="oral_review"){Complete(o,job,extraction);Complete(o,job,new JObject{["approved"]=true});}
            Test.True(o.CurrentStorage.DialogueArrangements.Count==1 && o.CurrentStorage.DialogueArrangements[0].TargetKingdomId=="b","mixed/legacy excludes bilateral but retains third party "+legacy);
        }
    }

    private static void ParsingAndReceipts()
    {
        Test.True(DiplomacyBilateralPayload.TryParse("MAKE_PEACE","P:A:12:14:A:P:town","p","a",out var canonical)
            &&canonical.Payload=="p:a:12:14:a:p:town"&&canonical.Terms.CessionFromKingdomId=="a","model spelling canonicalizes actual country identities before native execution and persistence");
        foreach(var test in new[]{("MAKE_PEACE","p:a:12:14",true),("MAKE_PEACE","a:p:12:14:a:p:town",true),
            ("MAKE_PEACE","a:p:12:14:a:p",false),("MAKE_PEACE","a:p:12:14:a:b:town",false),
            ("MAKE_PEACE","a:p:0:14",false),("MAKE_PEACE","a:p:auto:default",false),
            ("MAKE_PEACE","a:p:12:253",false),("FORM_ALLIANCE","a:p:default",true),("FORM_ALLIANCE","a:p:14",false),
            ("MAKE_TRADE","a:p:253",false),("MAKE_TRADE","a:p:default",true),("DECLARE_WAR","p:a",true),
            ("DECLARE_WAR","a:b",false),("BREAK_ALLIANCE","a:p:extra",false),
            ("MAKE_TRADE","a:p:14|document=diplomacy_document:abc;action=diplomacy_action:123",true),
            ("MAKE_TRADE","a:p:14|document=diplomacy_document:abc;action=",true),
            ("MAKE_TRADE","a:p:14|diplomacy_document:abc:",false)})
            Test.True(DiplomacyBilateralPayload.TryParse(test.Item1,test.Item2,"p","a",out _)==test.Item3,"production payload "+test.Item1+test.Item2);
        foreach(string payload in new[]{"action=Trade;move=NewMatter;target=b;days=14","action=Annexation;move=NewMatter;target=p;receiving=a;joining=p",
            "action=Trade;move=RejectProposal;target=p;source_document=diplomacy_document:123"})
            Test.True(DiplomacyBilateralRouting.NeedsRemainingReview("[ACTION:DIPLOMACY:COMMIT:"+payload+"]","a","p"),"valid retained COMMIT needs no marker");
        Test.True(!DiplomacyBilateralRouting.NeedsRemainingReview("[ACTION:DIPLOMACY:COMMIT:action=Trade;move=NewMatter;target=p]","a","p")
            && !DiplomacyBilateralRouting.NeedsRemainingReview("","a","p"),"bilateral COMMIT and missing tag never fall back");
        foreach(var status in new[]{WorldDiplomacyMakePeaceExecutionStatus.Applied,WorldDiplomacyMakePeaceExecutionStatus.PartiallyApplied,
            WorldDiplomacyMakePeaceExecutionStatus.UnknownAfterStart,WorldDiplomacyMakePeaceExecutionStatus.ActionNotApplied})
        foreach(string cession in new[]{"applied","owner_changed","unknown","throws"})
        {
            int peaceCalls=0,cessionCalls=0;
            var result=DiplomacyBilateralEffects.Peace(new(){CessionFromKingdomId="a",CessionToKingdomId="p",CessionSettlementId="town"},
                ()=>{peaceCalls++;return new(status,"a","p",12,14,"controlled");},
                ()=>{cessionCalls++;if(cession=="throws")throw new Exception("after_transfer");return new(true,cession=="applied",cession!="unknown",cession);});
            Test.True(peaceCalls==1&&cessionCalls==(status==WorldDiplomacyMakePeaceExecutionStatus.Applied?1:0),"cession follows complete peace only "+status+cession);
            Test.True((result.State=="applied")== (status==WorldDiplomacyMakePeaceExecutionStatus.Applied&&cession=="applied"),"partial package never becomes complete announcement");
            Test.True((result.Terms.CessionSettlementId=="town")== (result.State=="applied"),"only read-back cession enters actual terms");
        }
    }

    private static void SourceBinding()
    {
        foreach(string mode in new[]{"flat","action","wrong_terms","partial","rejected","forward_dependency","reverse_dependency","external_prerequisite"})
        {
            var(h,o)=Fixture();int effects=0;
            bool coupled=mode.Contains("dependency")||mode=="external_prerequisite";
            var round=o.EnsureActiveRound("a","p",false);string id="diplomacy_document:abcd-1234", action=mode=="action"||coupled?"diplomacy_action:one":"";
            var source=new WorldDiplomacyDocument{DocumentId=id,RoundId=round.RoundId,AuthorKingdomId="a",TargetKingdomId="p",IsReadyForPublication=true,Intent="propose_trade",TreatyTerms=new(){DurationDays=14}};
            if(action!="")source.Actions=new(){new(){ActionId=action,Intent="propose_trade",TargetKingdomId="p",TreatyTerms=new(){DurationDays=14}}};
            if(coupled)
            {
                source.Actions.Add(new(){ActionId="other",Intent="propose_alliance",TargetKingdomId="p"});
                if(mode=="reverse_dependency")source.Actions[1].DependencyActionIds=new(){action};
                else source.Actions[0].DependencyActionIds=new(){mode=="external_prerequisite"?"diplomacy_document:missing#missing":"other"};
            }
            o.CurrentStorage.Documents.Add(source);round.PendingOffers.Add(new(){SourceDocumentId=id,SourceActionId=action,ProposerKingdomId="a",TargetKingdomId="p",Intent="propose_trade",Status="open"});
            var old=new WorldDiplomacyDialogueArrangement{ArrangementId="old",ActorKingdomId="a",TargetKingdomId="p",RulerId="ruler_a",Action=DialogueDiplomaticAction.Trade,Status="published",DocumentId=id,RoundId=round.RoundId};
            o.CurrentStorage.DialogueArrangements.Add(old);
            Test.True(DiplomacyBilateralPayload.TryParse("MAKE_TRADE","a:p:"+(mode=="wrong_terms"?"15":"14")+"|document="+id+";action="+action,"p","a",out var request),"real IDs retain colon in optional source");
            o.ExecuteBilateral("ruler_a","a",DialogueDiplomaticAction.Trade,Origin(),()=>{effects++;return Result(mode is "partial" or "rejected"?mode:"applied");},request);
            Test.True(effects==(mode=="wrong_terms"||coupled?0:1),"source exact terms and conditions validated before effect "+mode);
            Test.True(round.PendingOffers.Single().Status==(mode=="partial"?"execution_uncertain":mode is "wrong_terms" or "rejected"||coupled?"open":"accepted"),"exact source settlement preserves failure semantics "+mode);
            if(coupled)
            {
                string payload="action=Trade;move=AcceptProposal;target=p;days=14;source_document="+id+";source_action="+action;
                Test.True(DialogueTagPayload.TryParse(payload,out var parsed)&&!o.OwnsDirectBilateralMatter(parsed,"a","p"),"coupled source retains original formal capability "+mode);
                o.CaptureSpeechReview("ruler_a","a",Origin(),"[ACTION:DIPLOMACY:COMMIT:"+payload+"]");
                Test.True(o.CurrentStorage.SpeechReviews.Count==1&&o.CurrentStorage.DialogueArrangements.Count==1&&old.Status=="published","coupled source can enter original review without changing existing proposal "+mode);
            }
            if(mode=="partial")Test.True((bool)Invoke(o,"HasUnfinishedEndingObligations",round)!,"unknown partial source cannot conclude as resolved");
            if(mode is "flat" or "action")Test.True(old.Status=="executed"&&o.CurrentStorage.ConcurrentRounds.Count==0,"exact pending source completes in same case without replay");
        }
    }

    private static void MissingSourceBinding()
    {
        {
            var(h,o)=Fixture();int effects=0;var round=o.EnsureActiveRound("a","p",false);
            foreach(string id in new[]{"proposal:one","proposal:two"})
            {
                o.CurrentStorage.Documents.Add(new(){DocumentId=id,RoundId=round.RoundId,AuthorKingdomId="a",TargetKingdomId="p",
                    IsReadyForPublication=true,Intent="propose_trade",TreatyTerms=new(){DurationDays=14}});
                round.PendingOffers.Add(new(){SourceDocumentId=id,ProposerKingdomId="a",TargetKingdomId="p",Intent="propose_trade",Status="open"});
                o.CurrentStorage.DialogueArrangements.Add(new(){ArrangementId=id,DocumentId=id,RoundId=round.RoundId,ActorKingdomId="a",TargetKingdomId="p",Action=DialogueDiplomaticAction.Trade,Status="published"});
            }
            o.ExecuteBilateral("ruler_a","a",DialogueDiplomaticAction.Trade,Origin(),()=>{effects++;return Result();});
            Test.True(effects==0,"two alternative proposals require an exact source");
            DiplomacyBilateralPayload.TryParse("MAKE_TRADE","a:p:14|document=proposal:one;action=","p","a",out var selected);
            o.ExecuteBilateral("ruler_a","a",DialogueDiplomaticAction.Trade,Origin(),()=>{effects++;return Result();},selected);
            Test.True(effects==1&&round.PendingOffers[0].Status=="accepted"&&round.PendingOffers[1].Status=="open"
                &&o.CurrentStorage.DialogueArrangements.Single(x=>x.ArrangementId=="proposal:two").Status=="published","explicit selection executes only one proposal and leaves the alternative unsettled");
        }
        foreach(string mode in new[]{"coupled","independent","third_country","other_action","closed","unknown_source","terminated_event"})
        {
            var(h,o)=Fixture();int effects=0;var round=o.EnsureActiveRound("a","p",false);
            string proposer=mode=="unknown_source"?"p":"a",target=proposer=="p"?"a":mode=="third_country"?"b":"p";
            string intent=mode=="other_action"?"propose_alliance":"propose_trade";
            var source=new WorldDiplomacyDocument{DocumentId="ordinary:proposal",RoundId=round.RoundId,AuthorKingdomId=proposer,
                TargetKingdomId=target,IsReadyForPublication=true,Intent=intent,Actions=new(){new(){ActionId="one",Intent=intent,TargetKingdomId=target,
                    DependencyActionIds=mode=="coupled"?new(){"unmet_prerequisite"}:new()}}};
            o.CurrentStorage.Documents.Add(source);
            round.PendingOffers.Add(new(){SourceDocumentId=source.DocumentId,SourceActionId="one",ProposerKingdomId=proposer,TargetKingdomId=target,
                Intent=intent,Status=mode=="closed"?"withdrawn":"open"});
            if(mode=="terminated_event")round.State="closed";
            o.ExecuteBilateral("ruler_a","a",DialogueDiplomaticAction.Trade,Origin(),()=>{effects++;return Result();});
            Test.True(effects==(mode is "coupled" or "independent"?0:1),"unbound ordinary proposals block only known live same-pair same-action conflict "+mode);
            Test.True(round.PendingOffers[0].Status==(mode=="closed"?"withdrawn":"open"),"no guessed proposal settlement "+mode);
        }
        foreach(string state in new[]{"partial","unknown"})
        {
            var(h,o)=Fixture();int effects=0;
            o.ExecuteBilateral("ruler_a","a",DialogueDiplomaticAction.Trade,Origin(),()=>{effects++;return Result(state);});
            o.ExecuteBilateral("ruler_a","a",DialogueDiplomaticAction.Trade,Origin(id:"another-turn"),()=>{effects++;return Result();});
            Test.True(effects==1,"unresolved effect cannot be blindly retried by changing interaction identity "+state);
        }
    }
}
