using AnimusForge;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    private sealed class LargePlayerSourceHost : Host
    {
        public override int EstimateTokens(string text) => text.StartsWith("{\"key\":\"world:current\"",StringComparison.Ordinal)
            ? 10001 : base.EstimateTokens(text);
    }

    internal static void PlayerOpeningRecovery()
    {
        OpeningEffectBounds();
        foreach(string consequence in new[]{"ordinary","conflict"})
        {
            var(h,o)=Owner();var r=o.EnsureActiveRound("a","p",false);
            var source=new WorldDiplomacyDocument{DocumentId="original-question",RoundId=r.RoundId,
                AuthorKingdomId="a",TargetKingdomId="p",Body="Please answer our demand.",IsReadyForPublication=true};o.AddDocument(source);
            var job=new WorldDiplomacyJob{JobId="silence-copy",Kind="generate",RoundId=r.RoundId,
                AuthorKingdomId="a",TargetKingdomId="p",SilenceSourceDocumentId=source.DocumentId,
                SilenceConsequence=consequence,PrivacySourceMaterial="Actual public question and no subsequent answer."};o.EnqueueJob(job);
            var draft=new JObject{["title"]="New title",["body"]="  "+source.Body+"  ",
                ["round_participation"]="withdraw",["response_requests"]=new JArray()};
            Call(o,"DeferDraftForPrivacyCheck",job,draft.ToString());
            Test.True(job.Kind=="generate" && job.PrivacyRepairFeedback.Contains("silence_closure_repeats_original")
                && h.Effects==0 && o.CurrentStorage.Documents.Count==1,
                "actual provider duplicated closure body is rewritten before review or execution: "+consequence);
            draft["body"]="We have received no further answer and now close this exchange.";
            Call(o,"DeferDraftForPrivacyCheck",job,draft.ToString());
            Test.True(job.Kind=="privacy_audit" && !job.PrivacyApproved,
                "a different consequence body still requires independent semantic review");
        }
        {
            var(h,o)=Owner(new ModelHost{PublishEnabled=true,PlayerAwareExecution=true,OralRange=(0,3000),CurrentDayValue=40});
            WorldDiplomacyPlayerApplication.SubmitPlayerDocument(new SubmissionWorld(o),"Original human words.",o);
            var root=o.CurrentStorage.Documents.Single();root.Title="Actual public declaration";
            var draft=new JObject{["body"]="ruler_p发布了"+root.DocumentId+"，ruler_a回应。未知ruler_unknown。",
                ["source"]=root.DocumentId,["hero_ids"]=new JArray("ruler_p")};
            Call(o,"ProjectEventPublicIdentities",o.ResolveRound(root.RoundId),draft);
            Test.True(((string)draft["body"]!).Contains("Garios发布了Actual public declaration")
                && ((string)draft["body"]!).Contains("Rhagaea回应") && ((string)draft["body"]!).Contains("ruler_unknown")
                && (string)draft["source"]! == root.DocumentId && (string)draft["hero_ids"]![0]! == "ruler_p"
                && root.Body=="Original human words.","known public IDs project before audit; unknown IDs, source identities and human words remain unchanged");
        }
        foreach(string kind in new[]{"event_concept_audit","event_boundary_audit","event_progress_audit"})
        {
            var(h,o)=Owner();
            WorldDiplomacyPlayerApplication.SubmitPlayerDocument(new SubmissionWorld(o),"An independent claim.",o);
            var root=o.CurrentStorage.Documents.Single();
            var job=new WorldDiplomacyJob{JobId="semantic-feedback",RoundId=root.RoundId,Kind=kind,EventSelectionStage="player",ForcedIntent="started"};
            o.CurrentStorage.Jobs.Add(job);
            if(kind=="event_concept_audit")
            {
                job.Kind="event_concept";job.AuthorKingdomId="p";job.CandidateKingdomIds=new(){"a","b","c","p"};job.EventSelectedCandidate="{}";job.EventComparison="{}";
                CountryAdmissionReplay.FreezeFixture(o,job,new[]{"a","p"});job.Kind=kind;
            }
            else o.ResolveRound(root.RoundId).CountryAdmissionVersion=0; // Existing pre-admission feedback contract.
            Complete(o,job,new(){["approved"]=false,["rejection_category"]="fact",["narrative_safety"]=new JObject{["reason"]="Actual nested semantic correction: the dispute remains unresolved."}});
            Test.True((job.MaterialRepairFeedback+job.PrivacySourceMaterial).Contains("Actual nested semantic correction") && h.Writes==0,
                "real nested rejection feedback reaches the correct writer without being replaced by a boolean-format error: "+kind);
        }
        foreach(string reason in new[]{"player_opening_requires_grounded_npc_participant_and_actual_declaration_source",
            "public_narrative_internal_identity:controlled_failure","unanswered_player_collective_decision:controlled_failure"})
        {
            var(h,o)=Owner();
            WorldDiplomacyPlayerApplication.SubmitPlayerDocument(new SubmissionWorld(o),"An independent claim.",o);
            var root=o.CurrentStorage.Documents.Single();
            var job=new WorldDiplomacyJob{JobId="repair-feedback",RoundId=root.RoundId,Kind="event_concept",EventSelectionStage="player"};
            o.CurrentStorage.Jobs.Add(job);Call(o,"HoldTechnicalRequest",job,reason);
            Test.True(job.MaterialRepairFeedback==reason && job.RetryAfterUtcTicks==0 && job.TechnicalRecoveryAttempts==1,
                "every player-planning guard returns its exact correction to the writer without transport backoff: "+reason);
        }
        foreach (bool oldWriter in new[] { false, true })
        {
            var (h,o)=Owner(new LargePlayerSourceHost { PublishEnabled=true,PlayerAwareExecution=true,
                OralRange=(0,3000),CurrentDayValue=40 });
            WorldDiplomacyPlayerApplication.SubmitPlayerDocument(new SubmissionWorld(o), "Our independent claim.",o);
            var root=o.CurrentStorage.Documents.Single();
            var analysis=o.CurrentStorage.Jobs.Single(x=>x.Kind=="analyze");
            analysis.RoundId="";analysis.OwnershipEpoch=0;analysis.SchedulingOwner="";
            analysis.TechnicalRecoveryAttempts=1;
            Reload(o);o.ReconcileActiveDiplomacyAfterLoad();
            root=o.ResolveDocument(root.DocumentId);
            analysis=o.CurrentStorage.Jobs.Single(x=>x.Kind=="analyze");
            Test.True(analysis.RoundId==root.RoundId && analysis.SchedulingOwner=="player"
                && analysis.TechnicalRecoveryAttempts==1 && o.CanDispatchDiplomacyJob(analysis),
                "legacy exchange-only analysis recovers the player owner without losing its retry budget");
            o.CommitAnalysis(analysis,new JObject { ["status"]="success",["title_summary"]="A claim",
                ["actions"]=new JArray(new JObject{["intent"]="statement",["commitment"]="non_binding"}),
                ["response_requests"]=new JArray(),["event_participation"]="participate" }.ToString());
            o.RemoveJob(analysis.JobId);
            var round=o.ResolveRound(root.RoundId);
            var plan=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_concept");
            if(oldWriter)
            {
                o.RemoveJob(plan.JobId);round.EventOpeningOrigin="";
                o.CurrentStorage.Jobs.Add(new(){JobId="obsolete",Kind="event_boundary",RoundId=round.RoundId,
                    ForcedIntent="started",SuspendedReason="narrative_unknown_actor:player",TechnicalRecoveryAttempts=3});
                round.TechnicalHoldReason="narrative_unknown_actor:player";
                Reload(o);o.ReconcileActiveDiplomacyAfterLoad();
                Test.True(o.CurrentStorage.Jobs.Count(x=>x.Kind=="event_concept")==1
                    && !o.CurrentStorage.Jobs.Any(x=>x.JobId=="obsolete"),"old unpublished writer migrates into one durable player plan");
                plan=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_concept");
            }
            o.PrepareSharedRequest(plan);
            string frozen=plan.EventMaterialSnapshot;
            var sources=JArray.Parse(frozen);
            Test.True(sources.Any(x=>(string)x["key"]! == root.DocumentId)
                && sources.Any(x=>(string)x["key"]! == "world:current"),
                "supporting-dossier budget cannot discard the authoritative world or original player declaration");
            int notices=h.Notices.Count;
            for(int attempt=0;attempt<3;attempt++) Call(o,"HoldTechnicalRequest",plan,"event_concept_invalid:controlled_failure");
            Test.True(h.Notices.Count==notices,"exhausted player planning remains diagnostic-only and never interrupts gameplay");
            Reload(o);o.ReconcileActiveDiplomacyAfterLoad();
            var restored=o.CurrentStorage.Jobs.Single(x=>x.Kind=="event_concept");
            Test.True(restored.TechnicalRecoveryAttempts==3 && restored.SuspendedReason!=""
                && restored.EventMaterialSnapshot==frozen,"reload preserves frozen evidence and finite retry exhaustion");
            Test.True(o.CurrentStorage.Documents.Count(x=>x.DocumentId==root.DocumentId)==1
                && o.ResolveRound(root.RoundId).State=="active" && !o.CurrentStorage.EventAnnouncements.Any(),
                "failed planning never deletes original speech, fakes an opening or closes the event");
            o.ReconcileActiveDiplomacyAfterLoad();
            Test.True(o.CurrentStorage.Jobs.Count(x=>x.Kind=="event_concept")==1,"repeat load reconciliation never duplicates planning");
        }
    }
}
