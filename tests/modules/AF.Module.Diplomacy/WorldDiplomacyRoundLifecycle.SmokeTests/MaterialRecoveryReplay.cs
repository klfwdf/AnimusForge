using System.Reflection;
using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class MaterialRecoveryReplay
{
    private sealed class Host : AidRequestReplay.Host, IDiplomacyModelConfiguration, IDiplomacyMaterialWorld
    {
        internal string Configuration = "provider_1";
        internal readonly Dictionary<string, List<string>> Fiefs = new();
        public string ConfigurationFingerprint() => Configuration;
        public IReadOnlyList<string> OwnedSettlementIds(string id) => Fiefs.TryGetValue(id, out var f) ? f : Array.Empty<string>();
        public override IReadOnlyList<string> AllKingdomIds() => new[] { "a", "b", "c", "d", "p", "enemy" };
    }
    private static object? Invoke(WorldDiplomacyOrchestration o, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(o, args);
    private static (Host h, WorldDiplomacyOrchestration o) Fixture()
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o; return (h, o);
    }
    private static void Complete(WorldDiplomacyOrchestration o, WorldDiplomacyJob job, JObject json) =>
        o.TryCompleteDiplomacyPipeline(job, new LlmJobResult { Success = true, Content = json.ToString() });
    private static DiplomacySpeechReview Conditional(WorldDiplomacyOrchestration o, string id)
    {
        o.CaptureSpeechReview("ruler_a", "a", new("scene", id, "session", "Only when c owns the town", "I will propose trade if c owns that town"), "");
        var job = o.CurrentStorage.Jobs.Last(x => x.Kind == "oral_review"); o.PrepareSharedRequest(job);
        Test.True(job.UserPrompt.Contains("current_public_facts") && job.SystemPrompt.Contains(DiplomacyPromptTexts.Get("SpeechReview_Conditions", "missing")), "actual review sends live facts and the selected package condition contract");
        Complete(o, job, JObject.Parse("{\"unresolved\":true,\"condition_watch\":[\"c\"],\"commitments\":[]}"));
        o.PrepareSharedRequest(job); Complete(o, job, JObject.Parse("{\"approved\":true}"));
        return o.CurrentStorage.SpeechReviews.Last();
    }
    internal static void Run()
    {
        var (h, o) = Fixture(); var held = Conditional(o, "conditional");
        Test.True(held.Status == "conditional" && held.WatchedKingdoms.SequenceEqual(new[] { "c" }) && o.CurrentStorage.DialogueArrangements.Count == 0, "audited unfulfilled prerequisite is saved without any action");
        h.CurrentDayValue += 5; Invoke(o, "MaintainSpeechReviews");
        Test.True(o.CurrentStorage.Jobs.Count == 0, "time does not resume a condition");
        h.Fiefs["d"] = new() { "unrelated" }; h.CurrentDayValue++; Invoke(o, "MaintainSpeechReviews");
        Test.True(held.Status == "conditional", "unrelated territory changes do not resume a watched promise");
        var saved = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
        o.ReplaceStorage(saved); held = saved.SpeechReviews.Single(); Invoke(o, "MaintainSpeechReviews");
        Test.True(held.Status == "conditional" && saved.Jobs.Count == 0, "save/reload preserves the observed baseline");
        h.Fiefs["c"] = new() { "promised_town" }; h.CurrentDayValue++; Invoke(o, "MaintainSpeechReviews");
        Test.True(held.Status == "pending" && saved.Jobs.Single().Kind == "oral_review" && saved.DialogueArrangements.Count == 0, "material territory change resumes review, never manufactures assent or executes");
        var review = saved.Jobs.Single(); o.PrepareSharedRequest(review);
        Test.True(review.UserPrompt.Contains("promised_town") && review.UserPrompt.Contains(held.NpcText), "resumed review retains the actual source and new concrete evidence");
        Complete(o, review, JObject.Parse("{\"unresolved\":true,\"condition_watch\":[\"c\"],\"commitments\":[]}"));
        o.PrepareSharedRequest(review); h.Fiefs["c"].Clear(); Complete(o, review, JObject.Parse("{\"approved\":true}"));
        Test.True(held.Attempts == 1 && held.Reason == "condition_evidence_changed_during_review" && saved.DialogueArrangements.Count == 0, "late audit cannot use superseded world evidence");

        var (sh, so) = Fixture();
        so.CaptureSpeechReview("ruler_a", "a", new("courier", "outage", "session", "Hello", "We should discuss this"), "");
        var failure = so.CurrentStorage.Jobs.Single(); var service = so.CurrentStorage.SpeechReviews.Single();
        for (int i = 0; i < 3; i++)
        {
            so.AdmitCompletion(failure, new LlmJobResult { Success = false, IsServiceFailure = true });
            Invoke(so, "FailSpeechReview", failure, "offline", true);
        }
        sh.CurrentDayValue += 8; Invoke(so, "MaintainSpeechReviews");
        Test.True(service.Status == "technical_hold" && so.CurrentStorage.Jobs.Count == 0, "exhausted service failure does not poll forever");
        so.AdmitCompletion(new WorldDiplomacyJob { Kind = "compress" }, new LlmJobResult { Success = true });
        sh.CurrentDayValue++; Invoke(so, "MaintainSpeechReviews");
        Test.True(service.Status == "pending" && service.Attempts == 0 && so.CurrentStorage.Jobs.Count == 1, "an actual admitted service recovery permits one new retry window");
        failure = so.CurrentStorage.Jobs.Single();
        for (int i = 0; i < 3; i++) Invoke(so, "FailSpeechReview", failure, "speech_audit_rejected", false);
        so.AdmitCompletion(new WorldDiplomacyJob { Kind = "compress" }, new LlmJobResult { Success = false, IsServiceFailure = true });
        so.AdmitCompletion(new WorldDiplomacyJob { Kind = "compress" }, new LlmJobResult { Success = true });
        sh.CurrentDayValue++; Invoke(so, "MaintainSpeechReviews");
        Test.True(service.Status == "technical_hold", "provider success cannot turn semantic rejection into a technical retry");
        sh.Configuration = "provider_2"; sh.CurrentDayValue++; Invoke(so, "MaintainSpeechReviews");
        Test.True(service.Status == "pending" && service.Attempts == 0, "an actual model configuration change permits bounded re-extraction");

        var (ah, ao) = Fixture(); ah.War("a", "enemy");
        var first = ao.CaptureAidRequest("a", "b", "enemy", "war_1", "native_request");
        ao.RecordNativeAidRefusal("a", "b", "enemy");
        ah.CurrentDayValue += 10; ah.Fiefs["d"] = new() { "remote_town" };
        Test.True(ReferenceEquals(first, ao.CaptureAidRequest("a", "b", "enemy", "war_1", "rewritten_request")), "time, wording and unrelated fief do not bypass refusal");
        ah.Fiefs["a"] = new() { "border_town" };
        var next = ao.CaptureAidRequest("a", "b", "enemy", "war_1", "native_request");
        Test.True(next.Id != first.Id && next.PreviousRequestId == first.Id && next.Revision == 1 && first.Status == "rejected" && next.Status == "requested", "same-war material change creates a linked new request while preserving the original refusal");
        Test.True(ReferenceEquals(next, ao.CaptureAidRequest("a", "b", "enemy", "war_1", "native_request")), "repeated changed-world callback deduplicates against the latest version");
        saved = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(ao.CurrentStorage))!; ao.ReplaceStorage(saved);
        Test.True(ao.CaptureAidRequest("a", "b", "enemy", "war_1", "native_request").Id == next.Id, "latest request version survives actual owner reload");
        ao.RecordNativeAidRefusal("a", "b", "enemy"); ah.War("b", "c");
        var third = ao.CaptureAidRequest("a", "b", "enemy", "war_1", "native_request");
        Test.True(third.Revision == 2 && third.Status == "requested" && ah.WarExecutions == 0, "new war involving the called realm allows discussion, not automatic entry into the requested war");
        third.Status = "fulfilled"; ah.Fiefs["enemy"] = new() { "new_town" };
        Test.True(ao.CaptureAidRequest("a", "b", "enemy", "war_1", "native_request").Id == third.Id, "material change cannot reopen a fulfilled participation obligation");
    }
}
