using System.Reflection;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialSupplyReplay
{
    private sealed class ObservationHost : EventStructureReplay.Host
    {
        internal readonly List<string> ObservationLogs = new();
        internal int OutputLimit = 12000;
        internal bool FailLog;
        public override int AnalysisMaxTokens() => OutputLimit;
        public override int EstimateTokens(string value) => (value?.Length ?? 0) / 2 + 1;
        public override void Log(string message)
        {
            if (!message.StartsWith("diplomacy.structure_observation ")) return;
            if (FailLog) throw new IOException("controlled optional sink failure");
            ObservationLogs.Add(message);
        }
    }

    // Paraphrased minimum data from the user's 2026-10-27 14:57 session:
    // A aea72e10f3ca48ae9c52a35d3635b10f (15:00), B 61e5cb83ee964bf3ac0afad2a7d697ed (15:09).
    // These are HUMAN-labelled examples and controlled replies, not model evaluations.
    private static JObject StructureFixture(string variant) => new()
    {
        ["rows"] = new JArray(new JObject
        {
            ["id"] = "domestic",
            ["theme"] = variant == "distinct" ? "Kinship changes the choice: expose a brother's betrayal or surrender one's own claim."
                : "Surface history label: removing the ancestor leaves the tribute protest and choices unchanged.",
            ["prior"] = variant == "distinct"
                ? "A: unlike a levy protest for autonomy, the claimant must choose truth versus protecting a sibling, losing inheritance or trust."
                : "A/B: changed levy triggers local nobles' protest against central allocation, demands autonomy from the ruler, costs troops, invites foreign leverage; renamed treasury/tribute does not change structure.",
            ["batch"] = "Nord plunder hearing and Khuzait tribute protest share local protest against central distribution despite different countries.",
            ["drift"] = "A distinctive conditional oath became nobles protesting the ruler with foreign observers.",
            ["parties"] = "Khuzait: answer the tax-right demand. Northern Empire: answer the old-province claim. Battania: concern only, no answerable issue."
        })
    };

    private static (ObservationHost h, WorldDiplomacyOrchestration o, WorldDiplomacyJob j) ObservationStart(string variant = "repeat")
    {
        var h = new ObservationHost { PublishEnabled = true, PlayerAwareExecution = true, OralRange = (0,3000), CurrentDayValue = 40 };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        o.TryScheduleNormalRound();
        var j = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_concept");
        var khuzait = Candidate();
        khuzait["conflict"] = "Khuzait clans protest Mongchug's allocation of eastern provincial tribute and threaten fewer levies.";
        khuzait["motive"] = "Local autonomy against centralized tax rights; ancestors cited as justification.";
        if (variant == "distinct")
        {
            khuzait["conflict"] = "Khuzait clans face a ruler's hearing: the heir must expose a brother's betrayal or surrender his own inheritance claim.";
            khuzait["motive"] = "The brother saved the heir's life; truthful testimony costs kinship, silence costs the claim. A foreign court holds the contested oath and must decide whether to release it.";
            khuzait["theme"] = "kinship";
            khuzait["theme_reason"] = "Removing the brother's rescue removes the reason to sacrifice the claim; kinship changes the choice and cost.";
        }
        var nord = Candidate("nord"); nord["conflict"] = "Nord jarls demand a hearing on Harald's division of plunder and sailing rights.";
        nord["motive"] = "Local autonomy against centralized distribution; Sturgia may exploit the split.";
        o.CurrentStorage.EventAnnouncements.Add(new() { Id="prior_A", EventId="A", Stage="started", State="public", Day=39,
            Title="Battania treasury hearing", Body="Battania nobles oppose crown finances, levies and centralization, unite for a public hearing, seek autonomy and foreign support. Neighboring rulers watch the split." });
        Complete(o, j, new() { ["candidates"] = new JArray(khuzait, nord) });
        return (h, o, j);
    }

    private static LlmJobResult ObservedResult(JObject business, string mode)
    {
        if (business["assessments"] is JArray assessments)
        {
            var sibling = (JObject)assessments[0]!.DeepClone(); sibling["id"] = "nord";
            assessments.Add(sibling);
        }
        var data = StructureFixture(mode);
        if (business["approved"] != null) data["rows"]![0]!["id"] = "selected";
        else
        {
            var sibling = data["rows"]![0]!.DeepClone(); sibling["id"] = "nord";
            ((JArray)data["rows"]!).Add(sibling);
        }
        return new()
        {
            Success = !mode.StartsWith("truncated"), IsOutputTruncated = mode.StartsWith("truncated"), CompletionTokens = 1700,
            Content = business.ToString(Formatting.None) + (mode is "missing" or "truncated_before_marker" ? ""
                : mode == "truncated_marker" ? "\nAF_STRUCT" : "\n"
                + WorldDiplomacyOrchestration.StructureObservationMarker + "\n" + (mode switch
                {
                    "malformed" => "{ not valid JSON", "oversized" => new string('x', 20000),
                    "refused" => "I cannot provide this diagnostic", "truncated" => "{\"rows\":[",
                    "wrong_type" => "{\"rows\":[{\"theme\":false}]}",
                    _ => data.ToString(Formatting.None)
                }))
        };
    }

    internal static void StructureObservation()
    {
        foreach (string mode in new[] { "missing", "repeat", "distinct", "malformed", "oversized", "refused", "wrong_type", "truncated", "truncated_marker", "truncated_before_marker", "sink_failure" })
        {
            var (h, o, j) = ObservationStart(mode);
            o.PrepareSharedRequest(j);
            Test.True(j.SystemPrompt.Contains(WorldDiplomacyOrchestration.StructureObservationMarker), "actual comparison requests optional observation");
            Test.True(j.UserPrompt.Contains("Battania nobles") && j.UserPrompt.Contains("Khuzait clans") && j.UserPrompt.Contains("Nord jarls"), "real prior and same-batch counterexamples reach comparison");
            int reads = h.MaterialReads, max = j.MaxTokens, jobs = o.CurrentStorage.Jobs.Count;
            h.FailLog = mode == "sink_failure";
            Test.True(o.TryCompleteDiplomacyPipeline(j, ObservedResult(Compare(), mode)), "controlled comparison completes");
            Test.True(j.EventSelectionStage == "expand" && j.SuspendedReason == "" && j.MaterialRepairFeedback == ""
                && (string)JObject.Parse(j.EventSelectedCandidate)["id"]! == "domestic" && j.TechnicalRecoveryAttempts == 0 && h.MaterialReads == reads && j.MaxTokens == max
                && o.CurrentStorage.Jobs.Count == jobs && !j.EventComparison.Contains("rows"),
                "observation cannot change selection, retries, retrieval, request count, cap or persisted comparison: " + mode);
            var concept = new JObject {
                ["title"]="The proposed hearing", ["background"]="A domestic petition raises the other court's responsibility for an old oath.",
                ["initiator"]="b", ["material_keys"]=new JArray("lore:succession"),
                ["participants"]=new JArray(Position("b","a"),Position("a","b")),
                ["party_review"]=new JArray(j.CandidateKingdomIds.Select(id=>new JObject { ["kingdom"]=id,
                    ["included"]=id is "a" or "b", ["reason"]="Grounded standing" })),
                ["narrative"]=new JArray(new JObject { ["kind"]="allegation", ["subject"]="b", ["source"]="lore:succession", ["text"]="The old oath is disputed." }) };
            Complete(o, j, concept); o.PrepareSharedRequest(j);
            Test.True(j.Kind == "event_concept_audit" && j.SystemPrompt.Contains(WorldDiplomacyOrchestration.StructureObservationMarker), "actual final audit observes expansion");
            var approved = MaterialCountryThirdReplay.Approved(j);
            o.TryCompleteDiplomacyPipeline(j, ObservedResult(approved, mode));
            var round = o.ResolveRound(j.RoundId);
            Test.True(round.EventPhase == "opening" && !o.CurrentStorage.Jobs.Contains(j)
                && !round.EventSelectionEvidence.Contains("rows"), "all optional results reach normal adoption without diagnostic persistence: " + mode);
            var boundary = o.CurrentStorage.Jobs.Single(x => x.Kind == "event_boundary");
            Complete(o, boundary, new() { ["title"]="The hearing opens", ["body"]="The courts acknowledge a disputed undertaking.",
                ["effects"]=new JArray(new JObject { ["kind"]="ruler_relation", ["kingdom"]="b", ["other"]="a", ["delta"]=-1, ["days"]=0, ["reason"]="The dispute strains relations." }),
                ["remove_effects"]=new JArray(), ["confirmed_documents"]=new JArray(), ["narrative"]=new JArray() });
            o.PrepareSharedRequest(boundary); Complete(o, boundary, MaterialCountryThirdReplay.Approved(boundary));
            Test.True(o.CurrentStorage.EventAnnouncements.Any(x => x.EventId == round.RoundId && x.Stage == "started"), "normal publication remains reachable: " + mode + ";" + boundary.SuspendedReason + ";" + boundary.MaterialRepairFeedback);
            Test.True(!JsonConvert.SerializeObject(o.CurrentStorage).Contains("Surface history label"), "diagnostic prose cannot leak into saved/public state");
            if (mode is "repeat" or "distinct") Test.True(h.ObservationLogs.Count(x => x.Contains("status=observed")) == 2, "semantic examples are recorded at both seams, never enforced");
            if (mode == "oversized") Test.True(h.ObservationLogs.Any(x => x.Contains("status=oversized")) && h.ObservationLogs.All(x => x.Length < 4000), "unbounded model trailer never becomes unbounded diagnostic log");
        }

        var (host, owner, job) = ObservationStart();
        owner.PrepareSharedRequest(job);
        var append = typeof(WorldDiplomacyOrchestration).GetMethod("AppendStructureObservation", BindingFlags.NonPublic | BindingFlags.Instance)!;
        job.SystemPrompt = "business"; append.Invoke(owner, new object[] { job, 27990L });
        Test.True(job.SystemPrompt == "business" && job.SuspendedReason == "", "input headroom skips observation without trimming/retry");
        host.OutputLimit = 2048; append.Invoke(owner, new object[] { job, 100L });
        Test.True(job.SystemPrompt == "business", "small output cap reserves all output for business");
        host.OutputLimit = 12000;
        foreach (string stage in new[] { "player", "oral" })
        {
            job.EventSelectionStage = stage; job.Kind = "event_concept_audit";
            append.Invoke(owner, new object[] { job, 100L });
            Test.True(job.SystemPrompt == "business", "bound matter prompt unchanged: " + stage);
        }
        job.EventSelectionStage = "compare"; job.Kind = "event_concept";
        job.EventTriggerMaterial = "existing obligation"; append.Invoke(owner, new object[] { job, 100L });
        Test.True(job.SystemPrompt == "business", "existing required agenda is not ordinary novelty work");
        job.EventTriggerMaterial = "";
        owner.ResolveRound(job.RoundId).EventSourceType = "policy_material"; append.Invoke(owner, new object[] { job, 100L });
        Test.True(job.SystemPrompt == "business", "policy work excluded");
        owner.ResolveRound(job.RoundId).EventSourceType = "npc_material";
        job.EventExplorationTheme = ""; // Legacy unfinished candidate has no theme.
        owner.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(owner.CurrentStorage))!);
        job = owner.CurrentStorage.Jobs.Single(x => x.JobId == job.JobId);
        owner.PrepareSharedRequest(job); owner.TryCompleteDiplomacyPipeline(job, ObservedResult(Compare(), "missing"));
        Test.True(job.EventSelectionStage == "expand" && job.EventCandidatePool.Contains("domestic") && job.EventExplorationTheme == "", "legacy candidate survives reload without forced theme or reselection");

        var (_, deniedOwner, deniedJob) = ObservationStart(); deniedOwner.PrepareSharedRequest(deniedJob);
        var invalid = Compare(); invalid.Remove("assessments");
        deniedOwner.TryCompleteDiplomacyPipeline(deniedJob, ObservedResult(invalid, "truncated"));
        Test.True(deniedJob.SuspendedReason.Contains("event_comparison_invalid"), "complete but invalid business still fails original validation");
        var (_, partialOwner, partialJob) = ObservationStart(); partialOwner.PrepareSharedRequest(partialJob);
        partialOwner.TryCompleteDiplomacyPipeline(partialJob, new() { IsOutputTruncated=true, Error="business_truncated", Content="{\"assessments\":[\n" + WorldDiplomacyOrchestration.StructureObservationMarker + "\n{}" });
        Test.True(partialJob.EventSelectionStage == "compare" && partialJob.SuspendedReason != "", "partial business is never recovered as optional truncation");
        var (_, failedOwner, failedJob) = ObservationStart(); failedOwner.PrepareSharedRequest(failedJob);
        var serviceFailure = ObservedResult(Compare(), "truncated"); serviceFailure.IsServiceFailure = true; serviceFailure.Error = "service_failure";
        failedOwner.TryCompleteDiplomacyPipeline(failedJob, serviceFailure);
        Test.True(failedJob.EventSelectionStage == "compare" && failedJob.SuspendedReason != "", "optional trailer cannot override a service failure");
        Console.WriteLine("PASS structure observation: controlled A/B and distinct-choice fixtures, both completion seams, non-blocking trailer, budgets, legacy and excluded lanes; real model semantics NOT_RUN");
    }
}
