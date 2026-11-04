using System.Reflection;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Reproduce the recorded failure shapes through the production planning owner.
// Game snapshots and model responses are controlled; no live provider is used.
internal static class PlanningRepairReplay
{
    private sealed class Host : EventStructureReplay.Host
    {
        public override int EstimateTokens(string text) => (text?.Length ?? 0) / 3;
    }
    private static object Call(WorldDiplomacyOrchestration owner, string method, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(owner, args)!;
    private static (Host h, WorldDiplomacyOrchestration o, WorldDiplomacyJob j) Start()
    {
        var h = new Host { PublishEnabled = true, CurrentDayValue = 40 };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        o.TryScheduleNormalRound();
        var j = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept");
        o.PrepareSharedRequest(j);
        return (h, o, j);
    }
    private static void Complete(WorldDiplomacyOrchestration o, WorldDiplomacyJob j, JObject value) =>
        o.TryCompleteDiplomacyPipeline(j, new LlmJobResult { Success = true, Content = value.ToString(Formatting.None) });
    private static JObject Candidate(string id) => new() {
        ["id"] = id, ["kingdoms"] = new JArray("a", "b"), ["material_keys"] = new JArray("world:current"),
        ["conflict"] = "Competing interests", ["motive"] = "A current need", ["new_development"] = "A proposed NPC petition",
        ["theme"] = "history", ["theme_reason"] = "Historical claims", ["npc_creation"] = "A proposal, not an executed action",
        ["negotiation_space"] = "The other court may refuse" };
    private static JObject Comparison(params string[] ids) => new() {
        ["assessments"] = new JArray(ids.Select(id => new JObject { ["id"] = id, ["qualified"] = true,
            ["strength"] = "Grounded", ["weakness"] = "No response yet", ["novelty_reason"] = "New proposal",
            ["urgency_reason"] = "Current motive", ["duplicate_of"] = "" })),
        ["best_equivalent"] = new JArray(ids[0]), ["urgent"] = new JArray(), ["comparison_reason"] = "Current evidence" };
    private static void Resume(WorldDiplomacyOrchestration o, WorldDiplomacyJob j, bool explicitly = false)
    {
        j.NextRecoveryProbeUtcTicks = 0; j.RetryAfterUtcTicks = 0;
        Call(o, "TryResumeTechnicalRequest", j, explicitly);
    }

    internal static void Run()
    {
        foreach (string language in new[] { "简体中文", "English" })
        {
            PromptLanguage.Install(() => language, id => id == "简体中文");
            ComparisonAndCumulativeBudget(); AdmissionContract(); AuditPreservesDraft(); FullEnvelopeAndExactDuplicate(); SupplementalBudget();
            Console.WriteLine("Planning repair language=" + PromptLanguage.Code + " PASS");
        }
    }

    private static void ComparisonAndCumulativeBudget()
    {
        var (h, o, j) = Start();
        string contract = DiplomacyPromptTexts.Get("PlanningEvidenceContract", "");
        Test.True(contract.Length > 100 && j.SystemPrompt.Contains(contract), "real candidate writer receives the shared proposal/evidence contract");
        Test.True(PromptLanguage.IsChinese ? contract.Contains("不要求来源证明提议已经发出")
            : contract.Contains("sources need not prove that the proposal was already sent"), "actual game-language reader selects the correct paired contract");
        Complete(o, j, new JObject { ["candidates"] = new JArray(Candidate("c1"), Candidate("c2")) });
        o.PrepareSharedRequest(j);
        Test.True(((JArray)JObject.Parse(j.UserPrompt)["assessment_ids"]!).Values<string>().SequenceEqual(new[] { "c1", "c2" }),
            "comparison advertises candidate IDs separately from countries");
        Complete(o, j, Comparison("c1", "khuzait", "battania", "sturgia", "aserai", "empire_s"));
        Test.True(j.MaterialRepairFeedback.Contains("exactly_2_rows") && j.MaterialRepairFeedback.Contains("valid_ids=c1,c2")
            && j.PlanningFailureAttempts == 1, "recorded six-country assessment error gets exact count and allowed IDs");
        Resume(o, j); o.PrepareSharedRequest(j); Complete(o, j, Comparison("c1", "c2"));
        Test.True(j.EventSelectionStage == "expand" && j.PlanningFailureAttempts == 1, "successful comparison does not refill task failures");
        // The ordinary local budget can still select a viable sibling, but its
        // cumulative failures and saved allowance belong to the same task.
        for (int n = 0; n < 3; n++)
        { Call(o, "HoldTechnicalRequest", j, "event_concept_invalid:controlled"); if (n < 2) Resume(o, j); }
        Test.True(j.EventSelectionStage == "compare" && j.PlanningFailureAttempts == 4, "candidate switch keeps cumulative failure count");
        o.PrepareSharedRequest(j); Complete(o, j, Comparison("c2"));
        Test.True(j.EventSelectionStage == "expand" && j.PlanningFailureAttempts == 4, "new sibling success cannot refill failures");
        Call(o, "HoldTechnicalRequest", j, "event_concept_invalid:controlled"); Resume(o, j);
        Call(o, "HoldTechnicalRequest", j, "event_concept_invalid:controlled");
        string candidate = j.EventSelectedCandidate, material = j.EventMaterialSnapshot;
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
        j = o.CurrentStorage.Jobs.Single(x => x.JobId == j.JobId);
        int reads = h.MaterialReads;
        for (int n = 0; n < 5; n++) Resume(o, j);
        Test.True(j.PlanningFailureAttempts == 6 && j.SuspendedReason.Length > 0 && j.EventSelectedCandidate == candidate
            && j.EventMaterialSnapshot == material && reads == h.MaterialReads, "six failures stop the task across save/load without changing candidate or rereading");
        Resume(o, j, true);
        Test.True(j.SuspendedReason.Length == 0 && j.PlanningFailureAttempts == 0 && j.EventSelectedCandidate == candidate,
            "explicit retry resumes the retained work with a fresh bounded allowance");
    }

    private static void AdmissionContract()
    {
        var (_, o, j) = Start();
        Complete(o, j, new JObject { ["candidates"] = new JArray(Candidate("c1")) });
        o.PrepareSharedRequest(j); Complete(o, j, Comparison("c1")); o.PrepareSharedRequest(j);
        string contract = DiplomacyPromptTexts.Get("PlanningEvidenceContract", "");
        Test.True(j.SystemPrompt.Contains(contract) && (string)JObject.Parse(j.UserPrompt)["phase"]! == "identify",
            "identification receives the same proposal semantics as generation");
        var seed = new JObject { ["direct_subjects"] = new JArray(new JObject { ["kind"] = "kingdom", ["id"] = "a",
            ["source"] = "world:current", ["action"] = "Propose negotiations; not yet sent or received",
            ["target"] = new JObject { ["kind"] = "kingdom", ["id"] = "b", ["source"] = "world:current" } }) };
        Complete(o, j, seed); o.PrepareSharedRequest(j);
        var input = JObject.Parse(j.UserPrompt);
        Test.True(j.SystemPrompt.Contains(contract) && (string)input["phase"]! == "audit", "independent admission audit receives the same proposal semantics");
        Complete(o, j, new JObject { ["approved"] = true, ["seed_digest"] = input["seed_digest"],
            ["reason"] = "Both identities and the proposed action are supported; no execution is asserted" });
        Test.True(j.CountryAdmission.Length > 0 && j.PlanningFailureAttempts == 0 && o.CurrentStorage.EventEffects.Count == 0,
            "accepting supported proposal identities is not an execution result or a failure");
        o.PrepareSharedRequest(j); o.PrepareSharedRequest(j);
        Test.True(j.SystemPrompt.Contains(contract) && j.SystemPrompt.Contains(DiplomacyPromptTexts.Get("EventConceptWriter", "MISSING")),
            "expansion uses the same evidence-status rules");
    }

    private static void AuditPreservesDraft()
    {
        var (_, o, j) = Start();
        j.Kind = "event_concept_audit"; j.EventSelectionStage = "expand";
        j.PrivacyDraft = "{\"title\":\"reviewed draft\"}";
        for (int n = 0; n < 3; n++) Call(o, "RetryConceptAudit", j, "approved_boolean_missing");
        Resume(o, j);
        Test.True(j.Kind == "event_concept_audit" && j.PrivacyDraft.Contains("reviewed draft")
            && j.SuspendedReason.Length > 0 && j.MaterialSearchPass == 0 && j.PlanningFailureAttempts == 3,
            "exhausted audit format corrections preserve the generated draft instead of rerolling candidates");
        Resume(o, j, true);
        Test.True(j.Kind == "event_concept_audit" && j.PrivacyDraft.Contains("reviewed draft"), "explicit audit retry reuses the same generated result");
    }

    private static void FullEnvelopeAndExactDuplicate()
    {
        var (h, o, j) = Start();
        string facts = "A quoted condition must remain complete: " + new string('x', 36000);
        j.EventTriggerMaterial = new JObject { ["known_public_facts"] = facts,
            ["original_evidence"] = "BEFORE\n" + facts + "\nAFTER", ["source_is_private"] = true }.ToString(Formatting.None);
        string original = j.EventTriggerMaterial;
        o.PrepareSharedRequest(j);
        var trigger = JObject.Parse(j.UserPrompt)["trigger_material"]!;
        Test.True((string)trigger["known_public_facts"]! == facts && ((string)trigger["original_evidence"]!).Contains("[evidence_ref:")
            && ((string)trigger["original_evidence"]!).StartsWith("BEFORE") && ((string)trigger["original_evidence"]!).EndsWith("AFTER")
            && j.EventTriggerMaterial == original, "only the exact duplicate is referenced; complete evidence, surrounding text and saved original survive");
        j.EventTriggerMaterial = new JObject { ["known_public_facts"] = new string('x', 120000) }.ToString(Formatting.None);
        o.PrepareSharedRequest(j);
        int reads = h.MaterialReads; string frozen = j.EventMaterialSnapshot;
        for (int n = 0; n < 4; n++) Resume(o, j);
        Test.True(!j.RequestPreparationPending && j.SuspendedReason.Length == 0
            && j.PlanningFailureAttempts == 0 && j.MaterialSearchPass == 0 && j.EventMaterialSnapshot == frozen && reads == h.MaterialReads,
            "oversized mandatory trigger remains sendable without switching candidates or repeated preparation");
    }

    private static void SupplementalBudget()
    {
        foreach (bool oversizeMandatory in new[] { false, true })
        {
            var (h, o, j) = Start();
            j.EventSelectionStage = "expand"; j.EventSelectedCandidate = Candidate("c1").ToString(Formatting.None);
            j.EventComparison = "{}";
            j.CountryAdmission = JsonConvert.SerializeObject(DiplomacyCountryAdmission.Calculate(
                new DiplomacyAdmissionInput { Direct = new() { "a", "b" }, Candidates = new() { "a", "b" } }, 1, "test"));
            j.EventTriggerMaterial = new JObject { ["known_public_facts"] = new string('z', 60000) }.ToString(Formatting.None);
            h.MaterialOverride = new JArray(new[] { "a", "b" }.SelectMany(id => new[] {
                new JObject { ["key"] = "required:" + id, ["known_by"] = id, ["source_kind"] = "character_setting",
                    ["text"] = oversizeMandatory ? new string('r', 100000) : "Mandatory complete persona" },
                new JObject { ["key"] = "optional:" + id, ["known_by"] = id, ["source_kind"] = "attributed_account", ["text"] = new string('q', 30000) }
            })).ToString(Formatting.None);
            int reads = h.MaterialReads;
            for (int n = 0; n < 4 && j.SuspendedReason.Length == 0; n++) o.PrepareSharedRequest(j);
            if (oversizeMandatory)
            {
                Resume(o, j);
                Test.True(j.SuspendedReason.Length == 0 && j.AdmissionSupplementedCountries.Count == 2
                    && j.EventMaterialSnapshot.Contains("required:a") && j.EventMaterialSnapshot.Contains("required:b"), "oversized mandatory evidence for both countries is retained and allowed to send");
            }
            else
            {
                Test.True(j.SuspendedReason.Length == 0 && j.AdmissionSupplementedCountries.Count == 2
                    && !j.EventMaterialSnapshot.Contains("optional:") && j.EventMaterialSnapshot.Contains("required:a")
                    && j.EventMaterialSnapshot.Contains("required:b") && h.EstimateTokens(j.UserPrompt) + h.EstimateTokens(j.SystemPrompt) < 32000,
                    "supplement uses complete-envelope headroom and keeps every admitted country's mandatory record");
            }
        }
    }
}
