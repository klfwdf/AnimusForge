using System.Reflection;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialSupplyReplay
{
    private static void RealmVoiceAndProgressBudget()
    {
        var (h, o, job) = Start();
        h.VoiceReady = false;
        foreach (string kind in new[] { "generate", "player_decision", "contact_wait" })
        {
            job.Kind = kind; job.UserPrompt = "old-not-for-send";
            o.PrepareSharedRequest(job);
            Test.True(job.RequestPreparationPending && job.Kind == kind && job.UserPrompt == "old-not-for-send"
                && job.TechnicalRecoveryAttempts == 0 && job.SuspendedReason == "",
                "realm lore pending defers the complete request without altering its lane or exhausting recovery: " + kind);
        }
        Test.True(h.VoicePreparations == 3, "all declaring-party request entrances wait for institutional lore");
        h.VoiceReady = true;

        var towns = new JArray(Enumerable.Range(0, 8).Select(i => new JObject {
            ["id"] = "town" + i, ["name"] = new string('地', 1800), ["map_x"] = i,
            ["nearby_foreign_forts"] = new JArray("foreign") }));
        var world = new JArray(new JObject { ["kingdom_id"] = "a", ["ruler_id"] = "ruler_a",
            ["enemies"] = new JArray("b"), ["ruler_relations"] = new JArray(new JObject { ["kingdom"] = "b", ["value"] = -10 }),
            ["geography_basis"] = "gate_positions_proximity_not_routes", ["towns"] = towns });
        var evidence = new JObject {
            ["world"] = world.ToString(Formatting.None), ["documents"] = new JArray(new string('文', 18000)),
            ["commitments"] = new JArray("exact pending promise"), ["proposed_progress"] = new string('审', 5000),
            ["world_book_sources"] = new JArray("complete attributed source"), ["player_waits"] = new JArray("must answer") };
        var original = (JObject)evidence.DeepClone();
        job.Kind = "event_progress_audit"; job.SystemPrompt = new string('规', 2000); job.UserPrompt = evidence.ToString(Formatting.None);
        var fit = typeof(WorldDiplomacyOrchestration).GetMethod("FitOptionalEventWorld", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Test.True(h.EstimateTokens(job.SystemPrompt) + h.EstimateTokens(job.UserPrompt) > 32000, "fixture reproduces oversized progress audit");
        fit.Invoke(o, new object[] { job });
        var projected = JObject.Parse(job.UserPrompt);
        foreach (string key in new[] { "documents", "commitments", "proposed_progress", "world_book_sources", "player_waits" })
            Test.True(JToken.DeepEquals(projected[key], original[key]), "budget keeps entire authoritative evidence: " + key);
        var compactWorld = JArray.Parse((string)projected["world"]!);
        Test.True((bool)compactWorld[0]["town_sample_incomplete"]! && (int)compactWorld[0]["omitted_town_records"]! > 0,
            "omitted whole town samples explicitly identified");
        foreach (string key in new[] { "kingdom_id", "ruler_id", "enemies", "ruler_relations", "geography_basis" })
            Test.True(JToken.DeepEquals(compactWorld[0][key], world[0][key]), "live realm and geography semantics retained: " + key);
        Test.True(h.EstimateTokens(job.SystemPrompt) + h.EstimateTokens(job.UserPrompt) + 32 < 32000, "oversized audit fits without changing evidence or limits");
        string first = job.UserPrompt; fit.Invoke(o, new object[] { job });
        Test.True(job.UserPrompt == first && towns.Count == 8, "projection is stable and leaves original world records intact");

        job.UserPrompt = new JObject { ["documents"] = new string('文', 35000), ["world"] = world.ToString(Formatting.None) }.ToString(Formatting.None);
        fit.Invoke(o, new object[] { job });
        string failure = "";
        bool fits = WorldDiplomacyLlmDispatchApplication.EnsureRequestFitsInputBudget(job,
            new JArray(new JObject { ["role"] = "system", ["content"] = job.SystemPrompt }, new JObject { ["role"] = "user", ["content"] = job.UserPrompt }),
            32000, 1000, h.EstimateTokens, _ => "", _ => false, (_, reason) => failure = reason, () => {}, _ => {});
        Test.True(!fits && failure.StartsWith("event_required_input_budget_exceeded:")
            && ((string)JObject.Parse(job.UserPrompt)["documents"]!).Length == 35000,
            "oversized required evidence fails explicitly instead of truncating a promise or increasing request limits");

        var round = o.ResolveRound(job.RoundId);
        job.PrivacyDraft = "{\"outcome\":\"continue\",\"reason\":\"same exact draft\"}";
        job.RoundConversationRevision = round.ConversationRevision;
        string draft = job.PrivacyDraft;
        AuditCall(o, "HoldTechnicalRequest", job, "input budget exceeded before send: 32487/32000; old snapshot");
        job.TechnicalRecoveryAttempts = 3; job.RetryAfterUtcTicks = 0; job.NextRecoveryProbeUtcTicks = 0;
        AuditCall(o, "TryResumeTechnicalRequest", job, false);
        Test.True(job.Kind == "event_progress_audit" && job.PrivacyDraft == draft && job.SuspendedReason == "",
            "legacy oversized audit resumes the existing draft, not another paid writer");
        AuditCall(o, "HoldTechnicalRequest", job, failure);
        int attempts = job.TechnicalRecoveryAttempts;
        for (int i = 0; i < 4; i++)
        {
            job.RetryAfterUtcTicks = 0; job.NextRecoveryProbeUtcTicks = 0;
            AuditCall(o, "TryResumeTechnicalRequest", job, false);
        }
        Test.True(job.SuspendedReason == failure && job.TechnicalRecoveryAttempts == attempts && job.PrivacyDraft == draft,
            "unchanged mandatory input cannot loop through automatic retries or discard its audited draft");
    }
}
