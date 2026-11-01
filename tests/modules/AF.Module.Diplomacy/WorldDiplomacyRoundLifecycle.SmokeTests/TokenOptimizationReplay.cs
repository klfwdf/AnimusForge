using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    internal static void TokenOptimization()
    {
        var world = new JArray(Enumerable.Range(0, 10).Select(i => new JObject {
            ["kingdom_id"] = "realm_" + i, ["name"] = "Realm {NAME} 王国", ["unknown"] = "retain this",
            ["ruler_relations"] = new JArray(Enumerable.Range(0, 10).Select(k => new JObject {
                ["kingdom"] = "realm_" + k, ["value"] = i - k, ["minimum"] = -100, ["maximum"] = 100 })),
            ["towns"] = new JArray(new JObject { ["id"] = "town_" + i, ["loyalty"] = 31.25m,
                ["loyalty_minimum"] = 0, ["loyalty_maximum"] = 100, ["food_maximum"] = i == 0 ? 300 : 1000,
                ["map_x"] = 12.321, ["omitted_records"] = true }), ["town_sample_incomplete"] = true }));
        string raw = world.ToString(Formatting.None);
        Test.True(DiplomacyRequestProjection.TryWorld(raw, out var compact), "production world projection accepts the actual schema");
        var expanded = (JArray)compact["kingdoms"]!.DeepClone();
        foreach (var realm in expanded.OfType<JObject>())
            foreach (var pair in new[] { ("ruler_relations", "ruler_relation_defaults"), ("towns", "town_defaults") })
                foreach (var row in ((JArray)realm[pair.Item1]!).OfType<JObject>())
                    foreach (var field in ((JObject)compact[pair.Item2]!).Properties())
                        if (row[field.Name] == null) row[field.Name] = field.Value.DeepClone();
        Test.True(JToken.DeepEquals(world, expanded), "expanding shared bounds restores every original fact, identity, unknown field and omission flag");
        Test.True(compact["town_defaults"]!["food_maximum"] == null, "nonuniform bounds are never guessed");
        Test.True(world.ToString(Formatting.None) == raw, "projection cannot mutate its input evidence");
        var job = new WorldDiplomacyJob { Kind = "event_concept_audit", SystemPrompt = "Review the supplied facts.",
            UserPrompt = new JObject { ["sources"] = new JArray(new JObject { ["key"] = "world:current", ["source_kind"] = "current_world",
                ["version"] = "original-digest", ["text"] = raw }), ["proposed_event"] = new JObject { ["body"] = "Exact original: no consent. 原文。" } }.ToString() };
        string original = job.UserPrompt;
        var sent = WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(job, _ => throw new Exception("unexpected history read"));
        var actual = JObject.Parse((string)sent.Last!["content"]!);
        Test.True(actual["sources"]![0]!["text"] is JObject && (string)actual["sources"]![0]!["version"]! == "original-digest",
            "actual message consumer projects only the copy, retaining evidence identity");
        Test.True(((string)sent[0]!["content"]!).Contains("af_world_defaults_v1") && job.UserPrompt == original,
            "actual CN/EN system contract accompanies projection without changing saved prompts");
        Test.True(sent.Sum(x => ((string)x["content"]!).Length) < original.Length, "including the explanatory contract still reduces input");
        Console.WriteLine("World request characters before=" + (original.Length + job.SystemPrompt.Length)
            + " after=" + sent.Sum(x => ((string)x["content"]!).Length));
        Test.True(DiplomacyRequestProjection.Project("event_concept", "{broken", out _) == "{broken", "malformed data falls back unchanged");
        Test.True(DiplomacyRequestProjection.Project("generate", original, out _) == original, "ordinary prose and frozen writer contracts are not structurally rewritten");
        Test.True(!DiplomacyRequestProjection.TryWorld("[1,2]", out _), "unknown world shapes are not reinterpreted");
        Test.True(DiplomacyRequestProjection.AppendContextOnce("request", "one\nfull exact context\ntwo", "full exact context") == "request",
            "exact generated context is supplied once");
        Test.True(DiplomacyRequestProjection.AppendContextOnce("request", "full OLD context", "full NEW context").Contains("full NEW context"),
            "changed context cannot reuse an older block");

        var (_, o, r, j) = EvidenceOpening();
        SubmitEvidenceDraft(o, j, "We reject confession and request a hearing. We have not signed a treaty.");
        var continuity = (string)Call(o, "BuildEventContinuity", r, j.AuthorKingdomId);
        Test.True(j.PrivacySourceMaterial.Contains(continuity), "fixture passes current event continuity through original source");
        Test.True(!j.UserPrompt.Contains("\n" + continuity), "actual audit no longer appends a second full copy");
        string draft = j.PrivacyDraft;
        for (int i = 0; i < 3; i++) Call(o, "RetryDeclarationAudit", j, "draft_digest_mismatch");
        Test.True(j.SuspendedReason.StartsWith("declaration_audit_invalid:") && j.PrivacyAuditRetries >= 3,
            "three failed local reviews retain their draft and a failure reason");
        j.NextRecoveryProbeUtcTicks = 0; j.RetryAfterUtcTicks = 0;
        Call(o, "TryResumeTechnicalRequest", j, false);
        Test.True(j.SuspendedReason != "" && j.PrivacyAuditRetries >= 3 && j.PrivacyDraft == draft,
            "unchanged input cannot refill an exhausted review budget");
        Reload(o); j = o.CurrentStorage.Jobs.Single(x => x.JobId == j.JobId);
        j.NextRecoveryProbeUtcTicks = 0; j.RetryAfterUtcTicks = 0;
        Call(o, "TryResumeTechnicalRequest", j, false);
        Test.True(j.SuspendedReason != "", "save/reload cannot refill the review allowance");
        Call(o, "TryResumeTechnicalRequest", j, true);
        Test.True(j.SuspendedReason == "" && j.Kind == "privacy_audit" && j.PrivacyDraft == draft,
            "explicit recovery retries the preserved audit, not the writer or a fabricated approval");

        var (_, serviceOwner, _, writer) = EvidenceOpening();
        serviceOwner.PrepareSharedRequest(writer);
        Test.True(writer.Kind == "privacy_material", "ordinary writer first isolates private sources");
        Complete(serviceOwner, writer, new JObject { ["safe"] = true, ["public_context"] = "Public facts only. No assent or treaty." });
        serviceOwner.PrepareSharedRequest(writer);
        Test.True(writer.Kind == "generate", "successful isolation reaches the ordinary writer");
        string isolated = writer.PrivacyPublicContext, source = writer.PrivacySourceMaterial;
        writer.RecoverOnService = true;
        Call(serviceOwner, "HoldTechnicalRequest", writer, "transport_unavailable");
        writer.RetryAfterUtcTicks = 0; writer.NextRecoveryProbeUtcTicks = 0;
        Call(serviceOwner, "TryResumeTechnicalRequest", writer, false);
        Test.True(writer.SuspendedReason == "" && writer.PrivacyPublicContext == isolated,
            "transport recovery retains already completed isolation");
        serviceOwner.PrepareSharedRequest(writer);
        Test.True(writer.Kind == "generate" && writer.PrivacySourceMaterial == source,
            "actual next request validates unchanged sources and avoids a second isolation call");
        writer.UserPrompt = "Fresh hard fact and a newly delivered confidentiality promise.";
        Call(serviceOwner, "PrepareDiplomacyPrivacy", writer);
        Test.True(writer.Kind == "privacy_material" && writer.PrivacyPublicContext == "" && writer.PrivacySourceMaterial.Contains("newly delivered"),
            "changed source invalidates reuse even when the conversation revision is unchanged");

        foreach (string stage in new[] { "privacy_audit", "event_concept_audit", "event_boundary_audit", "event_progress_audit" })
        {
            var (_, auditOwner, _, audit) = EvidenceOpening();
            SubmitEvidenceDraft(auditOwner, audit, "No agreement has been accepted. We request a hearing.");
            audit.Kind = stage; audit.ForcedIntent = "ended"; audit.RecoverOnService = true;
            string retained = audit.PrivacyDraft;
            Call(auditOwner, "HoldTechnicalRequest", audit, "transport_unavailable");
            audit.RetryAfterUtcTicks = 0; audit.NextRecoveryProbeUtcTicks = 0;
            Call(auditOwner, "TryResumeTechnicalRequest", audit, false);
            Test.True(audit.SuspendedReason == "" && audit.Kind == stage && audit.PrivacyDraft == retained,
                stage + " service recovery retains the paid writer output and retries only its audit");
        }
    }
}
