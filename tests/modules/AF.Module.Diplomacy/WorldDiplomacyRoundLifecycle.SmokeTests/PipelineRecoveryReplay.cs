using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    internal static void PipelineRecovery(string incidentPath = null)
    {
        var incidents = incidentPath == null ? new JArray() : JArray.Parse(File.ReadAllText(incidentPath));
        string body = (string?)incidents.OfType<JObject>().FirstOrDefault(x => (string?)x["time"] == "18:51:46")?["draft"]?["body"]
            ?? "Our nobles petition for lower taxes and reduced royal power. We reject foreign interference and demand respect for our authority.";
        foreach(var actual in incidents.OfType<JObject>().Where(x => x["draft"] is JObject a && a["negotiation_consistent"] != null))
        {
            var(_,o,_,j)=EvidenceOpening();SubmitEvidenceDraft(o,j,body);string original=j.PrivacyDraft;
            Complete(o,j,(JObject)actual["draft"]!);
            Test.True(j.Kind=="privacy_audit"&&j.PrivacyRewrites==0&&j.PrivacyDraft==original&&o.CurrentStorage.Documents.Count==0,
                "unaltered incident review cannot approve without ownership evidence or trigger blind prose rewriting: "+actual["time"]);
        }
        {
            var (h,o,r,j)=EvidenceOpening();
            var party=r.Participants.Single(x=>x.KingdomId=="a");
            party.Agenda="The dissident nobles demand the king reduce taxes and surrender royal powers.";
            var evidence=SubmitEvidenceDraft(o,j,body);
            string original=j.PrivacyDraft;
            o.TryCompleteDiplomacyPipeline(j,new(){Success=true,IsOutputTruncated=true,Content="{\"safe\":"});
            Test.True(j.Kind=="privacy_audit"&&j.PrivacyDraft==original&&j.PrivacyRewrites==0,
                "truncated review keeps exact draft and repairs the review instead of regenerating prose");
            var incomplete=SpanReview(evidence,true);incomplete.Remove("position_owner_valid");
            Complete(o,j,incomplete);
            Test.True(j.Kind=="privacy_audit"&&j.PrivacyRewrites==0&&j.PrivacyDraft==original,
                "generic audit approval cannot substitute for mandatory sovereign ownership review");
            var rejected=SpanReview(evidence,true);rejected["position_owner_valid"]=false;rejected["safe"]=false;
            rejected["repair_feedback"]="Stored agenda belongs to dissident nobles, not the king. Correct national ownership; no confidential fact is identified.";
            Complete(o,j,rejected);
            Test.True(j.Kind=="privacy_repair"&&j.PositionCorrectionRequired&&j.PrivacyRewrites==0
                &&j.UserPrompt.Contains("dissident nobles")&&j.UserPrompt.Contains("position_correction"),
                "ownership rejection selects mandatory field repair even when reviewer mislabels it as confidentiality");
            Complete(o,j,new());
            Test.True(j.Kind=="privacy_repair"&&j.DeclarationRepairAttempts==1&&j.PrivacyDraft==original,
                "omitting the correction cannot return to another full prose rewrite");
            var correction=new JObject{["kingdom"]="a",["ruler_hero_id"]="wrong",
                ["agenda"]="Reject foreign interference.",["gain"]="External respect for royal authority.",
                ["cost"]="Continued diplomatic tension.",["red_line"]="Foreign backing for domestic demands.",
                ["alternative"]="Continue diplomatic protest.",["reason"]="The prior demands belonged to domestic dissidents, not their king."};
            Complete(o,j,new(){["position_correction"]=correction});
            Test.True(j.DeclarationRepairAttempts==2&&j.PrivacyDraft==original,"wrong sovereign identity cannot repair state");
            correction["ruler_hero_id"]=h.PartyRulerId("a");
            Complete(o,j,new(){["position_correction"]=correction});
            Test.True(j.Kind=="privacy_audit"&&o.CurrentStorage.Documents.Count==0&&party.Agenda.Contains("dissident"),
                "valid repair remains an unpublished proposal, never silently replaces stored political history");
            Test.True((string)JObject.Parse(j.PrivacyDraft)["body"]==body,"field repair preserves original incident prose exactly");
            Reload(o);j=o.CurrentStorage.Jobs.Single(x=>x.JobId==j.JobId);r=o.ResolveRound(r.RoundId);
            Test.True(j.PositionCorrectionRequired&&j.DeclarationRepairAttempts==2,"save/load preserves correction obligation and bounded attempts");
            var approved=SpanReview(Evidence(o,j),true);approved["position_correction_valid"]=true;
            Complete(o,j,approved);
            var doc=o.CurrentStorage.Documents.Single(x=>x.IsReadyForPublication);
            Test.True(r.Participants.Single(x=>x.KingdomId=="a").Agenda=="Reject foreign interference."&&doc.Body==body,
                "only independent approval and actual publication commit the corrected national position");
            h.CurrentDayValue+=20;o.ProcessPropagationArrivals();Call(o,"ReconcileEventQueue");
            var reply=o.CurrentStorage.Jobs.Single(x=>x.Kind=="generate"&&x.AuthorKingdomId=="b");
            var replyEvidence=SubmitEvidenceDraft(o,reply,"We reject the accusation of interference. We request a public hearing.",false);
            Complete(o,reply,SpanReview(replyEvidence,true,doc.DocumentId));
            Test.True(o.CurrentStorage.Documents.Count(x=>x.IsReadyForPublication)==2,"corrected first declaration delivers and counterpart publishes its actual reply");
        }
        {
            var(_,o,_,j)=EvidenceOpening();SubmitEvidenceDraft(o,j,"We demand a public hearing.");
            string draft=j.PrivacyDraft;
            var empty=SpanReview(Evidence(o,j),false);Complete(o,j,empty);
            Test.True(j.Kind=="privacy_audit"&&j.PrivacyDraft==draft&&j.PrivacyRewrites==0
                &&j.PrivacyRepairFeedback.Contains("required_core_agenda_span_missing"),"approved empty core span repairs reviewer, not author");
            var malformed=JObject.Parse(draft);malformed.Remove("response_requests");j.Kind="generate";
            CompleteWriter(o,j,malformed);Test.True(j.Kind=="privacy_repair","missing request classification has a field owner");
            string retained=j.PrivacyDraft;
            Complete(o,j,new(){["response_requests"]=new JArray(new JObject{["kingdom"]=new JObject(),["kind"]="case_question",["quote"]="invented"})});
            Test.True(j.PrivacyDraft==retained&&j.DeclarationRepairAttempts==1,"malformed request identities fail without exception or draft mutation");
            Complete(o,j,new(){["response_requests"]=new JArray()});
            Test.True(j.Kind=="privacy_audit"&&JToken.DeepEquals(JObject.Parse(retained)["actions"],JObject.Parse(j.PrivacyDraft)["actions"]),
                "metadata completion preserves actions and returns to independent audit");
            var wrongBody=JObject.Parse(j.PrivacyDraft);wrongBody["body"]=new JObject();j.Kind="generate";
            CompleteWriter(o,j,wrongBody);
            Test.True(j.Kind=="generate"&&j.PrivacyRepairFeedback.Contains("declaration_body_string_required"),
                "non-text body returns precise author repair instead of throwing during classification preflight");
        }
        {
            var(h,o,r,j)=EvidenceOpening();SubmitEvidenceDraft(o,j,body);string original=j.PrivacyDraft;
            j.DeclarationRepairVersion=0;j.TechnicalRecoveryAttempts=3;j.SuspendedReason="declaration_rewrites_exhausted:core_agenda_not_expressed";
            j.RecoveryConfiguration=(string)Call(o,"SpeechConfiguration");j.RecoveryRevision=r.ConversationRevision;
            j.RecoveryKnowledgeRevision=j.CourtKnowledgeRevision;j.RetryAfterUtcTicks=0;j.NextRecoveryProbeUtcTicks=0;
            Call(o,"TryResumeTechnicalRequest",j,false);
            Test.True(j.SuspendedReason==""&&j.Kind=="privacy_audit"&&j.PrivacyDraft==original&&j.DeclarationRepairVersion==1,
                "real legacy exhausted reason gets one migration and retained-draft review");
            int notices=h.Notices.Count;
            for(int cycle=0;cycle<3;cycle++)
            {
                if(cycle==0){var noOwner=SpanReview(Evidence(o,j),true);noOwner["position_owner_valid"]=false;noOwner["repair_feedback"]="Dissident demands are assigned to the king.";Complete(o,j,noOwner);}
                for(int n=0;cycle==0&&n<3;n++)Complete(o,j,new());
                if(cycle<2){j.RetryAfterUtcTicks=0;j.NextRecoveryProbeUtcTicks=0;Call(o,"TryResumeTechnicalRequest",j,false);}
            }
            Test.True(j.TechnicalRecoveryAttempts==3&&h.Notices.Count==notices&&o.CurrentStorage.Documents.Count==0&&r.State=="active",
                "persistent bad model responses remain bounded, visible, unpublished and never become political withdrawal");
            Reload(o);j=o.CurrentStorage.Jobs.Single(x=>x.JobId==j.JobId);j.RetryAfterUtcTicks=0;j.NextRecoveryProbeUtcTicks=0;
            Call(o,"TryResumeTechnicalRequest",j,false);
            Test.True(j.SuspendedReason!=""&&j.TechnicalRecoveryAttempts==3,"reload cannot replenish exhausted new repair budget");
        }
        {
            var(_,o,_,j)=EvidenceOpening();SubmitEvidenceDraft(o,j,"We seek a hearing without accepting your claim.");
            var draft=JObject.Parse(j.PrivacyDraft);draft.Remove("round_status");j.Kind="generate";
            CompleteWriter(o,j,draft);Complete(o,j,SpanReview(Evidence(o,j),true));
            Test.True(j.Kind=="player_lifecycle"&&o.CurrentStorage.Documents.Count==0,"unclassified lifecycle is retained after initial prose audit");
            Complete(o,j,new());
            Test.True(j.UserPrompt.Contains("requires_response:boolean")&&j.LifecycleClassificationAttempts==1,
                "missing lifecycle fields return exact metadata contract against the same original");
            Complete(o,j,new(){["requires_response"]=true,["round_participation"]="continue",["round_status"]="continue"});
            Test.True(j.Kind=="privacy_audit"&&o.CurrentStorage.Documents.Count==0,"new lifecycle classification cannot bypass independent review");
            Complete(o,j,SpanReview(Evidence(o,j),true));
            Test.True(o.CurrentStorage.Documents.Single().IsReadyForPublication,"merged lifecycle publishes only after its own successful review");
        }
        {
            var(_,o,r,j)=EvidenceOpening();SubmitEvidenceDraft(o,j,body);
            var rejected=SpanReview(Evidence(o,j),true);rejected["position_owner_valid"]=false;rejected["repair_feedback"]="Incorrect domestic actor owns the agenda.";
            Complete(o,j,rejected);
            Test.True(o.CanDispatchDiplomacyJob(j),"specialized correction uses the real event dispatch gate");
            int revision=r.ConversationRevision;r.ConversationRevision++;
            Test.True(!o.AdmitCompletion(j,new(){Success=true,RoundConversationRevision=revision,OwnershipEpoch=r.OwnershipEpoch,
                SchedulingOwner=r.SchedulingOwner,PlayerAuthorityRulerId=r.PlayerAuthorityRulerId}),
                "late correction result cannot apply after conversation changes");
            Test.True(j.Kind=="generate"&&o.CurrentStorage.Documents.Count==0&&!j.PositionCorrectionRequired&&j.PrivacyDraft=="",
                "stale correction returns to fresh context without publication or an inherited correction obligation");
        }
        {
            var(_,o,r,j)=EvidenceOpening();SubmitEvidenceDraft(o,j,body);
            r.Participants.Single(x=>x.KingdomId==j.AuthorKingdomId).State="observer";
            var draft=JObject.Parse(j.PrivacyDraft);draft["position_correction"]=new JObject{["kingdom"]=j.AuthorKingdomId};j.PrivacyDraft=draft.ToString();
            Complete(o,j,SpanReview(Evidence(o,j),false));
            Test.True(j.Kind=="generate"&&j.PrivacyRepairFeedback.Contains("remove_forbidden_position_correction")&&!j.PositionCorrectionRequired,
                "an ineligible observer is told to remove unauthorized correction, never forced to invent a national position");
            Test.True(o.CurrentStorage.Documents.Count==0,"forbidden correction cannot publish or grant authority");
        }
        {
            var(_,o)=Owner();var r=o.EnsureActiveRound("a","b",false);
            var concept=new JObject{["participants"]=new JArray(new JObject{["kingdom"]="a",["agenda"]="Dissidents demand the king surrender power."})};
            var j=new WorldDiplomacyJob{JobId="ownership-concept",Kind="event_concept_audit",EventSelectionStage="expand",RoundId=r.RoundId,
                PrivacyDraft=concept.ToString(),PositionOwnerReviewVersion=1};o.CurrentStorage.Jobs.Add(j);
            j.Kind="event_concept";j.AuthorKingdomId="a";j.CandidateKingdomIds=new(){"a","b","c"};j.EventSelectedCandidate="{}";j.EventComparison="{}";
            CountryAdmissionReplay.FreezeFixture(o,j,new[]{"a","b"});j.Kind="event_concept_audit";j.PrivacyDraft=concept.ToString();
            Complete(o,j,new(){["approved"]=true});
            Test.True(j.Kind=="event_concept_audit"&&j.PrivacyAuditRetries==1&&j.MaterialRepairFeedback.Contains("position_owner_review_incomplete"),
                "new concept cannot be adopted on generic approval without per-country ownership evidence");
            var required=(JArray)Call(o,"BuildPositionOwnerReview",concept["participants"]!);
            var review=(JObject)required[0]!.DeepClone();review.Remove("position");review["consistent"]=false;review["reason"]="Dissident agenda assigned to the king.";
            Complete(o,j,new(){["approved"]=true,["position_reviews"]=new JArray(review)});
            Test.True(j.SuspendedReason.Contains("position_owner_rejected:a")&&r.EventBackground=="",
                "even contradictory approved=true cannot override an explicit ownership rejection or publish the event");
        }
    }
}
