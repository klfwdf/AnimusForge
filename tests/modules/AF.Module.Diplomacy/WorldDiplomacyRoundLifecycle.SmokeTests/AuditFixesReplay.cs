using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Production owners with deterministic transport/game leaves, never a live game.
internal static class AuditFixesReplay
{
    private static object? Invoke(WorldDiplomacyOrchestration o, string method, params object?[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args);
    private static (EventStructureReplay.Host h, WorldDiplomacyOrchestration o, WorldDiplomacyRound r) Fixture()
    {
        var h = new EventStructureReplay.Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        var r = o.EnsureActiveRound("a", "b", false);
        r.Participants.Single(x => x.KingdomId == "b").State = "active";
        return (h, o, r);
    }
    private static WorldDiplomacyJob Generation(WorldDiplomacyOrchestration o, WorldDiplomacyRound r)
    {
        var job = new WorldDiplomacyJob { JobId = "test_generation", Kind = "generate", RoundId = r.RoundId,
            ExchangeId = r.RoundId, AuthorKingdomId = "b", TargetKingdomId = "a", CandidateKingdomIds = new() { "a" } };
        o.EnqueueJob(job); return job;
    }
    internal static void Run()
    {
        ContractAndIsolation(); TechnicalRecovery(); TriggerHandoff(); ProgressEvidence(); AffiliatedReceipt();
    }
    private static void ContractAndIsolation()
    {
        string contract = WorldDiplomacyPromptContractRules.BuildDeclareModePrompt("safe", DiplomacyPromptTexts.Get);
        Test.True(contract.Contains("ultimatum/comply_ultimatum") && contract.Contains("binding") && contract.Contains("acceptance"),
            "actual final declaration mode carries complete intent/commitment mapping in either content pack");
        Test.True(!WorldDiplomacyOfferContractRules.CommitmentMatchesIntent("ultimatum", "proposal")
            && WorldDiplomacyOfferContractRules.CommitmentMatchesIntent("ultimatum", "binding"), "strict executor still rejects the live incident's invalid pairing");
        var (h, o, r) = Fixture();
        var source = new WorldDiplomacyDocument { DocumentId = "exact_offer", RoundId = r.RoundId, AuthorKingdomId = "a",
            TargetKingdomId = "b", Intent = "propose_peace", Commitment = "proposal", IsReadyForPublication = true,
            Body = "PUBLIC_BODY", PeaceTerms = new() { DailyTribute = 123, DurationDays = 17, TributePayerKingdomId = "b", TributeReceiverKingdomId = "a" },
            Actions = new() { new() { ActionId = "exact_action", Intent = "propose_peace", TargetKingdomId = "b", Commitment = "proposal" } } };
        o.AddDocument(source);
        WorldDiplomacyPropagationApplication.ReceiveCourt(o.CurrentStorage, source, "b", h.CurrentDayValue, () => false, () => { });
        r.PendingOffers.Add(new() { SourceDocumentId = source.DocumentId, SourceActionId = "exact_action", ProposerKingdomId = "a", TargetKingdomId = "b", Status = "open" });
        var job = Generation(o, r); job.SourceDocumentId = source.DocumentId; job.IsResponse = true;
        job.UserPrompt = "private ORIGINAL_SECRET and original facts"; job.SystemPrompt = "rules";
        Invoke(o, "PrepareDiplomacyPrivacy", job);
        o.TryCompleteDiplomacyPipeline(job, new() { Success = true, Content = "{\"safe\":true,\"public_context\":\"A vague safe summary\"}" });
        Invoke(o, "PrepareDiplomacyPrivacy", job);
        Test.True(job.UserPrompt.Contains("exact_offer") && job.UserPrompt.Contains("exact_action") && job.UserPrompt.Contains("123")
            && job.UserPrompt.Contains("17") && job.UserPrompt.Contains("eligible_targets") && job.UserPrompt.Contains("legal_actions"),
            "real post-isolation request restores source IDs, payment direction, amounts, duration and legal targets even if summary loses them");
        Test.True(!job.UserPrompt.Contains("ORIGINAL_SECRET"), "authoritative context never appends raw private material");
        var hidden = new WorldDiplomacyDocument { DocumentId = "undelivered", RoundId = r.RoundId, AuthorKingdomId = "c", TargetKingdomId = "b", IsReadyForPublication = true };
        o.AddDocument(hidden); r.PendingOffers.Add(new() { SourceDocumentId = hidden.DocumentId, ProposerKingdomId = "c", TargetKingdomId = "b", Status = "open" });
        string publicContext = (string)Invoke(o, "BuildPublicExecutionContext", job)!;
        Test.True(!publicContext.Contains("undelivered"), "post-isolation context cannot expose an unknown offer");
    }
    private static void TechnicalRecovery()
    {
        var (h, o, r) = Fixture(); var job = Generation(o, r);
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            o.CommitFailedJob(job, "pipeline_json_invalid");
            Test.True(!o.CanDispatchDiplomacyJob(job) && r.State == "active" && r.EventEndingId == "", "failed JSON is delayed technical work, never a political result");
            job.RetryAfterUtcTicks = 0; job.NextRecoveryProbeUtcTicks = 0;
            bool eligible = o.CanDispatchDiplomacyJob(job);
            Test.True(eligible == (attempt < 3), "same failed input has a finite persisted recovery budget: " + attempt);
        }
        var savedJob = JsonConvert.DeserializeObject<WorldDiplomacyJob>(JsonConvert.SerializeObject(job))!;
        Test.True(savedJob.TechnicalRecoveryAttempts == 3 && savedJob.SuspendedReason != "", "reload cannot erase exhausted request budget");
        o.CurrentStorage.Jobs[0] = savedJob;
        Test.True(!o.CanDispatchDiplomacyJob(savedJob), "restored exhausted job does not spin paid requests");
        Invoke(o, "RefreshEventRequests", r, "enabled_revalidate");
        Test.True(savedJob.SuspendedReason == "" && savedJob.TechnicalRecoveryAttempts == 0 && r.TechnicalHoldReason == "", "existing enable control safely releases the retained request with a fresh bounded budget");
        o.CommitFailedJob(savedJob, "again"); savedJob.RetryAfterUtcTicks = 0;
        r.TechnicalHoldReason = "event_document_execution_uncertain";
        Invoke(o, "TryResumeTechnicalRequest", savedJob, true);
        Test.True(savedJob.SuspendedReason != "" && r.TechnicalHoldReason == "event_document_execution_uncertain", "explicit resume cannot reroll an uncertain actual execution");
        Test.True(h.Writes == 0, "technical recovery never applies gameplay effects");
        var (mh, mo, mr) = Fixture(); var first = Generation(mo, mr);
        var second = new WorldDiplomacyJob { JobId = "second", RoundId = mr.RoundId, Kind = "event_progress", AuthorKingdomId = "a" }; mo.EnqueueJob(second);
        mr.PlayerNegotiationWaits.Add(new() { SourceDocumentId = "waiting", NotifiedHour = mh.CurrentCampaignHours(), RequiresAnswer = true });
        double originalHour = mr.PlayerNegotiationWaits[0].NotifiedHour;
        mo.CommitFailedJob(first, "first_failure"); mo.CommitFailedJob(second, "second_failure");
        mh.CurrentDayValue += 4;
        Invoke(mo, "TryResumeTechnicalRequest", second, true);
        Test.True(mr.TechnicalHoldReason != "", "one recovered job cannot release another held request");
        Invoke(mo, "TryResumeTechnicalRequest", first, true);
        Test.True(mr.TechnicalHoldReason == "" && mr.PlayerNegotiationWaits[0].NotifiedHour == originalHour + 96,
            "reverse recovery order clears only the request-owned hold and excludes all technical waiting hours");
    }
    private static void TriggerHandoff()
    {
        var (_, o, r) = Fixture();
        var decision = new WorldDiplomacyJob { JobId = "decision", Kind = "player_decision", RoundId = r.RoundId,
            AuthorKingdomId = "a", TargetKingdomId = "b", ForcedIntent = "opening", UserPrompt = "ACTUAL_RECORDED_PLAYER_EVENT; confidentiality applies",
            EventMaterialSnapshot = "RECORDED_PUBLIC_BATTLE" };
        o.EnqueueJob(decision);
        Invoke(o, "CompletePlayerDecision", decision, new JObject { ["participate"] = true, ["reason"] = "CURRENT_CAUSE" });
        var concept = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept");
        o.PrepareSharedRequest(concept);
        Test.True(concept.UserPrompt.Contains("ACTUAL_RECORDED_PLAYER_EVENT") && concept.UserPrompt.Contains("CURRENT_CAUSE")
            && concept.EventTriggerMaterial.Contains("source_is_private"), "actual agenda-to-concept handoff carries attributed original evidence and motive");
        Test.True(JArray.Parse(concept.EventMaterialSnapshot).OfType<JObject>().Any(x => (string?)x["source_kind"] == "recorded_agenda_facts"
            && (string?)x["known_by"] == "a" && (string?)x["text"] == "RECORDED_PUBLIC_BATTLE"), "original recorded public facts remain usable as attributed concept evidence without disclosing private recall");
        string frozen = concept.EventTriggerMaterial;
        concept.Kind = "event_concept_audit"; concept.PrivacyDraft = "{}"; o.PrepareSharedRequest(concept);
        Test.True(concept.EventTriggerMaterial == frozen && concept.UserPrompt.Contains("ACTUAL_RECORDED_PLAYER_EVENT"), "concept audit consumes the exact same frozen triggering evidence");
        Test.True(JsonConvert.DeserializeObject<WorldDiplomacyJob>(JsonConvert.SerializeObject(concept))!.EventTriggerMaterial == frozen,
            "trigger evidence survives job serialization");
    }
    private static void AffiliatedReceipt()
    {
        var (h, o, r) = Fixture(); h.AffiliatedParty = "b";
        var doc = new WorldDiplomacyDocument { DocumentId = "delivered_to_vassal_realm", RoundId = r.RoundId,
            AuthorKingdomId = "a", TargetKingdomId = "b", IsReadyForPublication = true, RequiresResponse = true };
        o.AddDocument(doc);
        WorldDiplomacyPropagationApplication.ReceiveCourt(o.CurrentStorage, doc, "b", h.CurrentDayValue, () => true,
            () => Invoke(o, "ProcessPlayerEventCourtArrival", "b", doc));
        Test.True(doc.HasReachedPlayerCourt && !h.IsPlayerParty("b"), "player vassal receives affiliated-court notice without being promoted to sovereign");
        Test.True(o.CurrentStorage.Jobs.Any(x => x.AuthorKingdomId == "b"), "affiliated NPC sovereign still handles its own response");
    }
    private static void ProgressEvidence()
    {
        var (h, o, r) = Fixture();
        var doc = new WorldDiplomacyDocument { DocumentId = "actual_apology", RoundId = r.RoundId, AuthorKingdomId = "a",
            TargetKingdomId = "b", IsReadyForPublication = true, ChangedDiplomaticState = false, Body = "A public apology" };
        o.AddDocument(doc);
        JObject Update(string evidence) => new() { ["title"] = "Negotiation progress", ["body"] = "The apology eases tensions.",
            ["effects"] = new JArray(new JObject { ["kind"] = "ruler_relation", ["kingdom"] = "a", ["other"] = "b", ["delta"] = 1,
                ["days"] = 0, ["reason"] = "The delivered apology eases tensions." }), ["confirmed_documents"] = new JArray(),
            ["progress_evidence"] = new JArray(evidence), ["remove_effects"] = new JArray(), ["narrative"] = new JArray() };
        bool Apply(string stage, JObject body) { object?[] args = { r, stage, body, "" }; return (bool)Invoke(o, "TryPublishEventBoundary", args)!; }
        Test.True(!Apply("progress:1", Update("missing")) && h.Writes == 0, "invented progress reference cannot change stats");
        Test.True(!Apply("progress:1", Update(doc.DocumentId)) && h.Writes == 0, "undelivered rhetoric cannot change the uninformed ruler's relation");
        foreach (string id in new[] { "a", "b" }) WorldDiplomacyPropagationApplication.ReceiveCourt(o.CurrentStorage, doc, id, h.CurrentDayValue, () => false, () => { });
        Test.True(Apply("progress:1", Update(doc.DocumentId)) && h.Relation == 1 && !doc.ChangedDiplomaticState,
            "actual delivered negotiating progress can change relation without pretending a treaty or war executed");
        Test.True(Apply("progress:1", Update(doc.DocumentId)) && h.Writes == 1, "same receipt retry applies effect once");
        Test.True(!Apply("progress:2", Update(doc.DocumentId)) && h.Writes == 1, "same evidence cannot repeatedly stack effects across conversation revisions");
        var receipt = o.CurrentStorage.EventAnnouncements.Single();
        Test.True(receipt.State == "applied" && receipt.ProgressEvidenceIds.Single() == doc.DocumentId && receipt.ConfirmedDocumentIds.Count == 0,
            "persisted progress receipt distinguishes evidence from executed actions and produces no public narrator announcement");
    }
}
