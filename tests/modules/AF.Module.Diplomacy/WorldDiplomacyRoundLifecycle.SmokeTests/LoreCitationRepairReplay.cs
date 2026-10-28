using AnimusForge;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    private static object CitationCheck(string method, params object[] args) => typeof(WorldDiplomacyOrchestration)
        .GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.Invoke(null, args)!;
    internal static void LoreCitationRepair()
    {
        var fixture = JObject.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "lore-citation-live-20261024.json")));
        var (h, o) = Owner();
        var draft = new JObject { ["body"] = "A source-bound draft remains unchanged during citation repair." };
        var job = new WorldDiplomacyJob { RoundId = "citation-replay", ForcedIntent = "started", EventNarrativeReviewVersion = 5,
            EventMaterialSnapshot = new JArray(new JObject { ["key"] = fixture["key"], ["source_kind"] = "world_book_account", ["text"] = fixture["text"] }).ToString(),
            UserPrompt = new JObject { ["narrative_safety_review_required"] = new JObject {
                ["digest"] = (string)CitationCheck("NarrativeSafetyDigest", new WorldDiplomacyJob { RoundId = "citation-replay", ForcedIntent = "started" }, draft)! } }.ToString() };
        var request = JObject.Parse(job.UserPrompt); request["sources"] = JArray.Parse(job.EventMaterialSnapshot); job.UserPrompt = request.ToString();
        var audit = Approved(job);
        var basis = (JArray)audit["narrative_safety"]!["world_book_basis"]!;
        basis[0]["quote"] = fixture["rejected_quote"];
        object[] args = { job, draft, audit, "", false };
        Test.True(!(bool)CitationCheck("ValidateNarrativeSafety", args)! && !(bool)args[4]
            && ((string)args[3]).Contains("quote_not_contiguous_source_text")
            && ((string)args[3]).Contains((string)fixture["rejected_quote"]!),
            "actual failed model quote is identified as a paraphrase, not missing metadata or an unknown key");
        basis[0]["quote"] = ((string)fixture["text"]!).Substring(0, 100);
        Test.True((bool)CitationCheck("ValidateNarrativeSafety", args)!, "an exact source passage passes the same unchanged draft");
        basis[0]["source_key"] = "unknown";
        Test.True(!(bool)CitationCheck("ValidateNarrativeSafety", args)! && ((string)args[3]).Contains("source_key_not_supplied"), "unknown source has a distinct correction");
        basis[0]["source_key"] = fixture["key"]; basis[0]["quote"] = new string('x', 601);
        Test.True(!(bool)CitationCheck("ValidateNarrativeSafety", args)! && ((string)args[3]).Contains("quote_length_must_be_6_to_600"), "length failure remains bounded and specific");
        audit["narrative_safety"]!["world_identity_valid"] = false;
        ((JObject)audit["narrative_safety"]!).Remove("world_book_basis");
        Test.True(!(bool)CitationCheck("ValidateNarrativeSafety", args)! && (bool)args[4], "unsupported identity returns to the writer without demanding fabricated positive evidence");

        foreach (string kind in new[] { "event_concept_audit", "event_boundary_audit", "event_progress_audit" })
        {
            (h, o) = Owner();
            WorldDiplomacyPlayerApplication.SubmitPlayerDocument(new SubmissionWorld(o), "A real player claim.", o);
            var root = o.CurrentStorage.Documents.Single(); var round = o.ResolveRound(root.RoundId);
            string prefix = kind == "event_boundary_audit" ? "boundary_audit_invalid:" : kind + "_invalid:";
            var held = new WorldDiplomacyJob { JobId = "held-citation", Kind = kind, RoundId = root.RoundId,
                EventNarrativeReviewVersion = 4, PrivacyDraft = draft.ToString(), RoundConversationRevision = round.ConversationRevision,
                RecoveryRevision = round.ConversationRevision, RecoveryConfiguration = (string)Call(o, "SpeechConfiguration")!,
                SuspendedReason = prefix + "narrative_world_book_review_missing:provide_world_identity_valid_and_exact_source_quotes", TechnicalRecoveryAttempts = 3 };
            o.CurrentStorage.Jobs.Add(held); round.TechnicalHoldReason = round.TechnicalRequestHoldReason = held.SuspendedReason;
            Call(o, "TryResumeTechnicalRequest", held, false);
            Test.True(held.Kind == kind && held.EventNarrativeReviewVersion == 5 && held.TechnicalRecoveryAttempts == 0
                && held.SuspendedReason == "" && held.PrivacyDraft == draft.ToString(), "old exhausted generic citation failure resumes the same draft once: " + kind);
            held.SuspendedReason = prefix + "narrative_world_book_citation_invalid:controlled_failure";
            held.TechnicalRecoveryAttempts = 3; held.NextRecoveryProbeUtcTicks = 0; held.RetryAfterUtcTicks = 0;
            Reload(o); held = o.CurrentStorage.Jobs.Single(x => x.JobId == "held-citation");
            Call(o, "TryResumeTechnicalRequest", held, false);
            Test.True(held.TechnicalRecoveryAttempts == 3 && held.SuspendedReason != "", "new exhausted failures cannot refill their budget on reload: " + kind);
        }
    }
}
