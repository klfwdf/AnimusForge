using System.Reflection;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialSupplyReplay
{
    private static JObject StructureDraft(WorldDiplomacyJob job) => new() {
        ["title"]="The contested oath", ["background"]="The heir must expose his brother's betrayal or surrender his own claim; the other court must decide whether to release the disputed oath.",
        ["initiator"]="b", ["material_keys"]=new JArray("lore:succession"),
        ["participants"]=new JArray(Position("b","a"),Position("a","b")),
        ["party_review"]=new JArray(job.CandidateKingdomIds.Select(id=>new JObject { ["kingdom"]=id,
            ["included"]=id is "a" or "b", ["reason"]="Grounded standing" })),
        ["narrative"]=new JArray(new JObject { ["kind"]="allegation", ["subject"]="b",
            ["source"]="lore:succession", ["text"]="The old oath is disputed." }) };

    private static JObject BothCompared(bool qualify)
    {
        var result=Compare(); var rows=(JArray)result["assessments"]!;
        var sibling=(JObject)rows[0]!.DeepClone(); sibling["id"]="nord"; rows.Add(sibling);
        if(!qualify)
        {
            foreach(var row in rows.OfType<JObject>())
            { row["qualified"]=false; row["weakness"]="A/B share local allocation protest, autonomy demand and foreign leverage despite renamed tribute/plunder."; }
            result["best_equivalent"]=new JArray(); result["comparison_reason"]="Both repeat prior A's choices and costs; personal motive is missing.";
        }
        return result;
    }

    internal static void StructureSelection()
    {
        MaterialBalance();
        // Human-labelled incident and explicit provider verdicts exercise the
        // production owner; these fixtures do not measure real model judgement.
        var (h,o,j)=ObservationStart(); o.PrepareSharedRequest(j);
        Test.True(j.SystemPrompt.Contains(DiplomacyPromptTexts.Get("EventStructureSelection","MISSING"))
            && j.UserPrompt.Contains("Battania nobles") && j.UserPrompt.Contains("Nord jarls"),
            "formal bilingual criteria and actual prior/sibling evidence reach comparison");
        int reads=h.MaterialReads; var rejected=BothCompared(false);
        rejected["research_hint"]="Check the heir's rescue obligation and whether truthful testimony costs kinship or inheritance.";
        Complete(o,j,rejected);
        Test.True(j.MaterialSearchPass==1 && j.EventSelectionStage=="candidates" && j.TechnicalRecoveryAttempts==0
            && j.SuspendedReason=="" && h.MaterialReads==reads && o.CurrentStorage.EventAnnouncements.Count==1,
            "known same-structure A/B and sibling rejections immediately consume one batch without technical rewrite, publication or extra evaluation");
        var evidence=JObject.Parse(o.ResolveRound(j.RoundId).EventSelectionEvidence);
        Test.True((string)evidence["research_hint"]! == (string)rejected["research_hint"]! && evidence["audit"]!=null,
            "explicit failed verdict and concrete research gap retained under the existing owner");

        // Genuine changes in choice/cost can still pass, even for ruler-vassal stories.
        var (_,positive,p)=ObservationStart("distinct"); Complete(positive,p,BothCompared(true));
        Complete(positive,p,StructureDraft(p)); positive.PrepareSharedRequest(p);
        Complete(positive,p,MaterialCountryThirdReplay.Approved(p));
        Test.True(positive.ResolveRound(p.RoundId).EventPhase!="forming" && !positive.CurrentStorage.Jobs.Any(x=>x.JobId==p.JobId),
            "substantively distinct dilemma reaches the original opening path with all fact/identity/admission audits");

        foreach(string verdict in new[]{"duplicate","quality","theme","motive","story"})
        {
            var (_,owner,job)=ObservationStart(); Complete(owner,job,BothCompared(true));
            Complete(owner,job,StructureDraft(job)); owner.PrepareSharedRequest(job);
            var audit=MaterialCountryThirdReplay.Approved(job);
            if(verdict is "duplicate" or "quality") { audit["approved"]=false; audit["rejection_category"]=verdict; audit["reason"]="Same choices as A; no substantive personal dilemma."; }
            if(verdict=="theme") audit["theme_review"]!["valid"]=false;
            if(verdict=="motive") audit["story_review"]!["motive_preserved"]=false;
            if(verdict=="story") audit["story_review"]!["comparisons"]![0]!["verdict"]="duplicate";
            audit["research_hint"]="Check the brother's prior rescue and the oath's limiting condition.";
            Complete(owner,job,audit);
            Test.True(job.EventSelectionStage=="compare" && job.MaterialSearchPass==0 && job.PrivacyDraft==""
                && job.TechnicalRecoveryAttempts==0 && job.PrivacyAuditRetries==0 && job.SuspendedReason==""
                && JArray.Parse(job.EventCandidatePool).Count==1 && !job.EventCandidatePool.Contains("domestic"),
                "semantic rejection tries the remaining qualified sibling without rewriting rejected seed: "+verdict);
            Test.True(owner.CurrentStorage.MaterialLeads.Any(x=>x.Candidate.Contains("domestic") && x.Status=="not_viable")
                && owner.CurrentStorage.EventAnnouncements.Count==1, "rejected lead cannot return as reusable or publish: "+verdict);
            if(verdict=="duplicate")
            {
                var siblingReview=Compare(); siblingReview["assessments"]![0]!["id"]="nord";
                siblingReview["assessments"]![0]!["qualified"]=false; siblingReview["best_equivalent"]=new JArray();
                Complete(owner,job,siblingReview);
                Test.True(job.MaterialSearchPass==1 && owner.CurrentStorage.MaterialLeads.All(x=>x.Status=="not_viable"),
                    "subsequent rejection also retires the sibling's previously reusable lead");
            }
        }

        // Same-batch duplicates are not resurrected by the reusable-lead lane.
        var (_,siblings,s)=ObservationStart(); var comparison=BothCompared(true);
        comparison["assessments"]![1]!["duplicate_of"]="domestic"; Complete(siblings,s,comparison);
        Test.True(siblings.CurrentStorage.MaterialLeads.Single(x=>x.Candidate.Contains("Nord jarls")).Status=="not_viable",
            "duplicate sibling is retired instead of saved as a reusable alternative");
        Complete(siblings,s,StructureDraft(s)); siblings.PrepareSharedRequest(s);
        var duplicateAudit=MaterialCountryThirdReplay.Approved(s); duplicateAudit["approved"]=false;
        duplicateAudit["rejection_category"]="duplicate"; duplicateAudit["reason"]="Same structure as prior A.";
        Complete(siblings,s,duplicateAudit);
        Test.True(s.MaterialSearchPass==1 && s.EventSelectionStage=="candidates", "no qualified sibling means next batch, not the duplicate sibling");

        DirectedStructureResearch();
        StructureSelectionGuards();
        Console.WriteLine("PASS structure selection: real business verdicts, causal A/B fixtures, qualified alternatives, directed retrieval, bounded persisted exhaustion, protected work; real model/game semantics NOT_RUN");
    }

    private static void DirectedStructureResearch()
    {
        var host=new ThemeHost { PublishEnabled=true, CurrentDayValue=40, DeferDeep=false };
        var owner=new WorldDiplomacyOrchestration(host,new WorldDiplomacyRuntimeState()); host.Owner=owner;
        owner.TryScheduleNormalRound(); var job=owner.CurrentStorage.Jobs.Single(x=>x.Kind=="event_concept");
        owner.PrepareSharedRequest(job); string theme=job.EventExplorationTheme;
        const string hint="Check the exile's living-return condition and the rival witness's disputed allegation.";
        JObject Empty(string gap)=>new(){["candidates"]=new JArray(),["reason"]="Inspected original material lacks a distinct supported choice.",["research_hint"]=gap};
        Complete(owner,job,Empty(hint));
        owner.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(owner.CurrentStorage))!);
        job=owner.CurrentStorage.Jobs.Single(x=>x.JobId==job.JobId); owner.PrepareSharedRequest(job);
        Test.True(job.MaterialSearchPass==1 && job.EventExplorationTheme==theme && host.Queries.Last().Contains(hint)
            && WorldDiplomacyOrchestration.IsThematicResearchTopic(host.Queries.Last()),
            "save/load preserves gap and first follow-up sends it to actual themed host retrieval with bounded thematic lanes");
        Test.True(job.UserPrompt.Contains(hint) && job.EventMaterialSnapshot.Contains("ONLY if the exile returns alive")
            && job.EventMaterialSnapshot.Contains("family denies"), "writer receives gap and original conditions/contradictions, not invented facts");
        int queries=host.Queries.Count; owner.PrepareSharedRequest(job);
        Test.True(host.Queries.Count==queries, "same frozen request retry causes no additional capture");
        Complete(owner,job,Empty(hint)); owner.PrepareSharedRequest(job);
        Test.True(job.MaterialSearchPass==2 && job.EventThemePass==1 && job.EventExplorationTheme!=theme
            && !host.Queries.Last().Contains(hint), "exhausted direction changes once; old gap cannot contaminate the new direction");
        owner.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(owner.CurrentStorage))!);
        job=owner.CurrentStorage.Jobs.Single(x=>x.JobId==job.JobId); var round=owner.ResolveRound(job.RoundId);
        Complete(owner,job,Empty(new string('x',601)));
        Test.True(round.State=="closed" && !owner.CurrentStorage.Jobs.Any(x=>x.JobId==job.JobId)
            && round.EventSelectionEvidence.Contains("Inspected original material") && owner.CurrentStorage.NextNormalRoundDay>host.CurrentDayValue,
            "third exhausted batch archives reason and releases slot without same-day restart after reload");
        owner.TryScheduleNormalRound(); Test.True(!owner.CurrentStorage.Jobs.Any(x=>x.Kind=="event_concept"), "paused day cannot immediately regenerate a new search owner");
        Test.True((string)JObject.Parse(round.EventSelectionEvidence)["research_hint"]! == "", "overlong optional hint is ignored without technical retry");

        var (_,technical,t)=Start(); Complete(technical,t,new(){["candidates"]=new JArray(Candidate())}); Complete(technical,t,Compare());
        Complete(technical,t,StructureDraft(t)); technical.PrepareSharedRequest(t); var malformed=MaterialCountryThirdReplay.Approved(t);
        malformed["story_review"]!["digest"]="wrong"; Complete(technical,t,malformed);
        Test.True(t.Kind=="event_concept_audit" && t.EventSelectionStage=="expand" && t.MaterialSearchPass==0 && t.PrivacyAuditRetries==1,
            "malformed technical audit keeps same draft instead of masquerading as semantic rejection");
        Complete(technical,t,new(){["approved"]=false,["rejection_category"]="duplicate"});
        Test.True(t.MaterialSearchPass==0 && t.EventSelectionStage=="expand" && t.SuspendedReason.Contains("reviewer_must_explain"),
            "unexplained rejection remains a technical repair rather than a substantive verdict");
    }

    private static void StructureSelectionGuards()
    {
        var reject=typeof(WorldDiplomacyOrchestration).GetMethod("RejectStoryCandidate",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var resume=typeof(WorldDiplomacyOrchestration).GetMethod("TryResumeTechnicalRequest",BindingFlags.Instance|BindingFlags.NonPublic)!;
        foreach(string guard in new[]{"player","oral","policy","trigger","document","announcement","arrangement","aid","offer","threat","response","effect","uncertain","legacy"})
        {
            var (_,owner,job)=Start(); owner.PrepareSharedRequest(job); var round=owner.ResolveRound(job.RoundId);
            switch(guard)
            {
                case "player": case "oral": job.EventSelectionStage=guard; break;
                case "policy": round.EventSourceType="policy_material"; break;
                case "trigger": job.EventTriggerMaterial="fixed agenda"; break;
                case "document": owner.CurrentStorage.Documents.Add(new(){DocumentId="published",RoundId=round.RoundId}); break;
                case "announcement": owner.CurrentStorage.EventAnnouncements.Add(new(){EventId=round.RoundId,State="public"}); break;
                case "arrangement": owner.CurrentStorage.DialogueArrangements.Add(new(){RoundId=round.RoundId}); break;
                case "aid": owner.CurrentStorage.AidRequests.Add(new(){RoundId=round.RoundId,Status="accepted"}); break;
                case "offer": round.PendingOffers.Add(new(){Status="open"}); break;
                case "threat": owner.CurrentStorage.DiplomaticThreats.Add(new(){StageRoundId=round.RoundId,Status="open"}); break;
                case "response": round.PlayerResponses.Add(new(){Status="pending"}); break;
                case "effect": owner.CurrentStorage.EventEffects.Add(new(){EventId=round.RoundId,Status="uncertain"}); break;
                case "uncertain": round.TechnicalHoldReason="event_document_execution_uncertain"; break;
                case "legacy": round.TechnicalHoldReason="legacy_unverified_work_preserved"; break;
            }
            Test.True(!(bool)reject.Invoke(owner,new object?[]{job,"event_concept_audit_failed:story_motive_replaced",null})!
                && round.State!="closed" && job.MaterialSearchPass==0, "novelty cannot discard protected lane: "+guard);
            if(guard is "uncertain" or "legacy" or "offer" or "threat")
            {
                job.SuspendedReason="event_concept_audit_failed:story_motive_replaced"; job.TechnicalRecoveryAttempts=3;
                resume.Invoke(owner,new object[]{job,false});
                Test.True(round.State!="closed" && job.MaterialSearchPass==0,"legacy recovery preserves outstanding work: "+guard);
            }
        }
        var (_,oldOwner,oldJob)=Start(); oldOwner.PrepareSharedRequest(oldJob);
        oldJob.SuspendedReason="event_concept_audit_failed:story_motive_replaced"; oldJob.TechnicalRecoveryAttempts=1;
        oldOwner.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(oldOwner.CurrentStorage))!);
        oldJob=oldOwner.CurrentStorage.Jobs.Single(x=>x.JobId==oldJob.JobId);
        resume.Invoke(oldOwner,new object[]{oldJob,false});
        Test.True(oldJob.MaterialSearchPass==1 && oldJob.SuspendedReason=="", "legacy semantic hold consumes one batch instead of rewriting indefinitely");
        resume.Invoke(oldOwner,new object[]{oldJob,false});
        Test.True(oldJob.MaterialSearchPass==1,"cleared hold cannot consume again or refill budget");
    }
}
