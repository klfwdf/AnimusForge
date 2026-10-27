using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class OralPrivacyAuthorityReplay
{
    private sealed class Host : ConcurrentOralMigrationReplay.Host
    {
        public override IWorldDiplomacyWarAdmissionPort WarAdmission(string first, string second) => new ActionSelectionReplay.Admission(true);
    }
    private static (ConcurrentOralMigrationReplay.Host h, WorldDiplomacyOrchestration o,
        WorldDiplomacyDialogueArrangement item, WorldDiplomacyJob job) Fixture(string channel = "native", bool war = false)
    {
        var h = new Host(); var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        h.PublishEnabled = h.PlayerAwareExecution = h.OralEffectPortsEnabled = true;
        o.SubmitOralDiplomaticCommitment("ruler_a", "a", war
            ? "action=DeclareWar;move=NewMatter;target=empire_s" : "action=Trade;move=NewMatter;target=p;days=14",
            new DialogueInteractionOrigin(channel, "privacy-loss", "session", "SECRET_PRIVATE_ROMANCE; agreed public terms", "I agree to the public terms"));
        return (h, o, o.CurrentStorage.DialogueArrangements.Single(), o.CurrentStorage.Jobs.Single());
    }
    private sealed class CompletionSource : IWorldDiplomacyCompletionSource
    {
        internal WorldDiplomacyOrchestration Owner;
        internal Queue<LlmJobResult> Results;
        public WorldDiplomacyStorage Storage => Owner.CurrentStorage;
        public long RuntimeGeneration => 42;
        public bool IsSaveRuntimeStale(long generation) => generation != 42;
        public int CurrentHour => 24;
        public int FailedServiceCooldownHours => 12;
        public bool TryDequeue(out LlmJobResult result) => Results.TryDequeue(out result);
        public void ReleaseLease(string id, long generation) { }
        public void LogUsage(WorldDiplomacyJob job, LlmJobResult result) { }
        public void RemoveJob(string id) => Owner.RemoveJob(id);
        public void Log(string text) { }
    }
    private static void Complete(WorldDiplomacyOrchestration o, WorldDiplomacyJob job, string reply, Action duringRequest = null)
    {
        Test.True(o.RequestLeases.TryClaim(job, 42, 1000, 1000, true, out var request), "oral pipeline owns actual request lease");
        var gate = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var worker = Task.Run(async () => await gate.Task);
        duringRequest?.Invoke(); gate.SetResult(reply);
        var result = new LlmJobResult { JobId = request.JobId, RuntimeGeneration = request.RuntimeGeneration,
            RequestAttempt = request.Attempt, OwnershipEpoch = request.OwnershipEpoch, SchedulingOwner = request.SchedulingOwner,
            PlayerAuthorityRulerId = request.PlayerAuthorityRulerId, RoundConversationRevision = request.ConversationRevision,
            Success = true, Content = worker.GetAwaiter().GetResult() };
        var source = new CompletionSource { Owner = o, Results = new(new[] { result }) };
        WorldDiplomacyCompletionApplication.Run(ref source, o);
    }
    private static void Isolate(WorldDiplomacyOrchestration o, WorldDiplomacyJob job)
    {
        o.PrepareSharedRequest(job);
        Test.True(job.Kind == "privacy_material" && job.UserPrompt.Contains("SECRET_PRIVATE_ROMANCE"), "private source belongs only to isolation");
        // Exactly the real failure: specialist discards IDs, action fields and terms.
        Complete(o, job, "{\"safe\":true,\"public_context\":\"The ruler wishes to issue a public declaration.\",\"attitude_summary\":\"firm\"}");
        o.PrepareSharedRequest(job);
        string sent = string.Join("\n", WorldDiplomacyLlmMessageApplication.BuildLlmMessageArray(job, _ => "")
            .OfType<JObject>().Select(x => (string)x["content"]));
        Test.True(job.Kind == "generate" && !sent.Contains("SECRET_PRIVATE_ROMANCE") && !sent.Contains("PRIVATE_MEMORY"), "actual generation messages exclude protected source");
        Test.True(sent.Contains("target_kingdom_id") && sent.Contains("duration_days")
            && sent.Contains(DiplomacyPromptTexts.Get("OralGeneration_PublicExecution", "MISSING_RESOURCE")),
            "actual request restores fixed IDs, terms and localized prose-only contract after isolation");
        Test.True(!sent.Contains(DiplomacyPromptTexts.Get("PlayerNegotiationContract", "MISSING_RESOURCE")),
            "oral drafting never reopens negotiation after the rulers already agreed");
    }
    private static string Prose(WorldDiplomacyDialogueArrangement item, bool war = false)
    {
        var draft = JObject.Parse(OralDeclarationReplay.Draft(item, war ? "declare_war" : "accept_trade", war
            ? "The ruler of a formally declares war on the Southern Empire. Our armies shall prepare for the conflict. This declaration takes effect upon publication."
            : "The rulers of a and p have agreed to trade for 14 days. We formally announce fulfillment of this agreement upon publication. Our officials shall act according to these terms."));
        draft["actions"] = new JArray();
        return draft.ToString();
    }
    internal static void Run()
    {
        var threatCase = Fixture();
        Isolate(threatCase.o, threatCase.job);
        var threat = new WorldDiplomacyThreat { Status = "open", IssuerKingdomId = "a", TargetKingdomId = "b", StageDocumentId = "threat-1" };
        threatCase.o.CurrentStorage.DiplomaticThreats.Add(threat);
        Test.True(WorldDiplomacyRoundLifecycleRules.HasStaleThreatPresentation(threatCase.job, threatCase.o.CurrentStorage.DiplomaticThreats), "oral threat drift detected");
        Test.True(threatCase.o.RefreshDiplomaticThreatPresentationAndPrompt(threatCase.job)
            && !WorldDiplomacyRoundLifecycleRules.HasStaleThreatPresentation(threatCase.job, threatCase.o.CurrentStorage.DiplomaticThreats), "oral refresh captures threat snapshot");
        threatCase.o.PrepareSharedRequest(threatCase.job);
        Complete(threatCase.o, threatCase.job, Prose(threatCase.item), () => threat.StageDocumentId = "threat-2");
        Test.True(threatCase.job.Kind == "generate" && threatCase.o.CurrentStorage.Documents.Count == 0
            && !WorldDiplomacyRoundLifecycleRules.HasStaleThreatPresentation(threatCase.job, threatCase.o.CurrentStorage.DiplomaticThreats), "changed threat discards draft and refreshes snapshot");
        threatCase.o.PrepareSharedRequest(threatCase.job);
        Complete(threatCase.o, threatCase.job, Prose(threatCase.item));
        Test.True(threatCase.job.Kind == "privacy_audit", "stable threat allows draft to reach privacy audit");
        threatCase.o.PrepareSharedRequest(threatCase.job);
        Complete(threatCase.o, threatCase.job, "{\"safe\":true,\"agreement_consistent\":true}");
        Test.True(threatCase.o.ResolveDocument(threatCase.item.DocumentId)?.IsReadyForPublication == true
            && threatCase.h.TradeExecutions == 1, "refreshed oral declaration publishes exactly once");

        foreach (string channel in new[] { "native", "scene", "courier" })
        foreach (bool war in new[] { false, true })
        {
            var f = Fixture(channel, war); Isolate(f.o, f.job);
            Test.True(f.job.UserPrompt.Contains(war ? "empire_s" : "accept_trade"), "canonical target/action survives real isolation: " + channel);
            Complete(f.o, f.job, Prose(f.item, war));
            Test.True(f.job.Kind == "privacy_audit" && f.h.Effects == 0 && f.o.CurrentStorage.Documents.Count == 0, "writing cannot publish before audit");
            f.o.PrepareSharedRequest(f.job);
            Test.True(f.job.UserPrompt.Contains("target_kingdom_id") && f.job.SystemPrompt.Contains("agreement_consistent"), "audit receives bound action and must verify prose consistency");
            Complete(f.o, f.job, "{\"safe\":true,\"agreement_consistent\":true}");
            var document = f.o.ResolveDocument(f.item.DocumentId);
            Test.True(f.h.Effects + f.h.TradeExecutions == 1 && document?.IsReadyForPublication == true && document.TargetKingdomId == f.item.TargetKingdomId,
                "program-bound action publishes and executes exactly once: " + channel + ":" + war + ":" + f.item.Reason);
            Test.True(war || f.h.TradeDuration == 14, "agreed duration executed, no further player declaration");
            f.o.MaintainDialogueAndMemory();
            Test.True(f.h.Effects + f.h.TradeExecutions == 1 && f.o.CurrentStorage.Documents.Count == 1, "daily recovery never repeats completed action");
        }
        foreach (string audit in new[] { "{\"safe\":true,\"agreement_consistent\":false}", "{\"safe\":true}" })
        {
            var f = Fixture(); Isolate(f.o, f.job); Complete(f.o, f.job, Prose(f.item));
            Complete(f.o, f.job, audit);
            Test.True(f.h.Effects == 0 && f.o.CurrentStorage.Documents.Count == 0 && f.item.Status == "deferred", "changed/omitted prose terms or missing consistency verdict never execute");
        }
        foreach (string changed in new[] { "terms", "cancel", "ruler", "version", "round" })
        {
            var f = Fixture(); Isolate(f.o, f.job); Complete(f.o, f.job, Prose(f.item));
            Complete(f.o, f.job, "{\"safe\":true,\"agreement_consistent\":true}", () => {
                if (changed == "terms") f.item.Terms.DurationDays = 99;
                if (changed == "cancel") f.o.CancelUnpublishedCommitmentWork(f.item.ArrangementId, f.item.Version, "cancelled");
                if (changed == "ruler") f.h.Dead = true;
                if (changed == "version") f.item.Version++;
                if (changed == "round") f.o.ResolveRound(f.item.RoundId).State = "closed";
            });
            Test.True(f.h.Effects == 0 && f.o.CurrentStorage.Documents.Count == 0, "late audit cannot authorize changed agreement: " + changed);
        }
        var leak = Fixture(); Isolate(leak.o, leak.job); Complete(leak.o, leak.job, Prose(leak.item));
        Complete(leak.o, leak.job, "{\"safe\":false,\"agreement_consistent\":true}");
        Test.True(leak.h.Effects == 0 && leak.o.CurrentStorage.Documents.Count == 0, "confidentiality rejection still prevents publication");
        var wrong = Fixture(); Isolate(wrong.o, wrong.job);
        var changedAction = JObject.Parse(OralDeclarationReplay.Draft(wrong.item, "accept_trade"));
        changedAction["actions"][0]["target_kingdom_id"] = "invented_realm";
        Complete(wrong.o, wrong.job, changedAction.ToString());
        Complete(wrong.o, wrong.job, "{\"safe\":true,\"agreement_consistent\":true}");
        Test.True(wrong.h.Effects == 0 && wrong.item.Status == "deferred", "explicit conflicting legacy action is rejected despite audit approval");
        RecoverOldHolds();
    }
    private static void RecoverOldHolds()
    {
        foreach (string state in new[] { "eligible", "new_contract", "unknown_reason", "explicit", "published", "uncertain", "cancelled", "document", "ruler", "closed" })
        {
            var f = Fixture(); f.item.Status = "deferred"; f.item.GenerationFailureAttempts = 3;
            f.item.GenerationRetryAfterDay = int.MaxValue;
            f.item.Reason = f.job.SuspendedReason = "oral_declaration_changes_agreed_action_or_terms";
            f.job.OralDraftingContractVersion = state == "new_contract" ? 1 : 0;
            if (state == "unknown_reason") f.item.Reason = f.job.SuspendedReason = "execution_uncertain";
            if (state == "explicit") f.item.ExplicitlyDeferred = true;
            if (state is "published" or "uncertain" or "cancelled") f.item.Status = state == "uncertain" ? "execution_uncertain" : state;
            if (state == "document") f.item.DocumentId = "potentially_executed_artifact";
            if (state == "ruler") f.h.Dead = true;
            if (state == "closed") f.o.ResolveRound(f.item.RoundId).State = "closed";
            var saved = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(f.o.CurrentStorage));
            f.o.ReplaceStorage(saved); var job = saved.Jobs.Single(); var item = saved.DialogueArrangements.Single();
            f.o.TryRebuildPendingJob(job); // Canonical/load rebuild must not erase old-contract evidence.
            f.o.MaintainDialogueAndMemory();
            bool recovered = job.SuspendedReason == "" && job.OralDraftingContractVersion == 1;
            Test.True(recovered == (state == "eligible") && f.h.Effects == 0, "safe one-time saved-hold recovery boundary: " + state);
            if (state != "eligible") continue;
            Test.True(item.GenerationFailureAttempts == 0 && item.Status == "accepted" && saved.Jobs.Count == 1, "recovery keeps original agreement and bounded queue");
            item.Status = "deferred"; item.GenerationFailureAttempts = 3; item.GenerationRetryAfterDay = int.MaxValue;
            item.Reason = job.SuspendedReason = "oral_declaration_changes_agreed_action_or_terms";
            f.h.CurrentDayValue++; f.o.MaintainDialogueAndMemory();
            Test.True(job.SuspendedReason != "" && item.GenerationFailureAttempts == 3, "new-contract failure cannot become an unlimited daily reset");
        }
        Test.True(JsonConvert.DeserializeObject<WorldDiplomacyJob>("{}").OralDraftingContractVersion == 0, "legacy job defaults to old drafting contract without changing save identity");
    }
}
