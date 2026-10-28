using System.Reflection;
using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialSupplyReplay
{
    private sealed class CurrentWarHost : EventStructureReplay.Host
    {
        internal bool War;
        public override bool IsAtWarByKingdomIds(string firstId, string secondId) => War;
        public override List<(string firstId, string secondId)> ActiveWarKingdomPairs() => War ? new() { ("a", "p") } : new();
    }

    internal static void BoundMatterRecovery()
    {
        foreach (string stage in new[] { "player", "oral" })
        foreach (string legacyReason in new[] { "event_concept_audit_invalid:story_review_missing_or_invalid",
            "event_required_evidence_budget_exceeded:retain_prior_cases_and_selected_sources" })
        {
            var (h, o, j) = Start(); var round = o.ResolveRound(j.RoundId);
            round.EventSourceType = stage == "oral" ? "dialogue_commitment" : "player";
            round.RootDocumentId = stage + "-root";
            if (stage == "player") round.DialogueArrangementId = "player_manual_declaration";
            o.CurrentStorage.Documents.Add(new() { DocumentId = round.RootDocumentId, RoundId = round.RoundId,
                AuthorKingdomId = stage == "player" ? "p" : "a", TargetKingdomId = "b",
                IsPlayerAuthored = stage == "player", IsReadyForPublication = true, AnalysisStatus = "success",
                PlayerAnalysisCommitted = true, Body = "Preserved original undertaking or question." });
            j.EventSelectionStage = stage; j.Kind = "event_concept_audit";
            j.RoundConversationRevision = round.ConversationRevision;
            j.EventSelectedCandidate = "{}";
            j.PrivacyDraft = new JObject { ["background"] = "The confirmed matter continues under the original conditions." }.ToString();
            var sources = new JArray(new JObject { ["key"] = "world:current", ["text"] = "Current authoritative world" },
                new JObject { ["key"] = "confirmed", ["source_kind"] = "confirmed_oral_terms", ["text"] = "Fourteen days, agreed by the same two sovereigns." });
            j.EventMaterialSnapshot = sources.ToString();
            // Real failure shape: three prior cases plus full evidence over 32k.
            var prior = new JArray(Enumerable.Range(0, 3).Select(i => new JObject {
                ["RoundId"] = "unrelated-" + i, ["RoundTopic"] = "Other dispute " + i,
                ["EventBackground"] = new string('x', 85000), ["EventOutcomeReason"] = "No new obligation." }));
            prior.Insert(0, new JObject { ["RoundId"] = "bound-history", ["RoundTopic"] = "Original terms",
                ["EventBackground"] = "Original agreement", ["EventOutcomeReason"] = "Trade starts ONLY after release; neither court admitted guilt." });
            j.EventTriggerMaterial = "Reference bound-history; keep its exact conditions.";
            j.UserPrompt = new JObject { ["sources"] = sources, ["prior_events"] = prior,
                ["proposed_event"] = j.PrivacyDraft, ["selected_candidate"] = "{}" }.ToString();
            string draft = j.PrivacyDraft, snapshot = j.EventMaterialSnapshot;
            j.SystemPrompt = ""; AuditCall(o, "FinishMaterialRequest", j);
            var request = JObject.Parse(j.UserPrompt);
            Test.True(j.SuspendedReason == "" && h.EstimateTokens(j.SystemPrompt) + h.EstimateTokens(j.UserPrompt) + 32 <= 32000,
                stage + ": unrelated historical cases no longer force protected evidence over the send gate");
            Test.True(request["story_review_required"] == null
                && !((JArray)request["required_audit_fields"]!).Values<string>().Contains("story_review")
                && j.SystemPrompt.Contains(DiplomacyPromptTexts.Get("BoundMatterPlanning", "")),
                stage + ": actual request keeps independent safety review without a novelty checklist");
            Test.True(j.PrivacyDraft == draft && JToken.DeepEquals(JArray.Parse(j.EventMaterialSnapshot), sources)
                && ((JArray)request["prior_events"]!).Any(x => (string)x["RoundId"]! == "bound-history"
                    && ((string)x["EventOutcomeReason"]!).Contains("ONLY after release")),
                stage + ": draft, confirmed terms, and explicitly linked historical conditions remain intact");
            object[] args = { j, new JObject(), "", false };
            Test.True((bool)AuditCall(o, "ValidateStoryReview", args), stage + ": novelty review is not required for a real bound matter");

            // Persisted exhausted legacy hold resumes once with the same draft.
            j.StoryReviewVersion = 1; j.TechnicalRecoveryAttempts = 2;
            AuditCall(o, "HoldTechnicalRequest", j, legacyReason);
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            j = o.CurrentStorage.Jobs.Single(x => x.JobId == j.JobId); round = o.ResolveRound(j.RoundId);
            j.RetryAfterUtcTicks = j.NextRecoveryProbeUtcTicks = 0;
            AuditCall(o, "TryResumeTechnicalRequest", j, false);
            Test.True(j.SuspendedReason == "" && j.StoryReviewVersion == 2 && j.Kind == "event_concept_audit"
                && j.PrivacyDraft == draft, stage + ": save/load resumes old exhausted audit without replacing the human/confirmed draft: "
                    + j.SuspendedReason + ";kind=" + j.Kind + ";version=" + j.StoryReviewVersion + ";attempts=" + j.TechnicalRecoveryAttempts);
            j.TechnicalRecoveryAttempts = 2;
            AuditCall(o, "HoldTechnicalRequest", j, legacyReason);
            j.RetryAfterUtcTicks = j.NextRecoveryProbeUtcTicks = 0;
            AuditCall(o, "TryResumeTechnicalRequest", j, false);
            Test.True(j.SuspendedReason != "" && j.TechnicalRecoveryAttempts == 3,
                stage + ": a second failure cannot refill the finite migration budget");
        }

        var warHost = new CurrentWarHost { PublishEnabled = true, PlayerAwareExecution = true };
        var owner = new WorldDiplomacyOrchestration(warHost, new()); warHost.Owner = owner;
        owner.TryScheduleNormalRound(); var job = owner.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept");
        owner.RecordNativeSignal("a", "p", "declare_war", "Declare war on p");
        var material = JObject.Parse((string)AuditCall(owner, "BuildPlayerAgendaMaterial", "a"));
        Test.True(material["known_native_facts"] == null
            && (string)material["captured_native_requests"]![0]!["execution_status"]! == "intercepted_request_not_execution_receipt"
            && (bool)material["captured_native_requests"]![0]!["currently_at_war"]! == false,
            "intercepted native war request is explicitly typed and accompanied by actual current state");
        warHost.War = true;
        material = JObject.Parse((string)AuditCall(owner, "BuildPlayerAgendaMaterial", "a"));
        Test.True((bool)material["captured_native_requests"]![0]!["currently_at_war"]!
            && ((JArray)material["current_war_pairs"]!).Count == 1,
            "a subsequently real war is reported from the authoritative host without upgrading the old request into an execution receipt");
        warHost.War = false;
        string context = WorldDiplomacyDocumentFactRules.BuildRecentNativeSignalContext(owner.CurrentStorage.NativeSignals, "a", "p");
        Test.True(context.Contains(DiplomacyMiscPromptTexts.Get("NativeRequest_NotExecuted", "")),
            "legacy stored reasons also render as requests in shared declaration/oral contexts");
        job.Kind = "event_concept_audit"; job.PrivacyDraft = "{\"background\":\"A falsely asserted current war\"}";
        var required = JObject.FromObject(AuditCall(owner, "NarrativeSafetyRequest", job, null!));
        job.UserPrompt = new JObject { ["narrative_safety_review_required"] = required }.ToString();
        var audit = new JObject { ["narrative_safety"] = MaterialCountryThirdReplay.SafetyReview(job) };
        var validator = typeof(WorldDiplomacyOrchestration).GetMethod("ValidateNarrativeSafety", BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (bool missing in new[] { true, false })
        {
            if (missing) ((JObject)audit["narrative_safety"]!).Remove("diplomatic_state_valid");
            else audit["narrative_safety"]!["diplomatic_state_valid"] = false;
            object[] args = { job, JObject.Parse(job.PrivacyDraft), audit, "", false };
            Test.True(!(bool)validator.Invoke(null, args)!
                && ((string)args[3]).Contains("diplomatic_state") && (bool)args[4] == !missing,
                "missing fact review retries; substantive false-war rejection cannot publish");
        }
        Console.WriteLine("PASS bound matters: protected evidence, finite legacy recovery, native request semantics and independent current-war review.");
    }
}
