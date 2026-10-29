using System.Reflection;
using AnimusForge;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Actual production admission and orchestration with deterministic model responses.
internal static class LiveBlockerReplay
{
    private sealed class Host : ConcurrentOralMigrationReplay.Host
    {
        internal bool PlayerRuler;
        public override string PlayerKingdomId() => PlayerRuler ? "p" : "";
    }
    private static (Host h, WorldDiplomacyOrchestration o) Fixture()
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o;
        return (h, o);
    }
    private static object? Call(WorldDiplomacyOrchestration o, string method, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(o, args);
    private static JObject Extraction(bool agreed = true, string npc = "I will propose an alliance to b.", string player = "Please propose an alliance to b; I cannot accept for b.") => new()
    {
        ["unresolved"] = false, ["commitments"] = new JArray(new JObject {
            ["payload"] = "action=Alliance;move=NewMatter;target=b", ["npc_quote"] = npc,
            ["player_agreed"] = agreed, ["player_quote"] = agreed ? player : "" })
    };
    private static WorldDiplomacyJob Capture(WorldDiplomacyOrchestration o, string channel = "native")
    {
        o.CaptureSpeechReview("ruler_a", "a", new(channel, "source", "session",
            "Please propose an alliance to b; I cannot accept for b.", "I will propose an alliance to b."), "");
        return o.CurrentStorage.Jobs.Single();
    }
    private static void Complete(WorldDiplomacyOrchestration o, WorldDiplomacyJob job, JObject response) =>
        o.TryCompleteDiplomacyPipeline(job, new LlmJobResult { Success = true, Content = response.ToString(Formatting.None) });

    internal static void Run()
    {
        foreach (string channel in new[] { "native", "scene", "courier" })
        foreach (string status in new[] { "accepted", "published", "executed" })
        {
            var (h, o) = Fixture();
            o.SubmitOralDiplomaticCommitment("ruler_a", "a", "action=Trade;move=NewMatter;target=b;days=30",
                new(channel, "original", "session", "Propose thirty days", "I will propose thirty days", false));
            var prior = o.CurrentStorage.DialogueArrangements.Single(); prior.Status = status; prior.DocumentId = "original_document";
            string original = JsonConvert.SerializeObject(prior); int jobs = o.CurrentStorage.Jobs.Count;
            string npc = "My original commitment stands: the same thirty-day proposal, no second letter.";
            o.CaptureSpeechReview("ruler_a", "a", new(channel, "confirmation", "session", "Confirm the same proposal only", npc), "");
            var job = o.CurrentStorage.Jobs.Single(x => x.Kind == "oral_review"); o.PrepareSharedRequest(job);
            var known = JObject.Parse(job.UserPrompt)["registered_commitments"]![0]!;
            Test.True((int)known["Terms"]!["DurationDays"]! == 30 && (string)known["DocumentId"]! == prior.DocumentId,
                "review receives actual full registered terms including executed commitments: " + status);
            var invalid = Extraction(false, npc);
            invalid["commitments"]![0]!["payload"] = "action=Trade;move=NewMatter;target=b;days=30;source_document=original_document;event=" + prior.RoundId;
            Test.True(!DialogueTagPayload.TryParse((string)invalid["commitments"]![0]!["payload"]!, out _),
                "live incident NewMatter plus source document reproduces parser rejection");
            Complete(o, job, invalid);
            Test.True(job.Kind == "oral_review" && job.PrivacyRewrites == 1 && o.CurrentStorage.SpeechReviews.Single().Attempts == 0,
                "invalid repeated confirmation receives one immediate corrective extraction");
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            job = o.CurrentStorage.Jobs.Single(x => x.Kind == "oral_review"); o.PrepareSharedRequest(job);
            var request = JObject.Parse(job.UserPrompt);
            Test.True((string)request["validation_error"]! == "speech_payload_invalid"
                && (string)request["rejected_extraction"]! == invalid.ToString(Formatting.None)
                && job.SystemPrompt.Contains(DiplomacyPromptTexts.Get("SpeechReview_RepairPayload", "MISSING")),
                "correction reason and rejected payload survive reload");
            Complete(o, job, JObject.Parse("{\"unresolved\":false,\"commitments\":[]}"));
            Test.True(job.Kind == "oral_review_audit" && o.CurrentStorage.SpeechReviews.Count == 1, "no-op reaffirmation still requires audit");
            Complete(o, job, JObject.Parse("{\"approved\":true}"));
            Test.True(o.CurrentStorage.SpeechReviews.Count == 0 && o.CurrentStorage.ReviewedSpeechSources.Count == 1
                && o.CurrentStorage.DialogueArrangements.Count == 1 && JsonConvert.SerializeObject(o.CurrentStorage.DialogueArrangements.Single()) == original
                && o.CurrentStorage.Jobs.Count == jobs && h.TradeExecutions == 0,
                "audited reaffirmation completes without changing terms, duplicating letters or executing: " + channel + "/" + status);
        }
        foreach (string correction in new[] { "invalid_again", "audit_rejected", "unresolved", "new_terms" })
        {
            var (_, o) = Fixture(); var job = Capture(o);
            var invalid = Extraction(false); invalid["commitments"]![0]!["payload"] += ";source_document=old";
            Complete(o, job, invalid);
            if (correction == "invalid_again")
            {
                Complete(o, job, invalid);
                Test.True(!o.CurrentStorage.Jobs.Contains(job) && o.CurrentStorage.SpeechReviews.Single().Attempts == 1,
                    "second invalid payload consumes finite normal failure budget");
            }
            else
            {
                Complete(o, job, correction == "new_terms" ? Extraction(false)
                    : JObject.Parse("{\"unresolved\":" + (correction == "unresolved" ? "true" : "false") + ",\"commitments\":[]}"));
                Complete(o, job, JObject.Parse("{\"approved\":" + (correction == "audit_rejected" ? "false" : "true") + "}"));
                Test.True(correction == "new_terms" ? o.CurrentStorage.DialogueArrangements.Count == 1
                    : o.CurrentStorage.DialogueArrangements.Count == 0 && o.CurrentStorage.SpeechReviews.Count == 1,
                    "repair preserves real new promises, unresolved conditions and rejected audit: " + correction);
            }
        }
        foreach (string text in new[] { "通商期限为三十日。", "期限为30日", "为期三十天", "Trade for thirty days.",
            "Trade for 30 days", "A thirty-day trade proposal.", "不涉及贡金，通商期限三十日。", "本条约有效三十日。", "合约为期三十日。" })
            Test.True(WorldDiplomacyDurationDisclosure.ContainsDays(text, 30), "exact duration accepted: " + text);
        foreach (string text in new[] { "期限十三日", "期限三十一日", "期限一百三十日", "期限130日", "期限30.5日", "期限1.30日",
            "支付30第纳尔，期限十四日", "支付三十第纳尔，期限十四日", "期限并非三十日", "不接受三十日通商", "期限不足三十日", "期限非三十日", "期限不为三十日", "三十日不适用",
            "期限约三十日", "期限三十日以上", "三十日后答复", "三十日内回信", "期限30至60日", "期限为期30", "三十日的期限不生效", "第三十日", "十月三十日", "第30日",
            "Trade for thirteen days", "Trade for one hundred thirty days", "Not thirty days", "We do not propose thirty days of trade",
            "Trade for at least thirty days", "Reply within thirty days", "Trade for 30-60 days", "Pay 30 denars for fourteen days", "Trade for 1,030 days" })
            Test.True(!WorldDiplomacyDurationDisclosure.ContainsDays(text, 30), "wrong/negated/non-duration rejected: " + text);
        foreach (var pair in new[] { (1, "一日"), (10, "十日"), (11, "十一日"), (101, "一百零一日"), (110, "一百一十日"),
            (252, "二百五十二日"), (252, "two hundred and fifty-two days") })
            Test.True(WorldDiplomacyDurationDisclosure.ContainsDays(pair.Item2, pair.Item1), "cardinal duration: " + pair.Item2);
        var terms = JObject.Parse("{\"peace_terms\":{\"duration_days\":30}}");
        Test.True(!WorldDiplomacyGenerationValidationRules.TryGetPublicPeaceTermsDisclosureViolation("propose_peace", "为期三十日", terms,
            "a", "b", _ => (null, null), _ => null, out _), "shared disclosure consumer recognizes written Chinese");
        Test.True(WorldDiplomacyGenerationValidationRules.TryGetPublicPeaceTermsDisclosureViolation("propose_peace", "支付30第纳尔", terms,
            "a", "b", _ => (null, null), _ => null, out string reason) && reason == "peace_terms_not_visible:duration", "unrelated number cannot disclose duration");

        foreach (string body in new[] { "我方向对方提出为期三十日的通商方案，须待对方接受。", "We propose trade for thirty days, subject to the other ruler's acceptance." })
        {
            var (h, o) = Fixture();
            o.SubmitOralDiplomaticCommitment("ruler_a", "a", "action=Trade;move=NewMatter;target=b;days=30",
                new DialogueInteractionOrigin("native", "trade", "session", "Propose trade", "I will propose trade", false));
            var arrangement = o.CurrentStorage.DialogueArrangements.Single(); var job = o.CurrentStorage.Jobs.Single();
            job.PrivacyApproved = true; // Models the already-passed independent writing audit from the incident.
            o.CommitGeneratedDocument(job, new JObject { ["title"] = "Trade proposal", ["body"] = body, ["actions"] = new JArray() }.ToString());
            Test.True(arrangement.Status == "published" && o.ResolveDocument(arrangement.DocumentId)?.Body == body
                && arrangement.Terms.DurationDays == 30 && h.TradeExecutions == 0,
                "original prose publishes exact proposal without fabricating third-party acceptance: " + arrangement.Reason);
        }

        foreach (string channel in new[] { "native", "scene", "courier" })
        {
            var (_, o) = Fixture(); var job = Capture(o, channel); o.PrepareSharedRequest(job);
            Test.True(!(bool)JObject.Parse(job.UserPrompt)["player_is_ruler"]!, "non-ruler authority passed to model");
            Complete(o, job, Extraction());
            Test.True(job.Kind == "oral_review" && job.PrivacyRewrites == 1 && o.CurrentStorage.DialogueArrangements.Count == 0
                && o.CurrentStorage.SpeechReviews.Single().Attempts == 0 && o.CanDispatchDiplomacyJob(job), "one same-day repair queued without false consent: " + channel);
            o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
            job = o.CurrentStorage.Jobs.Single(); o.PrepareSharedRequest(job);
            Test.True((string)JObject.Parse(job.UserPrompt)["validation_error"]! == "speech_player_consent_without_authority"
                && job.SystemPrompt.Contains(DiplomacyPromptTexts.Get("SpeechReview_RepairConsent", "MISSING"))
                && job.PrivacyRewrites == 1, "repair feedback and bound survive reload through actual package reader");
            Complete(o, job, Extraction(false));
            Test.True(job.Kind == "oral_review_audit" && o.CurrentStorage.DialogueArrangements.Count == 0, "corrected extraction still requires independent audit");
            Complete(o, job, JObject.Parse("{\"approved\":true}"));
            var arrangement = o.CurrentStorage.DialogueArrangements.Single();
            Test.True(arrangement.Action == DialogueDiplomaticAction.Alliance && arrangement.TargetKingdomId == "b"
                && arrangement.ConfirmedPlayerRulerId == "" && arrangement.SourceChannel == channel,
                "NPC third-party proposal retained without player or target assent: " + channel);
            Complete(o, job, JObject.Parse("{\"approved\":true}"));
            Test.True(o.CurrentStorage.DialogueArrangements.Count == 1, "late duplicate audit does not register twice");
        }

        var (failedHost, failedOwner) = Fixture(); var failedJob = Capture(failedOwner);
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            Complete(failedOwner, failedJob, Extraction()); Complete(failedOwner, failedJob, Extraction());
            var review = failedOwner.CurrentStorage.SpeechReviews.Single();
            Test.True(review.Attempts == attempt && failedOwner.CurrentStorage.DialogueArrangements.Count == 0
                && !failedOwner.CurrentStorage.Jobs.Contains(failedJob), "repeated wrong assent consumes bounded attempt");
            if (attempt < 3) { failedHost.CurrentDayValue = review.RetryDay; Call(failedOwner, "MaintainSpeechReviews"); failedJob = failedOwner.CurrentStorage.Jobs.Single(); }
        }
        Test.True(failedOwner.CurrentStorage.SpeechReviews.Single().Status == "technical_hold", "three failed review jobs remain held");
        var (oldHost, oldOwner) = Fixture(); Capture(oldOwner); oldOwner.CurrentStorage.Jobs.Clear();
        var oldReview = oldOwner.CurrentStorage.SpeechReviews.Single();
        oldReview.ExtractionContractVersion = 1; oldReview.Status = "technical_hold"; oldReview.Reason = "speech_extraction_invalid"; oldReview.Attempts = 3;
        Call(oldOwner, "MaintainSpeechReviews");
        Test.True(oldReview.ExtractionContractVersion == 3 && oldReview.Status == "pending" && oldReview.Attempts == 0,
            "previous contract's unapplied extraction failure receives one reevaluation");
        oldReview.Status = "technical_hold"; oldReview.Reason = "speech_extraction_invalid"; oldReview.Attempts = 3; oldHost.CurrentDayValue++;
        Call(oldOwner, "MaintainSpeechReviews");
        Test.True(oldReview.Status == "technical_hold" && oldReview.Attempts == 3, "contract migration does not repeatedly restart failures");
        foreach (string change in new[] { "rejected_audit", "invalid_quote", "lost_authority" })
        {
            var (h, o) = Fixture(); h.PlayerRuler = change == "lost_authority";
            var job = Capture(o);
            Complete(o, job, Extraction(h.PlayerRuler, change == "invalid_quote" ? "Fabricated promise" : "I will propose an alliance to b."));
            if (change == "lost_authority") h.PlayerRuler = false;
            if (job.Kind == "oral_review_audit") Complete(o, job, JObject.Parse(change == "rejected_audit" ? "{\"approved\":false}" : "{\"approved\":true}"));
            Test.True(o.CurrentStorage.DialogueArrangements.Count == 0, "guards retained: " + change);
        }
    }
}
