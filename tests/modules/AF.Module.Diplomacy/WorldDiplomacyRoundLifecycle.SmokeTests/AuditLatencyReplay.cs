using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    internal static void AuditLatency(string incidentPath = null)
    {
        var incidents = incidentPath == null ? new JArray() : JArray.Parse(File.ReadAllText(incidentPath));
        var missing = incidents.OfType<JObject>().Select(x => x["draft"] as JObject)
            .Where(x => x?["safe"]?.Type == JTokenType.Boolean && (bool)x["safe"]!
                && x["position_owner_valid"] == null && x["negotiation_consistent"]?.Value<bool>() == true).ToList();
        if (missing.Count == 0) missing.Add(new JObject { ["safe"] = true, ["negotiation_consistent"] = true });
        foreach (var actual in missing)
        {
            var (_, o, _, j) = EvidenceOpening(); SubmitEvidenceDraft(o, j, "We seek recognition through a public hearing.");
            string draft = j.PrivacyDraft;
            Complete(o, j, (JObject)actual!.DeepClone());
            Test.True(j.Kind == "privacy_audit" && j.PrivacyDraft == draft && j.PrivacyRewrites == 0
                && j.PrivacyRepairFeedback.Contains("position_owner_valid_boolean_missing") && o.CurrentStorage.Documents.Count == 0,
                "unaltered incident omission stays an unpublished same-draft review, never a prose rejection");
        }
        {
            var (h, o, r, j) = EvidenceOpening(); SubmitEvidenceDraft(o, j, "We seek recognition through a public hearing.");
            string draft = j.PrivacyDraft;
            var checks = (JObject)Call(o, "DeclarationAuditRequiredChecks", j);
            Test.True(checks.Properties().Select(x => x.Name).ToHashSet().SetEquals(new[] { "safe", "position_owner_valid", "negotiation_consistent" })
                && checks.Properties().All(x => (string)x.Value == "boolean") && j.UserPrompt.Contains("required_boolean_reviews"),
                "real request lists applicable review types without supplying approved answers");
            string output = DiplomacyPromptTexts.Get("DeclarationAuditOutput", "");
            using var reader = new JsonTextReader(new StringReader(output.Substring(output.IndexOf('{'))));
            var example = JObject.Load(reader);
            Test.True(new[] { "safe", "position_owner_valid", "negotiation_consistent", "position_correction_valid", "agreement_consistent", "silence_consistent" }
                .All(x => example[x]?.Type == JTokenType.Boolean), "actual CN/EN output example covers every enforced boolean review");
            for (int cycle = 0; cycle < 3; cycle++)
            {
                for (int n = 0; n < 3; n++) Complete(o, j, (JObject)missing[n % missing.Count]!.DeepClone());
                Test.True(j.SuspendedReason.StartsWith("declaration_audit_invalid:") && j.RetryAfterUtcTicks == 0
                    && j.TechnicalRecoveryAttempts == cycle + 1 && j.PrivacyDraft == draft && j.PrivacyRewrites == 0,
                    "format failure keeps the same finite budget and draft without 30/60 second outage backoff");
                Reload(o); j = o.CurrentStorage.Jobs.Single(x => x.JobId == j.JobId);
                Call(o, "TryResumeTechnicalRequest", j, false); // No clock edits and no explicit user resume.
                Test.True(cycle == 2 ? j.SuspendedReason != "" : j.SuspendedReason == "" && j.Kind == "privacy_audit",
                    "saved format failure resumes promptly twice, then remains bounded across reload");
            }
            Test.True(h.Notices.Count == 0 && o.CurrentStorage.Documents.Count == 0 && o.ResolveRound(r.RoundId).State == "active",
                "persistent malformed reviews remain visible technical holds, never political closure or publication");
        }
        {
            var (h, o, _, j) = EvidenceOpening(); SubmitEvidenceDraft(o, j, "We seek recognition through a public hearing.");
            string draft = j.PrivacyDraft;
            for (int n = 0; n < 3; n++) Complete(o, j, (JObject)missing[n % missing.Count]!.DeepClone());
            Call(o, "TryResumeTechnicalRequest", j, false);
            Complete(o, j, SpanReview(Evidence(o, j), true)); // Explicit controlled semantic judgement.
            Test.True(o.CurrentStorage.Documents.Single(x => x.IsReadyForPublication).Body == (string)JObject.Parse(draft)["body"]!
                && o.CurrentStorage.Documents.Single().Day == h.CurrentDayValue,
                "correct review publishes the exact retained body with actual current date, without fake backdating");
        }
        foreach (bool oral in new[] { false, true })
        {
            var (_, o, _, j) = EvidenceOpening(); SubmitEvidenceDraft(o, j, "We seek recognition through a public hearing.");
            if (oral) j.DialogueArrangementId = "agreement-fixture"; else j.SilenceSourceDocumentId = "silence-fixture";
            string key = oral ? "agreement_consistent" : "silence_consistent";
            string draft = j.PrivacyDraft;
            var checks = (JObject)Call(o, "DeclarationAuditRequiredChecks", j);
            Test.True((string)checks[key] == "boolean", "applicable optional review is explicitly requested");
            var malformed = SpanReview(Evidence(o, j), true); malformed[key] = "true";
            Complete(o, j, malformed);
            Test.True(j.Kind == "privacy_audit" && j.PrivacyRewrites == 0 && j.PrivacyDraft == draft
                && j.PrivacyRepairFeedback.Contains(key + "_boolean_missing"), "mistyped optional finding repairs the reviewer, not the author");
        }
        {
            var (_, o, _, j) = EvidenceOpening(); SubmitEvidenceDraft(o, j, "We seek recognition through a public hearing.");
            var rejected = SpanReview(Evidence(o, j), true); rejected["negotiation_consistent"] = false;
            rejected["repair_feedback"] = "The declaration must answer the actual counterproposal.";
            Complete(o, j, rejected);
            Test.True(j.Kind == "generate" && j.PrivacyRewrites == 1 && o.CurrentStorage.Documents.Count == 0,
                "a real substantive rejection still requires rewriting and cannot use the format fast path");
        }
        {
            var (_, o, _, j) = EvidenceOpening(); SubmitEvidenceDraft(o, j, "We seek recognition through a public hearing.");
            o.TryCompleteDiplomacyPipeline(j, new() { Success = false, IsServiceFailure = true, Error = "provider_unavailable" });
            Test.True(j.RetryAfterUtcTicks > DateTime.UtcNow.Ticks && j.SuspendedReason == "provider_unavailable",
                "real provider failures retain outage backoff");
        }
        Console.WriteLine("Audit latency: exact incident omissions=" + missing.Count + "; bounded immediate format recovery, actual publication and semantic rejection PASS; real provider/game NOT_RUN.");
    }
}
