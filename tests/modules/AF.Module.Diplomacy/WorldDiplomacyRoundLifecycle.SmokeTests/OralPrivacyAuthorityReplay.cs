using AnimusForge;
using AnimusForge.DiplomacyDialogue;
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
        foreach (string channel in new[] { "native", "scene", "courier" })
        foreach (bool war in new[] { false, true })
        {
            var f = Fixture(channel, war); Isolate(f.o, f.job);
            Test.True(f.job.UserPrompt.Contains(war ? "empire_s" : "accept_trade"), "canonical target/action survives real isolation: " + channel);
            string prose = Prose(f.item, war);
            Complete(f.o, f.job, prose);
            Test.True(f.job.Kind == "privacy_audit" && f.job.PrivacyRewrites == 0 && f.item.GenerationFailureAttempts == 0
                && f.h.Effects == 0 && f.o.CurrentStorage.Documents.Count == 0,
                "prose-only oral draft reaches audit without dependency rejection or fallback");
            f.o.PrepareSharedRequest(f.job);
            Test.True(f.job.UserPrompt.Contains("target_kingdom_id") && f.job.SystemPrompt.Contains("agreement_consistent"), "audit receives bound action and must verify prose consistency");
            Complete(f.o, f.job, "{\"safe\":true,\"agreement_consistent\":true}");
            var document = f.o.ResolveDocument(f.item.DocumentId);
            Test.True(f.h.Effects + f.h.TradeExecutions == 1 && document?.IsReadyForPublication == true && document.TargetKingdomId == f.item.TargetKingdomId,
                "program-bound action publishes and executes exactly once: " + channel + ":" + war + ":" + f.item.Reason);
            Test.True(war || f.h.TradeDuration == 14, "agreed duration executed, no further player declaration");
            Test.True(document.Body == (string)JObject.Parse(prose)["body"] && f.item.GenerationFailureAttempts == 0,
                "publication preserves AI prose instead of succeeding through the fixed fallback template");
            f.o.MaintainDialogueAndMemory();
            Test.True(f.h.Effects + f.h.TradeExecutions == 1 && f.o.CurrentStorage.Documents.Count == 1, "daily recovery never repeats completed action");
        }
        foreach (string audit in new[] { "{\"safe\":true,\"agreement_consistent\":false}", "{\"safe\":true}" })
        {
            var f = Fixture(); Isolate(f.o, f.job); Complete(f.o, f.job, Prose(f.item));
            Complete(f.o, f.job, audit);
            Test.True(f.h.Effects + f.h.TradeExecutions == 0 && f.o.CurrentStorage.Documents.Count == 0,
                "changed prose terms or missing consistency verdict never execute");
            Test.True(audit.Contains("false") ? f.item.Status == "deferred"
                : f.job.Kind == "privacy_audit" && f.job.PrivacyAuditRetries == 1
                    && f.job.PrivacyRepairFeedback.Contains("agreement_consistent_boolean_missing"),
                "negative semantic verdict defers; missing verdict uses existing bounded same-draft audit repair");
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
            Test.True(changed == "cancel" ? f.item.Status == "executed" && f.h.TradeExecutions == 1 : f.h.Effects == 0 && f.o.CurrentStorage.Documents.Count == 0, "locked acceptance survives cancellation; changed authority, terms and identity cannot execute: " + changed);
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
        AdmissionBoundaries();
        RecoverOldHolds();
    }
    private static void AdmissionBoundaries()
    {
        foreach (string kind in new[] { "ordinary_npc", "ordinary_player", "oral_bad_dependency" })
        {
            var f = Fixture(); Isolate(f.o, f.job);
            var draft = JObject.Parse(Prose(f.item));
            if (kind == "oral_bad_dependency")
            {
                draft = JObject.Parse(OralDeclarationReplay.Draft(f.item, "accept_trade"));
                draft["actions"][0]["dependency_action_ids"] = new JArray("action_99");
            }
            else
            {
                f.job.DialogueArrangementId = "";
                if (kind == "ordinary_player") f.job.AuthorKingdomId = "p";
            }
            f.o.CommitGeneratedDocument(f.job, draft.ToString());
            Test.True(f.job.Kind == "generate" && f.job.PrivacyRewrites == 1
                && f.job.PrivacyRepairFeedback.Contains(kind == "oral_bad_dependency"
                    ? "dependency_local_action_missing" : "dependency_actions_shape_invalid")
                && f.h.Effects + f.h.TradeExecutions == 0 && f.o.CurrentStorage.Documents.Count == 0,
                "oral empty-array exception does not bypass other admission checks: " + kind);
        }
        // Exact public writer response from Token_Stats.txt:591275 (2026-10-28).
        // Semantic review is a modeled leaf; this replays serialization/admission/execution,
        // not the live model's judgement or a user's save.
        const string actual = """
            {"title":"致南帝国：宣战之宣告","body":"赞美伟大的真主。南帝国的统治者们，我苏丹温吉德在此宣告：鉴于已同西帝国皇帝加里俄斯达成盟约与商定交易，阿塞莱苏丹国与南帝国之间的和平业已终结。自此协议达成之日起，弯刀与战马将为阿塞莱的荣耀与利益而鸣。此乃既定之实，吾等正式对南帝国宣战。","actions":[],"mentioned_kingdom_ids":["empire_s"],"tone":"firm","requires_response":false,"round_participation":"continue","round_status":"continue","response_requests":[],"answered_player_document_ids":[]}
            """;
        var live = Fixture(war: true); Isolate(live.o, live.job);
        Complete(live.o, live.job, actual);
        Test.True(live.job.Kind == "privacy_audit" && live.job.PrivacyRewrites == 0,
            "actual empty-action declaration reaches audit unchanged");
        Complete(live.o, live.job, "{\"safe\":true,\"agreement_consistent\":true}");
        var document = live.o.ResolveDocument(live.item.DocumentId);
        Test.True(document?.IsReadyForPublication == true && document.Body == (string)JObject.Parse(actual)["body"]
            && document.TargetKingdomId == "empire_s" && live.h.Effects == 1,
            "actual public draft retains prose and executes program-bound war against Southern Empire");
        live.o.CommitGeneratedDocument(live.job, actual);
        live.o.MaintainDialogueAndMemory();
        Test.True(live.h.Effects == 1 && live.o.CurrentStorage.Documents.Count == 1,
            "late duplicate completion and recovery do not repeat the oral action");
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
