using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialCountryThirdReplay
{
    private sealed class WorldBookHost : Host, IDiplomacyWorldBookHost
    {
        internal (string state, int count, string revision) Library = ("empty", 0, "empty-v1");
        internal readonly List<string> Notices = new();
        public (string state, int count, string revision) ReadWorldBookAvailability() => Library;
        public override void Notify(string text) { Notices.Add(text); }
    }

    private static (WorldBookHost h, WorldDiplomacyOrchestration o, WorldDiplomacyJob j) WorldBookCase()
    {
        var h = new WorldBookHost { PublishEnabled = true, PlayerAwareExecution = true, OralRange = (0, 3000), CurrentDayValue = 40 };
        var (_, o) = Owner(h);
        WorldDiplomacyPlayerApplication.SubmitPlayerDocument(new SubmissionWorld(o), "Our claim requires an answer.", o);
        var root = o.CurrentStorage.Documents.Single();
        var analysis = o.CurrentStorage.Jobs.Single(x => x.Kind == "analyze");
        o.CommitAnalysis(analysis, new JObject { ["status"] = "success", ["title_summary"] = "A claim",
            ["actions"] = new JArray(new JObject { ["intent"] = "statement", ["commitment"] = "non_binding" }),
            ["response_requests"] = new JArray(), ["event_participation"] = "participate" }.ToString());
        o.RemoveJob(analysis.JobId);
        var j = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept");
        return (h, o, j);
    }

    internal static void WorldBookWaitEnding()
    {
        foreach (string unavailable in new[] { "unavailable", "load_failed" })
        {
            var (waitingHost, waitingOwner, waitingJob) = WorldBookCase();
            waitingHost.Library = (unavailable, 0, "");
            waitingOwner.PrepareSharedRequest(waitingJob);
            Call(waitingOwner, "TryResumeTechnicalRequest", waitingJob, true);
            Test.True(waitingJob.SuspendedReason == "event_world_book_wait:" + unavailable
                && waitingHost.Notices.Count == 1 && waitingHost.MaterialReads == 0,
                "unavailable/load-failed libraries remain distinct, bounded and non-dispatchable");
        }
        var (h, o, j) = WorldBookCase();
        string originalId = j.DocumentId;
        o.PrepareSharedRequest(j);
        Test.True(j.SuspendedReason == "event_world_book_wait:empty" && h.MaterialReads == 0 && j.UserPrompt == "",
            "empty library waits before retrieval or an impossible model request");
        Test.True(h.Notices.Count == 1 && h.Notices[0] == DiplomacyUiLocalization.Text("AF_DIP_UI_WorldBookEmpty",
            "The world book is empty. Your declaration is preserved, but the event opening needs background sources. Import a world book; the event will retry automatically."), "real localized one-time notice reaches the game notification port");
        int attempts = j.TechnicalRecoveryAttempts;
        for (int i = 0; i < 3; i++)
        {
            Reload(o); j = o.CurrentStorage.Jobs.Single();
            Call(o, "TryResumeTechnicalRequest", j, true);
            Test.True(j.SuspendedReason.EndsWith(":empty") && h.Notices.Count == 1 && j.TechnicalRecoveryAttempts == attempts,
                "reload and explicit resume cannot refill an unchanged empty-library wait or repeat its notice");
        }
        h.Library = ("ready", 2, "import-v2");
        j.NextRecoveryProbeUtcTicks = 0;
        Call(o, "TryResumeTechnicalRequest", j, false);
        Test.True(j.SuspendedReason == "" && j.TechnicalRecoveryAttempts == 0, "a successful import resumes the same job");
        o.PrepareSharedRequest(j);
        CountryAdmissionReplay.FreezeFixture(o, j, new[] { "p", "a", "b" });
        Test.True(JArray.Parse(j.EventMaterialSnapshot).Any(x => (string)x["source_kind"]! == "world_book_account")
            && o.CurrentStorage.Documents.Single().DocumentId == originalId, "fresh sources are captured without republishing the player's document");

        h.MaterialOverride = "[]"; j.EventMaterialSnapshot = ""; j.EventComparison = "{\"supplemented\":true}";
        o.PrepareSharedRequest(j);
        Test.True(j.SuspendedReason == "event_world_book_wait:no_match" && h.Notices.Count == 2,
            "loaded but ungrounded material is distinguished from an empty library");
        Reload(o); j = o.CurrentStorage.Jobs.Single();
        Call(o, "TryResumeTechnicalRequest", j, true);
        Test.True(j.SuspendedReason.EndsWith(":no_match") && h.Notices.Count == 2, "same source revision stays held after reload");
        h.Library = ("ready", 2, "edited-v3"); h.MaterialOverride = null;
        Call(o, "TryResumeTechnicalRequest", j, true); o.PrepareSharedRequest(j);
        Test.True(j.SuspendedReason == "" && JArray.Parse(j.EventMaterialSnapshot).Any(x => (string)x["source_kind"]! == "world_book_account"),
            "same-count content edits invalidate the old missing-evidence snapshot and supplementation");

        // The actual legacy case exhausted all attempts with a nonempty JSON
        // snapshot containing no world book. Restore it without a manual re-post.
        j.SuspendedReason = "event_required_world_book_missing"; j.TechnicalRecoveryAttempts = 3;
        j.EventMaterialSnapshot = "[]";
        Call(o, "TryResumeTechnicalRequest", j, true);
        Test.True(j.SuspendedReason == "" && j.EventMaterialSnapshot == "", "legacy missing-world-book hold gets fresh capture once sources exist");

        var round = o.ResolveRound(j.RoundId); round.EventPhase = "closing";
        j.Kind = "event_boundary_audit"; j.ForcedIntent = "ended"; j.EndingContextVersion = 1;
        j.PrivacyDraft = "{\"body\":\"The documents leave the dispute unresolved.\",\"narrative\":[]}";
        j.RoundConversationRevision = round.ConversationRevision;
        j.SuspendedReason = "boundary_audit_invalid:narrative_world_book_basis_shape:provide_1_to_8_source_key_and_exact_quote_entries_in_narrative_safety.world_book_basis";
        j.TechnicalRecoveryAttempts = 3;
        j.RecoveryConfiguration = (string)Call(o, "SpeechConfiguration");
        j.RecoveryRevision = round.ConversationRevision; j.RecoveryKnowledgeRevision = DiplomacyEventRules.KnowledgeRevision(round, j.AuthorKingdomId);
        j.NextRecoveryProbeUtcTicks = 0;
        Call(o, "TryResumeTechnicalRequest", j, false); o.PrepareSharedRequest(j);
        var sent = JObject.Parse(j.UserPrompt); var checks = (JArray)sent["narrative_safety_review_required"]!["required_checks"]!;
        Test.True(j.EndingContextVersion == 2 && j.SuspendedReason == "" && ((JArray)sent["world_book_sources"]!).Count == 0
            && !checks.Values<string>().Contains("world_book_basis") && checks.Values<string>().Contains("event_record_basis_valid"),
            "exhausted legacy ending recovers with matching supplied evidence and requested checks");
        Test.True(!j.SystemPrompt.Contains(DiplomacyPromptTexts.Get("PlayerWorldBookEvidence", ""))
            && j.SystemPrompt.Contains(DiplomacyPromptTexts.Get("EventEndingEvidence", "")), "writer/reviewer use the real CN/EN ending contract");
        var draft = JObject.Parse(j.PrivacyDraft); var audit = Approved(j);
        object[] args = { j, draft, audit, "", false };
        Test.True((bool)CitationCheck("ValidateNarrativeSafety", args), "event-record audit needs no unavailable world-book quotation");
        foreach (string flag in new[] { "event_record_basis_valid", "world_identity_valid", "player_agency_valid", "continuity_valid", "geography_valid", "publication_order_valid", "diplomatic_state_valid" })
        {
            var rejected = Approved(j); rejected["narrative_safety"]![flag] = false;
            object[] negative = { j, draft, rejected, "", false };
            Test.True(!(bool)CitationCheck("ValidateNarrativeSafety", negative) && (bool)negative[4], "ending retains substantive rejection for " + flag);
        }
        ((JObject)audit["narrative_safety"]!).Remove("event_record_basis_valid");
        Test.True(!(bool)CitationCheck("ValidateNarrativeSafety", args), "a legacy generic approval cannot bypass the new ending review");
        j.SuspendedReason = "boundary_audit_invalid:narrative_event_record_basis_missing:controlled";
        j.TechnicalRecoveryAttempts = 3; j.RetryAfterUtcTicks = 0; j.NextRecoveryProbeUtcTicks = 0;
        Reload(o); j = o.CurrentStorage.Jobs.Single(); Call(o, "TryResumeTechnicalRequest", j, false);
        Test.True(j.TechnicalRecoveryAttempts == 3 && j.SuspendedReason != "", "contract migration cannot refill new failures on reload");
        LoreCitationRepair();
        PlayerOpeningLifecycle();
    }
}
