using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialSupplyReplay
{
    internal static void PlanningAuditBudget()
    {
        var (h, o, j) = Start();
        j.Kind = "event_concept_audit"; j.EventSelectionStage = "expand";
        j.EventSelectedCandidate = Candidate().ToString(Formatting.None);
        j.PrivacyDraft = new JObject { ["background"] = "A witness disputes the oath: " + new string('"', 4000),
            ["narrative"] = new JArray(), ["participants"] = new JArray() }.ToString(Formatting.None);
        string storedDraft = j.PrivacyDraft, storedCandidate = j.EventSelectedCandidate;
        var sources = new JArray(new JObject { ["key"] = "world:current", ["text"] = "Current world" },
            new JObject { ["key"] = "lore:succession", ["text"] = "Exact original; no confession or consent." });
        var request = new JObject { ["sources"] = sources, ["prior_events"] = new JArray(),
            ["selected_candidate"] = storedCandidate, ["proposed_event"] = storedDraft };
        string system = "\n" + DiplomacyPromptTexts.Get("EventStoryDiversity", "") + "\n" + DiplomacyPromptTexts.Get("EventStoryReview", "");
        int LegacySize() => h.EstimateTokens(system) + h.EstimateTokens(request.ToString(Formatting.None)) + 32;
        while (LegacySize() <= 32200) sources[1]!["text"] = (string)sources[1]!["text"]! + new string('x', 500);
        j.EventMaterialSnapshot = sources.ToString(Formatting.None); j.UserPrompt = request.ToString(Formatting.None); j.SystemPrompt = "";
        string storedSources = j.EventMaterialSnapshot;
        AuditCall(o, "FinishMaterialRequest", j);
        var actual = JObject.Parse(j.UserPrompt);
        Test.True(h.EstimateTokens(j.SystemPrompt) + h.EstimateTokens(j.UserPrompt) + 32 <= 32000 && j.SuspendedReason == "",
            "escaping-only overflow fits the unchanged 32000 gate after lossless structural projection");
        Test.True(JToken.DeepEquals(actual["proposed_event"], JObject.Parse(storedDraft))
            && JToken.DeepEquals(actual["selected_candidate"], JObject.Parse(storedCandidate))
            && JToken.DeepEquals(actual["sources"], sources) && j.PrivacyDraft == storedDraft
            && j.EventMaterialSnapshot == storedSources && j.EventSelectedCandidate == storedCandidate,
            "projection retains every draft/source character, key and stored identity without weakening evidence");
        Test.True(((JArray)actual["required_audit_fields"]!).Values<string>().Contains("story_review"),
            "reviewer receives an explicit output field manifest separate from author draft requirements");
        AuditCall(o, "HoldTechnicalRequest", j, "event_required_evidence_budget_exceeded:retain_prior_cases_and_selected_sources");
        AuditCall(o, "TryResumeTechnicalRequest", j, false);
        Test.True(j.Kind == "event_concept_audit" && j.PrivacyDraft == storedDraft && j.SuspendedReason == "",
            "local audit budget recovery retains the already-written draft instead of spending another writer request");
        Console.WriteLine("Planning audit budget: lossless request projection and same-draft budget recovery PASS; irreducible evidence remains fail-closed.");
    }
}
