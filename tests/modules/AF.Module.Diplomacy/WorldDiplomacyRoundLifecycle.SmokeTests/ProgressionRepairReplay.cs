using AnimusForge;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    private sealed class ProgressionHost : Host, IDiplomacyEventHost, IDiplomacySilenceEffectHost
    {
        internal List<DiplomacyEventStage> EventNotices = new();
        public IReadOnlyList<string> KingdomTownIds(string id) => new[] { "town_" + id };
        public new bool TryReadEventEffect(DiplomacyEventEffectState e, out float v, out float min, out float max)
        {
            base.TryReadEventEffect(e, out v, out min, out max);
            return e.Kind == "ruler_relation" || e.SettlementId == "town_" + e.KingdomId;
        }
        public new bool ShowEvent(DiplomacyEventPresentation data, out string image, Action ack, IReadOnlyList<string> recent)
        { EventNotices.Add(data.Stage); image = ""; ack(); return true; }
    }

    internal static void ProgressionRepair(string incidentPath = null)
    {
        LegacyClosureRecovery();
        JArray incidents = incidentPath == null ? new() : JArray.Parse(File.ReadAllText(incidentPath));
        JObject Actual(string time, JObject fallback) => incidents.OfType<JObject>().FirstOrDefault(x => (string)x["time"]! == time)?["draft"] as JObject ?? fallback;
        string firstBody = (string)Actual("17:41:17", new() { ["body"] = "We claim recognition. We reject the rival claim." })["body"]!;
        string secondBody = (string)Actual("17:41:52", new() { ["body"] = "We reject your claim. Our own claim stands." })["body"]!;
        var host = new ProgressionHost { PublishEnabled = true, PlayerAwareExecution = true, OralRange = (0,3000), CurrentDayValue = 40 };
        var (_,o,r,j) = EvidenceOpening(host);
        var firstEvidence = SubmitEvidenceDraft(o,j,firstBody,false);
        Complete(o,j,SpanReview(firstEvidence,true));
        var first=o.CurrentStorage.Documents.Single(x=>x.IsReadyForPublication);
        host.CurrentDayValue+=20;o.ProcessPropagationArrivals();Call(o,"ReconcileEventQueue");
        var reply=o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"&&x.AuthorKingdomId=="b");
        var evidence=SubmitEvidenceDraft(o,reply,secondBody,false);string original=reply.PrivacyDraft;
        var omitted=Actual("17:42:00",SpanReview(evidence,true));
        Complete(o,reply,omitted);
        bool legacyReview=reply.PrivacyRepairFeedback.Contains("position_owner_valid_boolean_missing");
        if(legacyReview)
        {
            Test.True(reply.Kind=="privacy_audit"&&reply.PrivacyDraft==original,"unaltered old audit requires the new ownership dimension");
            Complete(o,reply,SpanReview(evidence,true)); // Explicit controlled follow-up still omits the answer.
        }
        Test.True(reply.Kind=="privacy_audit" && reply.PrivacyDraft==original && reply.PrivacyRewrites==0
            && reply.PrivacyAuditRetries==(legacyReview?2:1) && reply.PrivacyRepairFeedback.Contains(first.DocumentId),
            "actual approved-but-empty answer audit repairs same body with exact required source, never rewrites prose");
        Complete(o,reply,SpanReview(Evidence(o,reply),true,first.DocumentId));
        var second=o.CurrentStorage.Documents.Single(x=>x.AuthorKingdomId=="b"&&x.IsReadyForPublication);
        Test.True(second.Body==secondBody,"corrected independent evidence publishes the original reply unchanged");
        host.CurrentDayValue+=20;o.ProcessPropagationArrivals();Call(o,"ReconcileEventQueue");
        var thirdJob=o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"&&x.AuthorKingdomId=="a");
        var thirdEvidence=SubmitEvidenceDraft(o,thirdJob,(string)Actual("17:43:14",new(){["body"]="We reject your counterclaim. Our demand remains."})["body"]!,false);
        Complete(o,thirdJob,SpanReview(thirdEvidence,false,second.DocumentId));
        host.CurrentDayValue+=20;o.ProcessPropagationArrivals();Call(o,"ReconcileEventQueue");
        Test.True(!o.CurrentStorage.Jobs.Any(x=>x.RoundId==r.RoundId),"three real delivered informational declarations leave no required speaker job");
        var progress=new WorldDiplomacyJob{JobId="closure-proof",RoundId=r.RoundId,Kind="event_progress",AuthorKingdomId="a"};
        o.CurrentStorage.Jobs.Add(progress);
        var unsupported=Actual("17:43:35",new(){["outcome"]="resolved",["reason"]="Both parties have exchanged statements."});
        Complete(o,progress,unsupported);
        Test.True(r.EventPhase!="closing"&&progress.PrivacySourceMaterial.StartsWith("event_closure_evidence_required"),
            "actual three-document resolved judgment cannot close without claim-by-claim evidence");
        var closure=new JObject{["outcome"]="resolved",["reason"]="Neither claimant concedes; no new terms remain.",
            ["positions"]=new JArray(Position("a"),Position("b")),
            ["closure"]=new JArray(new[]{"a","b"}.Select(id=>new JObject{["kingdom"]=id,["status"]="unresolved",
                ["reason"]="Recognition remains disputed after explicit rejection; no useful new proposal.",
                ["sources"]=new JArray(first.DocumentId,second.DocumentId)}))};
        Complete(o,progress,closure);
        Test.True(r.EventPhase!="closing"&&progress.PrivacySourceMaterial.StartsWith("event_closure_unresolved"),
            "answered positions are not mechanically converted to satisfied claims");
        closure["outcome"]="deadlocked";Complete(o,progress,closure);
        Test.True(progress.Kind=="event_progress_audit","honest unresolved outcome still requires independent semantic audit");
        o.PrepareSharedRequest(progress);
        Complete(o,progress,new(){["approved"]=true});
        Test.True(r.EventPhase!="closing"&&progress.PrivacySourceMaterial.Contains("event_closure_review_required"),
            "approval without independent closure verification does not end event");
        Test.True(progress.Kind=="event_progress_audit","missing closure audit keeps original proposed outcome");
        var approvedProgress = Approved(progress); approvedProgress["closure_consistent"] = true;
        Complete(o,progress,approvedProgress);
        Test.True(r.EventPhase=="closing"&&r.State=="active"&&r.EventClosureReviewVersion==1,
            "accepted stalemate stays active until ending is actually published");
        var ending=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_boundary"&&x.ForcedIntent=="ended");
        var badEnd=new JObject{["title"]="Disputed recognition",["body"]="Claims remain unresolved.",
            ["effects"]=new JArray(new JObject{["kind"]="town_security",["kingdom"]="a",["settlement"]="密泽亚",["delta"]=-1,["days"]=1,["reason"]="Public dispute"}),
            ["confirmed_documents"]=new JArray(first.DocumentId),["narrative"]=new JArray()};
        var neutral=(JObject)badEnd.DeepClone();
        badEnd=(JObject)Actual("17:43:42",badEnd).DeepClone();
        foreach(var effect in ((JArray)badEnd["effects"]!).OfType<JObject>())
            foreach(string key in new[]{"kingdom","other"})
                if((string)effect[key]=="empire")effect[key]="a";else if((string)effect[key]=="empire_s")effect[key]="b";
        // Only stable source/party identities are rebound; actual town names,
        // effect claims and declaration prose remain the incident payload.
        if(incidents.Count>0)badEnd["confirmed_documents"]=new JArray(first.DocumentId,second.DocumentId);
        int writes=host.Writes;Complete(o,ending,badEnd);
        Test.True(ending.Kind=="event_boundary"&&ending.PrivacySourceMaterial.Contains("allowed_ids=town_a")
            &&ending.PrivacySourceMaterial.Contains("statements_are_not_mechanical_results")&&host.Writes==writes,
            "preflight reports town-name and unexecuted-document errors together before paying for an audit or applying effects");
        Test.True(ending.RetryAfterUtcTicks==0,"bounded schema correction does not sleep for game days");
        Reload(o);r=o.ResolveRound(r.RoundId);ending=o.CurrentStorage.Jobs.Single(x=>x.ForcedIntent=="ended");
        o.PrepareSharedRequest(ending);
        Test.True(ending.UserPrompt.Contains("密泽亚")&&ending.UserPrompt.Contains("allowed_ids=town_a"),"saved repair retains rejected payload and precise feedback");
        var end=neutral;end["effects"]=new JArray();end["confirmed_documents"]=new JArray();
        Complete(o,ending,end);o.PrepareSharedRequest(ending);
        Test.True(ending.Kind=="event_boundary_audit"&&host.Writes==writes,"neutral ending needs review and cannot invent effects");
        Complete(o,ending,Audit(ending));
        Test.True(r.State!="active"&&o.CurrentStorage.EventAnnouncements.Any(x=>x.EventId==r.RoundId&&x.Stage=="ended"&&x.State=="public"),
            "ending publication really closes original event and preserves recorded outcome");
        for(int i=0;i<3;i++)
        { typeof(WorldDiplomacyOrchestration).GetField("_nextEventRuntimeUtc",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(o,0L);o.AdvanceEventRuntime(); }
        Test.True(host.EventNotices.Count(x=>x==DiplomacyEventStage.Ended)==1&&host.Writes==writes,"actual presentation bridge receives one ending, acknowledgement never executes effects");
        Reload(o);typeof(WorldDiplomacyOrchestration).GetField("_nextEventRuntimeUtc",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(o,0L);o.AdvanceEventRuntime();
        Test.True(host.EventNotices.Count(x=>x==DiplomacyEventStage.Ended)==1&&host.Writes==writes,"load cannot duplicate acknowledged ending or effects");
    }

    private static void LegacyClosureRecovery()
    {
        foreach(string receipt in new[]{"none","staged","uncertain","executing_document","public"})
        {
            var(h,o)=Owner();var r=o.EnsureActiveRound("a","b",false);Foundation(r);
            r.EventPhase="closing";r.EventOutcome="resolved";r.EventProgressRevision=r.ConversationRevision;
            r.TechnicalHoldReason=r.TechnicalRequestHoldReason="effect_target_unavailable";
            var j=new WorldDiplomacyJob{JobId="old-end",Kind="event_boundary",ForcedIntent="ended",RoundId=r.RoundId,
                SuspendedReason=r.TechnicalHoldReason,TechnicalRecoveryAttempts=3,PrivacyDraft="rejected original"};
            o.CurrentStorage.Jobs.Add(j);
            if(receipt is "staged" or "public")o.CurrentStorage.EventAnnouncements.Add(new(){Id=r.RoundId+":ended",EventId=r.RoundId,
                Stage="ended",State=receipt,CanonicalHistoryRecorded=true});
            if(receipt=="uncertain")o.CurrentStorage.EventEffects.Add(new(){Id="uncertain",EventId=r.RoundId,Stage="ended",Status="uncertain"});
            if(receipt=="executing_document")o.CurrentStorage.Documents.Add(new(){DocumentId="pending",RoundId=r.RoundId,EventExecutionPending=true});
            Reload(o);r=o.ResolveRound(r.RoundId);
            Test.True(receipt=="none"?r.EventPhase=="discussing"&&r.EventOutcome==""&&r.EventProgressRevision==-1
                &&r.EventClosureReviewVersion==1&&r.TechnicalHoldReason==""&&!o.CurrentStorage.Jobs.Any(x=>x.JobId=="old-end")
                :r.EventPhase=="closing"&&o.CurrentStorage.Jobs.Any(x=>x.JobId=="old-end"),
                "old premature closure reopens only before durable effects/publication: "+receipt);
            Test.True(h.Writes==0,"migration never applies or undoes an effect: "+receipt);
            Reload(o);Test.True(o.ResolveRound(r.RoundId).EventPhase==(receipt=="none"?"discussing":"closing"),"migration stable across second reload: "+receipt);
        }
        var(host,owner)=Owner();var round=owner.EnsureActiveRound("a","b",false);Foundation(round);round.EventPhase="closing";
        var ending=new WorldDiplomacyJob{JobId="held-ending",RoundId=round.RoundId,Kind="event_boundary",ForcedIntent="ended"};owner.CurrentStorage.Jobs.Add(ending);
        int before=host.Notices.Count;
        for(int i=0;i<4;i++)Call(owner,"HoldTechnicalRequest",ending,"boundary_execution_preflight:invalid_target");
        Test.True(host.Notices.Count==before&&round.State=="active","exhausted ending stays diagnostic-only without faking closure");
    }
}
